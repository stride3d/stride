// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics;

[DataContract(Inherited = true)]
[DefaultEntityComponentProcessor(typeof(CollidableProcessor), ExecutionMode = ExecutionMode.Runtime)]
public abstract class CollidableComponent : EntityComponent
{
    private static uint IdCounter;
    private static uint VersioningCounter;

    private float _springFrequency = 30;
    private float _springDampingRatio = 3;
    private float _frictionCoefficient = 1f;
    private float _maximumRecoveryVelocity = 1000;

    private CollisionLayer _collisionLayer = CollisionLayer.Layer0;
    private CollisionGroup _collisionGroup;

    private ICollider _collider;
    private IContactEventHandler? _trigger;
    private ISimulationSelector _simulationSelector = SceneBasedSimulationSelector.Shared;

    [DataMemberIgnore]
    internal CollidableProcessor? Processor { get; set; }

    internal Action<CollidableComponent>? OnFeaturesUpdated;

    [DataMemberIgnore]
    protected TypedIndex ShapeIndex { get; private set; }

    [DataMemberIgnore]
    internal uint Versioning { get; private set; }

    internal uint InstanceIndex { get; } = Interlocked.Increment(ref IdCounter);

    /// <summary>
    /// The simulation this object belongs to, null when it is not part of a simulation.
    /// </summary>
    [DataMemberIgnore]
    public BepuSimulation? Simulation { get; private set; }

    /// <summary>
    /// The collider definition used by this object.
    /// </summary>
    /// <remarks>
    /// Changing this value will reset some of the internal physics state of this body
    /// </remarks>
    [Display(Expand = ExpandRule.Always)]
    public required ICollider Collider
    {
        get
        {
            return _collider;
        }
        set
        {
            if (value.Component != null && ReferenceEquals(value.Component, this) == false)
            {
                throw new InvalidOperationException($"{value} is already assigned to {value.Component}, it cannot be shared with {this}");
            }

            _collider.Component = null;
            _collider = value;
            _collider.Component = this;
            TryUpdateFeatures();
        }
    }

    /// <summary>
    /// The bounce frequency in hz
    /// </summary>
    /// <remarks>
    /// Must be low enough that the simulation can actually represent it.
    /// If the contact is trying to make a bounce happen at 240hz,
    /// but the integrator timestep is only 60hz,
    /// the unrepresentable motion will get damped out and the body won't bounce as much.
    /// </remarks>
    public float SpringFrequency
    {
        get
        {
            return _springFrequency;
        }
        set
        {
            _springFrequency = value;
            TryUpdateMaterialProperties();
        }
    }

    /// <summary>
    /// The amount of energy/velocity lost when this collidable bounces off
    /// </summary>
    public float SpringDampingRatio
    {
        get
        {
            return _springDampingRatio;
        }
        set
        {
            _springDampingRatio = value;
            TryUpdateMaterialProperties();
        }
    }

    public float FrictionCoefficient
    {
        get => _frictionCoefficient;
        set
        {
            _frictionCoefficient = value;
            TryUpdateMaterialProperties();
        }
    }

    /// <summary>
    /// The maximum speed this object will exit out of the collision when overlapping another collidable
    /// </summary>
    public float MaximumRecoveryVelocity
    {
        get => _maximumRecoveryVelocity;
        set
        {
            _maximumRecoveryVelocity = value;
            TryUpdateMaterialProperties();
        }
    }

    /// <summary>
    /// Controls how this object interacts with other objects, allow or prevent collisions between
    /// it and other groups based on how <see cref="BepuSimulation.CollisionMatrix"/> is set up.
    /// </summary>
    [DefaultValue(CollisionLayer.Layer0)]
    [DataAlias("CollisionMask")]
    public CollisionLayer CollisionLayer
    {
        get => _collisionLayer;
        set
        {
            _collisionLayer = value;
            TryUpdateMaterialProperties();
        }
    }

    /// <inheritdoc cref="Stride.BepuPhysics.Definitions.CollisionGroup"/>
    [DataAlias("FilterByDistance")]
    public CollisionGroup CollisionGroup
    {
        get => _collisionGroup;
        set
        {
            _collisionGroup = value;
            TryUpdateMaterialProperties();
        }
    }

    /// <summary>
    /// Which simulation this object is assigned to
    /// </summary>
    [DefaultValueIsSceneBased]
    public ISimulationSelector SimulationSelector
    {
        get
        {
            return _simulationSelector;
        }
        set
        {
            _simulationSelector = value;
            if (Processor is not null)
                ReAttach(_simulationSelector.Pick(Processor.BepuConfiguration, Entity));
        }
    }

    /// <summary>
    /// Provides the ability to collect and mutate contact data when this object collides with other objects.
    /// </summary>
    public IContactEventHandler? ContactEventHandler
    {
        get
        {
            return _trigger;
        }
        set
        {
            if (IsContactHandlerRegistered())
                UnregisterContactHandler();

            _trigger = value;
            RegisterContactHandler();
            TryUpdateMaterialProperties();
        }
    }

    /// <summary>
    /// The center of mass of this object in local space
    /// </summary>
    /// <remarks>
    /// This property will always return <see cref="Vector3.Zero"/> if this object is not part of a simulation yet.
    /// </remarks>
    [DataMemberIgnore]
    public Vector3 CenterOfMass { get; private set; }

    protected internal abstract CollidableReference? CollidableReference { get; }

    public CollidableComponent()
    {
        _collider = new CompoundCollider();
        _collider.Component = this;
    }

    internal void TryUpdateFeatures()
    {
        #warning Norbo: Some of the callsites for this method may not require a full reconstruction of the body ? Something we should validate
        if (Simulation is not null)
            ReAttach(Simulation);
        else if (Processor is not null) // We may have to fall back to this when 'Collider.TryAttach' failed previously; when this collidable didn't have any collider before
            ReAttach(SimulationSelector.Pick(Processor.BepuConfiguration, Entity));

        OnFeaturesUpdated?.Invoke(this);
    }

    internal void ReAttach(BepuSimulation onSimulation)
    {
        Versioning = Interlocked.Increment(ref VersioningCounter);
        Detach(true);

        Debug.Assert(Processor is not null);

        if (false == Collider.TryAttach(onSimulation.Simulation.Shapes, onSimulation.BufferPool, Processor.ShapeCache, out var index, out var centerOfMass, out var shapeInertia))
        {
            return;
        }

        ShapeIndex = index;
        CenterOfMass = centerOfMass;

        Simulation = onSimulation;

        Entity.Transform.UpdateWorldMatrix();
        Entity.Transform.WorldMatrix.Decompose(out _, out Quaternion collidableWorldRotation, out Vector3 collidableWorldTranslation);
        var pose = new NRigidPose((collidableWorldTranslation + collidableWorldRotation * CenterOfMass).ToNumeric(), collidableWorldRotation.ToNumeric());

        AttachInner(pose, shapeInertia, ShapeIndex);

        if (ContactEventHandler != null && !IsContactHandlerRegistered())
            RegisterContactHandler();

        TryUpdateMaterialProperties();

        Processor?.OnPostAdd?.Invoke(this);
    }

    internal void Detach(bool reAttaching)
    {
        if (Simulation is null)
            return;

        int getHandleValue = GetHandleValue();

        Versioning = Interlocked.Increment(ref VersioningCounter);
        Processor?.OnPreRemove?.Invoke(this);

        CenterOfMass = new();

        if (IsContactHandlerRegistered())
        {
            UnregisterContactHandler();
        }

        if (ShapeIndex.Exists)
        {
            Collider.Detach(Simulation.Simulation.Shapes, Simulation.Simulation.BufferPool, ShapeIndex);
            ShapeIndex = default;
        }

        DetachInner();
        if (reAttaching == false)
        {
            Simulation.TemporaryDetachedLookup = (getHandleValue, this);
            Simulation.ContactEvents.ClearCollisionsOf(this); // Ensure that removing this collidable sends the appropriate contact events to listeners
            Simulation.TemporaryDetachedLookup = (-1, null);
        }

        Simulation = null;
    }

    protected void TryUpdateMaterialProperties()
    {
        if (Simulation is null)
            return;

        ref var mat = ref MaterialProperties;

        mat.SpringSettings = new(SpringFrequency, SpringDampingRatio);
        mat.FrictionCoefficient = FrictionCoefficient;
        mat.MaximumRecoveryVelocity = MaximumRecoveryVelocity;
        mat.IsTrigger = ContactEventHandler != null && ContactEventHandler.NoContactResponse;

        mat.Layer = CollisionLayer;
        mat.CollisionGroup = CollisionGroup;

#warning this is still kind of a mess, what should we do here ?
        mat.Gravity = this is BodyComponent body && body.Gravity;
    }

    protected abstract ref MaterialProperties MaterialProperties { get; }
    protected internal abstract NRigidPose? Pose { get; }

    /// <summary>
    /// Called every time this is added to a simulation
    /// </summary>
    /// <remarks>
    /// May occur when certain larger changes are made to the object, <see cref="Simulation"/> is the one this object is being added to
    /// </remarks>
    protected abstract void AttachInner(NRigidPose pose, BodyInertia shapeInertia, TypedIndex shapeIndex);

    /// <summary>
    /// Called every time this is removed from the simulation
    /// </summary>
    /// <remarks>
    /// May occur right before <see cref="AttachInner"/> when certain larger changes are made to the object, <see cref="Simulation"/> is the one this object was on prior to detaching
    /// </remarks>
    protected abstract void DetachInner();

    protected abstract int GetHandleValue();


    protected void RegisterContactHandler()
    {
        if (ContactEventHandler is not null && Simulation is not null)
            Simulation.ContactEvents.Register(this);
    }

    protected void UnregisterContactHandler()
    {
        if (Simulation is not null)
            Simulation.ContactEvents.Unregister(this);
    }

    protected bool IsContactHandlerRegistered()
    {
        return Simulation is not null && Simulation.ContactEvents.IsRegistered(this);
    }
}
