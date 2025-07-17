// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   Values that indicate how the graphics pipeline interprets input vertex data.
/// </summary>
[DataContract]
public enum PrimitiveType
{
    /// <summary>
    ///   The graphics pipeline has not been initialized with a specific primitive topology.
    ///   <br/>
    ///   The Input-Assembler stage will not function properly unless a primitive type is defined.
    /// </summary>
    Undefined = 0,

    /// <summary>
    ///   The data is ordered as a sequence of <strong>points</strong>, with no connectivity between them.
    ///   <br/>
    ///   This is useful for rendering particles or point clouds.
    ///   <br/>
    ///   The count may be any positive integer.
    /// </summary>
    PointList = 1,

    /// <summary>
    ///   The data is ordered as a sequence of <strong>line segments</strong>;
    ///   each line segment is described by two new vertices.
    ///   <br/>
    ///   The count may be any positive even integer.
    /// </summary>
    LineList = 2,

    /// <summary>
    ///   The data is ordered as a sequence of <strong>line segments</strong>;
    ///   each line segment is described by one new vertex and the last vertex from the previous line seqment.
    ///   <br/>
    ///   The count may be any positive integer greater than 2.
    /// </summary>
    LineStrip = 3,

    /// <summary>
    ///   The data is ordered as a sequence of <strong>triangles</strong>;
    ///   each triangle is described by three new vertices. Most commonly used for rendering 3D models.
    ///   <br/>
    ///   The count may be any positive integer multiple of 3.
    ///   <br/>
    ///   Back-face culling is affected by the current winding-order Render State.
    /// </summary>
    TriangleList = 4,

    /// <summary>
    ///   The data is ordered as a sequence of <strong>triangles</strong>;
    ///   each triangle is described by two new vertices and one vertex from the previous triangle.
    ///   <br/>
    ///   It is usually more space-efficient than a triangle list due to reduced vertex redundancy.
    ///   <br/>
    ///   The back-face culling flag is flipped automatically on even-numbered triangles.
    /// </summary>
    TriangleStrip = 5,

    /// <summary>
    ///   The data is ordered as a sequence of <strong>line segments</strong>, the same as <see cref="LineList"/>,
    ///   but <strong>including adjacency information</strong> that can be useful for Geometry Shaders;
    ///   each line segment is described by two new vertices.
    ///   <br/>
    ///   The count may be any positive even integer.
    /// </summary>
    LineListWithAdjacency = 10,

    /// <summary>
    ///   The data is ordered as a sequence of <strong>line segments</strong>, the same as <see cref="LineStrip"/>,
    ///   but <strong>including adjacency information</strong> that can be useful for Geometry Shaders;
    ///   each line segment is described by one new vertex and the last vertex from the previous line seqment.
    ///   <br/>
    ///   The count may be any positive integer greater than 2.
    /// </summary>
    LineStripWithAdjacency = 11,

    /// <summary>
    ///   The data is ordered as a sequence of <strong>triangles</strong>, the same as <see cref="TriangleList"/>,
    ///   but <strong>including adjacency information</strong> that can be useful for Geometry Shaders;
    ///   each triangle is described by three new vertices. Most commonly used for rendering 3D models.
    ///   <br/>
    ///   The count may be any positive integer multiple of 3.
    ///   <br/>
    ///   Back-face culling is affected by the current winding-order Render State.
    /// </summary>
    TriangleListWithAdjacency = 12,

    /// <summary>
    ///   The data is ordered as a sequence of <strong>triangles</strong>, the same as <see cref="TriangleStrip"/>,
    ///   but <strong>including adjacency information</strong> that can be useful for Geometry Shaders;
    ///   each triangle is described by two new vertices and one vertex from the previous triangle.
    ///   <br/>
    ///   It is usually more space-efficient than a triangle list due to reduced vertex redundancy.
    ///   <br/>
    ///   The back-face culling flag is flipped automatically on even-numbered triangles.
    /// </summary>
    TriangleStripWithAdjacency = 13,

    /// <summary>
    ///   The data is ordered as a sequence of <strong>patches</strong>, each one being a group of <strong>control points</strong>.
    ///   This is used for <strong>tessellation</strong>, where the Hull Shader processes each patch and determines tesselation factors,
    ///   optionally outputting more control point data.
    ///   <br/>
    ///   To indicate the number of control points in each patch, you can use the helper method <see cref="PrimitiveTypeExtensions.ControlPointCount"/>.
    ///   <br/>
    ///   These topologies unlock a lot of flexibility for surfaces like terrain, curved surfaces, or even animation rigs.
    /// </summary>
    PatchList = 33
}
