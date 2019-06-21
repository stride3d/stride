// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
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
        private readonly Vector3[] vertices;
        private readonly int[] indices;

        public IReadOnlyList<Vector3> Vertices => vertices;
        public IReadOnlyList<int> Indices => indices;


        /// <summary>
        /// Create a static collider from the vertices provided, ICollection will be duplicated before usage, 
        /// changes to the collection provided won't be reflected on the collider or <see cref="Vertices"/> and <see cref="Indices"/>.
        /// </summary>
        public StaticMeshColliderShape(ICollection<Vector3> vertices, ICollection<int> indices, Vector3 scaling) : this(vertices.ToArray(), indices.ToArray(), scaling)
        {

        }

        /// <summary>
        /// Internal constructor, expects readonly array; any changes made to the vertices won't be reflected on the physics shape
        /// </summary>
        StaticMeshColliderShape(Vector3[] verticesParam, int[] indicesParam, Vector3 scaling)
        {
            Type = ColliderShapeTypes.StaticMesh;
            Is2D = false;

            vertices = verticesParam;
            indices = indicesParam;
            
            var meshData = new BulletSharp.TriangleIndexVertexArray(indices, new XenkoToBulletWrapper(vertices));
            var baseCollider = new BulletSharp.BvhTriangleMeshShape(meshData, true);
            InternalShape = new BulletSharp.ScaledBvhTriangleMeshShape(baseCollider, scaling);
            DebugPrimitiveMatrix = Matrix.Scaling(Vector3.One * DebugScaling);
            Scaling = scaling;
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            var verts = new VertexPositionNormalTexture[vertices.Length];
            for(int i = 0; i < vertices.Length; i++)
            {
                verts[i].Position = vertices[i];
            }
            var meshData = new GeometricMeshData<VertexPositionNormalTexture>(verts, indices, false);

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
