// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

namespace Stride.Graphics
{
    public partial class Buffer
    {
        /// <summary>
        ///   Helper methods for creating <strong>Index Buffers</strong>.
        /// </summary>
        /// <remarks>
        ///   An <strong>Index Buffer</strong> is a <see cref="Buffer"/> that stores indices that reference vertices in a Vertex Buffer.
        ///   They are used for efficient rendering of triangle lists / strip. They are, however, limited to only 16-bit or 32-bit
        ///   values.
        /// </remarks>
        /// <seealso cref="Buffer"/>
        /// <seealso cref="Buffer{T}"/>
        public static class Index
        {
            /// <summary>
            ///   Creates a new <strong>Index Buffer</strong> of a given size.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="bufferSize">Size of the Buffer in bytes.</param>
            /// <param name="usage">
            ///   The usage for the Buffer, which determines who can read/write data. By default, it is <see cref="GraphicsResourceUsage.Default"/>.
            /// </param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer New(GraphicsDevice device, int bufferSize, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
            {
                return Buffer.New(device, bufferSize, BufferFlags.IndexBuffer, usage);
            }

            /// <summary>
            ///   Creates a new <strong>Index Buffer</strong>.
            /// </summary>
            /// <typeparam name="T">Type of the indices stored in the Buffer. It can only be <see cref="short"/> or <see cref="int"/>.</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="usage">
            ///   The usage for the Buffer, which determines who can read/write data. By default, it is <see cref="GraphicsResourceUsage.Default"/>.
            /// </param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
            {
                return Buffer.New<T>(device, elementCount: 1, BufferFlags.IndexBuffer, usage);
            }

            /// <summary>
            ///   Creates a new <strong>Index Buffer</strong> with initial data.
            /// </summary>
            /// <typeparam name="T">Type of the indices stored in the Buffer. It can only be <see cref="short"/> or <see cref="int"/>.</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="value">The value to initialize the Index Buffer.</param>
            /// <param name="usage">
            ///   The usage for the Buffer, which determines who can read/write data. By default, it is <see cref="GraphicsResourceUsage.Immutable"/>.
            /// </param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, ref readonly T value, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable) where T : unmanaged
            {
                return Buffer.New(device, in value, BufferFlags.IndexBuffer, usage);
            }

            /// <summary>
            ///   Creates a new <strong>Index Buffer</strong> with initial data.
            /// </summary>
            /// <typeparam name="T">Type of the indices stored in the Buffer. It can only be <see cref="short"/> or <see cref="int"/>.</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="data">The data to initialize the Index Buffer.</param>
            /// <param name="usage">
            ///   The usage for the Buffer, which determines who can read/write data. By default, it is <see cref="GraphicsResourceUsage.Immutable"/>.
            /// </param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, T[] data, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable) where T : unmanaged
            {
                return Buffer.New(device, (ReadOnlySpan<T>) data, BufferFlags.IndexBuffer, usage:usage);
            }

            /// <summary>
            ///   Creates a new <strong>Index Buffer</strong> with initial data.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="data">The data to initialize the Index Buffer.</param>
            /// <param name="is32BitIndex">
            ///   <see langword="true"/> if the Buffer will use 32-bit indices; <see langword="false"/> for using 16-bit indices.
            /// </param>
            /// <param name="usage">
            ///   The usage for the Buffer, which determines who can read/write data. By default, it is <see cref="GraphicsResourceUsage.Immutable"/>.
            /// </param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer New(GraphicsDevice device, byte[] data, bool is32BitIndex, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
            {
                return Buffer.New(device, data, elementSize: (is32BitIndex ? 4 : 2), BufferFlags.IndexBuffer, PixelFormat.None, usage);
            }

            /// <summary>
            ///   Creates a new <strong>Index Buffer</strong> with initial data.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="data">The data to initialize the Index Buffer.</param>
            /// <param name="usage">
            ///   The usage for the Buffer, which determines who can read/write data. By default, it is <see cref="GraphicsResourceUsage.Immutable"/>.
            /// </param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer New(GraphicsDevice device, ReadOnlySpan<byte> data, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
            {
                return Buffer.New(device, data, elementSize: 0, BufferFlags.IndexBuffer, PixelFormat.None, usage);
            }

            /// <summary>
            ///   Creates a new <strong>Index Buffer</strong>.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="dataPointer">The data pointer to the data to initialize the Index Buffer.</param>
            /// <param name="usage">
            ///   The usage for the Buffer, which determines who can read/write data. By default, it is <see cref="GraphicsResourceUsage.Immutable"/>.
            /// </param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            [Obsolete("This method is obsolete. Use the span-based methods instead")]
            public static Buffer New(GraphicsDevice device, DataPointer dataPointer, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
            {
                return Buffer.New(device, dataPointer, elementSize: 0, BufferFlags.IndexBuffer, usage);
            }
        }
    }
}
