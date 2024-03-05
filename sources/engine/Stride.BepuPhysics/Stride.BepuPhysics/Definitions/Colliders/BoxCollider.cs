using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Mathematics;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics.Definitions.Colliders;

[DataContract]
public sealed class BoxCollider : ColliderBase
{
    private Vector3 _size = new(1, 1, 1);

    public Vector3 Size
    {
        get => _size;
        set
        {
            _size = value;
            Component?.TryUpdateFeatures();
        }
    }

    internal override void AddToCompoundBuilder(ShapeCacheSystem shape, BufferPool pool, ref CompoundBuilder builder, NRigidPose localPose)
    {
        builder.Add(new Box(Size.X, Size.Y, Size.Z), localPose, Mass);
    }
}