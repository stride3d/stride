// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Graphics;
using Stride.Shaders.Compiler;
using Stride.Shaders.Compilers.SDSL;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// An effect whose <c>PSMain</c> resolves to an empty override has no pixel shader, as depth-only passes expect
/// (<c>ShadowMapCasterNoPixelShader</c>). A <c>PSMain</c> that does anything, even only <c>clip</c>, keeps it.
/// </summary>
public class EmptyPixelShaderTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-shader-empty-ps");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private static string Base(string pixelShaderBody) => $$"""
        namespace Stride.Shaders.Tests;

        shader EmptyPixelShaderBase
        {
            stream float4 Position : POSITION;
            stream float4 ShadingPosition : SV_Position;
            stream float4 ColorTarget : SV_Target0;

            module void VSMain()
            {
                streams.ShadingPosition = streams.Position;
            }

            module void PSMain()
            {
                {{pixelShaderBody}}
            }
        }
        """;

    [Theory]
    [InlineData(GraphicsPlatform.Direct3D11)]
    [InlineData(GraphicsPlatform.Direct3D12)]
    [InlineData(GraphicsPlatform.Vulkan)]
    public void EmptyOverrideDropsThePixelShader(GraphicsPlatform platform)
    {
        var stages = Mix(platform, "override module void PSMain() { }");
        Assert.Equal(new[] { ShaderStage.Vertex }, stages);
    }

    [Theory]
    [InlineData(GraphicsPlatform.Direct3D11, "override module void PSMain() { clip(-1); }")]
    [InlineData(GraphicsPlatform.Direct3D11, "")]
    public void PixelShaderThatDoesSomethingStays(GraphicsPlatform platform, string members)
    {
        var stages = Mix(platform, members);
        Assert.Equal(new[] { ShaderStage.Vertex, ShaderStage.Pixel }, stages.Order());
    }

    // Only an empty body counts: an override that calls an empty base method keeps a pixel shader that does nothing
    [Fact]
    public void OverrideThatCallsAnEmptyBaseKeepsThePixelShader()
    {
        var stages = Mix(GraphicsPlatform.Direct3D11, "override module void PSMain() { base.PSMain(); }", baseBody: "");
        Assert.Equal(new[] { ShaderStage.Vertex, ShaderStage.Pixel }, stages.Order());
    }

    private ShaderStage[] Mix(GraphicsPlatform platform, string members, string baseBody = "streams.ColorTarget = float4(1.0, 0.0, 0.0, 1.0);")
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, "EmptyPixelShaderBase.sdsl"), Base(baseBody));
        File.WriteAllText(Path.Combine(sourceDir.FullName, "EmptyPixelShader.sdsl"), $$"""
            namespace Stride.Shaders.Tests;

            shader EmptyPixelShader : EmptyPixelShaderBase
            {
                {{members}}
            }
            """);

        var log = new LoggerResult();
        var ok = new ShaderMixer(new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL"))
            .MergeSDSL(new ShaderClassSource("EmptyPixelShader"), EffectCompiler.GetMixerOptions(platform), log, out _, out _, out _, out var entryPoints);
        Assert.True(ok, log.ToText());
        return [.. entryPoints!.Select(x => x.Stage)];
    }
}
