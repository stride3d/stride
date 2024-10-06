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

[ComponentCategory("Bepu")]
public class BodyComponent : CollidableComponent
{
    private bool _kinematic = false;
    private bool _gravity = true;
    private ContinuousDetection _continuous = ContinuousDetection.Discrete;
    private float _sleepThreshold = 0.01f;
    private byte _minimumTimestepCountUnderThreshold = 32;
    private InterpolationMode _interpolationMode = InterpolationMode.None;
    private BodyInertia _nativeIntertia;

    /// <summary> Can be null when it isn't part of a simulation yet/anymore </summary>
    [DataMemberIgnore]
    internal BodyReference? BodyReference { get; private set; }

    [DataMemberIgnore]
    internal BodyComponent? Parent;

    [DataMemberIgnore]
    internal NRigidPose PreviousPose, CurrentPose; //Sets by AfterSimulationUpdate()

    /// <summary>
    /// Constraints that currently references this body, may not be attached if they are not in a valid state
    /// </summary>
    [DataMemberIgnore]
    internal List<ConstraintComponentBase>? BoundConstraints;

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
                description.LocalInertia = Kinematic ? new BodyInertia() : _nativeIntertia;
                bRef.ApplyDescription(description);
            }
        }
    }

    /// <summary> Whether gravity should affect the simulation's <see cref="BepuSimulation.PoseGravity"/> </summary>
    /// <remarks> Gravity is always active if <see cref="BepuSimulation.UsePerBodyAttributes"/> is false </remarks>
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
    /// Shortcut to <see cref="ContinuousDetection"/>.<see cref="ContinuousDetection.Mode"/>
    /// </summary>
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
    /// Teleports this object to a new world position.
    /// </summary>
    /// <remarks>
    /// Using this property to move objects around is not recommended,
    /// as it disregards any collider that may overlap with the body at this new position,
    /// you should make sure the area is clear to ensure this object does not become stuck in the scenery.
    /// </remarks>
    [DataMemberIgnore]
    public Vector3 Position
    {
        get => BodyReference?.Pose.Position.ToStride() ?? default;
        set
        {
            if (BodyReference is { } bodyRef)
                bodyRef.Pose.Position = PreviousPose.Position = value.ToNumeric();

            Quaternion dummy = default;
            WorldToLocal(ref value, ref dummy);
            Entity.Transform.Position = value;

        }
    }

    /// <summary>
    /// Teleports this object to a new world rotation.
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
        set
        {
            if (BodyReference is { } bodyRef)
                bodyRef.Pose.Orientation = PreviousPose.Orientation = value.ToNumeric();

            Vector3 dummy = default;
            WorldToLocal(ref dummy, ref value);
            Entity.Transform.Rotation = value;
        }
    }

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

    public void ApplyImpulse(Vector3 impulse, Vector3 impulseOffset)
    {
        BodyReference?.ApplyImpulse(impulse.ToNumeric(), impulseOffset.ToNumeric());
    }

    public void ApplyAngularImpulse(Vector3 impulse)
    {
        BodyReference?.ApplyAngularImpulse(impulse.ToNumeric());
    }

    public void ApplyLinearImpulse(Vector3 impulse)
    {
        BodyReference?.ApplyLinearImpulse(impulse.ToNumeric());
    }

    protected override ref MaterialProperties MaterialProperties => ref Simulation!.CollidableMaterials[BodyReference!.Value];
    protected internal override NRigidPose? Pose => BodyReference?.Pose;

    /// <inheritdoc cref="CollidableComponent.AttachInner"/>
    protected override void AttachInner(NRigidPose pose, BodyInertia shapeInertia, TypedIndex shapeIndex)
    {
        Debug.Assert(Simulation is not null);

        _nativeIntertia = shapeInertia;
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
            foreach (var constraint in BoundConstraints)
                constraint.TryReattachConstraint();
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

    protected override void RegisterContactHandler()
    {
        if (ContactEventHandler is not null && Simulation is not null && BodyReference is { } bRef)
            Simulation.ContactEvents.Register(bRef.Handle, ContactEventHandler);
    }

    protected override void UnregisterContactHandler()
    {
        if (Simulation is not null && BodyReference is { } bRef)
            Simulation.ContactEvents.Unregister(bRef.Handle);
    }

    protected override bool IsContactHandlerRegistered()
    {
        if (Simulation is not null && BodyReference is { } bRef)
            return Simulation.ContactEvents.IsListener(bRef.Handle);
        return false;
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
