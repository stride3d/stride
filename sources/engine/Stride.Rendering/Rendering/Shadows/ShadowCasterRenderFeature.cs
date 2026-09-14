// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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
            parameters.Set(ShadowMapCasterPassInfoKeys.ShadowMapViewFlag, value);
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
                
                var shadowMapRenderView = view as ShadowMapRenderView;
                foreach (var viewLayout in viewFeature.Layouts)
                {
                    // A layout the view draws nothing through has no resources prepared for it.
                    var resourceGroup = viewLayout.Entries[view.Index].Resources;
                    if (resourceGroup == null || resourceGroup.ConstantBuffer.Data == System.IntPtr.Zero)
                        continue;

                    // Written for every view, so a material reading the flag outside a shadow pass gets 0.
                    var shadowMapView = viewLayout.GetLogicalGroup(shadowMapViewKey);
                    if (shadowMapView.Hash != ObjectId.Empty)
                        resourceGroup.UpdateLogicalGroup(ref shadowMapView, shadowMapRenderView != null ? InShadowMapView : NotInShadowMapView);

                    if (shadowMapRenderView == null)
                        continue;
                    var shadowCaster = viewLayout.GetLogicalGroup(shadowCasterKey);
                    if (shadowCaster.Hash == ObjectId.Empty)
                        continue;

                    var shadowMapTexture = shadowMapRenderView.ShadowMapTexture;
                    shadowMapTexture.Renderer.ApplyViewParameters(context, shadowMapRenderView.ViewParameters, shadowMapTexture);
                    resourceGroup.UpdateLogicalGroup(ref shadowCaster, shadowMapRenderView.ViewParameters);
                }
            }
        }
    }
}
