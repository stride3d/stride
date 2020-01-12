// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Core.Mathematics;

namespace Xenko.Physics
{
    [DataContract]
    [Display("Float")]
    public class EmptyFloatHeightfieldHeightData : IInitialHeightfieldHeightData
    {
        [DataMemberIgnore]
        public HeightfieldTypes HeightType => HeightfieldTypes.Float;

        [DataMember(10)]
        public Int2 HeightStickSize { get; set; } = new Int2(65, 65);

        [DataMember(20)]
        public Vector2 HeightRange { get; set; } = new Vector2(-10, 10);

        [DataMemberIgnore]
        public float HeightScale => 1f;

        [DataMemberIgnore]
        public float[] Floats => null;

        [DataMemberIgnore]
        public short[] Shorts => null;

        [DataMemberIgnore]
        public byte[] Bytes => null;

        public bool Match(object obj)
        {
            var other = obj as EmptyFloatHeightfieldHeightData;

            if (other == null)
            {
                return false;
            }

            return other.HeightStickSize == HeightStickSize &&
                other.HeightRange == HeightRange;
        }
    }
}
