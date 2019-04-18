// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Core.Diagnostics;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering.Lights;

namespace Xenko.Rendering.Shadows
{
    /// <summary>
    /// Handles rendering of shadow map casters.
    /// </summary>
    [DataContract(DefaultMemberMode = DataMemberMode.Never)]
    public class ShadowMapRenderer : IShadowMapRenderer
    {
        public static readonly ProfilingKey ProfilingKey = new ProfilingKey(nameof(ShadowMapRenderer));

        // TODO: Extract a common interface and implem for shadow renderer (not only shadow maps)
        private readonly int maximumTextureSize = (int)(ReferenceShadowSize * ComputeSizeFactor(LightShadowMapSize.XLarge) * 2.0f);
        private const float ReferenceShadowSize = 1024;

        private readonly List<RenderStage> shadowMapRenderStages;

        private FastListStruct<ShadowMapAtlasTexture> atlases;

        private readonly List<LightShadowMapTexture> shadowMaps = new List<LightShadowMapTexture>();
        
        public ShadowMapRenderer()
        {
            atlases = new FastListStruct<ShadowMapAtlasTexture>(16);
        }

        [DataMember]
        public List<ILightShadowMapRenderer> Renderers { get; } = new List<ILightShadowMapRenderer>();

        public RenderSystem RenderSystem { get; set; }

        public HashSet<RenderView> RenderViewsWithShadows { get; } = new HashSet<RenderView>();

        // TODO
        public IReadOnlyList<RenderStage> ShadowMapRenderStages => shadowMapRenderStages;

        public ILightShadowMapRenderer FindRenderer(IDirectLight light)
        {
            foreach (var renderer in Renderers)
            {
                if (renderer.CanRenderLight(light))
                    return renderer;
            }

            return null;
        }

        public LightShadowMapTexture FindShadowMap(RenderView renderView, RenderLight light)
        {
            foreach (var shadowMap in shadowMaps)
            {
                if (shadowMap.RenderView == renderView && shadowMap.RenderLight == light)
                    return shadowMap;
            }

            return null;
        }

        public void Collect(RenderContext context, Dictionary<RenderView, ForwardLightingRenderFeature.RenderViewLightData> renderViewLightDatas)
        {
            // Reset the state of renderers
            foreach (var renderer in Renderers)
            {
                renderer.Reset(context);
            }

            shadowMaps.Clear();

            foreach (var renderViewData in renderViewLightDatas)
            {
                renderViewData.Value.RenderLightsWithShadows.Clear();

                // Collect shadows only if enabled on this view
                if (!RenderViewsWithShadows.Contains(renderViewData.Key))
                    continue;

                // Gets the current camera
                using (context.PushRenderViewAndRestore(renderViewData.Key))
                {
                    // Make sure the view is collected (if not done previously)
                    context.VisibilityGroup.TryCollect(renderViewData.Key);

                    if (MathUtil.NearEqual(renderViewData.Key.MinimumDistance, renderViewData.Key.MaximumDistance))
                    {
                        renderViewData.Key.MaximumDistance = renderViewData.Key.MinimumDistance + MathUtil.ZeroTolerance;
                    }

                    // Check if there is any shadow receivers at all
                    if (float.IsInfinity(renderViewData.Key.MinimumDistance) || float.IsInfinity(renderViewData.Key.MaximumDistance))
                    {
                        continue;
                    }

                    // Clear atlases
                    foreach (var atlas in atlases)
                    {
                        atlas.Clear();
                    }

                    // Collect all required shadow maps
                    CollectShadowMaps(renderViewData.Key, renderViewData.Value);

                    foreach (var lightShadowMapTexture in renderViewData.Value.RenderLightsWithShadows)
                    {
                        var shadowMapTexture = lightShadowMapTexture.Value;

                        // Could we allocate shadow map? if not, skip
                        if (shadowMapTexture.Atlas == null)
                            continue;

                        // Collect views
                        shadowMapTexture.Renderer.Collect(context, renderViewData.Key, shadowMapTexture);
                    }
                }
            }
        }

        public void PrepareAtlasAsRenderTargets(CommandList commandList)
        {
            // Clear atlases
            foreach (var atlas in atlases)
            {
                atlas.PrepareAsRenderTarget(commandList);
            }
        }

        public void PrepareAtlasAsShaderResourceViews(CommandList commandList)
        {
            foreach (var atlas in atlases)
            {
                atlas.PrepareAsShaderResourceView(commandList);
            }
        }

        public void Flush(RenderDrawContext context)
        {
            RenderViewsWithShadows.Clear();
        }

        public void Draw(RenderDrawContext drawContext)
        {
            var renderSystem = drawContext.RenderContext.RenderSystem;

            // Clear atlases
            PrepareAtlasAsRenderTargets(drawContext.CommandList);

            using (drawContext.PushRenderTargetsAndRestore())
            {
                // Draw all shadow views generated for the current view
                foreach (var renderView in renderSystem.Views)
                {
                    var shadowmapRenderView = renderView as ShadowMapRenderView;
                    if (shadowmapRenderView != null && shadowmapRenderView.RenderView == drawContext.RenderContext.RenderView)
                    {
                        using (drawContext.QueryManager.BeginProfile(Color.Black, ProfilingKey))
                        {
                            var shadowMapRectangle = shadowmapRenderView.Rectangle;
                            drawContext.CommandList.SetRenderTarget(shadowmapRenderView.ShadowMapTexture.Atlas.Texture, null);
                            shadowmapRenderView.ShadowMapTexture.Atlas.MarkClearNeeded();
                            drawContext.CommandList.SetViewport(new Viewport(shadowMapRectangle.X, shadowMapRectangle.Y, shadowMapRectangle.Width, shadowMapRectangle.Height));

                            renderSystem.Draw(drawContext, shadowmapRenderView, renderSystem.RenderStages[shadowmapRenderView.RenderStages[0].Index]);
                        }
                    }
                }
            }

            PrepareAtlasAsShaderResourceViews(drawContext.CommandList);
        }

        private void AssignRectangle(LightShadowMapTexture lightShadowMapTexture)
        {
            var size = lightShadowMapTexture.Size;

            // Try to fit the shadow map into an existing atlas
            ShadowMapAtlasTexture currentAtlas = null;
            foreach (var atlas in atlases)
            {
                if (atlas.TryInsert(size, size, lightShadowMapTexture.CascadeCount, (int index, ref Rectangle rectangle) => lightShadowMapTexture.SetRectangle(index, rectangle)))
                {
                    currentAtlas = atlas;
                    break;
                }
            }

            // Allocate a new atlas texture
            if (currentAtlas == null)
            {
                // For now, our policy is to allow only one shadow map, esp. because we can have only one shadow texture per lighting group
                // TODO: Group by DirectLightGroups, so that we can have different atlas per lighting group
                // TODO: Allow multiple textures per LightingGroup (using array of Texture?)
                if (atlases.Count == 0)
                {
                    // TODO: handle FilterType texture creation here
                    // TODO: This does not work for Omni lights
                    // TODO: Allow format selection externally

                    var texture = Texture.New2D(RenderSystem.GraphicsDevice, maximumTextureSize, maximumTextureSize, 1, PixelFormat.D32_Float, TextureFlags.DepthStencil | TextureFlags.ShaderResource);
                    currentAtlas = new ShadowMapAtlasTexture(texture, atlases.Count) { FilterType = lightShadowMapTexture.FilterType };
                    atlases.Add(currentAtlas);

                    for (int i = 0; i < lightShadowMapTexture.CascadeCount; i++)
                    {
                        var rect = Rectangle.Empty;
                        currentAtlas.Insert(size, size, ref rect);
                        lightShadowMapTexture.SetRectangle(i, rect);
                    }
                }
            }

            // Make sure the atlas cleared (will be clear just once)
            lightShadowMapTexture.Atlas = currentAtlas;
            lightShadowMapTexture.TextureId = (byte)(currentAtlas?.Id ?? 0);
        }

        private void CollectShadowMaps(RenderView renderView, ForwardLightingRenderFeature.RenderViewLightData renderViewLightData)
        {
            // TODO GRAPHICS REFACTOR Only lights of current scene!
            foreach (var renderLight in renderViewLightData.VisibleLightsWithShadows)
            {
                var light = renderLight.Type as IDirectLight;
                if (light == null)
                {
                    continue;
                }

                var shadowMap = light.Shadow;
                if (!shadowMap.Enabled)
                {
                    continue;
                }

                // Check if the light has a shadow map renderer
                var renderer = FindRenderer(light);
                if (renderer == null)
                {
                    continue;
                }

                var direction = renderLight.Direction;
                var position = renderLight.Position;

                // Compute the coverage of this light on the screen
                var size = light.ComputeScreenCoverage(renderView, position, direction);

                // Converts the importance into a shadow size factor
                var sizeFactor = ComputeSizeFactor(shadowMap.Size);

                // Compute the size of the final shadow map
                // TODO: Handle GraphicsProfile
                var shadowMapSize = (int)Math.Min(ReferenceShadowSize * sizeFactor, MathUtil.NextPowerOfTwo(size * sizeFactor));

                if (shadowMapSize <= 0) // TODO: Validate < 0 earlier in the setters
                {
                    continue;
                }

                var shadowMapTexture = renderer.CreateShadowMapTexture(renderView, renderLight, light, shadowMapSize);

                // Assign rectangles for shadowmap
                AssignRectangle(shadowMapTexture);

                renderViewLightData.RenderLightsWithShadows.Add(renderLight, shadowMapTexture);

                shadowMaps.Add(shadowMapTexture);
            }
        }

        private static float ComputeSizeFactor(LightShadowMapSize shadowMapSize)
        {
            // Then reduce the size based on the shadow map size
            var factor = (float)Math.Pow(2.0f, (int)shadowMapSize - 3.0f);
            return factor;
        }
    }
}
