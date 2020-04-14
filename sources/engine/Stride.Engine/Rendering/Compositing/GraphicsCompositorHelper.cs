// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Background;
using Stride.Rendering.Images;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Shadows;
using Stride.Rendering.Sprites;

namespace Stride.Rendering.Compositing
{
    /// <summary>
    /// Helper functions for creating <see cref="GraphicsCompositor"/>.
    /// </summary>
    public static class GraphicsCompositorHelper
    {
        /// <summary>
        /// Creates a typical graphics compositor programatically. It can render meshes, sprites and backgrounds.
        /// </summary>
        public static GraphicsCompositor CreateDefault(bool enablePostEffects, string modelEffectName = "StrideForwardShadingEffect", CameraComponent camera = null, Color4? clearColor = null, GraphicsProfile graphicsProfile = GraphicsProfile.Level_10_0, RenderGroupMask groupMask = RenderGroupMask.All)
        {
            var opaqueRenderStage = new RenderStage("Opaque", "Main") { SortMode = new StateChangeSortMode() };
            var transparentRenderStage = new RenderStage("Transparent", "Main") { SortMode = new BackToFrontSortMode() };
            var shadowCasterRenderStage = new RenderStage("ShadowMapCaster", "ShadowMapCaster") { SortMode = new FrontToBackSortMode() };
            var shadowCasterCubeMapRenderStage = new RenderStage("ShadowMapCasterCubeMap", "ShadowMapCasterCubeMap") { SortMode = new FrontToBackSortMode() };
            var shadowCasterParaboloidRenderStage = new RenderStage("ShadowMapCasterParaboloid", "ShadowMapCasterParaboloid") { SortMode = new FrontToBackSortMode() };

            var postProcessingEffects = enablePostEffects
                ? new PostProcessingEffects
                {
                    ColorTransforms =
                    {
                        Transforms =
                        {
                            new ToneMap(),
                        },
                    },
                }
                : null;

            if (postProcessingEffects != null)
            {
                postProcessingEffects.DisableAll();
                postProcessingEffects.ColorTransforms.Enabled = true;
            }

            var singleView = new ForwardRenderer
            {
                Clear = { Color = clearColor ?? Color.CornflowerBlue },
                OpaqueRenderStage = opaqueRenderStage,
                TransparentRenderStage = transparentRenderStage,
                ShadowMapRenderStages = { shadowCasterRenderStage, shadowCasterParaboloidRenderStage, shadowCasterCubeMapRenderStage },
                PostEffects = postProcessingEffects,
            };

            var forwardLighting = graphicsProfile >= GraphicsProfile.Level_10_0
                ? new ForwardLightingRenderFeature
                {
                    LightRenderers =
                    {
                        new LightAmbientRenderer(),
                        new LightSkyboxRenderer(),
                        new LightDirectionalGroupRenderer(),
                        new LightPointGroupRenderer(),
                        new LightSpotGroupRenderer(),
                        new LightClusteredPointSpotGroupRenderer(),
                    },
                    ShadowMapRenderer = new ShadowMapRenderer
                    {
                        Renderers =
                        {
                            new LightDirectionalShadowMapRenderer
                            {
                                ShadowCasterRenderStage = shadowCasterRenderStage,
                            },
                            new LightSpotShadowMapRenderer
                            {
                                ShadowCasterRenderStage = shadowCasterRenderStage,
                            },
                            new LightPointShadowMapRendererParaboloid
                            {
                                ShadowCasterRenderStage = shadowCasterParaboloidRenderStage,
                            },
                            new LightPointShadowMapRendererCubeMap
                            {
                                ShadowCasterRenderStage = shadowCasterCubeMapRenderStage,
                            },
                        },
                    },
                }
                : new ForwardLightingRenderFeature
                {
                    LightRenderers =
                    {
                        new LightAmbientRenderer(),
                        new LightDirectionalGroupRenderer(),
                        new LightSkyboxRenderer(),
                        new LightPointGroupRenderer(),
                        new LightSpotGroupRenderer(),
                    },
                };

            var cameraSlot = new SceneCameraSlot();
            if (camera != null)
                camera.Slot = cameraSlot.ToSlotId();

            return new GraphicsCompositor
            {
                Cameras =
                {
                    cameraSlot,
                },
                RenderStages =
                {
                    opaqueRenderStage,
                    transparentRenderStage,
                    shadowCasterRenderStage,
                    shadowCasterParaboloidRenderStage,
                    shadowCasterCubeMapRenderStage,
                },
                RenderFeatures =
                {
                    new MeshRenderFeature
                    {
                        RenderFeatures =
                        {
                            new TransformRenderFeature(),
                            new SkinningRenderFeature(),
                            new MaterialRenderFeature(),
                            new ShadowCasterRenderFeature(),
                            forwardLighting,
                        },
                        RenderStageSelectors =
                        {
                            new MeshTransparentRenderStageSelector
                            {
                                EffectName = modelEffectName,
                                OpaqueRenderStage = opaqueRenderStage,
                                TransparentRenderStage = transparentRenderStage,
                                RenderGroup = groupMask,
                            },
                            new ShadowMapRenderStageSelector
                            {
                                EffectName = modelEffectName + ".ShadowMapCaster",
                                ShadowMapRenderStage = shadowCasterRenderStage,
                                RenderGroup = groupMask,
                            },
                            new ShadowMapRenderStageSelector
                            {
                                EffectName = modelEffectName + ".ShadowMapCasterParaboloid",
                                ShadowMapRenderStage = shadowCasterParaboloidRenderStage,
                                RenderGroup = groupMask,
                            },
                            new ShadowMapRenderStageSelector
                            {
                                EffectName = modelEffectName + ".ShadowMapCasterCubeMap",
                                ShadowMapRenderStage = shadowCasterCubeMapRenderStage,
                                RenderGroup = groupMask,
                            },
                        },
                        PipelineProcessors =
                        {
                            new MeshPipelineProcessor { TransparentRenderStage = transparentRenderStage },
                            new ShadowMeshPipelineProcessor { ShadowMapRenderStage = shadowCasterRenderStage },
                            new ShadowMeshPipelineProcessor { ShadowMapRenderStage = shadowCasterParaboloidRenderStage, DepthClipping = true },
                            new ShadowMeshPipelineProcessor { ShadowMapRenderStage = shadowCasterCubeMapRenderStage, DepthClipping = true },
                        },
                    },
                    new SpriteRenderFeature
                    {
                        RenderStageSelectors =
                        {
                            new SpriteTransparentRenderStageSelector
                            {
                                EffectName = "Test",
                                OpaqueRenderStage = opaqueRenderStage,
                                TransparentRenderStage = transparentRenderStage,
                                RenderGroup = groupMask,
                            },
                        },
                    },
                    new BackgroundRenderFeature
                    {
                        RenderStageSelectors =
                        {
                            new SimpleGroupToRenderStageSelector
                            {
                                RenderStage = opaqueRenderStage,
                                EffectName = "Test",
                                RenderGroup = groupMask,
                            },
                        },
                    },
                },
                Game = new SceneCameraRenderer()
                {
                    Child = singleView,
                    Camera = cameraSlot,
                },
                Editor = singleView,
                SingleView = singleView,
            };
        }
    }
}
