// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Rendering.Lights;
using Stride.Shaders;

namespace Stride.Rendering.Shadows
{
    /// <summary>
    /// Provides basic functionality for shadow map shader groups with a single shader source and a filter based on the <see cref="LightShadowType"/>
    /// </summary>
    public abstract class LightShadowMapShaderGroupDataBase : ILightShadowMapShaderGroupData // TODO: Rename? Some classes have "...ShaderGroup" others have "...GroupShader" in their name. Wth?
    {
        public LightShadowMapShaderGroupDataBase(LightShadowType shadowType)
        {
            ShadowType = shadowType;
        }

        public LightShadowType ShadowType { get; private set; }

        public ShaderMixinSource ShadowShader { get; private set; }

        /// <summary>
        /// The first member name argument passed to the instantiated filter
        /// </summary>
        protected virtual string FilterMemberName { get; } = "PerDraw.Lighting";

        public virtual void ApplyShader(ShaderMixinSource mixin)
        {
            mixin.CloneFrom(ShadowShader);
        }

        public virtual void UpdateLightCount(int lightLastCount, int lightCurrentCount)
        {
            ShadowShader = new ShaderMixinSource();
            ShadowShader.Mixins.Add(CreateShaderSource(lightCurrentCount));

            // Add filter for current shadow type
            switch (ShadowType & LightShadowType.FilterMask)
            {
                case LightShadowType.PCF3x3:
                    ShadowShader.Mixins.Add(new ShaderClassSource("ShadowMapFilterPcf", FilterMemberName, 3));
                    break;
                case LightShadowType.PCF5x5:
                    ShadowShader.Mixins.Add(new ShaderClassSource("ShadowMapFilterPcf", FilterMemberName, 5));
                    break;
                case LightShadowType.PCF7x7:
                    ShadowShader.Mixins.Add(new ShaderClassSource("ShadowMapFilterPcf", FilterMemberName, 7));
                    break;
                default:
                    ShadowShader.Mixins.Add(new ShaderClassSource("ShadowMapFilterDefault", FilterMemberName));
                    break;
            }
        }

        public virtual void UpdateLayout(string compositionName)
        {
        }

        public virtual void ApplyViewParameters(RenderDrawContext context, ParameterCollection parameters, FastListStruct<LightDynamicEntry> currentLights)
        {
        }

        public virtual void ApplyDrawParameters(RenderDrawContext context, ParameterCollection parameters, FastListStruct<LightDynamicEntry> currentLights, ref BoundingBoxExt boundingBox)
        {
        }

        /// <summary>
        /// Creates the shader source that performs shadowing
        /// </summary>
        /// <returns></returns>
        public abstract ShaderClassSource CreateShaderSource(int lightCurrentCount);
    }
}
