// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// The tonemap logarithmic operator.
    /// </summary>
    [DataContract("ToneMapLogarithmicOperator")]
    [Display("Logarithmic")]
    public class ToneMapLogarithmicOperator : ToneMapCommonOperator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToneMapLogarithmicOperator"/> class.
        /// </summary>
        public ToneMapLogarithmicOperator()
            : base("ToneMapLogarithmicOperatorShader")
        {
        }
    }
}
