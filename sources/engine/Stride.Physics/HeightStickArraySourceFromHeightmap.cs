// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Physics
{
    [DataContract]
    [Display("Heightmap")]
    public class HeightStickArraySourceFromHeightmap : IHeightStickArraySource
    {
        /// <summary>
        /// The heightmap to initialize the height stick array.
        /// </summary>
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

        public bool IsValid() => Heightmap?.IsValid() ?? false;

        public void CopyTo<T>(UnmanagedArray<T> heightStickArray, int index) where T : struct
        {
            if (Heightmap == null) throw new InvalidOperationException($"{ nameof(Heightmap) } is a null");
            if (heightStickArray == null) throw new ArgumentNullException(nameof(heightStickArray));

            var heightStickArrayLength = heightStickArray.Length - index;
            if (heightStickArrayLength <= 0) throw new IndexOutOfRangeException(nameof(index));

            var typeOfT = typeof(T);
            T[] heights;

            if (typeOfT == typeof(float))
            {
                if (Heightmap.Floats == null) throw new InvalidOperationException($"{ nameof(Heightmap.Floats) } is a null.");
                heights = (T[])(object)Heightmap.Floats;
            }
            else if (typeOfT == typeof(short))
            {
                if (Heightmap.Shorts == null) throw new InvalidOperationException($"{ nameof(Heightmap.Shorts) } is a null.");
                heights = (T[])(object)Heightmap.Shorts;
            }
            else if (typeOfT == typeof(byte))
            {
                if (Heightmap.Bytes == null) throw new InvalidOperationException($"{ nameof(Heightmap.Bytes) } is a null.");
                heights = (T[])(object)Heightmap.Bytes;
            }
            else
            {
                throw new NotSupportedException($"{ typeof(UnmanagedArray<T>) } type is not supported.");
            }

            var heightsLength = heights.Length;
            if (heightStickArrayLength < heightsLength) throw new ArgumentException($"{ nameof(heightStickArray) }.{ nameof(heightStickArray.Length) } is not enough to copy.");

            heightStickArray.Write(heights, index * Utilities.SizeOf<T>(), 0, heightsLength);
        }

        public bool Match(object obj)
        {
            var other = obj as HeightStickArraySourceFromHeightmap;

            if (other == null)
            {
                return false;
            }

            return other.Heightmap == Heightmap;
        }
    }
}
