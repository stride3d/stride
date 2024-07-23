// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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
    private static uint VersioningCounter;

    private int _simulationIndex = 0;
    private float _springFrequency = 30;
    private float _springDampingRatio = 3;
    private float _frictionCoefficient = 1f;
    private float _maximumRecoveryVelocity = 1000;

    private CollisionMask _collisionMask = CollisionMask.Layer0;
    private FilterByDistance _filterByDistance;

    private ICollider _collider;
    private IContactEventHandler? _trigger;

    [DataMemberIgnore]
    public BepuSimulation? Simulation { get; private set; }

    [DataMemberIgnore]
    internal CollidableProcessor? Processor { get; set; }

    internal Action<CollidableComponent>? OnFeaturesUpdated;

    [DataMemberIgnore]
    protected TypedIndex ShapeIndex { get; private set; }

    [DataMemberIgnore]
    internal uint Versioning { get; private set; }

    /// <remarks>
    /// Changing this value will reset some of the internal physics state of this body
    /// </remarks>
    [MemberRequired, Display(Expand = ExpandRule.Always)]
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

    public int SimulationIndex
    {
        get
        {
            return _simulationIndex;
        }
        set
        {
            _simulationIndex = value;
            if (Processor is not null)
                ReAttach(Processor.BepuConfiguration.BepuSimulations[_simulationIndex]);
            else
                Detach();
        }
    }

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

    public float MaximumRecoveryVelocity
    {
        get => _maximumRecoveryVelocity;
        set
        {
            _maximumRecoveryVelocity = value;
            TryUpdateMaterialProperties();
        }
    }

    public CollisionMask CollisionMask
    {
        get => _collisionMask;
        set
        {
            _collisionMask = value;
            TryUpdateMaterialProperties();
        }
    }

    public FilterByDistance FilterByDistance
    {
        get => _filterByDistance;
        set
        {
            _filterByDistance = value;
            TryUpdateMaterialProperties();
        }
    }

    [DataMemberIgnore]
    public Vector3 CenterOfMass { get; private set; }

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
            ReAttach(Processor.BepuConfiguration.BepuSimulations[SimulationIndex]);

        OnFeaturesUpdated?.Invoke(this);
    }

    internal void ReAttach(BepuSimulation onSimulation)
    {
        Versioning = Interlocked.Increment(ref VersioningCounter);
        Detach();

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

    internal void Detach()
    {
        if (Simulation is null)
            return;

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

        mat.ColliderCollisionMask = CollisionMask;
        mat.FilterByDistance = FilterByDistance;

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
    protected abstract void RegisterContactHandler();
    protected abstract void UnregisterContactHandler();
    protected abstract bool IsContactHandlerRegistered();
}
