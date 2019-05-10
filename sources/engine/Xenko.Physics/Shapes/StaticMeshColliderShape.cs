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
        private readonly Vector3[] verticesList;
        private readonly int[] indicesList;

        public IReadOnlyList<Vector3> Vertices => verticesList;
        public IReadOnlyList<int> Indices => indicesList;
        Vector3 meshScaling;

        public StaticMeshColliderShape(ICollection<Vector3> vertices, ICollection<int> indices, Vector3 scaling)
        {
            Type = ColliderShapeTypes.StaticMesh;
            Is2D = false;

            // Enfore static data
            verticesList = vertices.ToArray();
            indicesList = indices.ToArray();
            
            var meshData = new BulletSharp.TriangleIndexVertexArray(indicesList, new XenkoToBulletWrapper(verticesList));
            var baseCollider = new BulletSharp.BvhTriangleMeshShape(meshData, true);
            InternalShape = new BulletSharp.ScaledBvhTriangleMeshShape(baseCollider, scaling);
            DebugPrimitiveMatrix = Matrix.Scaling(Vector3.One * DebugScaling);
            meshScaling = scaling;
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            var verts = new VertexPositionNormalTexture[verticesList.Length];
            for(int i = 0; i < verticesList.Length; i++)
            {
                verts[i].Position = verticesList[i] * meshScaling;
            }
            var meshData = new GeometricMeshData<VertexPositionNormalTexture>(verts, indicesList, false);

            return new GeometricPrimitive(device, meshData).ToMeshDraw();
        }
        

        class XenkoToBulletWrapper : ICollection<BulletSharp.Math.Vector3>
        {
            ICollection<Vector3> internalColl;
            public XenkoToBulletWrapper(ICollection<Vector3> collectionToConvert)
            {
                internalColl = collectionToConvert;
            }

            public int Count => internalColl.Count;

            public bool IsReadOnly => true;

            public bool Contains(BulletSharp.Math.Vector3 item) => internalColl.Contains(item);

            public void CopyTo(BulletSharp.Math.Vector3[] array, int arrayIndex)
            {
                foreach (var value in internalColl)
                {
                    if (arrayIndex >= array.Length)
                        return;
                    array[arrayIndex++] = value;
                }
            }

            public void Add(BulletSharp.Math.Vector3 item) { throw new System.InvalidOperationException("Collection is read only"); }

            public bool Remove(BulletSharp.Math.Vector3 item) { throw new System.InvalidOperationException("Collection is read only"); }

            public void Clear() { throw new System.InvalidOperationException("Collection is read only"); }

            public IEnumerator<BulletSharp.Math.Vector3> GetEnumerator()
            {
                foreach (var value in internalColl)
                    yield return value;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
