// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.IO;
using Stride.Core.Annotations;

namespace Stride.Core.Serialization
{
    /// <summary>
    /// Binary serialization method helpers to easily read/write data from a stream.
    /// </summary>
    /// <remarks>
    /// This class is a simple front end to <see cref="BinarySerializationReader"/> and <see cref="BinarySerializationWriter"/>.
    /// </remarks>
    public class BinarySerialization
    {
        /// <summary>
        /// Reads an object instance from the specified stream.
        /// </summary>
        /// <typeparam name="T">Type of the object to read</typeparam>
        /// <param name="stream">The stream to read the object instance.</param>
        /// <returns>An object instance of type T.</returns>
        public static T Read<T>([NotNull] Stream stream)
        {
            var reader = new BinarySerializationReader(stream);
            return reader.Read<T>();
        }

        /// <summary>
        /// Reads an object instance from the specified byte buffer.
        /// </summary>
        /// <typeparam name="T">Type of the object to read</typeparam>
        /// <param name="buffer">The byte buffer to read the object instance.</param>
        /// <returns>An object instance of type T.</returns>
        public static T Read<T>([NotNull] byte[] buffer)
        {
            var reader = new BinarySerializationReader(new MemoryStream(buffer));
            return reader.Read<T>();
        }

        /// <summary>
        /// Writes an object instance to the specified stream.
        /// </summary>
        /// <typeparam name="T">Type of the object to write</typeparam>
        /// <param name="stream">The stream to write the object instance to.</param>
        /// <param name="value">The value to write.</param>
        public static void Write<T>([NotNull] Stream stream, T value)
        {
            var writer = new BinarySerializationWriter(stream);
            writer.Write(value);
        }
    }
}
