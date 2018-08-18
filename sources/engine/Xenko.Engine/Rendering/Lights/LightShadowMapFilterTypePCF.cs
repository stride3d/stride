// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

using Xenko.Core;

namespace Xenko.Rendering.Lights
{
    public enum LightShadowMapFilterTypePcfSize
    {
        Filter3x3,

        Filter5x5,

        Filter7x7,
    }

    /// <summary>
    /// No shadowmap filter.
    /// </summary>
    [DataContract("LightShadowMapFilterTypePcf")]
    [Display("PCF")]
    public class LightShadowMapFilterTypePcf : ILightShadowMapFilterType
    {
        public LightShadowMapFilterTypePcf()
        {
            FilterSize = LightShadowMapFilterTypePcfSize.Filter3x3;
        }

        /// <summary>
        /// Gets or sets the size of the filter.
        /// </summary>
        /// <value>The size of the filter.</value>
        /// <userdoc>The size of the filter (size of the kernel).</userdoc>
        [DataMember(10)]
        [DefaultValue(LightShadowMapFilterTypePcfSize.Filter3x3)]
        public LightShadowMapFilterTypePcfSize FilterSize { get; set; }

        public bool RequiresCustomBuffer()
        {
            return false;
        }
    }
}
