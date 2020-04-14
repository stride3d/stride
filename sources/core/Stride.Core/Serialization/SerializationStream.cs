// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.IO;

namespace Stride.Core.Serialization
{
    /// <summary>
    /// Base class for implementation of <see cref="SerializationStream"/>.
    /// </summary>
    public abstract class SerializationStream
    {
        protected const int BufferTLSSize = 1024;

        // Helper buffer for classes needing it.
        // If null, it should be initialized with BufferTLSSize constant.
        [ThreadStatic]
        protected static byte[] bufferTLS;

        /// <summary>
        /// The underlying native stream.
        /// </summary>
        public NativeStream NativeStream { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializationStream"/> class.
        /// </summary>
        protected SerializationStream()
        {
            Context = new SerializerContext();
        }

        /// <summary>
        /// The serializer context.
        /// </summary>
        public SerializerContext Context { get; set; }

        /// <summary>
        /// Serializes the specified boolean value.
        /// </summary>
        /// <param name="value">The value to serialize</param>
        public abstract void Serialize(ref bool value);

        /// <summary>
        /// Serializes the specified float value.
        /// </summary>
        /// <param name="value">The value to serialize</param>
        public abstract void Serialize(ref float value);

        /// <summary>
        /// Serializes the specified double value.
        /// </summary>
        /// <param name="value">The value to serialize</param>
        public abstract void Serialize(ref double value);

        /// <summary>
        /// Serializes the specified short value.
        /// </summary>
        /// <param name="value">The value to serialize</param>
        public abstract void Serialize(ref short value);

        /// <summary>
        /// Serializes the specified integer value.
        /// </summary>
        /// <param name="value">The value to serialize</param>
        public abstract void Serialize(ref int value);

        /// <summary>
        /// Serializes the specified long value.
        /// </summary>
        /// <param name="value">The value to serialize</param>
        public abstract void Serialize(ref long value);

        /// <summary>
        /// Serializes the specified ushort value.
        /// </summary>
        /// <param name="value">The value to serialize</param>
        public abstract void Serialize(ref ushort value);

        /// <summary>
        /// Serializes the specified unsigned integer value.
        /// </summary>
        /// <param name="value">The value to serialize</param>
        public abstract void Serialize(ref uint value);

        /// <summary>
        /// Serializes the specified unsigned long value.
        /// </summary>
        /// <param name="value">The value to serialize</param>
        public abstract void Serialize(ref ulong value);

        /// <summary>
        /// Serializes the specified string value.
        /// </summary>
        /// <param name="value">The value to serialize</param>
        public abstract void Serialize(ref string value);

        /// <summary>
        /// Serializes the specified char value.
        /// </summary>
        /// <param name="value">The value to serialize</param>
        public abstract void Serialize(ref char value);

        /// <summary>
        /// Serializes the specified byte value.
        /// </summary>
        /// <param name="value">The value to serialize</param>
        public abstract void Serialize(ref byte value);

        /// <summary>
        /// Serializes the specified signed byte value.
        /// </summary>
        /// <param name="value">The value to serialize</param>
        public abstract void Serialize(ref sbyte value);

        /// <summary>
        /// Serializes the specified byte array.
        /// </summary>
        /// <param name="values">The buffer to serialize.</param>
        /// <param name="offset">The starting offset in the buffer to begin serializing.</param>
        /// <param name="count">The size, in bytes, to serialize.</param>
        public abstract void Serialize(byte[] values, int offset, int count);

        /// <summary>
        /// Serializes the specified memory area.
        /// </summary>
        /// <param name="memory">The memory area to serialize.</param>
        /// <param name="count">The size, in bytes, to serialize.</param>
        public abstract void Serialize(IntPtr memory, int count);

        /// <summary>
        /// Flushes all recent writes (for better batching).
        /// Please note that if only Serialize has been used (no PopTag()),
        /// Flush() should be called manually.
        /// </summary>
        public abstract void Flush();
    }

    // TODO: Switch to extensible/composite enumeration
    public class SerializationTagType
    {
        public static readonly SerializationTagType StartElement = new SerializationTagType();
        public static readonly SerializationTagType EndElement = new SerializationTagType();
        public static readonly SerializationTagType Identifier = new SerializationTagType();
    }

    public delegate void TagMarkedDelegate(SerializationStream stream, SerializationTagType tagType, object tagParam);
}
