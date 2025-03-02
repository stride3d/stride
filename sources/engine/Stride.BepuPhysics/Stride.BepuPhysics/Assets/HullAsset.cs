// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Assets;
using Stride.BepuPhysics.Definitions;
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Rendering;
using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Assets;
using Stride.Assets.Textures;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Graphics.Data;
using VHACDSharp;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.BepuPhysics.Assets
{
    [Display((int)AssetDisplayPriority.Physics, "Hull")]
    [DataContract("HullAsset")]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(DecomposedHulls))]
    [AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "2.0.0.0")]
    public class HullAsset : Asset
    {
        private const string CurrentVersion = "2.0.0.0";

        public const string FileExtension = ".sdhull";

        /// <summary>
        /// Multiple meshes -> Multiple Hulls per mesh
        /// </summary>
        [Display(Browsable = false)]
        [DataMember(10)]
        public List<List<DecomposedHulls.Hull>> ConvexHulls = null;

        /// <userdoc>
        /// Model asset from where the engine will derive the convex hull.
        /// </userdoc>
        [DataMember(30)]
        public Model Model; // Do note that this field is also assigned through reflection in HullAssetFactoryTemplateGenerator as a workaround

        /// <userdoc>
        /// The offset with the real graphic mesh.
        /// </userdoc>
        [DataMember(31)]
        public Vector3 LocalOffset;

        /// <userdoc>
        /// The local rotation of the collider shape.
        /// </userdoc>
        [DataMember(32)]
        public Quaternion LocalRotation = Quaternion.Identity;

        /// <userdoc>
        /// The scaling of the generated convex hull.
        /// </userdoc>
        [DataMember(45)]
        public Vector3 Scaling = Vector3.One;

        /// <summary>
        /// Parameters used when decomposing the given <see cref="Model"/> into a hull
        /// </summary>
        [DataMember(50)]
        [NotNull]
        public ConvexHullDecompositionParameters Decomposition { get; set; } = new ConvexHullDecompositionParameters();
    }

    [AssetCompiler(typeof(HullAsset), typeof(AssetCompilationContext))]
    internal class HullAssetCompiler : AssetCompilerBase
    {
        static HullAssetCompiler()
        {
            NativeLibraryHelper.PreloadLibrary("VHACD", typeof(HullAssetCompiler));
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
        }

        public override IEnumerable<Type> GetInputTypesToExclude(AssetItem assetItem)
        {
            foreach (var type in AssetRegistry.GetAssetTypes(typeof(Material)))
            {
                yield return type;
            }
            yield return typeof(TextureAsset);
        }

        public override IEnumerable<ObjectUrl> GetInputFiles(AssetItem assetItem)
        {
            var asset = (HullAsset)assetItem.Asset;
            if (asset.Model != null)
            {
                var url = AttachedReferenceManager.GetUrl(asset.Model);

                if (!string.IsNullOrEmpty(url))
                {
                    yield return new ObjectUrl(UrlType.Content, url);
                }
            }
        }

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (HullAsset)assetItem.Asset;

            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new HullDecomposition(targetUrlInStorage, asset, assetItem.Package) { InputFilesGetter = () => GetInputFiles(assetItem) });
        }

        public class HullDecomposition : AssetCommand<HullAsset>
        {
            public HullDecomposition(string url, HullAsset parameters, IAssetFinder assetFinder)
                : base(url, parameters, assetFinder)
            {
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);

                if (Parameters.Model != null)
                {
                    var loadSettings = new ContentManagerLoaderSettings
                    {
                        ContentFilter = ContentManagerLoaderSettings.NewContentFilterByType(typeof(Mesh), typeof(Skeleton))
                    };

                    var modelAsset = assetManager.Load<Model>(AttachedReferenceManager.GetUrl(Parameters.Model), loadSettings);

                    commandContext.Logger.Info("Processing convex hull generation, this might take a while!");

                    var nodeTransforms = new List<Matrix>();

                    //pre-compute all node transforms, assuming nodes are ordered... see ModelViewHierarchyUpdater

                    if (modelAsset.Skeleton == null)
                    {
                        Matrix baseMatrix;
                        Matrix.Transformation(ref Parameters.Scaling, ref Parameters.LocalRotation, ref Parameters.LocalOffset, out baseMatrix);
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
                                Matrix.Transformation(ref Parameters.Scaling, ref Parameters.LocalRotation, ref Parameters.LocalOffset, out baseMatrix);
                                nodeTransforms.Add(baseMatrix * worldMatrix);
                            }
                            else
                            {
                                nodeTransforms.Add(worldMatrix);
                            }
                        }
                    }

                    Parameters.ConvexHulls = new();
                    for (var i = 0; i < nodeTransforms.Count; i++)
                    {
                        var i1 = i;
                        if (modelAsset.Meshes.All(x => x.NodeIndex != i1)) continue; // no geometry in the node

                        var combinedVerts = new List<float>();
                        var combinedIndices = new List<uint>();

                        var hullsList = new List<DecomposedHulls.Hull>();
                        Parameters.ConvexHulls.Add(hullsList);

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
                            Depth = Parameters.Decomposition.Depth,
                            PosSampling = Parameters.Decomposition.PosSampling,
                            PosRefine = Parameters.Decomposition.PosRefine,
                            AngleSampling = Parameters.Decomposition.AngleSampling,
                            AngleRefine = Parameters.Decomposition.AngleRefine,
                            Alpha = Parameters.Decomposition.Alpha,
                            Threshold = Parameters.Decomposition.Threshold,
                            SimpleHull = !Parameters.Decomposition.Enabled,
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

                            var pointsV3 = new Vector3[points.Length / 3];

                            for (var v = 0; v < points.Length; v += 3)
                            {
                                pointsV3[v / 3] = new Vector3(points[v + 0], points[v + 1], points[v + 2]);

                                vertexCountHull++;
                            }

                            uint[] indices;
                            convexHullMesh.CopyIndices(h, out indices);

                            for (var t = 0; t < indices.Length; t += 3)
                            {
                                Core.Utilities.Swap(ref indices[t], ref indices[t + 2]);
                            }

                            hullsList.Add(new DecomposedHulls.Hull(pointsV3, indices));
                        }

                        convexHullMesh.Dispose();

                        commandContext.Logger.Info("For a total of " + vertexCountHull + " vertices");
                    }
                }

                var runtimeShape = new DecomposedHulls(Parameters.ConvexHulls.Select(x => new DecomposedHulls.DecomposedMesh(x.ToArray())).ToArray());
                assetManager.Save(Url, runtimeShape);
                Parameters.ConvexHulls = null;

                return Task.FromResult(ResultStatus.Successful);
            }
        }
    }

    internal class HullAssetFactory : AssetFactory<HullAsset>
    {
        public static HullAsset Create()
        {
            return new HullAsset();
        }

        public override HullAsset New()
        {
            return Create();
        }
    }

    [ContentSerializer(typeof(DataContentSerializer<ConvexHullDecompositionParameters>))]
    [DataContract("BepuDecompositionParameters")]
    [Display("DecompositionParameters")]
    public class ConvexHullDecompositionParameters
    {
        /// <userdoc>
        /// If this is unchecked the following parameters are totally ignored, as only a simple convex hull of the whole model will be generated.
        /// </userdoc>
        public bool Enabled { get; set; }

        /// <userdoc>
        /// Control how many sub convex hulls will be created, more depth will result in a more complex decomposition.
        /// </userdoc>
        [DataMember(60)]
        public int Depth { get; set; } = 10;

        /// <userdoc>
        /// How many position samples to internally compute clipping planes ( the higher the more complex ).
        /// </userdoc>
        [DataMember(70)]
        public int PosSampling { get; set; } = 10;

        /// <userdoc>
        /// How many angle samples to internally compute clipping planes ( the higher the more complex ), nested with position samples, for each position sample it will compute the amount defined here.
        /// </userdoc>
        [DataMember(80)]
        public int AngleSampling { get; set; } = 10;

        /// <userdoc>
        /// If higher then 0 the computation will try to further improve the shape position sampling (this will slow down the process).
        /// </userdoc>
        [DataMember(90)]
        public int PosRefine { get; set; } = 5;

        /// <userdoc>
        /// If higher then 0 the computation will try to further improve the shape angle sampling (this will slow down the process).
        /// </userdoc>
        [DataMember(100)]
        public int AngleRefine { get; set; } = 5;

        /// <userdoc>
        /// Applied to the concavity during crippling plane approximation.
        /// </userdoc>
        [DataMember(110)]
        public float Alpha { get; set; } = 0.01f;

        /// <userdoc>
        /// Threshold of concavity, rising this will make the shape simpler.
        /// </userdoc>
        [DataMember(120)]
        public float Threshold { get; set; } = 0.01f;

        public bool Match(object obj)
        {
            var other = obj as ConvexHullDecompositionParameters;

            if (other == null)
            {
                return false;
            }

            return other.Enabled == Enabled &&
                other.Depth == Depth &&
                other.PosSampling == PosSampling &&
                other.AngleSampling == AngleSampling &&
                other.PosRefine == PosRefine &&
                other.AngleRefine == AngleRefine &&
                MathF.Abs(other.Alpha - Alpha) < float.Epsilon &&
                MathF.Abs(other.Threshold - Threshold) < float.Epsilon;
        }
    }
}
