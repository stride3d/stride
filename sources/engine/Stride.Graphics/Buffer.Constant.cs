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
        ///   Helper methods for creating <strong>Constant Buffers</strong>.
        /// </summary>
        /// <remarks>
        ///   A <strong>Constant Buffer</strong> is a <see cref="Buffer"/> that is used to pass shader constants / parameters to shaders.
        ///   They have fast access due to special hardware optimizations, being read-only to shaders, and being of a limited size
        ///   (64 kB usually).
        /// </remarks>
        /// <seealso cref="Buffer"/>
        /// <seealso cref="Buffer{T}"/>
        public static class Constant
        {
            /// <summary>
            ///   Creates a new <strong>Constant Buffer</strong> of a given size.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="bufferSize">Size of the Buffer in bytes.</param>
            /// <param name="usage">
            ///   The usage for the Buffer, which determines who can read/write data. By default, it is <see cref="GraphicsResourceUsage.Dynamic"/>.
            /// </param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer New(GraphicsDevice device, int bufferSize, GraphicsResourceUsage usage = GraphicsResourceUsage.Dynamic)
            {
                return Buffer.New(device, bufferSize, BufferFlags.ConstantBuffer, usage);
            }

            /// <summary>
            ///   Creates a new <strong>Constant Buffer</strong> with <see cref="GraphicsResourceUsage.Dynamic"/> usage.
            /// </summary>
            /// <typeparam name="T">Type of the data stored in the Buffer.</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer<T> New<T>(GraphicsDevice device) where T : unmanaged
            {
                return Buffer.New<T>(device, elementCount: 1, BufferFlags.ConstantBuffer, GraphicsResourceUsage.Dynamic);
            }

            /// <summary>
            ///   Creates a new <strong>Constant Buffer</strong> with initial data.
            /// </summary>
            /// <typeparam name="T">Type of the data stored in the Buffer.</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="value">The value to initialize the Constant Buffer.</param>
            /// <param name="usage">
            ///   The usage for the Buffer, which determines who can read/write data. By default, it is <see cref="GraphicsResourceUsage.Dynamic"/>.
            /// </param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, ref readonly T value, GraphicsResourceUsage usage = GraphicsResourceUsage.Dynamic) where T : unmanaged
            {
                return Buffer.New(device, in value, BufferFlags.ConstantBuffer, usage);
            }

            /// <summary>
            ///   Creates a new <strong>Constant Buffer</strong> with initial data.
            /// </summary>
            /// <typeparam name="T">Type of the data stored in the Buffer.</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="data">The data to initialize the Constant Buffer.</param>
            /// <param name="usage">
            ///   The usage for the Buffer, which determines who can read/write data. By default, it is <see cref="GraphicsResourceUsage.Dynamic"/>.
            /// </param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, T[] data, GraphicsResourceUsage usage = GraphicsResourceUsage.Dynamic) where T : unmanaged
            {
                return Buffer.New(device, data, BufferFlags.ConstantBuffer, usage:usage);
            }

            /// <summary>
            ///   Creates a new <strong>Constant Buffer</strong> with initial data.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="data">The data to initialize the Constant Buffer.</param>
            /// <param name="usage">
            ///   The usage for the Buffer, which determines who can read/write data. By default, it is <see cref="GraphicsResourceUsage.Dynamic"/>.
            /// </param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer New(GraphicsDevice device, ReadOnlySpan<byte> data, GraphicsResourceUsage usage = GraphicsResourceUsage.Dynamic)
            {
                return Buffer.New(device, data, elementSize: 0, BufferFlags.ConstantBuffer, usage:usage);
            }

            /// <summary>
            ///   Creates a new <strong>Constant Buffer</strong> with initial data.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="dataPointer">The data pointer to the data to initialize the Constant Buffer.</param>
            /// <param name="usage">
            ///   The usage for the Buffer, which determines who can read/write data. By default, it is <see cref="GraphicsResourceUsage.Dynamic"/>.
            /// </param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            [Obsolete("This method is obsolete. Use the span-based methods instead")]
            public static Buffer New(GraphicsDevice device, DataPointer dataPointer, GraphicsResourceUsage usage = GraphicsResourceUsage.Dynamic)
            {
                return Buffer.New(device, dataPointer, 0, BufferFlags.ConstantBuffer, usage);
            }
        }
    }
}
