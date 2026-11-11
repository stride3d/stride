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
        /// The views whose lights this group holds this frame, one bit per view index of the lighting feature
        /// (<see cref="ViewBit"/>), as its renderer told them with <see cref="IncludeView"/>. An effect compiled for a
        /// set of views takes the group when the set shares a view with it. A group whose renderer tells no view is
        /// taken by every effect, and so is one that overrides <see cref="IsUsedIn"/> to say so.
        /// </summary>
        public ulong ViewMask { get; private set; }

        /// <summary>
        /// The bit of the view at <paramref name="viewIndex"/> in a view mask; the views past the 64th share the last bit.
        /// </summary>
        public static ulong ViewBit(int viewIndex)
        {
            return 1UL << Math.Min(viewIndex, 63);
        }

        /// <summary>
        /// Marks the group as holding lights of the view at <paramref name="viewIndex"/>.
        /// </summary>
        public void IncludeView(int viewIndex)
        {
            ViewMask |= ViewBit(viewIndex);
        }

        /// <summary>
        /// Whether an effect compiled for the views of <paramref name="viewMask"/> takes this group.
        /// </summary>
        public virtual bool IsUsedIn(ulong viewMask)
        {
            return ViewMask == 0 || (ViewMask & viewMask) != 0;
        }

        /// <summary>
        /// The number of lights the group's shader holds when laid out for the views of <paramref name="viewMask"/>;
        /// zero for a group whose shader is not sized by its lights. Two sets of views that take the same groups with
        /// the same light counts are compiled for the same shaders, and share one lighting set.
        /// </summary>
        public virtual int GetLightCount(ulong viewMask)
        {
            return 0;
        }

        /// <summary>
        /// Selects the views the next <see cref="UpdateLayout"/> lays the group out for, so a group whose shader is sized
        /// by its lights holds <see cref="GetLightCount"/> of them.
        /// </summary>
        public virtual void SelectViews(ulong viewMask)
        {
        }

        /// <summary>
        /// Called when layout is updated, so that parameter keys can be recomputed. A group laid out in several sets of
        /// views at different indices is laid out again before each use.
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
