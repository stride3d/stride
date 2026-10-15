// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Shaders.Compilers.SDSL;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Texture and sampler generic parameters are not supported. The compiler reports them on the class
/// instead of failing while it compiles the generic header.
/// </summary>
public class TextureGenericParameterTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-shader-texture-generic");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private const string TextureGeneric = """
        namespace Stride.Shaders.Tests;

        shader TextureGeneric<Texture2D TTexture> : ComputeShaderBase
        {
            RWBuffer<float4> Out;
            SamplerState Sampler;
            override void Compute() { Out[0] = TTexture.SampleLevel(Sampler, float2(0, 0), 0); }
        }
        """;

    private const string SamplerGeneric = """
        namespace Stride.Shaders.Tests;

        shader SamplerGeneric<SamplerState TSampler> : ComputeShaderBase
        {
            RWBuffer<float4> Out;
            Texture2D Texture;
            override void Compute() { Out[0] = Texture.SampleLevel(TSampler, float2(0, 0), 0); }
        }
        """;

    [Fact]
    public void TextureGenericParameterIsReported()
    {
        var (ok, log) = Mix("TextureGeneric", TextureGeneric, "Texturing.Texture0");
        Assert.False(ok);
        Assert.Contains("SDSL0118: TextureGeneric declares generic parameter 'TTexture' of type Texture2D at line 3", log);
    }

    [Fact]
    public void SamplerGenericParameterIsReported()
    {
        var (ok, log) = Mix("SamplerGeneric", SamplerGeneric, "Texturing.LinearSampler");
        Assert.False(ok);
        Assert.Contains("SDSL0118: SamplerGeneric declares generic parameter 'TSampler' of type SamplerState at line 3", log);
    }

    private (bool ok, string log) Mix(string name, string code, string genericValue)
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, $"{name}.sdsl"), code);

        var loader = new ShaderLoader(sourceDir.FullName, "./assets/Stride/SDSL");
        var source = new ShaderMixinSource { Mixins = { new ShaderClassSource(name, genericValue) } };

        var log = new LoggerResult();
        var ok = new ShaderMixer(loader).MergeSDSL(source, new ShaderMixer.Options(true), log, out _, out _, out _, out _);

        return (ok, string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));
    }
}
