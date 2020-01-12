// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;

namespace Xenko.Physics
{
    [DataContract]
    [Display("Short")]
    public class EmptyShortHeightfieldHeightData : IInitialHeightfieldHeightData
    {
        [DataMemberIgnore]
        public HeightfieldTypes HeightType => HeightfieldTypes.Short;

        [DataMember(10)]
        public Int2 HeightStickSize { get; set; } = new Int2(65, 65);

        [DataMember(20)]
        public Vector2 HeightRange { get; set; } = new Vector2(-10, 10);

        [DataMemberIgnore]
        public float HeightScale => HeightScaleCalculator.Calculate(this);

        [DataMember(30)]
        [NotNull]
        [Display("HeightScale", Expand = ExpandRule.Always)]
        public IHeightfieldHeightScaleCalculator HeightScaleCalculator { get; set; } = new HeightfieldHeightScaleCalculator();

        [DataMemberIgnore]
        public float[] Floats => null;

        [DataMemberIgnore]
        public short[] Shorts => null;

        [DataMemberIgnore]
        public byte[] Bytes => null;

        public bool Match(object obj)
        {
            var other = obj as EmptyShortHeightfieldHeightData;

            if (other == null)
            {
                return false;
            }

            return other.HeightStickSize == HeightStickSize &&
                other.HeightRange == HeightRange &&
                Math.Abs(other.HeightScale - HeightScale) < float.Epsilon;
        }
    }
}
