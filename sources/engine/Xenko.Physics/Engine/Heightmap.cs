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

        public static Heightmap Create<T>(Int2 size, HeightfieldTypes heightType, Vector2 heightRange, float heightScale, T[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            HeightmapUtils.CheckHeightParameters(size, heightType, heightRange, heightScale, true);

            var length = size.X * size.Y;

            switch (data)
            {
                case float[] floats when floats.Length == length: break;
                case short[] shorts when shorts.Length == length: break;
                case byte[] bytes when bytes.Length == length: break;
                default: throw new ArgumentException($"{ typeof(T[]) } is not supported in { heightType } height type. Or { nameof(data) }.{ nameof(data).Length } doesn't match { nameof(size) }.");
            }

            var heightmap = new Heightmap
            {
                HeightType = heightType,
                Size = size,
                HeightRange = heightRange,
                HeightScale = heightScale,
                Floats = data as float[],
                Shorts = data as short[],
                Bytes = data as byte[],
            };

            return heightmap;
        }
    }
}
