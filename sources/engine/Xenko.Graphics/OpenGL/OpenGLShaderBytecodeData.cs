// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Serialization;

namespace Xenko.Graphics
{
    internal struct OpenGLShaderBytecodeData
    {
        public bool IsBinary;

        public string Profile;
        public string EntryPoint;
        public string Source;

        public int BinaryFormat;
        public byte[] Binary;

        public void Serialize(SerializationStream stream, ArchiveMode mode)
        {
            // Check version number (should be 0 for now, for future use)
            if (mode == ArchiveMode.Serialize)
            {
                stream.Write(0);
            }
            else if (mode == ArchiveMode.Deserialize)
            {
                if (stream.ReadInt32() != 0)
                    throw new InvalidOperationException("Unexpected version number.");
            }

            // Serialize content
            stream.Serialize(ref IsBinary);
            if (IsBinary)
            {
                stream.Serialize(ref BinaryFormat);
                stream.Serialize(ref Binary, mode);
            }
            else
            {
                stream.Serialize(ref Profile);
                stream.Serialize(ref EntryPoint);
                stream.Serialize(ref Source);
            }
        }
    }
}
