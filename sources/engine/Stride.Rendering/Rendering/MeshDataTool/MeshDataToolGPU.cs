// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;

namespace Stride.Rendering.Rendering.MeshDataTool
{
    public class MeshDataToolGPU : MeshDataToolBase
    {
        static readonly PixelFormat[] supportedFormats = { PixelFormat.R32G32B32_Float, PixelFormat.R32G32B32A32_Float, PixelFormat.R32G32_Float };
        public MeshDataToolGPU(Mesh srcMesh) : base(srcMesh)
        {
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

        private T readGPUVerticesByGivenIndex<T>(string dataName, int index) where T : unmanaged
        {
            var gpuWindow = this.getGPUWindow();

            var commandList = (CommandList)typeof(GraphicsDevice).GetField("InternalMainCommandList",
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.GetField
                | System.Reflection.BindingFlags.FlattenHierarchy).GetValue(mesh.Draw.IndexBuffer.Buffer.GraphicsDevice);

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
                        {
                            return new T();
                        }

                        posData = posDataNullable.Value;

                        if (!supportedFormats.Contains(posData.decl.Format))
                            return new T();
                    }

                    fixed (byte* window = &gpuWindow[0])
                    {
                        var sizeInBytes = bufferBinding.Buffer.Description.SizeInBytes;
                        var elementCount = bufferBinding.Count;

                        var realIndex = index - totalVertices;
                        totalVertices += elementCount;
                        if (totalVertices >= index)
                        {
                            FetchBufferData(bufferBinding.Buffer, commandList, new DataPointer(window, sizeInBytes));
                            byte* start = window + bufferBinding.Offset;
                            byte* vStart = &start[realIndex * stride + posData.offset];
                            T r = *(T*)vStart;

                            return r;
                        }
                    }
                }
            }

            return new T();
        }

        private T[] readGPUVertices<T>(string dataName) where T : unmanaged
        {
            T[] vertices = new T[this.getTotalVerticies()];
            var gpuWindow = this.getGPUWindow();

            var commandList = (CommandList)typeof(GraphicsDevice).GetField("InternalMainCommandList",
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.GetField
                | System.Reflection.BindingFlags.FlattenHierarchy).GetValue(mesh.Draw.IndexBuffer.Buffer.GraphicsDevice);

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
                        {
                            return null;
                        }

                        posData = posDataNullable.Value;

                        if (!supportedFormats.Contains(posData.decl.Format))
                            return null;
                    }

                    fixed (byte* window = &gpuWindow[0])
                    {
                        var sizeInBytes = bufferBinding.Buffer.Description.SizeInBytes;
                        var elementCount = bufferBinding.Count;

                        FetchBufferData(bufferBinding.Buffer, commandList, new DataPointer(window, sizeInBytes));

                        byte* start = window + bufferBinding.Offset;

                        for (int i = 0; i < elementCount; i++)
                        {
                            byte* vStart = &start[i * stride + posData.offset];
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
            return this.readGPUVertices<Vector3>(VertexElementUsage.Position);
        }

        public override Vector4[] getTangents()
        {
            return this.readGPUVertices<Vector4>(VertexElementUsage.Tangent);
        }

        public override Vector3[] getNormals()
        {
            return this.readGPUVertices<Vector3>(VertexElementUsage.Normal);
        }

        public override Vector2[] getUVs()
        {
            return this.readGPUVertices<Vector2>(VertexElementUsage.TextureCoordinate);
        }
        #endregion


        #region IndexedValues
        public override Vector3 getPosition(int index)
        {
            return this.readGPUVerticesByGivenIndex<Vector3>(VertexElementUsage.Position, index);
        }

        public override Vector4 getTangent(int index)
        {
            return this.readGPUVerticesByGivenIndex<Vector4>(VertexElementUsage.Tangent, index);
        }

        public override Vector3 getNormal(int index)
        {
            return this.readGPUVerticesByGivenIndex<Vector3>(VertexElementUsage.Normal, index);
        }

        public override Vector2 getUV(int index)
        {
            return this.readGPUVerticesByGivenIndex<Vector2>(VertexElementUsage.TextureCoordinate, index);
        }

        #endregion


        public override int[] getIndicies()
        {
            int[] indices = new int[this.getTotalIndicies()];
            foreach (var bufferBinding in mesh.Draw.VertexBuffers)
            {
                var gpuWindow = this.getGPUWindow();

                var commandList = (CommandList)typeof(GraphicsDevice).GetField("InternalMainCommandList",
                    System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.GetField
                    | System.Reflection.BindingFlags.FlattenHierarchy).GetValue(mesh.Draw.IndexBuffer.Buffer.GraphicsDevice);

                unsafe
                {
                    int iCollOffset = 0;
                    int vCollOffset = 0;

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

                        vCollOffset += bufferBinding.Count;
                    }
                }
            }

            return indices;
        }

        private unsafe void FetchBufferData(Graphics.Buffer buffer, CommandList commandList, DataPointer ptr)
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
    }
}
