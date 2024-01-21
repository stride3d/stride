using System.Diagnostics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Definitions;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.BepuPhysics;

[ComponentCategory("Bepu")]
public class BodyComponent : ContainerComponent
{
    private bool _kinematic = false;
    private bool _ignoreGlobalGravity = false;
    private ContinuousDetection _continuous = ContinuousDetection.Discrete;
    private float _sleepThreshold = 0.01f;
    private byte _minimumTimestepCountUnderThreshold = 32;
    private InterpolationMode _interpolationMode = InterpolationMode.None;

    /// <summary> Can be null when it isn't part of a simulation yet/anymore </summary>
    [DataMemberIgnore]
    internal BodyReference? BodyReference { get; private set; }

    [DataMemberIgnore]
    internal BodyComponent? Parent;

    [DataMemberIgnore]
    internal RigidPose PreviousPose, CurrentPose; //Sets by AfterSimulationUpdate()

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
                #warning maybe setting bRef.LocalInertia is enough instead of getting and applying description ... ?
                bRef.GetDescription(out var description);
                description.LocalInertia = Kinematic ? new BodyInertia() : _nativeIntertia;
                bRef.ApplyDescription(description);
            }
        }
    }

    /// <summary> Whether to ignore the simulation's <see cref="Configurations.BepuSimulation.PoseGravity"/> </summary>
    /// <remarks> Gravity is always active if <see cref="Configurations.BepuSimulation.UsePerBodyAttributes"/> is false </remarks>
    public bool IgnoreGlobalGravity
    {
        get => _ignoreGlobalGravity;
        set
        {
            if (_ignoreGlobalGravity == value)
                return;

            _ignoreGlobalGravity = value;
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
#warning maybe setting bRef.Activity.SleepThreshold is enough instead of getting and applying description ... ?
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
#warning maybe setting bRef.Activity.MinimumTimestepsUnderThreshold is enough instead of getting and applying description ... ?
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
            if (BodyReference is {} bodyRef)
                bodyRef.Awake = value;
        }
    }

    [DataMemberIgnore]
    public Vector3 LinearVelocity
    {
        get => BodyReference?.Velocity.Linear.ToStrideVector() ?? default;
        set
        {
            if (BodyReference is {} bodyRef)
                bodyRef.Velocity.Linear = value.ToNumericVector();
        }
    }

    [DataMemberIgnore]
    public Vector3 AngularVelocity
    {
        get => BodyReference?.Velocity.Angular.ToStrideVector() ?? default;
        set
        {
            if (BodyReference is {} bodyRef)
                bodyRef.Velocity.Angular = value.ToNumericVector();
        }
    }

    [DataMemberIgnore]
    public Vector3 Position
    {
        get => BodyReference?.Pose.Position.ToStrideVector() ?? default;
        set
        {
            if (BodyReference is {} bodyRef)
                bodyRef.Pose.Position = value.ToNumericVector();
        }
    }

    [DataMemberIgnore]
    public Quaternion Orientation
    {
        get => BodyReference?.Pose.Orientation.ToStrideQuaternion() ?? Quaternion.Identity;
        set
        {
            if (BodyReference is {} bodyRef)
                bodyRef.Pose.Orientation = value.ToNumericQuaternion();
        }
    }

    [DataMemberIgnore]
    public BodyInertia BodyInertia
    {
        get => BodyReference?.LocalInertia ?? default;
        set
        {
            if (BodyReference is {} bodyRef)
                bodyRef.LocalInertia = value;
        }
    }

    [DataMemberIgnore]
    public float SpeculativeMargin
    {
        get => BodyReference?.Collidable.SpeculativeMargin ?? default;
        set
        {
            if (BodyReference is {} bodyRef)
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
            if (BodyReference is {} bodyRef)
                bodyRef.Collidable.Continuity = _continuous;
        }
    }

    public void ApplyImpulse(Vector3 impulse, Vector3 impulseOffset)
    {
        BodyReference?.ApplyImpulse(impulse.ToNumericVector(), impulseOffset.ToNumericVector());
    }

    public void ApplyAngularImpulse(Vector3 impulse)
    {
        BodyReference?.ApplyAngularImpulse(impulse.ToNumericVector());
    }

    public void ApplyLinearImpulse(Vector3 impulse)
    {
        BodyReference?.ApplyLinearImpulse(impulse.ToNumericVector());
    }

    protected override ref MaterialProperties MaterialProperties => ref Simulation!.CollidableMaterials[BodyReference!.Value];
    protected internal override RigidPose? Pose => BodyReference?.Pose;

    private BodyInertia _nativeIntertia;
    protected override void AttachInner(RigidPose containerPose, BodyInertia shapeInertia, TypedIndex shapeIndex)
    {
        Debug.Assert(Simulation is not null);

        _nativeIntertia = shapeInertia;
        if (Kinematic)
            shapeInertia = new BodyInertia();

        var bDescription = BodyDescription.CreateDynamic(containerPose, shapeInertia, shapeIndex, new(SleepThreshold, MinimumTimestepCountUnderThreshold));

        if (BodyReference is { } bRef)
        {
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
            Simulation.UnregisterInterpolated(this);

        Parent = FindParentContainer(this, Simulation);
        SetParentForChildren(this, Entity.Transform, Simulation);
    }

    protected override void DetachInner()
    {
        Debug.Assert(BodyReference is not null);
        Debug.Assert(Simulation is not null);

        Simulation.Simulation.Bodies.Remove(BodyReference.Value.Handle);
        Simulation.Bodies[BodyReference.Value.Handle.Value] = null;
        if (InterpolationMode != InterpolationMode.None)
            Simulation.UnregisterInterpolated(this);

        if (Parent is { } parent) // Make sure that children we leave behind can count on their grand-parent to take care of them
            SetParentForChildren(parent, Entity.Transform, Simulation);
        Parent = null;

        BodyReference = null;
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

    private static void SetParentForChildren(BodyComponent parent, TransformComponent root, BepuSimulation simulation)
    {
        foreach (var child in root.Children)
        {
            if (child.Entity.Get<BodyComponent>() is { } container && ReferenceEquals(simulation, container.Simulation))
                container.Parent = parent;
            else
                SetParentForChildren(parent, child, simulation);
        }
    }

    private static BodyComponent? FindParentContainer(BodyComponent component, BepuSimulation simulation)
    {
        for (var parent = component.Entity.Transform.Parent; parent != null; parent = parent.Parent)
        {
            if (parent.Entity.Get<BodyComponent>() is { } container && ReferenceEquals(simulation, container.Simulation))
                return container;
        }

        return null;
    }
}