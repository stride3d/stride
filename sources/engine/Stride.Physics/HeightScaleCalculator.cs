// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;

namespace Stride.Physics
{
    [DataContract]
    [Display("Auto")]
    public class HeightScaleCalculator : IHeightScaleCalculator
    {
        public float Calculate(IHeightStickParameters heightDescription)
        {
            var heightRange = heightDescription.HeightRange;

            switch (heightDescription.HeightType)
            {
                case HeightfieldTypes.Float:
                    return 1f;

                case HeightfieldTypes.Short:
                    return Math.Max(Math.Abs(heightRange.X), Math.Abs(heightRange.Y)) / short.MaxValue;

                case HeightfieldTypes.Byte:
                    if (Math.Abs(heightRange.X) <= Math.Abs(heightRange.Y))
                    {
                        return heightRange.Y / byte.MaxValue;
                    }
                    else
                    {
                        return heightRange.X / byte.MaxValue;
                    }

                default:
                    throw new NotSupportedException($"Unknown height type.");
            }
        }
    }
}
