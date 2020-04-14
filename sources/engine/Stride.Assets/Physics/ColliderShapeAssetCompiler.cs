// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Rendering;
using Stride.Graphics.Data;
using Stride.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Serialization.Contents;
using Stride.Assets.Textures;
using VHACDSharp;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Assets.Physics
{
    [AssetCompiler(typeof(ColliderShapeAsset), typeof(AssetCompilationContext))]
    internal class ColliderShapeAssetCompiler : AssetCompilerBase
    {
        static ColliderShapeAssetCompiler()
        {
            NativeLibrary.PreloadLibrary("VHACD.dll", typeof(ColliderShapeAssetCompiler));
        }

        public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
        {
            foreach (var type in AssetRegistry.GetAssetTypes(typeof(Model)))
            {
                yield return new BuildDependencyInfo(type, typeof(AssetCompilationContext), BuildDependencyType.CompileContent);
            }
            foreach (var type in AssetRegistry.GetAssetTypes(typeof(Skeleton)))
            {
                yield return new BuildDependencyInfo(type, typeof(AssetCompilationContext), BuildDependencyType.CompileContent);
            }
            foreach (var type in AssetRegistry.GetAssetTypes(typeof(Heightmap)))
            {
                yield return new BuildDependencyInfo(type, typeof(AssetCompilationContext), BuildDependencyType.CompileContent);
            }
        }

        public override IEnumerable<Type> GetInputTypesToExclude(AssetItem assetItem)
        {
            foreach(var type in AssetRegistry.GetAssetTypes(typeof(Material)))
            {
                yield return type;
            }
            yield return typeof(TextureAsset);
        }

        public override IEnumerable<ObjectUrl> GetInputFiles(AssetItem assetItem)
        {
            var asset = (ColliderShapeAsset)assetItem.Asset;
            foreach (var desc in asset.ColliderShapes)
            {
                if (desc is ConvexHullColliderShapeDesc)
                {
                    var convexHullDesc = desc as ConvexHullColliderShapeDesc;

                    if (convexHullDesc.Model != null)
                    {
                        var url = AttachedReferenceManager.GetUrl(convexHullDesc.Model);

                        if (!string.IsNullOrEmpty(url))
                        {
                            yield return new ObjectUrl(UrlType.Content, url);
                        }
                    }
                }
                else if (desc is HeightfieldColliderShapeDesc)
                {
                    var heightfieldDesc = desc as HeightfieldColliderShapeDesc;
                    var heightmapSource = heightfieldDesc?.HeightStickArraySource as HeightStickArraySourceFromHeightmap;

                    if (heightmapSource?.Heightmap != null)
                    {
                        var url = AttachedReferenceManager.GetUrl(heightmapSource.Heightmap);

                        if (!string.IsNullOrEmpty(url))
                        {
                            yield return new ObjectUrl(UrlType.Content, url);
                        }
                    }
                }
            }
        }

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (ColliderShapeAsset)assetItem.Asset;

            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new ColliderShapeCombineCommand(targetUrlInStorage, asset, assetItem.Package) { InputFilesGetter = () => GetInputFiles(assetItem) });
        }

        public class ColliderShapeCombineCommand : AssetCommand<ColliderShapeAsset>
        {
            public ColliderShapeCombineCommand(string url, ColliderShapeAsset parameters, IAssetFinder assetFinder)
                : base(url, parameters, assetFinder)
            {
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);

                // Cloned list of collider shapes
                var descriptions = Parameters.ColliderShapes.ToList();

                var validShapes = Parameters.ColliderShapes.Where(x => x != null
                    && (x.GetType() != typeof(ConvexHullColliderShapeDesc) || ((ConvexHullColliderShapeDesc)x).Model != null)).ToList();

                //pre process special types
                foreach (var convexHullDesc in
                    (from shape in validShapes let type = shape.GetType() where type == typeof(ConvexHullColliderShapeDesc) select shape)
                    .Cast<ConvexHullColliderShapeDesc>())
                {
                    // Clone the convex hull shape description so the fields that should not be serialized can be cleared (Model in this case)
                    ConvexHullColliderShapeDesc convexHullDescClone = new ConvexHullColliderShapeDesc
                    {
                        Scaling = convexHullDesc.Scaling,
                        LocalOffset = convexHullDesc.LocalOffset,
                        LocalRotation = convexHullDesc.LocalRotation,
                        Decomposition = convexHullDesc.Decomposition,
                    };

                    // Replace shape in final result with cloned description
                    int replaceIndex = descriptions.IndexOf(convexHullDesc);
                    descriptions[replaceIndex] = convexHullDescClone;

                    var loadSettings = new ContentManagerLoaderSettings
                    {
                        ContentFilter = ContentManagerLoaderSettings.NewContentFilterByType(typeof(Mesh), typeof(Skeleton))
                    };
                    
                    var modelAsset = assetManager.Load<Model>(AttachedReferenceManager.GetUrl(convexHullDesc.Model), loadSettings);
                    if (modelAsset == null) continue;

                    convexHullDescClone.ConvexHulls = new List<List<List<Vector3>>>();
                    convexHullDescClone.ConvexHullsIndices = new List<List<List<uint>>>();

                    commandContext.Logger.Info("Processing convex hull generation, this might take a while!");

                    var nodeTransforms = new List<Matrix>();

                    //pre-compute all node transforms, assuming nodes are ordered... see ModelViewHierarchyUpdater
                    
                    if (modelAsset.Skeleton == null)
                    {
                        Matrix baseMatrix;
                        Matrix.Transformation(ref convexHullDescClone.Scaling, ref convexHullDescClone.LocalRotation, ref convexHullDescClone.LocalOffset, out baseMatrix);
                        nodeTransforms.Add(baseMatrix);
                    }
                    else
                    {
                        var nodesLength = modelAsset.Skeleton.Nodes.Length;
                        for (var i = 0; i < nodesLength; i++)
                        {
                            Matrix localMatrix;
                            Matrix.Transformation(
                                ref modelAsset.Skeleton.Nodes[i].Transform.Scale,
                                ref modelAsset.Skeleton.Nodes[i].Transform.Rotation,
                                ref modelAsset.Skeleton.Nodes[i].Transform.Position, out localMatrix);

                            Matrix worldMatrix;
                            if (modelAsset.Skeleton.Nodes[i].ParentIndex != -1)
                            {
                                var nodeTransform = nodeTransforms[modelAsset.Skeleton.Nodes[i].ParentIndex];
                                Matrix.Multiply(ref localMatrix, ref nodeTransform, out worldMatrix);
                            }
                            else
                            {
                                worldMatrix = localMatrix;
                            }

                            if (i == 0)
                            {
                                Matrix baseMatrix;
                                Matrix.Transformation(ref convexHullDescClone.Scaling, ref convexHullDescClone.LocalRotation, ref convexHullDescClone.LocalOffset, out baseMatrix);
                                nodeTransforms.Add(baseMatrix*worldMatrix);
                            }
                            else
                            {
                                nodeTransforms.Add(worldMatrix); 
                            }                           
                        }
                    }

                    for (var i = 0; i < nodeTransforms.Count; i++)
                    {
                        var i1 = i;
                        if (modelAsset.Meshes.All(x => x.NodeIndex != i1)) continue; // no geometry in the node

                        var combinedVerts = new List<float>();
                        var combinedIndices = new List<uint>();

                        var hullsList = new List<List<Vector3>>();
                        convexHullDescClone.ConvexHulls.Add(hullsList);

                        var indicesList = new List<List<uint>>();
                        convexHullDescClone.ConvexHullsIndices.Add(indicesList);

                        foreach (var meshData in modelAsset.Meshes.Where(x => x.NodeIndex == i1))
                        {
                            var indexOffset = (uint)combinedVerts.Count / 3;

                            var stride = meshData.Draw.VertexBuffers[0].Declaration.VertexStride;

                            var vertexBufferRef = AttachedReferenceManager.GetAttachedReference(meshData.Draw.VertexBuffers[0].Buffer);
                            byte[] vertexData;
                            if (vertexBufferRef.Data != null)
                            {
                                vertexData = ((BufferData)vertexBufferRef.Data).Content;
                            }
                            else if (!string.IsNullOrEmpty(vertexBufferRef.Url))
                            {
                                var dataAsset = assetManager.Load<Buffer>(vertexBufferRef.Url);
                                vertexData = dataAsset.GetSerializationData().Content;
                            }
                            else
                            {
                                continue;
                            }

                            var vertexIndex = meshData.Draw.VertexBuffers[0].Offset;
                            for (var v = 0; v < meshData.Draw.VertexBuffers[0].Count; v++)
                            {
                                var posMatrix = Matrix.Translation(new Vector3(BitConverter.ToSingle(vertexData, vertexIndex + 0), BitConverter.ToSingle(vertexData, vertexIndex + 4), BitConverter.ToSingle(vertexData, vertexIndex + 8)));

                                Matrix rotatedMatrix;
                                var nodeTransform = nodeTransforms[i];
                                Matrix.Multiply(ref posMatrix, ref nodeTransform, out rotatedMatrix);

                                combinedVerts.Add(rotatedMatrix.TranslationVector.X);
                                combinedVerts.Add(rotatedMatrix.TranslationVector.Y);
                                combinedVerts.Add(rotatedMatrix.TranslationVector.Z);

                                vertexIndex += stride;
                            }

                            var indexBufferRef = AttachedReferenceManager.GetAttachedReference(meshData.Draw.IndexBuffer.Buffer);
                            byte[] indexData;
                            if (indexBufferRef.Data != null)
                            {
                                indexData = ((BufferData)indexBufferRef.Data).Content;
                            }
                            else if (!string.IsNullOrEmpty(indexBufferRef.Url))
                            {
                                var dataAsset = assetManager.Load<Buffer>(indexBufferRef.Url);
                                indexData = dataAsset.GetSerializationData().Content;
                            }
                            else
                            {
                                throw new Exception("Failed to find index buffer while building a convex hull.");
                            }

                            var indexIndex = meshData.Draw.IndexBuffer.Offset;
                            for (var v = 0; v < meshData.Draw.IndexBuffer.Count; v++)
                            {
                                if (meshData.Draw.IndexBuffer.Is32Bit)
                                {
                                    combinedIndices.Add(BitConverter.ToUInt32(indexData, indexIndex) + indexOffset);
                                    indexIndex += 4;
                                }
                                else
                                {
                                    combinedIndices.Add(BitConverter.ToUInt16(indexData, indexIndex) + indexOffset);
                                    indexIndex += 2;
                                }
                            }
                        }

                        var decompositionDesc = new ConvexHullMesh.DecompositionDesc
                        {
                            VertexCount = (uint)combinedVerts.Count / 3,
                            IndicesCount = (uint)combinedIndices.Count,
                            Vertexes = combinedVerts.ToArray(),
                            Indices = combinedIndices.ToArray(),
                            Depth = convexHullDesc.Decomposition.Depth,
                            PosSampling = convexHullDesc.Decomposition.PosSampling,
                            PosRefine = convexHullDesc.Decomposition.PosRefine,
                            AngleSampling = convexHullDesc.Decomposition.AngleSampling,
                            AngleRefine = convexHullDesc.Decomposition.AngleRefine,
                            Alpha = convexHullDesc.Decomposition.Alpha,
                            Threshold = convexHullDesc.Decomposition.Threshold,
                            SimpleHull = !convexHullDesc.Decomposition.Enabled,
                        };

                        var convexHullMesh = new ConvexHullMesh();

                        convexHullMesh.Generate(decompositionDesc);

                        var count = convexHullMesh.Count;

                        commandContext.Logger.Info("Node generated " + count + " convex hulls");

                        var vertexCountHull = 0;

                        for (uint h = 0; h < count; h++)
                        {
                            float[] points;
                            convexHullMesh.CopyPoints(h, out points);

                            var pointList = new List<Vector3>();

                            for (var v = 0; v < points.Length; v += 3)
                            {
                                var vert = new Vector3(points[v + 0], points[v + 1], points[v + 2]);
                                pointList.Add(vert);

                                vertexCountHull++;
                            }

                            hullsList.Add(pointList);

                            uint[] indices;
                            convexHullMesh.CopyIndices(h, out indices);

                            for (var t = 0; t < indices.Length; t += 3)
                            {
                                Core.Utilities.Swap(ref indices[t], ref indices[t + 2]);
                            }

                            var indexList = new List<uint>(indices);

                            indicesList.Add(indexList);
                        }

                        convexHullMesh.Dispose();

                        commandContext.Logger.Info("For a total of " + vertexCountHull + " vertexes");
                    }
                }

                var runtimeShape = new PhysicsColliderShape(descriptions);
                assetManager.Save(Url, runtimeShape);

                return Task.FromResult(ResultStatus.Successful);
            }
        }
    }
}
