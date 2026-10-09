// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics;
using BepuPhysics.Constraints.Contact;
using BepuUtilities;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Debug.Effects;
using Stride.Core.Mathematics;
using NQuaternion = System.Numerics.Quaternion;
using NVector3 = System.Numerics.Vector3;

namespace Stride.BepuPhysics.Debug;

/// <summary>
/// Records the penetrating contacts of a simulation's solver after each collision detection, and turns them into lines.
/// </summary>
/// <remarks>
/// Contacts are recorded in body A's local space while its pose still matches the narrow phase,
/// so they stay on the body once the solver has moved it.
/// </remarks>
internal sealed class ContactRecorder : IDisposable
{
    private readonly Simulation _simulation;
    private readonly List<RecordedContact> _contacts = new();

    public ContactRecorder(Simulation simulation)
    {
        _simulation = simulation;
        _simulation.Timestepper.CollisionsDetected += Record;
    }

    public void Dispose() => _simulation.Timestepper.CollisionsDetected -= Record;

    /// <summary>
    /// Adds the contacts of the last step to <paramref name="lines"/>, a null color skips that part of the contact.
    /// </summary>
    public void AddLines(LineRenderObject lines, float size, Color? pointColor, Color? normalColor)
    {
        var markerSize = size * 0.15f;
        foreach (var contact in _contacts)
        {
            if (!_simulation.Bodies.BodyExists(contact.Body))
                continue;

            var pose = _simulation.Bodies[contact.Body].Pose;
            var point = (pose.Position + NVector3.Transform(contact.LocalOffset, pose.Orientation)).ToStride();
            if (pointColor is { } p)
            {
                lines.AddLine(point - new Vector3(markerSize, 0, 0), point + new Vector3(markerSize, 0, 0), p);
                lines.AddLine(point - new Vector3(0, markerSize, 0), point + new Vector3(0, markerSize, 0), p);
                lines.AddLine(point - new Vector3(0, 0, markerSize), point + new Vector3(0, 0, markerSize), p);
            }

            if (normalColor is { } n)
                lines.AddLine(point, point + NVector3.Transform(contact.LocalNormal, pose.Orientation).ToStride() * size, n);
        }
    }

    private void Record(float dt, IThreadDispatcher? threadDispatcher)
    {
        _contacts.Clear();
        var narrowPhase = _simulation.NarrowPhase;
        var sets = _simulation.Solver.Sets;
        var extractor = new Extractor(_simulation, _contacts);
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
                        narrowPhase.TryExtractSolverContactData(typeBatch.IndexToHandle[i], ref extractor);
                }
            }
        }
    }

    private readonly record struct RecordedContact(BodyHandle Body, NVector3 LocalOffset, NVector3 LocalNormal);

    private readonly struct Extractor(Simulation simulation, List<RecordedContact> contacts) : ISolverContactDataExtractor
    {
        private void AddContact(BodyHandle bodyA, in NVector3 offsetA, in NVector3 normal, float depth)
        {
            // Speculative contacts are not touching yet
            if (depth < 0)
                return;

            // Offsets are relative to body A, and the normal points from B to A
            var toLocal = NQuaternion.Conjugate(simulation.Bodies[bodyA].Pose.Orientation);
            contacts.Add(new RecordedContact(bodyA, NVector3.Transform(offsetA, toLocal), NVector3.Transform(normal, toLocal)));
        }

        private void AddConvex<TPrestep>(BodyHandle bodyA, ref TPrestep prestep) where TPrestep : struct, IConvexContactPrestep<TPrestep>
        {
            Vector3Wide.ReadFirst(TPrestep.GetNormal(ref prestep), out var normal);
            for (int i = 0; i < TPrestep.ContactCount; i++)
            {
                ref var contact = ref TPrestep.GetContact(ref prestep, i);
                Vector3Wide.ReadFirst(contact.OffsetA, out var offsetA);
                AddContact(bodyA, offsetA, normal, contact.Depth[0]);
            }
        }

        private void AddNonconvex<TPrestep>(BodyHandle bodyA, ref TPrestep prestep) where TPrestep : struct, INonconvexContactPrestep<TPrestep>
        {
            for (int i = 0; i < TPrestep.ContactCount; i++)
            {
                ref var contact = ref TPrestep.GetContact(ref prestep, i);
                Vector3Wide.ReadFirst(contact.Offset, out var offsetA);
                Vector3Wide.ReadFirst(contact.Normal, out var normal);
                AddContact(bodyA, offsetA, normal, contact.Depth[0]);
            }
        }

        public void ConvexOneBody<TPrestep, TAccumulatedImpulses>(BodyHandle bodyHandle, ref TPrestep prestep, ref TAccumulatedImpulses impulses)
            where TPrestep : struct, IConvexContactPrestep<TPrestep>
            where TAccumulatedImpulses : struct, IConvexContactAccumulatedImpulses<TAccumulatedImpulses>
            => AddConvex(bodyHandle, ref prestep);

        public void ConvexTwoBody<TPrestep, TAccumulatedImpulses>(BodyHandle bodyHandleA, BodyHandle bodyHandleB, ref TPrestep prestep, ref TAccumulatedImpulses impulses)
            where TPrestep : struct, ITwoBodyConvexContactPrestep<TPrestep>
            where TAccumulatedImpulses : struct, IConvexContactAccumulatedImpulses<TAccumulatedImpulses>
            => AddConvex(bodyHandleA, ref prestep);

        public void NonconvexOneBody<TPrestep, TAccumulatedImpulses>(BodyHandle bodyHandle, ref TPrestep prestep, ref TAccumulatedImpulses impulses)
            where TPrestep : struct, INonconvexContactPrestep<TPrestep>
            where TAccumulatedImpulses : struct, INonconvexContactAccumulatedImpulses<TAccumulatedImpulses>
            => AddNonconvex(bodyHandle, ref prestep);

        public void NonconvexTwoBody<TPrestep, TAccumulatedImpulses>(BodyHandle bodyHandleA, BodyHandle bodyHandleB, ref TPrestep prestep, ref TAccumulatedImpulses impulses)
            where TPrestep : struct, ITwoBodyNonconvexContactPrestep<TPrestep>
            where TAccumulatedImpulses : struct, INonconvexContactAccumulatedImpulses<TAccumulatedImpulses>
            => AddNonconvex(bodyHandleA, ref prestep);
    }
}
