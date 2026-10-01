// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Definitions.Colliders.Voxels;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics.GeometricPrimitives;
using NRigidPose = BepuPhysics.RigidPose;
using NVector3 = System.Numerics.Vector3;

namespace Stride.BepuPhysics.Definitions.Colliders;

/// <summary>
/// Collides against a voxel density field, building the shape of each cell from the samples when the narrow phase asks for it.
/// </summary>
/// <typeparam name="TSource">How the samples are stored; see <see cref="IVoxelDensitySource"/>.</typeparam>
/// <remarks>
/// Derive from it to collide against samples kept in your own layout without copying them; <see cref="VoxelCollider"/> owns a copy of float samples.
/// A <see cref="MeshCollider"/> collides only with the triangle cell shapes, and two voxel colliders never collide with each other.
/// </remarks>
[DataContract(Inherited = true)]
public abstract class VoxelColliderBase<TSource> : ICollider
    where TSource : unmanaged, IVoxelDensitySource
{
    private VoxelCellShape _cellShape = VoxelCellShape.SurfaceNets;
    private float _cellSize = 1f;
    private float _isoLevel = 0.5f;
    private bool _invertWinding;
    private bool _sealBorder = true;
    private float _sphereRadiusScale = 1f;
    private float _mass = 1f;

    private CollidableComponent? _component;
    CollidableComponent? ICollider.Component { get => _component; set => _component = value; }

    /// <summary>Gets or sets the shape each cell presents to the narrow phase.</summary>
    public VoxelCellShape CellShape
    {
        get => _cellShape;
        set => Set(ref _cellShape, value);
    }

    /// <summary>Gets or sets the edge length of one cell, before the entity's scale.</summary>
    public float CellSize
    {
        get => _cellSize;
        set
        {
            value.ValidateGreaterThanZeroFinite(this);
            Set(ref _cellSize, value);
        }
    }

    /// <summary>Gets or sets the density at or above which a sample is solid.</summary>
    /// <remarks>Use the value the renderer meshes the same field with, so the collision surface matches the visible one.</remarks>
    public float IsoLevel
    {
        get => _isoLevel;
        set => Set(ref _isoLevel, value);
    }

    /// <summary>Gets or sets whether triangles collide from the solid side instead of the air side.</summary>
    public bool InvertWinding
    {
        get => _invertWinding;
        set => Set(ref _invertWinding, value);
    }

    /// <summary>Gets or sets whether the samples on the faces of the grid are read as air, closing the volume.</summary>
    /// <remarks>Turn it off for a chunk whose field continues into a neighbouring chunk, or the chunks are walled off from each other.</remarks>
    public bool SealBorder
    {
        get => _sealBorder;
        set => Set(ref _sealBorder, value);
    }

    /// <summary>Gets or sets the radius of the <see cref="VoxelCellShape.Sphere"/> children, as a multiple of half a cell.</summary>
    /// <remarks>At 1 neighbouring spheres touch and small bodies can slip between them; around 1.4 covers the face diagonals.</remarks>
    public float SphereRadiusScale
    {
        get => _sphereRadiusScale;
        set
        {
            value.ValidateGreaterThanZeroFinite(this);
            Set(ref _sphereRadiusScale, value);
        }
    }

    /// <summary>Gets or sets the mass of a body using this collider, spread over the grid's whole box.</summary>
    public float Mass
    {
        get => _mass;
        set
        {
            value.ValidateGreaterThanZeroFinite(this);
            Set(ref _mass, value);
        }
    }

    /// <summary>Rebuilds what is derived from the field outside of the simulation, such as the debug view's wireframe.</summary>
    /// <remarks>The simulation reads the samples as it goes and does not need it; call it too when the grid is resized or replaced.</remarks>
    public void NotifyFieldChanged() => _component?.TryUpdateFeatures();

    /// <summary>Gets the samples as they stand.</summary>
    /// <param name="source">The source, read on the physics threads until the next attach.</param>
    /// <returns>False when there is no field, which leaves the collider detached.</returns>
    protected abstract bool TryGetSource(out TSource source);

    public int Transforms => 1;

    public void GetLocalTransforms(CollidableComponent collidable, Span<ShapeTransform> transforms)
    {
        transforms[0].PositionLocal = Vector3.Zero;
        transforms[0].RotationLocal = Quaternion.Identity;
        transforms[0].Scale = MeshCollider.ComputeMeshScale(collidable);
    }

    bool ICollider.TryAttach(Shapes shapes, BufferPool pool, ShapeCacheSystem shapeCache, bool shouldCalculateInertia, out TypedIndex index, out Vector3 centerOfMass, out BodyInertia inertia)
    {
        index = default;
        centerOfMass = default;
        inertia = default;
        if (_component is null || !TryBuildGrid(MeshCollider.ComputeMeshScale(_component).ToNumeric(), out var grid))
            return false;

        VoxelCollisionTasks.EnsureRegistered<TSource>(shapes);
        // The grid is centred on its box, which is where the mass is spread.
        var extent = grid.Extent;
        grid.Origin = -extent * 0.5f;
        index = _cellShape switch
        {
            VoxelCellShape.Box => shapes.Add(new VoxelBoxShape<TSource> { GridData = grid }),
            VoxelCellShape.Sphere => shapes.Add(new VoxelSphereShape<TSource> { GridData = grid }),
            _ => shapes.Add(new VoxelTriangleShape<TSource> { GridData = grid, SurfaceNets = _cellShape == VoxelCellShape.SurfaceNets }),
        };
        centerOfMass = (extent * 0.5f).ToStride();
        if (shouldCalculateInertia)
            inertia = new Box(extent.X, extent.Y, extent.Z).ComputeInertia(_mass);
        return true;
    }

    void ICollider.Detach(Shapes shapes, BufferPool pool, TypedIndex index) => shapes.Remove(index);

    void ICollider.RayTest<TRayHitHandler>(Shapes shapes, TypedIndex shapeIndex, in NRigidPose pose, in RayData ray, ref float maximumT, ref TRayHitHandler hitHandler, BufferPool pool)
    {
        var type = shapeIndex.Type;
        if (type == VoxelShapeTypeIds<TSource>.Box)
            shapes.GetShape<VoxelBoxShape<TSource>>(shapeIndex.Index).RayTest(pose, ray, ref maximumT, pool, ref hitHandler);
        else if (type == VoxelShapeTypeIds<TSource>.Sphere)
            shapes.GetShape<VoxelSphereShape<TSource>>(shapeIndex.Index).RayTest(pose, ray, ref maximumT, pool, ref hitHandler);
        else if (type == VoxelShapeTypeIds<TSource>.Triangle)
            shapes.GetShape<VoxelTriangleShape<TSource>>(shapeIndex.Index).RayTest(pose, ray, ref maximumT, pool, ref hitHandler);
    }

    /// <remarks>Walks the whole field; the debug view, the navigation mesh builder and the editor gizmo call it.</remarks>
    void ICollider.AppendModel(List<BasicMeshBuffers> buffer, ShapeCacheSystem shapeCache, out object? cacheOut)
    {
        cacheOut = null;
        // Unscaled, from the grid's corner: GetLocalTransforms carries the scale.
        if (!TryBuildGrid(NVector3.One, out var grid))
            return;

        var vertices = new List<VertexPosition3>();
        var indices = new List<int>();
        if (_cellShape == VoxelCellShape.Box)
            AppendCellSolids(grid, shapeCache._boxShapeData, vertices, indices, grid.CellSize);
        else if (_cellShape == VoxelCellShape.Sphere)
            AppendCellSolids(grid, s_cellSphere, vertices, indices, new NVector3(grid.SphereRadius * 2f));
        else
            AppendSurface(new VoxelTriangleShape<TSource> { GridData = grid, SurfaceNets = _cellShape == VoxelCellShape.SurfaceNets }, vertices, indices);
        buffer.Add(new BasicMeshBuffers { Vertices = vertices.ToArray(), Indices = indices.ToArray() });
    }

    private void Set<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        _component?.TryUpdateFeatures();
    }

    private bool TryBuildGrid(NVector3 scale, out VoxelGridData<TSource> grid)
    {
        grid = default;
        if (!TryGetSource(out var source))
            return false;
        var cellSize = _cellSize * scale;
        grid = new VoxelGridData<TSource>
        {
            Source = source,
            CellSize = cellSize,
            IsoLevel = _isoLevel,
            InvertWinding = _invertWinding,
            SealBorder = _sealBorder,
            SphereRadius = MathF.Min(cellSize.X, MathF.Min(cellSize.Y, cellSize.Z)) * 0.5f * _sphereRadiusScale,
        };
        return true;
    }

    /// <summary>Tessellation of the cell spheres, coarser than a sphere collider's so hundreds of them stay readable.</summary>
    private const int CellSphereTessellation = 4;

    private static readonly BasicMeshBuffers s_cellSphere = CreateCellSphere();

    private static BasicMeshBuffers CreateCellSphere()
    {
        var sphere = GeometricPrimitive.Sphere.New(1f, CellSphereTessellation);
        return new BasicMeshBuffers { Vertices = sphere.Vertices.Select(v => new VertexPosition3(v.Position)).ToArray(), Indices = sphere.Indices };
    }

    /// <summary>A unit box or sphere per solid cell with an air neighbour.</summary>
    private static void AppendCellSolids(in VoxelGridData<TSource> grid, BasicMeshBuffers unitShape, List<VertexPosition3> vertices, List<int> indices, NVector3 scale)
    {
        for (int x = 0; x < grid.CellsX; ++x)
        {
            for (int y = 0; y < grid.CellsY; ++y)
            {
                for (int z = 0; z < grid.CellsZ; ++z)
                {
                    if (!grid.CellIsSolid(x, y, z) || !HasAirNeighbour(grid, x, y, z))
                        continue;
                    var centre = grid.CellCentre(x, y, z);
                    var first = vertices.Count;
                    foreach (var vertex in unitShape.Vertices)
                        vertices.Add(new VertexPosition3((centre + vertex.Position.ToNumeric() * scale).ToStride()));
                    foreach (var i in unitShape.Indices)
                        indices.Add(first + i);
                }
            }
        }
    }

    private static bool HasAirNeighbour(in VoxelGridData<TSource> grid, int x, int y, int z)
        => !grid.CellIsSolid(x - 1, y, z) || !grid.CellIsSolid(x + 1, y, z)
        || !grid.CellIsSolid(x, y - 1, z) || !grid.CellIsSolid(x, y + 1, z)
        || !grid.CellIsSolid(x, y, z - 1) || !grid.CellIsSolid(x, y, z + 1);

    /// <summary>The triangles the narrow phase would build, one vertex per corner.</summary>
    private static void AppendSurface(in VoxelTriangleShape<TSource> shape, List<VertexPosition3> vertices, List<int> indices)
    {
        var grid = shape.GridData;
        for (int x = 0; x < grid.CellsX; ++x)
        {
            for (int y = 0; y < grid.CellsY; ++y)
            {
                for (int z = 0; z < grid.CellsZ; ++z)
                {
                    for (int slot = 0; slot < VoxelTriangleShape<TSource>.SlotsPerCell; ++slot)
                    {
                        if (!shape.TryGetTriangle(x, y, z, slot, out var triangle))
                            continue;
                        indices.Add(vertices.Count);
                        indices.Add(vertices.Count + 1);
                        indices.Add(vertices.Count + 2);
                        vertices.Add(new VertexPosition3(triangle.A.ToStride()));
                        vertices.Add(new VertexPosition3(triangle.B.ToStride()));
                        vertices.Add(new VertexPosition3(triangle.C.ToStride()));
                    }
                }
            }
        }
    }
}
