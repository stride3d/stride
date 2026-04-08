// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   Determines the <strong>fill mode</strong> to use when rendering triangles.
/// </summary>
/// <remarks>
///   This enumeration is part of a Rasterizer State object description (see <see cref="RasterizerStateDescription"/>).
/// </remarks>
[DataContract]
public enum FillMode : int
{
    /// <summary>
    ///   Draw lines connecting the vertices.
    /// </summary>
    Wireframe = 2,

    /// <summary>
    ///   Fill the triangles formed by the vertices.
    /// </summary>
    Solid = 3
}
