// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Stride.Core.Annotations;

namespace Stride.Core.Serialization
{
    /// <summary>
    /// Various useful extension methods on top of SerializationStream for serialization/deserialization of common types.
    /// </summary>
    public static class SerializerExtensions
    {
        public static T Clone<T>(T obj)
        {
            var memoryStream = new MemoryStream();
            var writer = new BinarySerializationWriter(memoryStream);
            writer.SerializeExtended(obj, ArchiveMode.Serialize);
            writer.Flush();

            var result = default(T);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var reader = new BinarySerializationReader(memoryStream);
            reader.SerializeExtended(ref result, ArchiveMode.Deserialize);
            return result;
        }

        /// <summary>
        /// Serializes the specified object.
        /// </summary>
        /// <typeparam name="T">The object type to serialize.</typeparam>
        /// <param name="stream">The stream to serialize to.</param>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="mode">The serialization mode.</param>
        /// <param name="dataSerializer">The data serializer (can be null).</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SerializeExtended<T>([NotNull] this SerializationStream stream, T obj, ArchiveMode mode, DataSerializer<T> dataSerializer = null)
        {
            MemberReuseSerializer<T>.SerializeExtended(ref obj, mode, stream, dataSerializer);
        }

        /// <summary>
        /// Serializes the specified object.
        /// </summary>
        /// <typeparam name="T">The object type to serialize.</typeparam>
        /// <param name="stream">The stream to serialize to.</param>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="mode">The serialization mode.</param>
        /// <param name="dataSerializer">The data serializer (can be null).</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SerializeExtended<T>([NotNull] this SerializationStream stream, ref T obj, ArchiveMode mode, DataSerializer<T> dataSerializer = null)
        {
            MemberReuseSerializer<T>.SerializeExtended(ref obj, mode, stream, dataSerializer);
        }

        /// <summary>
        /// Reads the specified object from the stream.
        /// </summary>
        /// <typeparam name="T">The type of the object to read.</typeparam>
        /// <param name="stream">The stream to read the object from.</param>
        /// <returns>The object that has just been read.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>([NotNull] this SerializationStream stream)
        {
            var result = default(T);
            stream.Serialize(ref result, ArchiveMode.Deserialize);
            return result;
        }
        /// <summary>
        /// Writes the specified object to the stream.
        /// </summary>
        /// <typeparam name="T">The type of the object to write.</typeparam>
        /// <param name="stream">The stream to write the object to.</param>
        /// <param name="obj">The object to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>([NotNull] this SerializationStream stream, T obj)
        {
            Serialize(stream, ref obj, ArchiveMode.Serialize);
        }

        /// <summary>
        /// Serializes the specified object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream">The stream to serialize to.</param>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="mode">The serialization mode.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Serialize<T>([NotNull] this SerializationStream stream, ref T obj, ArchiveMode mode)
        {
            var dataSerializer = stream.Context.SerializerSelector.GetSerializer<T>();
            if (dataSerializer == null)
                throw new InvalidOperationException($"Could not find serializer for type {typeof(T)}.");

            dataSerializer.PreSerialize(ref obj, mode, stream);
            dataSerializer.Serialize(ref obj, mode, stream);
        }

        /// <summary>
        /// Reads a boolean value from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A boolean value read from the stream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadBoolean([NotNull] this SerializationStream stream)
        {
            var value = false;
            stream.Serialize(ref value);
            return value;
        }

        /// <summary>
        /// Reads a 4-byte floating point value from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A 4-byte floating point value read from the stream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadSingle([NotNull] this SerializationStream stream)
        {
            var value = 0.0f;
            stream.Serialize(ref value);
            return value;
        }

        /// <summary>
        /// Reads a 8-byte floating point value from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A 8-byte floating point value read from the stream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReadDouble([NotNull] this SerializationStream stream)
        {
            var value = 0.0;
            stream.Serialize(ref value);
            return value;
        }

        /// <summary>
        /// Reads a 2-byte signed integer from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A 2-byte signed integer read from the stream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16([NotNull] this SerializationStream stream)
        {
            short value = 0;
            stream.Serialize(ref value);
            return value;
        }

        /// <summary>
        /// Reads a 4-byte signed integer from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A 4-byte signed integer read from the stream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32([NotNull] this SerializationStream stream)
        {
            var value = 0;
            stream.Serialize(ref value);
            return value;
        }

        /// <summary>
        /// Reads a 8-byte signed integer from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A 8-byte signed integer read from the stream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64([NotNull] this SerializationStream stream)
        {
            long value = 0;
            stream.Serialize(ref value);
            return value;
        }

        /// <summary>
        /// Reads a 2-byte unsigned integer from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A 2-byte unsigned integer read from the stream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16([NotNull] this SerializationStream stream)
        {
            ushort value = 0;
            stream.Serialize(ref value);
            return value;
        }

        /// <summary>
        /// Reads a 4-byte unsigned integer from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A 4-byte unsigned integer read from the stream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32([NotNull] this SerializationStream stream)
        {
            uint value = 0;
            stream.Serialize(ref value);
            return value;
        }

        /// <summary>
        /// Reads a 8-byte unsigned integer from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A 8-byte unsigned integer read from the stream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadUInt64([NotNull] this SerializationStream stream)
        {
            ulong value = 0;
            stream.Serialize(ref value);
            return value;
        }

        /// <summary>
        /// Reads a string.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A string read from the stream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadString([NotNull] this SerializationStream stream)
        {
            string value = null;
            stream.Serialize(ref value);
            return value;
        }

        /// <summary>
        /// Reads a unicode character from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A unicode character read from the stream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ReadChar([NotNull] this SerializationStream stream)
        {
            var value = '\0';
            stream.Serialize(ref value);
            return value;
        }

        /// <summary>
        /// Reads a unsigned byte integer from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>An unsigned byte read from the stream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadByte([NotNull] this SerializationStream stream)
        {
            byte value = 0;
            stream.Serialize(ref value);
            return value;
        }

        /// <summary>
        /// Reads a signed byte from the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A signed byte read from the stream.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReadSByte([NotNull] this SerializationStream stream)
        {
            sbyte value = 0;
            stream.Serialize(ref value);
            return value;
        }

        /// <summary>
        /// Reads the specified number of bytes.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="count"></param>
        /// <returns>A byte array containing the data read from the stream.</returns>
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ReadBytes([NotNull] this SerializationStream stream, int count)
        {
            var value = new byte[count];
            stream.Serialize(value, 0, count);
            return value;
        }

        /// <summary>
        /// Writes a boolean value to the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The boolean value to write.</param>
        /// <returns>The stream.</returns>
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializationStream Write([NotNull] this SerializationStream stream, bool value)
        {
            stream.Serialize(ref value);
            return stream;
        }

        /// <summary>
        /// Writes a 4-byte floating point value to the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The 4-byte floating point value to write.</param>
        /// <returns>The stream.</returns>
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializationStream Write([NotNull] this SerializationStream stream, float value)
        {
            stream.Serialize(ref value);
            return stream;
        }

        /// <summary>
        /// Writes a 8-byte floating point value to the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The 8-byte floating point value to write.</param>
        /// <returns>The stream.</returns>
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializationStream Write([NotNull] this SerializationStream stream, double value)
        {
            stream.Serialize(ref value);
            return stream;
        }

        /// <summary>
        /// Writes a 2-byte signed integer to the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The 2-byte signed integer to write.</param>
        /// <returns>The stream.</returns>
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializationStream Write([NotNull] this SerializationStream stream, short value)
        {
            stream.Serialize(ref value);
            return stream;
        }

        /// <summary>
        /// Writes a 4-byte signed integer to the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The 4-byte signed integer to write.</param>
        /// <returns>The stream.</returns>
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializationStream Write([NotNull] this SerializationStream stream, int value)
        {
            stream.Serialize(ref value);
            return stream;
        }

        /// <summary>
        /// Writes a 8-byte signed integer to the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The 8-byte signed integer to write.</param>
        /// <returns>The stream.</returns>
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializationStream Write([NotNull] this SerializationStream stream, long value)
        {
            stream.Serialize(ref value);
            return stream;
        }

        /// <summary>
        /// Writes a 2-byte unsigned integer to the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The 2-byte unsigned integer to write.</param>
        /// <returns>The stream.</returns>
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializationStream Write([NotNull] this SerializationStream stream, ushort value)
        {
            stream.Serialize(ref value);
            return stream;
        }

        /// <summary>
        /// Writes a 4-byte unsigned integer to the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The 4-byte unsigned integer to write.</param>
        /// <returns>The stream.</returns>
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializationStream Write([NotNull] this SerializationStream stream, uint value)
        {
            stream.Serialize(ref value);
            return stream;
        }

        /// <summary>
        /// Writes a 8-byte unsigned integer to the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The 8-byte unsigned integer to write.</param>
        /// <returns>The stream.</returns>
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializationStream Write([NotNull] this SerializationStream stream, ulong value)
        {
            stream.Serialize(ref value);
            return stream;
        }

        /// <summary>
        /// Writes a string to the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The string to write.</param>
        /// <returns>The stream.</returns>
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializationStream Write([NotNull] this SerializationStream stream, string value)
        {
            stream.Serialize(ref value);
            return stream;
        }

        /// <summary>
        /// Writes a unicode character to the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The unicode character to write.</param>
        /// <returns>The stream.</returns>
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializationStream Write([NotNull] this SerializationStream stream, char value)
        {
            stream.Serialize(ref value);
            return stream;
        }

        /// <summary>
        /// Writes an unsigned byte to the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The unsigned byte to write.</param>
        /// <returns>The stream.</returns>
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializationStream Write([NotNull] this SerializationStream stream, byte value)
        {
            stream.Serialize(ref value);
            return stream;
        }

        /// <summary>
        /// Writes a signed byte to the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The signed byte to write.</param>
        /// <returns>The stream.</returns>
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializationStream Write([NotNull] this SerializationStream stream, sbyte value)
        {
            stream.Serialize(ref value);
            return stream;
        }

        /// <summary>
        /// Writes a byte array region to the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="values">The byte array to write.</param>
        /// <param name="offset">The starting offset in values to write.</param>
        /// <param name="count">The number of bytes to write.</param>
        /// <returns>
        /// The stream.
        /// </returns>
        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializationStream Write([NotNull] this SerializationStream stream, byte[] values, int offset, int count)
        {
            stream.Serialize(values, offset, count);
            return stream;
        }
    }
}
