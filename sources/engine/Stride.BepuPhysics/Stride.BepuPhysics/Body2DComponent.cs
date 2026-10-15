// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.Core;
using Stride.Engine;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics;

/// <summary>
/// A dynamic body confined to the XY plane, simulated by Bepu's ordinary 3D solver.
/// </summary>
/// <remarks>
/// <para>
/// Planar behavior is enforced in two places: X/Y rotation is locked by zeroing the corresponding
/// inverse-inertia terms when the body attaches, and Z drift is corrected by setting linear Z velocity
/// before each solve.
/// </para>
/// <para>
/// The positional correction is velocity-based (not teleport-based) so contact resolution stays stable.
/// Sleeping bodies are left untouched.
/// </para>
/// <para>
/// For hull colliders, attach-time tuning applies conservative contact settings: it caps
/// <see cref="CollidableComponent.MaximumRecoveryVelocity"/> and
/// <see cref="CollidableComponent.SpringFrequency"/>, and raises
/// <see cref="CollidableComponent.SpringDampingRatio"/> to at least one.
/// Set values after attach if you want stricter behavior.
/// </para>
/// </remarks>
[ComponentCategory("Physics - Bepu 2D")]
public class Body2DComponent : BodyComponent, ISimulationUpdate
{
    /// <summary>Cap on recovery velocity for hull colliders, which are prone to energetic corrections.</summary>
    private const float HullMaximumRecoveryVelocity = 1.5f;

    /// <summary>Minimum contact spring damping for hull colliders, to settle piles rather than bounce them.</summary>
    private const float HullSpringDampingRatio = 1f;

    /// <summary>Cap on contact spring frequency for hull colliders; stiffer springs fight the substep count.</summary>
    private const float HullSpringFrequency = 30f;

    /// <summary>
    /// Ceiling on the plane-restoring speed, in world units per second.
    /// </summary>
    /// <remarks>
    /// This mainly guards extreme cases (for example, a body spawned far from the plane) by preventing
    /// an overly aggressive snap-back velocity.
    /// </remarks>
    private const float MaximumCorrectionSpeed = 1f;

    /// <summary>
    /// Tracks the kinematic state the rotation lock was applied for, so it can be restored when the
    /// body switches back to dynamic and Bepu reinstates the full shape inertia.
    /// </summary>
    private bool _lockedWhileKinematic;

    /// <summary>One millimetre at Stride's default scale.</summary>
    private const float DefaultZTolerance = 0.001f;

    private float _zTolerance = DefaultZTolerance;

    /// <summary>
    /// Gets or sets how far the body may drift off the Z = 0 plane before it is pulled back, in world
    /// units. Defaults to 0.001 (one millimetre at Stride's default scale).
    /// </summary>
    /// <remarks>
    /// Out-of-plane velocity is always cleared; this value only controls when positional correction
    /// starts. Invalid values (non-finite or non-positive) are replaced with the default.
    /// </remarks>
    [Display("Z tolerance", category: CategoryActivity)]
    public float ZTolerance
    {
        get => _zTolerance;
        set => _zTolerance = float.IsFinite(value) && value > 0f ? value : DefaultZTolerance;
    }

    /// <summary>
    /// Initializes a new <see cref="Body2DComponent"/> with interpolation enabled, so rendering stays
    /// smooth when the display refreshes faster than the fixed physics step.
    /// </summary>
    public Body2DComponent() => InterpolationMode = InterpolationMode.Interpolated;

    /// <inheritdoc />
    /// <remarks>
    /// Preserves Z-axis rotation while locking X/Y rotation. Hull colliders also receive softer default
    /// contact tuning.
    /// </remarks>
    protected override void AttachInner(NRigidPose pose, BodyInertia shapeInertia, TypedIndex shapeIndex)
    {
        base.AttachInner(pose, shapeInertia, shapeIndex);

        ApplyRotationLock();

        if (!HasConvexHull(Collider)) return;

        MaximumRecoveryVelocity = MathF.Min(MaximumRecoveryVelocity, HullMaximumRecoveryVelocity);
        SpringDampingRatio = MathF.Max(SpringDampingRatio, HullSpringDampingRatio);
        SpringFrequency = MathF.Min(SpringFrequency, HullSpringFrequency);
    }

    /// <summary>
    /// Confines the body to the plane, before the solver runs for this step.
    /// </summary>
    /// <param name="sim">The simulation stepping this body.</param>
    /// <param name="simTimeStep">The fixed time step, in seconds.</param>
    /// <remarks>
    /// Runs before solve so correction participates in contact resolution. Sleeping bodies are skipped,
    /// but the rotation lock is refreshed first so state stays valid across kinematic changes.
    ///
    /// Z correction uses a bounded velocity target (not teleporting), with a gentle proportional pull
    /// toward the plane. <paramref name="simTimeStep"/> is intentionally unused.
    /// </remarks>
    public virtual void SimulationUpdate(BepuSimulation sim, float simTimeStep)
    {
        if (BodyReference is not { } bodyRef) return;

        // Deliberately ahead of the sleep check. Turning off Kinematic hands the body its full shape
        // inertia back, and if that happened while it slept it would be free to tumble during the
        // first solve after waking - the lock freezes rotation rather than correcting it, so any tilt
        // picked up in that one step would stay for good
        RestoreRotationLockIfKinematicChanged();

        if (!bodyRef.Awake) return;

        // Out-of-plane velocity is never wanted. Removing it even inside the tolerance band is what
        // stops slow drift accumulating until it crosses the threshold
        var zError = bodyRef.Pose.Position.Z;
        var targetVelocityZ = MathF.Abs(zError) > ZTolerance
            ? Math.Clamp(-zError, -MaximumCorrectionSpeed, MaximumCorrectionSpeed)
            : 0f;

        if (bodyRef.Velocity.Linear.Z != targetVelocityZ)
        {
            bodyRef.Velocity.Linear.Z = targetVelocityZ;
        }

        // Rotation about X and Y is already impossible, but a velocity can survive from before the
        // body attached or from a direct assignment
        if (bodyRef.Velocity.Angular.X != 0f || bodyRef.Velocity.Angular.Y != 0f)
        {
            bodyRef.Velocity.Angular.X = 0f;
            bodyRef.Velocity.Angular.Y = 0f;
        }
    }

    /// <summary>
    /// Does nothing. The whole correction happens before the solve, in <see cref="SimulationUpdate"/>.
    /// </summary>
    /// <param name="sim">The simulation that stepped this body.</param>
    /// <param name="simTimeStep">The fixed time step, in seconds.</param>
    public virtual void AfterSimulationUpdate(BepuSimulation sim, float simTimeStep) { }

    /// <summary>
    /// Removes the body's ability to rotate about X and Y, leaving Z free.
    /// </summary>
    private void ApplyRotationLock()
    {
        var inertia = BodyInertia;
        var inverseInertia = inertia.InverseInertiaTensor;

        inverseInertia.XX = 0f;
        inverseInertia.YY = 0f;
        inverseInertia.YX = 0f;
        inverseInertia.ZX = 0f;
        inverseInertia.ZY = 0f; // ZZ is left alone, so the body can still roll in the plane

        inertia.InverseInertiaTensor = inverseInertia;
        BodyInertia = inertia;

        _lockedWhileKinematic = Kinematic;
    }

    /// <summary>
    /// Reapplies the rotation lock after a switch between kinematic and dynamic.
    /// </summary>
    /// <remarks>
    /// Turning <see cref="BodyComponent.Kinematic"/> off restores the body's full shape inertia, which
    /// silently undoes the lock applied at attach time and would let the body tumble out of the plane.
    /// </remarks>
    private void RestoreRotationLockIfKinematicChanged()
    {
        if (_lockedWhileKinematic == Kinematic) return;

        ApplyRotationLock();
    }

    /// <summary>
    /// Determines whether a collider contains at least one <see cref="ConvexHullCollider"/>.
    /// </summary>
    /// <param name="collider">The collidable's collider, which may be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a convex hull is present.</returns>
    /// <remarks>
    /// In this collider model, hull colliders appear as children of <see cref="CompoundCollider"/>.
    /// A single pass over direct children is therefore sufficient.
    /// </remarks>
    private static bool HasConvexHull(ICollider? collider)
    {
        if (collider is not CompoundCollider compound) return false;

        var colliders = compound.Colliders;

        for (var i = 0; i < colliders.Count; i++)
        {
            if (colliders[i] is ConvexHullCollider) return true;
        }

        return false;
    }
}
