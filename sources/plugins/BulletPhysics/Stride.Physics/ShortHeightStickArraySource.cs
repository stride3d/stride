// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.CompilerServices;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;

namespace Stride.Physics
{
    [DataContract]
    [Display("Short")]
    public class ShortHeightStickArraySource : IHeightStickArraySource
    {
        [DataMemberIgnore]
        public HeightfieldTypes HeightType => HeightfieldTypes.Short;

        [DataMember(10)]
        [Display("Size")]
        public Int2 HeightStickSize { get; set; } = new Int2(65, 65);

        [DataMember(20)]
        public Vector2 HeightRange { get; set; } = new Vector2(-10, 10);

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
        [DataMemberRange(-32767, 32767, 1, 10, 0)]
        public short InitialShort { get; set; } = 0;

        public bool IsValid() => HeightmapUtils.CheckHeightParameters(HeightStickSize, HeightType, HeightRange, HeightScale, false) &&
            MathUtil.IsInRange(InitialShort, -short.MaxValue, short.MaxValue);

        public void CopyTo<T>(Span<T> heightStickArray) where T : struct
        {
            if (typeof(T) == typeof(short))
            {
                heightStickArray.Fill(Unsafe.BitCast<short, T>(InitialShort));
            }
            else
            {
                throw new NotSupportedException($"{ typeof(Span<T>) } type is not supported.");
            }
        }

        public bool Match(object obj)
        {
            var other = obj as ShortHeightStickArraySource;

            if (other == null)
            {
                return false;
            }

            return other.HeightStickSize == HeightStickSize &&
                other.HeightRange == HeightRange &&
                Math.Abs(other.HeightScale - HeightScale) < float.Epsilon &&
                other.InitialShort == InitialShort;
        }
    }
}
