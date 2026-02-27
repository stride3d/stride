using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.HighPerformance;
using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using Stride.Shaders.Compilers;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using Spv = Stride.Shaders.Spirv.Tools.Spv;

namespace Stride.Shaders.Parsers.Tests;

public class StrideShaderTests
{
    [Fact]
    public void Tessellation()
    {
        // Dumped from TessellationTest using ShaderSource.ToCode()
        var shaderSource = new ShaderMixinSource
        {
            Mixins =
{
new ShaderClassSource("ShaderBase"),
new ShaderClassSource("ShadingBase"),
new ShaderClassSource("TransformationBase"),
new ShaderClassSource("NormalStream"),
new ShaderClassSource("TransformationWAndVP"),
new ShaderClassSource("NormalFromMesh"),
new ShaderClassSource("TessellationPN"),
new ShaderClassSource("TessellationAE4","PositionWS"),
new ShaderClassSource("MaterialSurfacePixelStageCompositor"),
},
            Compositions =
{
["environmentLights"] = new ShaderArraySource
{
new ShaderMixinSource
{
Mixins ={new ShaderClassSource("LightSimpleAmbient")},
Macros =
{
new ShaderMacro("InputControlPointCount", "12"),
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
},
["materialPixelStage"] = new ShaderMixinSource
{
Mixins ={new ShaderClassSource("MaterialSurfaceArray")},
Compositions =
{
["layers"] = new ShaderArraySource
{
new ShaderMixinSource
{
Mixins ={new ShaderClassSource("MaterialSurfaceDiffuse")},
Compositions ={["diffuseMap"] = new ShaderClassSource("ComputeColorConstantColorLink","Material.DiffuseValue")},
Macros =
{
new ShaderMacro("InputControlPointCount", "12"),
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
new ShaderMixinSource
{
Mixins ={new ShaderClassSource("MaterialSurfaceLightingAndShading")},
Compositions =
{
["surfaces"] = new ShaderArraySource
{
new ShaderClassSource("MaterialSurfaceShadingDiffuseLambert","false"),
},
},
Macros =
{
new ShaderMacro("InputControlPointCount", "12"),
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
},
},
Macros =
{
new ShaderMacro("InputControlPointCount", "12"),
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
["streamInitializerPixelStage"] = new ShaderMixinSource
{
Mixins =
{
new ShaderClassSource("MaterialStream"),
new ShaderClassSource("MaterialPixelShadingStream"),
},
Macros =
{
new ShaderMacro("InputControlPointCount", "12"),
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
},
},
            Macros =
{
new ShaderMacro("InputControlPointCount", "12"),
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_1", "37120"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_2", "37376"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_9_3", "37632"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_0", "40960"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_10_1", "41216"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_0", "45056"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_1", "45312"),
new ShaderMacro("GRAPHICS_PROFILE_LEVEL_11_2", "45568"),
new ShaderMacro("class", "shader"),
},
        };

        var shaderMixer = new ShaderMixer(new ShaderLoader("./assets/Stride/SDSL"));
        shaderMixer.MergeSDSL(shaderSource, new ShaderMixer.Options(true), new Stride.Core.Diagnostics.LoggerResult(), out var bytecode, out var effectReflection, out _, out _);

        File.WriteAllBytes($"StrideTessellation.spv", bytecode);
        File.WriteAllText($"StrideTessellation.spvdis", Spv.Dis(SpirvBytecode.CreateBufferFromBytecode(bytecode), DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true));

        var translator = new SpirvTranslator(bytecode.ToArray().AsMemory().Cast<byte, uint>());
        var entryPoints = translator.GetEntryPoints();
        var codeHS = translator.Translate(Backend.Hlsl, entryPoints.First(x => x.ExecutionModel == ExecutionModel.TessellationControl));
        var codeDS = translator.Translate(Backend.Hlsl, entryPoints.First(x => x.ExecutionModel == ExecutionModel.TessellationEvaluation));
    }
}
