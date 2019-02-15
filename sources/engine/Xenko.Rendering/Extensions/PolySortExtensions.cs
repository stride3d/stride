// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Graphics.Data;
using Xenko.Rendering;

namespace Xenko.Extensions
{
    public static class PolySortExtensions
    {
        public static unsafe void SortMeshPolygons(this MeshDraw meshData, Vector3 viewDirectionForSorting)
        {
            // need to have alreade an vertex buffer
            if (meshData.VertexBuffers == null)
                throw new ArgumentException();
            // For now, require a MeshData with an index buffer
            if (meshData.IndexBuffer == null)
                throw new NotImplementedException("The mesh Data needs to have index buffer");
            if (meshData.VertexBuffers.Length != 1)
                throw new NotImplementedException("Sorting not implemented for multiple vertex buffers by submeshdata");

            if (viewDirectionForSorting == Vector3.Zero)
            {
                // By default to -Z if sorting is set to null
                viewDirectionForSorting = -Vector3.UnitZ;
            }

            const int PolySize = 3; // currently only triangle list are supported
            var polyIndicesSize = PolySize * Utilities.SizeOf<int>();
            var vertexBuffer = meshData.VertexBuffers[0];
            var oldIndexBuffer = meshData.IndexBuffer;
            var vertexStride = vertexBuffer.Declaration.VertexStride;

            // Generate the sort list
            var sortList = new List<KeyValuePair<int, Vector3>>();

            fixed (byte* vertexBufferPointerStart = &vertexBuffer.Buffer.GetSerializationData().Content[vertexBuffer.Offset])
            fixed (byte* indexBufferPointerStart = &oldIndexBuffer.Buffer.GetSerializationData().Content[oldIndexBuffer.Offset])
            {
                for (var i = 0; i < oldIndexBuffer.Count / PolySize; ++i) 
                {
                    // compute the bary-center
                    var accu = Vector3.Zero;
                    for (var u = 0; u < PolySize; ++u)
                    {
                        var curIndex = *(int*)(indexBufferPointerStart + Utilities.SizeOf<int>() * (i * PolySize + u));
                        var pVertexPos = (Vector3*)(vertexBufferPointerStart + vertexStride * curIndex);
                        accu += *pVertexPos;
                    }

                    var center = accu / PolySize;

                    // add to the list to sort
                    sortList.Add(new KeyValuePair<int, Vector3>(i, center));
                }
            }

            // sort the list
            var sortedIndices = sortList.OrderBy(x => Vector3.Dot(x.Value, viewDirectionForSorting)).Select(x => x.Key).ToList();   // TODO have a generic delegate for sorting
            
            // re-write the index buffer
            var newIndexBufferData = new byte[oldIndexBuffer.Count * Utilities.SizeOf<int>()];
            fixed (byte* newIndexDataStart = &newIndexBufferData[0])
            fixed (byte* oldIndexDataStart = &oldIndexBuffer.Buffer.GetSerializationData().Content[0])
            {
                var newIndexBufferPointer = newIndexDataStart;

                foreach (var index in sortedIndices)
                {
                    Utilities.CopyMemory((IntPtr)(newIndexBufferPointer), (IntPtr)(oldIndexDataStart + index * polyIndicesSize), polyIndicesSize);

                    newIndexBufferPointer += polyIndicesSize;
                }
            }
            meshData.IndexBuffer = new IndexBufferBinding(new BufferData(BufferFlags.IndexBuffer, newIndexBufferData).ToSerializableVersion(), oldIndexBuffer.Is32Bit, oldIndexBuffer.Count);
        }
    }
}
