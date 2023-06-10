// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using Stride.Core.Annotations;

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
            UnderlyingStream.WriteByte(value ? (byte)1 : (byte)0);
        }

        /// <inheritdoc />
        public override unsafe void Serialize(ref float value)
        {
            unsafe
            {
                fixed (float* ptr = &value)
                {
                    var span = new Span<byte>(ptr, sizeof(float));
                    Writer.Write(span);
                }
            }
        }

        /// <inheritdoc />
        public override unsafe void Serialize(ref double value)
        {
            unsafe
            {
                fixed (double* ptr = &value)
                {
                    var span = new Span<byte>(ptr, sizeof(double));
                    Writer.Write(span);
                }
            }
        }

        /// <inheritdoc />
        public override void Serialize(ref short value)
        {
            unsafe
            {
                fixed (short* ptr = &value)
                {
                    var span = new Span<byte>(ptr, sizeof(short));
                    Writer.Write(span);
                }
            }
        }

        /// <inheritdoc />
        public override void Serialize(ref int value)
        {
            unsafe
            {
                fixed (int* ptr = &value)
                {
                    var span = new Span<byte>(ptr, sizeof(int));
                    Writer.Write(span);
                }
            }
        }

        /// <inheritdoc />
        public override void Serialize(ref long value)
        {
            unsafe
            {
                fixed (long* ptr = &value)
                {
                    var span = new Span<byte>(ptr, sizeof(long));
                    Writer.Write(span);
                }
            }
        }

        /// <inheritdoc />
        public override void Serialize(ref ushort value)
        {
            unsafe
            {
                fixed (ushort* ptr = &value)
                {
                    var span = new Span<byte>(ptr, sizeof(ushort));
                    Writer.Write(span);
                }
            }
        }

        /// <inheritdoc />
        public override void Serialize(ref uint value)
        {
            unsafe
            {
                fixed (uint* ptr = &value)
                {
                    var span = new Span<byte>(ptr, sizeof(uint));
                    Writer.Write(span);
                }
            }
        }

        /// <inheritdoc />
        public override void Serialize(ref ulong value)
        {
            unsafe
            {
                fixed (ulong* ptr = &value)
                {
                    var span = new Span<byte>(ptr, sizeof(ulong));
                    Writer.Write(span);
                }
            }
        }

        /// <inheritdoc />
        public override void Serialize(ref string value)
        {
            Writer.Write(value.AsSpan());
        }

        /// <inheritdoc />
        public override void Serialize(ref char value)
        {
            Writer.Write(value);
        }

        /// <inheritdoc />
        public override void Serialize(ref byte value)
        {
            unsafe
            {
                fixed (byte* ptr = &value)
                {
                    var span = new Span<byte>(ptr, sizeof(byte));
                    Writer.Write(span);
                }
            }
        }

        /// <inheritdoc />
        public override void Serialize(ref sbyte value)
        {
            unsafe
            {
                fixed (sbyte* ptr = &value)
                {
                    var span = new Span<byte>(ptr, sizeof(sbyte));
                    Writer.Write(span);
                }
            }
        }

        /// <inheritdoc />
        public override void Serialize([NotNull] byte[] values, int offset, int count)
        {
            UnderlyingStream.Write(values, offset, count);
        }

        /// <inheritdoc />
        public override void Serialize(Span<byte> buffer) => UnderlyingStream.Write(buffer);

        /// <inheritdoc />
        public override void Flush()
        {
            UnderlyingStream.Flush();
        }
    }
}
