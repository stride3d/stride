// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Rendering.Images
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
