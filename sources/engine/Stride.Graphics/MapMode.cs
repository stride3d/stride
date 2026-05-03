// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Describes how the CPU is accessing a <see cref="GraphicsResource"/> with the <see cref="CommandList.MapSubResource"/> method.
/// </summary>
public enum MapMode
{
    /// <summary>
    ///   The Graphics Resource is <strong>mapped for reading</strong>.
    /// </summary>
    /// <remarks>
    ///   The Graphics Resource must have been created with <see cref="GraphicsResourceUsage.Staging"/>.
    /// </remarks>
    Read = 1,

    /// <summary>
    ///   The Graphics Resource is <strong>mapped for writing</strong>.
    /// </summary>
    /// <remarks>
    ///   The Graphics Resource must have been created with <see cref="GraphicsResourceUsage.Dynamic"/> or <see cref="GraphicsResourceUsage.Staging"/>.
    /// </remarks>
    Write = 2,

    /// <summary>
    ///   The Graphics Resource is <strong>mapped for reading and writing</strong>.
    /// </summary>
    /// <remarks>
    ///   The Graphics Resource must have been created with <see cref="GraphicsResourceUsage.Staging"/>.
    /// </remarks>
    ReadWrite = 3,

    /// <summary>
    ///   The Graphics Resource is <strong>mapped for writing</strong>, making the previous contents of the resource undefined.
    /// </summary>
    /// <remarks>
    ///   The Graphics Resource must have been created with <see cref="GraphicsResourceUsage.Dynamic"/>.
    /// </remarks>
    WriteDiscard = 4,

    /// <summary>
    ///   The Graphics Resource is <strong>mapped for writing</strong>, ensuring the existing contents of the resource cannot be overwritten.
    /// </summary>
    /// <remarks>
    ///   This flag is only valid on Vertex Buffers and Index Buffers.
    /// </remarks>
    WriteNoOverwrite = 5
}
