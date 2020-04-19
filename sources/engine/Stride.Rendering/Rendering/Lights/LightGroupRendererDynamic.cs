// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Rendering.Shadows;

namespace Stride.Rendering.Lights
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
