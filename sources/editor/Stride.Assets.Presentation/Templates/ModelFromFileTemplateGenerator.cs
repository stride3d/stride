// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO; 
using System.Linq;
using System.Threading.Tasks;
using Stride.Assets.Entities;
using Stride.Assets.Materials;
using Stride.Assets.Models;
using Stride.Assets.Textures;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using Stride.Core.Serialization;
using Stride.Core.Settings;
using Stride.Engine;
using Stride.Importer.Common;
using Stride.Rendering;

namespace Stride.Assets.Presentation.Templates
{
    public static class ModelFromFileTemplateSettings
    {
        public static SettingsKey<bool> ImportMaterials = new SettingsKey<bool>("Templates/ModelFromFile/ImportMaterials", PackageUserSettings.SettingsContainer, true);
        public static SettingsKey<bool> DeduplicateMaterials = new SettingsKey<bool>("Templates/ModelFromFile/DeduplicateMaterials", PackageUserSettings.SettingsContainer, true);
        public static SettingsKey<bool> ImportTextures = new SettingsKey<bool>("Templates/ModelFromFile/ImportTextures", PackageUserSettings.SettingsContainer, true);
        public static SettingsKey<bool> ImportAnimations = new SettingsKey<bool>("Templates/ModelFromFile/ImportAnimations", PackageUserSettings.SettingsContainer, true);
        public static SettingsKey<bool> ImportSkeleton = new SettingsKey<bool>("Templates/ModelFromFile/ImportSkeleton", PackageUserSettings.SettingsContainer, true);
        public static SettingsKey<AssetId> DefaultSkeleton = new SettingsKey<AssetId>("Templates/ModelFromFile/DefaultSkeleton", PackageUserSettings.SettingsContainer, AssetId.Empty);
        public static SettingsKey<bool> SplitHierarchy = new SettingsKey<bool>("Templates/ModelFromFile/SplitHierarchy", PackageUserSettings.SettingsContainer, false);
    }

    public class ModelFromFileTemplateGenerator : AssetFromFileTemplateGenerator
    {
        public new static readonly ModelFromFileTemplateGenerator Default = new ModelFromFileTemplateGenerator();

        public static Guid Id = new Guid("3B778954-54C4-4FF3-97EF-4CD7AEA0B97D");

        protected static readonly PropertyKey<bool> ImportMaterialsKey = new PropertyKey<bool>("ImportMaterials", typeof(ModelFromFileTemplateGenerator));
        protected static readonly PropertyKey<bool> DeduplicateMaterialsKey = new PropertyKey<bool>("DeduplicateMaterials", typeof(ModelFromFileTemplateGenerator));
        protected static readonly PropertyKey<bool> ImportTexturesKey = new PropertyKey<bool>("ImportTextures", typeof(ModelFromFileTemplateGenerator));
        protected static readonly PropertyKey<bool> ImportAnimationsKey = new PropertyKey<bool>("ImportAnimations", typeof(ModelFromFileTemplateGenerator));
        protected static readonly PropertyKey<bool> ImportSkeletonKey = new PropertyKey<bool>("ImportSkeleton", typeof(ModelFromFileTemplateGenerator));
        protected static readonly PropertyKey<Skeleton> SkeletonToUseKey = new PropertyKey<Skeleton>("SkeletonToUse", typeof(ModelFromFileTemplateGenerator));
        protected static readonly PropertyKey<bool> SplitHierarchyKey = new PropertyKey<bool>("SplitHierarchy", typeof(ModelFromFileTemplateGenerator));

        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            return templateDescription.Id == Id;
        }

        protected override async Task<bool> PrepareAssetCreation(AssetTemplateGeneratorParameters parameters)
        {
            var result = await base.PrepareAssetCreation(parameters);
            if (!result)
                return false;

            var files = parameters.Tags.Get(SourceFilesPathKey);
            if (files == null)
                return true;

            var showDeduplicateMaterialsCheckBox = files.Any(x => ImportThreeDCommand.IsSupportingExtensions(x.GetFileExtension()));
            // Load settings from the last time this template was used for this project
            var profile = parameters.Package.UserSettings.Profile;
            var window = new ModelAssetTemplateWindow
            {
                Parameters =
                {
                    ImportMaterials = ModelFromFileTemplateSettings.ImportMaterials.GetValue(profile, true),
                    ShowDeduplicateMaterialsCheckBox = showDeduplicateMaterialsCheckBox,
                    ShowFbxDedupeNotSupportedWarning = false,
                    DeduplicateMaterials = ModelFromFileTemplateSettings.DeduplicateMaterials.GetValue(profile, true),
                    ImportTextures = ModelFromFileTemplateSettings.ImportTextures.GetValue(profile, true),
                    ImportAnimations = ModelFromFileTemplateSettings.ImportAnimations.GetValue(profile, true),
                    ImportSkeleton = ModelFromFileTemplateSettings.ImportSkeleton.GetValue(profile, true),
                    SplitHierarchy = ModelFromFileTemplateSettings.SplitHierarchy.GetValue(profile, false)
                }
            };

            var skeletonId = ModelFromFileTemplateSettings.DefaultSkeleton.GetValue();
            var skeleton = SessionViewModel.Instance?.GetAssetById(skeletonId);
            if (skeleton != null)
            {
                window.Parameters.ReuseSkeleton = true;
                window.Parameters.SkeletonToReuse = ContentReferenceHelper.CreateReference<Skeleton>(skeleton);
            }

            await window.ShowModal();

            if (window.Result == DialogResult.Cancel)
                return false;

            // Apply settings
            var skeletonToReuse = window.Parameters.ReuseSkeleton ? window.Parameters.SkeletonToReuse : null;
            parameters.Tags.Set(ImportMaterialsKey, window.Parameters.ImportMaterials);
            parameters.Tags.Set(DeduplicateMaterialsKey, window.Parameters.DeduplicateMaterials);
            parameters.Tags.Set(ImportTexturesKey, window.Parameters.ImportTextures);
            parameters.Tags.Set(ImportAnimationsKey, window.Parameters.ImportAnimations);
            parameters.Tags.Set(ImportSkeletonKey, window.Parameters.ImportSkeleton);
            parameters.Tags.Set(SkeletonToUseKey, skeletonToReuse);
            parameters.Tags.Set(SplitHierarchyKey, window.Parameters.SplitHierarchy);

            // Save settings
            ModelFromFileTemplateSettings.ImportMaterials.SetValue(window.Parameters.ImportMaterials, profile);
            ModelFromFileTemplateSettings.DeduplicateMaterials.SetValue(window.Parameters.DeduplicateMaterials, profile);
            ModelFromFileTemplateSettings.ImportTextures.SetValue(window.Parameters.ImportTextures, profile);
            ModelFromFileTemplateSettings.ImportAnimations.SetValue(window.Parameters.ImportAnimations, profile);
            ModelFromFileTemplateSettings.ImportSkeleton.SetValue(window.Parameters.ImportSkeleton, profile);
            ModelFromFileTemplateSettings.SplitHierarchy.SetValue(window.Parameters.SplitHierarchy, profile);
            skeletonId = AttachedReferenceManager.GetAttachedReference(skeletonToReuse)?.Id ?? AssetId.Empty;
            ModelFromFileTemplateSettings.DefaultSkeleton.SetValue(skeletonId, profile);
            parameters.Package.UserSettings.Save();

            return true;
        }

        protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
        {
            var files = parameters.Tags.Get(SourceFilesPathKey);
            if (files == null)
                return base.CreateAssets(parameters);

            var importMaterials = parameters.Tags.Get(ImportMaterialsKey);
            var deduplicateMaterials = parameters.Tags.Get(DeduplicateMaterialsKey);
            var importTextures = parameters.Tags.Get(ImportTexturesKey);
            var importAnimations = parameters.Tags.Get(ImportAnimationsKey);
            var importSkeleton = parameters.Tags.Get(ImportSkeletonKey);
            var skeletonToReuse = parameters.Tags.Get(SkeletonToUseKey);
            var splitHierarchy = parameters.Tags.Get(SplitHierarchyKey);   // <-- you read it here

            var importParameters = new AssetImporterParameters { Logger = parameters.Logger };
            importParameters.InputParameters.Set(ModelAssetImporter.DeduplicateMaterialsKey, deduplicateMaterials);
            importParameters.InputParameters.Set(ModelAssetImporter.SplitHierarchyKey, splitHierarchy); 
            importParameters.SelectedOutputTypes.Add(typeof(ModelAsset), true);
            importParameters.SelectedOutputTypes.Add(typeof(MaterialAsset), importMaterials);
            importParameters.SelectedOutputTypes.Add(typeof(TextureAsset), importTextures);
            importParameters.SelectedOutputTypes.Add(typeof(AnimationAsset), importAnimations);
            importParameters.SelectedOutputTypes.Add(typeof(SkeletonAsset), importSkeleton);

            var importedAssets = new List<AssetItem>();

            foreach (var file in files)
            {
                var importer = AssetRegistry.FindImporterForFile(file).OfType<ModelAssetImporter>().FirstOrDefault();
                if (importer == null)
                {
                    parameters.Logger.Warning($"No importer found for file \"{file}\"");
                    continue;
                }

                var assets = importer.Import(file, importParameters)
                    .Select(x => new AssetItem(UPath.Combine(parameters.TargetLocation, x.Location), x.Asset))
                    .ToList();

                var baseName = file.GetFileNameWithoutExtension();

                //Find, unique name each item before building prefab  
                if (splitHierarchy)
                {
                    var entityInfo = importer.GetEntityInfo(file, parameters.Logger, importParameters);
                    if (entityInfo?.Models.Count > 0)
                    {
                        var firstModelItem = assets.FirstOrDefault(a => a.Asset is ModelAsset);
                        if (firstModelItem != null)
                        {
                            assets.RemoveAll(a => a.Asset is ModelAsset);

                            var perMeshAssets = new List<AssetItem>();
                            var partCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                            for (int i = 0; i < entityInfo.Models.Count; i++)
                            {
                                var rawMeshName = entityInfo.Models[i].MeshName;
                                var meshPart = SanitizePart(rawMeshName) ?? $"Mesh-{i + 1}";

                                if (!partCounts.TryGetValue(meshPart, out var dupCount))
                                    dupCount = 0;
                                partCounts[meshPart] = dupCount + 1;

                                var disambiguated = (dupCount == 0) ? meshPart : $"{meshPart}-{dupCount + 1}";
                                var desiredNoExt = $"{baseName}-{disambiguated}";

                                var uniqueFile = MakeUniqueFileName(desiredNoExt, assets);

                                AssetItem itemForThisMesh;

                                if (i == 0)
                                {                 
                                    var baseModel = (ModelAsset)firstModelItem.Asset;
                                    baseModel.Id = AssetId.New();
                                    baseModel.MeshName = rawMeshName;
                                    itemForThisMesh = new AssetItem(UPath.Combine(parameters.TargetLocation, uniqueFile), baseModel);
                                }
                                else
                                {
                                    var clonedAsset = AssetCloner.Clone(firstModelItem.Asset);
                                    ((ModelAsset)clonedAsset).Id = AssetId.New();
                                    ((ModelAsset)clonedAsset).MeshName = rawMeshName;
                                    itemForThisMesh = new AssetItem(UPath.Combine(parameters.TargetLocation, uniqueFile), clonedAsset);
                                }

                                perMeshAssets.Add(itemForThisMesh);
                                assets.Add(itemForThisMesh); 
                            }

                            //Assign materials 
                            foreach (var item in perMeshAssets)
                            {
                                ResetMaterialsOnPrefabItems(item, entityInfo);                              
                            }       

                            var prefabAssetItem = BuildPrefabForSplitHierarchy(
                                baseName,
                                entityInfo,
                                perMeshAssets,
                                parameters.TargetLocation);


                            if (prefabAssetItem != null)
                                assets.Add(prefabAssetItem);
                        }
                    }
                }
            

                foreach (var model in assets.Select(x => x.Asset).OfType<ModelAsset>())
                {
                    if (skeletonToReuse != null)
                    {
                        model.Skeleton = skeletonToReuse;
                    }
                }

                importedAssets.AddRange(MakeUniqueNames(assets));
            }

            return importedAssets;
        }

        private static void ResetMaterialsOnPrefabItems(AssetItem assetItem,  EntityInfo entityInfo)
        {
            ModelAsset asset = assetItem?.Asset as ModelAsset;
            if (asset == null || asset.Materials==null)
                return;

            var underlyingModel=entityInfo.Models.Where(C=>C.MeshName==asset.MeshName).FirstOrDefault();          
            var nodeContainingMesh=entityInfo.Nodes.Where(c=>c.Name== underlyingModel.NodeName).FirstOrDefault();

            var materialIndices=entityInfo.NodeNameToMaterialIndices?.Where(c=>c.Key== nodeContainingMesh.Name)?.FirstOrDefault().Value;
        
            if(materialIndices?.Count()< 1)  
                return; 

            List<ModelMaterial> materialsToApply = null;
            for (int i = 0; i < asset.Materials.Count; i++)
            {
                if (materialIndices.Contains(i))
                {
                    (materialsToApply??=new List<ModelMaterial>()).Add(asset.Materials[i]);
                }
            }

            asset.Materials.Clear();
            materialsToApply?.ForEach(_mat => asset.Materials.Add(_mat));          
        }

        private static AssetItem? BuildPrefabForSplitHierarchy(string baseName, EntityInfo entityInfo, IList<AssetItem> perMeshModels, UDirectory targetLocation)
        {
            if (entityInfo?.Nodes == null || entityInfo.Nodes.Count == 0)
                return null;

            // Step 1. Set up entites transversing the tree from imported entityinfo 
            var entities = new List<Entity>(entityInfo.Nodes.Count);
            var stack = new Stack<Entity>();
            Entity root = null;

            for (int i = 0; i < entityInfo.Nodes.Count; i++)
            {
                var node = entityInfo.Nodes[i];
                var e = new Entity(string.IsNullOrEmpty(node.Name) ? $"Node_{i}" : node.Name);

                while (stack.Count > node.Depth)
                    stack.Pop();

                if (stack.Count == 0)
                {       
                    root = e;
                }
                else
                {
                    stack.Peek().AddChild(e);
                }

                stack.Push(e);
                entities.Add(e);
            }


            // Step 2. Apply TRS on each entity to that of imported source file
            for (int i = 0; i < entityInfo.Nodes.Count; i++)
            {
                entities[i].Transform.Position = entityInfo.Nodes[i].Position;
                entities[i].Transform.Rotation = entityInfo.Nodes[i].Rotation;
                entities[i].Transform.Scale = entityInfo.Nodes[i].Scale;
            }

            //Step 3. Attach ModelComponent and set up hierachical order 
            if (entityInfo?.Models?.Count > 0)
            {
                var nodeNameToIndex = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int i = 0; i < entityInfo.Nodes.Count; i++)
                {
                    var n = entityInfo.Nodes[i].Name;
                    if (!string.IsNullOrEmpty(n) && !nodeNameToIndex.ContainsKey(n))
                        nodeNameToIndex.Add(n, i);
                }

                var extraChildCountByNode = new Dictionary<int, int>();

                for (int m = 0; m < entityInfo.Models.Count; m++)
                {

                    var meshInfo = entityInfo.Models[m];

                    var nodeIndex = nodeNameToIndex[meshInfo.NodeName];
 
                    var modelItem =  perMeshModels[m];
                    if (modelItem?.Asset is ModelAsset)
                    {
                        var mc = new ModelComponent
                        {
                            Model = AttachedReferenceManager.CreateProxyObject<Model>(modelItem.Id, modelItem.Location)
                        };

                        var host = entities[nodeIndex];

                        
                        if (host.Get<ModelComponent>() != null)
                        {
                            if (!extraChildCountByNode.TryGetValue(nodeIndex, out var counter))
                                counter = 0;
                            counter++;
                            extraChildCountByNode[nodeIndex] = counter;

                            var childName = string.IsNullOrEmpty(meshInfo.NodeName)
                                ? $"Mesh_{m}"
                                : $"{meshInfo.NodeName}_Mesh{counter}";

                            var child = new Entity(childName);
                            host.AddChild(child);

                            entities.Add(child);

                            host = child; 
                        }

                        host.Components.Add(mc); 
                    }
                }
            }

            root ??= entities[0];

            // Heuristic: collapse trivial wrapper root (same name/base or "RootNode") with a single child
            var firstNode = entityInfo.Nodes[0];
            var firstName = firstNode.Name ?? string.Empty;
            bool looksLikeWrapper =
                firstName.Equals(baseName, StringComparison.OrdinalIgnoreCase)
                ||
                firstName.Equals("RootNode", StringComparison.OrdinalIgnoreCase);

           
            if (looksLikeWrapper && root.Transform.Children.Count == 1)
            {
                var onlyChild = root.Transform.Children[0].Entity;
                onlyChild.Transform.Parent = null;
                root = onlyChild;
            }

            var prefab = new PrefabAsset();

            foreach (var e in entities)
            {
                var design = new EntityDesign(e);
                prefab.Hierarchy.Parts.Add(e.Id, design);
            }

            prefab.Hierarchy.RootParts.Add(root);

            prefab.Id = AssetId.New(); // ensure unique Id
            var prefabUrl = new UFile($"{baseName} Prefab");
            return new AssetItem(UPath.Combine(targetLocation, prefabUrl), prefab);
        }

        private static string SanitizePart(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var clean = new string(s.Where(ch => !invalid.Contains(ch)).ToArray()).Trim();
            return string.IsNullOrEmpty(clean) ? null : clean;
        }

        private static UFile MakeUniqueFileName(string desiredNameNoExt, List<AssetItem> assets)
        {
            var existing = new HashSet<string>(
                assets.Select(a => a.Location.GetFileNameWithoutExtension()),
                StringComparer.OrdinalIgnoreCase);

            var name = desiredNameNoExt;
            var i = 0;
            while (existing.Contains(name))
                name = $"{desiredNameNoExt}-{++i}";

            return new UFile(name); 
        }

    }
}
