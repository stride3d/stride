using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Systems;
using Stride.Core;

namespace Stride.BepuPhysics.Definitions.Colliders;

[DataContract]
public sealed class SphereCollider : ColliderBase
{
    private float _radius = 0.5f;

    public float Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            Container?.TryUpdateContainer();
        }
    }

    internal override void AddToCompoundBuilder(ShapeCacheSystem shape, BufferPool pool, ref CompoundBuilder builder, RigidPose localPose)
    {
        builder.Add(new Sphere(Radius), localPose, Mass);
    }
}