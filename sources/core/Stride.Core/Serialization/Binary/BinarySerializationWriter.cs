// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Stride.Core.Annotations;
using Stride.Core.IO;

namespace Stride.Core.Serialization
{
    /// <summary>
    /// Implements <see cref="SerializationStream"/> as a binary writer.
    /// </summary>
    public class BinarySerializationWriter : SerializationStream
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinarySerializationWriter"/> class.
        /// </summary>
        /// <param name="outputStream">The output stream.</param>
        public BinarySerializationWriter([NotNull] Stream outputStream)
        {
            Writer = new BinaryWriter(outputStream);
            UnderlyingStream = outputStream;
        }

        private BinaryWriter Writer { get; }

        /// <inheritdoc />
        public override void Serialize(ref bool value)
        {
            Writer.Write(value);
        }

        /// <inheritdoc />
        public override unsafe void Serialize(ref float value)
        {
            Writer.Write(value);
        }

        /// <inheritdoc />
        public override unsafe void Serialize(ref double value)
        {
            Writer.Write(value);
        }

        /// <inheritdoc />
        public override void Serialize(ref short value)
        {
            Writer.Write(value);
        }

        /// <inheritdoc />
        public override void Serialize(ref int value)
        {
            Writer.Write(value);
        }

        /// <inheritdoc />
        public override void Serialize(ref long value)
        {
            Writer.Write(value);
        }

        /// <inheritdoc />
        public override void Serialize(ref ushort value)
        {
            Writer.Write(value);
        }

        /// <inheritdoc />
        public override void Serialize(ref uint value)
        {
            Writer.Write(value);
        }

        /// <inheritdoc />
        public override void Serialize(ref ulong value)
        {
            Writer.Write(value);
        }

        /// <inheritdoc />
        public override void Serialize(ref string value)
        {
            Writer.Write(value);
        }

        /// <inheritdoc />
        public override void Serialize(ref char value)
        {
            Writer.Write(value);
        }

        /// <inheritdoc />
        public override void Serialize(ref byte value)
        {
            Writer.Write(value);
        }

        /// <inheritdoc />
        public override void Serialize(ref sbyte value)
        {
            Writer.Write(value);
        }

        /// <inheritdoc />
        public override void Serialize([NotNull] byte[] values, int offset, int count)
        {
            Writer.Write(values, offset, count);
        }

        /// <inheritdoc />
        public override void Serialize(Span<byte> buffer) => Writer.Write(buffer);

        /// <inheritdoc />
        public override void Flush()
        {
            Writer.Flush();
        }
    }
}
