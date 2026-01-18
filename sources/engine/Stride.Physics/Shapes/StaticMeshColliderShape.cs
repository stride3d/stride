// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Graphics.Semantics;
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
                sharedData.Dispose();
                return;
            }

            lock (MeshSharingCache)
            {
                sharedData.RefCount--;
                if (sharedData.RefCount == 0)
                {
                    MeshSharingCache.Remove(sharedData.Key);
                    sharedData.Dispose();
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

        static SharedMeshData BuildAndShareMeshes(Model model, IServiceRegistry services)
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

            var combinedVerts = new Vector3[totalVerts];
            var combinedIndices = new int[totalIndices];
            var verticesLeft = combinedVerts.AsSpan();
            var indicesLeft = combinedIndices.AsSpan();

            int indexOffset = 0;
            foreach (var meshData in model.Meshes)
            {
                meshData.Draw.VertexBuffers[0].AsReadable(services, out var vertexHelper, out var vertexCount);
                meshData.Draw.IndexBuffer.AsReadable(services, out var indexHelper, out var indexCount);

                var vertSlice = verticesLeft[..vertexCount];
                vertexHelper.Copy<PositionSemantic, Vector3>(vertSlice);

                if (nodeTransforms != null)
                {
                    for (int i = 0; i < vertSlice.Length; i++)
                    {
                        Matrix posMatrix = Matrix.Translation(vertSlice[i]);
                        Matrix.Multiply(ref posMatrix, ref nodeTransforms[meshData.NodeIndex], out var finalMatrix);
                        vertSlice[i] = finalMatrix.TranslationVector;
                    }
                }

                var indicesForSlice = indicesLeft[..indexCount];
                indexHelper.CopyTo(indicesForSlice);
                for (int i = 0; i < indicesForSlice.Length; i++)
                    indicesForSlice[i] += indexOffset;
                indexOffset += vertexCount;

                verticesLeft = verticesLeft[vertexCount..];
                indicesLeft = indicesLeft[indexCount..];
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

        private record SharedMeshData : IDisposable
        {
            public BulletSharp.TriangleIndexVertexArray BulletMesh;
            public int RefCount;
            public string Key;

            public void Dispose()
            {
                BulletMesh.IndexedMeshArray.Clear();
                BulletMesh.Dispose();
            }
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
