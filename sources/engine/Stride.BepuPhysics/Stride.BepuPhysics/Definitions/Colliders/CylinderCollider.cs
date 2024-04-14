using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics.Definitions.Colliders;

[DataContract]
public sealed class CylinderCollider : ColliderBase
{
    private float _radius = 0.5f;
    private float _length = 1f;

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
        builder.Add(new Cylinder(Radius, Length), localPose, Mass);
    }

    internal override void OnDetach(BufferPool pool){ }
}
