// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Mathematics;
using Xenko.Extensions;
using Xenko.Graphics;
using Xenko.Graphics.GeometricPrimitives;
using Xenko.Rendering;

namespace Xenko.Physics
{
    public class StaticMeshColliderShape : ColliderShape
    {
        private readonly IReadOnlyList<Vector3> pointsList;
        private readonly IReadOnlyList<uint> indicesList;

        public StaticMeshColliderShape(IReadOnlyList<Vector3> points, IReadOnlyList<uint> indices, Vector3? scaling = null)
        {
            Type = ColliderShapeTypes.StaticMesh;
            Is2D = false;

            cachedScaling = scaling ?? Vector3.One;

            pointsList = points;
            indicesList = indices;

            var meshData = new BulletSharp.TriangleIndexVertexArray(new UIntToInt(indices), new V3ToBullet(points));
            var baseCollider = new BulletSharp.BvhTriangleMeshShape(meshData, true);
            if( scaling.HasValue == false || scaling.Value == Vector3.One ) {
                InternalShape = baseCollider;
            } else {
                InternalShape = new BulletSharp.ScaledBvhTriangleMeshShape(baseCollider, scaling.Value);
            }

            DebugPrimitiveMatrix = Matrix.Scaling(new Vector3(1, 1, 1) * DebugScaling);
        }

        public IReadOnlyList<Vector3> Points
        {
            get { return pointsList; }
        }
        public IReadOnlyList<uint> Indices
        {
            get { return indicesList; }
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            var verts = new VertexPositionNormalTexture[pointsList.Count];
            for (var i = 0; i < pointsList.Count; i++)
            {
                verts[i].Position = pointsList[i];
                verts[i].TextureCoordinate = Vector2.Zero;
                verts[i].Normal = Vector3.Zero;
            }

            var intIndices = indicesList.Select(x => (int)x).ToArray();

            ////calculate basic normals
            ////todo verify, winding order might be wrong?
            for (var i = 0; i < indicesList.Count; i += 3)
            {
                var i1 = intIndices[i];
                var i2 = intIndices[i + 1];
                var i3 = intIndices[i + 2];
                var a = verts[i1];
                var b = verts[i2];
                var c = verts[i3];
                var n = Vector3.Cross((b.Position - a.Position), (c.Position - a.Position));
                n.Normalize();
                verts[i1].Normal = verts[i2].Normal = verts[i3].Normal = n;
            }

            var meshData = new GeometricMeshData<VertexPositionNormalTexture>(verts, intIndices, false);

            return new GeometricPrimitive(device, meshData).ToMeshDraw();
        }


        class V3ToBullet : CollectionWrapper<Vector3, BulletSharp.Math.Vector3>
        {
            public V3ToBullet(IReadOnlyCollection<Vector3> collectionToConvert) : base(collectionToConvert)
            {
            }

            protected override BulletSharp.Math.Vector3 Convert(Vector3 from) => from;

            protected override Vector3 Convert(BulletSharp.Math.Vector3 from) => from;
        }
        class UIntToInt : CollectionWrapper<uint, int>
        {
            public UIntToInt(IReadOnlyCollection<uint> collectionToConvert) : base(collectionToConvert)
            {
            }

            protected override int Convert(uint from) { checked { return (int)from; } }

            protected override uint Convert(int from) { checked { return (uint)from; } }
        }

        abstract class CollectionWrapper<FromT, ToT> : ICollection<ToT>
        {
            IReadOnlyCollection<FromT> internalColl;
            public CollectionWrapper(IReadOnlyCollection<FromT> collectionToConvert)
            {
                internalColl = collectionToConvert;
            }

            public int Count => internalColl.Count;

            public bool IsReadOnly => true;

            public void Add(ToT item) { throw new System.InvalidOperationException("Collection is read only"); }

            public bool Remove(ToT item) { throw new System.InvalidOperationException("Collection is read only"); }

            public void Clear() { throw new System.InvalidOperationException("Collection is read only"); }

            public bool Contains(ToT item) => internalColl.Contains(Convert(item));

            public void CopyTo(ToT[] array, int arrayIndex)
            {
                foreach (var value in internalColl)
                {
                    if (arrayIndex >= array.Length)
                        return;
                    array[arrayIndex++] = Convert(value);
                }
            }

            public IEnumerator<ToT> GetEnumerator()
            {
                foreach (var value in internalColl)
                    yield return Convert(value);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            protected abstract ToT Convert(FromT from);
            protected abstract FromT Convert(ToT from);
        }
    }
}
