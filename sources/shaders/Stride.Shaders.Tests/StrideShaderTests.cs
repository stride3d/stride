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
    public void TextureDecorateStringWarning()
    {
        var shaderSource = new ShaderMixinSource
        {
            Mixins =
{
new ShaderClassSource("ShaderBase"),
new ShaderClassSource("ShadingBase"),
new ShaderClassSource("TransformationBase"),
new ShaderClassSource("NormalStream"),
new ShaderClassSource("TransformationWAndVP"),
new ShaderClassSource("NormalFromNormalMapping"),
new ShaderClassSource("MaterialSurfacePixelStageCompositor"),
},
            Compositions =
{
["directLightGroups"] = new ShaderArraySource
{
new ShaderMixinSource
{
Mixins ={new ShaderClassSource("LightClusteredPointGroup")},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
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
Mixins ={new ShaderClassSource("LightClusteredSpotGroup")},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
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
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
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
Mixins ={new ShaderClassSource("EnvironmentLight")},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
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
Compositions ={["diffuseMap"] = new ShaderClassSource("ComputeColorTextureScaledOffsetDynamicSampler","Material.DiffuseMap","TEXCOORD0","Material.Sampler.i0","rgba","Material.TextureScale","Material.TextureOffset")},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
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
Mixins ={new ShaderClassSource("MaterialSurfaceNormalMap","true","true")},
Compositions ={["normalMap"] = new ShaderClassSource("ComputeColorTextureScaledOffsetDynamicSampler","Material.NormalMap","TEXCOORD0","Material.Sampler.i0","rgba","Material.TextureScale.i1","Material.TextureOffset.i1")},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
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
Mixins ={new ShaderClassSource("MaterialSurfaceGlossinessMap","false")},
Compositions ={["glossinessMap"] = new ShaderClassSource("ComputeColorTextureScaledOffsetDynamicSampler","Material.GlossinessMap","TEXCOORD0","Material.Sampler.i0","r","Material.TextureScale.i2","Material.TextureOffset.i2")},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
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
Mixins ={new ShaderClassSource("MaterialSurfaceMetalness")},
Compositions ={["metalnessMap"] = new ShaderClassSource("ComputeColorConstantFloatLink","Material.MetalnessValue")},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
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
new ShaderMixinSource
{
Mixins ={new ShaderClassSource("MaterialSurfaceShadingSpecularMicrofacet")},
Compositions =
{
["environmentFunction"] = new ShaderClassSource("MaterialSpecularMicrofacetEnvironmentGGXLUT"),
["fresnelFunction"] = new ShaderClassSource("MaterialSpecularMicrofacetFresnelSchlick"),
["geometricShadowingFunction"] = new ShaderClassSource("MaterialSpecularMicrofacetVisibilitySmithSchlickGGX"),
["normalDistributionFunction"] = new ShaderClassSource("MaterialSpecularMicrofacetNormalDistributionGGX"),
},
Macros =
{
new ShaderMacro("STRIDE_RENDER_TARGET_COUNT", "1"),
new ShaderMacro("STRIDE_MULTISAMPLE_COUNT", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D", "1"),
new ShaderMacro("STRIDE_GRAPHICS_API_DIRECT3D11", "1"),
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
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
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
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
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
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
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
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
new ShaderMacro("STRIDE_GRAPHICS_PROFILE", "40960"),
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

        var log = new Stride.Core.Diagnostics.LoggerResult();
        var shaderMixer = new ShaderMixer(new ShaderLoader("./assets/Stride/SDSL"));
        shaderMixer.MergeSDSL(shaderSource, new ShaderMixer.Options(true), log, out var bytecode, out var effectReflection, out _, out _);

        var warnings = log.Messages
            .Where(m => m.Type == Stride.Core.Diagnostics.LogMessageType.Warning && m.Text.Contains("Mismatched decorations"))
            .Select(m => m.Text).ToList();
        Assert.Empty(warnings);
    }


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

        TestCore("StrideTessellation", shaderSource, "./assets/Stride/SDSL");
    }

    private static void TestCore(string shaderName, ShaderMixinSource shaderSource, params string[] searchPaths)
    {
        var shaderMixer = new ShaderMixer(new ShaderLoader(searchPaths));
        var log = new Stride.Core.Diagnostics.LoggerResult();
        shaderMixer.MergeSDSL(shaderSource, new ShaderMixer.Options(true), log, out var bytecode, out var effectReflection, out _, out _);

        if (log.HasErrors)
            Assert.Fail(string.Join(Environment.NewLine, log.Messages.Where(m => m.Type == Stride.Core.Diagnostics.LogMessageType.Error).Select(m => m.Text)));

        File.WriteAllBytes($"{shaderName}.spv", bytecode);
        File.WriteAllText($"{shaderName}.spvdis", Spv.Dis(SpirvBytecode.CreateFromSpan(bytecode), DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true));

        // Validate SPIR-V
        var validationResult = Spv.ValidateFile($"{shaderName}.spv");
        Assert.True(validationResult.IsValid, validationResult.Output);

        var translator = new SpirvTranslator(bytecode.ToArray().AsMemory().Cast<byte, uint>());
        var entryPoints = translator.GetEntryPoints();
        foreach (var entryPoint in entryPoints)
        {
            var hlsl = translator.Translate(Backend.Hlsl, entryPoint);
            Console.WriteLine(hlsl);
        }
    }
}
