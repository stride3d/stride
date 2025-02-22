// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Rendering.Lights
{
    /// <summary>
    /// A directional light.
    /// </summary>
    [DataContract("LightDirectional")]
    [Display("Directional")]
    public class LightDirectional : ColorLightBase, IDirectLight
    {
        /// <summary>
        /// Gets or sets the shadow.
        /// </summary>
        /// <value>The shadow.</value>
        /// <userdoc>The settings of the light shadow</userdoc>
        [DataMember(200)]
        public LightDirectionalShadowMap Shadow { get; set; }

        public LightDirectional()
        {
            Shadow = new()
            {
                Size = LightShadowMapSize.Large,
            };
        }

        public bool HasBoundingBox
        {
            get
            {
                return false;
            }
        }

        public BoundingBox ComputeBounds(Vector3 position, Vector3 direction)
        {
            return BoundingBox.Empty;
        }

        public float ComputeScreenCoverage(RenderView renderView, Vector3 position, Vector3 direction)
        {
            // As the directional light is covering the whole screen, we take the max of current width, height
            return Math.Max(renderView.ViewSize.X, renderView.ViewSize.Y);
        }

        public override bool Update(RenderLight light)
        {
            return true;
        }

        LightShadowMap IDirectLight.Shadow => Shadow;
    }
}
