// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Core.Mathematics;

namespace Xenko.Physics
{
    [DataContract]
    [Display("Empty")]
    public class EmptyHeightData : IInitialHeightData
    {
        [DataMember(10)]
        public HeightfieldTypes HeightType { get; set; } = HeightfieldTypes.Float;

        [DataMember(20)]
        public Int2 HeightStickSize { get; set; } = new Int2(65, 65);

        [Display(Browsable = false)]
        public float[] Floats => null;

        [Display(Browsable = false)]
        public short[] Shorts => null;

        [Display(Browsable = false)]
        public byte[] Bytes => null;

        public bool Match(object obj)
        {
            var other = obj as EmptyHeightData;

            if (other == null)
            {
                return false;
            }

            return other.HeightType == HeightType &&
                other.HeightStickSize == HeightStickSize;
        }
    }
}
