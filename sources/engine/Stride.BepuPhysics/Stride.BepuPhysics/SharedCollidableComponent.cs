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
using Stride.Physics;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics;

[DataContract(Inherited = true)]
[ComponentCategory("Physics - Bepu")]
public class SharedCollidableComponent : EntityComponentWithTryUpdateFeature
{
    private ICollider _collider;
    private ISimulationSelector _simulationSelector = SceneBasedSimulationSelector.Shared;

    [DataMemberIgnore]
    internal CollidableProcessor? Processor { get; set; }

    [DataMemberIgnore]
    internal List<SharedColliderRef> Childs { get; private set; } = new();
    [DataMemberIgnore]
    internal TypedIndex ShapeIndex { get; private set; }
    [DataMemberIgnore]
    internal Vector3 CenterOfMass { get; private set; }
    [DataMemberIgnore]
    internal BodyInertia ShapeInertia { get; private set; }

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

            TryDeleteFeatures();
            _collider.Component = null;
            _collider = value;
            _collider.Component = this;
            TryUpdateFeatures();
        }
    }

    [DefaultValueIsSceneBased]
    public ISimulationSelector SimulationSelector
    {
        get
        {
            return _simulationSelector;
        }
        set
        {
            TryDeleteFeatures();
            _simulationSelector = value;
            TryUpdateFeatures();
        }
    }

    public SharedCollidableComponent()
    {
        _collider = new CompoundCollider();
        _collider.Component = this;
    }

    public override void Start()
    {
        Processor = SceneSystem.SceneInstance.Processors.Get<CollidableProcessor>();
        TryUpdateFeatures();
    }

    internal override void TryUpdateFeatures()
    {
        if (Processor is null)
            return;

        var simulation = _simulationSelector.Pick(Processor.BepuConfiguration, Entity);

        Debug.Assert(simulation is not null);

        TryDeleteFeatures();

        if (false == Collider.TryAttach(simulation.Simulation.Shapes, simulation.BufferPool, Processor.ShapeCache, out var index, out var centerOfMass, out var shapeInertia))
        {
            return;
        }

        ShapeIndex = index;
        CenterOfMass = centerOfMass;
        ShapeInertia = shapeInertia;
        foreach (var item in Childs.ToArray())
        {
            item.GetComponent?.TryUpdateFeatures();
        }
    }
    internal void TryDeleteFeatures()
    {
        if (Processor is null)
            return;

        var simulation = _simulationSelector.Pick(Processor.BepuConfiguration, Entity);

        Debug.Assert(simulation is not null);

        if (ShapeIndex.Exists)
            simulation.Simulation.Shapes.Remove(ShapeIndex);

        ShapeIndex = default;
        CenterOfMass = default;
        ShapeInertia = default;

        foreach (var item in Childs.ToArray())
        {
            item.GetComponent?.TryUpdateFeatures();
        }
    }
}
