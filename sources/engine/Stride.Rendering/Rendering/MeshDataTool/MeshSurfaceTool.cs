// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Data;

namespace Stride.Rendering.Rendering.MeshDataTool
{
    public struct MeshSurfaceToolVertex
    {
        public Vector2 uv { get; set; }
        public Vector4 tangent { get; set; }
        public Vector3 normal { get; set; } 
        public Vector3 position { get; set; }

        public string getHash()
        {
            return "p" + this.position.GetHashCode() + "u" + uv.GetHashCode() + "n" + normal.GetHashCode() + "t" + tangent.GetHashCode();
        }
    }
    public class MeshSurfaceTool
    {
        private MeshSurfaceToolVertex[] positions;
        private int[] indices;

        private bool hasUvs = false;
        private bool hasTangents = false;
        private bool hasNormals = false;

        public void SetUvs(Vector2[] p_uvs)
        {
            if (positions == null)
            {
                throw new Exception("You have to setup first the vertex position");
            }

            if (p_uvs.Length != positions.Length)
            {
                throw new Exception("Inconsistent size of uvs. Required: " + positions.Length);
            }

            for (int idx = 0; idx < p_uvs.Length; idx++)
            {
                positions[idx].uv = p_uvs[idx];
            }

            hasUvs = true;
        }

        public void SetNormals(Vector3[] p_normals)
        {
            if (positions == null)
            {
                throw new Exception("You have to setup first the vertex position");
            }

            if (p_normals.Length != positions.Length)
            {
                throw new Exception("Inconsistent size of normals. Required: " + positions.Length);
            }

            for (int idx = 0; idx < p_normals.Length; idx++)
            {
                positions[idx].normal = p_normals[idx];
            }

            hasNormals = true;
        }
        public void SetTangents(Vector4[] p_tangents)
        {
            if(positions == null)
            {
                throw new Exception("You have to setup first the vertex position");
            }

            if (p_tangents.Length != positions.Length)
            {
                throw new Exception("Inconsistent size of tangets. Required: " + positions.Length);
            }

            for (int idx = 0; idx < p_tangents.Length; idx++)
            {
                positions[idx].tangent = p_tangents[idx];
            }

            hasTangents = true;
        }

        public void SetPositions(Vector3[] p_positions)
        {

            positions = p_positions.Select(p => new MeshSurfaceToolVertex { position = p}).ToArray();
        }

        public void SetIndices(int[] p_indices)
        {
            if (p_indices.Length < positions.Length)
            {
                throw new Exception("Less indices then vertices make no sense. Required min. " + positions.Length);
            }

            if (p_indices.Length % 3 != 0)
            {
                throw new Exception("A triangle mesh required % 3");
            }

            indices = p_indices;
        }

        public void Reindex()
        {
            throw new Exception("Not implemented yet");
        }

        public MeshDraw Generate()
        {
            if (this.positions.Length < 3)
            {
                throw new Exception("Min required 3 vertices to generate a MeshDraw.");
            }

            var draw = new MeshDraw();
            draw.PrimitiveType = PrimitiveType.TriangleList;
            draw.DrawCount = indices.Length;

            this.createIndexBuffer(draw);
            this.createVertexBuffer(draw);

            return draw;
        }

        private List<VertexElement> getVertexElements()
        {
            var offset = 0;
            var elements = new List<VertexElement>();
            elements.Add(VertexElement.Position<Vector3>(0, offset));
            offset += Vector3.SizeInBytes;

            if (hasNormals)
            {
                elements.Add(VertexElement.Normal<Vector3>(0, offset));
                offset += Vector3.SizeInBytes;
            }

            if (hasUvs)
            {
                elements.Add(VertexElement.TextureCoordinate<Vector2>(0, offset));
                offset += Vector2.SizeInBytes;
            }

            if (hasTangents)
            {
                elements.Add(VertexElement.Tangent<Vector4>(0, offset));
                offset += Vector4.SizeInBytes;
            }

            return elements;
        }

        private void createVertexBuffer(MeshDraw meshDraw)
        {
            var vertexDecl = new VertexDeclaration(this.getVertexElements().ToArray());
            byte[] vertexData = new byte[vertexDecl.VertexStride * positions.Length];

            var offset = 0;
            unsafe
            {
                fixed (byte* vbPointer = &vertexData[0])
                {
                    foreach (var vert in positions)
                    {
                        Vector3* posOffset = (Vector3*)(vbPointer + offset);
                        *posOffset = vert.position;
                        offset += Vector3.SizeInBytes;

                        if (hasNormals)
                        {
                            Vector3* normalOffset = (Vector3*)(vbPointer + offset);
                            *normalOffset = vert.normal;
                            offset += Vector3.SizeInBytes;
                        }

                        if (hasUvs)
                        {
                            Vector2* uvOffset = (Vector2*)(vbPointer + offset);
                            *uvOffset = vert.uv;
                            offset += Vector2.SizeInBytes;
                        }

                        if (hasTangents)
                        {
                            Vector4* tangentOffset = (Vector4*)(vbPointer + offset);
                            *tangentOffset = vert.tangent;
                            offset += Vector4.SizeInBytes;
                        }
                    }
                }
            }

            if (offset != vertexData.Length)
            {
                throw new Exception("Something went wrong on conversation.");
            }

            var bufferData = new BufferData(BufferFlags.VertexBuffer, vertexData);
            meshDraw.VertexBuffers = new[] { new VertexBufferBinding(GraphicsSerializerExtensions.ToSerializableVersion(bufferData), vertexDecl, positions.Length, vertexDecl.VertexStride,0) };
        }

        private void createIndexBuffer(MeshDraw meshDraw)
        {
            //its ushort or int
            if (indices.Length > 65535)
            {
                byte[] indicesData = new byte[indices.Length * sizeof(int)];
                int offset = 0;
                unsafe
                {
                    fixed (byte* vbPointer = &indicesData[0])
                    {
                        for (int i = 0; i < indices.Length; i++)
                        {
                            int* indexRecord = (int*)(vbPointer + offset);
                            *indexRecord = (int)indices[i];
                            offset += sizeof(int);
                        }
                    }
                }

                if (indicesData.Length != indices.Length * sizeof(int))
                {
                    throw new Exception("Sometint went wrong on index buffer creation..");
                }

                var bufferData = new BufferData(BufferFlags.IndexBuffer, indicesData);
                meshDraw.IndexBuffer = new IndexBufferBinding(GraphicsSerializerExtensions.ToSerializableVersion(bufferData), true, indices.Length);
            }
            else
            {
                byte[] indicesData = new byte[indices.Length * sizeof(ushort)];
                int offset = 0;
                unsafe
                {
                    fixed (byte* vbPointer = &indicesData[0])
                    {
                        for (int i = 0; i < indices.Length; i++)
                        {
                            ushort* indexRecord = (ushort*)(vbPointer + offset);
                            *indexRecord = (ushort)indices[i];
                            offset += sizeof(ushort);
                        }
                    }
                }

                if (indicesData.Length != indices.Length * sizeof(ushort))
                {
                    throw new Exception("Sometint went wrong on index buffer creation..");
                }

                var bufferData = new BufferData(BufferFlags.IndexBuffer, indicesData);

                meshDraw.IndexBuffer = new IndexBufferBinding(GraphicsSerializerExtensions.ToSerializableVersion(bufferData), false, indices.Length);
            }
        }
    }
}
