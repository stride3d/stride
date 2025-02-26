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
        /// Structured buffer helper methods.
        /// </summary>
        /// <remarks>
        /// Example in HLSL: StructuredBuffer&lt;float4&gt; or RWStructuredBuffer&lt;float4&gt; for structured buffers supporting unordered access.
        /// </remarks>
        public static class Structured
        {
            /// <summary>
            /// Creates a new Structured buffer accessible as a <see cref="Stride.Shaders.EffectParameterClass.ShaderResourceView" /> and optionally as a <see cref="Stride.Shaders.EffectParameterClass.UnorderedAccessView" />.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="count">The number of element in this buffer.</param>
            /// <param name="elementSize">Size of the struct.</param>
            /// <param name="isUnorderedAccess">if set to <c>true</c> this buffer supports unordered access (RW in HLSL).</param>
            /// <returns>A Structured buffer</returns>
            public static Buffer New(GraphicsDevice device, int elementCount, int elementSize, bool unorderedAccess = false)
            {
                var bufferFlags = BufferFlags.StructuredBuffer | BufferFlags.ShaderResource;

                if (unorderedAccess)
                    bufferFlags |= BufferFlags.UnorderedAccess;

                return Buffer.New(device, elementCount * elementSize, elementSize, bufferFlags, PixelFormat.None);
            }

            /// <summary>
            /// Creates a new Structured buffer accessible as a <see cref="Stride.Shaders.EffectParameterClass.ShaderResourceView" /> and optionally as a <see cref="Stride.Shaders.EffectParameterClass.UnorderedAccessView" />.
            /// </summary>
            /// <typeparam name="T">Type of the element in the structured buffer</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="count">The number of element in this buffer.</param>
            /// <param name="isUnorderedAccess">if set to <c>true</c> this buffer supports unordered access (RW in HLSL).</param>
            /// <returns>A Structured buffer</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, int elementCount, bool unorderedAccess = false) where T : unmanaged
            {
                var bufferFlags = BufferFlags.StructuredBuffer | BufferFlags.ShaderResource;

                if (unorderedAccess)
                    bufferFlags |= BufferFlags.UnorderedAccess;

                return Buffer.New<T>(device, elementCount, bufferFlags);
            }

            /// <summary>
            /// Creates a new Structured buffer <see cref="GraphicsResourceUsage.Default" /> usage.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <typeparam name="T">Type of the Structured buffer to get the sizeof from</typeparam>
            /// <param name="value">The value to initialize the Structured buffer.</param>
            /// <param name="isUnorderedAccess">if set to <c>true</c> this buffer supports unordered access (RW in HLSL).</param>
            /// <returns>A Structured buffer</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, T[] data, bool unorderedAccess = false) where T : unmanaged
            {
                var bufferFlags = BufferFlags.StructuredBuffer | BufferFlags.ShaderResource;

                if (unorderedAccess)
                    bufferFlags |= BufferFlags.UnorderedAccess;

                return Buffer.New(device, (ReadOnlySpan<T>) data, bufferFlags);
            }

            /// <summary>
            /// Creates a new Structured buffer <see cref="GraphicsResourceUsage.Default" /> usage.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="value">The value to initialize the Structured buffer.</param>
            /// <param name="elementSize">Size of the element.</param>
            /// <param name="isUnorderedAccess">if set to <c>true</c> this buffer supports unordered access (RW in HLSL).</param>
            /// <returns>A Structured buffer</returns>
            public static Buffer New(GraphicsDevice device, ReadOnlySpan<byte> data, int elementSize, bool unorderedAccess = false)
            {
                var bufferFlags = BufferFlags.StructuredBuffer | BufferFlags.ShaderResource;

                if (unorderedAccess)
                    bufferFlags |= BufferFlags.UnorderedAccess;

                return Buffer.New(device, data, elementSize, bufferFlags);
            }

            /// <summary>
            /// Creates a new Structured buffer <see cref="GraphicsResourceUsage.Default" /> usage.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="value">The value to initialize the Structured buffer.</param>
            /// <param name="elementSize">Size of the element.</param>
            /// <param name="isUnorderedAccess">if set to <c>true</c> this buffer supports unordered access (RW in HLSL).</param>
            /// <returns>A Structured buffer</returns>
            [Obsolete("Use span instead")]
            public static Buffer New(GraphicsDevice device, DataPointer dataPointer, int elementSize, bool unorderedAccess = false)
            {
                var bufferFlags = BufferFlags.StructuredBuffer | BufferFlags.ShaderResource;

                if (unorderedAccess)
                    bufferFlags |= BufferFlags.UnorderedAccess;

                return Buffer.New(device, dataPointer, elementSize, bufferFlags, PixelFormat.None);
            }
        }

        /// <summary>
        /// StructuredAppend buffer helper methods.
        /// </summary>
        /// <remarks>
        /// Example in HLSL: AppendStructuredBuffer&lt;float4&gt; or ConsumeStructuredBuffer&lt;float4&gt;.
        /// </remarks>
        public static class StructuredAppend
        {
            private const BufferFlags StructuredAppendBufferFlags = BufferFlags.StructuredAppendBuffer | BufferFlags.ShaderResource | BufferFlags.UnorderedAccess;

            /// <summary>
            /// Creates a new StructuredAppend buffer accessible as a <see cref="Stride.Shaders.EffectParameterClass.ShaderResourceView" /> and as a <see cref="Stride.Shaders.EffectParameterClass.UnorderedAccessView" />.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="count">The number of element in this buffer.</param>
            /// <param name="elementSize">Size of the struct.</param>
            /// <returns>A StructuredAppend buffer</returns>
            public static Buffer New(GraphicsDevice device, int elementCount, int elementSize)
            {
                return Buffer.New(device, elementCount * elementSize, elementSize, StructuredAppendBufferFlags, PixelFormat.None);
            }

            /// <summary>
            /// Creates a new StructuredAppend buffer accessible as a <see cref="Stride.Shaders.EffectParameterClass.ShaderResourceView" /> and optionally as a <see cref="Stride.Shaders.EffectParameterClass.UnorderedAccessView" />.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <typeparam name="T">Type of the element in the structured buffer</typeparam>
            /// <param name="count">The number of element in this buffer.</param>
            /// <returns>A Structured buffer</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, int elementCount) where T : unmanaged
            {
                return Buffer.New<T>(device, elementCount, StructuredAppendBufferFlags);
            }

            /// <summary>
            /// Creates a new StructuredAppend buffer <see cref="GraphicsResourceUsage.Default" /> usage.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <typeparam name="T">Type of the StructuredAppend buffer to get the sizeof from</typeparam>
            /// <param name="value">The value to initialize the StructuredAppend buffer.</param>
            /// <returns>A StructuredAppend buffer</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, T[] data) where T : unmanaged
            {
                return Buffer.New(device, (ReadOnlySpan<T>) data, StructuredAppendBufferFlags);
            }

            /// <summary>
            /// Creates a new StructuredAppend buffer <see cref="GraphicsResourceUsage.Default" /> usage.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="value">The value to initialize the StructuredAppend buffer.</param>
            /// <param name="elementSize">Size of the element.</param>
            /// <returns>A StructuredAppend buffer</returns>
            public static Buffer New(GraphicsDevice device, ReadOnlySpan<byte> data, int elementSize)
            {
                return Buffer.New(device, data, elementSize, StructuredAppendBufferFlags);
            }

            /// <summary>
            /// Creates a new StructuredAppend buffer <see cref="GraphicsResourceUsage.Default" /> usage.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="value">The value to initialize the StructuredAppend buffer.</param>
            /// <param name="elementSize">Size of the element.</param>
            /// <returns>A StructuredAppend buffer</returns>
            [Obsolete("Use span instead")]
            public static Buffer New(GraphicsDevice device, DataPointer dataPointer, int elementSize)
            {
                return Buffer.New(device, dataPointer, elementSize, StructuredAppendBufferFlags, PixelFormat.None);
            }
        }

        /// <summary>
        /// StructuredCounter buffer helper methods.
        /// </summary>
        /// <remarks>
        /// Example in HLSL: StructuredBuffer&lt;float4&gt; or RWStructuredBuffer&lt;float4&gt; for structured buffers supporting unordered access.
        /// </remarks>
        public static class StructuredCounter
        {
            const BufferFlags StructuredCounterBufferFlags = BufferFlags.StructuredCounterBuffer | BufferFlags.ShaderResource | BufferFlags.UnorderedAccess;

            /// <summary>
            /// Creates a new StructuredCounter buffer accessible as a <see cref="Stride.Shaders.EffectParameterClass.ShaderResourceView" /> and as a <see cref="Stride.Shaders.EffectParameterClass.UnorderedAccessView" />.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="count">The number of element in this buffer.</param>
            /// <param name="elementSize">Size of the struct.</param>
            /// <returns>A StructuredCounter buffer</returns>
            public static Buffer New(GraphicsDevice device, int elementCount, int elementSize)
            {
                return Buffer.New(device, elementCount* elementSize, elementSize, StructuredCounterBufferFlags, PixelFormat.None);
            }

            /// <summary>
            /// Creates a new StructuredCounter buffer accessible as a <see cref="Stride.Shaders.EffectParameterClass.ShaderResourceView" /> and optionally as a <see cref="Stride.Shaders.EffectParameterClass.UnorderedAccessView" />.
            /// </summary>
            /// <typeparam name="T">Type of the element in the structured buffer</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="count">The number of element in this buffer.</param>
            /// <returns>A Structured buffer</returns>
            public static Buffer<T> New<T>(GraphicsDevice device, int elementCount) where T : unmanaged
            {
                return Buffer.New<T>(device, elementCount, StructuredCounterBufferFlags);
            }

            /// <summary>
            /// Creates a new StructuredCounter buffer <see cref="GraphicsResourceUsage.Default" /> usage.
            /// </summary>
            /// <typeparam name="T">Type of the StructuredCounter buffer to get the sizeof from</typeparam>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="value">The value to initialize the StructuredCounter buffer.</param>
            /// <returns>A StructuredCounter buffer</returns>
            public static Buffer New<T>(GraphicsDevice device, T[] data) where T : unmanaged
            {
                return Buffer.New(device, (ReadOnlySpan<T>) data, StructuredCounterBufferFlags);
            }

            /// <summary>
            /// Creates a new StructuredCounter buffer <see cref="GraphicsResourceUsage.Default" /> usage.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="value">The value to initialize the StructuredCounter buffer.</param>
            /// <param name="elementSize">Size of the element.</param>
            /// <returns>A StructuredCounter buffer</returns>
            public static Buffer New(GraphicsDevice device, ReadOnlySpan<byte> data, int elementSize)
            {
                return Buffer.New(device, data, elementSize, StructuredCounterBufferFlags);
            }

            /// <summary>
            /// Creates a new StructuredCounter buffer <see cref="GraphicsResourceUsage.Default" /> usage.
            /// </summary>
            /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
            /// <param name="value">The value to initialize the StructuredCounter buffer.</param>
            /// <param name="elementSize">Size of the element.</param>
            /// <returns>A StructuredCounter buffer</returns>
            [Obsolete("Use span instead")]
            public static Buffer New(GraphicsDevice device, DataPointer dataPointer, int elementSize)
            {
                return Buffer.New(device, dataPointer, elementSize, StructuredCounterBufferFlags, PixelFormat.None);
            }
        }
    }
}
