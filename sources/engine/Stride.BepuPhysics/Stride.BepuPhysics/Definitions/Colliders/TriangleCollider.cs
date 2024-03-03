using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Mathematics;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics.Definitions.Colliders;

[DataContract]
public sealed class TriangleCollider : ColliderBase
{
    private Vector3 _a = new(1, 1, 1);
    private Vector3 _b = new(1, 1, 1);
    private Vector3 _c = new(1, 1, 1);

    public Vector3 A
    {
        get => _a;
        set
        {
            _a = value;
            Container?.TryUpdateContainer();
        }
    }

    public Vector3 B
    {
        get => _b;
        set
        {
            _b = value;
            Container?.TryUpdateContainer();
        }
    }

    public Vector3 C
    {
        get => _c;
        set
        {
            _c = value;
            Container?.TryUpdateContainer();
        }
    }

    internal override void AddToCompoundBuilder(ShapeCacheSystem shape, BufferPool pool, ref CompoundBuilder builder, NRigidPose localPose)
    {
        builder.Add(new Triangle(A.ToNumeric(), B.ToNumeric(), C.ToNumeric()), localPose, Mass);
    }
}
