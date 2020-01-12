// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;

namespace Xenko.Physics
{
    [DataContract]
    [Display("Value")]
    public class HeightfieldHeightScaleCalculatorWithValue : IHeightfieldHeightScaleCalculator
    {
        [DataMember(10)]
        public float Value { get; set; } = 1f;

        public float Calculate(IHeightfieldHeightDescription heightDescription) => Value;
    }
}
