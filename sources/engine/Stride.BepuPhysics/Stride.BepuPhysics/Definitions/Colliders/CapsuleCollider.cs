using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics.Definitions.Colliders;

[DataContract]
public sealed class CapsuleCollider : ColliderBase
{
    private float _radius = 0.35f;
    private float _length = 0.5f;

    public float Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            Component?.TryUpdateFeatures();
        }
    }

    public float Length
    {
        get => _length;
        set
        {
            _length = value;
            Component?.TryUpdateFeatures();
        }
    }

    internal override void AddToCompoundBuilder(ShapeCacheSystem shape, BufferPool pool, ref CompoundBuilder builder, NRigidPose localPose)
    {
        builder.Add(new Capsule(Radius, Length), localPose, Mass);
    }
}