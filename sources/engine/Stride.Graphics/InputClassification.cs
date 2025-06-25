// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Specifies whether the data of a Input Element is <strong>per-vertex</strong> or <strong>per-instance</strong>.
/// </summary>
public enum InputClassification
{
    /// <summary>
    ///   The data is per-vertex, meaning it represents a standard vertex attribute.
    /// </summary>
    Vertex,

    /// <summary>
    ///   The data is per-instance, meaning it represents information about a particular geometry instance
    ///   in an <em>instancing</em> scenario.
    /// </summary>
    Instance
}
