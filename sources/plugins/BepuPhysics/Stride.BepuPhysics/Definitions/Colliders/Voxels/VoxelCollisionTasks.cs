// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.CollisionDetection.CollisionTasks;
using BepuPhysics.CollisionDetection.SweepTasks;

namespace Stride.BepuPhysics.Definitions.Colliders.Voxels;

/// <summary>
/// The Bepu shape type ids of the three voxel shapes over one density source, handed out on first use.
/// </summary>
internal static class VoxelShapeTypeIds<TSource> where TSource : unmanaged, IVoxelDensitySource
{
    public static readonly int Box = VoxelCollisionTasks.ReserveShapeTypeIds();
    public static int Sphere => Box + 1;
    public static int Triangle => Box + 2;
}

/// <summary>
/// Registers the collision and sweep tasks of the voxel shapes with a simulation, once per density source.
/// </summary>
internal static class VoxelCollisionTasks
{
    /// <summary>First shape type id handed to voxel shapes, after Bepu's built-in ones.</summary>
    private const int FirstShapeTypeId = 12;

    /// <summary>Shape types per density source: box, sphere and triangle.</summary>
    private const int ShapeTypesPerSource = 3;

    private static int s_nextShapeTypeId = FirstShapeTypeId;

    private static readonly ConditionalWeakTable<Shapes, Registrations> s_simulations = new();

    private sealed class Registrations(Simulation simulation)
    {
        public readonly Simulation Simulation = simulation;
        public readonly HashSet<Type> Sources = [];
    }

    public static int ReserveShapeTypeIds() => Interlocked.Add(ref s_nextShapeTypeId, ShapeTypesPerSource) - ShapeTypesPerSource;

    /// <summary>Lets <see cref="EnsureRegistered{TSource}"/> find the simulation owning a shape set.</summary>
    public static void Track(Simulation simulation) => s_simulations.Add(simulation.Shapes, new Registrations(simulation));

    /// <summary>Registers the tasks of the shapes over <typeparamref name="TSource"/> with the simulation owning <paramref name="shapes"/>, if not done yet.</summary>
    public static void EnsureRegistered<TSource>(Shapes shapes) where TSource : unmanaged, IVoxelDensitySource
    {
        if (!s_simulations.TryGetValue(shapes, out var registrations))
            throw new InvalidOperationException("The voxel collider is attached to shapes of a simulation the voxel shapes do not know about.");
        lock (registrations)
        {
            if (!registrations.Sources.Add(typeof(TSource)))
                return;
            RegisterConvexChildren<VoxelBoxShape<TSource>, Box, BoxWide>(registrations.Simulation);
            RegisterConvexChildren<VoxelSphereShape<TSource>, Sphere, SphereWide>(registrations.Simulation);
            RegisterTriangleChildren<TSource>(registrations.Simulation);
        }
    }

    /// <summary>Box and sphere children: contacts are combined with a <see cref="NonconvexReduction"/>.</summary>
    private static void RegisterConvexChildren<TShape, TChild, TChildWide>(Simulation simulation)
        where TShape : unmanaged, IHomogeneousCompoundShape<TChild, TChildWide>, IVoxelShape
        where TChild : unmanaged, IConvexShape
        where TChildWide : unmanaged, IShapeWide<TChild>
    {
        var collisions = simulation.NarrowPhase.CollisionTaskRegistry;
        collisions.Register(new ConvexCompoundCollisionTask<Sphere, TShape, ConvexCompoundOverlapFinder<Sphere, SphereWide, TShape>, ConvexVoxelContinuations<TShape>, NonconvexReduction>());
        collisions.Register(new ConvexCompoundCollisionTask<Capsule, TShape, ConvexCompoundOverlapFinder<Capsule, CapsuleWide, TShape>, ConvexVoxelContinuations<TShape>, NonconvexReduction>());
        collisions.Register(new ConvexCompoundCollisionTask<Box, TShape, ConvexCompoundOverlapFinder<Box, BoxWide, TShape>, ConvexVoxelContinuations<TShape>, NonconvexReduction>());
        collisions.Register(new ConvexCompoundCollisionTask<Triangle, TShape, ConvexCompoundOverlapFinder<Triangle, TriangleWide, TShape>, ConvexVoxelContinuations<TShape>, NonconvexReduction>());
        collisions.Register(new ConvexCompoundCollisionTask<Cylinder, TShape, ConvexCompoundOverlapFinder<Cylinder, CylinderWide, TShape>, ConvexVoxelContinuations<TShape>, NonconvexReduction>());
        collisions.Register(new ConvexCompoundCollisionTask<ConvexHull, TShape, ConvexCompoundOverlapFinder<ConvexHull, ConvexHullWide, TShape>, ConvexVoxelContinuations<TShape>, NonconvexReduction>());
        collisions.Register(new CompoundPairCollisionTask<Compound, TShape, CompoundPairOverlapFinder<Compound, TShape>, CompoundVoxelContinuations<Compound, TShape>, NonconvexReduction>());
        collisions.Register(new CompoundPairCollisionTask<BigCompound, TShape, CompoundPairOverlapFinder<BigCompound, TShape>, CompoundVoxelContinuations<BigCompound, TShape>, NonconvexReduction>());

        RegisterSweeps<TShape, TChild, TChildWide>(simulation);
    }

    /// <summary>Triangle children go through Bepu's mesh continuations, whose <see cref="MeshReduction"/> smooths contacts across internal edges.</summary>
    private static void RegisterTriangleChildren<TSource>(Simulation simulation) where TSource : unmanaged, IVoxelDensitySource
    {
        var collisions = simulation.NarrowPhase.CollisionTaskRegistry;
        collisions.Register(new ConvexCompoundCollisionTask<Sphere, VoxelTriangleShape<TSource>, ConvexCompoundOverlapFinder<Sphere, SphereWide, VoxelTriangleShape<TSource>>, ConvexMeshContinuations<VoxelTriangleShape<TSource>>, MeshReduction>());
        collisions.Register(new ConvexCompoundCollisionTask<Capsule, VoxelTriangleShape<TSource>, ConvexCompoundOverlapFinder<Capsule, CapsuleWide, VoxelTriangleShape<TSource>>, ConvexMeshContinuations<VoxelTriangleShape<TSource>>, MeshReduction>());
        collisions.Register(new ConvexCompoundCollisionTask<Box, VoxelTriangleShape<TSource>, ConvexCompoundOverlapFinder<Box, BoxWide, VoxelTriangleShape<TSource>>, ConvexMeshContinuations<VoxelTriangleShape<TSource>>, MeshReduction>());
        collisions.Register(new ConvexCompoundCollisionTask<Triangle, VoxelTriangleShape<TSource>, ConvexCompoundOverlapFinder<Triangle, TriangleWide, VoxelTriangleShape<TSource>>, ConvexMeshContinuations<VoxelTriangleShape<TSource>>, MeshReduction>());
        collisions.Register(new ConvexCompoundCollisionTask<Cylinder, VoxelTriangleShape<TSource>, ConvexCompoundOverlapFinder<Cylinder, CylinderWide, VoxelTriangleShape<TSource>>, ConvexMeshContinuations<VoxelTriangleShape<TSource>>, MeshReduction>());
        collisions.Register(new ConvexCompoundCollisionTask<ConvexHull, VoxelTriangleShape<TSource>, ConvexCompoundOverlapFinder<ConvexHull, ConvexHullWide, VoxelTriangleShape<TSource>>, ConvexMeshContinuations<VoxelTriangleShape<TSource>>, MeshReduction>());
        collisions.Register(new CompoundPairCollisionTask<Compound, VoxelTriangleShape<TSource>, CompoundPairOverlapFinder<Compound, VoxelTriangleShape<TSource>>, CompoundMeshContinuations<Compound, VoxelTriangleShape<TSource>>, CompoundMeshReduction>());
        collisions.Register(new CompoundPairCollisionTask<BigCompound, VoxelTriangleShape<TSource>, CompoundPairOverlapFinder<BigCompound, VoxelTriangleShape<TSource>>, CompoundMeshContinuations<BigCompound, VoxelTriangleShape<TSource>>, CompoundMeshReduction>());
        collisions.Register(new CompoundPairCollisionTask<Mesh, VoxelTriangleShape<TSource>, MeshPairOverlapFinder<Mesh, VoxelTriangleShape<TSource>>, MeshPairContinuations<Mesh, VoxelTriangleShape<TSource>>, CompoundMeshReduction>());

        RegisterSweeps<VoxelTriangleShape<TSource>, Triangle, TriangleWide>(simulation);
    }

    private static void RegisterSweeps<TShape, TChild, TChildWide>(Simulation simulation)
        where TShape : unmanaged, IHomogeneousCompoundShape<TChild, TChildWide>, IVoxelShape
        where TChild : unmanaged, IConvexShape
        where TChildWide : unmanaged, IShapeWide<TChild>
    {
        var sweeps = simulation.NarrowPhase.SweepTaskRegistry;
        sweeps.Register(new ConvexHomogeneousCompoundSweepTask<Sphere, SphereWide, TShape, TChild, TChildWide, ConvexCompoundSweepOverlapFinder<Sphere, TShape>>());
        sweeps.Register(new ConvexHomogeneousCompoundSweepTask<Capsule, CapsuleWide, TShape, TChild, TChildWide, ConvexCompoundSweepOverlapFinder<Capsule, TShape>>());
        sweeps.Register(new ConvexHomogeneousCompoundSweepTask<Box, BoxWide, TShape, TChild, TChildWide, ConvexCompoundSweepOverlapFinder<Box, TShape>>());
        sweeps.Register(new ConvexHomogeneousCompoundSweepTask<Triangle, TriangleWide, TShape, TChild, TChildWide, ConvexCompoundSweepOverlapFinder<Triangle, TShape>>());
        sweeps.Register(new ConvexHomogeneousCompoundSweepTask<Cylinder, CylinderWide, TShape, TChild, TChildWide, ConvexCompoundSweepOverlapFinder<Cylinder, TShape>>());
        sweeps.Register(new ConvexHomogeneousCompoundSweepTask<ConvexHull, ConvexHullWide, TShape, TChild, TChildWide, ConvexCompoundSweepOverlapFinder<ConvexHull, TShape>>());
        sweeps.Register(new CompoundHomogeneousCompoundSweepTask<Compound, TShape, TChild, TChildWide, CompoundPairSweepOverlapFinder<Compound, TShape>>());
        sweeps.Register(new CompoundHomogeneousCompoundSweepTask<BigCompound, TShape, TChild, TChildWide, CompoundPairSweepOverlapFinder<BigCompound, TShape>>());
    }
}
