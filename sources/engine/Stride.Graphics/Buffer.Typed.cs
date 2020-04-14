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
        /// Typed buffer helper methods.
        /// </summary>
        /// <remarks>
        /// Example in HLSL: Buffer&lt;float4&gt;.
        /// </remarks>
        public static class Typed
        {
            /// <summary>
            /// Creates a new Typed buffer <see cref="GraphicsResourceUsage.Default" /> uasge.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="count">The number of data with the following viewFormat.</param>
            /// <param name="viewFormat">The view format of the buffer.</param>
            /// <param name="isUnorderedAccess">if set to <c>true</c> this buffer supports unordered access (RW in HLSL).</param>
            /// <param name="usage">The usage.</param>
            /// <returns>A Typed buffer</returns>
            public static Buffer New(GraphicsDevice device, int count, PixelFormat viewFormat, bool isUnorderedAccess = false, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
            {
                return Buffer.New(device, count * viewFormat.SizeInBytes(), BufferFlags.ShaderResource | (isUnorderedAccess ? BufferFlags.UnorderedAccess : BufferFlags.None), viewFormat, usage);
            }

            /// <summary>
            /// Creates a new Typed buffer <see cref="GraphicsResourceUsage.Default" /> uasge.
            /// </summary>
            /// <typeparam name="T">Type of the Typed buffer to get the sizeof from</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="value">The value to initialize the Typed buffer.</param>
            /// <param name="viewFormat">The view format of the buffer.</param>
            /// <param name="isUnorderedAccess">if set to <c>true</c> this buffer supports unordered access (RW in HLSL).</param>
            /// <param name="usage">The usage of this resource.</param>
            /// <returns>A Typed buffer</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, T[] value, PixelFormat viewFormat, bool isUnorderedAccess = false, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : struct
            {
                return Buffer.New(device, value, BufferFlags.ShaderResource | (isUnorderedAccess ? BufferFlags.UnorderedAccess : BufferFlags.None), viewFormat, usage);
            }

            /// <summary>
            /// Creates a new Typed buffer <see cref="GraphicsResourceUsage.Default" /> uasge.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="value">The value to initialize the Typed buffer.</param>
            /// <param name="viewFormat">The view format of the buffer.</param>
            /// <param name="isUnorderedAccess">if set to <c>true</c> this buffer supports unordered access (RW in HLSL).</param>
            /// <param name="usage">The usage of this resource.</param>
            /// <returns>A Typed buffer</returns>
            public static Buffer New(GraphicsDevice device, DataPointer value, PixelFormat viewFormat, bool isUnorderedAccess = false, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
            {
                return Buffer.New(device, value, 0, BufferFlags.ShaderResource | (isUnorderedAccess ? BufferFlags.UnorderedAccess : BufferFlags.None), viewFormat, usage);
            }
        }
    }
}
