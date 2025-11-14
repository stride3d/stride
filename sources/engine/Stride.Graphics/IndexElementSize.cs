// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Specifies the size of indices in an Index Buffer.
/// </summary>
public enum IndexElementSize
{
    /// <summary>
    ///   The indices are 16-bit integers (2 bytes).
    /// </summary>
    Int16 = 2,

    /// <summary>
    ///   The indices are 32-bit integers (4 bytes).
    /// </summary>
    Int32 = 4
}
