using BepuPhysics;
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

    public float Mass
    {
        get => _mass;
        set
        {
            _mass = value;
            Container?.TryUpdateContainer();
        }
    }

    /// <summary>
    /// Local position of this collider relative to its parent
    /// </summary>
    [DataAlias("LinearOffset")]
    public Vector3 PositionLocal
    {
        get => _positionLocal;
        set
        {
            _positionLocal = value;
            Container?.TryUpdateContainer();
        }
    }

    /// <summary>
    /// Local rotation of this collider relative to its parent
    /// </summary>
    public Quaternion RotationLocal
    {
        get => _rotationLocal;
        set
        {
            _rotationLocal = value;
            Container?.TryUpdateContainer();
        }
    }

    [DataMemberIgnore]
    public ContainerComponent? Container { get; internal set; }

    internal abstract void AddToCompoundBuilder(ShapeCacheSystem shape, BufferPool pool, ref CompoundBuilder builder, NRigidPose localPose);
}