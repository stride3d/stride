/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using Stride.Rendering;
using Stride.Engine.Shaders.Mixins;
using Stride.Core.IO;
using Stride.Shaders.Compiler;
using Stride.Core.Shaders.Utility;

namespace Stride.Core.Shaders.Tests
{
    class TestRealMix
    {
        private ShaderSourceManager sourceManager;
        private ShaderLoader shaderLoader;

        [SetUp]
        public void Init()
        {
            sourceManager = new ShaderSourceManager();
            sourceManager.LookupDirectoryList.Add(@"../../../../../shaders");
            shaderLoader = new ShaderLoader(sourceManager);
        }
        [Fact]
        public void TestModule() // simple mix with inheritance
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("AlbedoDiffuseBase"),
                    new ShaderClassSource("AlbedoFlatShading"),
                    new ShaderClassSource("AlbedoSpecularBase"),
                    new ShaderClassSource("AmbientShading"),
                    new ShaderClassSource("BRDFDiffuseBase"),
                    new ShaderClassSource("BRDFSpecularBase"),
                    new ShaderClassSource("Camera"),
                    new ShaderClassSource("ColorBase"),
                    new ShaderClassSource("ComposeToneMap"),
                    new ShaderClassSource("ComputeBRDFColor"),
                    new ShaderClassSource("ComputeBRDFColorFresnel"),
                    new ShaderClassSource("ComputeBRDFColorSpecularBlinnPhong"),
                    new ShaderClassSource("ComputeBRDFDiffuseLambert"),
                    new ShaderClassSource("ComputeColor"),
                    new ShaderClassSource("ComputeColor3"),
                    new ShaderClassSource("ComputeColorAdd"),
                    new ShaderClassSource("ComputeColorAdd3"),
                    new ShaderClassSource("ComputeColorCave"),
                    new ShaderClassSource("ComputeColorFixed"),
                    new ShaderClassSource("ComputeColorLerpAlpha"),
                    new ShaderClassSource("ComputeColorMachoLifebar"),
                    new ShaderClassSource("ComputeColorMultiply"),
                    new ShaderClassSource("ComputeColorOutdoor"),
                    new ShaderClassSource("ComputeColorOverlay"),
                    new ShaderClassSource("ComputeColorScaler"),
                    new ShaderClassSource("ComputeColorStream"),
                    new ShaderClassSource("ComputeColorSubstituteAlpha"),
                    new ShaderClassSource("ComputeColorSynthetic"),
                    new ShaderClassSource("ComputeColorTexture"),
                    new ShaderClassSource("ComputeColorTextureDisplacement"),
                    new ShaderClassSource("ComputeColorTextureRepeat"),
                    new ShaderClassSource("ComputeColorTextureRepeatMacho"),
                    new ShaderClassSource("ComputeFloat"),
                    new ShaderClassSource("ComputeMagmaNormals"),
                    new ShaderClassSource("ComputeShaderBase"),
                    new ShaderClassSource("ComputeSkyboxColor"),
                    new ShaderClassSource("ComputeSkyboxDomeColorTexture"),
                    new ShaderClassSource("ComputeSkyboxGroundColorTexture"),
                    new ShaderClassSource("ComputeToneMap"),
                    new ShaderClassSource("DepthBase"),
                    new ShaderClassSource("DiscardTransparent"),
                    new ShaderClassSource("EditorIcon"),
                    new ShaderClassSource("GBuffer"),
                    new ShaderClassSource("GBufferBase"),
                    new ShaderClassSource("Global"),
                    new ShaderClassSource("LightDeferredShading"),
                    new ShaderClassSource("LightDirectionalBase"),
                    new ShaderClassSource("LightDirectionalComputeColor"),
                    new ShaderClassSource("LightDirectionalShading"),
                    new ShaderClassSource("LightDirectionalShadingTerrain"),
                    new ShaderClassSource("LightMultiDirectionalShadingDiffusePerPixel"),
                    new ShaderClassSource("LightMultiDirectionalShadingPerPixel"),
                    new ShaderClassSource("LightMultiDirectionalShadingPerVertex"),
                    new ShaderClassSource("LightMultiDirectionalShadingSpecularPerPixel"),
                    new ShaderClassSource("LightPrepass"),
                    new ShaderClassSource("LightPrepassDebug"),
                    new ShaderClassSource("LightShadingBase"),
                    new ShaderClassSource("Material"),
                    new ShaderClassSource("MinMaxBounding"),
                    new ShaderClassSource("Noise2dBase"),
                    new ShaderClassSource("Noise3dBase"),
                    new ShaderClassSource("Noise4dBase"),
                    new ShaderClassSource("NoiseBase"),
                    new ShaderClassSource("NormalMapTexture"),
                    new ShaderClassSource("NormalPack"),
                    new ShaderClassSource("NormalSkinning"),
                    new ShaderClassSource("NormalStream"),
                    new ShaderClassSource("NormalBase"),
                    new ShaderClassSource("NormalVSGBuffer"),
                    new ShaderClassSource("NormalVSStream"),
                    new ShaderClassSource("Particle"),
                    new ShaderClassSource("ParticleBase"),
                    new ShaderClassSource("ParticleBillboard"),
                    new ShaderClassSource("ParticleBitonicSort1", 0),
                    new ShaderClassSource("ParticleBitonicSort2", 1),
                    new ShaderClassSource("ParticleRenderBase"),
                    new ShaderClassSource("ParticleRenderTest1"),
                    new ShaderClassSource("ParticleSimpleDataBase"),
                    new ShaderClassSource("ParticleSortInitializer"),
                    new ShaderClassSource("ParticleUpdaterBase", 0),

                    new ShaderClassSource("ParticleUpdaterTest1"),
                    new ShaderClassSource("PickingGBuffer"),
                    new ShaderClassSource("PickingGS"),
                    new ShaderClassSource("PickingRasterizer"),
                    new ShaderClassSource("PositionHStream4"),
                    new ShaderClassSource("PositionStream"),
                    new ShaderClassSource("PositionStream2"),
                    new ShaderClassSource("PositionStream4"),
                    new ShaderClassSource("PositionVSBase"),
                    new ShaderClassSource("PositionVSGBuffer"),
                    new ShaderClassSource("PositionVertexTransform"),
                    new ShaderClassSource("PostEffectBase"),
                    new ShaderClassSource("PostEffectBilateralGaussian"),
                    new ShaderClassSource("PostEffectBlur"),
                    new ShaderClassSource("PostEffectBlur5x5"),
                    new ShaderClassSource("PostEffectBlurHVsm"),
                    new ShaderClassSource("PostEffectBoundingRay"),
                    new ShaderClassSource("PostEffectBrightFilter"),
                    new ShaderClassSource("PostEffectBrightPass"),
                    new ShaderClassSource("PostEffectFXAA"),
                    new ShaderClassSource("PostEffectHBAO"),
                    new ShaderClassSource("PostEffectHBAOBlur"),
                    new ShaderClassSource("PostEffectHeatShimmer"),
                    new ShaderClassSource("PostEffectHeatShimmerDisplay"),
                    new ShaderClassSource("PostEffectLightShafts"),
                    new ShaderClassSource("PostEffectLightShaftsNoise"),
                    new ShaderClassSource("PostEffectMinMax"),
                    new ShaderClassSource("PostEffectTexturing"),
                    new ShaderClassSource("PostEffectTexturing2"),
                    new ShaderClassSource("PostEffectTransition"),
                    new ShaderClassSource("ShaderBase"),
                    new ShaderClassSource("ShaderBaseTessellation"),
                    new ShaderClassSource("ShadingBase"),
                    new ShaderClassSource("ShadingColor"),
                    new ShaderClassSource("ShadingOverlay"),
                    new ShaderClassSource("ShadowBase"),
                    new ShaderClassSource("ShadowMap"),
                    new ShaderClassSource("ShadowMapBase"),
                    new ShaderClassSource("ShadowMapCascadeBase"),
                    new ShaderClassSource("ShadowMapCasterBase"),
                    new ShaderClassSource("ShadowMapColor"),
                    new ShaderClassSource("ShadowMapFilterBase"),
                    new ShaderClassSource("ShadowMapFilterDefault"),
                    new ShaderClassSource("ShadowMapFilterPcf"),
                    new ShaderClassSource("ShadowMapFilterVsm"),
                    new ShaderClassSource("ShadowMapReceiver"),
                    new ShaderClassSource("ShadowMapUtils"),
                    new ShaderClassSource("SimplexNoise"),
                    new ShaderClassSource("SkyBox"),
                    new ShaderClassSource("SkyBoxDomeDragon"),
                    new ShaderClassSource("SpecularPowerBase"),
                    new ShaderClassSource("SpecularPowerGBuffer"),
                    new ShaderClassSource("SpecularPowerPerMesh"),
                    new ShaderClassSource("SwapUV"),
                    new ShaderClassSource("TangentSkinning"),
                    new ShaderClassSource("TessellationAEN"),
                    new ShaderClassSource("TessellationDisplacement"),
                    //new ShaderClassSource("TessellationDisplacementAEN"),
                    new ShaderClassSource("TessellationFlat"),
                    new ShaderClassSource("TessellationPN"),
                    new ShaderClassSource("TextureKey"),
                    new ShaderClassSource("TextureStream"),
                    new ShaderClassSource("Texturing"),
                    new ShaderClassSource("Transformation"),
                    new ShaderClassSource("TransformationBase"),
                    new ShaderClassSource("TransformationSkinning"),
                    new ShaderClassSource("TransformationWVP"),
                    new ShaderClassSource("TransformationZero"),
                    new ShaderClassSource("TransparentShading"),
                    new ShaderClassSource("TurbulenceDynamicNoise"),
                    new ShaderClassSource("TurbulenceNoiseBase"),
                    new ShaderClassSource("Utilities"),
                    new ShaderClassSource("Wireframe")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            if (mcm.ErrorWarningLog.HasErrors)
            {
                Console.WriteLine("----------------------------ERRORS FOUND-----------------------------------");
                foreach (var message in mcm.ErrorWarningLog.Messages.Where(x => x.Level == ReportMessageLevel.Error))
                    Console.WriteLine(message);
                Console.WriteLine("-----------------------------ERRORS END------------------------------------");

                Console.WriteLine("----------------------------WARNINGS FOUND-----------------------------------");
                foreach (var message in mcm.ErrorWarningLog.Messages.Where(x => x.Level == ReportMessageLevel.Warning))
                    Console.WriteLine(message);
                Console.WriteLine("-----------------------------WARNINGS END------------------------------------");
            }

            Assert.False(mcm.ErrorWarningLog.HasErrors);
        }

        [Fact]
        public void TestModuleShort() // simple mix with inheritance
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("TessellationDisplacement", 100, 8),
                    new ShaderClassSource("ShaderBaseTessellation"),
                    new ShaderClassSource("TessellationFlat"),
                    new ShaderClassSource("ShaderBase"),
                    new ShaderClassSource("Texturing"),
                    new ShaderClassSource("NormalBase"),
                    new ShaderClassSource("NormalVSStream"),
                    new ShaderClassSource("NormalStream"),
                    new ShaderClassSource("ComputeColor"),
                    new ShaderClassSource("PositionVertexTransform"),
                    new ShaderClassSource("Transformation"),
                    new ShaderClassSource("TransformationBase"),
                    new ShaderClassSource("PositionVSBase"),
                    new ShaderClassSource("PositionStream"),
                    new ShaderClassSource("PositionStream4"),
                    new ShaderClassSource("Camera"),
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            if (mcm.ErrorWarningLog.HasErrors)
            {
                Console.WriteLine("----------------------------ERRORS FOUND-----------------------------------");
                foreach (var message in mcm.ErrorWarningLog.Messages.Where(x => x.Level == ReportMessageLevel.Error))
                    Console.WriteLine(message);
                Console.WriteLine("-----------------------------ERRORS END------------------------------------");

                Console.WriteLine("----------------------------WARNINGS FOUND-----------------------------------");
                foreach (var message in mcm.ErrorWarningLog.Messages.Where(x => x.Level == ReportMessageLevel.Warning))
                    Console.WriteLine(message);
                Console.WriteLine("-----------------------------WARNINGS END------------------------------------");
            }

            Assert.False(mcm.ErrorWarningLog.HasErrors);
        }
    }
}
*/