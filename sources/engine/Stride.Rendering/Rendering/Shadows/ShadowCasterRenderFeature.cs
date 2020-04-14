// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Storage;
using Stride.Rendering.Lights;

namespace Stride.Rendering.Shadows
{
    public class ShadowCasterRenderFeature : SubRenderFeature
    {
        private LogicalGroupReference shadowCasterKey;

        protected override void InitializeCore()
        {
            base.InitializeCore();
            shadowCasterKey = ((RootEffectRenderFeature)RootRenderFeature).CreateViewLogicalGroup("ShadowCaster");
        }

        public override void Prepare(RenderDrawContext context)
        {
            base.Prepare(context);
            
            for (int index = 0; index < RenderSystem.Views.Count; index++)
            {
                var view = RenderSystem.Views[index];
                var viewFeature = view.Features[RootRenderFeature.Index];
                
                // Process only shadow views
                var shadowMapRenderView = view as ShadowMapRenderView;
                if (shadowMapRenderView != null)
                {
                    var renderer = shadowMapRenderView.ShadowMapTexture.Renderer;
                    foreach (var viewLayout in viewFeature.Layouts)
                    {
                        var shadowCaster = viewLayout.GetLogicalGroup(shadowCasterKey);
                        if (shadowCaster.Hash == ObjectId.Empty)
                            continue;

                        var shadowMapTexture = shadowMapRenderView.ShadowMapTexture;
                        renderer.ApplyViewParameters(context, shadowMapRenderView.ViewParameters, shadowMapTexture);
                        
                        var resourceGroup = viewLayout.Entries[view.Index].Resources;
                        resourceGroup.UpdateLogicalGroup(ref shadowCaster, shadowMapRenderView.ViewParameters);
                    }
                }
            }
        }
    }
}
