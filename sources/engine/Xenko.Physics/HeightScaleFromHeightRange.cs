// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;

namespace Xenko.Physics
{
    [DataContract]
    [Display("Adjust in range")]
    public class HeightScaleFromHeightRange : IHeightScale
    {
        public float CalculateHeightScale(HeightfieldColliderShapeDesc desc)
        {
            if (desc.InitialHeights == null)
            {
                return 0;
            }

            switch (desc.InitialHeights.HeightType)
            {
                case HeightfieldTypes.Float:
                    return 1f;

                case HeightfieldTypes.Short:
                    return Math.Max(Math.Abs(desc.HeightRange.X), Math.Abs(desc.HeightRange.Y)) / short.MaxValue;

                case HeightfieldTypes.Byte:
                    if (Math.Abs(desc.HeightRange.X) <= Math.Abs(desc.HeightRange.Y))
                    {
                        return desc.HeightRange.Y / byte.MaxValue;
                    }
                    else
                    {
                        return desc.HeightRange.X / byte.MaxValue;
                    }

                default:
                    return 0f;
            }
        }

        public bool Match(object obj)
        {
            var other = obj as HeightScaleFromHeightRange;

            if (other == null)
            {
                return false;
            }

            return true;
        }
    }
}
