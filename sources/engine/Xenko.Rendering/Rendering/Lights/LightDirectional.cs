// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;

namespace Xenko.Rendering.Lights
{
    /// <summary>
    /// A directional light.
    /// </summary>
    [DataContract("LightDirectional")]
    [Display("Directional")]
    public class LightDirectional : DirectLightBase
    {
        public LightDirectional()
        {
            Shadow = new LightDirectionalShadowMap
            {
                Size = LightShadowMapSize.Large,
            };
        }

        public override bool HasBoundingBox
        {
            get
            {
                return false;
            }
        }

        public override BoundingBox ComputeBounds(Vector3 position, Vector3 direction)
        {
            return BoundingBox.Empty;
        }

        public override float ComputeScreenCoverage(RenderView renderView, Vector3 position, Vector3 direction)
        {
            // As the directional light is covering the whole screen, we take the max of current width, height
            return Math.Max(renderView.ViewSize.X, renderView.ViewSize.Y);
        }

        public override bool Update(RenderLight light)
        {
            return true;
        }
    }
}
