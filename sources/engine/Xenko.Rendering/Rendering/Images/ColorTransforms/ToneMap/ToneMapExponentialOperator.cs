// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// The tonemap exponential operator.
    /// </summary>
    [DataContract("ToneMapExponentialOperator")]
    [Display("Exponential")]
    public class ToneMapExponentialOperator : ToneMapCommonOperator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToneMapExponentialOperator"/> class.
        /// </summary>
        public ToneMapExponentialOperator()
            : base("ToneMapExponentialOperatorShader")
        {
        }
    }
}
