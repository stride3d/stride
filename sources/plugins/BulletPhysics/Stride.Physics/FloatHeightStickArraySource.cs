// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.CompilerServices;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Physics
{
    [DataContract]
    [Display("Float")]
    public class FloatHeightStickArraySource : IHeightStickArraySource
    {
        [DataMemberIgnore]
        public HeightfieldTypes HeightType => HeightfieldTypes.Float;

        [DataMember(10)]
        [Display("Size")]
        public Int2 HeightStickSize { get; set; } = new Int2(65, 65);

        [DataMember(20)]
        public Vector2 HeightRange { get; set; } = new Vector2(-10, 10);

        [DataMemberIgnore]
        public float HeightScale => 1f;

        /// <summary>
        /// The value to fill the height stick array.
        /// </summary>
        [DataMember(30)]
        public float InitialHeight { get; set; } = 0;

        public bool IsValid() => HeightmapUtils.CheckHeightParameters(HeightStickSize, HeightType, HeightRange, HeightScale, false) &&
            MathUtil.IsInRange(InitialHeight, HeightRange.X, HeightRange.Y);

        public void CopyTo<T>(Span<T> heightStickArray) where T : struct
        {
            if (typeof(T) == typeof(float))
            {
                heightStickArray.Fill(Unsafe.BitCast<float, T>(InitialHeight));
            }
            else
            {
                throw new NotSupportedException($"{ typeof(Span<T>) } type is not supported.");
            }
        }

        public bool Match(object obj)
        {
            var other = obj as FloatHeightStickArraySource;

            if (other == null)
            {
                return false;
            }

            return other.HeightStickSize == HeightStickSize &&
                other.HeightRange == HeightRange &&
                Math.Abs(other.InitialHeight - InitialHeight) < float.Epsilon;
        }
    }
}
