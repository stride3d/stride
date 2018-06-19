// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Xenko.Core;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// Base operator shared by Reinhard, Drago, Exponential and Logarithmic.
    /// </summary>
    [DataContract]
    public abstract class ToneMapCommonOperator : ToneMapOperator
    {
        protected ToneMapCommonOperator(string effectName)
            : base(effectName)
        {
        }

        /// <summary>
        /// Gets or sets the luminance saturation.
        /// </summary>
        /// <value>The luminance saturation.</value>
        [DataMember(5)]
        [DefaultValue(1f)]
        public float LuminanceSaturation
        {
            get
            {
                return Parameters.Get(ToneMapCommonOperatorShaderKeys.LuminanceSaturation);
            }
            set
            {
                Parameters.Set(ToneMapCommonOperatorShaderKeys.LuminanceSaturation, value);
            }
        }

        /// <summary>
        /// Gets or sets the white level.
        /// </summary>
        /// <value>The white level.</value>
        [DataMember(8)]
        [DefaultValue(5f)]
        public float WhiteLevel
        {
            get
            {
                return Parameters.Get(ToneMapCommonOperatorShaderKeys.WhiteLevel);
            }
            set
            {
                Parameters.Set(ToneMapCommonOperatorShaderKeys.WhiteLevel, value);
            }
        }
    }
}
