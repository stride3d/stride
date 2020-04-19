// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering.Shadows;
using Stride.Shaders;

namespace Stride.Rendering.Lights
{
    /// <summary>
    /// Light renderer for <see cref="LightSpot"/>.
    /// Handles grouping by shadow type and projection texture.
    /// </summary>
    public class LightSpotGroupRenderer : LightGroupRendererDynamic
    {
        private readonly ShadowComparer shadowComparer = new ShadowComparer();
        private FastListStruct<LightDynamicEntry> processedLights = new FastListStruct<LightDynamicEntry>(8);
        private readonly FastList<LightShaderGroupEntry<LightGroupKey>> lightShaderGroups = new FastList<LightShaderGroupEntry<LightGroupKey>>();
        private readonly Dictionary<TextureProjectionRendererKey, ITextureProjectionRenderer> textureProjectionRenderers = new Dictionary<TextureProjectionRendererKey, ITextureProjectionRenderer>();
        private readonly Dictionary<LightGroupKey, LightShaderGroupDynamic> lightShaderGroupPool = new Dictionary<LightGroupKey, LightShaderGroupDynamic>();

        private struct SpotLightGroupParameters
        {
            public LightShadowType ShadowType;
            public ILightShadowMapRenderer ShadowRenderer;
            public SpotLightTextureParameters SpotParameters;

            public static SpotLightGroupParameters Null = new SpotLightGroupParameters
            {
                ShadowType = 0,
                ShadowRenderer = null,
                SpotParameters = SpotLightTextureParameters.Default,
            };

            public bool Equals(ref SpotLightGroupParameters other)
            {
                return ShadowType == other.ShadowType && ShadowRenderer == other.ShadowRenderer && SpotParameters.Equals(ref other.SpotParameters);
            }
        }

        public override Type[] LightTypes { get; } = { typeof(LightSpot) };

        public override LightShaderGroupDynamic CreateLightShaderGroup(RenderDrawContext context, ILightShadowMapShaderGroupData shadowShaderGroupData)
        {
            // TODO: This function does not receive any ITextureProjectionShaderGroupData! One of the consequences is the fact that light shafts wont support texture projection.
            return new SpotLightShaderGroup(context.RenderContext, shadowShaderGroupData, null);
        }

        public override void Reset()
        {
            base.Reset();

            lightShaderGroups.Clear();
            //textureProjectionRenderers.Clear();   // TODO: MEMORY: Ideally this shouldn't be cleared every frame (because it would cause the renderers to be reallocated every frame)! But the question is, when should we clear it?

            // TODO: MEMORY: Ideally this should also be cleared at some point.
            foreach (var lightShaderGroup in lightShaderGroupPool)
            {
                lightShaderGroup.Value.Reset();
            }
        }

        public override void SetViews(FastList<RenderView> views)
        {
            base.SetViews(views);

            foreach (var lightShaderGroup in lightShaderGroupPool)
            {
                lightShaderGroup.Value.SetViews(views);
            }
        }
        
        private ILightShadowMapShaderGroupData CreateShadowMapShaderGroupData(ILightShadowMapRenderer shadowRenderer, LightShadowType shadowType)
        {
            ILightShadowMapShaderGroupData shadowGroupData = shadowRenderer?.CreateShaderGroupData(shadowType);
            return shadowGroupData;
        }
        
        private ITextureProjectionShaderGroupData CreateTextureProjectionShaderGroupData(ITextureProjectionRenderer textureProjectionRenderer)
        {
            ITextureProjectionShaderGroupData textureProjectionShaderGroupData = textureProjectionRenderer?.CreateShaderGroupData();
            return textureProjectionShaderGroupData;
        }

        private LightShaderGroupDynamic FindOrCreateLightShaderGroup(LightGroupKey lightGroupKey, ProcessLightsParameters parameters)
        {
            LightShaderGroupDynamic lightShaderGroup;

            // Check to see if this combination of parameters has already been stored as a group:
            if (!lightShaderGroupPool.TryGetValue(lightGroupKey, out lightShaderGroup))
            {
                // If a group with the same key has not already been added, create it:
                ILightShadowMapShaderGroupData shadowMapGroupData = CreateShadowMapShaderGroupData(lightGroupKey.ShadowRenderer, lightGroupKey.ShadowType);
                ITextureProjectionShaderGroupData textureProjectionGroupData = CreateTextureProjectionShaderGroupData(lightGroupKey.TextureProjectionRenderer);

                lightShaderGroup = new SpotLightShaderGroup(parameters.Context.RenderContext, shadowMapGroupData, textureProjectionGroupData);
                lightShaderGroup.SetViews(parameters.Views);

                lightShaderGroupPool.Add(lightGroupKey, lightShaderGroup);
            }

            return lightShaderGroup;
        }

        private ITextureProjectionRenderer FindOrCreateTextureProjectionRenderer(SpotLightGroupParameters groupParamaters)
        {
            if (groupParamaters.SpotParameters.ProjectionTexture == null)
            {
                return null;    // If no projection texture is set for this group, it means it doesn't require a texture projection renderer.
            }

            // Check if a texture projection renderer with the desired properties has already been created:
            var textureProjectionRendererKey = new TextureProjectionRendererKey(groupParamaters.SpotParameters);
            ITextureProjectionRenderer textureProjectionRenderer = null;

            if (textureProjectionRenderers.TryGetValue(textureProjectionRendererKey, out textureProjectionRenderer))
            {
                // The desired texture projection renderer has already been created. Therefore we will reuse that one.
                return textureProjectionRenderer;
            }

            // If the desired texture projection renderer is not already present in the dictionary, we create and add it:
            textureProjectionRenderer = new LightSpotTextureProjectionRenderer(groupParamaters.SpotParameters);
            textureProjectionRenderers.Add(textureProjectionRendererKey, textureProjectionRenderer);

            return textureProjectionRenderer;
        }

        public override void ProcessLights(ProcessLightsParameters parameters)
        {
            if (parameters.LightCollection.Count == 0)
                return;

            // Check if we have a fallback renderer next in the chain, in case we don't need shadows
            bool hasNextRenderer = parameters.RendererIndex < (parameters.Renderers.Length - 1);

            var currentGroupParameters = SpotLightGroupParameters.Null;

            // Start by filtering/sorting what can be processed
            shadowComparer.ShadowMapTexturesPerLight = parameters.ShadowMapTexturesPerLight;
            shadowComparer.Lights = parameters.LightCollection;
            parameters.LightIndices.Sort(0, parameters.LightIndices.Count, shadowComparer);

            // Loop over the number of lights + 1 where the last iteration will always flush the last batch of lights
            for (int j = 0; j < parameters.LightIndices.Count + 1;)
            {
                // TODO: Eventually move this loop to a separate function that returns a structure.

                // These variables will contain the relevant parameters of the next usable light:
                var nextGroupParameters = SpotLightGroupParameters.Null;
                LightShadowMapTexture nextShadowTexture = null;
                RenderLight nextLight = null;

                // Find the next light whose attributes aren't null:
                if (j < parameters.LightIndices.Count)
                {
                    nextLight = parameters.LightCollection[parameters.LightIndices[j]];
                    
                    if (nextLight.Type is LightSpot spotLight)
                    {
                        if (spotLight.ProjectiveTexture != null) // TODO: Remove this branch?!
                        {
                            nextGroupParameters.SpotParameters.ProjectionTexture = spotLight.ProjectiveTexture;
                            nextGroupParameters.SpotParameters.FlipMode = spotLight.FlipMode;
                            nextGroupParameters.SpotParameters.UVScale = spotLight.UVScale;
                            nextGroupParameters.SpotParameters.UVOffset = spotLight.UVOffset;
                        }
                    }

                    if (parameters.ShadowMapRenderer != null
                        && parameters.ShadowMapTexturesPerLight.TryGetValue(nextLight, out nextShadowTexture)
                        && nextShadowTexture.Atlas != null) // atlas could not be allocated? treat it as a non-shadowed texture
                    {
                        nextGroupParameters.ShadowType = nextShadowTexture.ShadowType;
                        nextGroupParameters.ShadowRenderer = nextShadowTexture.Renderer;
                    }
                }

                // Flush current group
                // If we detect that the previous light's attributes don't match the next one's, create a new group (or add to an existing one that has the same attributes):
                if (j == parameters.LightIndices.Count || !currentGroupParameters.Equals(ref nextGroupParameters))
                {
                    if (processedLights.Count > 0)
                    {
                        ITextureProjectionRenderer currentTextureProjectionRenderer = FindOrCreateTextureProjectionRenderer(currentGroupParameters);

                        var lightGroupKey = new LightGroupKey(currentGroupParameters.ShadowRenderer, currentGroupParameters.ShadowType, currentTextureProjectionRenderer);
                        LightShaderGroupDynamic lightShaderGroup = FindOrCreateLightShaderGroup(lightGroupKey, parameters);

                        // Add view and lights to the current group:
                        var allowedLightCount = lightShaderGroup.AddView(parameters.ViewIndex, parameters.View, processedLights.Count);
                        for (int i = 0; i < allowedLightCount; ++i)
                        {
                            LightDynamicEntry light = processedLights[i];
                            lightShaderGroup.AddLight(light.Light, light.ShadowMapTexture);
                        }

                        // TODO: assign extra lights to non-shadow rendering if possible
                        //for (int i = lightCount; i < processedLights.Count; ++i)
                        //    XXX.AddLight(processedLights[i], null);

                        // Add the current light shader group to the collection if it hasn't already been added:
                        var lightShaderGroupEntry = new LightShaderGroupEntry<LightGroupKey>(lightGroupKey, lightShaderGroup);
                        if (!lightShaderGroups.Contains(lightShaderGroupEntry))
                        {
                            lightShaderGroups.Add(lightShaderGroupEntry);
                        }

                        processedLights.Clear();
                    }

                    // Start next group
                    currentGroupParameters = nextGroupParameters;
                }

                if (j < parameters.LightIndices.Count)
                {
                    // Do we need to process non shadowing lights or defer it to something else?
                    if (nextShadowTexture == null && hasNextRenderer)
                    {
                        // Break out so the remaining lights can be handled by the next renderer
                        break;
                    }

                    parameters.LightIndices.RemoveAt(j);
                    processedLights.Add(new LightDynamicEntry(nextLight, nextShadowTexture));
                }
                else
                {
                    j++;
                }
            }

            processedLights.Clear();
        }

        public override void UpdateShaderPermutationEntry(ForwardLightingRenderFeature.LightShaderPermutationEntry shaderEntry)
        {
            // Sort to make sure we generate the same permutations
            lightShaderGroups.Sort(LightShaderGroupComparer.Default);

            foreach (var lightShaderGroup in lightShaderGroups)
            {
                shaderEntry.DirectLightGroups.Add(lightShaderGroup.Value);
            }
        }

        private class LightShaderGroupComparer : Comparer<LightShaderGroupEntry<LightGroupKey>>
        {
            public static new readonly LightShaderGroupComparer Default = new LightShaderGroupComparer();

            public override int Compare(LightShaderGroupEntry<LightGroupKey> x, LightShaderGroupEntry<LightGroupKey> y)
            {
                int compareShadowRenderer = (x.Key.ShadowRenderer != null).CompareTo(y.Key.ShadowRenderer != null);
                if (compareShadowRenderer != 0)
                    return compareShadowRenderer;

                int compareTextureProjectionRenderer = (x.Key.TextureProjectionRenderer != null).CompareTo(y.Key.TextureProjectionRenderer != null);
                if (compareTextureProjectionRenderer != 0)
                    return compareTextureProjectionRenderer;

                return ((int)x.Key.ShadowType).CompareTo((int)y.Key.ShadowType);
            }
        }

        private struct LightGroupKey : IEquatable<LightGroupKey>
        {
            public readonly ILightShadowMapRenderer ShadowRenderer;
            public readonly ITextureProjectionRenderer TextureProjectionRenderer;
            public readonly LightShadowType ShadowType;

            public LightGroupKey(ILightShadowMapRenderer shadowRenderer,
                                 LightShadowType shadowType,
                                 ITextureProjectionRenderer textureProjectionRenderer)
            {
                ShadowRenderer = shadowRenderer;
                ShadowType = shadowType;
                TextureProjectionRenderer = textureProjectionRenderer;
            }

            public bool Equals(LightGroupKey other)
            {
                // Temporary variables for easier debugging:
                bool shadowRenderersAreEqual = Equals(ShadowRenderer, other.ShadowRenderer);
                bool shadowTypesAreEqual = ShadowType == other.ShadowType;
                bool textureProjectionRenderersAreEqual = TextureProjectionRenderer == other.TextureProjectionRenderer;
                return shadowRenderersAreEqual && shadowTypesAreEqual && textureProjectionRenderersAreEqual;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is LightGroupKey && Equals((LightGroupKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (ShadowRenderer != null ? ShadowRenderer.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (TextureProjectionRenderer != null ? TextureProjectionRenderer.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (int)ShadowType;
                    return hashCode;
                }
            }

            public override string ToString()
            {
                return $"Lights with shadow type [{ShadowType}] and projection texture renderer [{TextureProjectionRenderer}]";
            }
        }

        private struct TextureProjectionRendererKey : IEquatable<TextureProjectionRendererKey>
        {
            public readonly SpotLightTextureParameters SpotLightParameters;

            public TextureProjectionRendererKey(SpotLightTextureParameters spotParameters)
            {
                SpotLightParameters = spotParameters;
            }

            public bool Equals(TextureProjectionRendererKey other)
            {
                return SpotLightParameters.Equals(other.SpotLightParameters);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is TextureProjectionRendererKey && Equals((TextureProjectionRendererKey)obj);
            }

            public override int GetHashCode()
            {
                return SpotLightParameters.GetHashCode();
            }
            public override string ToString()
            {
                return $"Texture projection renderer: Texture=[{SpotLightParameters.ProjectionTexture}], flip mode=[{SpotLightParameters.FlipMode}], UVScale=[{SpotLightParameters.UVScale}], UVOffset=[{SpotLightParameters.UVOffset}]";
            }
        }

        private class SpotLightShaderGroup : LightShaderGroupDynamic
        {
            private ValueParameterKey<int> countKey;
            private ValueParameterKey<SpotLightData> lightsKey;
            private FastListStruct<SpotLightData> lightsData = new FastListStruct<SpotLightData>(8);
            private readonly object applyLock = new object();

            public ITextureProjectionShaderGroupData TextureProjectionShaderGroupData { get; }

            public SpotLightShaderGroup(RenderContext renderContext, ILightShadowMapShaderGroupData shadowGroupData, ITextureProjectionShaderGroupData textureProjectionShaderGroupData)
                : base(renderContext, shadowGroupData)
            {
                TextureProjectionShaderGroupData = textureProjectionShaderGroupData;
            }

            public override void UpdateLayout(string compositionName)
            {
                base.UpdateLayout(compositionName);
                TextureProjectionShaderGroupData?.UpdateLayout(compositionName);

                countKey = DirectLightGroupPerDrawKeys.LightCount.ComposeWith(compositionName);
                lightsKey = LightSpotGroupKeys.Lights.ComposeWith(compositionName);
            }

            protected override void UpdateLightCount()
            {
                base.UpdateLightCount();
                TextureProjectionShaderGroupData?.UpdateLightCount(LightLastCount, LightCurrentCount);

                var mixin = new ShaderMixinSource();

                // Old fixed path kept in case we need it again later
                //mixin.Mixins.Add(new ShaderClassSource("LightSpotGroup", LightCurrentCount));
                //mixin.Mixins.Add(new ShaderClassSource("DirectLightGroupFixed", LightCurrentCount));
                mixin.Mixins.Add(new ShaderClassSource("LightSpotGroup", LightCurrentCount));   // Add the base shader for the light group.
                ShadowGroup?.ApplyShader(mixin);    // Add the shader for shadow mapping.
                TextureProjectionShaderGroupData?.ApplyShader(mixin);   // Add the shader for texture projection.

                ShaderSource = mixin;
            }

            /// <inheritdoc/>
            public override int AddView(int viewIndex, RenderView renderView, int lightCount)
            {
                base.AddView(viewIndex, renderView, lightCount);

                // We allow more lights than LightCurrentCount (they will be culled)
                return lightCount;
            }

            public override void ApplyDrawParameters(RenderDrawContext context, int viewIndex, ParameterCollection parameters, ref BoundingBoxExt boundingBox)
            {
                // TODO THREADING: Make CurrentLights and lightData (thread-) local
                lock (applyLock)
                {
                    currentLights.Clear();
                    var lightRange = lightRanges[viewIndex];
                    for (int i = lightRange.Start; i < lightRange.End; ++i)
                        currentLights.Add(lights[i]);

                    base.ApplyDrawParameters(context, viewIndex, parameters, ref boundingBox);

                    // TODO: Octree structure to select best lights quicker
                    var boundingBox2 = (BoundingBox)boundingBox;
                    for (int i = 0; i < currentLights.Count; i++)
                    {
                        var light = currentLights[i].Light;

                        if (light.BoundingBox.Intersects(ref boundingBox2))
                        {
                            var spotLight = (LightSpot)light.Type;
                            lightsData.Add(new SpotLightData
                            {
                                PositionWS = light.Position,
                                DirectionWS = light.Direction,
                                AngleOffsetAndInvSquareRadius = new Vector3(spotLight.LightAngleScale, spotLight.LightAngleOffset, spotLight.InvSquareRange),
                                Color = light.Color,
                            });

                            // Did we reach max number of simultaneous lights?
                            // TODO: Still collect everything but sort by importance and remove the rest?
                            if (lightsData.Count >= LightCurrentCount)
                                break;
                        }
                    }

                    parameters.Set(countKey, lightsData.Count);
                    parameters.Set(lightsKey, lightsData.Count, ref lightsData.Items[0]);
                    lightsData.Clear();

                    TextureProjectionShaderGroupData?.ApplyDrawParameters(context, parameters, currentLights, ref boundingBox);
                }
            }
        }
    }
}
