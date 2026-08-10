// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.Engine;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics;

/// <summary>
/// A dynamic body confined to the XY plane, simulated by Bepu's ordinary 3D solver.
/// </summary>
/// <remarks>
/// <para>
/// Two things keep a body in the plane. Rotation is locked at the source, by zeroing the X and Y
/// terms of the inverse inertia tensor once when the body attaches, which makes the solver unable to
/// rotate about those axes - there is nothing to correct afterwards. Position is kept on Z = 0 by a
/// small velocity correction applied before each solve, so the solver resolves it together with
/// every contact rather than having its result overwritten afterwards.
/// </para>
/// <para>
/// Correcting by velocity rather than by teleporting is deliberate. Moving a body's position between
/// steps injects energy, which shows up as jitter and creeping instability in dense piles, and it
/// discards the contact information the solver just computed.
/// </para>
/// <para>
/// Bodies are left free to fall asleep, which is what makes large 2D scenes cheap. Nothing here
/// wakes a resting body: the correction is skipped entirely while asleep, and once settled at
/// Z = 0 with no out-of-plane velocity there is nothing left to write.
/// </para>
/// <para>
/// This mirrors how the rest of the engine, and other engines, confine a body to a plane. Stride's
/// Bullet integration sets <c>LinearFactor = (1,1,0)</c> and <c>AngularFactor = (0,0,1)</c> for 2D
/// shapes, and Unity, Unreal and Godot all expose the same idea as per-axis freeze flags. Zeroing the
/// inverse inertia is the angular factor, and clearing out-of-plane velocity each step is the linear
/// one. Bepu has no linear factor to set, and the solver can still introduce Z velocity after this
/// runs, which is what the small positional correction cleans up - the others get it for free inside
/// the integrator.
/// </para>
/// <para>
/// Locking an axis prevents change; it does not undo what is already there. A body tilted about X or
/// Y when it attaches keeps that tilt, frozen at that angle, exactly as a frozen rotation behaves in
/// those other engines. Give bodies an identity or Z-only rotation if that is not wanted.
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
    /// Normal drift is measured in millimetres and never comes close to this. It exists for the
    /// pathological cases - a body spawned or teleported far off the plane, or thrown there by a
    /// numerical blow-up - where an unbounded correction would otherwise fling it back fast enough to
    /// tunnel through geometry and destabilise the solver.
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
    /// <para>
    /// Out-of-plane velocity is always removed; this only governs the positional correction. A larger
    /// value settles more readily, a smaller one holds the plane more tightly.
    /// </para>
    /// <para>
    /// Values that are not finite and positive fall back to the default rather than being stored.
    /// Zero or negative would make the correction fire on floating-point noise alone and write a
    /// velocity every step, which can stop bodies sleeping; NaN and infinity would disable it entirely.
    /// </para>
    /// </remarks>
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
    /// Keeps the shape-derived inertia so rolling about Z still works, and zeroes the inverse inertia
    /// terms that would allow yaw and pitch. Hull colliders additionally get milder contact settings,
    /// because they tend to generate energetic corrections in dense piles.
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
    /// <para>
    /// Runs before the solve so the correction is resolved alongside contacts instead of overwriting
    /// their result. Sleeping bodies return before the correction - they cannot move, so there is
    /// nothing to correct, and this is dispatched for every registered body on every step whether it
    /// is awake or not - but the rotation lock is refreshed first, so it is never stale when a body
    /// wakes. The body reference is taken once, because every pose and velocity accessor resolves it
    /// again.
    /// </para>
    /// <para>
    /// The correction sets a velocity equal and opposite to the drift, a proportional gain of one per
    /// second, so an error decays with a time constant of roughly a second, and is capped at
    /// <see cref="MaximumCorrectionSpeed"/> so a badly placed body cannot be flung back. It is
    /// deliberately gentle: a stiffer pull would fight contact resolution and reintroduce the jitter
    /// that teleporting causes. <paramref name="simTimeStep"/> is therefore unused - expressing the
    /// correction as a velocity already makes that time constant independent of the step size.
    /// </para>
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
    /// A hull can only ever be a child of a compound, never the collider itself: the property is typed
    /// <see cref="ICollider"/>, which <see cref="ColliderBase"/> - and so <see cref="ConvexHullCollider"/>
    /// - does not implement. Testing <c>collider is ConvexHullCollider</c> here is dead code, and the
    /// compiler says as much with CS0184. Compounds cannot nest either, for the same reason, so a
    /// single pass over the children covers every case.
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
