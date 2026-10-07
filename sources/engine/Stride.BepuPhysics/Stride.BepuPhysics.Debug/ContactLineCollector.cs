// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics;
using BepuPhysics.Constraints.Contact;
using BepuUtilities;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Debug.Effects;
using Stride.Core.Mathematics;
using NVector3 = System.Numerics.Vector3;

namespace Stride.BepuPhysics.Debug;

/// <summary>
/// Reads the contact constraints of a simulation's solver and turns each penetrating contact into lines.
/// </summary>
/// <remarks> A null color skips that part of the contact. </remarks>
internal struct ContactLineCollector(Simulation simulation, LineRenderObject lines, float size, Color? pointColor, Color? normalColor) : ISolverContactDataExtractor
{
    public readonly void CollectAll()
    {
        var narrowPhase = simulation.NarrowPhase;
        var sets = simulation.Solver.Sets;
        var collector = this;
        for (int setIndex = 0; setIndex < sets.Length; setIndex++)
        {
            ref var set = ref sets[setIndex];
            if (!set.Allocated)
                continue;

            for (int batchIndex = 0; batchIndex < set.Batches.Count; batchIndex++)
            {
                ref var batch = ref set.Batches[batchIndex];
                for (int typeBatchIndex = 0; typeBatchIndex < batch.TypeBatches.Count; typeBatchIndex++)
                {
                    ref var typeBatch = ref batch.TypeBatches[typeBatchIndex];
                    if (!narrowPhase.TryGetContactConstraintAccessor(typeBatch.TypeId, out _))
                        continue;

                    for (int i = 0; i < typeBatch.ConstraintCount; i++)
                        narrowPhase.TryExtractSolverContactData(typeBatch.IndexToHandle[i], ref collector);
                }
            }
        }
    }

    private readonly void AddContact(BodyHandle bodyA, in NVector3 offsetA, in NVector3 normal, float depth)
    {
        // Speculative contacts are not touching yet
        if (depth < 0)
            return;

        // Offsets are relative to body A, and the normal points from B to A
        var point = (simulation.Bodies[bodyA].Pose.Position + offsetA).ToStride();
        if (pointColor is { } p)
        {
            var markerSize = size * 0.15f;
            lines.AddLine(point - new Vector3(markerSize, 0, 0), point + new Vector3(markerSize, 0, 0), p);
            lines.AddLine(point - new Vector3(0, markerSize, 0), point + new Vector3(0, markerSize, 0), p);
            lines.AddLine(point - new Vector3(0, 0, markerSize), point + new Vector3(0, 0, markerSize), p);
        }

        if (normalColor is { } n)
            lines.AddLine(point, point + normal.ToStride() * size, n);
    }

    private readonly void AddConvex<TPrestep>(BodyHandle bodyA, ref TPrestep prestep) where TPrestep : struct, IConvexContactPrestep<TPrestep>
    {
        Vector3Wide.ReadFirst(TPrestep.GetNormal(ref prestep), out var normal);
        for (int i = 0; i < TPrestep.ContactCount; i++)
        {
            ref var contact = ref TPrestep.GetContact(ref prestep, i);
            Vector3Wide.ReadFirst(contact.OffsetA, out var offsetA);
            AddContact(bodyA, offsetA, normal, contact.Depth[0]);
        }
    }

    private readonly void AddNonconvex<TPrestep>(BodyHandle bodyA, ref TPrestep prestep) where TPrestep : struct, INonconvexContactPrestep<TPrestep>
    {
        for (int i = 0; i < TPrestep.ContactCount; i++)
        {
            ref var contact = ref TPrestep.GetContact(ref prestep, i);
            Vector3Wide.ReadFirst(contact.Offset, out var offsetA);
            Vector3Wide.ReadFirst(contact.Normal, out var normal);
            AddContact(bodyA, offsetA, normal, contact.Depth[0]);
        }
    }

    public readonly void ConvexOneBody<TPrestep, TAccumulatedImpulses>(BodyHandle bodyHandle, ref TPrestep prestep, ref TAccumulatedImpulses impulses)
        where TPrestep : struct, IConvexContactPrestep<TPrestep>
        where TAccumulatedImpulses : struct, IConvexContactAccumulatedImpulses<TAccumulatedImpulses>
        => AddConvex(bodyHandle, ref prestep);

    public readonly void ConvexTwoBody<TPrestep, TAccumulatedImpulses>(BodyHandle bodyHandleA, BodyHandle bodyHandleB, ref TPrestep prestep, ref TAccumulatedImpulses impulses)
        where TPrestep : struct, ITwoBodyConvexContactPrestep<TPrestep>
        where TAccumulatedImpulses : struct, IConvexContactAccumulatedImpulses<TAccumulatedImpulses>
        => AddConvex(bodyHandleA, ref prestep);

    public readonly void NonconvexOneBody<TPrestep, TAccumulatedImpulses>(BodyHandle bodyHandle, ref TPrestep prestep, ref TAccumulatedImpulses impulses)
        where TPrestep : struct, INonconvexContactPrestep<TPrestep>
        where TAccumulatedImpulses : struct, INonconvexContactAccumulatedImpulses<TAccumulatedImpulses>
        => AddNonconvex(bodyHandle, ref prestep);

    public readonly void NonconvexTwoBody<TPrestep, TAccumulatedImpulses>(BodyHandle bodyHandleA, BodyHandle bodyHandleB, ref TPrestep prestep, ref TAccumulatedImpulses impulses)
        where TPrestep : struct, ITwoBodyNonconvexContactPrestep<TPrestep>
        where TAccumulatedImpulses : struct, INonconvexContactAccumulatedImpulses<TAccumulatedImpulses>
        => AddNonconvex(bodyHandleA, ref prestep);
}
