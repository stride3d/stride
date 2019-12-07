// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
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
        public Int2 Size { get; set; }

        public static Heightmap Create<T>(Int2 size, T[] data) where T : struct
        {
            if (size.X <= 1 || size.Y <= 1 || data == null)
            {
                return null;
            }

            var type = data.GetType();

            if (type == typeof(float[]))
            {
                return new Heightmap
                {
                    HeightType = HeightfieldTypes.Float,
                    Size = size,
                    Floats = data as float[],
                };
            }
            else if (type == typeof(short[]))
            {
                return new Heightmap
                {
                    HeightType = HeightfieldTypes.Short,
                    Size = size,
                    Shorts = data as short[],
                };
            }
            else if (type == typeof(byte[]))
            {
                return new Heightmap
                {
                    HeightType = HeightfieldTypes.Byte,
                    Size = size,
                    Bytes = data as byte[],
                };
            }

            return null;
        }
    }
}
