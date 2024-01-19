using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Definitions;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Quaternion = Stride.Core.Mathematics.Quaternion;
using Vector3 = Stride.Core.Mathematics.Vector3;

namespace Stride.BepuPhysics;


[ComponentCategory("Bepu")]
public class StaticColliderComponent : ContainerComponent
{
    /// <summary> Can be null when it isn't part of a simulation yet/anymore </summary>
    internal StaticReference? StaticReference { get; private set; } = null;

    [DataMemberIgnore]
    public Vector3 Position
    {
        get => StaticReference?.Pose.Position.ToStrideVector() ?? default;
        set
        {
            if (StaticReference is {} staticRef)
                staticRef.Pose.Position = value.ToNumericVector();
        }
    }

    [DataMemberIgnore]
    public Quaternion Orientation
    {
        get => StaticReference?.Pose.Orientation.ToStrideQuaternion() ?? default;
        set
        {
            if (StaticReference is {} staticRef)
                staticRef.Pose.Orientation = value.ToNumericQuaternion();
        }
    }

    [DataMemberIgnore]
    public ContinuousDetection ContinuousDetection
    {
        get => StaticReference?.Continuity ?? default;
        set
        {
            if (StaticReference is {} staticRef)
                staticRef.Continuity = value;
        }
    }

    protected override ref MaterialProperties MaterialProperties => ref Simulation!.CollidableMaterials[StaticReference!.Value.Handle];
    protected internal override RigidPose? Pose => StaticReference?.Pose;

    protected override void AttachInner(RigidPose containerPose, BodyInertia shapeInertia, TypedIndex shapeIndex)
    {
        Debug.Assert(Processor is not null);
        Debug.Assert(Simulation is not null);

        var sDescription = new StaticDescription(containerPose, shapeIndex);

        if (StaticReference is { } sRef)
        {
            sRef.ApplyDescription(sDescription);
        }
        else
        {
            var sHandle = Simulation.Simulation.Statics.Add(sDescription);
            StaticReference = Simulation.Simulation.Statics[sHandle];

            while (Simulation.Statics.Count <= sHandle.Value) // There may be more than one add if soft physics inserted a couple of bodies
                Simulation.Statics.Add(null);
            Simulation.Statics[sHandle.Value] = this;

            Simulation.CollidableMaterials.Allocate(sHandle) = new();
        }

        Processor.Statics.Add(this, Unsafe.As<Matrix, Matrix4x4>(ref Entity.Transform.WorldMatrix));
    }

    protected override void DetachInner()
    {
        Debug.Assert(Processor is not null);
        Debug.Assert(Simulation is not null);
        Debug.Assert(StaticReference is not null);

        Simulation.Simulation.Statics.Remove(StaticReference.Value.Handle);
        Simulation.Statics[StaticReference.Value.Handle.Value] = null;
        StaticReference = null;

        Processor.Statics.Remove(this);
    }

    protected override void RegisterContact()
    {
        if (ContactEventHandler is not null && Simulation is not null && StaticReference is { } sRef)
            Simulation.ContactEvents.Register(sRef.Handle, ContactEventHandler);
    }

    protected override void UnregisterContact()
    {
        if (Simulation is not null && StaticReference is { } sRef)
            Simulation.ContactEvents.Unregister(sRef.Handle);
    }

    protected override bool IsRegistered()
    {
        if (Simulation is not null && StaticReference is { } sRef)
            return Simulation.ContactEvents.IsListener(sRef.Handle);
        return false;
    }
}