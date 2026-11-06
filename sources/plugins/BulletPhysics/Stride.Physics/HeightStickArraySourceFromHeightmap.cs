// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
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

        public void CopyTo<T>(Span<T> heightStickArray) where T : struct
        {
            if (Heightmap == null) throw new InvalidOperationException($"{ nameof(Heightmap) } is a null");

            T[] heights;

            if (typeof(T) == typeof(float))
            {
                if (Heightmap.Floats == null) throw new InvalidOperationException($"{ nameof(Heightmap.Floats) } is a null.");
                heights = (T[])(object)Heightmap.Floats;
            }
            else if (typeof(T) == typeof(short))
            {
                if (Heightmap.Shorts == null) throw new InvalidOperationException($"{ nameof(Heightmap.Shorts) } is a null.");
                heights = (T[])(object)Heightmap.Shorts;
            }
            else if (typeof(T) == typeof(byte))
            {
                if (Heightmap.Bytes == null) throw new InvalidOperationException($"{ nameof(Heightmap.Bytes) } is a null.");
                heights = (T[])(object)Heightmap.Bytes;
            }
            else
            {
                throw new NotSupportedException($"{ typeof(Span<T>) } type is not supported.");
            }

            heights.AsSpan().CopyTo(heightStickArray);
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
