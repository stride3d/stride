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

        /// <summary>
        /// Can return null if the model is empty
        /// </summary>
        public static StaticMeshColliderShape FromModel(Model model, Vector3 localPosition, Quaternion localRotation, Vector3 scale)
        {
            int[] indices;
            Vector3[] vertices;
            {
                int totalIndices = 0;
                int totalVerts = 0;

                foreach (var mesh in model.Meshes)
                {
                    foreach (var bufferBinding in mesh.Draw.VertexBuffers)
                    {
                        // We have to duplicate the index buffer for each vertex buffers since
                        // bullet doesn't have a construct sharing index buffers
                        totalIndices += mesh.Draw.IndexBuffer.Count;
                        totalVerts += bufferBinding.Count;
                    }
                }

                if (totalIndices == 0 || totalVerts == 0)
                    return null;

                indices = new int[totalIndices];
                vertices = new Vector3[totalVerts];
            }

            // Allocate one byte array to push GPU data onto
            byte[] gpuWindow;
            {
                int largestBuffer = 0;
                foreach (var mesh in model.Meshes)
                {
                    largestBuffer = Math.Max(largestBuffer, mesh.Draw.IndexBuffer.Buffer.Description.SizeInBytes);
                    foreach (var bufferBinding in mesh.Draw.VertexBuffers)
                    {
                        largestBuffer = Math.Max(largestBuffer, bufferBinding.Buffer.Description.SizeInBytes);
                    }
                }

                gpuWindow = new byte[largestBuffer];
            }


            int iCollOffset = 0;
            int vCollOffset = 0;
            foreach (var mesh in model.Meshes)
            {
                var commandList = (CommandList)typeof(GraphicsDevice).GetField("InternalMainCommandList",
                    System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.GetField
                    | System.Reflection.BindingFlags.FlattenHierarchy).GetValue(mesh.Draw.IndexBuffer.Buffer.GraphicsDevice);

                foreach (var bufferBinding in mesh.Draw.VertexBuffers)
                {
                    // Take care of the index buffer
                    unsafe
                    {
                        fixed (byte* window = &gpuWindow[0])
                        {
                            var binding = mesh.Draw.IndexBuffer;
                            var buffer = binding.Buffer;
                            var elementCount = binding.Count;
                            var sizeInBytes = buffer.Description.SizeInBytes;

                            FetchBufferData(buffer, commandList, new DataPointer(window, sizeInBytes));
                            byte* start = window + binding.Offset;

                            if (binding.Is32Bit)
                            {
                                // For multiple meshes, indices have to be offset
                                // since we're merging all the meshes together
                                int* shortPtr = (int*)start;
                                for (int i = 0; i < elementCount; i++)
                                {
                                    indices[iCollOffset++] = vCollOffset + shortPtr[i];
                                }
                            }
                            // convert ushort gpu representation to uint
                            else
                            {
                                ushort* shortPtr = (ushort*)start;
                                for (int i = 0; i < elementCount; i++)
                                {
                                    indices[iCollOffset++] = vCollOffset + shortPtr[i];
                                }
                            }
                        }
                    }

                    int stride = 0;
                    (int offset, VertexElement decl) posData;
                    // Find position within struct and buffer
                    {
                        int tempOffset = 0;
                        (int offset, VertexElement decl)? posDataNullable = null;
                        foreach (var elemDecl in bufferBinding.Declaration.VertexElements)
                        {
                            if (elemDecl.SemanticName.Equals("POSITION", StringComparison.Ordinal))
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
                            throw new Exception($"No position data within {mesh}'s {nameof(mesh.Draw.VertexBuffers)}");

                        posData = posDataNullable.Value;
                    }

                    // Fetch vertex position data from GPU
                    unsafe
                    {
                        fixed (byte* window = &gpuWindow[0])
                        {
                            var sizeInBytes = bufferBinding.Buffer.Description.SizeInBytes;
                            var elementCount = bufferBinding.Count;

                            FetchBufferData(bufferBinding.Buffer, commandList, new DataPointer(window, sizeInBytes));

                            byte* start = window + bufferBinding.Offset;

                            if (posData.decl.Format != PixelFormat.R32G32B32_Float)
                                throw new NotImplementedException(posData.decl.Format.ToString());

                            for (int i = 0; i < elementCount; i++)
                            {
                                byte* vStart = &start[i * stride + posData.offset];
                                vertices[vCollOffset++] = *(Vector3*)vStart;
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                localRotation.Rotate(ref vertices[i]);
                vertices[i] += localPosition;
            }

            return new StaticMeshColliderShape(vertices, indices, scale);
        }

        static unsafe void FetchBufferData(Graphics.Buffer buffer, CommandList commandList, DataPointer ptr)
        {
            if (buffer.Description.Usage == GraphicsResourceUsage.Staging)
            {
                // Directly if this is a staging resource
                buffer.GetData(commandList, buffer, ptr);
            }
            else
            {
                // Unefficient way to use the Copy method using dynamic staging texture
                using (var throughStaging = buffer.ToStaging())
                    buffer.GetData(commandList, throughStaging, ptr);
            }
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
