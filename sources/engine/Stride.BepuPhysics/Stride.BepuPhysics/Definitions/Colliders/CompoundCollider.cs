using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.BepuPhysics.Systems;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics.Definitions.Colliders;


[DataContract]
public class CompoundCollider : ICollider
{
    private readonly ListOfColliders _colliders;
    private ContainerComponent? _container;

    [DataMember]
    public IList<ColliderBase> Colliders => _colliders;

    [DataMemberIgnore]
    public Action OnEditCallBack { get; set; }

    [DataMemberIgnore]
    public int Transforms => _colliders.Count;

    [DataMemberIgnore]
    ContainerComponent? ICollider.Container { get => _container; set => _container = value; }

#warning shouldn't we build heuristics to swap automatically instead of querying users, is there a definite case where it always performs better as a BigCompound instead of a normal one ? We can't really expect users to choose the right one by themselves
    public bool IsBig => false;

    public CompoundCollider()
    {
        OnEditCallBack = () => _container?.TryUpdateContainer();
        _colliders = new() { Owner = this };
    }

    public void GetLocalTransforms(ContainerComponent container, Span<ShapeTransform> transforms)
    {
        for (int i = 0; i < _colliders.Count; i++)
        {
            var collider = _colliders[i];
            transforms[i].PositionLocal = collider.PositionLocal;
            transforms[i].RotationLocal = collider.RotationLocal;
            transforms[i].Scale = collider switch
            {
                BoxCollider box => box.Size,
                CapsuleCollider cap => Vector3.One,
                CylinderCollider cyl => new(cyl.Radius, cyl.Length, cyl.Radius),
                SphereCollider sph => new(sph.Radius),
                TriangleCollider tri => Vector3.One,
                ConvexHullCollider convex => convex.Scale,
                _ => throw new NotImplementedException($"Collider type {collider.GetType()} is missing in {nameof(GetLocalTransforms)}, please fill an issue or fix it"),
            };
        }
    }

    bool ICollider.TryAttach(Shapes shapes, BufferPool pool, ShapeCacheSystem shapeCache, out TypedIndex index, out Vector3 centerOfMass, out BodyInertia inertia)
    {
        if (_colliders.Count == 0)
        {
            index = default;
            centerOfMass = default;
            inertia = default;
            return false;
        }

        var compoundBuilder = new CompoundBuilder(pool, shapes, _colliders.Count);
        try
        {
            foreach (var collider in _colliders)
            {
                var localTranslation = collider.PositionLocal;
                var localRotation = collider.RotationLocal;

                var compoundChildLocalPose = new NRigidPose(localTranslation.ToNumeric(), localRotation.ToNumeric());
                collider.AddToCompoundBuilder(shapeCache, pool, ref compoundBuilder, compoundChildLocalPose);
                collider.Container = _container;
            }

            Buffer<CompoundChild> compoundChildren;
            System.Numerics.Vector3 shapeCenter;
            compoundBuilder.BuildDynamicCompound(out compoundChildren, out inertia, out shapeCenter);

            index = IsBig ? shapes.Add(new BigCompound(compoundChildren, shapes, pool)) : shapes.Add(new Compound(compoundChildren));
            centerOfMass = shapeCenter.ToStride();
        }
        finally
        {
            compoundBuilder.Dispose();
        }

        return true;
    }

    void ICollider.Detach(Shapes shapes, BufferPool pool, TypedIndex index)
    {
        shapes.RemoveAndDispose(index, pool);
    }

    void ICollider.AppendModel(List<BasicMeshBuffers> buffer, ShapeCacheSystem shapeCache, out object? cache)
    {
        cache = null;
        foreach (var collider in _colliders)
        {
            buffer.Add(collider switch
            {
                BoxCollider box => shapeCache._boxShapeData,
                CapsuleCollider cap => shapeCache.BuildCapsule(cap),
                CylinderCollider cyl => shapeCache._cylinderShapeData,
                SphereCollider sph => shapeCache._sphereShapeData,
                TriangleCollider tri => shapeCache.BuildTriangle(tri),
                ConvexHullCollider con => shapeCache.BorrowHull(con),
                _ => throw new NotImplementedException($"collider type {collider.GetType()} is missing in ContainerShapeProcessor, please fill an issue or fix it"),
            });
        }
    }
}
