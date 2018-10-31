// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

using Xenko.Games;

namespace Xenko.Graphics
{
    public partial class Buffer
    {
        /// <summary>
        /// Vertex buffer helper methods.
        /// </summary>
        public static class Vertex
        {
            /// <summary>
            /// Creates a new Vertex buffer with <see cref="GraphicsResourceUsage.Default"/> uasge by default.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="size">The size in bytes.</param>
            /// <param name="usage">The usage.</param>
            /// <param name="bindFlags">The bind flags, can be combined with <see cref="BufferFlags.StreamOutput"/> to use the buffer as a stream output target.</param>
            /// <returns>
            /// A Vertex buffer
            /// </returns>
            public static Buffer New(GraphicsDevice device, int size, GraphicsResourceUsage usage = GraphicsResourceUsage.Default, BufferFlags bindFlags = BufferFlags.VertexBuffer)
            {
                return Buffer.New(device, size, bindFlags, usage);
            }

            /// <summary>
            /// Creates a new Vertex buffer with <see cref="GraphicsResourceUsage.Default"/> uasge by default.
            /// </summary>
            /// <typeparam name="T">Type of the Vertex buffer to get the sizeof from</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="usage">The usage.</param>
            /// <returns>A Vertex buffer</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : struct
            {
                return Buffer.New<T>(device, 1, BufferFlags.VertexBuffer, usage);
            }

            /// <summary>
            /// Creates a new Vertex buffer with <see cref="GraphicsResourceUsage.Immutable"/> uasge by default.
            /// </summary>
            /// <typeparam name="T">Type of the Vertex buffer to get the sizeof from</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="value">The value to initialize the Vertex buffer.</param>
            /// <param name="usage">The usage of this resource.</param>
            /// <returns>A Vertex buffer</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, ref T value, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable) where T : struct
            {
                return Buffer.New(device, ref value, BufferFlags.VertexBuffer, usage);
            }

            /// <summary>
            /// Creates a new Vertex buffer with <see cref="GraphicsResourceUsage.Immutable"/> uasge by default.
            /// </summary>
            /// <typeparam name="T">Type of the Vertex buffer to get the sizeof from</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="value">The value to initialize the Vertex buffer.</param>
            /// <param name="usage">The usage of this resource.</param>
            /// <returns>A Vertex buffer</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, T[] value, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable) where T : struct
            {
                return Buffer.New(device, value, BufferFlags.VertexBuffer, usage);
            }

            /// <summary>
            /// Creates a new Vertex buffer with <see cref="GraphicsResourceUsage.Default"/> usage by default.
            /// </summary>
            /// <typeparam name="T">Type of the Vertex buffer to get the sizeof from</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="vertexBufferCount">Number of vertex in this buffer with the sizeof(T).</param>
            /// <param name="usage">The usage.</param>
            /// <returns>A Vertex buffer</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, int vertexBufferCount, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : struct
            {
                return Buffer.New<T>(device, vertexBufferCount, BufferFlags.VertexBuffer, usage);
            }

            /// <summary>
            /// Creates a new Vertex buffer with <see cref="GraphicsResourceUsage.Immutable"/> uasge by default.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="value">The value to initialize the Vertex buffer.</param>
            /// <param name="usage">The usage of this resource.</param>
            /// <returns>A Vertex buffer</returns>
            public static Buffer New(GraphicsDevice device, DataPointer value, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
            {
                return Buffer.New(device, value, 0, BufferFlags.VertexBuffer, usage);
            }
        }
    }
}
