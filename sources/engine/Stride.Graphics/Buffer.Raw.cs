// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

using Stride.Games;

namespace Stride.Graphics
{
    public partial class Buffer
    {
        /// <summary>
        /// Raw buffer helper methods.
        /// </summary>
        /// <remarks>
        /// Example in HLSL: ByteAddressBuffer or RWByteAddressBuffer for raw buffers supporting unordered access.
        /// </remarks>
        public static class Raw
        {
            /// <summary>
            /// Creates a new Raw buffer <see cref="GraphicsResourceUsage.Default" /> uasge.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="size">The size in bytes.</param>
            /// <param name="additionalBindings">The additional bindings (for example, to create a combined raw/index buffer, pass <see cref="BufferFlags.IndexBuffer" />)</param>
            /// <param name="usage">The usage.</param>
            /// <returns>A Raw buffer</returns>
            public static Buffer New(GraphicsDevice device, int size, BufferFlags additionalBindings = BufferFlags.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
            {
                return Buffer.New(device, size, BufferFlags.RawBuffer | additionalBindings, usage);
            }

            /// <summary>
            /// Creates a new Raw buffer with <see cref="GraphicsResourceUsage.Default"/> uasge by default.
            /// </summary>
            /// <typeparam name="T">Type of the Raw buffer to get the sizeof from</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="additionalBindings">The additional bindings (for example, to create a combined raw/index buffer, pass <see cref="BufferFlags.IndexBuffer" />)</param>
            /// <param name="usage">The usage.</param>
            /// <returns>A Raw buffer</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, BufferFlags additionalBindings = BufferFlags.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : struct
            {
                return Buffer.New<T>(device, 1, BufferFlags.RawBuffer | additionalBindings, usage);
            }

            /// <summary>
            /// Creates a new Raw buffer with <see cref="GraphicsResourceUsage.Default"/> uasge by default.
            /// </summary>
            /// <typeparam name="T">Type of the Raw buffer to get the sizeof from</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="value">The value to initialize the Raw buffer.</param>
            /// <param name="additionalBindings">The additional bindings (for example, to create a combined raw/index buffer, pass <see cref="BufferFlags.IndexBuffer" />)</param>
            /// <param name="usage">The usage of this resource.</param>
            /// <returns>A Raw buffer</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, ref T value, BufferFlags additionalBindings = BufferFlags.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : struct
            {
                return Buffer.New(device, ref value, BufferFlags.RawBuffer | additionalBindings, usage);
            }

            /// <summary>
            /// Creates a new Raw buffer with <see cref="GraphicsResourceUsage.Default"/> uasge by default.
            /// </summary>
            /// <typeparam name="T">Type of the Raw buffer to get the sizeof from</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="value">The value to initialize the Raw buffer.</param>
            /// <param name="additionalBindings">The additional bindings (for example, to create a combined raw/index buffer, pass <see cref="BufferFlags.IndexBuffer" />)</param>
            /// <param name="usage">The usage of this resource.</param>
            /// <returns>A Raw buffer</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, T[] value, BufferFlags additionalBindings = BufferFlags.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : struct
            {
                return Buffer.New(device, value, BufferFlags.RawBuffer | additionalBindings, usage);
            }

            /// <summary>
            /// Creates a new Raw buffer with <see cref="GraphicsResourceUsage.Default"/> uasge by default.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="value">The value to initialize the Raw buffer.</param>
            /// <param name="additionalBindings">The additional bindings (for example, to create a combined raw/index buffer, pass <see cref="BufferFlags.IndexBuffer" />)</param>
            /// <param name="usage">The usage of this resource.</param>
            /// <returns>A Raw buffer</returns>
            public static Buffer New(GraphicsDevice device, DataPointer value, BufferFlags additionalBindings = BufferFlags.None, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
            {
                return Buffer.New(device, value, 0, BufferFlags.RawBuffer | additionalBindings, usage);
            }
        }
    }
}
