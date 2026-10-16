// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;
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
/// Methods with structured or byte address buffer parameters: the mixer gives each call a copy of the method that uses the
/// call's global buffer, as SPIR-V only accepts a StorageBuffer pointer argument with variable pointers. The value checks are
/// RenderTests (<c>BufferParameter*</c>) and ComputeTests (<c>CSBufferParameter*</c>).
/// </summary>
public class BufferParameterTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-buffer-parameters");

    public void Dispose()
    {
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void EachSetOfBuffersGetsOneCopy()
    {
        // Get(Source) twice and Get(Reversed) once
        var module = SpirvModule.Read(Mix("./assets/SDSL/RenderTests", "BufferParameterRead", RendererBackend.Vulkan));

        Assert.Equal(["BufferParameterRead.Get_Reversed", "BufferParameterRead.Get_Source"], module.FunctionNames.Where(x => x.Contains(".Get")).Order());
        Assert.Empty(module.BufferParameters);
    }

    [Fact]
    public void NestedCallsUseCopiesToo()
    {
        // Copy(First, Input) and Copy(Second, Input) call Set and Get with their parameters
        var module = SpirvModule.Read(Mix("./assets/SDSL/RenderTests", "BufferParameterNested", RendererBackend.Vulkan));

        Assert.Equal(
            ["BufferParameterNested.Copy_First_Input", "BufferParameterNested.Copy_Second_Input", "BufferParameterNested.Get_Input", "BufferParameterNested.Set_First", "BufferParameterNested.Set_Second"],
            module.FunctionNames.Where(x => x.Contains(".Copy") || x.Contains(".Get") || x.Contains(".Set")).Order());
        Assert.Empty(module.BufferParameters);
    }

    [Fact]
    public void Direct3D11LegalizationInlinesTheCopies()
    {
        SpirvCrossSupport.SkipUnlessAvailable();
        FxcSupport.SkipUnlessAvailable();

        // The Direct3D 11 path: the copies are plain functions, which spirv-opt inlines like any other
        var words = MemoryMarshal.Cast<byte, uint>(Mix("./assets/SDSL/ComputeTests", "CSBufferParameterNested", RendererBackend.Direct3D11)).ToArray();
        var legalized = SpirvTools.LegalizeForHlsl(words);

        Assert.Null(SpirvTools.Validate(legalized));
        Assert.Equal(0, SpirvModule.Read(MemoryMarshal.AsBytes(legalized.AsSpan()).ToArray()).Calls);

        var translator = new SpirvTranslator(legalized.AsMemory());
        var entryPoint = translator.GetEntryPoints().Single(x => x.ExecutionModel == ExecutionModel.GLCompute);
        Assert.Null(FxcSupport.Compile(translator.Translate(Backend.Hlsl, entryPoint), "cs_5_0", entryPoint.TranslatedName));
    }

    [Theory]
    [InlineData("AppendStructuredBuffer<float4> values", "Parameter 'values' of Use: AppendStructuredBuffer is not supported")]
    [InlineData("ConsumeStructuredBuffer<float4> values", "Parameter 'values' of Use: ConsumeStructuredBuffer is not supported")]
    public void AppendAndConsumeParametersAreReported(string parameter, string message)
    {
        var (ok, log) = Compile("CheckAppend", $$"""
            shader CheckAppend
            {
                void Use({{parameter}}) { }
            }
            """);

        Assert.False(ok);
        Assert.Contains($"SDSL0180: {message}", log);
    }

    [Theory]
    [InlineData("out", "RWStructuredBuffer<float4>")]
    [InlineData("inout", "StructuredBuffer<float4>")]
    [InlineData("out", "RWByteAddressBuffer")]
    public void OutBufferParametersAreReported(string modifier, string type)
    {
        var (ok, log) = Compile("CheckOut", $$"""
            shader CheckOut
            {
                void Use({{modifier}} {{type}} values) { }
            }
            """);

        Assert.False(ok);
        Assert.Contains($"SDSL0181: Buffer parameter 'values' of Use is '{modifier}'", log);
    }

    [Theory]
    [InlineData("StructuredBuffer<float4>", "RWStructuredBuffer<float4>")]
    [InlineData("RWStructuredBuffer<float4>", "StructuredBuffer<float4>")]
    [InlineData("StructuredBuffer<float3>", "StructuredBuffer<float4>")]
    public void ABufferOfAnotherTypeIsReported(string bufferType, string parameterType)
    {
        // DXC and FXC have no conversion between buffer types; the copy would use the buffer as the parameter's type
        File.WriteAllText(Path.Combine(sourceDir.FullName, "CheckType.sdsl"), $$"""
            shader CheckType
            {
                {{bufferType}} Values;
                RWStructuredBuffer<float4> Output;

                float4 Get({{parameterType}} values) { return values[0].xyzz; }

                [numthreads(1, 1, 1)]
                void CSMain() { Output[0] = Get(Values); }
            }
            """);

        var log = new LoggerResult();
        var mixer = new ShaderMixer(new ShaderLoader(sourceDir.FullName));
        Assert.False(mixer.MergeSDSL(new ShaderClassSource("CheckType"), new ShaderMixer.Options(true), log, out _, out _, out _, out _));
        // The class compile reports it first (SDSL0161, the argument check of a single-signature call)
        Assert.Contains(log.Messages, m => m.Text.Contains($"CheckType.sdsl(9,") && m.Text.Contains($"SDSL0161: Argument 1 of the call to Get({parameterType}) has type {bufferType}"));
    }

    private static byte[] Mix(string folder, string shaderName, RendererBackend backend)
    {
        var mixer = new ShaderMixer(new ShaderLoader(folder));
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

    /// <summary>
    /// The function names, the parameters that are StorageBuffer pointers and the number of calls of a module.
    /// </summary>
    private sealed record SpirvModule(List<string> FunctionNames, List<string> BufferParameters, int Calls)
    {
        public static SpirvModule Read(byte[] bytecode)
        {
            const uint storageBuffer = 12;
            var words = MemoryMarshal.Cast<byte, uint>(bytecode);
            var names = new Dictionary<uint, string>();
            var storageBufferPointers = new HashSet<uint>();
            var functions = new List<uint>();
            var bufferParameters = new List<string>();
            var calls = 0;
            for (var w = 5; w < words.Length; w += (int)(words[w] >> 16))
            {
                switch ((SpirvOp)(words[w] & 0xFFFF))
                {
                    case SpirvOp.OpName:
                        var text = MemoryMarshal.AsBytes(words.Slice(w + 2, (int)(words[w] >> 16) - 2));
                        names[words[w + 1]] = Encoding.UTF8.GetString(text[..text.IndexOf((byte)0)]);
                        break;
                    case SpirvOp.OpTypePointer when words[w + 2] == storageBuffer:
                        storageBufferPointers.Add(words[w + 1]);
                        break;
                    case SpirvOp.OpFunction:
                        functions.Add(words[w + 2]);
                        break;
                    case SpirvOp.OpFunctionParameter when storageBufferPointers.Contains(words[w + 1]):
                        bufferParameters.Add(names.GetValueOrDefault(words[w + 2], $"%{words[w + 2]}"));
                        break;
                    case SpirvOp.OpFunctionCall:
                        calls++;
                        break;
                }
            }
            return new([.. functions.Select(x => names.GetValueOrDefault(x, $"%{x}"))], bufferParameters, calls);
        }
    }
}
