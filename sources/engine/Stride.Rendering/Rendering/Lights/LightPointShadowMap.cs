// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;

namespace Stride.Rendering.Lights
{
    /// <summary>
    /// A standard shadow map.
    /// </summary>
    [DataContract("LightPointShadowMap")]
    [Display("Point ShadowMap")]
    public sealed class LightPointShadowMap : LightShadowMap
    {
        /// <summary>
        /// The type of shadow mapping technique to use for this point light
        /// </summary>
        [DefaultValue(LightPointShadowMapType.CubeMap)]
        public LightPointShadowMapType Type { get; set; } = LightPointShadowMapType.CubeMap;
    }
}
