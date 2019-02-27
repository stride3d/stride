// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// The tonemap Reinhard operator.
    /// </summary>
    [DataContract("ToneMapReinhardOperator")]
    [Display("Reinhard")]
    public class ToneMapReinhardOperator : ToneMapCommonOperator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToneMapReinhardOperator"/> class.
        /// </summary>
        public ToneMapReinhardOperator()
            : base("ToneMapReinhardOperatorShader")
        {
        }
    }
}
