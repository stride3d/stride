// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering.Lights;

namespace Xenko.Rendering.Shadows
{
    /// <summary>
    /// Render shadow maps; should be set on <see cref="ForwardLightingRenderFeature.ShadowMapRenderer"/>.
    /// </summary>
    public interface IShadowMapRenderer
    {
        RenderSystem RenderSystem { get; set; }

        HashSet<RenderView> RenderViewsWithShadows { get; }

        List<ILightShadowMapRenderer> Renderers { get; }

        LightShadowMapTexture FindShadowMap(RenderView renderView, RenderLight light);

        void Collect(RenderContext context, Dictionary<RenderView, ForwardLightingRenderFeature.RenderViewLightData> renderViewLightDatas);

        void Draw(RenderDrawContext drawContext);

        void PrepareAtlasAsRenderTargets(CommandList commandList);

        void PrepareAtlasAsShaderResourceViews(CommandList commandList);

        void Flush(RenderDrawContext context);
    }
}
