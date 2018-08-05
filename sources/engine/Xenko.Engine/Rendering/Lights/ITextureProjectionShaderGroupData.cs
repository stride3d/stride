// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Shaders;

namespace Xenko.Rendering.Lights
{
    public interface ITextureProjectionShaderGroupData
    {
        void ApplyShader(ShaderMixinSource mixin);
        void UpdateLayout(string compositionName);
        void UpdateLightCount(int lightLastCount, int lightCurrentCount);
        void ApplyDrawParameters(RenderDrawContext context, ParameterCollection parameters, FastListStruct<LightDynamicEntry> currentLights, ref BoundingBoxExt boundingBox);
        void Collect(RenderContext context, RenderView sourceView, int lightIndex, LightComponent lightComponent); // TODO: Inspect "context" and "sourceView".
    }
}
