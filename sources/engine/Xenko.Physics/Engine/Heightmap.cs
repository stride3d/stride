// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xenko.Core;
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
        public HeightfieldTypes HeightfieldType;

        [DataMember(50)]
        public int Width;

        [DataMember(60)]
        public int Length;

        public static Heightmap Create<T>(int width, int length, T[] data) where T : struct
        {
            if (width <= 1 || length <= 1 || data == null)
            {
                return null;
            }

            var type = data.GetType();

            if (type == typeof(float[]))
            {
                return new Heightmap
                {
                    HeightfieldType = HeightfieldTypes.Float,
                    Width = width,
                    Length = length,
                    Floats = data as float[],
                };
            }
            else if (type == typeof(short[]))
            {
                return new Heightmap
                {
                    HeightfieldType = HeightfieldTypes.Short,
                    Width = width,
                    Length = length,
                    Shorts = data as short[],
                };
            }
            else if (type == typeof(byte[]))
            {
                return new Heightmap
                {
                    HeightfieldType = HeightfieldTypes.Byte,
                    Width = width,
                    Length = length,
                    Bytes = data as byte[],
                };
            }

            return null;
        }
    }
}
