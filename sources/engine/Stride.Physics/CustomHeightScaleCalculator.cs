// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Physics
{
    [DataContract]
    [Display("Custom")]
    public class CustomHeightScaleCalculator : IHeightScaleCalculator
    {
        [DataMember(10)]
        public float Numerator { get; set; } = 1;

        [DataMember(20)]
        public float Denominator { get; set; } = 255;

        public float Calculate(IHeightStickParameters heightDescription) => MathUtil.IsZero(Denominator) ? 0 : (Numerator / Denominator);
    }
}
