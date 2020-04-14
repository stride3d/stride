// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;

namespace Stride.Physics
{
    [DataContract]
    [Display("Byte")]
    public class ByteHeightStickArraySource : IHeightStickArraySource
    {
        [DataMemberIgnore]
        public HeightfieldTypes HeightType => HeightfieldTypes.Byte;

        [DataMember(10)]
        [Display("Size")]
        public Int2 HeightStickSize { get; set; } = new Int2(65, 65);

        [DataMember(20)]
        public Vector2 HeightRange { get; set; } = new Vector2(0, 10);

        [DataMemberIgnore]
        public float HeightScale => HeightScaleCalculator.Calculate(this);

        /// <summary>
        /// Select how to calculate HeightScale.
        /// </summary>
        [DataMember(30)]
        [NotNull]
        [Display("HeightScale", Expand = ExpandRule.Always)]
        public IHeightScaleCalculator HeightScaleCalculator { get; set; } = new HeightScaleCalculator();

        /// <summary>
        /// The value to fill the height stick array.
        /// </summary>
        [DataMember(40)]
        [DataMemberRange(0, 255, 1, 10, 0)]
        public byte InitialByte { get; set; } = 0;

        public bool IsValid() => HeightmapUtils.CheckHeightParameters(HeightStickSize, HeightType, HeightRange, HeightScale, false) &&
            MathUtil.IsInRange(InitialByte, byte.MinValue, byte.MaxValue);

        public void CopyTo<T>(UnmanagedArray<T> heightStickArray, int index) where T : struct
        {
            if (heightStickArray == null) throw new ArgumentNullException(nameof(heightStickArray));
            if (heightStickArray is UnmanagedArray<byte> unmanagedArray)
            {
                unmanagedArray.Fill(InitialByte, index, HeightStickSize.X * HeightStickSize.Y);
            }
            else
            {
                throw new NotSupportedException($"{ typeof(UnmanagedArray<T>) } type is not supported.");
            }
        }

        public bool Match(object obj)
        {
            var other = obj as ByteHeightStickArraySource;

            if (other == null)
            {
                return false;
            }

            return other.HeightStickSize == HeightStickSize &&
                other.HeightRange == HeightRange &&
                Math.Abs(other.HeightScale - HeightScale) < float.Epsilon &&
                other.InitialByte == InitialByte;
        }
    }
}
