using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Systems;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Definitions.Colliders
{
    public class EmptyCollider : ICollider
    {
        public int Transforms => 0;
        private ContainerComponent? _container;
        ContainerComponent? ICollider.Container { get => _container; set => _container = value; }

        int ICollider.Transforms => throw new NotImplementedException();

        public void GetLocalTransforms(ContainerComponent container, Span<ShapeTransform> transforms)
        {
        }

        void ICollider.AppendModel(List<BasicMeshBuffers> buffer, ShapeCacheSystem shapeCache, out object? cache)
        {
            cache = null;
        }

        void ICollider.Detach(Shapes shapes, BufferPool pool, TypedIndex index)
        {
        }

        void ICollider.GetLocalTransforms(ContainerComponent container, Span<ShapeTransform> transforms)
        {
        }

        bool ICollider.TryAttach(Shapes shapes, BufferPool pool, ShapeCacheSystem shapeCache, out TypedIndex index, out Vector3 centerOfMass, out BodyInertia inertia)
        {
            index = new();
            centerOfMass = new();
            inertia = new Sphere(1).ComputeInertia(1);
            return true;
        }
    }
}
