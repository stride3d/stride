// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine.Design;

namespace Stride.Physics
{
    [DataContract]
    [ContentSerializer(typeof(DataContentSerializer<Heightmap>))]
    [DataSerializerGlobal(typeof(CloneSerializer<Heightmap>), Profile = "Clone")]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<Heightmap>), Profile = "Content")]
    public class Heightmap
    {
        /// <summary>
        /// Float height array.
        /// </summary>
        [DataMember(10)]
        [Display(Browsable = false)]
        public float[] Floats;

        /// <summary>
        /// Short height array.
        /// </summary>
        [DataMember(20)]
        [Display(Browsable = false)]
        public short[] Shorts;

        /// <summary>
        /// Byte height array.
        /// </summary>
        [DataMember(30)]
        [Display(Browsable = false)]
        public byte[] Bytes;

        /// <summary>
        /// The type of the height.
        /// </summary>
        [DataMember(40)]
        [Display(Browsable = false)]
        public HeightfieldTypes HeightType;

        /// <summary>
        /// The size of the heightmap.
        /// </summary>
        /// <remarks>
        /// X is width and Y is length.
        /// They should be greater than or equal to 2.
        /// For example, this size should be 65 * 65 when you want 64 * 64 size in a scene.
        /// </remarks>
        [DataMember(50)]
        public Int2 Size;

        /// <summary>
        /// The range of the height.
        /// </summary>
        /// <remarks>
        /// X is min height and Y is max height.
        /// (height * HeightScale) should be in this range.
        /// Positive and negative heights can not be handle at the same time when the height type is Byte.
        /// </remarks>
        [DataMember(60)]
        public Vector2 HeightRange;

        /// <summary>
        /// Used to calculate the height when the height type is Short or Byte. HeightScale should be 1 when the height type is Float.
        /// </summary>
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
