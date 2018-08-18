// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Rendering.Shadows;

namespace Xenko.Rendering.Lights
{
    /// <summary>
    /// A light group renderer that can handle a varying number of lights, i.e. point, spots, directional.
    /// </summary>
    public abstract class LightGroupRendererDynamic : LightGroupRendererBase
    {
        public abstract LightShaderGroupDynamic CreateLightShaderGroup(RenderDrawContext context,
                                                                       ILightShadowMapShaderGroupData shadowShaderGroupData);
    }
}
