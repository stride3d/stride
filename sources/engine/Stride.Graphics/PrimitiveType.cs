// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics;

[DataContract]
public enum PrimitiveType
{
    /// <summary>
    /// Defines how vertex data is ordered.
    /// </summary>
        /// <summary>
        /// No documentation.
        /// </summary>
    Undefined = 0,

        /// <summary>
        /// No documentation.
        /// </summary>
    PointList = 1,

        /// <summary>
        /// The data is ordered as a sequence of line segments; each line segment is described by two new vertices. The count may be any positive integer.
        /// </summary>
    LineList = 2,

        /// <summary>
        /// The data is ordered as a sequence of line segments; each line segment is described by one new vertex and the last vertex from the previous line seqment. The count may be any positive integer.
        /// </summary>
    LineStrip = 3,

        /// <summary>
        /// The data is ordered as a sequence of triangles; each triangle is described by three new vertices. Back-face culling is affected by the current winding-order render state.
        /// </summary>
    TriangleList = 4,

        /// <summary>
        /// The data is ordered as a sequence of triangles; each triangle is described by two new vertices and one vertex from the previous triangle. The back-face culling flag is flipped automatically on even-numbered
        /// </summary>
    TriangleStrip = 5,

        /// <summary>
        /// No documentation.
        /// </summary>
    LineListWithAdjacency = 10,

        /// <summary>
        /// No documentation.
        /// </summary>
    LineStripWithAdjacency = 11,

        /// <summary>
        /// No documentation.
        /// </summary>
    TriangleListWithAdjacency = 12,

        /// <summary>
        /// No documentation.
        /// </summary>
    TriangleStripWithAdjacency = 13,

    PatchList = 33
}
