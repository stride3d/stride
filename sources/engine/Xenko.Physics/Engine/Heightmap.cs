// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Engine.Design;

namespace Xenko.Physics
{
    [DataContract]
    [ContentSerializer(typeof(DataContentSerializer<Heightmap>))]
    [DataSerializerGlobal(typeof(CloneSerializer<Heightmap>), Profile = "Clone")]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<Heightmap>), Profile = "Content")]
    public class Heightmap
    {
        [DataMember(10)]
        [Display(Browsable = false)]
        public float[] Floats;

        [DataMember(20)]
        [Display(Browsable = false)]
        public short[] Shorts;

        [DataMember(30)]
        [Display(Browsable = false)]
        public byte[] Bytes;

        [DataMember(40)]
        [Display(Browsable = false)]
        public HeightfieldTypes HeightType;

        [DataMember(50)]
        public Int2 Size;

        [DataMember(60)]
        public Vector2 HeightRange;

        [DataMember(70)]
        public float HeightScale;

        public static Heightmap Create(Int2 size, Vector2 range, float scale, object data)
        {
            if (!HeightfieldColliderShapeDesc.IsValidHeightStickSize(size)) throw new ArgumentOutOfRangeException(nameof(size));
            if (range.Y < range.X) throw new ArgumentException($"{nameof(range)} is invalid. Max height should be greater than min height.");
            if (data == null) throw new ArgumentNullException(nameof(data));

            var arrayLength = size.X * size.Y;

            HeightfieldTypes heightType = default;
            float[] floats = null;
            short[] shorts = null;
            byte[] bytes = null;

            if (data is float[])
            {
                heightType = HeightfieldTypes.Float;
                floats = data as float[];
                if (floats.Length != arrayLength) throw new ArgumentException($"Not matched {nameof(size)} and size of {nameof(data)}.");
            }
            else if (data is short[])
            {
                heightType = HeightfieldTypes.Short;
                shorts = data as short[];
                if (shorts.Length != arrayLength) throw new ArgumentException($"Not matched {nameof(size)} and size of {nameof(data)}.");
            }
            else if (data is byte[])
            {
                heightType = HeightfieldTypes.Byte;
                bytes = data as byte[];
                if (bytes.Length != arrayLength) throw new ArgumentException($"Not matched {nameof(size)} and size of {nameof(data)}.");
            }
            else
            {
                throw new NotSupportedException($"Not supported height type '{data.GetType()}'.");
            }

            return new Heightmap
            {
                HeightType = heightType,
                Size = size,
                HeightRange = range,
                HeightScale = scale,
                Floats = floats,
                Shorts = shorts,
                Bytes = bytes,
            };
        }

        public static Heightmap Create<T>(Int2 size, Vector2 range, float scale, T[] data) where T : struct
        {
            return Create(size, range, scale, data as object);
        }
    }
}
