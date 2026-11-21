// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering.Shadows;
using Stride.Shaders;

namespace Stride.Rendering.Lights
{
    /// <summary>
    /// A group of lights of the same type (single loop in the shader).
    /// </summary>
    public abstract class LightShaderGroup
    {
        protected LightShaderGroup()
        {
        }

        protected LightShaderGroup(ShaderSource mixin)
        {
            ShaderSource = mixin;
        }

        public ShaderSource ShaderSource { get; protected set; }

        public bool HasEffectPermutations { get; protected set; } = false;

        /// <summary>
        /// Mask of the views this group has lights in (see <see cref="ViewBit"/>). A group which doesn't include any view is used in all of them.
        /// </summary>
        public ulong ViewMask { get; private set; }

        /// <summary>
        /// Gets the bit of a view in a view mask (views past the 64th share the last bit).
        /// </summary>
        public static ulong ViewBit(int viewIndex)
        {
            return 1UL << Math.Min(viewIndex, 63);
        }

        /// <summary>
        /// Marks this group as having lights in the specified view.
        /// </summary>
        public void IncludeView(int viewIndex)
        {
            ViewMask |= ViewBit(viewIndex);
        }

        /// <summary>
        /// Checks if effects compiled for the specified views use this group.
        /// </summary>
        public virtual bool IsUsedIn(ulong viewMask)
        {
            return ViewMask == 0 || (ViewMask & viewMask) != 0;
        }

        /// <summary>
        /// Gets the number of lights supported by the shader when laid out for the specified views (0 if it doesn't depend on lights).
        /// </summary>
        public virtual int GetLightCount(ulong viewMask)
        {
            return 0;
        }

        /// <summary>
        /// Selects the views the next <see cref="UpdateLayout"/> is done for.
        /// </summary>
        public virtual void SelectViews(ulong viewMask)
        {
        }

        /// <summary>
        /// Called when layout is updated, so that parameter keys can be recomputed.
        /// </summary>
        /// <param name="compositionName"></param>
        public virtual void UpdateLayout(string compositionName)
        {
        }

        /// <summary>
        /// Resets states.
        /// </summary>
        public virtual void Reset()
        {
            ViewMask = 0;
        }

        /// <summary>
        /// Applies effect permutations.
        /// </summary>
        /// <param name="renderEffect"></param>
        public virtual void ApplyEffectPermutations(RenderEffect renderEffect)
        {
        }

        /// <summary>
        /// Applies PerView lighting parameters.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="viewIndex"></param>
        /// <param name="parameters"></param>
        public virtual void ApplyViewParameters(RenderDrawContext context, int viewIndex, ParameterCollection parameters)
        {
        }

        /// <summary>
        /// Applies PerDraw lighting parameters.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="viewIndex"></param>
        /// <param name="parameters"></param>
        /// <param name="boundingBox"></param>
        public virtual void ApplyDrawParameters(RenderDrawContext context, int viewIndex, ParameterCollection parameters, ref BoundingBoxExt boundingBox)
        {
        }

        /// <summary>
        /// Prepares PerView lighting parameters.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="viewIndex"></param>
        public virtual void UpdateViewResources(RenderDrawContext context, int viewIndex)
        {
        }
    }

    public struct LightDynamicEntry
    {
        public readonly RenderLight Light;
        public readonly LightShadowMapTexture ShadowMapTexture;

        public LightDynamicEntry(RenderLight light, LightShadowMapTexture shadowMapTexture)
        {
            Light = light;
            ShadowMapTexture = shadowMapTexture;
        }
    }
}
