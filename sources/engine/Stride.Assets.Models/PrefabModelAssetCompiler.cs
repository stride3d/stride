// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Assets.Entities;
using Stride.Engine;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Assets.Models
{
    [AssetCompiler(typeof(PrefabModelAsset), typeof(AssetCompilationContext))]
    internal class PrefabModelAssetCompiler : AssetCompilerBase
    {
        public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
        {
            // We need to read the prefab asset to collect models
            yield return new BuildDependencyInfo(typeof(PrefabAsset), typeof(AssetCompilationContext), BuildDependencyType.CompileAsset);
            foreach (var type in AssetRegistry.GetAssetTypes(typeof(Model)))
            {
                yield return new BuildDependencyInfo(type, typeof(AssetCompilationContext), BuildDependencyType.CompileContent);
            }
        }

        public override IEnumerable<ObjectUrl> GetInputFiles(AssetItem assetItem)
        {
            var asset = (PrefabModelAsset)assetItem.Asset;
            var prefab = assetItem.Package.Session.FindAssetFromProxyObject(asset.Prefab)?.Asset as PrefabAsset;
            if (prefab != null)
            {
                foreach (var entity in prefab.Hierarchy.Parts.Values.Select(x => x.Entity))
                {
                    var modelComponent = entity.Get<ModelComponent>();
                    if (modelComponent == null)
                        continue;

                    var model = assetItem.Package.Session.FindAssetFromProxyObject(modelComponent.Model);
                    if (model == null)
                        continue;

                    // We need any model to be compiled before generating the prefab model
                    yield return new ObjectUrl(UrlType.Content, model.Location);

                    // We need all materials to be compiled before generating the prefab model
                    foreach (var material in modelComponent.Materials.Values.Select(x => assetItem.Package.Session.FindAssetFromProxyObject(x)).NotNull())
                    {
                        yield return new ObjectUrl(UrlType.Content, material.Location);
                    }
                    foreach (var material in ((IModelAsset)model.Asset).Materials.Select(x => assetItem.Package.Session.FindAssetFromProxyObject(x.MaterialInstance.Material)).NotNull())
                    {
                        yield return new ObjectUrl(UrlType.Content, material.Location);
                    }
                }
            }
        }

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (PrefabModelAsset)assetItem.Asset;
            var renderingSettings = context.GetGameSettingsAsset().GetOrCreate<RenderingSettings>();
            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new PrefabModelAssetCompileCommand(targetUrlInStorage, asset, assetItem, renderingSettings));
        }

        private class PrefabModelAssetCompileCommand : AssetCommand<PrefabModelAsset>
        {
            private readonly RenderingSettings renderingSettings;

            public PrefabModelAssetCompileCommand(string url, PrefabModelAsset parameters, AssetItem assetItem, RenderingSettings renderingSettings)
                : base(url, parameters, assetItem.Package)
            {
                this.renderingSettings = renderingSettings;
            }

            protected override void ComputeParameterHash(BinarySerializationWriter writer)
            {
                base.ComputeParameterHash(writer);

                var prefabAsset = AssetFinder.FindAssetFromProxyObject(Parameters.Prefab);
                if (prefabAsset != null)
                {
                    writer.Write(prefabAsset.Version);
                }
            }

            private class MeshData
            {
                public readonly List<byte> VertexData = new List<byte>();
                public int VertexStride;

                public readonly List<byte> IndexData = new List<byte>();
                public int IndexOffset;
            }

            private struct EntityChunk
            {
                public Entity Entity;
                public Model Model;
                public int MaterialIndex;
            }

            private static unsafe void ProcessMaterial(ContentManager manager, ICollection<EntityChunk> chunks, MaterialInstance material, Model prefabModel)
            {
                //we need to futher group by VertexDeclaration
                var meshes = new Dictionary<VertexDeclaration, MeshData>();

                //actually create the mesh
                foreach (var chunk in chunks)
                {
                    foreach (var modelMesh in chunk.Model.Meshes)
                    {
                        //process only right material
                        if (modelMesh.MaterialIndex == chunk.MaterialIndex)
                        {
                            MeshData mesh;
                            if (!meshes.TryGetValue(modelMesh.Draw.VertexBuffers[0].Declaration, out mesh))
                            {
                                mesh = new MeshData { VertexStride = modelMesh.Draw.VertexBuffers[0].Stride };
                                meshes.Add(modelMesh.Draw.VertexBuffers[0].Declaration, mesh);
                            }

                            //vertexes
                            var vertexBufferRef = AttachedReferenceManager.GetAttachedReference(modelMesh.Draw.VertexBuffers[0].Buffer);
                            byte[] vertexData;
                            if (vertexBufferRef.Data != null)
                            {
                                vertexData = ((BufferData)vertexBufferRef.Data).Content;
                            }
                            else if (!string.IsNullOrEmpty(vertexBufferRef.Url))
                            {
                                var dataAsset = manager.Load<Buffer>(vertexBufferRef.Url);
                                vertexData = dataAsset.GetSerializationData().Content;
                            }
                            else
                            {
                                throw new Exception($"Failed to get Vertex BufferData for entity {chunk.Entity.Name}'s model.");
                            }

                            //transform the vertexes according to the entity
                            var vertexDataCopy = vertexData.ToArray();
                            chunk.Entity.Transform.UpdateWorldMatrix(); //make sure matrix is computed
                            var worldMatrix = chunk.Entity.Transform.WorldMatrix;
                            var up = Vector3.Cross(worldMatrix.Right, worldMatrix.Forward);
                            bool isScalingNegative = Vector3.Dot(worldMatrix.Up, up) < 0.0f;

                            modelMesh.Draw.VertexBuffers[0].TransformBuffer(vertexDataCopy, ref worldMatrix);

                            //add to the big single array
                            var vertexes = vertexDataCopy
                                .Skip(modelMesh.Draw.VertexBuffers[0].Offset)
                                .Take(modelMesh.Draw.VertexBuffers[0].Count*modelMesh.Draw.VertexBuffers[0].Stride)
                                .ToArray();

                            mesh.VertexData.AddRange(vertexes);

                            //indices
                            var indexBufferRef = AttachedReferenceManager.GetAttachedReference(modelMesh.Draw.IndexBuffer.Buffer);
                            byte[] indexData;
                            if (indexBufferRef.Data != null)
                            {
                                indexData = ((BufferData)indexBufferRef.Data).Content;
                            }
                            else if (!string.IsNullOrEmpty(indexBufferRef.Url))
                            {
                                var dataAsset = manager.Load<Buffer>(indexBufferRef.Url);
                                indexData = dataAsset.GetSerializationData().Content;
                            }
                            else
                            {
                                throw new Exception("Failed to get Indices BufferData for entity {chunk.Entity.Name}'s model.");
                            }

                            var indexSize = modelMesh.Draw.IndexBuffer.Is32Bit ? sizeof(uint) : sizeof(ushort);

                            byte[] indices;
                            if (isScalingNegative)
                            {
                                // Get reversed winding order
                                modelMesh.Draw.GetReversedWindingOrder(out indices);
                                indices = indices.Skip(modelMesh.Draw.IndexBuffer.Offset)
                                    .Take(modelMesh.Draw.IndexBuffer.Count*indexSize)
                                    .ToArray();
                            }
                            else
                            {
                                // Get indices normally
                                indices = indexData
                                    .Skip(modelMesh.Draw.IndexBuffer.Offset)
                                    .Take(modelMesh.Draw.IndexBuffer.Count*indexSize)
                                    .ToArray();
                            }

                            // Convert indices to 32 bits
                            if (indexSize == sizeof(ushort))
                            {
                                var uintIndices = new byte[indices.Length*2];
                                fixed (byte* psrc = indices)
                                fixed (byte* pdst = uintIndices)
                                {
                                    var src = (ushort*)psrc;
                                    var dst = (uint*)pdst;

                                    int numIndices = indices.Length/sizeof(ushort);
                                    for (var i = 0; i < numIndices; i++)
                                    {
                                        dst[i] = src[i];
                                    }
                                }
                                indices = uintIndices;
                            }

                            // Offset indices by mesh.IndexOffset
                            fixed (byte* pdst = indices)
                            {
                                var dst = (uint*)pdst;

                                int numIndices = indices.Length/sizeof(uint);
                                for (var i = 0; i < numIndices; i++)
                                {
                                    // Offset indices
                                    dst[i] += (uint)mesh.IndexOffset;
                                }
                            }

                            mesh.IndexOffset += modelMesh.Draw.VertexBuffers[0].Count;

                            mesh.IndexData.AddRange(indices);
                        }
                    }
                }

                //Sort out material
                var matIndex = prefabModel.Materials.Count;
                prefabModel.Materials.Add(material);

                foreach (var meshData in meshes)
                {
                    //todo need to take care of short index
                    var vertexArray = meshData.Value.VertexData.ToArray();
                    var indexArray = meshData.Value.IndexData.ToArray();

                    var vertexCount = vertexArray.Length/meshData.Value.VertexStride;
                    var indexCount = indexArray.Length/4;

                    var gpuMesh = new Mesh
                    {
                        Draw = new MeshDraw { PrimitiveType = PrimitiveType.TriangleList, DrawCount = indexCount, StartLocation = 0 },
                        MaterialIndex = matIndex
                    };

                    var vertexBuffer = new BufferData(BufferFlags.VertexBuffer, new byte[vertexArray.Length]);
                    var indexBuffer = new BufferData(BufferFlags.IndexBuffer, new byte[indexArray.Length]);

                    var vertexBufferSerializable = vertexBuffer.ToSerializableVersion();
                    var indexBufferSerializable = indexBuffer.ToSerializableVersion();

                    Array.Copy(vertexArray, vertexBuffer.Content, vertexArray.Length);
                    Array.Copy(indexArray, indexBuffer.Content, indexArray.Length);

                    gpuMesh.Draw.VertexBuffers = new VertexBufferBinding[1];
                    gpuMesh.Draw.VertexBuffers[0] = new VertexBufferBinding(vertexBufferSerializable, meshData.Key, vertexCount);
                    gpuMesh.Draw.IndexBuffer = new IndexBufferBinding(indexBufferSerializable, true, indexCount);

                    prefabModel.Meshes.Add(gpuMesh);
                }
            }

            private static MaterialInstance ExtractMaterialInstance(MaterialInstance baseInstance, int index, ModelComponent modelComponent, Material fallbackMaterial)
            {
                var instance = new MaterialInstance
                {
                    Material = modelComponent.Materials.SafeGet(index) ?? baseInstance.Material ?? fallbackMaterial,
                    IsShadowCaster = modelComponent.IsShadowCaster,
                };

                if (baseInstance != null)
                {
                    instance.IsShadowCaster = instance.IsShadowCaster && baseInstance.IsShadowCaster;
                }

                return instance;
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var contentManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);

                var device = GraphicsDevice.New();

                var fallbackMaterial = Material.New(device, new MaterialDescriptor
                {
                    Attributes =
                    {
                        Diffuse = new MaterialDiffuseMapFeature(new ComputeTextureColor()),
                        DiffuseModel = new MaterialDiffuseLambertModelFeature()
                    }
                });

                var loadSettings = new ContentManagerLoaderSettings
                {
                    ContentFilter = ContentManagerLoaderSettings.NewContentFilterByType(typeof(Mesh), typeof(Material))
                };

                IList<Entity> allEntities = new List<Entity>();
                
                if (Parameters.Prefab != null)
                {
                    var prefab = AssetFinder.FindAssetFromProxyObject(Parameters.Prefab)?.Asset as PrefabAsset;
                    if (prefab != null)
                        allEntities = prefab.Hierarchy.Parts.Values.Select(x => x.Entity).ToList();
                }

                var prefabModel = new Model();

                //The objective is to create 1 mesh per material/shadow params
                //1. We group by materials
                //2. Create a mesh per material (might need still more meshes if 16bit indexes or more then 32bit)

                var materials = new Dictionary<MaterialInstance, List<EntityChunk>>();
                var loadedModel = new List<Model>();

                foreach (var subEntity in allEntities)
                {
                    var modelComponent = subEntity.Get<ModelComponent>();

                    if (modelComponent?.Model == null || (modelComponent.Skeleton != null && modelComponent.Skeleton.Nodes.Length != 1) || !modelComponent.Enabled)
                        continue;

                    var modelAsset = AssetFinder.FindAssetFromProxyObject(modelComponent.Model);
                    if (modelAsset == null)
                        continue;

                    var model = contentManager.Load<Model>(modelAsset.Location, loadSettings);
                    loadedModel.Add(model);

                    if (model == null ||
                        model.Meshes.Any(x => x.Draw.PrimitiveType != PrimitiveType.TriangleList || x.Draw.VertexBuffers == null || x.Draw.VertexBuffers.Length != 1) ||
                        model.Materials.Any(x => x.Material != null && x.Material.Passes.Any(pass => pass.HasTransparency)) ||
                        modelComponent.Materials.Values.Any(x => x.Passes.Any(pass => pass.HasTransparency))) //For now we limit only to TriangleList types and interleaved vertex buffers, also we skip transparent
                    {
                        commandContext.Logger.Info($"Skipped entity {subEntity.Name} since it's not compatible with PrefabModel.");
                        continue;
                    }

                    for (var index = 0; index < model.Materials.Count; index++)
                    {
                        var material = model.Materials[index];
                        var mat = ExtractMaterialInstance(material, index, modelComponent, fallbackMaterial);

                        var chunk = new EntityChunk { Entity = subEntity, Model = model, MaterialIndex = index };

                        if (materials.TryGetValue(mat, out var entities))
                        {
                            entities.Add(chunk);
                        }
                        else
                        {
                            materials.Add(mat, new List<EntityChunk> { chunk });
                        }
                    }
                }

                foreach (var material in materials)
                {
                    ProcessMaterial(contentManager, material.Value, material.Key, prefabModel);
                }

                // split the meshes if necessary
                prefabModel.Meshes = SplitExtensions.SplitMeshes(prefabModel.Meshes, renderingSettings.DefaultGraphicsProfile > GraphicsProfile.Level_9_3);

                //handle boundng box/sphere
                var modelBoundingBox = prefabModel.BoundingBox;
                var modelBoundingSphere = prefabModel.BoundingSphere;
                foreach (var mesh in prefabModel.Meshes)
                {
                    var vertexBuffers = mesh.Draw.VertexBuffers;
                    if (vertexBuffers.Length > 0)
                    {
                        // Compute local mesh bounding box (no node transformation)
                        var matrix = Matrix.Identity;
                        mesh.BoundingBox = vertexBuffers[0].ComputeBounds(ref matrix, out mesh.BoundingSphere);

                        // Compute model bounding box (includes node transformation)
                        BoundingSphere meshBoundingSphere;
                        var meshBoundingBox = vertexBuffers[0].ComputeBounds(ref matrix, out meshBoundingSphere);
                        BoundingBox.Merge(ref modelBoundingBox, ref meshBoundingBox, out modelBoundingBox);
                        BoundingSphere.Merge(ref modelBoundingSphere, ref meshBoundingSphere, out modelBoundingSphere);
                    }

                    mesh.Draw.CompactIndexBuffer();
                }
                prefabModel.BoundingBox = modelBoundingBox;
                prefabModel.BoundingSphere = modelBoundingSphere;

                //save
                contentManager.Save(Url, prefabModel);

                foreach (var model in loadedModel.NotNull())
                {
                    contentManager.Unload(model);
                }
                device.Dispose();

                return Task.FromResult(ResultStatus.Successful);
            }
        }
    }
}
