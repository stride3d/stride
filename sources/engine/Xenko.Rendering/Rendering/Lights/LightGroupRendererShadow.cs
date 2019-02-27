// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name

using System;
using System.Collections.Generic;
using Xenko.Core.Collections;
using Xenko.Engine;
using Xenko.Rendering.Shadows;

namespace Xenko.Rendering.Lights
{
    internal class ShadowComparer : IComparer<int>
    {
        public Dictionary<RenderLight, LightShadowMapTexture> ShadowMapTexturesPerLight;
        public RenderLightCollection Lights;

        private LightShadowType GetShadowType(RenderLight light)
        {
            ShadowMapTexturesPerLight.TryGetValue(light, out LightShadowMapTexture shadow);
            return shadow?.ShadowType ?? 0;
        }

        public int Compare(int a, int b)
        {
            LightShadowType shadowTypeA = GetShadowType(Lights[a]);
            LightShadowType shadowTypeB = GetShadowType(Lights[b]);

            // Decreasing order so that non shadowed lights are last
            return ((int)shadowTypeB).CompareTo((int)shadowTypeA);
        }
    }

    public struct LightShaderGroupEntry<T> : IEquatable<LightShaderGroupEntry<T>>
    {
        public readonly T Key;
        public readonly LightShaderGroup Value;

        public LightShaderGroupEntry(T key, LightShaderGroup value)
        {
            Key = key;
            Value = value;
        }

        public bool Equals(LightShaderGroupEntry<T> other)
        {
            return Key.Equals(other.Key) && Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is LightShaderGroupEntry<T> && Equals((LightShaderGroupEntry<T>)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Key.GetHashCode() * 397) ^ Value.GetHashCode();
            }
        }
    }

    /// <summary>
    /// Base class for light renderers that use shadow maps.
    /// </summary>
    public abstract class LightGroupRendererShadow : LightGroupRendererDynamic
    {
        private readonly ShadowComparer shadowComparer = new ShadowComparer();
        private readonly Dictionary<LightGroupKey, LightShaderGroupDynamic> lightShaderGroupPool = new Dictionary<LightGroupKey, LightShaderGroupDynamic>();
        private readonly FastList<LightShaderGroupEntry<LightGroupKey>> lightShaderGroups = new FastList<LightShaderGroupEntry<LightGroupKey>>();
        private FastListStruct<LightDynamicEntry> processedLights = new FastListStruct<LightDynamicEntry>(8);

        public override Type[] LightTypes { get; }

        public override void Reset()
        {
            base.Reset();

            lightShaderGroups.Clear();

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
            ILightShadowMapShaderGroupData shadowGroupData = null;

            if (shadowRenderer != null)
            {
                shadowGroupData = shadowRenderer.CreateShaderGroupData(shadowType);
            }

            return shadowGroupData;
        }

        private LightShaderGroupDynamic FindOrCreateLightShaderGroup(LightGroupKey lightGroupKey, ProcessLightsParameters parameters)
        {
            LightShaderGroupDynamic lightShaderGroup;

            // Check to see if this combination of parameters has already been stored as a group:
            if (!lightShaderGroupPool.TryGetValue(lightGroupKey, out lightShaderGroup))
            {
                // If a group with the same key has not already been added, create it:
                ILightShadowMapShaderGroupData shadowGroupData = CreateShadowMapShaderGroupData(lightGroupKey.ShadowRenderer, lightGroupKey.ShadowType);

                lightShaderGroup = CreateLightShaderGroup(parameters.Context, shadowGroupData);
                lightShaderGroup.SetViews(parameters.Views);

                lightShaderGroupPool.Add(lightGroupKey, lightShaderGroup);
            }

            return lightShaderGroup;
        }

        public override void ProcessLights(ProcessLightsParameters parameters)
        {
            if (parameters.LightCollection.Count == 0)
                return;

            // Check if we have a fallback renderer next in the chain, in case we don't need shadows
            bool hasNextRenderer = parameters.RendererIndex < (parameters.Renderers.Length - 1);

            ILightShadowMapRenderer currentShadowRenderer = null;
            LightShadowType currentShadowType = 0;

            // Start by filtering/sorting what can be processed
            shadowComparer.ShadowMapTexturesPerLight = parameters.ShadowMapTexturesPerLight;
            shadowComparer.Lights = parameters.LightCollection;
            parameters.LightIndices.Sort(0, parameters.LightIndices.Count, shadowComparer);

            // Loop over the number of lights + 1 where the last iteration will always flush the last batch of lights
            for (int j = 0; j < parameters.LightIndices.Count + 1;)
            {
                // TODO: Eventually move this loop to a separate function that returns a structure.

                // These variables will contain the relevant parameters of the next usable light:
                LightShadowType nextShadowType = 0;
                ILightShadowMapRenderer nextShadowRenderer = null;
                LightShadowMapTexture nextShadowTexture = null;
                RenderLight nextLight = null;

                // Find the next light whose attributes aren't null:
                if (j < parameters.LightIndices.Count)
                {
                    nextLight = parameters.LightCollection[parameters.LightIndices[j]];

                    if (parameters.ShadowMapRenderer != null
                        && parameters.ShadowMapTexturesPerLight.TryGetValue(nextLight, out nextShadowTexture)
                        && nextShadowTexture.Atlas != null) // atlas could not be allocated? treat it as a non-shadowed texture
                    {
                        nextShadowType = nextShadowTexture.ShadowType;
                        nextShadowRenderer = nextShadowTexture.Renderer;
                    }
                }

                // Flush current group
                // If we detect that the previous light's attributes don't match the next one's, create a new group (or add to an existing one that has the same attributes):
                if (j == parameters.LightIndices.Count ||
                    currentShadowType != nextShadowType ||
                    currentShadowRenderer != nextShadowRenderer) // TODO: Refactor this into a little structure instead.
                {
                    if (processedLights.Count > 0)
                    {
                        var lightGroupKey = new LightGroupKey(currentShadowRenderer, currentShadowType);
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
                    currentShadowType = nextShadowType;
                    currentShadowRenderer = nextShadowRenderer;
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
                if (IsEnvironmentLight)
                    shaderEntry.EnvironmentLights.Add(lightShaderGroup.Value);
                else
                    shaderEntry.DirectLightGroups.Add(lightShaderGroup.Value);
            }
        }

        private class LightShaderGroupComparer : Comparer<LightShaderGroupEntry<LightGroupKey>>
        {
            public static new readonly LightShaderGroupComparer Default = new LightShaderGroupComparer();

            public override int Compare(LightShaderGroupEntry<LightGroupKey> x, LightShaderGroupEntry<LightGroupKey> y)
            {
                var compareRenderer = (x.Key.ShadowRenderer != null).CompareTo(y.Key.ShadowRenderer != null);
                if (compareRenderer != 0)
                    return compareRenderer;

                return ((int)x.Key.ShadowType).CompareTo((int)y.Key.ShadowType);
            }
        }

        private struct LightGroupKey : IEquatable<LightGroupKey>
        {
            public readonly ILightShadowMapRenderer ShadowRenderer;
            public readonly LightShadowType ShadowType;

            public LightGroupKey(ILightShadowMapRenderer shadowRenderer, LightShadowType shadowType)
            {
                ShadowRenderer = shadowRenderer;
                ShadowType = shadowType;
            }

            public bool Equals(LightGroupKey other)
            {
                // Temporary variables for easier debugging:
                bool shadowRenderersAreEqual = Equals(ShadowRenderer, other.ShadowRenderer);
                bool shadowTypesAreEqual = ShadowType == other.ShadowType;
                return shadowRenderersAreEqual && shadowTypesAreEqual;
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
                    hashCode = (hashCode * 397) ^ (int)ShadowType;
                    return hashCode;
                }
            }

            public override string ToString()
            {
                return $"Lights with shadow type [{ShadowType}]";
            }
        }
    }
}
