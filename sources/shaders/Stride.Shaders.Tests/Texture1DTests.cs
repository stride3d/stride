// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using Stride.Core.Diagnostics;
using Stride.Shaders.Compilers;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Spirv.Tools;
using Spv = Stride.Shaders.Spirv.Tools.Spv;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// 1D textures need the Sampled1D / Image1D capabilities, and their Load coordinate is a scalar.
/// </summary>
public class Texture1DTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-texture1d");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData("Sampled1D", ExecutionModel.Fragment, "ps_5_0", """
        shader Sampled1D
        {
            stream float4 ColorTarget : SV_Target0;
            stream float2 TexCoord : TEXCOORD0;
            Texture1D<float4> Gradient;
            Texture1DArray<float4> Gradients;
            SamplerState Linear;
            void PSMain()
            {
                uint width, levels;
                Gradient.GetDimensions(0, width, levels);
                streams.ColorTarget = Gradient.Sample(Linear, streams.TexCoord.x) + Gradient.SampleLevel(Linear, 0.5, 0)
                    + Gradient.Load(int2(width - 1, 0)) + Gradients.Sample(Linear, float2(streams.TexCoord.x, 1));
            }
        }
        """)]
    [InlineData("Storage1D", ExecutionModel.GLCompute, "cs_5_0", """
        shader Storage1D
        {
            stage stream uint3 DispatchThreadId : SV_DispatchThreadID;
            RWTexture1D<float4> Line;
            RWTexture1DArray<float> Lines;
            [numthreads(8, 1, 1)]
            void CSMain()
            {
                uint x = streams.DispatchThreadId.x;
                Line[x] = Line[x + 1] * 2;
                Lines[uint2(x, 1)] = 1;
            }
        }
        """)]
    public void Texture1DCompiles(string name, ExecutionModel stage, string profile, string code)
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, $"{name}.sdsl"), code);

        // Vulkan: Sampled1D / Image1D
        Mix(name, vulkan: true);

        // Direct3D 11: through SPIRV-Cross and FXC
        SpirvCrossSupport.SkipUnlessAvailable();
        FxcSupport.SkipUnlessAvailable();
        var translator = new SpirvTranslator(SpirvTools.LegalizeForHlsl(MemoryMarshal.Cast<byte, uint>(Mix(name, vulkan: false))).AsMemory());
        var entryPoint = translator.GetEntryPoints().Single(x => x.ExecutionModel == stage);
        Assert.Null(FxcSupport.Compile(translator.Translate(Backend.Hlsl, entryPoint), profile, entryPoint.TranslatedName));
    }

    private byte[] Mix(string shaderName, bool vulkan)
    {
        var mixer = new ShaderMixer(new ShaderLoader(sourceDir.FullName));
        mixer.ShaderLoader.LoadExternalBuffer(shaderName, [], out _, out _, out _);
        var log = new LoggerResult();
        var options = vulkan
            ? new ShaderMixer.Options(ResourcesRegisterSeparate: false, StripGoogleUserType: true, Direct3DStructuredBufferLayout: true)
            : new ShaderMixer.Options(ResourcesRegisterSeparate: true);
        Assert.True(mixer.MergeSDSL(new ShaderClassSource(shaderName), options, log, out var bytecode, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));
        var result = bytecode.ToArray();
        var validation = Spv.ValidateBinary(result, targetVulkan: vulkan);
        Assert.True(validation.IsValid, validation.Output);
        return result;
    }
}
