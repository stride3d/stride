// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;
using Xenko.Graphics;
using Xenko.Rendering;

namespace Xenko.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<StaticMeshColliderShapeDesc>))]
    [DataContract("StaticMeshColliderShapeDesc")]
    [Display(500, "Static Mesh")]
    public class StaticMeshColliderShapeDesc : IInlineColliderShapeDesc
    {
        /// <userdoc>
        /// Model asset from which the engine will derive the collider shape.
        /// </userdoc>
        [DataMember(10)]
        public Model Model;

        /// <userdoc>
        /// The local offset of the collider shape.
        /// </userdoc>
        [DataMember(20)]
        public Vector3 LocalOffset;

        /// <userdoc>
        /// The local rotation of the collider shape.
        /// </userdoc>
        [DataMember(30)]
        public Quaternion LocalRotation = Quaternion.Identity;

        /// <userdoc>
        /// The scaling of the collider shape.
        /// </userdoc>
        [DataMember(40)]
        public Vector3 Scaling = Vector3.One;


        public bool Match(object obj)
        {
            if (obj is StaticMeshColliderShapeDesc other)
            {
                return other.Model == Model
                    && other.LocalOffset == LocalOffset
                    && other.LocalRotation == LocalRotation 
                    && other.Scaling == Scaling;
            }

            return false;
        }

        public ColliderShape NewShapeFromDesc()
        {
            if(Model == null)
                return null;
            
            int[] indices;
            Vector3[] vertices;
            {
                int totalIndices = 0;
                int totalVerts = 0;

                foreach(var mesh in Model.Meshes)
                {
                    foreach(var bufferBinding in mesh.Draw.VertexBuffers)
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
            
            int iCollOffset = 0;
            int vCollOffset = 0;
            foreach(var mesh in Model.Meshes)
            {
                var commandList = (CommandList)typeof(GraphicsDevice).GetField("InternalMainCommandList",
                    System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.GetField
                    | System.Reflection.BindingFlags.FlattenHierarchy).GetValue(mesh.Draw.IndexBuffer.Buffer.GraphicsDevice);
                
                foreach(var bufferBinding in mesh.Draw.VertexBuffers)
                {
                    // Take care of the index buffer
                    unsafe
                    {
                        var binding = mesh.Draw.IndexBuffer;
                        var buffer = binding.Buffer;
                        var elementCount = binding.Count;
                        var sizeInBytes = buffer.Description.SizeInBytes;

                        byte* window = stackalloc byte[sizeInBytes];
                        FetchBufferData(buffer, commandList, new DataPointer(window, sizeInBytes));
                        window += binding.Offset;

                        if (binding.Is32Bit)
                        {
                            // For multiple meshes, indices have to be offset
                            // since we're merging all the meshes together
                            int* shortPtr = (int*)window;
                            for (int i = 0; i < elementCount; i++)
                            {
                                indices[iCollOffset++] = vCollOffset + shortPtr[i];
                            }
                        }
                        // convert ushort gpu representation to uint
                        else
                        {
                            ushort* shortPtr = (ushort*)window;
                            for (int i = 0; i < elementCount; i++)
                            {
                                indices[iCollOffset++] = vCollOffset + shortPtr[i];
                            }
                        }
                    }
                    
                    int stride = 0;
                    (int offset, VertexElement decl) posData;
                    // Find position within struct and buffer
                    {
                        int tempOffset = 0;
                        (int offset, VertexElement decl)? posDataNullable = null;
                        foreach(var elemDecl in bufferBinding.Declaration.VertexElements)
                        {
                            if(elemDecl.SemanticName.Equals("POSITION", StringComparison.Ordinal))
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
                        
                        if(posDataNullable == null)
                            throw new Exception($"No position data within {mesh}'s {nameof(mesh.Draw.VertexBuffers)}");
                        
                        posData = posDataNullable.Value;
                    }
                    
                    // Fetch vertex position data from GPU
                    unsafe
                    {
                        var sizeInBytes = bufferBinding.Buffer.Description.SizeInBytes;
                        var elementCount = bufferBinding.Count;
                        
                        byte* window = stackalloc byte[sizeInBytes];
                        FetchBufferData(bufferBinding.Buffer, commandList, new DataPointer(window, sizeInBytes));

                        window += bufferBinding.Offset;
                        
                        if (posData.decl.Format != PixelFormat.R32G32B32_Float)
                            throw new NotImplementedException(posData.decl.Format.ToString());
                        
                        for(int i = 0; i < elementCount; i++)
                        {
                            byte* vStart = &window[i * stride + posData.offset];
                            vertices[vCollOffset++] = *(Vector3*)vStart;
                        }
                    }
                }
            }

            for(int i = 0; i < vertices.Length; i++)
            {
                LocalRotation.Rotate(ref vertices[i]);
                vertices[i] += LocalOffset;
            }

            return new StaticMeshColliderShape(vertices, indices, Scaling);
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


    }
}
