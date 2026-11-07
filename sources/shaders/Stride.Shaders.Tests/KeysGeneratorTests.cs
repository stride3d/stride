// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Stride.Shaders.Parsing.SDFX;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// The keys generator (<see cref="EffectGenerator"/>) on a shader, then the C# compiler on what it generates: the keys build.
/// </summary>
public class KeysGeneratorTests
{
    // A key of a struct type needs a C# struct of the same name (written by hand, like DirectionalLightData); static and
    // groupshared members are not parameters
    [Fact]
    public void GeneratedKeysBuild()
    {
        var shader = """
            namespace Stride.Shaders.Tests.Keys;

            shader KeysGeneratorCheck
            {
                struct KeysWithCSharpType
                {
                    float4 A;
                };

                struct KeysWithoutCSharpType
                {
                    float4 B;
                };

                static KeysWithoutCSharpType StaticStruct;
                groupshared float4 Shared[4];
                KeysWithoutCSharpType GlobalStruct;

                cbuffer PerDraw
                {
                    KeysWithCSharpType WithType;
                    KeysWithoutCSharpType WithoutType;
                    float4 Plain;
                };
            };
            """;
        var csharp = """
            namespace Stride.Shaders.Tests.Keys
            {
                public struct KeysWithCSharpType
                {
                    public Stride.Core.Mathematics.Vector4 A;
                }
            }
            """;

        var (generated, errors) = Build(shader, csharp);

        Assert.Empty(errors);
        Assert.Contains("ValueParameterKey<KeysWithCSharpType> WithType", generated);
        Assert.Contains("ValueParameterKey<Vector4> Plain", generated);
        Assert.Contains("// No key for WithoutType: no C# type KeysWithoutCSharpType for its struct type", generated);
        Assert.Contains("// No key for GlobalStruct: no C# type KeysWithoutCSharpType for its struct type", generated);
        Assert.DoesNotContain("StaticStruct", generated);
        Assert.DoesNotContain("Shared", generated);
    }

    // An initializer list as a default value: the C# constructor takes the same values in the same order
    [Fact]
    public void InitializerListDefaultsBuild()
    {
        var shader = """
            namespace Stride.Shaders.Tests.Keys;

            shader KeysGeneratorCheck
            {
                cbuffer PerDraw
                {
                    float3 Direction = { 0, -1.5, 1 };
                    int2 Cell = { 1, -2 };
                    float4x4 Transform = { {1, 0, 0, 0}, {0, 1, 0, 0}, {0, 0, 1, 0}, {0, 0, 0, 1} };
                    float2 NotLiteral = { Cell.x, 0 };
                };
            };
            """;

        var (generated, errors) = Build(shader, "");

        Assert.Empty(errors);
        Assert.Contains("ParameterKeys.NewValue<Vector3>(new Vector3(0f, -1.5f, 1f))", generated);
        Assert.Contains("ParameterKeys.NewValue<Int2>(new Int2(1, -2))", generated);
        Assert.Contains("ParameterKeys.NewValue<Matrix>(new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f))", generated);
        // A default the key can't hold: none
        Assert.Contains("ParameterKeys.NewValue<Vector2>()", generated);
    }

    [Fact]
    public void PreprocessorErrorNamesTheFileAndLine()
    {
        var (generated, errors) = Build("shader KeysGeneratorCheck { };\n#error bad thing\n", "");

        Assert.Equal("// The errors of this file are in the build output\n", generated);
        Assert.Contains("KeysGeneratorCheck.sdsl(2,2): #error bad thing", Assert.Single(errors));
    }

    // The compiler doesn't show an #error of generated code when the project's own code has errors: the error is a diagnostic of the file
    [Theory]
    [InlineData("Shader.sdsl", "shader S\n{\n    float F() { return 1 }\n};", "Shader.sdsl(3,")]
    [InlineData("Effects.sdfx", "namespace Game\n{\n    effect E\n    {\n        using params P;\n        using params P;\n    }\n}", "Effects.sdfx(6,9): error SDSL: Unexpected declaration of using params. This variable is already declared in this scope")]
    [InlineData("Effects.sdfx", "namespace Game\n{\n    shader S { float4 F() { return 0; } };\n}", "Effects.sdfx(3,5): error SDSL: 'Effects.sdfx' contains a 'shader' declaration.")]
    [InlineData("Effects.sdfx", "namespace Game\n{\n    shader E\n    {\n        mixin A;\n        mixin child B;\n    };\n}", "Effects.sdfx(3,5): error SDSL0313: An .sdfx file declares an effect with 'effect', not 'shader': write 'effect E'")]
    [InlineData("Effects.sdfx", "partial shader E { mixin A; }", "Effects.sdfx(1,9): error SDSL0313: An .sdfx file declares an effect with 'effect', not 'shader': write 'partial effect E'")]
    public void AnErrorOfAShaderFileIsADiagnosticOfThatFile(string path, string text, string message)
    {
        var (generated, errors) = Build([(path, text)], "using Game.Missing; class UsesTheEffects { }");

        Assert.Equal("// The errors of this file are in the build output\n", generated);
        Assert.Contains(errors, x => x.Contains(message));
        Assert.Contains(errors, x => x.Contains("error CS0246"));
    }

    private static (string Generated, List<string> Errors) Build(string shader, string csharp) => Build([("KeysGeneratorCheck.sdsl", shader)], csharp);

    // Runs the generator on the shader files and compiles its output with the C# source
    private static (string Generated, List<string> Errors) Build((string Path, string Text)[] files, string csharp)
    {
        // The assemblies the generated keys use
        var references = new[] { typeof(object), typeof(Stride.Rendering.ParameterKeys), typeof(Stride.Core.Mathematics.Vector4), typeof(Stride.Graphics.Texture), typeof(Stride.Core.DataContractAttribute), typeof(Stride.Shaders.ShaderMixinSource) }
            .Select(x => x.Assembly.Location)
            .Concat(Directory.EnumerateFiles(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "*.dll").Where(x => Path.GetFileName(x) is "System.Runtime.dll" or "netstandard.dll" or "System.Collections.dll"))
            .Distinct()
            .Select(x => MetadataReference.CreateFromFile(x))
            .ToList();
        var compilation = CSharpCompilation.Create("KeysGeneratorCheck", [CSharpSyntaxTree.ParseText(csharp)], references, new(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new EffectGenerator().AsSourceGenerator()], additionalTexts: [.. files.Select(x => new ShaderText(x.Path, x.Text))]);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);

        var generated = string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(x => x.ToString()));
        var errors = generatorDiagnostics.Concat(output.GetDiagnostics()).Where(x => x.Severity == DiagnosticSeverity.Error).Select(x => x.ToString()).ToList();
        return (generated, errors);
    }

    private sealed class ShaderText(string path, string text) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(text, Encoding.UTF8);
    }
}
