// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Shaders.Compilers.SDSL;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// A method with the signature of an inherited one must be marked <c>override</c>. Without it, the method
/// would start a separate method group and calls through the base would never reach it, so the compiler
/// reports it on the method itself.
/// </summary>
public class ImplicitOverrideTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-shader-override");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private const string Contract = """
        namespace Stride.Shaders.Tests;

        shader OverrideContract
        {
            abstract int Value();
            int Offset() { return 0; }
        }
        """;

    private const string ExplicitImplementation = """
        namespace Stride.Shaders.Tests;

        shader OverrideImplementation : OverrideContract
        {
            override int Value() { return 7; }
            override int Offset() { return 1; }
        }
        """;

    private const string MissingAbstractOverride = """
        namespace Stride.Shaders.Tests;

        shader OverrideImplementation : OverrideContract
        {
            int Value() { return 7; }
        }
        """;

    private const string MissingOverride = """
        namespace Stride.Shaders.Tests;

        shader OverrideImplementation : OverrideContract
        {
            override int Value() { return 7; }
            int Offset() { return 1; }
        }
        """;

    private const string Root = """
        namespace Stride.Shaders.Tests;

        shader OverrideRoot : ComputeShaderBase
        {
            compose OverrideContract contract;
            RWBuffer<int> Out;
            override void Compute() { Out[0] = contract.Value() + contract.Offset(); }
        }
        """;

    [Fact]
    public void ExplicitOverridesAreCalledThroughAComposition()
    {
        var (ok, log) = Mix(ExplicitImplementation);
        Assert.True(ok, log);
    }

    [Fact]
    public void MissingOverrideOfAnAbstractMethodIsReportedOnTheImplementation()
    {
        var (ok, log) = Mix(MissingAbstractOverride);
        Assert.False(ok);
        Assert.Contains("SDSL0114: OverrideImplementation.Value() at line 5 implements abstract method OverrideContract.Value", log);
    }

    [Fact]
    public void MissingOverrideOfAnInheritedMethodIsReportedOnTheImplementation()
    {
        var (ok, log) = Mix(MissingOverride);
        Assert.False(ok);
        Assert.Contains("SDSL0115: OverrideImplementation.Offset() at line 6 hides inherited method OverrideContract.Offset", log);
    }

    private (bool ok, string log) Mix(string implementation)
    {
        Write("OverrideContract", Contract);
        Write("OverrideImplementation", implementation);
        Write("OverrideRoot", Root);

        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL");
        var source = new ShaderMixinSource
        {
            Mixins = { new ShaderClassSource("OverrideRoot") },
            Compositions = { ["contract"] = new ShaderMixinSource { Mixins = { new ShaderClassSource("OverrideImplementation") } } },
        };

        var log = new LoggerResult();
        var ok = new ShaderMixer(loader).MergeSDSL(source, new ShaderMixer.Options(true), log, out _, out _, out _, out _);

        return (ok, string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));
    }

    private void Write(string name, string code)
        => File.WriteAllText(Path.Combine(sourceDir.FullName, $"{name}.sdsl"), code);
}
