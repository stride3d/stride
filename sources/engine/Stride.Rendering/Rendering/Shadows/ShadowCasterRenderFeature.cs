// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Core.Storage;
using Stride.Rendering.Lights;

namespace Stride.Rendering.Shadows
{
    public class ShadowCasterRenderFeature : SubRenderFeature
    {
        private LogicalGroupReference shadowCasterKey;
        private LogicalGroupReference shadowMapViewKey;
        private static readonly ParameterCollection InShadowMapView = ShadowMapViewFlag(1);
        private static readonly ParameterCollection NotInShadowMapView = ShadowMapViewFlag(0);

        private static ParameterCollection ShadowMapViewFlag(float value)
        {
            var parameters = new ParameterCollection();
            parameters.Set(ShadowMapCasterPassInfoKeys.ShadowMapViewFlag, new Vector4(value, 0, 0, 0));
            return parameters;
        }

        protected override void InitializeCore()
        {
            base.InitializeCore();
            shadowCasterKey = ((RootEffectRenderFeature)RootRenderFeature).CreateViewLogicalGroup("ShadowCaster");
            shadowMapViewKey = ((RootEffectRenderFeature)RootRenderFeature).CreateViewLogicalGroup("ShadowMapView");
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

                // Whether this is a shadow map view, for ShadowMapCasterPassInfo. Written for every
                // view that has the group, not only the shadow ones: a per-view constant buffer is
                // fresh memory each frame, and a group left unwritten reads whatever was there.
                foreach (var viewLayout in viewFeature.Layouts)
                {
                    var shadowMapView = viewLayout.GetLogicalGroup(shadowMapViewKey);
                    if (shadowMapView.Hash == ObjectId.Empty)
                        continue;
                    // A view that draws nothing through this layout has no buffer prepared for it.
                    var resourceGroup = viewLayout.Entries[view.Index].Resources;
                    if (resourceGroup == null || resourceGroup.ConstantBuffer.Data == System.IntPtr.Zero)
                        continue;
                    resourceGroup.UpdateLogicalGroup(ref shadowMapView, shadowMapRenderView != null ? InShadowMapView : NotInShadowMapView);
                }
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
