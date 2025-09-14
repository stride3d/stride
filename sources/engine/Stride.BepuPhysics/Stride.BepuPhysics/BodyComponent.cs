// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics;

[ComponentCategory("Physics - Bepu")]
public class BodyComponent : CollidableComponent
{
    private bool _kinematic = false;
    private bool _gravity = true;
    private ContinuousDetection _continuous = ContinuousDetection.Discrete;
    private float _sleepThreshold = 0.01f;
    private byte _minimumTimestepCountUnderThreshold = 32;
    private InterpolationMode _interpolationMode = InterpolationMode.None;
    private BodyInertia _nativeInertia;

    /// <summary> Can be null when it isn't part of a simulation yet/anymore </summary>
    [DataMemberIgnore]
    internal BodyReference? BodyReference { get; private set; }

    [DataMemberIgnore]
    internal BodyComponent? Parent;

    /// <summary>
    /// Data used in conjunction with <see cref="InterpolationMode"/> set for this object, see <see cref="BepuSimulation.InterpolateTransforms"/>
    /// </summary>
    [DataMemberIgnore]
    internal NRigidPose PreviousPose, CurrentPose;

    /// <summary>
    /// Constraints that currently references this body, may not be attached if they are not in a valid state
    /// </summary>
    [DataMemberIgnore]
    internal List<ConstraintComponentBase>? BoundConstraints;

    /// <summary>
    /// When kinematic is set, the object will not be affected by physics forces like gravity or collisions but will still push away bodies it collides with.
    /// </summary>
    [Display(category: CategoryForces)]
    public bool Kinematic
    {
        get => _kinematic;
        set
        {
            if (_kinematic == value)
                return;

            _kinematic = value;
            if (BodyReference is { } bRef)
            {
#warning Norbo: maybe setting bRef.LocalInertia is enough instead of getting and applying description ... ?
                bRef.GetDescription(out var description);
                description.LocalInertia = Kinematic ? new BodyInertia() : _nativeInertia;
                bRef.ApplyDescription(description);
            }
        }
    }

    /// <summary> Whether gravity should affect the simulation's <see cref="BepuSimulation.PoseGravity"/> </summary>
    /// <remarks> Gravity is always active if <see cref="BepuSimulation.UsePerBodyAttributes"/> is false </remarks>
    [Display(category: CategoryForces)]
    public bool Gravity
    {
        get => _gravity;
        set
        {
            if (_gravity == value)
                return;

            _gravity = value;
            TryUpdateMaterialProperties();
        }
    }

    /// <summary>
    /// Whether the object's path or only its destination is checked for collision when moving, prevents objects from passing through each other at higher speed
    /// </summary>
    /// <remarks>
    /// This property is a shortcut to the <see cref="ContinuousDetection"/>.<see cref="ContinuousDetection.Mode"/> property
    /// </remarks>
    [Display(category: CategoryForces)]
    public ContinuousDetectionMode ContinuousDetectionMode
    {
        get => _continuous.Mode;
        set
        {
            if (_continuous.Mode == value)
                return;

            _continuous = value switch
            {
                ContinuousDetectionMode.Discrete => ContinuousDetection.Discrete,
                ContinuousDetectionMode.Passive => ContinuousDetection.Passive,
                ContinuousDetectionMode.Continuous => ContinuousDetection.Continuous(),
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
        }
    }

    /// <summary>
    /// Controls whether and how the motion of this body is smoothed out between physics update
    /// </summary>
    [Display(category: CategoryForces)]
    public InterpolationMode InterpolationMode
    {
        get => _interpolationMode;
        set
        {
            if (_interpolationMode == InterpolationMode.None && value != InterpolationMode.None)
                Simulation?.RegisterInterpolated(this);
            if (_interpolationMode != InterpolationMode.None && value == InterpolationMode.None)
                Simulation?.UnregisterInterpolated(this);
            _interpolationMode = value;
        }
    }

    /// <summary>
    /// Threshold of squared combined velocity under which the body is allowed to go to sleep.
    /// Setting this to a negative value guarantees the body cannot go to sleep without user action.
    /// </summary>
    [Display(category: CategoryActivity)]
    public float SleepThreshold
    {
        get => _sleepThreshold;
        set
        {
            if (_sleepThreshold == value)
                return;

            _sleepThreshold = value;
            if (BodyReference is { } bRef)
            {
#warning Norbo: maybe setting bRef.Activity.SleepThreshold is enough instead of getting and applying description ... ?
                bRef.GetDescription(out var description);
                description.Activity.SleepThreshold = value;
                bRef.ApplyDescription(description);
            }
        }
    }

    /// <summary>
    /// The number of time steps that the body must be under the sleep threshold before the body becomes a sleeping candidate.
    /// Note that the body is not guaranteed to go to sleep immediately after meeting this minimum.
    /// </summary>
    [Display(category: CategoryActivity)]
    public byte MinimumTimestepCountUnderThreshold
    {
        get => _minimumTimestepCountUnderThreshold;
        set
        {
            if (_minimumTimestepCountUnderThreshold == value)
                return;

            _minimumTimestepCountUnderThreshold = value;
            if (BodyReference is { } bRef)
            {
#warning Norbo: maybe setting bRef.Activity.MinimumTimestepsUnderThreshold is enough instead of getting and applying description ... ?
                bRef.GetDescription(out var description);
                description.Activity.MinimumTimestepCountUnderThreshold = value;
                bRef.ApplyDescription(description);
            }
        }
    }

    /// <summary>
    /// Whether the body is being actively simulated.
    /// Setting this to true will attempt to wake the body; setting it to false will force the body and any constraint-connected bodies asleep.
    /// </summary>
    [DataMemberIgnore]
    public bool Awake
    {
        get => BodyReference?.Awake ?? false;
        set
        {
            if (BodyReference is { } bodyRef)
                bodyRef.Awake = value;
        }
    }

    /// <summary>
    /// The translation velocity in unit per second
    /// </summary>
    [DataMemberIgnore]
    public Vector3 LinearVelocity
    {
        get => BodyReference?.Velocity.Linear.ToStride() ?? default;
        set
        {
            if (BodyReference is { } bodyRef)
                bodyRef.Velocity.Linear = value.ToNumeric();
        }
    }

    /// <summary>
    /// The rotation velocity in unit per second
    /// </summary>
    /// <remarks>
    /// The rotation format is in axis-angle,
    /// meaning that AngularVelocity.Normalized is the axis of rotation,
    /// while AngularVelocity.Length is the amount of rotation around that axis in radians per second
    /// </remarks>
    [DataMemberIgnore]
    public Vector3 AngularVelocity
    {
        get => BodyReference?.Velocity.Angular.ToStride() ?? default;
        set
        {
            if (BodyReference is { } bodyRef)
                bodyRef.Velocity.Angular = value.ToNumeric();
        }
    }

    /// <summary>
    /// The position of this body in the physics scene, setting it will teleport this object to the position provided.
    /// </summary>
    /// <remarks>
    /// Using this property to move objects around is not recommended,
    /// as it disregards any collider that may overlap with the body at this new position,
    /// you should make sure the area is clear to ensure this object does not become stuck in the scenery.<br/><br/>
    /// This value is slightly offset from this entity's Transform <see cref="TransformComponent.Position"/> based on its <see cref="CollidableComponent.CenterOfMass"/>
    /// </remarks>
    [DataMemberIgnore]
    public Vector3 Position
    {
        get => BodyReference?.Pose.Position.ToStride() ?? default;
        [Obsolete($"Setter will be removed in a future version, use {nameof(SetTargetPose)} or {nameof(Teleport)}")]
        set => Teleport(value, Orientation);
    }

    /// <summary>
    /// The rotation of this body in the physics scene, setting it will 'teleport' this object's rotation to the one provided.
    /// </summary>
    /// <remarks>
    /// Using this property to move objects around is not recommended,
    /// as it disregards any collider that may overlap with the body at this new orientation,
    /// you should make sure the area is clear to ensure this object does not become stuck in the scenery.
    /// </remarks>
    [DataMemberIgnore]
    public Quaternion Orientation
    {
        get => BodyReference?.Pose.Orientation.ToStride() ?? Quaternion.Identity;
        [Obsolete($"Setter will be removed in a future version, use {nameof(SetTargetPose)} or {nameof(Teleport)}")]
        set => Teleport(Position, value);
    }

    /// <summary>
    /// The mass and inertia tensor of this body
    /// </summary>
    [DataMemberIgnore]
    public BodyInertia BodyInertia
    {
        get => BodyReference?.LocalInertia ?? default;
        set
        {
            if (BodyReference is { } bodyRef)
                bodyRef.LocalInertia = value;
        }
    }

    /// <summary>
    /// Automatically computed size of the margin around the surface of the shape in which contacts can be generated. These contacts will have negative depth and only contribute if the frame's velocities
    /// would push the shapes of a pair into overlap.
    /// <para>This is automatically set by bounding box prediction each frame, and is bound by the collidable's <see cref="Collidable.MinimumSpeculativeMargin"/> and <see cref="Collidable.MaximumSpeculativeMargin"/> values.
    /// The effective speculative margin for a collision pair can also be modified from <see cref="global::BepuPhysics.CollisionDetection.INarrowPhaseCallbacks"/> callbacks.</para>
    /// <para>This should be positive to avoid jittering.</para>
    /// <para>It can also be used as a form of continuous collision detection, but excessively high values combined with fast motion may result in visible 'ghost collision' artifacts.
    /// For continuous collision detection with less chance of ghost collisions, use <see cref="ContinuousDetectionMode.Continuous"/>.</para>
    /// <para>If using <see cref="ContinuousDetectionMode.Continuous"/>, consider setting <see cref="Collidable.MaximumSpeculativeMargin"/> to a smaller value to help filter ghost collisions.</para>
    /// <para>For more information, see the <see href="https://github.com/bepu/bepuphysics2/blob/master/Documentation/ContinuousCollisionDetection.md">Continuous Collision Detection</see> documentation.</para>
    /// </summary>
    [DataMemberIgnore]
    public float SpeculativeMargin
    {
        get => BodyReference?.Collidable.SpeculativeMargin ?? default;
        set
        {
            if (BodyReference is { } bodyRef)
                bodyRef.Collidable.SpeculativeMargin = value;
        }
    }

    /// <summary>
    /// Determines the continuous collision detection configuration set for that object. Helps prevent fast-moving objects from tunneling through other objects.
    /// </summary>
    [DataMemberIgnore]
    public ContinuousDetection ContinuousDetection
    {
        get => _continuous;
        set
        {
            _continuous = value;
            if (BodyReference is { } bodyRef)
                bodyRef.Collidable.Continuity = _continuous;
        }
    }

    /// <summary>
    /// The constraints targeting this body, some of those may not be <see cref="ConstraintComponentBase.Attached"/>
    /// </summary>
    public IReadOnlyList<ConstraintComponentBase> Constraints => BoundConstraints ?? (IReadOnlyList<ConstraintComponentBase>)Array.Empty<ConstraintComponentBase>();


    protected internal override CollidableReference? CollidableReference
    {
        get
        {
            if (BodyReference is { } bRef)
                return bRef.CollidableReference;
            return null;
        }
    }

    /// <summary>
    /// Applies an explosive force at a specific offset off of this body which will affect both its angular and linear velocity
    /// </summary>
    /// <remarks>
    /// Does not wake the body up
    /// </remarks>
    /// <param name="impulse">Impulse to apply to the velocity</param>
    /// <param name="impulseOffset">World space offset from the center of the body to apply the impulse at</param>
    public void ApplyImpulse(Vector3 impulse, Vector3 impulseOffset)
    {
        BodyReference?.ApplyImpulse(impulse.ToNumeric(), impulseOffset.ToNumeric());
    }

    /// <summary>
    /// Applies an explosive force which will only affect this body's angular velocity
    /// </summary>
    /// <remarks>
    /// Does not wake the body up
    /// </remarks>
    public void ApplyAngularImpulse(Vector3 impulse)
    {
        BodyReference?.ApplyAngularImpulse(impulse.ToNumeric());
    }

    /// <summary>
    /// Applies an explosive force which will only affect this body's linear velocity
    /// </summary>
    /// <remarks>
    /// Does not wake the body up
    /// </remarks>
    public void ApplyLinearImpulse(Vector3 impulse)
    {
        BodyReference?.ApplyLinearImpulse(impulse.ToNumeric());
    }

    /// <summary>
    /// Set the pose this body should try to match on the next physics tick, this will collide with objects on the way
    /// </summary>
    /// <remarks>
    /// Using this function to move objects around is not recommended as it results in unrealistic forces being applied on this body, or unexpected stuttering depending on the input.
    /// Consider using a constraint between this body and whatever it is following, or using the different Impulse methods instead <br/><br/>
    /// <paramref name="targetPosition"/> is slightly offset from this entity's Transform <see cref="TransformComponent.Position"/> based on its <see cref="CollidableComponent.CenterOfMass"/> <br/><br/>
    /// This method sets this body's <see cref="LinearVelocity"/> and <see cref="AngularVelocity"/>, setting these properties after the call would overwrite the result of this method
    /// </remarks>
    public void SetTargetPose(Vector3 targetPosition, Quaternion targetOrientation)
    {
        if (Simulation is null)
            return;

        Awake = true;

        float deltaTime = (float)Simulation.FixedTimeStep.TotalSeconds;

        LinearVelocity = (targetPosition - Position) / deltaTime;
        var quatDelta = Quaternion.Invert(Orientation) * targetOrientation;
        AngularVelocity = new Vector3(quatDelta.X, quatDelta.Y, quatDelta.Z) / deltaTime;
    }

    /// <summary>
    /// Teleport this body into a new pose
    /// </summary>
    /// <remarks>
    /// Using this function to move objects around is not recommended,
    /// as it disregards any collider that may overlap with the body at this new position,
    /// you should make sure the area is clear to ensure this object does not become stuck in the scenery.<br/><br/>
    /// <paramref name="position"/> is slightly offset from this entity's Transform <see cref="TransformComponent.Position"/> based on its <see cref="CollidableComponent.CenterOfMass"/>
    /// </remarks>
    public void Teleport(Vector3 position, Quaternion orientation)
    {
        if (BodyReference is { } bodyRef)
        {
            bodyRef.Pose.Orientation = PreviousPose.Orientation = orientation.ToNumeric();
            bodyRef.Pose.Position = PreviousPose.Position = position.ToNumeric();
            bodyRef.UpdateBounds();
            CurrentPose = bodyRef.Pose; // Update interpolation data as well
        }

        WorldToLocal(ref position, ref orientation);
        Entity.Transform.Position = position;
        Entity.Transform.Rotation = orientation;
    }

    /// <summary>
    /// Teleport this body into a new pose
    /// </summary>
    /// <remarks>
    /// Using this function to move objects around is not recommended,
    /// as it disregards any collider that may overlap with the body at this new position,
    /// you should make sure the area is clear to ensure this object does not become stuck in the scenery.<br/><br/>
    /// <paramref name="position"/> is slightly offset from this entity's Transform <see cref="TransformComponent.Position"/> based on its <see cref="CollidableComponent.CenterOfMass"/>
    /// </remarks>
    [Obsolete($"This method will be removed in the future, use {nameof(Teleport)} instead")]
    public void SetPose(Vector3 position, Quaternion orientation) => Teleport(position, orientation);

    protected override ref MaterialProperties MaterialProperties => ref Simulation!.CollidableMaterials[BodyReference!.Value];
    protected internal override NRigidPose? Pose => BodyReference?.Pose;

    /// <inheritdoc cref="CollidableComponent.AttachInner"/>
    protected override void AttachInner(NRigidPose pose, BodyInertia shapeInertia, TypedIndex shapeIndex)
    {
        Debug.Assert(Simulation is not null);

        _nativeInertia = shapeInertia;
        if (Kinematic)
            shapeInertia = new BodyInertia();

        var bDescription = BodyDescription.CreateDynamic(pose, shapeInertia, shapeIndex, new(SleepThreshold, MinimumTimestepCountUnderThreshold));

        if (BodyReference is { } bRef)
        {
#warning Norbo: maybe setting bRef.Velocity is enough instead of getting and applying description ... ?
            bRef.GetDescription(out var previousDesc);
            bDescription.Velocity = previousDesc.Velocity; //Keep velocity when updating
            bRef.ApplyDescription(bDescription);
        }
        else
        {
            var bHandle = Simulation.Simulation.Bodies.Add(bDescription);
            BodyReference = Simulation.Simulation.Bodies[bHandle];
            BodyReference.Value.Collidable.Continuity = ContinuousDetection;

            while (Simulation.Bodies.Count <= bHandle.Value) // There may be more than one add if soft physics inserted a couple of bodies
                Simulation.Bodies.Add(null);
            Simulation.Bodies[bHandle.Value] = this;

            Simulation.CollidableMaterials.Allocate(bHandle) = new();
        }

        if (InterpolationMode != InterpolationMode.None)
            Simulation.RegisterInterpolated(this);

        Parent = FindParentBody(this, Simulation);
        SetParentForChildren(this, Entity.Transform, Simulation);

        if (BoundConstraints is not null)
        {
            // Reverse for loop as the constraints remove themselves from this list
            for (int i = BoundConstraints.Count - 1; i >= 0; i--)
            {
                BoundConstraints[i].TryReattachConstraint();
            }
        }
    }

    /// <inheritdoc cref="CollidableComponent.DetachInner"/>
    protected override void DetachInner()
    {
        Debug.Assert(BodyReference is not null);
        Debug.Assert(Simulation is not null);

        if (BoundConstraints is not null)
        {
            foreach (var constraint in BoundConstraints)
                constraint.DetachConstraint();
        }

        Simulation.Simulation.Bodies.Remove(BodyReference.Value.Handle);
        Simulation.Bodies[BodyReference.Value.Handle.Value] = null;
        if (InterpolationMode != InterpolationMode.None)
            Simulation.UnregisterInterpolated(this);

        if (Parent is { } parent) // Make sure that children we leave behind can count on their grandparent to take care of them
            SetParentForChildren(parent, Entity.Transform, Simulation);
        Parent = null;

        BodyReference = null;
    }

    protected override int GetHandleValue()
    {
        if (BodyReference is { } bRef)
            return bRef.Handle.Value;

        throw new InvalidOperationException();
    }

    /// <summary>
    /// A special variant taking the center of mass into consideration
    /// </summary>
    internal void WorldToLocal(ref Vector3 worldPos, ref Quaternion worldRot)
    {
        var entityTransform = Entity.Transform;
        if (entityTransform.Parent is { } parent)
        {
            parent.WorldMatrix.Decompose(out Vector3 _, out Quaternion parentEntityRotation, out Vector3 parentEntityPosition);
            var iRotation = Quaternion.Invert(parentEntityRotation);
            worldPos = Vector3.Transform(worldPos - parentEntityPosition, iRotation);
            worldRot *= iRotation;
        }

        worldPos -= Vector3.Transform(CenterOfMass, worldRot);
    }

    private static void SetParentForChildren(BodyComponent parent, TransformComponent root, BepuSimulation simulation)
    {
        foreach (var child in root.Children)
        {
            if (child.Entity.Get<BodyComponent>() is { } body && ReferenceEquals(simulation, body.Simulation))
                body.Parent = parent;
            else
                SetParentForChildren(parent, child, simulation);
        }
    }

    private static BodyComponent? FindParentBody(BodyComponent component, BepuSimulation simulation)
    {
        for (var parent = component.Entity.Transform.Parent; parent != null; parent = parent.Parent)
        {
            if (parent.Entity.Get<BodyComponent>() is { } body && ReferenceEquals(simulation, body.Simulation))
                return body;
        }

        return null;
    }
}
