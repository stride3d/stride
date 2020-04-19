// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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
