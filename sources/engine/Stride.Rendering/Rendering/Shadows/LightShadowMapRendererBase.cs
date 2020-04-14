// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Collections;
using Stride.Engine;
using Stride.Rendering.Lights;

namespace Stride.Rendering.Shadows
{
    /// <summary>
    /// Base class for shadow map renderers
    /// </summary>
    [DataContract(Inherited = true, DefaultMemberMode = DataMemberMode.Never)]
    public abstract class LightShadowMapRendererBase : ILightShadowMapRenderer
    {
        protected PoolListStruct<ShadowMapRenderView> shadowRenderViews;
        protected PoolListStruct<LightShadowMapTexture> shadowMaps;

        protected LightShadowMapRendererBase()
        {
            shadowRenderViews = new PoolListStruct<ShadowMapRenderView>(16, () => new ShadowMapRenderView());
            shadowMaps = new PoolListStruct<LightShadowMapTexture>(16, () => new LightShadowMapTexture());
        }

        /// <summary>
        /// The shadow map render stage this light shadow map renderer uses
        /// </summary>
        [DataMember]
        public RenderStage ShadowCasterRenderStage { get; set; }

        public virtual void Reset(RenderContext context)
        {
            foreach (var view in shadowRenderViews)
                context.RenderSystem.Views.Remove(view);

            shadowRenderViews.Clear();
            shadowMaps.Clear();
        }

        public virtual LightShadowType GetShadowType(LightShadowMap shadowMap)
        {
            var shadowType = (LightShadowType)0;
            switch (shadowMap.GetCascadeCount())
            {
                case 1:
                    shadowType |= LightShadowType.Cascade1;
                    break;
                case 2:
                    shadowType |= LightShadowType.Cascade2;
                    break;
                case 4:
                    shadowType |= LightShadowType.Cascade4;
                    break;
            }

            var pcfFilter = shadowMap.Filter as LightShadowMapFilterTypePcf;
            if (pcfFilter != null)
            {
                switch (pcfFilter.FilterSize)
                {
                    case LightShadowMapFilterTypePcfSize.Filter3x3:
                        shadowType |= LightShadowType.PCF3x3;
                        break;
                    case LightShadowMapFilterTypePcfSize.Filter5x5:
                        shadowType |= LightShadowType.PCF5x5;
                        break;
                    case LightShadowMapFilterTypePcfSize.Filter7x7:
                        shadowType |= LightShadowType.PCF7x7;
                        break;
                }
            }

            if (shadowMap.Debug)
            {
                shadowType |= LightShadowType.Debug;
            }
            return shadowType;
        }

        public abstract ILightShadowMapShaderGroupData CreateShaderGroupData(LightShadowType shadowType);

        public abstract bool CanRenderLight(IDirectLight light);

        public abstract void Collect(RenderContext context, RenderView sourceView, LightShadowMapTexture lightShadowMap);

        public virtual void ApplyViewParameters(RenderDrawContext context, ParameterCollection parameters, LightShadowMapTexture shadowMapTexture)
        {
        }

        public virtual LightShadowMapTexture CreateShadowMapTexture(RenderView renderView, RenderLight renderLight, IDirectLight light, int shadowMapSize)
        {
            var shadowMap = shadowMaps.Add();
            shadowMap.Initialize(renderView, renderLight, light, light.Shadow, shadowMapSize, this);
            return shadowMap;
        }

        /// <summary>
        /// Creates a default view with the shadow caster stage added to it
        /// </summary>
        public virtual ShadowMapRenderView CreateRenderView()
        {
            var shadowRenderView = shadowRenderViews.Add();
            shadowRenderView.RenderStages.Add(ShadowCasterRenderStage);
            return shadowRenderView;
        }
    }
}
