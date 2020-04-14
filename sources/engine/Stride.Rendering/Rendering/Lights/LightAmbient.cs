// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Engine;

namespace Stride.Rendering.Lights
{
    /// <summary>
    /// An ambient light.
    /// </summary>
    [DataContract("LightAmbient")]
    [Display("Ambient")]
    public class LightAmbient : ColorLightBase
    {
        public override bool Update(RenderLight light)
        {
            return true;
        }
    }
}
