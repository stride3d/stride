// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.BepuPhysics.Definitions.Colliders;

/// <summary>
/// The shape each cell of a <see cref="VoxelCollider"/> presents to the narrow phase.
/// </summary>
[DataContract]
public enum VoxelCellShape
{
    /// <summary>
    /// A box filling each solid cell: the cheapest test, with a blocky surface up to half a cell away from the iso-surface.
    /// </summary>
    Box,

    /// <summary>
    /// A sphere inscribed in each solid cell: no corners to catch on, with gaps along cell diagonals.
    /// </summary>
    Sphere,

    /// <summary>
    /// The marching cubes triangles of each cell, up to five per cell.
    /// </summary>
    MarchingCubes,

    /// <summary>
    /// The surface nets quads of each cell, two triangles per sign-changing edge: fewer and larger triangles than <see cref="MarchingCubes"/>.
    /// </summary>
    SurfaceNets,
}
