// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   Indicates the facing direction of triangles that <strong>will be culled (not drawn)</strong>.
/// </summary>
/// <remarks>
///   This enumeration is part of a Rasterizer State object description (see <see cref="RasterizerStateDescription"/>).
/// </remarks>
[DataContract]
public enum CullMode
{
    /// <summary>
    ///   Always draw <strong>all triangles</strong>.
    /// </summary>
    None = 1,

    /// <summary>
    ///   Do not draw triangles that are <strong>front-facing</strong>.
    /// </summary>
    Front = 2,

    /// <summary>
    ///   Do not draw triangles that are <strong>back-facing</strong>.
    /// </summary>
    Back = 3
}
