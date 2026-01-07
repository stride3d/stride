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
        ///   Helper methods for creating <strong>Typed Buffers</strong>.
        /// </summary>
        /// <remarks>
        ///   A <strong>Typed Buffer</strong> is a <see cref="Buffer"/> that is accessed through a Shader Resource View with a specific format.
        ///   Because of this, they are created with that specific format.
        ///   <para>
        ///     An example of this kind of Buffer in SDSL would be:
        ///     <code>
        ///       Buffer&lt;float4&gt; tb;
        ///     </code>
        ///   </para>
        /// </remarks>
        /// <seealso cref="Buffer"/>
        /// <seealso cref="Buffer{T}"/>
        public static class Typed
        {
            /// <summary>
            ///   Creates a new <strong>Typed Buffer</strong> of a given size.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="elementCount">The number of data with the following viewFormat.</param>
            /// <param name="elementFormat">The format of the typed elements in the Buffer.</param>
            /// <param name="unorderedAccess"><see langword="true"/> if the Buffer should support unordered access (<c>RW</c> in SDSL).</param>
            /// <param name="usage">
            ///   The usage for the Buffer, which determines who can read/write data. By default, it is <see cref="GraphicsResourceUsage.Default"/>.
            /// </param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer New(GraphicsDevice device, int elementCount, PixelFormat elementFormat, bool unorderedAccess = false, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
            {
                return Buffer.New(device, bufferSize: elementCount * elementFormat.SizeInBytes, BufferFlags.ShaderResource | (unorderedAccess ? BufferFlags.UnorderedAccess : BufferFlags.None), elementFormat, usage);
            }

            /// <summary>
            ///   Creates a new <strong>Typed Buffer</strong> with initial data.
            /// </summary>
            /// <typeparam name="T">Type of the data stored in the Buffer.</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="data">The data to initialize the Typed Buffer.</param>
            /// <param name="elementFormat">The format of the typed elements in the Buffer.</param>
            /// <param name="unorderedAccess"><see langword="true"/> if the Buffer should support unordered access (<c>RW</c> in SDSL).</param>
            /// <param name="usage">
            ///   The usage for the Buffer, which determines who can read/write data. By default, it is <see cref="GraphicsResourceUsage.Default"/>.
            /// </param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, T[] data, PixelFormat elementFormat, bool unorderedAccess = false, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
            {
                return Buffer.New(device, (ReadOnlySpan<T>)data, BufferFlags.ShaderResource | (unorderedAccess ? BufferFlags.UnorderedAccess : BufferFlags.None), elementFormat, usage);
            }

            /// <summary>
            ///   Creates a new <strong>Typed Buffer</strong> with initial data.
            /// </summary>
            /// <typeparam name="T">Type of the data stored in the Buffer.</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="data">The data to initialize the Typed Buffer.</param>
            /// <param name="elementFormat">The format of the typed elements in the Buffer.</param>
            /// <param name="unorderedAccess"><see langword="true"/> if the Buffer should support unordered access (<c>RW</c> in SDSL).</param>
            /// <param name="usage">
            ///   The usage for the Buffer, which determines who can read/write data. By default, it is <see cref="GraphicsResourceUsage.Default"/>.
            /// </param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, ReadOnlySpan<T> data, PixelFormat elementFormat, bool unorderedAccess = false, GraphicsResourceUsage usage = GraphicsResourceUsage.Default) where T : unmanaged
            {
                return Buffer.New(device, data, BufferFlags.ShaderResource | (unorderedAccess ? BufferFlags.UnorderedAccess : BufferFlags.None), elementFormat, usage);
            }

            /// <summary>
            ///   Creates a new <strong>Typed Buffer</strong> with initial data.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="dataPointer">The data pointer to the data to initialize the Typed Buffer.</param>
            /// <param name="elementFormat">The format of the typed elements in the Buffer.</param>
            /// <param name="unorderedAccess"><see langword="true"/> if the Buffer should support unordered access (<c>RW</c> in SDSL).</param>
            /// <param name="usage">
            ///   The usage for the Buffer, which determines who can read/write data. By default, it is <see cref="GraphicsResourceUsage.Default"/>.
            /// </param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            [Obsolete("This method is obsolete. Use the span-based methods instead")]
            public static Buffer New(GraphicsDevice device, DataPointer dataPointer, PixelFormat elementFormat, bool unorderedAccess = false, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
            {
                return Buffer.New(device, dataPointer, 0, BufferFlags.ShaderResource | (unorderedAccess ? BufferFlags.UnorderedAccess : BufferFlags.None), elementFormat, usage);
            }
        }
    }
}
