// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Member declarations that can't be compiled get a coded error at the member: an initial value that is not a constant (SDSL0173),
/// and a cbuffer with both stage and non-stage members (SDSL0174). The fixes they give are RenderTests
/// <c>StaticFromMember</c> and <c>CBufferSplitStage</c>.
/// </summary>
public class MemberDeclarationErrorTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-shader-member-declarations");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData("float2 R = ViewSize;", "R", "ViewSize", "float2")]
    [InlineData("float Scale = ViewSize.x * 2;", "Scale", "ViewSize.x * 2", "float")]
    public void InitialValueFromAnotherMemberIsAnError(string declaration, string name, string value, string type)
    {
        var message = CompileError("MemberFromMember", $$"""
            shader MemberFromMember
            {
                float2 ViewSize;
                {{declaration}}
                float4 Use() { return ViewSize.xyxy; }
            };
            """);
        Assert.Contains($"SDSL0173: The initial value of member {name} ({value}) is not a constant.", message);
        Assert.Contains($"declare it static (static {type} {name} = {value};)", message);
    }

    [Fact]
    public void ConstantAndStaticInitialValuesCompile()
    {
        Compile("MemberInitialValues", """
            shader MemberInitialValues
            {
                float2 ViewSize;
                float Threshold = .5;
                float2 Offset = float2(1, 2) * 3;
                static float2 R = ViewSize;
                float4 Use() { return float4(R * Threshold, Offset); }
            };
            """);
    }

    [Theory]
    // Stride3D_Demos' AtmosphereForwardRender
    [InlineData("float BottomRadius; stage float4 Slices;")]
    [InlineData("stage float4 Slices; float BottomRadius;")]
    public void CBufferWithStageAndNonStageMembersIsAnError(string members)
    {
        var message = CompileError("MixedStageCBuffer", $$"""
            shader MixedStageCBuffer
            {
                cbuffer PerView.Atmosphere { {{members}} }
            };
            """);
        Assert.Contains("SDSL0174: cbuffer PerView.Atmosphere mixes stage and non-stage members (Slices is stage, BottomRadius is not).", message);
    }

    private void Compile(string name, string code)
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, $"{name}.sdsl"), code);
        new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL").LoadExternalBuffer(name, [], out _, out _, out _);
    }

    private string CompileError(string name, string code)
        => Assert.Throws<InvalidOperationException>(() => Compile(name, code)).Message;
}
