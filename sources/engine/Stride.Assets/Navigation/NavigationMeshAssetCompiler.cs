// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Assets.Entities;
using Stride.Assets.Physics;
using Stride.Navigation;
using Stride.Physics;

namespace Stride.Assets.Navigation
{
    [AssetCompiler(typeof(NavigationMeshAsset), typeof(AssetCompilationContext))]
    class NavigationMeshAssetCompiler : AssetCompilerBase
    { 
        public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
        {
            yield return new BuildDependencyInfo(typeof(SceneAsset), typeof(AssetCompilationContext), BuildDependencyType.CompileAsset);
            yield return new BuildDependencyInfo(typeof(ColliderShapeAsset), typeof(AssetCompilationContext), BuildDependencyType.CompileContent);
            yield return new BuildDependencyInfo(typeof(HeightmapAsset), typeof(AssetCompilationContext), BuildDependencyType.CompileContent);
        }

        public override IEnumerable<ObjectUrl> GetInputFiles(AssetItem assetItem)
        {
            var asset = (NavigationMeshAsset)assetItem.Asset;
            if (asset.Scene != null)
            {
                string sceneUrl = AttachedReferenceManager.GetUrl(asset.Scene);
                var sceneAsset = (SceneAsset)assetItem.Package.Session.FindAsset(sceneUrl)?.Asset;
                if (sceneAsset == null)
                    yield break;

                var sceneEntities = sceneAsset.Hierarchy.Parts.Select(x => x.Value.Entity).ToList();
                foreach (var entity in sceneEntities)
                {
                    var collider = entity.Get<StaticColliderComponent>();

                    // Only process enabled colliders
                    bool colliderEnabled = collider != null && ((CollisionFilterGroupFlags)collider.CollisionGroup & asset.IncludedCollisionGroups) != 0 && collider.Enabled;
                    if (colliderEnabled) // Removed or disabled
                    {
                        foreach (var desc in collider.ColliderShapes)
                        {
                            var shapeAssetDesc = desc as ColliderShapeAssetDesc;
                            if (shapeAssetDesc?.Shape != null)
                            {
                                var assetReference = AttachedReferenceManager.GetAttachedReference(shapeAssetDesc.Shape);
                                if (assetReference != null)
                                {
                                    yield return new ObjectUrl(UrlType.Content, assetReference.Url);
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
                }
            }
        }

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (NavigationMeshAsset)assetItem.Asset;

            // Compile the navigation mesh itself
            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new NavmeshBuildCommand(targetUrlInStorage, assetItem, asset, context) { InputFilesGetter = () => GetInputFiles(assetItem) });
        }

        private class NavmeshBuildCommand : AssetCommand<NavigationMeshAsset>
        {
            private readonly ContentManager contentManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
            private readonly Dictionary<string, PhysicsColliderShape> loadedColliderShapes = new Dictionary<string, PhysicsColliderShape>();
            private readonly Dictionary<string, object> loadedHeightfieldInitialDatas = new Dictionary<string, object>();

            private NavigationMesh oldNavigationMesh;

            private UFile assetUrl;
            private NavigationMeshAsset asset;

            private int sceneHash = 0;
            private SceneAsset clonedSceneAsset;
            private GameSettingsAsset gameSettingsAsset;
            private bool sceneCloned = false; // Used so that the scene is only cloned once when ComputeParameterHash or DoCommand is called

            // Automatically calculated bounding box
            private List<StaticColliderData> staticColliderDatas = new List<StaticColliderData>();
            private List<BoundingBox> boundingBoxes = new List<BoundingBox>();

            public NavmeshBuildCommand(string url, AssetItem assetItem, NavigationMeshAsset value, AssetCompilerContext context)
                : base(url, value, assetItem.Package)
            {
                gameSettingsAsset = context.GetGameSettingsAsset();
                asset = value;
                assetUrl = url;
                
                Version = 1; // Removed separate debug model stored in the navigation mesh
            }

            protected override void ComputeParameterHash(BinarySerializationWriter writer)
            {
                base.ComputeParameterHash(writer);

                EnsureClonedSceneAndHash();
                writer.Write(sceneHash);
                writer.Write(asset.SelectedGroups);
                
                var navigationSettings = gameSettingsAsset.GetOrDefault<NavigationSettings>();
                writer.Write(navigationSettings.Groups);
            }
            
            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var intermediateDataId = ComputeAssetIntermediateDataId();

                oldNavigationMesh = LoadIntermediateData(intermediateDataId);
                var navigationMeshBuilder = new NavigationMeshBuilder(oldNavigationMesh);
                navigationMeshBuilder.Logger = commandContext.Logger;

                foreach (var colliderData in staticColliderDatas)
                    navigationMeshBuilder.Add(colliderData);
                
                var navigationSettings = gameSettingsAsset.GetOrDefault<NavigationSettings>();
                var groupsLookup = navigationSettings.Groups.ToDictionary(x => x.Id, x => x);

                var groups = new List<NavigationMeshGroup>();
                // Resolve groups
                foreach (var groupId in asset.SelectedGroups)
                {
                    NavigationMeshGroup group;
                    if (groupsLookup.TryGetValue(groupId, out group))
                    {
                        groups.Add(group);
                    }
                    else
                    {
                        commandContext.Logger.Warning($"Group not defined in game settings {{{groupId}}}");
                    }
                }

                var result = navigationMeshBuilder.Build(asset.BuildSettings, groups, asset.IncludedCollisionGroups, boundingBoxes, CancellationToken.None);
                
                // Unload loaded collider shapes
                foreach (var pair in loadedColliderShapes)
                {
                    contentManager.Unload(pair.Key);
                }
                foreach (var pair in loadedHeightfieldInitialDatas)
                {
                    contentManager.Unload(pair.Key);
                }

                if (!result.Success)
                    return Task.FromResult(ResultStatus.Failed);

                // Save complete navigation mesh + intermediate data to cache
                SaveIntermediateData(intermediateDataId, result.NavigationMesh);

                // Clear intermediate data and save to content database
                result.NavigationMesh.Cache = null;
                contentManager.Save(assetUrl, result.NavigationMesh);

                return Task.FromResult(ResultStatus.Successful);
            }

            /// <summary>
            /// Computes a unique Id for this asset used to store intermediate / build cache data
            /// </summary>
            /// <returns>The object id for asset intermediate data</returns>
            private ObjectId ComputeAssetIntermediateDataId()
            {
                var stream = new DigestStream(Stream.Null);
                var writer = new BinarySerializationWriter(stream);
                writer.Context.SerializerSelector = SerializerSelector.AssetWithReuse;
                writer.Write(CommandCacheVersion);

                // Write binary format version
                writer.Write(DataSerializer.BinaryFormatVersion);

                // Compute assembly hash
                ComputeAssemblyHash(writer);

                // Write asset Id
                writer.Write(asset.Id);

                return stream.CurrentHash;
            }

            /// <summary>
            /// Loads intermediate data used for building a navigation mesh
            /// </summary>
            /// <param name="objectId">The unique Id for this data in the object database</param>
            /// <returns>The found cached build or null if there is no previous build</returns>
            private NavigationMesh LoadIntermediateData(ObjectId objectId)
            {
                try
                {
                    var objectDatabase = MicrothreadLocalDatabases.DatabaseFileProvider.ObjectDatabase;
                    using (var stream = objectDatabase.OpenStream(objectId))
                    {
                        var reader = new BinarySerializationReader(stream);
                        NavigationMesh result = new NavigationMesh();
                        reader.Serialize(ref result, ArchiveMode.Deserialize);
                        return result;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }

            /// <summary>
            /// Saves intermediate data used for building a navigation mesh
            /// </summary>
            /// <param name="objectId">The unique Id for this data in the object database</param>
            /// <param name="build">The build data to save</param>
            private void SaveIntermediateData(ObjectId objectId, NavigationMesh build)
            {
                var objectDatabase = MicrothreadLocalDatabases.DatabaseFileProvider.ObjectDatabase;
                using (var stream = objectDatabase.OpenStream(objectId, VirtualFileMode.Create, VirtualFileAccess.Write))
                {
                    var writer = new BinarySerializationWriter(stream);
                    writer.Serialize(ref build, ArchiveMode.Serialize);
                    writer.Flush();
                }
            }

            private void EnsureClonedSceneAndHash()
            {
                if (!sceneCloned)
                {
                    // Hash relevant scene objects
                    if (asset.Scene != null)
                    {
                        string sceneUrl = AttachedReferenceManager.GetUrl(asset.Scene);
                        var sceneAsset = (SceneAsset)AssetFinder.FindAsset(sceneUrl)?.Asset;

                        // Clone scene asset because we update the world transformation matrices
                        clonedSceneAsset = (SceneAsset)AssetCloner.Clone(sceneAsset);

                        // Turn the entire entity hierarchy into a single list
                        var sceneEntities = clonedSceneAsset.Hierarchy.Parts.Select(x => x.Value.Entity).ToList();

                        sceneHash = 0;
                        foreach (var entity in sceneEntities)
                        {
                            // Collect bounding box entities
                            NavigationBoundingBoxComponent boundingBoxComponent = entity.Get<NavigationBoundingBoxComponent>();
                            // Collect static collider entities
                            StaticColliderComponent colliderComponent = entity.Get<StaticColliderComponent>();

                            if (boundingBoxComponent == null && colliderComponent == null)
                                continue;
                            
                            // Update world transform
                            entity.Transform.UpdateWorldMatrix();

                            if (boundingBoxComponent != null)
                            {
                                Vector3 scale;
                                Quaternion rotation;
                                Vector3 translation;
                                boundingBoxComponent.Entity.Transform.WorldMatrix.Decompose(out scale, out rotation, out translation);
                                var boundingBox = new BoundingBox(translation - boundingBoxComponent.Size * scale, translation + boundingBoxComponent.Size * scale);
                                boundingBoxes.Add(boundingBox);

                                // Hash collider for ComputeParameterHash
                                sceneHash = (sceneHash * 397) ^ boundingBox.GetHashCode();
                            }

                            if (colliderComponent != null)
                            {
                                staticColliderDatas.Add(new StaticColliderData
                                {
                                    Component = colliderComponent,
                                });

                                if (colliderComponent.Enabled && !colliderComponent.IsTrigger && ((int)asset.IncludedCollisionGroups & (int)colliderComponent.CollisionGroup) != 0)
                                {
                                    // Load collider shape assets since the scene asset is being used, which does not have these loaded by default
                                    foreach (var desc in colliderComponent.ColliderShapes)
                                    {
                                        var shapeAssetDesc = desc as ColliderShapeAssetDesc;
                                        if (shapeAssetDesc?.Shape != null)
                                        {
                                            var assetReference = AttachedReferenceManager.GetAttachedReference(shapeAssetDesc.Shape);
                                            PhysicsColliderShape loadedColliderShape;
                                            if (!loadedColliderShapes.TryGetValue(assetReference.Url, out loadedColliderShape))
                                            {
                                                loadedColliderShape = contentManager.Load<PhysicsColliderShape>(assetReference.Url);
                                                loadedColliderShapes.Add(assetReference.Url, loadedColliderShape); // Store where we loaded the shapes from
                                            }
                                            shapeAssetDesc.Shape = loadedColliderShape;
                                        }
                                        else if (desc is HeightfieldColliderShapeDesc)
                                        {
                                            var heightfieldDesc = desc as HeightfieldColliderShapeDesc;
                                            var heightmapSource = heightfieldDesc?.HeightStickArraySource as HeightStickArraySourceFromHeightmap;

                                            if (heightmapSource?.Heightmap != null)
                                            {
                                                var assetReference = AttachedReferenceManager.GetAttachedReference(heightmapSource.Heightmap);
                                                object loadedHeightfieldInitialData;
                                                if (!loadedHeightfieldInitialDatas.TryGetValue(assetReference.Url, out loadedHeightfieldInitialData))
                                                {
                                                    loadedHeightfieldInitialData = contentManager.Load(typeof(Heightmap), assetReference.Url);
                                                    loadedHeightfieldInitialDatas.Add(assetReference.Url, loadedHeightfieldInitialData);
                                                }
                                                heightmapSource.Heightmap = loadedHeightfieldInitialData as Heightmap;
                                            }
                                        }
                                    }
                                }

                                // Hash collider for ComputeParameterHash
                                sceneHash = (sceneHash * 397) ^ Stride.Navigation.NavigationMeshBuildUtils.HashEntityCollider(colliderComponent, asset.IncludedCollisionGroups);
                            }
                        }
                    }
                    sceneCloned = true;
                }
            }
        }
    }
}
