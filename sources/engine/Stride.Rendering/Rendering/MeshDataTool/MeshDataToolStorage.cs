// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;
using Stride.Graphics.Data;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Rendering.Rendering.MeshDataTool
{
    public class MeshDataToolStorage : MeshDataToolBase
    {
        protected ContentManager contentManager;
        static readonly PixelFormat[] supportedFormats = { PixelFormat.R32G32B32_Float, PixelFormat.R32G32B32A32_Float, PixelFormat.R32G32_Float };
        public MeshDataToolStorage(Mesh srcMesh, ContentManager p_contentManager) : base(srcMesh)
        {
            contentManager = p_contentManager;
        }

        public override int getTotalVerticies()
        {
            int totalVerts = 0;

            foreach (var bufferBinding in mesh.Draw.VertexBuffers)
            {
                totalVerts += bufferBinding.Count;
            }

            return totalVerts;
        }

        public override int getTotalIndicies()
        {
            int totalIndices = 0;

            foreach (var bufferBinding in mesh.Draw.VertexBuffers)
            {
                totalIndices += mesh.Draw.IndexBuffer.Count;
            }

            return totalIndices;
        }

        private byte[] getGPUWindow()
        {
            byte[] gpuWindow;
            {
                int largestBuffer = 0;

                largestBuffer = Math.Max(largestBuffer, mesh.Draw.IndexBuffer.Buffer.Description.SizeInBytes);
                foreach (var bufferBinding in mesh.Draw.VertexBuffers)
                {
                    largestBuffer = Math.Max(largestBuffer, bufferBinding.Buffer.Description.SizeInBytes);
                }

                gpuWindow = new byte[largestBuffer];
            };

            return gpuWindow;
        }

        private T readStorageVerticesByGivenIndex<T>(string dataName, int index) where T : unmanaged
        {
            unsafe
            {
                int vCollOffset = 0;
                var totalVertices = 0;

                foreach (var bufferBinding in mesh.Draw.VertexBuffers)
                {
                    int stride = 0;
                    (int offset, VertexElement decl) posData;
                    // Find position within struct and buffer
                    {
                        int tempOffset = 0;
                        (int offset, VertexElement decl)? posDataNullable = null;
                        foreach (var elemDecl in bufferBinding.Declaration.VertexElements)
                        {
                            if (elemDecl.SemanticName.Equals(dataName, StringComparison.Ordinal))
                            {
                                posDataNullable = (tempOffset, elemDecl);
                            }

                            // Get new offset (if specified)
                            var currentElementOffset = elemDecl.AlignedByteOffset;
                            if (currentElementOffset != VertexElement.AppendAligned)
                                tempOffset = currentElementOffset;

                            var elementSize = elemDecl.Format.SizeInBytes();

                            // Compute next offset (if automatic)
                            tempOffset += elementSize;

                            stride = Math.Max(stride, tempOffset); // element are not necessary ordered by increasing offsets
                        }

                        if (posDataNullable == null)
                            throw new Exception("No position data found.");

                        posData = posDataNullable.Value;

                        if (!supportedFormats.Contains(posData.decl.Format))
                            throw new Exception("Type format is not supported");
                    }

                    var realIndex = index - totalVertices;
                    totalVertices += bufferBinding.Count;
                    if (totalVertices >= index)
                    {
                        //vertices
                        var vertexBufferRef = AttachedReferenceManager.GetAttachedReference(bufferBinding.Buffer);
                        byte[] vertexData;
                        if (vertexBufferRef.Data != null)
                        {
                            vertexData = ((BufferData)vertexBufferRef.Data).Content;
                        }
                        else if (!string.IsNullOrEmpty(vertexBufferRef.Url))
                        {
                            var dataAsset = contentManager.Load<Buffer>(vertexBufferRef.Url);
                            vertexData = dataAsset.GetSerializationData().Content;
                        }
                        else
                        {
                            throw new Exception("Failed to get Vertices BufferData for entity {chunk.Entity.Name}'s model.");
                        }

                        fixed (byte* vStart = &vertexData[bufferBinding.Offset + (realIndex * stride) + posData.offset])
                        {
                            return *(T*)vStart;
                        }
                    }
                }
            }

            return new T();
        }

        private T[] readStorageVertices<T>(string dataName) where T : unmanaged
        {
            T[] vertices = new T[this.getTotalVerticies()];

            unsafe
            {
                int vCollOffset = 0;

                foreach (var bufferBinding in mesh.Draw.VertexBuffers)
                {
                    int stride = 0;
                    (int offset, VertexElement decl) posData;
                    // Find position within struct and buffer
                    {
                        int tempOffset = 0;
                        (int offset, VertexElement decl)? posDataNullable = null;
                        foreach (var elemDecl in bufferBinding.Declaration.VertexElements)
                        {
                            if (elemDecl.SemanticName.Equals(dataName, StringComparison.Ordinal))
                            {
                                posDataNullable = (tempOffset, elemDecl);
                            }

                            // Get new offset (if specified)
                            var currentElementOffset = elemDecl.AlignedByteOffset;
                            if (currentElementOffset != VertexElement.AppendAligned)
                                tempOffset = currentElementOffset;

                            var elementSize = elemDecl.Format.SizeInBytes();

                            // Compute next offset (if automatic)
                            tempOffset += elementSize;

                            stride = Math.Max(stride, tempOffset); // element are not necessary ordered by increasing offsets
                        }

                        if (posDataNullable == null)
                            throw new Exception("No position data found.");

                        posData = posDataNullable.Value;

                        if (!supportedFormats.Contains(posData.decl.Format))
                            throw new Exception("Type format is not supported");
                    }

                    //vertices
                    var vertexBufferRef = AttachedReferenceManager.GetAttachedReference(bufferBinding.Buffer);
                    byte[] vertexData;
                    if (vertexBufferRef.Data != null)
                    {
                        vertexData = ((BufferData)vertexBufferRef.Data).Content;
                    }
                    else if (!string.IsNullOrEmpty(vertexBufferRef.Url))
                    {
                        var dataAsset = contentManager.Load<Buffer>(vertexBufferRef.Url);
                        vertexData = dataAsset.GetSerializationData().Content;
                    }
                    else
                    {
                        throw new Exception("Failed to get Vertices BufferData for entity {chunk.Entity.Name}'s model.");
                    }

                    for (int id = 0; id < bufferBinding.Count; id++)
                    {
                        fixed (byte* vStart = &vertexData[bufferBinding.Offset + (id * stride) + posData.offset])
                        {
                           vertices[vCollOffset++] = *(T*)vStart;
                        }
                    }
                }
            }

            return vertices;
        }

        #region Arrays
        public override Vector3[] getPositions()
        {
            return this.readStorageVertices<Vector3>(VertexElementUsage.Position);
        }

        public override Vector4[] getTangents()
        {
            return this.readStorageVertices<Vector4>(VertexElementUsage.Tangent);
        }

        public override Vector3[] getNormals()
        {
            return this.readStorageVertices<Vector3>(VertexElementUsage.Normal);
        }

        public override Vector2[] getUVs()
        {
            return this.readStorageVertices<Vector2>(VertexElementUsage.TextureCoordinate);
        }
        #endregion


        #region IndexedValues
        public override Vector3 getPosition(int index)
        {
            return this.readStorageVerticesByGivenIndex<Vector3>(VertexElementUsage.Position, index);
        }

        public override Vector4 getTangent(int index)
        {
            return this.readStorageVerticesByGivenIndex<Vector4>(VertexElementUsage.Tangent, index);
        }

        public override Vector3 getNormal(int index)
        {
            return this.readStorageVerticesByGivenIndex<Vector3>(VertexElementUsage.Normal, index);
        }

        public override Vector2 getUV(int index)
        {
            return this.readStorageVerticesByGivenIndex<Vector2>(VertexElementUsage.TextureCoordinate, index);
        }

        #endregion

        public override int[] getIndicies()
        {
            int[] indices = new int[this.getTotalIndicies()];
            foreach (var bufferBinding in mesh.Draw.VertexBuffers)
            {
                unsafe
                {
                    int iCollOffset = 0;

                    //indicies
                    var indexBufferRef = AttachedReferenceManager.GetAttachedReference(mesh.Draw.IndexBuffer.Buffer);
                    byte[] indexData;
                    if (indexBufferRef.Data != null)
                    {
                        indexData = ((BufferData)indexBufferRef.Data).Content;
                    }
                    else if (!string.IsNullOrEmpty(indexBufferRef.Url))
                    {
                        var dataAsset = contentManager.Load<Buffer>(indexBufferRef.Url);
                        indexData = dataAsset.GetSerializationData().Content;
                    }
                    else
                    {
                        throw new Exception("Failed to get Indices BufferData for entity {chunk.Entity.Name}'s model.");
                    }

                    var indexSize = mesh.Draw.IndexBuffer.Is32Bit ? sizeof(uint) : sizeof(ushort);
                    var indicies = indexData
                               .Skip(mesh.Draw.IndexBuffer.Offset)
                               .Take(mesh.Draw.IndexBuffer.Count * indexSize)
                               .ToArray();

                    for(int id = 0; id < mesh.Draw.IndexBuffer.Count; id++)
                    {
                        fixed (byte* vStart = &indicies[id * indexSize])
                        {
                            if (mesh.Draw.IndexBuffer.Is32Bit)
                            {
                               indices[iCollOffset++] = *(int*)vStart;
                            }
                            // convert ushort gpu representation to uint
                            else
                            {
                                indices[iCollOffset++] = *(ushort*)vStart;
                            }
                        }
                    }
                }
            }

            return indices;
        }
    }
}
