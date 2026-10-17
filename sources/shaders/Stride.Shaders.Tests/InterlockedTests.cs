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
/// Interlocked* destinations that DXC rejects or Stride doesn't support get a coded error, and texel destinations compile.
/// The values are checked by the ComputeTests <c>CSInterlocked*</c>.
/// </summary>
public class InterlockedTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-interlocked");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    [Theory]
    [InlineData("InterlockedAdd(Floats[0], 1);", "SDSL0185: InterlockedAdd works on an int or uint, but its destination Floats[0] has type float")]
    [InlineData("InterlockedMax(Pairs[0], 1);", "SDSL0185: InterlockedMax works on an int or uint, but its destination Pairs[0] has type int2")]
    [InlineData("InterlockedAdd(Pairs[0].xy, 1);", "SDSL0185: InterlockedAdd works on an int or uint, but its destination Pairs[0].xy has type int2")]
    [InlineData("int local = 0; InterlockedAdd(local, 1);", "SDSL0186: The destination of InterlockedAdd, local, is a local variable or a parameter")]
    [InlineData("InterlockedAdd(Shared, 1);", "SDSL0186: The destination of InterlockedAdd, Shared, is a static variable or a stream")]
    [InlineData("InterlockedAdd(ReadOnly[0], 1);", "SDSL0186: The destination of InterlockedAdd, ReadOnly[0], is an element of a read-only buffer")]
    [InlineData("InterlockedAdd(Constant, 1);", "SDSL0186: The destination of InterlockedAdd, Constant, is a constant buffer member")]
    [InlineData("InterlockedAdd(Values[0] + 1, 1);", "SDSL0186: The destination of InterlockedAdd, ( Values[0] + 1 ), is a value, not a variable")]
    [InlineData("float original; InterlockedExchange(Floats[0], 1.5, original);", "SDSL0187: InterlockedExchange on a float is not supported")]
    [InlineData("InterlockedCompareStoreFloatBitwise(Floats[0], 0.0, 1.5);", "SDSL0187: InterlockedCompareStoreFloatBitwise on a float is not supported")]
    [InlineData("float original; Raw.InterlockedExchangeFloat(0, 1.5, original);", "SDSL0187: InterlockedExchangeFloat on a float is not supported")]
    [InlineData("Raw.InterlockedAdd64(0, 1ull);", "SDSL0188: InterlockedAdd64: 64-bit atomic operations are not supported")]
    public void InvalidDestinationsAreReported(string statement, string message)
    {
        var (ok, log) = Compile("CheckAtomic", $$"""
            shader CheckAtomic
            {
                RWStructuredBuffer<int> Values;
                RWStructuredBuffer<float> Floats;
                RWStructuredBuffer<int2> Pairs;
                StructuredBuffer<int> ReadOnly;
                RWByteAddressBuffer Raw;
                static int Shared;
                int Constant;

                [numthreads(1, 1, 1)]
                void CSMain()
                {
                    {{statement}}
                }
            }
            """);

        Assert.False(ok);
        Assert.Contains("CheckAtomic.sdsl(14,", log);
        Assert.Contains(message, log);
    }

    [Theory]
    [InlineData("RWBuffer<int>", "0")]
    [InlineData("RWTexture2D<uint>", "uint2(1, 2)")]
    [InlineData("RWTexture2D<int>", "uint2(1, 2)")]
    [InlineData("RWTexture2DArray<uint>", "uint3(1, 2, 3)")]
    [InlineData("RWTexture3D<int>", "uint3(1, 2, 3)")]
    public void TexelDestinationsCompile(string type, string coordinate)
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, "TexelAtomic.sdsl"), $$"""
            shader TexelAtomic
            {
                {{type}} Texels;
                RWStructuredBuffer<uint> Output;

                [numthreads(1, 1, 1)]
                void CSMain()
                {
                    int original;
                    InterlockedAdd(Texels[{{coordinate}}], -1, original);
                    InterlockedMax(Texels[{{coordinate}}], 2);
                    Output[0] = original;
                }
            }
            """);

        // Vulkan: the image declares R32i / R32ui, which OpImageTexelPointer needs
        Mix("TexelAtomic", RendererBackend.Vulkan);

        // Direct3D 11: through SPIRV-Cross and FXC
        SpirvCrossSupport.SkipUnlessAvailable();
        FxcSupport.SkipUnlessAvailable();
        var legalized = SpirvTools.LegalizeForHlsl(MemoryMarshal.Cast<byte, uint>(Mix("TexelAtomic", RendererBackend.Direct3D11)));
        var translator = new SpirvTranslator(legalized.AsMemory());
        var entryPoint = translator.GetEntryPoints().Single(x => x.ExecutionModel == ExecutionModel.GLCompute);
        var hlsl = translator.Translate(Backend.Hlsl, entryPoint);
        Assert.Contains("InterlockedAdd(", hlsl);
        Assert.Null(FxcSupport.Compile(hlsl, "cs_5_0", entryPoint.TranslatedName));
    }

    private byte[] Mix(string shaderName, RendererBackend backend)
    {
        var mixer = new ShaderMixer(new ShaderLoader(sourceDir.FullName));
        mixer.ShaderLoader.LoadExternalBuffer(shaderName, [], out _, out _, out _);
        var log = new LoggerResult();
        var options = backend == RendererBackend.Direct3D11
            ? new ShaderMixer.Options(ResourcesRegisterSeparate: true)
            : new ShaderMixer.Options(ResourcesRegisterSeparate: false, StripGoogleUserType: true, Direct3DStructuredBufferLayout: true);
        Assert.True(mixer.MergeSDSL(new ShaderClassSource(shaderName), options, log, out var bytecode, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));
        var result = bytecode.ToArray();
        var validation = Spv.ValidateBinary(result, targetVulkan: backend == RendererBackend.Vulkan);
        Assert.True(validation.IsValid, validation.Output);
        return result;
    }

    // Compiles one class alone, like the first effect that needs it would
    private (bool Ok, string Log) Compile(string name, string code)
    {
        File.WriteAllText(Path.Combine(sourceDir.FullName, $"{name}.sdsl"), code);
        var log = new LoggerResult();
        var loader = new ShaderLoader(sourceDir.FullName) { Log = log };
        bool ok;
        try
        {
            ok = loader.LoadExternalBuffer(name, [], out _, out _, out _);
        }
        catch (InvalidOperationException)
        {
            ok = false;
        }
        return (ok && !log.HasErrors, string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));
    }
}
