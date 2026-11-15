// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using Stride.Shaders.Compilers;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Spirv.Tools;
using Spv = Stride.Shaders.Spirv.Tools.Spv;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Methods of structured buffers, and the error for a member that buffers don't have (<c>buffer.Length</c>: SDSL0150).
/// The values of <c>StructuredBuffer.GetDimensions</c> and <c>Load</c> are checked by RenderTest <c>StructuredBufferGetDimensions</c>.
/// </summary>
public class BufferMethodTests
{
    // RWStructuredBuffer: valid SPIR-V, and HLSL that FXC accepts
    [Fact]
    public void GetDimensionsCompilesOnRWStructuredBuffer()
    {
        SpirvCrossSupport.SkipUnlessAvailable();
        FxcSupport.SkipUnlessAvailable();

        var shaderMixer = new ShaderMixer(new ShaderLoader("./assets/SDSL/CompilerTests"));
        var log = new Stride.Core.Diagnostics.LoggerResult();
        Assert.True(shaderMixer.MergeSDSL(new ShaderClassSource("CSStructuredBufferGetDimensions"), new ShaderMixer.Options(true), log, out var bytecode, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));

        var validation = Spv.ValidateBinary(bytecode);
        Assert.True(validation.IsValid, validation.Output);

        var legalized = SpirvTools.LegalizeForHlsl(MemoryMarshal.Cast<byte, uint>(bytecode));
        var translator = new SpirvTranslator(legalized.AsMemory());
        var compute = translator.GetEntryPoints().First(x => x.ExecutionModel == ExecutionModel.GLCompute);
        var hlsl = translator.Translate(Backend.Hlsl, compute);
        var errors = FxcSupport.Compile(hlsl, "cs_5_0");
        Assert.True(errors is null, errors + Environment.NewLine + hlsl);
    }

    [Fact]
    public void LengthOnBufferIsAnError()
    {
        // Compile errors are thrown by the loader
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests", "./assets/Stride/SDSL");
        var exception = Assert.Throws<InvalidOperationException>(() => loader.LoadExternalBuffer("BufferLength", [], out _, out _, out _));

        Assert.Contains("SDSL0150: StructuredBuffer<int> has no member 'Length'. To get the size of a buffer, call Indices.GetDimensions(out uint count, out uint stride).", exception.Message);
        Assert.Contains("SDSL0150: RWBuffer<float> has no member 'Count'. To get the size of a buffer, call Values.GetDimensions(out uint width).", exception.Message);
        // The rest of the method still analyzes: no internal error after them
        Assert.DoesNotContain("Unsupported type", exception.Message);
    }
}
