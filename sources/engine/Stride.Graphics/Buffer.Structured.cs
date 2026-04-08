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
        ///   Helper methods for creating <strong>Structured Buffers</strong>.
        /// </summary>
        /// <remarks>
        ///   A <strong>Structured Buffer</strong> is a <see cref="Buffer"/> that can be read in shaders using a structured format.
        ///   They are an array of uniformly sized structures.
        ///   <para>
        ///     An example of this kind of Buffer in SDSL would be:
        ///     <code>
        ///       StructuredBuffer&lt;float4&gt; sb;
        ///       RWStructuredBuffer&lt;float4&gt; rwsb; // For Structured Buffers supporting unordered access
        ///     </code>
        ///   </para>
        /// </remarks>
        /// <seealso cref="Buffer"/>
        /// <seealso cref="Buffer{T}"/>
        public static class Structured
        {
            /// <summary>
            ///   Creates a new <strong>Structured Buffer</strong> of a given size.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="elementCount">The number of elements in the Buffer.</param>
            /// <param name="elementSize">The size in bytes of each element (the structure).</param>
            /// <param name="unorderedAccess"><see langword="true"/> if the Buffer should support unordered access (<c>RW</c> in SDSL).</param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer New(GraphicsDevice device, int elementCount, int elementSize, bool unorderedAccess = false)
            {
                var bufferFlags = BufferFlags.StructuredBuffer | BufferFlags.ShaderResource;

                if (unorderedAccess)
                    bufferFlags |= BufferFlags.UnorderedAccess;

                return Buffer.New(device, elementCount * elementSize, elementSize, bufferFlags, PixelFormat.None);
            }

            /// <summary>
            ///   Creates a new <strong>Structured Buffer</strong> of a given size.
            /// </summary>
            /// <typeparam name="T">Type of the data stored in the Buffer.</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="elementCount">The number of elements in the Buffer.</param>
            /// <param name="unorderedAccess"><see langword="true"/> if the Buffer should support unordered access (<c>RW</c> in SDSL).</param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, int elementCount, bool unorderedAccess = false) where T : unmanaged
            {
                var bufferFlags = BufferFlags.StructuredBuffer | BufferFlags.ShaderResource;

                if (unorderedAccess)
                    bufferFlags |= BufferFlags.UnorderedAccess;

                return Buffer.New<T>(device, elementCount, bufferFlags);
            }

            /// <summary>
            ///   Creates a new <strong>Structured Buffer</strong> with initial data.
            /// </summary>
            /// <typeparam name="T">Type of the Structured buffer to get the sizeof from</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="data">The data to initialize the Structured Buffer.</param>
            /// <param name="unorderedAccess"><see langword="true"/> if the Buffer should support unordered access (<c>RW</c> in SDSL).</param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, T[] data, bool unorderedAccess = false) where T : unmanaged
            {
                var bufferFlags = BufferFlags.StructuredBuffer | BufferFlags.ShaderResource;

                if (unorderedAccess)
                    bufferFlags |= BufferFlags.UnorderedAccess;

                return Buffer.New(device, (ReadOnlySpan<T>) data, bufferFlags);
            }

            /// <summary>
            ///   Creates a new <strong>Structured Buffer</strong> with initial data.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="data">The data to initialize the Structured Buffer.</param>
            /// <param name="elementSize">The size in bytes of each element (the structure).</param>
            /// <param name="unorderedAccess"><see langword="true"/> if the Buffer should support unordered access (<c>RW</c> in SDSL).</param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer New(GraphicsDevice device, ReadOnlySpan<byte> data, int elementSize, bool unorderedAccess = false)
            {
                var bufferFlags = BufferFlags.StructuredBuffer | BufferFlags.ShaderResource;

                if (unorderedAccess)
                    bufferFlags |= BufferFlags.UnorderedAccess;

                return Buffer.New(device, data, elementSize, bufferFlags);
            }

            /// <summary>
            ///   Creates a new <strong>Structured Buffer</strong> with initial data.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="dataPointer">The data pointer to the data to initialize the Structured Buffer.</param>
            /// <param name="elementSize">The size in bytes of each element (the structure).</param>
            /// <param name="unorderedAccess"><see langword="true"/> if the Buffer should support unordered access (<c>RW</c> in SDSL).</param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            [Obsolete("This method is obsolete. Use the span-based methods instead")]
            public static Buffer New(GraphicsDevice device, DataPointer dataPointer, int elementSize, bool unorderedAccess = false)
            {
                var bufferFlags = BufferFlags.StructuredBuffer | BufferFlags.ShaderResource;

                if (unorderedAccess)
                    bufferFlags |= BufferFlags.UnorderedAccess;

                return Buffer.New(device, dataPointer, elementSize, bufferFlags, PixelFormat.None);
            }
        }


        /// <summary>
        ///   Helper methods for creating <strong>Structured Append Buffers</strong>.
        /// </summary>
        /// <remarks>
        ///   A <strong>Structured Append Buffer</strong> (also known as <strong>Append / Consume Buffer</strong>) is a <see cref="Buffer"/> that
        ///   allows atomic append operations from shaders. They work like a stack: elements can be appended to the end.
        ///   They are a special kind of Structured Buffers, so they are also an array of uniformly sized structures.
        ///   <para>
        ///     An example of this kind of Buffer in SLSL would be:
        ///     <code>
        ///       AppendStructuredBuffer&lt;float4&gt; asb;
        ///       ConsumeStructuredBuffer&lt;float4&gt; csb;
        ///     </code>
        ///   </para>
        /// </remarks>
        /// <seealso cref="Buffer"/>
        /// <seealso cref="Buffer{T}"/>
        public static class StructuredAppend
        {
            private const BufferFlags StructuredAppendBufferFlags = BufferFlags.StructuredAppendBuffer | BufferFlags.ShaderResource | BufferFlags.UnorderedAccess;

            /// <summary>
            ///   Creates a new <strong>Structured Append Buffer</strong> of a given size.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="elementCount">The number of elements in the Buffer.</param>
            /// <param name="elementSize">The size in bytes of each element (the structure).</param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer New(GraphicsDevice device, int elementCount, int elementSize)
            {
                return Buffer.New(device, elementCount * elementSize, elementSize, StructuredAppendBufferFlags, PixelFormat.None);
            }

            /// <summary>
            ///   Creates a new <strong>Structured Append Buffer</strong> of a given size.
            /// </summary>
            /// <typeparam name="T">Type of the data stored in the Buffer.</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="elementCount">The number of elements in the Buffer.</param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, int elementCount) where T : unmanaged
            {
                return Buffer.New<T>(device, elementCount, StructuredAppendBufferFlags);
            }

            /// <summary>
            ///   Creates a new <strong>Structured Append Buffer</strong> with initial data.
            /// </summary>
            /// <typeparam name="T">Type of the data stored in the Buffer.</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="data">The data to initialize the Structured Append Buffer.</param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, T[] data) where T : unmanaged
            {
                return Buffer.New(device, (ReadOnlySpan<T>) data, StructuredAppendBufferFlags);
            }

            /// <summary>
            ///   Creates a new <strong>Structured Append Buffer</strong> with initial data.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="data">The data to initialize the Structured Append Buffer.</param>
            /// <param name="elementSize">The size in bytes of each element (the structure).</param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer New(GraphicsDevice device, ReadOnlySpan<byte> data, int elementSize)
            {
                return Buffer.New(device, data, elementSize, StructuredAppendBufferFlags);
            }

            /// <summary>
            ///   Creates a new <strong>Structured Append Buffer</strong> with initial data.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="dataPointer">The data pointer to the data to initialize the Structured Append Buffer.</param>
            /// <param name="elementSize">The size in bytes of each element (the structure).</param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            [Obsolete("This method is obsolete. Use the span-based methods instead")]
            public static Buffer New(GraphicsDevice device, DataPointer dataPointer, int elementSize)
            {
                return Buffer.New(device, dataPointer, elementSize, StructuredAppendBufferFlags, PixelFormat.None);
            }
        }


        /// <summary>
        ///   Helper methods for creating <strong>Structured Counter Buffers</strong>.
        /// </summary>
        /// <remarks>
        ///   A <strong>Structured Counter Buffer</strong> is a <see cref="Buffer"/> that allows atomic append operations
        ///   from shaders (similar to Append / Consume Buffers, see <see cref="StructuredAppend"/>), but with an associated counter.
        ///   That counter can be read / written atomically from shaders.
        ///   They are a special kind of Structured Buffers, so they are also an array of uniformly sized structures.
        ///   <para>
        ///     An example of this kind of Buffer in SLSL would be:
        ///     <code>
        ///       StructuredBuffer&lt;float4&gt; sb;
        ///       RWStructuredBuffer&lt;float4&gt; rwsb; // For structured buffers supporting unordered access
        ///     </code>
        ///   </para>
        /// </remarks>
        /// <seealso cref="Buffer"/>
        /// <seealso cref="Buffer{T}"/>
        public static class StructuredCounter
        {
            const BufferFlags StructuredCounterBufferFlags = BufferFlags.StructuredCounterBuffer | BufferFlags.ShaderResource | BufferFlags.UnorderedAccess;

            /// <summary>
            ///   Creates a new <strong>Structured Counter Buffer</strong> of a given size.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="elementCount">The number of elements in the Buffer.</param>
            /// <param name="elementSize">The size in bytes of each element (the structure).</param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer New(GraphicsDevice device, int elementCount, int elementSize)
            {
                return Buffer.New(device, elementCount* elementSize, elementSize, StructuredCounterBufferFlags, PixelFormat.None);
            }

            /// <summary>
            ///   Creates a new <strong>Structured Counter Buffer</strong> of a given size.
            /// </summary>
            /// <typeparam name="T">Type of the data stored in the Buffer.</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="elementCount">The number of elements in the Buffer.</param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, int elementCount) where T : unmanaged
            {
                return Buffer.New<T>(device, elementCount, StructuredCounterBufferFlags);
            }

            /// <summary>
            ///   Creates a new <strong>Structured Counter Buffer</strong> with initial data.
            /// </summary>
            /// <typeparam name="T">Type of the StructuredCounter buffer to get the sizeof from</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="data">The data to initialize the Structured Counter Buffer.</param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer New<T>(GraphicsDevice device, T[] data) where T : unmanaged
            {
                return Buffer.New(device, (ReadOnlySpan<T>) data, StructuredCounterBufferFlags);
            }

            /// <summary>
            ///   Creates a new <strong>Structured Counter Buffer</strong> with initial data.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="data">The data to initialize the Structured Counter Buffer.</param>
            /// <param name="elementSize">The size in bytes of each element (the structure).</param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            public static Buffer New(GraphicsDevice device, ReadOnlySpan<byte> data, int elementSize)
            {
                return Buffer.New(device, data, elementSize, StructuredCounterBufferFlags);
            }

            /// <summary>
            ///   Creates a new <strong>Structured Counter Buffer</strong> with initial data.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="dataPointer">The data pointer to the data to initialize the Structured Counter Buffer.</param>
            /// <param name="elementSize">The size in bytes of each element (the structure).</param>
            /// <returns>A new instance of <see cref="Buffer"/>.</returns>
            [Obsolete("This method is obsolete. Use the span-based methods instead")]
            public static Buffer New(GraphicsDevice device, DataPointer dataPointer, int elementSize)
            {
                return Buffer.New(device, dataPointer, elementSize, StructuredCounterBufferFlags, PixelFormat.None);
            }
        }
    }
}
