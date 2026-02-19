using System;
using System.Collections.Generic;
using System.Text;
using Stride.Shaders.Compilers.SDSL;

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
new ShaderClassSource("TessellationFlat"),
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
        shaderMixer.MergeSDSL(shaderSource, new ShaderMixer.Options(true), out var bytecode, out var effectReflection, out _, out _);
    }
}
