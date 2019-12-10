// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Core.Mathematics;

namespace Xenko.Physics
{
    [DataContract]
    [Display("Heightmap")]
    public class HeightDataFromHeightmap : IInitialHeightData
    {
        [DataMember(10)]
        public Heightmap Heightmap { get; set; }

        [Display(Browsable = false)]
        public HeightfieldTypes HeightType => Heightmap?.HeightType ?? default;

        [Display(Browsable = false)]
        public Int2 HeightStickSize => Heightmap?.Size ?? default;

        [Display(Browsable = false)]
        public float[] Floats => Heightmap?.Floats;

        [Display(Browsable = false)]
        public short[] Shorts => Heightmap?.Shorts;

        [Display(Browsable = false)]
        public byte[] Bytes => Heightmap?.Bytes;

        public bool Match(object obj)
        {
            var other = obj as HeightDataFromHeightmap;

            if (other == null)
            {
                return false;
            }

            return other.Heightmap == Heightmap;
        }
    }
}
