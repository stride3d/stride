// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xenko.Core.Assets;
using Xenko.Core;
using Xenko.Core.Assets.Serializers;
using Xenko.Core.Assets.Templates;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization;
using Xenko.Core.Storage;
using Xenko.Core.Yaml;
using Xenko.Core.Yaml.Serialization;
using Xenko.TextureConverter;
using Xenko.Assets.Effect;
using Xenko.Assets.Templates;
using Xenko.Graphics;

namespace Xenko.Assets
{
#if XENKO_SUPPORT_BETA_UPGRADE
    [PackageUpgrader(XenkoConfig.PackageName, "1.10.0-alpha01", CurrentVersion)]
#else
    [PackageUpgrader(XenkoConfig.PackageName, "2.0.0.0", CurrentVersion)]
#endif
    public partial class XenkoPackageUpgrader : PackageUpgrader
    {
        // Should match Xenko.nupkg
        public const string CurrentVersion = XenkoVersion.NuGetVersion;

        public static readonly string DefaultGraphicsCompositorLevel9Url = "DefaultGraphicsCompositorLevel9";
        public static readonly string DefaultGraphicsCompositorLevel10Url = "DefaultGraphicsCompositorLevel10";

        public static readonly Guid DefaultGraphicsCompositorLevel9CameraSlot = new Guid("bbfef2cb-8c63-4cab-9caf-6ae48f44a8ba");
        public static readonly Guid DefaultGraphicsCompositorLevel10CameraSlot = new Guid("d0a6bf72-b3cd-4bd4-94ca-69952999d537");

        public static readonly Guid UpdatePlatformsTemplateId = new Guid("446B52D3-A6A8-4274-A357-736ADEA87321");

        public override bool Upgrade(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage, IList<PackageLoadingAssetFile> assetFiles)
        {
#if XENKO_SUPPORT_BETA_UPGRADE
            // Graphics Compositor asset
            if (dependency.Version.MinVersion < new PackageVersion("1.11.0.0"))
            {
                // Find game settings (if there is none, it's not a game and nothing to do)
                var gameSettings = assetFiles.FirstOrDefault(x => x.AssetLocation == GameSettingsAsset.GameSettingsLocation);
                if (gameSettings != null)
                {
                    RunAssetUpgradersUntilVersion(log, dependentPackage, dependency.Name, gameSettings.Yield().ToList(), new PackageVersion("1.10.0-alpha02"));

                    using (var gameSettingsYaml = gameSettings.AsYamlAsset())
                    {
                        // Figure out graphics profile; default is Level_10_0 (which is same as GraphicsCompositor default)
                        var graphicsProfile = GraphicsProfile.Level_10_0;
                        try
                        {
                            foreach (var mapping in gameSettingsYaml.DynamicRootNode.Defaults)
                            {
                                if (mapping.Node.Tag == "!Xenko.Graphics.RenderingSettings,Xenko.Graphics")
                                {
                                    if (mapping.DefaultGraphicsProfile != null)
                                        Enum.TryParse((string)mapping.DefaultGraphicsProfile, out graphicsProfile);
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            // If something goes wrong, keep going with the default value
                        }

                        // Add graphics compositor asset by creating a derived asset of Compositing/DefaultGraphicsCompositor.xkgfxcomp
                        var graphicsCompositorUrl = graphicsProfile >= GraphicsProfile.Level_10_0 ? DefaultGraphicsCompositorLevel10Url : DefaultGraphicsCompositorLevel9Url;

                        var defaultGraphicsCompositor = dependencyPackage.Assets.Find(graphicsCompositorUrl);
                        if (defaultGraphicsCompositor == null)
                        {
                            log.Error($"Could not find graphics compositor in Xenko package at location [{graphicsCompositorUrl}]");
                            return false;
                        }

                        // Note: we create a derived asset without its content
                        // We don't use defaultGraphicsCompositor content because it might be a newer version that next upgrades might not understand.
                        // The override system will restore all the properties for us.
                        var graphicsCompositorAssetId = AssetId.New();
                        var graphicsCompositorAsset = new PackageLoadingAssetFile(dependentPackage, "GraphicsCompositor.xkgfxcomp", null)
                        {
                            AssetContent = System.Text.Encoding.UTF8.GetBytes($"!GraphicsCompositorAsset\r\nId: {graphicsCompositorAssetId}\r\nSerializedVersion: {{Xenko: 1.10.0-beta01}}\r\nArchetype: {defaultGraphicsCompositor.ToReference()}"),
                        };

                        assetFiles.Add(graphicsCompositorAsset);

                        // Update game settings to point to our newly created compositor
                        gameSettingsYaml.DynamicRootNode.GraphicsCompositor = new AssetReference(graphicsCompositorAssetId, graphicsCompositorAsset.AssetLocation).ToString();
                    }
                }

                // Delete EffectLogAsset
                foreach (var assetFile in assetFiles)
                {
                    if (assetFile.FilePath.GetFileNameWithoutExtension() == EffectLogAsset.DefaultFile)
                    {
                        assetFile.Deleted = true;
                    }
                }
            }


            if (dependency.Version.MinVersion < new PackageVersion("1.11.1.0"))
            {
                ConvertNormalMapsInvertY(assetFiles);
            }

            // Skybox/Background separation
            if (dependency.Version.MinVersion < new PackageVersion("1.11.1.1"))
            {
                SplitSkyboxLightingUpgrader upgrader = new SplitSkyboxLightingUpgrader();
                foreach (var skyboxAsset in assetFiles.Where(f => f.FilePath.GetFileExtension() == ".xksky"))
                {
                    upgrader.ProcessSkybox(skyboxAsset);
                }
                foreach (var sceneAsset in assetFiles.Where(f => (f.FilePath.GetFileExtension() == ".xkscene") || (f.FilePath.GetFileExtension() == ".xkprefab")))
                {
                    using (var yaml = sceneAsset.AsYamlAsset())
                    {
                        upgrader.UpgradeAsset(yaml.DynamicRootNode);
                    }
                }
            }
            
            if (dependency.Version.MinVersion < new PackageVersion("1.11.1.2"))
            {
                var navigationMeshAssets = assetFiles.Where(f => f.FilePath.GetFileExtension() == ".xknavmesh");
                var scenes = assetFiles.Where(f => f.FilePath.GetFileExtension() == ".xkscene");
                UpgradeNavigationBoundingBox(navigationMeshAssets, scenes);

                // Upgrade game settings to have groups for navigation meshes
                var gameSettingsAsset = assetFiles.FirstOrDefault(x => x.AssetLocation == GameSettingsAsset.GameSettingsLocation);
                if (gameSettingsAsset != null)
                {
                    // Upgrade the game settings first to contain navigation mesh settings entry
                    RunAssetUpgradersUntilVersion(log, dependentPackage, dependency.Name, gameSettingsAsset.Yield().ToList(), new PackageVersion("1.11.1.2"));

                    UpgradeNavigationMeshGroups(navigationMeshAssets, gameSettingsAsset);
                }
            }

            if (dependency.Version.MinVersion < new PackageVersion("2.0.0.2"))
            {
                RunAssetUpgradersUntilVersion(log, dependentPackage, dependency.Name, assetFiles, new PackageVersion("2.0.0.0"));

                Guid defaultCompositorId = Guid.Empty;
                var defaultGraphicsCompositorCameraSlot = Guid.Empty;

                // Step one: find the default compositor, that will be the reference one to patch scenes
                var gameSettings = assetFiles.FirstOrDefault(x => x.AssetLocation == GameSettingsAsset.GameSettingsLocation);
                if (gameSettings != null)
                {
                    using (var gameSettingsYaml = gameSettings.AsYamlAsset())
                    {
                        dynamic asset = gameSettingsYaml.DynamicRootNode;
                        string compositorReference = asset.GraphicsCompositor?.ToString();
                        var guidString = compositorReference?.Split(':').FirstOrDefault();
                        Guid.TryParse(guidString, out defaultCompositorId);

                        // Figure out graphics profile; default is Level_10_0 (which is same as GraphicsCompositor default)
                        var graphicsProfile = GraphicsProfile.Level_10_0;
                        try
                        {
                            foreach (var mapping in gameSettingsYaml.DynamicRootNode.Defaults)
                            {
                                if (mapping.Node.Tag == "!Xenko.Graphics.RenderingSettings,Xenko.Graphics")
                                {
                                    if (mapping.DefaultGraphicsProfile != null)
                                        Enum.TryParse((string)mapping.DefaultGraphicsProfile, out graphicsProfile);
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            // If something goes wrong, keep going with the default value
                        }

                        // store the camera slot of the default graphics compositor, because the one from the project will be empty since upgrade relies on reconcile with base, which happens after
                        defaultGraphicsCompositorCameraSlot = graphicsProfile >= GraphicsProfile.Level_10_0 ? DefaultGraphicsCompositorLevel10CameraSlot : DefaultGraphicsCompositorLevel9CameraSlot;
                    }
                }

                // Step two: add an Guid for each item in the SceneCameraSlotCollection of each graphics compositor
                Dictionary<int, Guid> slotIds = new Dictionary<int, Guid>();

                // This upgrades a projects that already had a graphics compositor before (ie. a project created with public 1.10)
                // In this case, the compositor that has been created above is empty, and the upgrade relies on reconciliation with base
                // to fill it properly, which means that for now we have no camera slot. Fortunately, we know the camera slot id from the archetype.
                slotIds.Add(0, defaultGraphicsCompositorCameraSlot);

                var graphicsCompositorAssets = assetFiles.Where(f => f.FilePath.GetFileExtension() == ".xkgfxcomp");
                foreach (var graphicsCompositorAsset in graphicsCompositorAssets)
                {
                    using (var yamlAsset = graphicsCompositorAsset.AsYamlAsset())
                    {
                        dynamic asset = yamlAsset.DynamicRootNode;
                        int i = 0;
                        var localSlotIds = new Dictionary<int, Guid>();
                        if (asset.Cameras != null)
                        {
                            // This upgrades a projects that already had a graphics compositor before (ie. an internal project created with 1.11)
                            foreach (dynamic cameraSlot in asset.Cameras)
                            {
                                var guid = Guid.NewGuid();
                                Guid assetId;
                                if (Guid.TryParse(asset.Id.ToString(), out assetId) && assetId == defaultCompositorId)
                                {
                                    slotIds[i] = guid;
                                }
                                localSlotIds.Add(i, guid);
                                cameraSlot.Value.Id = guid;
                                ++i;
                            }
                            var indexString = asset.Game?.Camera?.Index?.ToString();
                            int index;
                            int.TryParse(indexString, out index);
                            if (localSlotIds.ContainsKey(index) && asset.Game?.Camera != null)
                            {
                                asset.Game.Camera = $"ref!! {localSlotIds[index]}";
                            }
                        }
                        else
                        {
                            asset.Cameras = new YamlMappingNode();
                            asset.Cameras.de2e75c3b2b23e54162686363f3f138e = new YamlMappingNode();
                            asset.Cameras.de2e75c3b2b23e54162686363f3f138e.Id = defaultGraphicsCompositorCameraSlot;
                            asset.Cameras.de2e75c3b2b23e54162686363f3f138e.Name = "Main";
                        }
                    }
                }

                // Step three: patch every CameraComponent to reference the Guid instead of an index
                var entityHierarchyAssets = assetFiles.Where(f => f.FilePath.GetFileExtension() == ".xkscene" || f.FilePath.GetFileExtension() == ".xkprefab");
                foreach (var entityHierarchyAsset in entityHierarchyAssets)
                {
                    using (var yamlAsset = entityHierarchyAsset.AsYamlAsset())
                    {
                        dynamic asset = yamlAsset.DynamicRootNode;
                        foreach (var entity in asset.Hierarchy.Parts)
                        {
                            foreach (var component in entity.Entity.Components)
                            {
                                if (component.Value.Node.Tag == "!CameraComponent")
                                {
                                    var indexString = component.Value.Slot?.Index?.ToString() ?? "0";
                                    int index;
                                    if (int.TryParse(indexString, out index))
                                    {
                                        if (slotIds.ContainsKey(index))
                                        {
                                            component.Value.Slot = slotIds[index].ToString();
                                        }
                                        else
                                        {
                                            component.Value.Slot = Guid.Empty.ToString();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
#endif
            // Put any new upgrader after this #endif
            if (dependency.Version.MinVersion < new PackageVersion("2.2.0.1"))
            {
                foreach (var assetFile in assetFiles)
                {
                    // Add new generic parameter to ShadowMapReceiverDirectional in effect log
                    if (assetFile.FilePath.GetFileExtension() == ".xkeffectlog")
                    {
                        var assetContent = assetFile.AssetContent ?? File.ReadAllBytes(assetFile.FilePath.FullPath);
                        var assetContentString = System.Text.Encoding.UTF8.GetString(assetContent);
                        var newAssetContentString = System.Text.RegularExpressions.Regex.Replace(assetContentString, @"(\s*-   ClassName: ShadowMapReceiverDirectional\r\n\s*    GenericArguments: \[\w*, \w*, \w*, \w*, \w*)\]", "$1, false]");
                        newAssetContentString = newAssetContentString.Replace("EffectName: SkyboxShader\r\n", "EffectName: SkyboxShaderCubemap\r\n");
                        if (assetContentString != newAssetContentString)
                        {
                            // Need replacement, update with replaced text
                            assetFile.AssetContent = System.Text.Encoding.UTF8.GetBytes(newAssetContentString);
                        }
                    }

                    // Set BackgroundComponent.Is2D when necessary
                    if (assetFile.FilePath.GetFileExtension() == ".xkscene"
                        || assetFile.FilePath.GetFileExtension() == ".xkprefab")
                    {
                        using (var yamlAsset = assetFile.AsYamlAsset())
                        {
                            dynamic asset = yamlAsset.DynamicRootNode;
                            foreach (var entity in asset.Hierarchy.Parts)
                            {
                                foreach (var component in entity.Entity.Components)
                                {
                                    if (component.Value.Node.Tag == "!BackgroundComponent" && component.Value.Texture != null)
                                    {
                                        if (!AssetReference.TryParse((string)component.Value.Texture, out var textureReference))
                                            continue;

                                        // Find texture
                                        var textureAssetFile = assetFiles.FirstOrDefault(x => x.AssetLocation == textureReference.Location);
                                        if (textureAssetFile == null)
                                            continue;

                                        // Get texture source
                                        try
                                        {
                                            using (var yamlTextureAsset = textureAssetFile.AsYamlAsset())
                                            {
                                                dynamic textureAsset = yamlTextureAsset.DynamicRootNode;
                                                if (textureAsset.Source == null)
                                                    continue;

                                                var textureSource = UFile.Combine(textureAssetFile.FilePath.GetFullDirectory(), new UFile((string)textureAsset.Source));
                                                if (!File.Exists(textureSource))
                                                    continue;

                                                var texTool = new TextureTool();
                                                var image = texTool.Load(textureSource, false);
                                                if (image != null)
                                                {
                                                    if (image.Dimension != TexImage.TextureDimension.TextureCube)
                                                    {
                                                        // We have a texture which is not a cubemap, mark background as 2D
                                                        component.Value.Is2D = true;
                                                    }

                                                    image.Dispose();
                                                }

                                                texTool.Dispose();
                                            }
                                        }
                                        catch
                                        {
                                            // If something goes wrong, keep default value (assume cubemap)
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (dependency.Version.MinVersion < new PackageVersion("3.0.0.0"))
            {
                // Delete EffectLogAsset
                foreach (var assetFile in assetFiles)
                {
                    var assetContent = assetFile.AssetContent ?? File.ReadAllBytes(assetFile.FilePath.FullPath);
                    var assetContentString = System.Text.Encoding.UTF8.GetString(assetContent);
                    var newAssetContentString = RemoveSiliconStudioNamespaces(assetContentString);
                    if (assetContentString != newAssetContentString)
                    {
                        // Need replacement, update with replaced text
                        assetFile.AssetContent = System.Text.Encoding.UTF8.GetBytes(newAssetContentString);
                    }
                }
            }

            return true;
        }

        private void RunAssetUpgradersUntilVersion(ILogger log, Package dependentPackage, string dependencyName, IList<PackageLoadingAssetFile> assetFiles, PackageVersion maxVersion)
        {
            foreach (var assetFile in assetFiles)
            {
                if (assetFile.Deleted)
                    continue;

                var context = new AssetMigrationContext(dependentPackage, assetFile.ToReference(), assetFile.FilePath.ToWindowsPath(), log);
                AssetMigration.MigrateAssetIfNeeded(context, assetFile, dependencyName, maxVersion);
            }
        }

        /// <inheritdoc/>
        public override bool UpgradeAfterAssetsLoaded(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage, PackageVersionRange dependencyVersionBeforeUpdate)
        {
            if (dependencyVersionBeforeUpdate.MinVersion < new PackageVersion("1.3.0-alpha02"))
            {
                // Add everything as root assets (since we don't know what the project was doing in the code before)
                foreach (var assetItem in dependentPackage.Assets)
                {
                    if (!AssetRegistry.IsAssetTypeAlwaysMarkAsRoot(assetItem.Asset.GetType()))
                        dependentPackage.RootAssets.Add(new AssetReference(assetItem.Id, assetItem.Location));
                }
            }

            if (dependencyVersionBeforeUpdate.MinVersion < new PackageVersion("1.6.0-beta"))
            {
                // Mark all assets dirty to force a resave
                foreach (var assetItem in dependentPackage.Assets)
                {
                    if (!(assetItem.Asset is SourceCodeAsset))
                    {
                        assetItem.IsDirty = true;
                    }
                }
            }

            return true;
        }

        public override bool UpgradeBeforeAssembliesLoaded(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage)
        {
            if (dependency.Version.MinVersion < new PackageVersion("3.0.0.0"))
            {
                UpgradeCode(dependentPackage, log, new RenameToXenkoCodeUpgrader());
            }

            // Update NuGet references
            var projectFullPath = (dependentPackage.Container as SolutionProject)?.FullPath;
            if (projectFullPath != null)
            {
                try
                {
                    var projectFile = UPath.Combine(dependentPackage.FullPath.GetFullDirectory(), projectFullPath);
                    var project = VSProjectHelper.LoadProject(projectFile.ToWindowsPath());
                    var isProjectDirty = false;

                    var packageReferences = project.GetItems("PackageReference").ToList();
                    foreach (var packageReference in packageReferences)
                    {
                        if (packageReference.EvaluatedInclude == "Xenko" && packageReference.GetMetadataValue("Version") != CurrentVersion)
                        {
                            packageReference.SetMetadataValue("Version", CurrentVersion);
                            isProjectDirty = true;
                        }
                    }

                    if (isProjectDirty)
                        project.Save();

                    project.ProjectCollection.UnloadAllProjects();
                    project.ProjectCollection.Dispose();
                }
                catch (Exception e)
                {
                    log.Warning($"Unable to load project [{projectFullPath.GetFileName()}]", e);
                }
            }

            // Run NuGet restore
            if (loadParameters.AutoCompileProjects)
            {
                if (session.SolutionPath != null)
                {
                    // .sln needs to be up to date -> save everything
                    session.Save(new ForwardingLoggerResult(log));
                    VSProjectHelper.RestoreNugetPackages(log, session.SolutionPath).Wait();
                }
                else
                {
                    dependentPackage.RestoreNugetPackages(log);
                }
            }

            return true;
        }
        private bool IsYamlAsset(PackageLoadingAssetFile assetFile)
        {
            // Determine if asset was Yaml or not
            var assetFileExtension = Path.GetExtension(assetFile.FilePath);
            assetFileExtension = assetFileExtension?.ToLowerInvariant();

            var serializer = AssetFileSerializer.FindSerializer(assetFileExtension);
            return serializer is YamlAssetSerializer;
        }

        private void UpgradeNavigationBoundingBox(IEnumerable<PackageLoadingAssetFile> navigationMeshes, IEnumerable<PackageLoadingAssetFile> scenes)
        {
            foreach (var navigationMesh in navigationMeshes)
            {
                using (var navmeshYamlAsset = navigationMesh.AsYamlAsset())
                {
                    var navmeshAsset = navmeshYamlAsset.DynamicRootNode;
                    var sceneId = (string)navmeshAsset.Scene;
                    var sceneName = sceneId.Split(':').Last();
                    var matchingScene = scenes.Where(x => x.AssetLocation == sceneName).FirstOrDefault();
                    if (matchingScene != null)
                    {
                        var boundingBox = navmeshAsset.BoundingBox;
                        var boundingBoxMin = new Vector3((float)boundingBox.Minimum.X, (float)boundingBox.Minimum.Y, (float)boundingBox.Minimum.Z);
                        var boundingBoxMax = new Vector3((float)boundingBox.Maximum.X, (float)boundingBox.Maximum.Y, (float)boundingBox.Maximum.Z);
                        var boundingBoxSize = (boundingBoxMax - boundingBoxMin) * 0.5f;
                        var boundingBoxCenter = boundingBoxSize + boundingBoxMin;
                            
                        using (var matchingSceneYamlAsset = matchingScene.AsYamlAsset())
                        {
                            var sceneAsset = matchingSceneYamlAsset.DynamicRootNode;
                            var parts = (DynamicYamlArray)sceneAsset.Hierarchy.Parts;
                            var rootParts = (DynamicYamlArray)sceneAsset.Hierarchy.RootPartIds;
                            dynamic newEntity = new DynamicYamlMapping(new YamlMappingNode());
                            newEntity.Id = Guid.NewGuid().ToString();
                            newEntity.Name = "Navigation bounding box";
                                
                            var components = new DynamicYamlMapping(new YamlMappingNode());

                            // Transform component
                            dynamic transformComponent = new DynamicYamlMapping(new YamlMappingNode());
                            transformComponent.Node.Tag = "!TransformComponent";
                            transformComponent.Id = Guid.NewGuid().ToString();
                            transformComponent.Position = new DynamicYamlMapping(new YamlMappingNode
                            {
                                { "X", $"{boundingBoxCenter.X}" }, { "Y", $"{boundingBoxCenter.Y}" }, { "Z", $"{boundingBoxCenter.Z}" }
                            });
                            transformComponent.Rotation = new DynamicYamlMapping(new YamlMappingNode
                            {
                                { "X", "0.0" }, { "Y", "0.0"}, { "Z", "0.0" }, { "W", "0.0" }
                            });
                            transformComponent.Scale = new DynamicYamlMapping(new YamlMappingNode
                            {
                                { "X", "1.0" }, { "Y", "1.0" }, { "Z", "1.0" }
                            });
                            transformComponent.Children = new DynamicYamlMapping(new YamlMappingNode());
                            components.AddChild(Guid.NewGuid().ToString("N"), transformComponent);

                            // Bounding box component
                            dynamic boxComponent = new DynamicYamlMapping(new YamlMappingNode());
                            boxComponent.Id = Guid.NewGuid().ToString();
                            boxComponent.Node.Tag = "!Xenko.Navigation.NavigationBoundingBoxComponent,Xenko.Navigation";
                            boxComponent.Size = new DynamicYamlMapping(new YamlMappingNode
                            {
                                { "X", $"{boundingBoxSize.X}" }, { "Y", $"{boundingBoxSize.Y}" }, { "Z", $"{boundingBoxSize.Z}" }
                            }); ;
                            components.AddChild(Guid.NewGuid().ToString("N"), boxComponent);

                            newEntity.Components = components;

                            dynamic part = new DynamicYamlMapping(new YamlMappingNode());
                            part.Entity = newEntity;
                            parts.Add(part);
                            rootParts.Add((string)newEntity.Id);

                            // Currently need to sort children by Id
                            List<YamlNode> partsList = (List<YamlNode>)parts.Node.Children;
                            var entityKey = new YamlScalarNode("Entity");
                            var idKey = new YamlScalarNode("Id");
                            partsList.Sort((x,y) =>
                            {
                                var entityA = (YamlMappingNode)((YamlMappingNode)x).Children[entityKey];
                                var entityB = (YamlMappingNode)((YamlMappingNode)y).Children[entityKey];
                                var guidA =  new Guid(((YamlScalarNode)entityA.Children[idKey]).Value);
                                var guidB = new Guid(((YamlScalarNode)entityB.Children[idKey]).Value);
                                return guidA.CompareTo(guidB);
                            });
                        }
                    }
                }
            }
        }

        private void UpgradeNavigationMeshGroups(IEnumerable<PackageLoadingAssetFile> navigationMeshAssets, PackageLoadingAssetFile gameSettingsAsset)
        {
            // Collect all unique groups from all navigation mesh assets
            Dictionary<ObjectId, YamlMappingNode> agentSettings = new Dictionary<ObjectId, YamlMappingNode>();
            foreach (var navigationMeshAsset in navigationMeshAssets)
            {
                using (var navigationMesh = navigationMeshAsset.AsYamlAsset())
                {
                    HashSet<ObjectId> selectedGroups = new HashSet<ObjectId>();
                    foreach (var setting in navigationMesh.DynamicRootNode.NavigationMeshAgentSettings)
                    {
                        var currentAgentSettings = setting.Value;
                        using (DigestStream digestStream = new DigestStream(Stream.Null))
                        {
                            BinarySerializationWriter writer = new BinarySerializationWriter(digestStream);
                            writer.Write((float)currentAgentSettings.Height);
                            writer.Write((float)currentAgentSettings.Radius);
                            writer.Write((float)currentAgentSettings.MaxClimb);
                            writer.Write((float)currentAgentSettings.MaxSlope.Radians);
                            if (!agentSettings.ContainsKey(digestStream.CurrentHash))
                                agentSettings.Add(digestStream.CurrentHash, currentAgentSettings.Node);
                            selectedGroups.Add(digestStream.CurrentHash);
                        }
                    }

                    // Replace agent settings with group reference on the navigation mesh
                    navigationMesh.DynamicRootNode.NavigationMeshAgentSettings = DynamicYamlEmpty.Default;
                    dynamic selectedGroupsMapping = navigationMesh.DynamicRootNode.SelectedGroups = new DynamicYamlMapping(new YamlMappingNode());
                    foreach (var selectedGroup in selectedGroups)
                    {
                        selectedGroupsMapping.AddChild(Guid.NewGuid().ToString("N"), selectedGroup.ToGuid().ToString("D"));
                    }
                }
            }

            // Add them to the game settings
            int groupIndex = 0;
            using (var gameSettings = gameSettingsAsset.AsYamlAsset())
            {
                var defaults = gameSettings.DynamicRootNode.Defaults;
                foreach (var setting in defaults)
                {
                    if (setting.Node.Tag == "!Xenko.Navigation.NavigationSettings,Xenko.Navigation")
                    {
                        var groups = setting.Groups as DynamicYamlArray;
                        foreach (var groupToAdd in agentSettings)
                        {
                            dynamic newGroup = new DynamicYamlMapping(new YamlMappingNode());
                            newGroup.Id = groupToAdd.Key.ToGuid().ToString("D");
                            newGroup.Name = $"Group {groupIndex++}";
                            newGroup.AgentSettings = groupToAdd.Value;
                            groups.Add(newGroup);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Splits skybox lighting functionality from background functionality
        /// </summary>
        private class SplitSkyboxLightingUpgrader
        {
            private readonly Dictionary<string, SkyboxAssetInfo> skyboxAssetInfos = new Dictionary<string, SkyboxAssetInfo>();
            
            public void UpgradeAsset(dynamic asset)
            {
                var parts = GetPartsArray(asset);
                foreach (dynamic part in parts)
                {
                    var entity = part.Entity;
                    var components = entity.Components;

                    List<ComponentInfo> skyboxInfos = new List<ComponentInfo>();
                    List<dynamic> skyboxKeys = new List<dynamic>();

                    // Find skybox components
                    foreach (dynamic component in components)
                    {
                        ComponentInfo componentInfo = GetComponentInfo(component);

                        if (componentInfo.Component.Node.Tag == "!SkyboxComponent")
                        {
                            skyboxInfos.Add(componentInfo);
                            skyboxKeys.Add(component);
                        }
                    }

                    if (skyboxInfos.Count == 0)
                        continue;

                    // Remove skybox light dependency on skybox component
                    foreach (var component in entity.Components)
                    {
                        ComponentInfo componentInfo = GetComponentInfo(component);
                        if (componentInfo.Component.Node.Tag == "!LightComponent")
                        {
                            var lightComponent = componentInfo.Component;
                            if (lightComponent.Type != null && lightComponent.Type.Node.Tag == "!LightSkybox")
                            {
                                // Use first skybox component
                                var skyboxInfo = skyboxInfos.First();
                                
                                // Combine light and skybox intensity into light intensity
                                var lightIntensity = lightComponent.Intensity;
                                var skyboxIntensity = skyboxInfo.Component.Intensity;
                                float intensity = (lightIntensity != null) ? lightIntensity : 1.0f;
                                intensity *= ((skyboxIntensity != null) ? (float)skyboxIntensity : 1.0f);
                                lightComponent.Intensity = intensity;

                                // Copy skybox assignment
                                lightComponent.Type["Skybox"] = (string)skyboxInfo.Component.Skybox;

                                // Check if this light is now referencing a removed skybox asset
                                string referenceId = ((string)skyboxInfo.Component.Skybox)?.Split('/').Last().Split(':').First();
                                if (referenceId == null || !skyboxAssetInfos.ContainsKey(referenceId) || skyboxAssetInfos[referenceId].Deleted)
                                {
                                    lightComponent.Type["Skybox"] = "null";
                                }

                                // 1 light per entity max.
                                break;
                            }
                        }
                    }

                    // Add background components
                    foreach (var skyboxInfo in skyboxInfos)
                    {
                        SkyboxAssetInfo skyboxAssetInfo;
                        if (skyboxInfo.Component.Skybox == null)
                            continue;

                        string referenceId = ((string)skyboxInfo.Component.Skybox).Split('/').Last().Split(':').First();
                        if (!skyboxAssetInfos.TryGetValue(referenceId, out skyboxAssetInfo))
                            continue;
                        
                        if (skyboxAssetInfo.IsBackground)
                        {
                            var backgroundComponentNode = new YamlMappingNode();
                            backgroundComponentNode.Tag = "!BackgroundComponent";
                            backgroundComponentNode.Add("Texture", skyboxAssetInfo.TextureReference);
                            if (skyboxInfo.Component.Intensity != null)
                                backgroundComponentNode.Add("Intensity", (string)skyboxInfo.Component.Intensity);
                            AddComponent(components, backgroundComponentNode, Guid.NewGuid());
                        }
                    }

                    // Remove skybox components
                    foreach (var skybox in skyboxKeys)
                    {
                        RemoveComponent(components, skybox);
                    }
                }
            }

            public void ProcessSkybox(PackageLoadingAssetFile skyboxAsset)
            {
                using (var skyboxYaml = skyboxAsset.AsYamlAsset())
                {
                    var root = skyboxYaml.DynamicRootNode;
                    var rootMapping = (DynamicYamlMapping)root;

                    string cubemapReference = "null";

                    // Insert cubmap into skybox root instead of in Model
                    if (root.Model != null)
                    {
                        if (root.Model.Node.Tag == "!SkyboxCubeMapModel")
                        {
                            cubemapReference = root.Model.CubeMap;
                        }
                        rootMapping.RemoveChild("Model");
                    }
                    rootMapping.AddChild("CubeMap", cubemapReference);
                    var splitReference = cubemapReference.Split('/'); // TODO
                    
                    // We will remove skyboxes that are only used as a background
                    if (root.Usage != null && (string)root.Usage == "Background")
                    {
                        skyboxAsset.Deleted = true;
                    }
                    
                    bool isBackground = root.Usage == null ||
                                        (string)root.Usage == "Background" ||
                                        (string)root.Usage == "LightingAndBackground";
                    skyboxAssetInfos.Add((string)root.Id, new SkyboxAssetInfo
                    {
                        TextureReference = splitReference.Last(),
                        IsBackground = isBackground,
                        Deleted = skyboxAsset.Deleted,
                    });
                }
            }

            private void AddComponent(dynamic componentsNode, YamlMappingNode node, Guid id)
            {
                // New format (1.9)
                DynamicYamlMapping mapping = (DynamicYamlMapping)componentsNode;
                mapping.AddChild(new YamlScalarNode(Guid.NewGuid().ToString("N")), node);
                node.Add("Id", id.ToString("D"));
            }

            private void RemoveComponent(dynamic componentsNode, dynamic componentsEntry)
            {
                // New format (1.9)
                DynamicYamlMapping mapping = (DynamicYamlMapping)componentsNode;
                mapping.RemoveChild(componentsEntry.Key);
            }

            private DynamicYamlArray GetPartsArray(dynamic asset)
            {
                var hierarchy = asset.Hierarchy;
                return (DynamicYamlArray)hierarchy.Parts; // > 1.6.0
            }
            
            private ComponentInfo GetComponentInfo(dynamic componentNode)
            {
                // New format (1.9)
                return new ComponentInfo
                {
                    Id = (string)componentNode.Key,
                    Component = componentNode.Value
                };
            }

            private struct SkyboxAssetInfo
            {
                public string TextureReference;
                public bool IsBackground;
                public bool Deleted;
            }

            private struct ComponentInfo
            {
                public string Id;
                public dynamic Component;
            }
        }

        private void ConvertNormalMapsInvertY(IList<PackageLoadingAssetFile> assetFiles)
        {
            var materialAssets = assetFiles.Where(f => f.FilePath.GetFileExtension() == ".xkmat").ToList();
            var textureAssets = assetFiles.Where(f => f.FilePath.GetFileExtension() == ".xktex").ToList();

            foreach (var materialFile in materialAssets)
            {
                if (!IsYamlAsset(materialFile))
                    continue;

                // This upgrader will also mark every yaml asset as dirty. We want to re-save everything with the new serialization system
                using (var yamlAsset = materialFile.AsYamlAsset())
                {
                    dynamic asset = yamlAsset.DynamicRootNode;

                    var assetTag = asset.Node.Tag;
                    if (assetTag != "!MaterialAsset")
                        continue;

                    if (asset.Attributes.Surface == null)
                        continue;

                    var surface = asset.Attributes.Surface;
                    var materialTag = surface.Node.Tag;
                    if (materialTag != "!MaterialNormalMapFeature")
                        continue;

                    var invertY = (asset.Attributes.Surface.InvertY == null || asset.Attributes.Surface.InvertY == "true");
                    if (invertY)
                        continue; // This is the default value for normal map textures, so no need to change it

                    // TODO Find all referenced files
                    if (asset.Attributes.Surface.NormalMap == null || asset.Attributes.Surface.NormalMap.Node.Tag != "!ComputeTextureColor")
                        continue;

                    dynamic texture = asset.Attributes.Surface.NormalMap.Texture;
                    var textureId = (string)texture.Node.Value;

                    foreach (var textureFile in textureAssets)
                    {
                        if (!IsYamlAsset(textureFile))
                            continue;

                        using (var yamlAssetTex = textureFile.AsYamlAsset())
                        {
                            dynamic assetTex = yamlAssetTex.DynamicRootNode;

                            var assetTagTex = assetTex.Node.Tag;
                            if (assetTagTex != "!Texture")
                                continue;

                            var assetIdTex = (string)assetTex.Id;
                            if (!textureId.Contains(assetIdTex))
                                continue;

                            assetTex["InvertY"] = false;
                        }
                    }
                }
            }
        }
    }
}
