// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.Physics
{
    public class StaticMeshColliderShape : ColliderShape
    {
        /// <summary> Can be null when this was created without Model </summary>
        [CanBeNull]
        public readonly Model Model;
        
        private readonly SharedMeshData sharedData;
        
        private static readonly Dictionary<string, SharedMeshData> MeshSharingCache = new();
        
        /// <summary>
        /// Create a static collider from an asset model, any changes the model receives won't be reflected on the collider
        /// </summary>
        public StaticMeshColliderShape(Model model, IServiceRegistry services) : this(BuildAndShareMeshes(model, services)) => this.Model = model;
        
        /// <summary>
        /// Create a static collider from the data provided, data will only be read, changes to it
        /// won't be reflected on the collider.
        /// </summary>
        public StaticMeshColliderShape(ICollection<Vector3> vertices, ICollection<int> indices) 
            : this(new SharedMeshData
            {
                BulletMesh = new BulletSharp.TriangleIndexVertexArray(indices, new StrideToBulletWrapper(vertices))
            })
        {
        }
        
        StaticMeshColliderShape(SharedMeshData sharedDataParam)
        {
            sharedData = sharedDataParam;
            Type = ColliderShapeTypes.StaticMesh;
            Is2D = false;

            InternalShape = new BulletSharp.BvhTriangleMeshShape(sharedDataParam.BulletMesh, true);
            DebugPrimitiveMatrix = Matrix.Scaling(Vector3.One * DebugScaling);
        }

        public override void Dispose()
        {
            base.Dispose();
            if (sharedData.Key == null)
            {
                // Not actually shared, dispose and move on
                sharedData.BulletMesh.Dispose();
                return;
            }

            lock (MeshSharingCache)
            {
                sharedData.RefCount--;
                if (sharedData.RefCount == 0)
                {
                    MeshSharingCache.Remove(sharedData.Key);
                    sharedData.BulletMesh.Dispose();
                }
            }
        }
        
        public unsafe void GetMeshDataCopy(out Vector3[] verts, out int[] indices)
        {
            var iMesh = sharedData.BulletMesh.IndexedMeshArray[0];
            {
                int lengthInBytes = iMesh.NumVertices * iMesh.VertexStride;
                verts = new Span<Vector3>( (void*)iMesh.VertexBase, lengthInBytes / sizeof(Vector3) ).ToArray();
            }
            {
                int lengthInBytes = iMesh.NumTriangles * iMesh.TriangleIndexStride;
                indices = new Span<int>( (void*)iMesh.TriangleIndexBase, lengthInBytes / sizeof(int) ).ToArray();
            }
        }
        
        public override MeshDraw CreateDebugPrimitive(GraphicsDevice device)
        {
            GetMeshDataCopy(out var verts, out var indices);
            var vPos = new VertexPositionNormalTexture[verts.Length];
            for (int i = 0; i < vPos.Length; i++)
                vPos[i].Position = verts[i];
            
            var meshData = new GeometricMeshData<VertexPositionNormalTexture>(vPos, indices, false);
            return new GeometricPrimitive(device, meshData).ToMeshDraw();
        }

        static unsafe SharedMeshData BuildAndShareMeshes(Model model, IServiceRegistry services)
        {
            var sharedContent = services.GetService<ContentManager>();

            string modelUrl = null;
            if (sharedContent != null && sharedContent.TryGetAssetUrl(model, out modelUrl))
            {
                lock (MeshSharingCache)
                {
                    if (MeshSharingCache.TryGetValue(modelUrl, out var sharedData))
                    {
                        sharedData.RefCount++;
                        return sharedData;
                    }
                }
            }
            
            var dbProvider = services.GetService<IDatabaseFileProviderService>();
            ContentManager rawContent = null;
            
            Matrix[] nodeTransforms = null;
            if (model.Skeleton != null)
            {
                var nodesLength = model.Skeleton.Nodes.Length;
                nodeTransforms = new Matrix[nodesLength];
                nodeTransforms[0] = Matrix.Identity;
                for (var i = 0; i < nodesLength; i++)
                {
                    var node = model.Skeleton.Nodes[i];
                    Matrix.Transformation(ref node.Transform.Scale, ref node.Transform.Rotation, ref node.Transform.Position, out var localMatrix);

                    Matrix worldMatrix;
                    if (node.ParentIndex != -1)
                    {
                        if (node.ParentIndex >= i)
                            throw new InvalidOperationException("Skeleton nodes are not sorted");
                        var nodeTransform = nodeTransforms[node.ParentIndex];
                        Matrix.Multiply(ref localMatrix, ref nodeTransform, out worldMatrix);
                    }
                    else
                    {
                        worldMatrix = localMatrix;
                    }

                    if (i != 0)
                    {
                        nodeTransforms[i] = worldMatrix;
                    }
                }
            }

            int totalVerts = 0, totalIndices = 0;
            foreach (var meshData in model.Meshes)
            {
                totalVerts += meshData.Draw.VertexBuffers[0].Count;
                totalIndices += meshData.Draw.IndexBuffer.Count;
            }

            var combinedVerts = new List<Vector3>(totalVerts);
            var combinedIndices = new List<int>(totalIndices);

            foreach (var meshData in model.Meshes)
            {
                var vBuffer = meshData.Draw.VertexBuffers[0].Buffer;
                var iBuffer = meshData.Draw.IndexBuffer.Buffer;
                byte[] verticesBytes = TryFetchBufferContent(vBuffer, ref rawContent, sharedContent, dbProvider);
                byte[] indicesBytes = TryFetchBufferContent(iBuffer, ref rawContent, sharedContent, dbProvider);

                if((verticesBytes?.Length ?? 0) == 0 || (indicesBytes?.Length ?? 0) == 0)
                {
                    throw new InvalidOperationException(
                        $"Failed to find mesh buffers while attempting to build a {nameof(StaticMeshColliderShape)}. " +
                        $"Make sure that the {nameof(model)} is either an asset on disk, or has its buffer data attached to the buffer through '{nameof(AttachedReference)}'\n" +
                        $"You can also explicitly build a {nameof(StaticMeshColliderShape)} using the second constructor instead of this one.");
                }

                int vertMappingStart = combinedVerts.Count;

                fixed (byte* bytePtr = verticesBytes)
                {
                    var vBindings = meshData.Draw.VertexBuffers[0];
                    int count = vBindings.Count;
                    int stride = vBindings.Declaration.VertexStride;
                    for (int i = 0, vHead = vBindings.Offset; i < count; i++, vHead += stride)
                    {
                        var pos = *(Vector3*)(bytePtr + vHead);
                        if (nodeTransforms != null)
                        {
                            Matrix posMatrix = Matrix.Translation(pos);
                            Matrix.Multiply(ref posMatrix, ref nodeTransforms[meshData.NodeIndex], out var finalMatrix);
                            pos = finalMatrix.TranslationVector;
                        }

                        combinedVerts.Add(pos);
                    }
                }

                fixed (byte* bytePtr = indicesBytes)
                {
                    if (meshData.Draw.IndexBuffer.Is32Bit)
                    {
                        foreach (int i in new Span<int>(bytePtr + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                        {
                            combinedIndices.Add(vertMappingStart + i);
                        }
                    }
                    else
                    {
                        foreach (ushort i in new Span<ushort>(bytePtr + meshData.Draw.IndexBuffer.Offset, meshData.Draw.IndexBuffer.Count))
                        {
                            combinedIndices.Add(vertMappingStart + i);
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(modelUrl))
            {
                return new SharedMeshData
                {
                    BulletMesh = new BulletSharp.TriangleIndexVertexArray(combinedIndices, new StrideToBulletWrapper(combinedVerts))
                };
            }
            
            lock (MeshSharingCache)
            {
                if (MeshSharingCache.TryGetValue(modelUrl, out var sharedData))
                {
                    // Another thread was building this concurrently and it finished before us,
                    // nothing to cleanup so we can just use theirs and move on.
                    sharedData.RefCount++;
                    return sharedData;
                }
                
                sharedData = new SharedMeshData
                {
                    BulletMesh = new BulletSharp.TriangleIndexVertexArray(combinedIndices, new StrideToBulletWrapper(combinedVerts)),
                    Key = modelUrl,
                    RefCount = 1,
                };
                MeshSharingCache.Add(modelUrl, sharedData);
                return sharedData;
            }
        }
        
        static unsafe byte[] TryFetchBufferContent(Graphics.Buffer buffer, ref ContentManager rawContent, ContentManager sharedContent, IDatabaseFileProviderService dbProvider)
        {
            byte[] output;
            var bufRef = AttachedReferenceManager.GetAttachedReference(buffer);
            if (bufRef.Data != null && (output = ((BufferData)bufRef.Data).Content) != null)
                return output;
            
            // Editor-specific workaround, we can't load assets when the file provider is null,
            // will most likely break on non-dx11 APIs
            if (dbProvider != null && dbProvider.FileProvider == null)
            {
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var commandList = (CommandList)typeof(GraphicsDevice)
                    .GetField("InternalMainCommandList", flags)
                    .GetValue(buffer.GraphicsDevice);

                output = new byte[buffer.SizeInBytes];
                fixed (byte* window = output)
                {
                    var ptr = new DataPointer(window, output.Length);
                    if (buffer.Description.Usage == GraphicsResourceUsage.Staging)
                    {
                        // Directly if this is a staging resource
                        buffer.GetData(commandList, buffer, ptr);
                    }
                    else
                    {
                        // inefficient way to use the Copy method using dynamic staging texture
                        using var throughStaging = buffer.ToStaging();
                        buffer.GetData(commandList, throughStaging, ptr);
                    }
                }

                return output;
            }

            if (sharedContent.TryGetAssetUrl(buffer, out var url))
            {
                rawContent ??= new ContentManager(dbProvider);
                var data = rawContent.Load<Graphics.Buffer>(url);
                try
                {
                    return data.GetSerializationData().Content;
                }
                finally
                {
                    rawContent.Unload(url);
                }
            }

            return null;
        }

        private record SharedMeshData
        {
            public BulletSharp.TriangleIndexVertexArray BulletMesh;
            public int RefCount;
            public string Key;
        }
        
        private class StrideToBulletWrapper : ICollection<BulletSharp.Math.Vector3>
        {
            private readonly ICollection<Vector3> internalColl;
            public StrideToBulletWrapper(ICollection<Vector3> collectionToConvert)
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

            public void Add(BulletSharp.Math.Vector3 item) => throw new InvalidOperationException("Collection is read only");
            public bool Remove(BulletSharp.Math.Vector3 item) => throw new InvalidOperationException("Collection is read only");
            public void Clear() => throw new InvalidOperationException("Collection is read only");

            public IEnumerator<BulletSharp.Math.Vector3> GetEnumerator()
            {
                foreach (var value in internalColl)
                    yield return value;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
