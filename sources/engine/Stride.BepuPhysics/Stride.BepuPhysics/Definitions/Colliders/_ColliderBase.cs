// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Mathematics;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics.Definitions.Colliders;

[DataContract(Inherited = true)]
public abstract class ColliderBase
{
    private float _mass = 1f;
    private Vector3 _positionLocal = Vector3.Zero;
    private Quaternion _rotationLocal = Quaternion.Identity;

    /// <remarks>
    /// Changing this value will reset some of the internal physics state of this body
    /// </remarks>
    public float Mass
    {
        get => _mass;
        set
        {
            _mass = value;
            TryUpdateFeatures();
        }
    }

    /// <summary>
    /// Local position of this collider relative to its parent
    /// </summary>
    /// <remarks>
    /// Changing this value will reset some of the internal physics state of this body
    /// </remarks>
    [DataAlias("LinearOffset")]
    public Vector3 PositionLocal
    {
        get => _positionLocal;
        set
        {
            _positionLocal = value;
            TryUpdateFeatures();
        }
    }

    /// <summary>
    /// Local rotation of this collider relative to its parent
    /// </summary>
    /// <remarks>
    /// Changing this value will reset some of the internal physics state of this body
    /// </remarks>
    public Quaternion RotationLocal
    {
        get => _rotationLocal;
        set
        {
            _rotationLocal = value;
            TryUpdateFeatures();
        }
    }

    [DataMemberIgnore]
    public CompoundCollider? Container { get; internal set; }

    protected void TryUpdateFeatures()
    {
        ((ICollider?)Container)?.Component?.TryUpdateFeatures();
    }

    internal abstract void AddToCompoundBuilder(ShapeCacheSystem shape, BufferPool pool, ref CompoundBuilder builder, NRigidPose localPose);
    internal abstract void OnDetach(BufferPool pool);
}
