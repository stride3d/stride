// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    ///   Identifies the intended use of a graphics <see cref="Buffer"/> when rendering.
    /// </summary>
    [Flags]
    [DataContract]
    public enum BufferFlags
    {
        /// <summary>
        ///   No special flags.
        /// </summary>
        None = 0,

        /// <summary>
        ///   The buffer is a constant buffer.
        /// </summary>
        ConstantBuffer = 1,

        /// <summary>
        ///   The buffer is an index buffer.
        /// </summary>
        IndexBuffer = 2,

        /// <summary>
        ///   The buffer is a vertex buffer.
        /// </summary>
        VertexBuffer = 4,

        /// <summary>
        ///   The buffer can be used as a render target.
        /// </summary>
        RenderTarget = 8,

        /// <summary>
        ///   The buffer can be used as a shader resource.
        /// </summary>
        ShaderResource = 16,

        /// <summary>
        ///   The buffer can be used as an unordered access buffer.
        /// </summary>
        UnorderedAccess = 32,

        /// <summary>
        ///   The buffer can be used as a structured buffer.
        /// </summary>
        StructuredBuffer = 64,

        /// <summary>
        ///   The buffer can be used as a structured buffer that supports unordered acccess and append.
        /// </summary>
        StructuredAppendBuffer = UnorderedAccess | StructuredBuffer | 128,

        /// <summary>
        ///   The buffer can be used as a structured buffer that supports unordered acccess and counter.
        /// </summary>
        StructuredCounterBuffer = UnorderedAccess | StructuredBuffer | 256,

        /// <summary>
        ///   The buffer is a raw buffer.
        /// </summary>
        RawBuffer = 512,

        /// <summary>
        ///   The buffer is an indirect arguments buffer.
        /// </summary>
        ArgumentBuffer = 1024,

        /// <summary>
        ///   The buffer is a buffer for the geometry shader stream-output stage.
        /// </summary>
        StreamOutput = 2048
    }
}
