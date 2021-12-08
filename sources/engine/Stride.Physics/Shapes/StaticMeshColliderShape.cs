// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.Physics
{

    public class StaticMeshColliderShape : ColliderShape
    {
        struct DrawEdge
        {
            public Vector3 a;
            public Vector3 b;

            public static bool operator <(DrawEdge p_edge_a, DrawEdge p_edge_b)
            {
                if (p_edge_a.a == p_edge_b.a)
                {
                    return p_edge_a.b < p_edge_b.b;
                }
                else
                {
                    return p_edge_a.a < p_edge_b.a;
                }
            }

            public static bool operator >(DrawEdge p_edge_a, DrawEdge p_edge_b)
            {
                if (p_edge_a.a == p_edge_b.a)
                {
                    return p_edge_a.b > p_edge_b.b;
                }
                else
                {
                    return p_edge_a.a > p_edge_b.a;
                }
            }

            public DrawEdge(Vector3 p_a, Vector3 p_b)
            {
                a = p_a;
                b = p_b;

                if (a < b)
                {
                    b = p_a;
                    a = p_b;
                }
            }
        }

        private readonly IReadOnlyList<Vector3> faces;

        public StaticMeshColliderShape(IReadOnlyList<Vector3> _faces, Vector3 scaling)
        {
            Type = ColliderShapeTypes.StaticMesh;
            Is2D = false;

            cachedScaling = scaling;

            faces = _faces;

            var trimesh = new BulletSharp.TriangleMesh();
            for (var i = 0; i < faces.Count; i += 3)
            {
                var a = faces[i];
                var b = faces[i+1];
                var c = faces[i+2];

                trimesh.AddTriangle(a, b, c);
            }

            InternalShape = new BulletSharp.BvhTriangleMeshShape(trimesh, true)
            {
                LocalScaling = cachedScaling
            };

            DebugPrimitiveMatrix = Matrix.Scaling(Vector3.One * DebugScaling);
        }

        public IReadOnlyList<Vector3> Faces
        {
            get { return faces; }
        }

        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            int index_count = faces.Count;
            if ((index_count % 3) != 0)
            {
                throw new GraphicsException("Face points count is not (index_count % 3) != 0. Size: " + index_count);
            }

            List<DrawEdge> edges = new List<DrawEdge>();
            for (int i = 0; i < index_count; i += 3)
            {
                for (int j = 0; j < 3; j++)
                {
                    DrawEdge de = new DrawEdge(faces[i + j], faces[i + ((j + 1) % 3)]);
                    edges.Add(de);
                }
            }

            var tempVerts = new List<VertexPositionNormalTexture>();
            var tempIndicies = new List<int>();

            int idx = 0;
            foreach (var edge in edges)
            {
                var vertA = new VertexPositionNormalTexture();
                vertA.TextureCoordinate = Vector2.Zero;
                vertA.Normal = Vector3.Zero;
                vertA.Position = edge.a;

                tempVerts.Add(vertA); // from

                var vertB = new VertexPositionNormalTexture();
                vertB.TextureCoordinate = Vector2.Zero;
                vertB.Normal = Vector3.Zero;
                vertB.Position = edge.b;

                tempVerts.Add(vertB); // to
                tempIndicies.Add(idx + 0);
                tempIndicies.Add(idx + 1);

                idx += 2;
            }

            var verts = tempVerts.ToArray();
            var indicies = tempIndicies.ToArray();

            var meshData = new GeometricMeshData<VertexPositionNormalTexture>(verts, indicies, false);
            var instance = new GeometricPrimitive(device, meshData);

            var vertexBufferBinding = new VertexBufferBinding(instance.VertexBuffer, VertexPositionNormalTexture.Layout, instance.VertexBuffer.ElementCount);
            var indexBufferBinding = new IndexBufferBinding(instance.IndexBuffer, instance.IsIndex32Bits, instance.IndexBuffer.ElementCount);

            var data = new MeshDraw
            {
                StartLocation = 0,
                PrimitiveType = PrimitiveType.LineList,
                VertexBuffers = new[] { vertexBufferBinding },
                IndexBuffer = indexBufferBinding,
                DrawCount = instance.IndexBuffer.ElementCount,
            };

            return data;
        }
    }
}
