// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.IO;

namespace Stride.Core.Storage
{
    public class DigestStream : OdbStreamWriter
    {
        private ObjectIdBuilder builder = new ObjectIdBuilder();

        public override ObjectId CurrentHash
        {
            get
            {
                return builder.ComputeHash();
            }
        }

        public DigestStream(Stream stream) : base(stream, null)
        {
        }

        internal DigestStream(Stream stream, string temporaryName) : base(stream, temporaryName)
        {
        }

        public void Reset()
        {
            Position = 0;
            builder.Reset();
        }

        public override void WriteByte(byte value)
        {
            builder.WriteByte(value);
            stream.WriteByte(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            builder.Write(buffer, offset, count);
            stream.Write(buffer, offset, count);
        }
    }
}
