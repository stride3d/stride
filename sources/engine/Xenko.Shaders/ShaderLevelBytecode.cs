// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Serializers;

namespace Xenko.Shaders
{
    /// <summary>
    /// Structure containing the shaders for both OpenGL ES 2 and OpenGL ES 3.
    /// </summary>
    [DataSerializer(typeof(ShaderLevelBytecode.Serializer))]
    public struct ShaderLevelBytecode
    {
        public string DataES2;
        public string DataES3;

        internal class Serializer : DataSerializer<ShaderLevelBytecode>
        {
            public override void Serialize(ref ShaderLevelBytecode obj, ArchiveMode mode, SerializationStream stream)
            {
                if (mode == ArchiveMode.Serialize)
                {
                    stream.Write(obj.DataES2 ?? "");
                    stream.Write(obj.DataES3 ?? "");
                }
                else
                {
                    var es2Data = stream.ReadString();
                    var es3Data = stream.ReadString();

                    obj = new ShaderLevelBytecode
                    {
                        DataES2 = String.IsNullOrEmpty(es2Data) ? null : es2Data,
                        DataES3 = String.IsNullOrEmpty(es3Data) ? null : es3Data
                    };
                }
            }
        }
    }
}
