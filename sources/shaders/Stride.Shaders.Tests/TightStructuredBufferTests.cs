// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Spirv.Tools;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// For the APIs that read the SPIR-V layout (Vulkan, Direct3D 12), structured buffers whose std430 layout differs from Direct3D's use
/// Direct3D's tight layout, and only those: the values are checked by RenderTest <c>StructuredBufferTightLayout</c>.
/// </summary>
public class TightStructuredBufferTests
{
    static byte[] Compile(string shaderName, bool tightLayout, out Stride.Shaders.EffectReflection reflection)
    {
        var shaderMixer = new ShaderMixer(new ShaderLoader("./assets/SDSL/CompilerTests"));
        shaderMixer.ShaderLoader.LoadExternalBuffer(shaderName, [], out _, out _, out _);
        var log = new LoggerResult();
        var options = new ShaderMixer.Options(ResourcesRegisterSeparate: false, StripGoogleUserType: true, Direct3DStructuredBufferLayout: tightLayout);
        Assert.True(shaderMixer.MergeSDSL(new ShaderClassSource(shaderName), options, log, out var bytecode, out var effectReflection, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));
        reflection = effectReflection!;
        return bytecode.ToArray();
    }

    static string? TightBuffers(byte[] spirv) => SpirvTools.TightStructuredBuffers(System.Runtime.InteropServices.MemoryMarshal.Cast<byte, uint>(spirv));

    [Fact]
    public void OnlyBuffersWhoseLayoutDiffersAreTight()
    {
        var spirv = Compile("TightLayoutMarker", tightLayout: true, out _);
        // float3 elements, float3x3 elements, also writable; not float4 nor { float3; float } (std430 places them as Direct3D does)
        Assert.Equal(["TightLayoutMarker_M33", "TightLayoutMarker_Output", "TightLayoutMarker_V3"], TightBuffers(spirv)!.Split(' ').Order());
        Assert.True(Spv.ValidateBinary(spirv, targetVulkan: true).IsValid);

        // Direct3D 11 packs the HLSL itself: its SPIR-V keeps std430
        Assert.Null(TightBuffers(Compile("TightLayoutMarker", tightLayout: false, out _)));
    }

    [Fact]
    public void CBuffersAndByteAddressBuffersKeepTheirLayout()
    {
        var withOption = Compile("NoTightLayout", tightLayout: true, out var reflection);
        var withoutOption = Compile("NoTightLayout", tightLayout: false, out _);
        Assert.Null(TightBuffers(withOption));
        Assert.Equal(withoutOption, withOption);

        // Direct3D cbuffer packing: Scale fills the float3's register, Color starts the next one
        var perDraw = reflection.ConstantBuffers.Single(x => x.Name == "PerDraw");
        Assert.Equal([0, 12, 16], perDraw.Members.Select(x => x.Offset));
    }

    // The mark is what the Vulkan backend reads before creating the pipeline (PipelineState.Vulkan.CreateShaderStages): it must reach
    // the stage bytecode through the effect compiler, and survive the serialization of the effect cache and precompiled bytecode
    [Fact]
    public void TheMarkReachesTheVulkanBytecode()
    {
        using var provider = new Stride.Core.IO.FileSystemProvider($"/tightlayout-{Guid.NewGuid():N}", AppContext.BaseDirectory);
        using var compiler = new Stride.Shaders.Compiler.EffectCompiler(provider) { ValidateSpirv = true };
        compiler.SourceDirectories.Add("assets/SDSL/CompilerTests");

        var mixin = new ShaderMixinSource { Name = "TightLayoutMarker" };
        mixin.Mixins.Add(new ShaderClassSource("TightLayoutMarker"));
        var effectParameters = Stride.Shaders.Compiler.EffectCompilerParameters.Default with { Platform = Stride.Graphics.GraphicsPlatform.Vulkan };
        var result = compiler.Compile(mixin, effectParameters, new Stride.Shaders.Compiler.CompilerParameters(), Stride.Core.Storage.ObjectId.New()).WaitForResult();
        Assert.False(result.CompilationLog.HasErrors, string.Join(Environment.NewLine, result.CompilationLog.Messages.Select(m => m.Text)));

        using var stream = new MemoryStream();
        result.Bytecode.WriteTo(stream);
        stream.Position = 0;
        var reloaded = Stride.Shaders.EffectBytecode.FromStream(stream);

        foreach (var stage in result.Bytecode.Stages.Concat(reloaded.Stages))
            Assert.Equal("TightLayoutMarker_M33 TightLayoutMarker_Output TightLayoutMarker_V3", string.Join(' ', TightBuffers(stage.Data)!.Split(' ').Order()));
    }

    // Direct3D 12 compiles the SPIR-V with spirv_to_dxil, which reads the byte offsets from it: it must take the tight layout
    [Fact]
    public unsafe void Direct3D12CompilesTheTightLayout()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Skip("spirv_to_dxil is only shipped for Windows");

        var shaderMixer = new ShaderMixer(new ShaderLoader("./assets/SDSL/RenderTests"));
        shaderMixer.ShaderLoader.LoadExternalBuffer("StructuredBufferTightLayout", [], out _, out _, out _);
        var log = new LoggerResult();
        // The options EffectCompiler uses for Direct3D 12
        var options = new ShaderMixer.Options(ResourcesRegisterSeparate: false, Direct3DStructuredBufferLayout: true);
        Assert.True(shaderMixer.MergeSDSL(new ShaderClassSource("StructuredBufferTightLayout"), options, log, out var bytecode, out _, out _, out var entryPoints),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));
        Assert.NotNull(TightBuffers(bytecode.ToArray()));

        var words = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, uint>(bytecode).ToArray();
        var conf = new Stride.Shaders.Compilers.Direct3D.RuntimeConf
        {
            runtime_data_cbv = { base_shader_register = 0, register_space = 31 },
            push_constant_cbv = { base_shader_register = 1, register_space = 31 },
            yzflip_mode = Stride.Shaders.Compilers.Direct3D.FlipMode.YZFlipNone,
            shader_model_max = Stride.Shaders.Compilers.Direct3D.dxil_shader_model.SHADER_MODEL_6_2,
        };
        var debug = new Stride.Shaders.Compilers.Direct3D.DebugOptions();
        var logger = new Stride.Shaders.Compilers.Direct3D.DXILSpirvLogger { log = &IgnoreLog };
        fixed (uint* w = words)
        {
            Assert.True(Stride.Shaders.Compilers.Direct3D.Spv2DXIL.spirv_to_dxil(w, words.Length, null, 0, Stride.Shaders.Compilers.Direct3D.ShaderStage.DXIL_SPIRV_SHADER_FRAGMENT,
                entryPoints!.Single(e => e.Name.Contains("PS")).Name, Stride.Shaders.Compilers.Direct3D.ValidatorVersion.DXIL_VALIDATOR_1_4, ref debug, ref conf, ref logger, out var dxil));
            Stride.Shaders.Compilers.Direct3D.Spv2DXIL.spirv_to_dxil_free(ref dxil);
        }
    }

    [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    static unsafe void IgnoreLog(void* priv, byte* message) { }

    // The other structured buffer kinds: not supported (their own error), so no layout to choose
    [Theory]
    [InlineData("Append")]
    [InlineData("Consume")]
    public void AppendAndConsumeBuffersAreStillRejected(string kind)
    {
        var loader = new ShaderLoader("./assets/SDSL/CompilerTests");
        var exception = Assert.Throws<InvalidOperationException>(() => loader.LoadExternalBuffer($"{kind}BufferMember", [], out _, out _, out _));
        Assert.Contains($"{kind}StructuredBuffer is not supported", exception.Message);
    }
}
