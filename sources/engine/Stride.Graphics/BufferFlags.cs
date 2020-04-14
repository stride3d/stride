// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;

namespace Stride.Graphics
{
    [Flags]
    [DataContract]
    public enum BufferFlags
    {
        /// <summary>
        /// Creates a none buffer.
        /// </summary>
        /// <remarks>
        /// This is equivalent to <see cref="BindFlags.None"/>.
        /// </remarks>
        None = 0,

        /// <summary>
        /// Creates a constant buffer.
        /// </summary>
        ConstantBuffer = 1,

        /// <summary>
        /// Creates an index buffer.
        /// </summary>
        IndexBuffer = 2,

        /// <summary>
        /// Creates a vertex buffer.
        /// </summary>
        VertexBuffer = 4,

        /// <summary>
        /// Creates a render target buffer.
        /// </summary>
        RenderTarget = 8,

        /// <summary>
        /// Creates a buffer usable as a ShaderResourceView.
        /// </summary>
        ShaderResource = 16,

        /// <summary>
        /// Creates an unordered access buffer.
        /// </summary>
        UnorderedAccess = 32,

        /// <summary>
        /// Creates a structured buffer.
        /// </summary>
        StructuredBuffer = 64,

        /// <summary>
        /// Creates a structured buffer that supports unordered acccess and append.
        /// </summary>
        StructuredAppendBuffer = UnorderedAccess | StructuredBuffer | 128,

        /// <summary>
        /// Creates a structured buffer that supports unordered acccess and counter.
        /// </summary>
        StructuredCounterBuffer = UnorderedAccess | StructuredBuffer | 256,

        /// <summary>
        /// Creates a raw buffer.
        /// </summary>
        RawBuffer = 512,

        /// <summary>
        /// Creates an indirect arguments buffer.
        /// </summary>
        ArgumentBuffer = 1024,

        /// <summary>
        /// Creates a buffer for the geometry shader stream-output stage.
        /// </summary>
        StreamOutput = 2048,
    }
}
