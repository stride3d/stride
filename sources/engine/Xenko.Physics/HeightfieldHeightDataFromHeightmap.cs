// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Core.Mathematics;

namespace Xenko.Physics
{
    [DataContract]
    [Display("Heightmap")]
    public class HeightfieldHeightDataFromHeightmap : IInitialHeightfieldHeightData
    {
        [DataMember(10)]
        public Heightmap Heightmap { get; set; }

        [DataMemberIgnore]
        public HeightfieldTypes HeightType => Heightmap?.HeightType ?? default;

        [DataMemberIgnore]
        public Int2 HeightStickSize => Heightmap?.Size ?? default;

        [DataMemberIgnore]
        public Vector2 HeightRange => Heightmap?.HeightRange ?? default;

        [DataMemberIgnore]
        public float HeightScale => Heightmap?.HeightScale ?? default;

        [DataMemberIgnore]
        public float[] Floats => Heightmap?.Floats;

        [DataMemberIgnore]
        public short[] Shorts => Heightmap?.Shorts;

        [DataMemberIgnore]
        public byte[] Bytes => Heightmap?.Bytes;

        public bool Match(object obj)
        {
            var other = obj as HeightfieldHeightDataFromHeightmap;

            if (other == null)
            {
                return false;
            }

            return other.Heightmap == Heightmap;
        }
    }
}
