using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.BepuPhysics.Systems;

namespace Stride.BepuPhysics.Definitions.Colliders;

#warning consider swapping List<> to an IList<> in the future to avoid cast down misuse
#warning Or/and consider adding a property 'List<ColliderBase> Colliders', so that it work in stride (we actually cannot add/remove BoxColliders/OthersColliders)
#warning With your new implementation, we can also copy this class and name it "BigCompoundCollider", and use Bepu.BigCompound instead of bepu.Compound (it's faster for bigs compounds)

[DataContract]
public class CompoundCollider : List<ColliderBase>, ICollider, IList<ColliderBase>
{
    private ContainerComponent? _container;
    [DataMemberIgnore]
    ContainerComponent? ICollider.Container { get => _container; set => _container = value; }

    public new void Add(ColliderBase item)
    {
        base.Add(item);
        _container?.TryUpdateContainer();
    }
    public new void Remove(ColliderBase item)
    {
        base.Remove(item);
        _container?.TryUpdateContainer();
    }
    public new void RemoveAll(Predicate<ColliderBase> match)
    {
        base.RemoveAll(match);
        _container?.TryUpdateContainer();
    }
    public new void RemoveAt(int index)
    {
        base.RemoveAt(index);
        _container?.TryUpdateContainer();
    }
    public new void RemoveRange(int index, int count)
    {
        base.RemoveRange(index, count);
        _container?.TryUpdateContainer();
    }
    public new void AddRange(IEnumerable<ColliderBase> collection)
    {
        base.AddRange(collection);
        _container?.TryUpdateContainer();
    }
    public new void Clear()
    {
        base.Clear();
        _container?.TryUpdateContainer();
    }

    [DataMemberIgnore]
    public int Transforms => Count;

    public void GetLocalTransforms(ContainerComponent container, Span<ShapeTransform> transforms)
    {
        for (int i = 0; i < Count; i++)
        {
            var collider = this[i];
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
        if (Count == 0)
        {
            index = default;
            centerOfMass = default;
            inertia = default;
            return false;
        }

        var compoundBuilder = new CompoundBuilder(pool, shapes, Count);
        try
        {
            foreach (var collider in this)
            {
                var localTranslation = collider.PositionLocal;
                var localRotation = collider.RotationLocal;

                var compoundChildLocalPose = new RigidPose(localTranslation.ToNumericVector(), localRotation.ToNumericQuaternion());
                collider.AddToCompoundBuilder(shapeCache, pool, ref compoundBuilder, compoundChildLocalPose);
                collider.Container = _container;
            }

            Buffer<CompoundChild> compoundChildren;
            System.Numerics.Vector3 shapeCenter;
            compoundBuilder.BuildDynamicCompound(out compoundChildren, out inertia, out shapeCenter);

            index = shapes.Add(new Compound(compoundChildren));
            centerOfMass = shapeCenter.ToStrideVector();
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
        foreach (var collider in this)
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