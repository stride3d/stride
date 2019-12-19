// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;

namespace Xenko.Physics
{
    [DataContract]
    [Display("Value")]
    public class HeightScale : IHeightScale
    {
        [DataMember(10)]
        public float Scale { get; set; } = 1f;

        public float CalculateHeightScale(HeightfieldColliderShapeDesc desc)
        {
            return Scale;
        }

        public bool Match(object obj)
        {
            var other = obj as HeightScale;

            if (other == null)
            {
                return false;
            }

            return Math.Abs(other.Scale - Scale) < float.Epsilon;
        }
    }
}
