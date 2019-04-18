// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Graphics;
using Xenko.Graphics.Data;
using Xenko.Rendering;

namespace Xenko.Extensions
{
    public static class SplitExtensions
    {
        public static List<Mesh> SplitMeshes(List<Mesh> meshes, bool can32bitIndex)
        {
            var finalList = new List<Mesh>();
            foreach (var mesh in meshes)
            {
                var drawDatas = SplitMesh(mesh.Draw, can32bitIndex);
                if (drawDatas.Count <= 1)
                {
                    finalList.Add(mesh);
                }
                else
                {
                    foreach (var draw in drawDatas)
                    {
                        var newMeshData = new Mesh(draw, mesh.Parameters)
                            {
                                MaterialIndex = mesh.MaterialIndex,
                                Name = mesh.Name,
                                NodeIndex = mesh.NodeIndex,
                                Skinning = mesh.Skinning,
                            };
                        finalList.Add(newMeshData);
                    }
                }
            }

            return finalList;
        }

        /// <summary>
        /// Split the mesh if it has strictly more than 65535 vertices (max index = 65534) on a plaftorm that does not support 32 bits indices.
        /// </summary>
        /// <param name="meshDrawData">The mesh to analyze.</param>
        /// <param name="can32bitIndex">A flag stating if 32 bit indices are allowed.</param>
        /// <returns>A list of meshes.</returns>
        public static unsafe List<MeshDraw> SplitMesh(MeshDraw meshDrawData, bool can32bitIndex)
        {
            if (meshDrawData.IndexBuffer == null)
                return new List<MeshDraw> { meshDrawData };

            if (!meshDrawData.IndexBuffer.Is32Bit) // already 16 bits buffer
                return new List<MeshDraw> { meshDrawData };

            var verticesCount = meshDrawData.VertexBuffers[0].Count;
            if (verticesCount <= ushort.MaxValue) // can be put in a 16 bits buffer - 65535 = 0xFFFF is kept for primitive restart in strip
            {
                meshDrawData.CompactIndexBuffer();
                return new List<MeshDraw> { meshDrawData };
            }

            // now, we only have a 32 bits buffer that is justified because of a large vertex buffer

            if (can32bitIndex) // do nothing
                return new List<MeshDraw> { meshDrawData };

            // TODO: handle primitives other than triangle list
            if (meshDrawData.PrimitiveType != PrimitiveType.TriangleList)
                return new List<MeshDraw> { meshDrawData };

            // Split the mesh
            var finalList = new List<MeshDraw>();
            fixed (byte* indicesByte = &meshDrawData.IndexBuffer.Buffer.GetSerializationData().Content[0])
            {
                var indicesUint = (uint*)indicesByte;

                var splitInfos = new List<SplitInformation>();
                var currentSplit = new SplitInformation();
                currentSplit.StartTriangleIndex = 0;
                var currentIndexUintPtr = indicesUint;
                for (int triangleIndex = 0; triangleIndex < meshDrawData.IndexBuffer.Count / 3; ++triangleIndex)
                {
                    var verticesToAdd = 0;
                    var index0 = *currentIndexUintPtr++;
                    var index1 = *currentIndexUintPtr++;
                    var index2 = *currentIndexUintPtr++;
                    if (!currentSplit.UsedIndices.Contains(index0)) { ++verticesToAdd; }
                    if (!currentSplit.UsedIndices.Contains(index1)) { ++verticesToAdd; }
                    if (!currentSplit.UsedIndices.Contains(index2)) { ++verticesToAdd; }

                    if (currentSplit.UsedIndices.Count + verticesToAdd > 65535) // append in the same group
                    {
                        splitInfos.Add(currentSplit);
                        currentSplit = new SplitInformation();
                        currentSplit.StartTriangleIndex = triangleIndex;
                    }
                    AddTriangle(currentSplit, index0, index1, index2, triangleIndex);
                }

                if (currentSplit.UsedIndices.Count > 0)
                    splitInfos.Add(currentSplit);

                foreach (var splitInfo in splitInfos)
                {
                    var triangleCount = splitInfo.LastTriangleIndex - splitInfo.StartTriangleIndex + 1;
                    var newMeshDrawData = new MeshDraw
                    {
                        PrimitiveType = PrimitiveType.TriangleList,
                        DrawCount = 3 * triangleCount,
                        VertexBuffers = new VertexBufferBinding[meshDrawData.VertexBuffers.Length],
                    };

                    // vertex buffers
                    for (int vbIndex = 0; vbIndex < meshDrawData.VertexBuffers.Length; ++vbIndex)
                    {
                        var stride = meshDrawData.VertexBuffers[vbIndex].Stride;
                        if (stride == 0)
                            stride = meshDrawData.VertexBuffers[vbIndex].Declaration.VertexStride;
                        var newVertexBuffer = new byte[splitInfo.UsedIndices.Count * stride];

                        fixed (byte* vertexBufferPtr = &meshDrawData.VertexBuffers[vbIndex].Buffer.GetSerializationData().Content[0])
                        fixed (byte* newVertexBufferPtr = &newVertexBuffer[vbIndex])
                        {
                            //copy vertex buffer
                            foreach (var index in splitInfo.UsedIndices)
                                Utilities.CopyMemory((IntPtr)(newVertexBufferPtr + stride * splitInfo.IndexRemapping[index]), (IntPtr)(vertexBufferPtr + stride * index), stride);
                        }

                        newMeshDrawData.VertexBuffers[vbIndex] = new VertexBufferBinding(
                            new BufferData(BufferFlags.VertexBuffer, newVertexBuffer).ToSerializableVersion(),
                            meshDrawData.VertexBuffers[vbIndex].Declaration,
                            splitInfo.UsedIndices.Count);
                    }

                    // index buffer
                    var newIndexBuffer = new byte[sizeof(ushort) * 3 * triangleCount];
                    fixed (byte* newIndexBufferPtr = &newIndexBuffer[0])
                    {
                        var newIndexBufferUshortPtr = (ushort*)newIndexBufferPtr;
                        var currentIndexPtr = &indicesUint[3 * splitInfo.StartTriangleIndex];
                        for (int triangleIndex = 0; triangleIndex < triangleCount; ++triangleIndex)
                        {
                            var index0 = *currentIndexPtr++;
                            var index1 = *currentIndexPtr++;
                            var index2 = *currentIndexPtr++;

                            var newIndex0 = splitInfo.IndexRemapping[index0];
                            var newIndex1 = splitInfo.IndexRemapping[index1];
                            var newIndex2 = splitInfo.IndexRemapping[index2];

                            *newIndexBufferUshortPtr++ = newIndex0;
                            *newIndexBufferUshortPtr++ = newIndex1;
                            *newIndexBufferUshortPtr++ = newIndex2;
                        }
                    }

                    newMeshDrawData.IndexBuffer = new IndexBufferBinding(
                        new BufferData(BufferFlags.IndexBuffer, newIndexBuffer).ToSerializableVersion(),
                        false,
                        triangleCount * 3);

                    finalList.Add(newMeshDrawData);
                }
            }
            return finalList;
        }

        /// <summary>
        /// Add the triangle to the split information.
        /// </summary>
        /// <param name="currentSplit">The current split information.</param>
        /// <param name="index0">The index of the first vertex.</param>
        /// <param name="index1">The index of the second vertex.</param>
        /// <param name="index2">The index of the third vertex.</param>
        /// <param name="triangleIndex">The original index of the triangle.</param>
        private static void AddTriangle(SplitInformation currentSplit, uint index0, uint index1, uint index2, int triangleIndex)
        {
            if (currentSplit.UsedIndices.Add(index0)) currentSplit.IndexRemapping.Add(index0, (ushort)(currentSplit.UsedIndices.Count - 1));
            if (currentSplit.UsedIndices.Add(index1)) currentSplit.IndexRemapping.Add(index1, (ushort)(currentSplit.UsedIndices.Count - 1));
            if (currentSplit.UsedIndices.Add(index2)) currentSplit.IndexRemapping.Add(index2, (ushort)(currentSplit.UsedIndices.Count - 1));
            currentSplit.LastTriangleIndex = triangleIndex;
        }

        private class SplitInformation
        {
            public readonly Dictionary<uint, ushort> IndexRemapping;

            public readonly HashSet<uint> UsedIndices;

            public int StartTriangleIndex;

            public int LastTriangleIndex;

            public SplitInformation()
            {
                IndexRemapping = new Dictionary<uint, ushort>();
                UsedIndices = new HashSet<uint>();
            }
        }
    }
}
