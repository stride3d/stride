// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Definitions;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Quaternion = Stride.Core.Mathematics.Quaternion;
using Vector3 = Stride.Core.Mathematics.Vector3;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics;


[ComponentCategory("Physics - Bepu")]
public class StaticComponent : CollidableComponent
{
    /// <summary> Can be null when it isn't part of a simulation yet/anymore </summary>
    internal StaticReference? StaticReference { get; private set; } = null;

    /// <summary>
    /// The position of this static in the physics scene, setting it will 'teleport' this object to the position provided.
    /// </summary>
    /// <remarks>
    /// This is performed automatically by the engine at the end of the frame when this entity's <see cref="TransformComponent"/> has been moved,
    /// you may call this manually earlier if needed.<br/><br/>
    /// Moving statics is very inefficient, do not write to this property every frame.<br/><br/>
    /// This value is slightly offset from this entity's Transform <see cref="TransformComponent.Position"/> based on its <see cref="CollidableComponent.CenterOfMass"/>.<br/><br/>
    /// Writing to this property is equivalent to calling <see cref="Teleport"/>.<br/><br/>
    /// This property overwrites this <see cref="Entity.Transform"/>'s data.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="value"/> must be finite</exception>
    [DataMemberIgnore]
    public Vector3 Position
    {
        get => StaticReference?.Pose.Position.ToStride() ?? default;
        set => Teleport(value, Orientation);
    }


    /// <summary>
    /// The rotation of this body in the physics scene, setting it will 'teleport' this object's rotation to the one provided.
    /// </summary>
    /// <remarks>
    /// This is performed automatically by the engine at the end of the frame when this entity's <see cref="TransformComponent"/> has been moved,
    /// you may call this manually earlier if needed.<br/><br/>
    /// Moving statics is very inefficient, do not write to this property every frame.<br/><br/>
    /// Writing to this property is equivalent to calling <see cref="Teleport"/>.<br/><br/>
    /// This property overwrites this <see cref="Entity.Transform"/>'s data.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="value"/> must be normalized</exception>
    [DataMemberIgnore]
    public Quaternion Orientation
    {
        get => StaticReference?.Pose.Orientation.ToStride() ?? default;
        set => Teleport(Position, value);
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
    protected internal override NRigidPose? Pose => StaticReference?.Pose;

    protected internal override CollidableReference? CollidableReference
    {
        get
        {
            if (StaticReference is { } sRef)
                return sRef.CollidableReference;
            return null;
        }
    }

    protected override void AttachInner(NRigidPose pose, BodyInertia shapeInertia, TypedIndex shapeIndex)
    {
        Debug.Assert(Processor is not null);
        Debug.Assert(Simulation is not null);

        var sDescription = new StaticDescription(pose, shapeIndex);

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

        Processor.Statics.Add(this, Unsafe.As<Matrix, System.Numerics.Matrix4x4>(ref Entity.Transform.WorldMatrix));
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

    /// <summary>
    /// Teleport this static into a new pose
    /// </summary>
    /// <remarks>
    /// This is performed automatically by the engine at the end of the frame when this entity's <see cref="TransformComponent"/> has been moved,
    /// you may call this manually earlier if needed<br/><br/>
    /// <paramref name="position"/> is slightly offset from this entity's Transform <see cref="TransformComponent.Position"/> based on its <see cref="CollidableComponent.CenterOfMass"/>.<br/><br/>
    /// This method overwrites this <see cref="Entity.Transform"/>'s data.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="orientation"/> must be normalized; <paramref name="position"/> must be finite</exception>
    public void Teleport(Vector3 position, Quaternion orientation)
    {
        TeleportNoTransformUpdate(position, orientation);
        UpdateTransformationComponent(position, orientation);
    }

    /// <inheritdoc cref="Teleport"/>
    internal void TeleportNoTransformUpdate(Vector3 position, Quaternion orientation)
    {
        position.ValidateRange(nameof(TeleportNoTransformUpdate), nameof(position));
        orientation.ValidateRange(nameof(TeleportNoTransformUpdate), nameof(orientation));

        if (StaticReference is { } staticRef)
        {
            staticRef.Pose.Orientation = orientation.ToNumeric();
            staticRef.Pose.Position = position + CenterOfMass;
            staticRef.UpdateBounds();
        }
    }
}
