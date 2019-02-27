// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core;
using Xenko.Graphics;
using Xenko.Graphics.Data;
using Xenko.Rendering;

namespace Xenko.Extensions
{
    public static class MergeExtensions
    {
        /// <summary>
        /// Transform a vertex buffer positions, normals, tangents and bitangents using the given matrix.
        /// </summary>
        /// <param name="meshDrawDatas">The mesh draw datas.</param>
        /// <param name="can32BitIndex">A flag stating if 32 bit index buffers.</param>
        public static unsafe MeshDraw MergeDrawData(IList<MeshDraw> meshDrawDatas, bool can32BitIndex)
        {
            if (meshDrawDatas.Count == 0)
                throw new ArgumentException("Need at least 1 MeshDrawData.", "meshDrawDatas");

            if (meshDrawDatas.Count == 1)
                return meshDrawDatas[0];

            // Check that vertex buffer declarations are matching
            var firstMeshDrawData = meshDrawDatas[0];
            if (!firstMeshDrawData.IsSimple())
                throw new InvalidOperationException("Can only merge simple MeshDrawData.");

            var firstVertexBuffer = firstMeshDrawData.VertexBuffers[0];
            var hasIndexBuffer = IsIndexed(meshDrawDatas);
            int totalVertexCount = 0;
            int totalIndexCount = 0;

            //TODO: extend to non-simple vertex declarations, fill with default values it missing declarations etc. ?

            for (int i = 0; i < meshDrawDatas.Count; i++)
            {
                var meshDrawData = meshDrawDatas[i];

                // This should not happen anymore
                if (i != 0)
                {
                    if (!meshDrawData.IsSimple())
                        throw new InvalidOperationException("Can only merge simple MeshDrawData.");

                    if (meshDrawData.VertexBuffers.Length != firstMeshDrawData.VertexBuffers.Length)
                        throw new InvalidOperationException("Non-matching vertex buffer declarations.");

                    if (!meshDrawData.VertexBuffers[0].Declaration.Equals(firstMeshDrawData.VertexBuffers[0].Declaration))
                        throw new InvalidOperationException("Non-matching vertex buffer declarations.");
                }

                if (meshDrawData.PrimitiveType != PrimitiveType.TriangleList)
                    throw new InvalidOperationException("Can only merge TriangleList.");

                // Update vertex/index counts
                totalVertexCount += meshDrawData.VertexBuffers[0].Count;
                if (hasIndexBuffer)
                {
                    if (meshDrawData.IndexBuffer != null)
                        totalIndexCount += meshDrawData.IndexBuffer.Count;
                    else
                        totalIndexCount += meshDrawData.VertexBuffers[0].Count;
                }
            }

            // Allocate vertex buffer
            var result = new MeshDraw { PrimitiveType = PrimitiveType.TriangleList };
            var destBufferData = new byte[firstVertexBuffer.Declaration.VertexStride * totalVertexCount];
            result.VertexBuffers = new VertexBufferBinding[]
            {
                new VertexBufferBinding(
                    new BufferData(BufferFlags.VertexBuffer, destBufferData).ToSerializableVersion(),
                    firstVertexBuffer.Declaration,
                    totalVertexCount,
                    firstVertexBuffer.Stride),
            };

            // Copy vertex buffers
            fixed (byte* destBufferDataStart = &destBufferData[0])
            {
                var destBufferDataCurrent = destBufferDataStart;
                foreach (var meshDrawData in meshDrawDatas)
                {
                    var sourceBuffer = meshDrawData.VertexBuffers[0].Buffer.GetSerializationData();
                    fixed (byte* sourceBufferDataStart = &sourceBuffer.Content[0])
                    {
                        Utilities.CopyMemory((IntPtr)destBufferDataCurrent, (IntPtr)sourceBufferDataStart, sourceBuffer.Content.Length);
                        destBufferDataCurrent += sourceBuffer.Content.Length;
                    }
                }
            }

            if (hasIndexBuffer)
            {
                var use32BitIndex = can32BitIndex && totalVertexCount > ushort.MaxValue; // 65535 = 0xFFFF is kept for primitive restart in strip

                // Allocate index buffer
                destBufferData = new byte[(use32BitIndex ? sizeof(uint) : sizeof(ushort)) * totalIndexCount];
                result.IndexBuffer = new IndexBufferBinding(
                    new BufferData(BufferFlags.IndexBuffer, destBufferData).ToSerializableVersion(),
                    use32BitIndex,
                    totalIndexCount);

                // Copy index buffers
                fixed (byte* destBufferDataStart = &destBufferData[0])
                {
                    var destBufferDataCurrent = destBufferDataStart;
                    var offset = 0;
                    foreach (var meshDrawData in meshDrawDatas)
                    {
                        var indexBuffer = meshDrawData.IndexBuffer;
                        byte[] sourceBufferContent = null;
                        var is32Bit = false;

                        if (indexBuffer != null)
                        {
                            sourceBufferContent = indexBuffer.Buffer.GetSerializationData().Content;
                            is32Bit = indexBuffer.Is32Bit;
                        }

                        if (offset != 0 || (use32BitIndex != meshDrawData.IndexBuffer.Is32Bit))
                        {
                            if (use32BitIndex)
                                sourceBufferContent = CreateIntIndexBuffer(offset, meshDrawData.IndexBuffer.Count, sourceBufferContent, is32Bit);
                            else
                                sourceBufferContent = CreateShortIndexBuffer(offset, meshDrawData.IndexBuffer.Count, sourceBufferContent, is32Bit);
                        }
                        
                        fixed (byte* sourceBufferDataStart = &sourceBufferContent[0])
                        {
                            Utilities.CopyMemory((IntPtr)destBufferDataCurrent, (IntPtr)sourceBufferDataStart,
                                sourceBufferContent.Length);
                            destBufferDataCurrent += sourceBufferContent.Length;
                        }

                        offset += meshDrawData.VertexBuffers[0].Count;
                    }
                }

                result.DrawCount = totalIndexCount;
            }
            else
            {
                result.DrawCount = totalVertexCount;
            }

            return result;
        }
        
        /// <summary>
        /// Group meshes that can be merged because they have the same vertex declaration.
        /// </summary>
        /// <param name="meshDrawDatas">The list of meshes.</param>
        /// <returns>A list of grouped meshes.</returns>
        public static List<List<MeshDraw>> CreateDeclarationMergeGroup(IList<MeshDraw> meshDrawDatas)
        {
            var mergingLists = new List<List<MeshDraw>>();

            foreach (var meshDrawData in meshDrawDatas)
            {
                if (!meshDrawData.IsSimple() || meshDrawData.PrimitiveType != PrimitiveType.TriangleList) // only one (interlaced) vertex buffer is allowed
                {
                    mergingLists.Add(new List<MeshDraw> { meshDrawData });
                    continue;
                }

                bool createNewGroup = true;
                var currentDecl = meshDrawData.VertexBuffers[0].Declaration;
                // look for matching descriptions
                foreach (var group in mergingLists)
                {
                    if (currentDecl.Equals(group[0].VertexBuffers[0].Declaration))
                    {
                        group.Add(meshDrawData);
                        createNewGroup = false;
                        break;
                    }
                }

                if (createNewGroup)
                    mergingLists.Add(new List<MeshDraw> { meshDrawData });
            }

            return mergingLists;
        }

        /// <summary>
        /// Create group of MeshDrawData that will be merged.
        /// </summary>
        /// <param name="meshDrawDatas">List of MehsDrawData to merge.</param>
        /// <param name="can32BitIndex">A flag stating if 32 bit index buffers are allowed.</param>
        /// <returns>A List of groups to merge internally.</returns>
        public static List<List<MeshDraw>> CreateOptimizedMergeGroups(IList<MeshDraw> meshDrawDatas, bool can32BitIndex)
        {
            if (meshDrawDatas.Count == 0)
                return new List<List<MeshDraw>>();
                
            if (meshDrawDatas.Count == 1)
                return new List<List<MeshDraw>> { meshDrawDatas.ToList() };

            // list of (vertices count -> list of meshes)
            // vertices count is the higher possible value for an index
            var mergingLists = new List<KeyValuePair<int, List<MeshDraw>>>();

            var maxVerticesCount = can32BitIndex ? uint.MaxValue : ushort.MaxValue;

            foreach (var drawData in meshDrawDatas)
            {
                mergingLists = mergingLists.OrderBy(x => x.Key).Reverse().ToList();

                // TODO : be careful of null vertex buffers too ?
                var vertexCount = drawData.VertexBuffers[0].Count;
                var insertIndex = -1;
                for (var j = 0; j < mergingLists.Count; ++j)
                {
                    if (mergingLists[j].Key + vertexCount <= maxVerticesCount)
                    {
                        insertIndex = j;
                        break;
                    }
                }

                if (insertIndex < 0) // new entry
                {
                    var newList = new List<MeshDraw>() { drawData };
                    mergingLists.Add(new KeyValuePair<int, List<MeshDraw>>(vertexCount, newList));
                }
                else // append
                {
                    var kvp = mergingLists[insertIndex];
                    kvp.Value.Add(drawData);
                    mergingLists[insertIndex] = new KeyValuePair<int, List<MeshDraw>>(vertexCount + kvp.Key, kvp.Value);
                }
            }

            return mergingLists.Select(x => x.Value).ToList();
        }

        /// <summary>
        /// Create an short typed index buffer.
        /// </summary>
        /// <param name="offset">The offset to apply to the indices.</param>
        /// <param name="count">The number of indices.</param>
        /// <param name="baseIndices">A possible base index buffer</param>
        /// <param name="is32Bit">Stating if baseIndices is filled with 32 bits int</param>
        /// <returns>A new index buffer.</returns>
        public static byte[] CreateShortIndexBuffer(int offset, int count, byte[] baseIndices = null, bool is32Bit = true)
        {
            var indices = new ushort[count];
            if (baseIndices == null)
            {
                for (var i = 0; i < count; ++i)
                {
                    indices[i] = (ushort)(i + offset);
                }
            }
            else
            {
                for (var i = 0; i < count; ++i)
                {
                    if (is32Bit)
                        indices[i] = (ushort)(BitConverter.ToInt32(baseIndices, 4 * i) + offset);
                    else
                        indices[i] = (ushort)(BitConverter.ToInt16(baseIndices, 2 * i) + offset);
                }
            }

            var sizeOf = Utilities.SizeOf(indices);
            var buffer = new byte[sizeOf];
            Utilities.Write(buffer, indices, 0, indices.Length);
            return buffer;
        }

        /// <summary>
        /// Create an int typed index buffer.
        /// </summary>
        /// <param name="offset">The offset to apply to the indices.</param>
        /// <param name="count">The number of indices.</param>
        /// <param name="baseIndices">A possible base index buffer</param>
        /// <param name="is32Bits">Stating if baseIndices is filled with 32 bits int</param>
        /// <returns>A new index buffer.</returns>
        public static byte[] CreateIntIndexBuffer(int offset, int count, byte[] baseIndices = null, bool is32Bits = true)
        {
            var indices = new uint[count];
            if (baseIndices == null)
            {
                for (var i = 0; i < count; ++i)
                {
                    indices[i] = (uint)(i + offset);
                }
            }
            else
            {
                for (var i = 0; i < count; ++i)
                {
                    if (is32Bits)
                        indices[i] = (uint)(BitConverter.ToInt32(baseIndices, 4 * i) + offset);
                    else
                        indices[i] = (uint)(BitConverter.ToInt16(baseIndices, 2 * i) + offset);
                }
            }

            var sizeOf = Utilities.SizeOf(indices);
            var buffer = new byte[sizeOf];
            Utilities.Write(buffer, indices, 0, indices.Length);
            return buffer;
        }

        /// <summary>
        /// Check if a index buffer will be needed for this merge group.
        /// </summary>
        /// <param name="meshDrawDatas">The list of MeshDrawdata to merge.</param>
        /// <returns>True if an index is needed, false otherwise.</returns>
        public static bool IsIndexed(IList<MeshDraw> meshDrawDatas)
        {
            return meshDrawDatas.Any(drawdata => drawdata.IndexBuffer != null);
        }

        /// <summary>
        /// Group the meshes.
        /// </summary>
        /// <param name="meshDrawDatas">The list of meshes to group.</param>
        /// <param name="can32BitIndex">A flag stating if 32 bit index buffers are allowed</param>
        /// <returns>The list of merged meshes.</returns>
        public static List<MeshDraw> GroupDrawData(this IList<MeshDraw> meshDrawDatas, bool can32BitIndex)
        {
            var declGroups = CreateDeclarationMergeGroup(meshDrawDatas);
            var groups = declGroups.SelectMany(x => CreateOptimizedMergeGroups(x, can32BitIndex)).ToList();
            return groups.Select(x => MergeDrawData(x, can32BitIndex)).ToList();
        }
    }
}
