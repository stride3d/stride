// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;

namespace Stride.Rendering.Shadows
{
    /// <summary>
    /// A view used to render a shadow map to a <see cref="LightShadowMapTexture"/>
    /// </summary>
    public class ShadowMapRenderView : RenderView
    {
        public ShadowMapRenderView()
        {
            VisiblityIgnoreDepthPlanes = true;
        }

        /// <summary>
        /// The view for which this shadow map is rendered
        /// </summary>
        public RenderView RenderView;

        /// <summary>
        /// The shadow map to render
        /// </summary>
        public LightShadowMapTexture ShadowMapTexture;

        /// <summary>
        /// The rectangle to render to in the shadow map
        /// </summary>
        public Rectangle Rectangle;

        public ProfilingKey ProfilingKey { get; } = new ProfilingKey($"ShadowMapRenderView");
        
        internal ParameterCollection ViewParameters = new ParameterCollection();
    }
}
