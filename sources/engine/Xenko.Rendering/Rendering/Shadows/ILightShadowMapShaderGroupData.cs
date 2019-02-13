// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Rendering.Lights;
using Xenko.Shaders;

namespace Xenko.Rendering.Shadows
{
    public interface ILightShadowMapShaderGroupData
    {
        void ApplyShader(ShaderMixinSource mixin);

        void UpdateLayout(string compositionName);

        void UpdateLightCount(int lightLastCount, int lightCurrentCount);

        void ApplyViewParameters(RenderDrawContext context, ParameterCollection parameters, FastListStruct<LightDynamicEntry> currentLights);

        void ApplyDrawParameters(RenderDrawContext context, ParameterCollection parameters, FastListStruct<LightDynamicEntry> currentLights, ref BoundingBoxExt boundingBox);
    }
}
