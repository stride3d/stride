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
using SpirvOp = Stride.Shaders.Spirv.Specification.Op;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Texture offsets that are constants only once helpers are inlined and [unroll] loops unrolled become ConstOffset operands,
/// as in DXC; the others are error SDSL0189. The values are checked by the RenderTest <c>TextureOffsetAfterUnroll</c>.
/// </summary>
public class SampleOffsetTests : IDisposable
{
    private const uint OffsetOperand = 0x10;
    private const uint ImageGatherExtended = 25;

    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-sample-offsets");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData(RendererBackend.Direct3D11)]
    [InlineData(RendererBackend.Vulkan)]
    public void OffsetsBecomeConstants(RendererBackend backend)
    {
        Write("UnrolledOffsets", """
            shader UnrolledOffsets
            {
                stream float4 ColorTarget : SV_Target0;
                stream float2 TexCoord : TEXCOORD0;

                Texture2D Height;
                Texture2D Depth;
                SamplerState Point;
                SamplerComparisonState Shadow;

                float Fetch(float2 uv, int2 offset)
                {
                    return Height.SampleLevel(Point, uv, 0, offset).x + Height.Load(int3(0, 0, 0), offset).x
                        + Depth.SampleCmpLevelZero(Shadow, uv, 0.5, offset);
                }

                void PSMain()
                {
                    float sum = 0;
                    [unroll]
                    for (int i = 0; i < 3; i++)
                        sum += Fetch(streams.TexCoord, int2(i - 1, 2));
                    streams.ColorTarget = sum;
                }
            }
            """);

        var (ok, log, bytecode) = Mix("UnrolledOffsets", backend);
        Assert.True(ok, log);

        // Vulkan allows the Offset operand on gathers only, and ImageGatherExtended only with its device feature
        var words = MemoryMarshal.Cast<byte, uint>(bytecode).ToArray();
        var (images, offsets, gatherExtended) = (0, 0, false);
        for (var w = 5; w < words.Length; w += (int)(words[w] >> 16))
        {
            var op = (SpirvOp)(words[w] & 0xFFFF);
            if (op == SpirvOp.OpCapability && words[w + 1] == ImageGatherExtended)
                gatherExtended = true;
            var maskIndex = op switch
            {
                SpirvOp.OpImageSampleExplicitLod or SpirvOp.OpImageFetch => 5,
                SpirvOp.OpImageSampleDrefExplicitLod => 6,
                _ => 0,
            };
            if (maskIndex == 0)
                continue;
            images++;
            if ((words[w + maskIndex] & OffsetOperand) != 0)
                offsets++;
        }
        Assert.Equal(9, images);
        Assert.Equal(0, offsets);
        Assert.False(gatherExtended);

        var validation = Spv.ValidateBinary(bytecode, targetVulkan: backend == RendererBackend.Vulkan);
        Assert.True(validation.IsValid, validation.Output);

        if (backend == RendererBackend.Direct3D11)
        {
            SpirvCrossSupport.SkipUnlessAvailable();
            FxcSupport.SkipUnlessAvailable();
            var translator = new SpirvTranslator(SpirvTools.LegalizeForHlsl(words).AsMemory());
            var entryPoint = translator.GetEntryPoints().Single(x => x.ExecutionModel == ExecutionModel.Fragment);
            Assert.Null(FxcSupport.Compile(translator.Translate(Backend.Hlsl, entryPoint), "ps_5_0", entryPoint.TranslatedName));
        }
    }

    [Theory]
    [InlineData("[loop] for (int i = 0; i < 4; i++) sum += Height.SampleLevel(Point, streams.TexCoord, 0, int2(i, 0)).x;")]
    [InlineData("sum = Height.SampleLevel(Point, streams.TexCoord, 0, Shift).x;")]
    public void VariableOffsetsAreReported(string statement)
    {
        Write("VariableOffset", $$"""
            shader VariableOffset
            {
                stream float4 ColorTarget : SV_Target0;
                stream float2 TexCoord : TEXCOORD0;

                Texture2D Height;
                SamplerState Point;
                int2 Shift;

                void PSMain()
                {
                    float sum = 0;
                    {{statement}}
                    streams.ColorTarget = sum;
                }
            }
            """);

        foreach (var backend in new[] { RendererBackend.Direct3D11, RendererBackend.Vulkan })
        {
            var (ok, log, _) = Mix("VariableOffset", backend);
            Assert.False(ok);
            Assert.Contains("VariableOffset.sdsl(13,", log);
            Assert.Contains("SDSL0189: This texture offset is not a constant", log);
        }
    }

    private void Write(string name, string code) => File.WriteAllText(Path.Combine(sourceDir.FullName, $"{name}.sdsl"), code);

    private (bool Ok, string Log, byte[] Bytecode) Mix(string shaderName, RendererBackend backend)
    {
        var mixer = new ShaderMixer(new ShaderLoader(sourceDir.FullName));
        mixer.ShaderLoader.LoadExternalBuffer(shaderName, [], out _, out _, out _);
        var log = new LoggerResult();
        var options = backend == RendererBackend.Direct3D11
            ? new ShaderMixer.Options(ResourcesRegisterSeparate: true)
            : new ShaderMixer.Options(ResourcesRegisterSeparate: false, StripGoogleUserType: true, Direct3DStructuredBufferLayout: true);
        var ok = mixer.MergeSDSL(new ShaderClassSource(shaderName), options, log, out var bytecode, out _, out _, out _);
        return (ok && !log.HasErrors, string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)), bytecode.ToArray());
    }
}
