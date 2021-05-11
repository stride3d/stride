// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.ComponentModel;
using Stride.Core;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// The Tone mapping ACES operator.
    /// </summary>
    [DataContract("ToneMapACESOperator")]
    [Display("ACES")]
    public class ToneMapACESOperator : ToneMapCommonOperator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToneMapReinhardOperator"/> class.
        /// </summary>
        public ToneMapACESOperator()
            : base("ToneMapACESOperatorShader")
        {
        }
    }
}
