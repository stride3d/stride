// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.Shadows;

namespace Stride.Rendering.Lights
{
    /// <summary>
    /// Base class to build light shader groups with <see cref="LightGroupRendererDynamic"/>.
    /// </summary>
    public abstract class LightShaderGroupDynamic : LightShaderGroup
    {
        protected GraphicsProfile graphicsProfile;

        /// <summary>
        /// List of all available lights.
        /// </summary>
        protected FastListStruct<LightDynamicEntry> lights = new FastListStruct<LightDynamicEntry>(8);

        protected LightRange[] lightRanges;

        /// <summary>
        /// The number of lights the shader needs for each view, <see cref="ComputeLightCount"/> of the view's lights.
        /// </summary>
        private int[] viewLightCounts;

        /// <summary>
        /// List of lights selected for this rendering.
        /// </summary>
        protected FastListStruct<LightDynamicEntry> currentLights = new FastListStruct<LightDynamicEntry>(8);

        public ILightShadowMapShaderGroupData ShadowGroup { get; }

        public int LightCurrentCount { get; private set; }

        /// <summary>
        /// The light count the shader was last laid out for; <see cref="UpdateLayout"/> builds the shader again when
        /// <see cref="LightCurrentCount"/> differs. It stays across a frame that lays the group out for no set.
        /// </summary>
        public int LightLastCount { get; private set; }

        protected LightShaderGroupDynamic(RenderContext renderContext, ILightShadowMapShaderGroupData shadowGroup)
        {
            graphicsProfile = renderContext.GraphicsDevice.EffectProfile;
            ShadowGroup = shadowGroup;
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            lights.Clear();
            LightCurrentCount = 0;
        }

        public virtual void SetViews(List<RenderView> views)
        {
            Array.Resize(ref lightRanges, views.Count);
            Array.Resize(ref viewLightCounts, views.Count);

            // Reset ranges
            for (var i = 0; i < views.Count; ++i)
            {
                lightRanges[i] = new LightRange(0, 0);
                viewLightCounts[i] = 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="viewIndex"></param>
        /// <param name="renderView"></param>
        /// <param name="lightCount"></param>
        /// <returns>The number of lights accepted in <see cref="currentLights"/>.</returns>
        public virtual int AddView(int viewIndex, RenderView renderView, int lightCount)
        {
            lightRanges[viewIndex] = new LightRange(lights.Count, lights.Count + lightCount);
            viewLightCounts[viewIndex] = ComputeLightCount(lightCount);
            LightCurrentCount = Math.Max(LightCurrentCount, viewLightCounts[viewIndex]);
            if (lightCount > 0)
                IncludeView(viewIndex);

            return Math.Min(LightCurrentCount, lightCount);
        }

        /// <inheritdoc/>
        public override int GetLightCount(ulong viewMask)
        {
            // As many lights as the largest of the selected views needs, not of every view of the frame; a group with
            // no light in the selected views, one every effect takes, keeps the count of the frame
            var selectedLightCount = 0;
            var frameLightCount = 0;
            for (var viewIndex = 0; viewIndex < viewLightCounts.Length; viewIndex++)
            {
                var lightCount = viewLightCounts[viewIndex];
                frameLightCount = Math.Max(frameLightCount, lightCount);
                if ((ViewBit(viewIndex) & viewMask) != 0)
                    selectedLightCount = Math.Max(selectedLightCount, lightCount);
            }

            return selectedLightCount > 0 ? selectedLightCount : frameLightCount;
        }

        /// <inheritdoc/>
        public override void SelectViews(ulong viewMask)
        {
            base.SelectViews(viewMask);
            LightCurrentCount = GetLightCount(viewMask);
        }

        /// <summary>
        /// Compute the number of light supported by this shader. Usually a different number of light will trigger a permutation and layout update.
        /// </summary>
        /// <param name="lightCount"></param>
        /// <returns></returns>
        protected virtual int ComputeLightCount(int lightCount)
        {
            // Shadows: return exact number
            // TODO: Only for PerView; PerDraw could be little bit more loose to avoid extra permutations
            if (ShadowGroup != null)
            {
                return lightCount;
            }

            // Use next power of two
            lightCount = MathUtil.NextPowerOfTwo(lightCount);

            // Make sure it is at least 8 to avoid unecessary permutations
            lightCount = Math.Max(lightCount, graphicsProfile >= GraphicsProfile.Level_10_0 ? 8 : 0);

            return lightCount;
        }

        /// <summary>
        /// Try to add light to this group (returns false if not possible).
        /// </summary>
        /// <param name="light"></param>
        /// <param name="shadowMapTexture"></param>
        /// <returns></returns>
        public bool AddLight(RenderLight light, LightShadowMapTexture shadowMapTexture)
        {
            lights.Add(new LightDynamicEntry(light, shadowMapTexture));
            return true;
        }

        /// <inheritdoc/>
        public override void UpdateLayout(string compositionName)
        {
            base.UpdateLayout(compositionName);
            ShadowGroup?.UpdateLayout(compositionName);

            if (LightLastCount != LightCurrentCount)
            {
                ShadowGroup?.UpdateLightCount(LightLastCount, LightCurrentCount);
                UpdateLightCount();
                LightLastCount = LightCurrentCount;
            }
        }

        protected virtual void UpdateLightCount()
        {
        }

        /// <inheritdoc/>
        public override void ApplyViewParameters(RenderDrawContext context, int viewIndex, ParameterCollection parameters)
        {
            base.ApplyViewParameters(context, viewIndex, parameters);
            ShadowGroup?.ApplyViewParameters(context, parameters, currentLights);
        }

        /// <inheritdoc/>
        public override void ApplyDrawParameters(RenderDrawContext context, int viewIndex, ParameterCollection parameters, ref BoundingBoxExt boundingBox)
        {
            base.ApplyDrawParameters(context, viewIndex, parameters, ref boundingBox);
            ShadowGroup?.ApplyDrawParameters(context, parameters, currentLights, ref boundingBox);
        }

        public struct LightRange
        {
            public readonly int Start;
            public readonly int End;

            public LightRange(int start, int end)
            {
                Start = start;
                End = end;
            }

            public override string ToString()
            {
                return $"LightRange {Start}..{End}";
            }
        }
    }
}
