// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using Xenko.Core.Annotations;

namespace Xenko.Core.Serialization
{
    public class HashSerializationWriter : BinarySerializationWriter
    {
        public HashSerializationWriter([NotNull] Stream outputStream) : base(outputStream)
        {
        }

        /// <inheritdoc/>
        public override unsafe void Serialize(ref string value)
        {
            fixed (char* bufferStart = value)
            {
                Serialize((IntPtr)bufferStart, sizeof(char) * value.Length);
            }
        }
    }
}
