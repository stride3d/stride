// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
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
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using Stride.Core.Serialization;
using Stride.Core.Settings;
using Stride.Engine;
using Stride.Rendering;
using Stride.Importer.Common;
using System.IO; 

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
                // TODO: should we allow to select the importer?
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

                if (splitHierarchy)
                {
                    var entityInfo = importer.GetEntityInfo(file, parameters.Logger, importParameters);
                    if (entityInfo != null && (entityInfo.Models?.Count ?? 0) > 0)
                    {
                        // Collect the first imported model (we'll clone/rename it per-mesh)
                        var firstModelItem = assets.FirstOrDefault(a => a.Asset is ModelAsset);
                        if (firstModelItem != null)
                        {
                            // Remove any model assets we just imported; we'll re-add per-mesh ones with proper names
                            assets.RemoveAll(a => a.Asset is ModelAsset);

                            var perMeshAssets = new List<AssetItem>();
                            for (int i = 0; i < entityInfo.Models.Count; i++)
                            {
                                var rawMeshName = entityInfo.Models[i].MeshName;
                                var meshPart = SanitizePart(rawMeshName) ?? $"Mesh-{i + 1}";
                                var desiredNoExt = $"{baseName}-{meshPart}";

                                // Ensure uniqueness *within this import batch*
                                var uniqueFile = MakeUniqueFileName(desiredNoExt, assets);

                                AssetItem itemForThisMesh;

                                if (i == 0)
                                {
                                    // Reuse the first imported model's ASSET, but give it a fresh Id before re-wrapping it
                                    var baseModel = (ModelAsset)firstModelItem.Asset;
                                    baseModel.Id = AssetId.New(); // <<—— ensure unique Id
                                    itemForThisMesh = new AssetItem(UPath.Combine(parameters.TargetLocation, uniqueFile), baseModel);
                                }
                                else
                                {
                                    // Clone the first model's asset and give it a fresh Id
                                    var clonedAsset = AssetCloner.Clone(firstModelItem.Asset);
                                    ((ModelAsset)clonedAsset).Id = AssetId.New(); // <<—— ensure unique Id
                                    itemForThisMesh = new AssetItem(UPath.Combine(parameters.TargetLocation, uniqueFile), clonedAsset);
                                }
                              
                                perMeshAssets.Add(itemForThisMesh);
                                assets.Add(itemForThisMesh); // keep list current so MakeUniqueFileName sees it
                            }


                            // Build prefab using these per-mesh models
                            var perMeshByName = assets
                                .Where(a => a.Asset is ModelAsset)
                                .ToDictionary(a => a.Location.GetFileNameWithoutExtension(), a => a, StringComparer.OrdinalIgnoreCase);

                            var perMeshModels = new List<AssetItem>(entityInfo.Models.Count);
                            for (int i = 0; i < entityInfo.Models.Count; i++)
                            {
                                var rawMeshName = entityInfo.Models[i].MeshName;
                                var meshPart = SanitizePart(rawMeshName) ?? $"Mesh-{i + 1}";
                                var expectedName = $"{baseName}-{meshPart}";
                                perMeshByName.TryGetValue(expectedName, out var item);
                                perMeshModels.Add(item);
                            }

                            // No combined "(All)" model when splitting; Prefab is the combined representation
                            AssetItem allModelAsset = null;

                            var prefabAssetItem = BuildPrefabForSplitHierarchy(
                                baseName,
                                entityInfo,
                                perMeshModels,
                                allModelAsset,
                                parameters.TargetLocation);

                            if (prefabAssetItem != null)
                                assets.Add(prefabAssetItem);
                        }
                    }
                }
                else
                {
                    // Split OFF: keep a single Model named exactly after the source file (no "(All)")
                    var idx = assets.FindIndex(a => a.Asset is ModelAsset);
                    if (idx >= 0)
                    {
                        var old = assets[idx];
                        assets.RemoveAt(idx);

                        // Assign a fresh Id to the single model to avoid any Id collisions
                        ((ModelAsset)old.Asset).Id = AssetId.New(); // <<—— ensure unique Id

                        var uniqueFile = MakeUniqueFileName(baseName, assets);
                        var renamed = new AssetItem(UPath.Combine(parameters.TargetLocation, uniqueFile), old.Asset);
                        assets.Insert(idx, renamed);
                    }
                }

                foreach (var model in assets.Select(x => x.Asset).OfType<ModelAsset>())
                {
                    if (skeletonToReuse != null)
                    {
                        model.Skeleton = skeletonToReuse;
                    }
                }

                // Create unique names amongst the list of assets
                importedAssets.AddRange(MakeUniqueNames(assets));
            }

            return importedAssets;
        }

        // Filters the asset-level materials list so this ModelAsset only keeps the material that matches `wantedMaterialName`
        // Keep only the material whose *asset name* (without extension) matches wantedMaterialName.
        // Works across Stride branches where ModelMaterial may expose either "Material" or "MaterialInstance".
        private static void KeepOnlyMaterialByName(ModelAsset modelAsset, string wantedMaterialName)
        {
            if (modelAsset?.Materials == null || modelAsset.Materials.Count == 0 || string.IsNullOrWhiteSpace(wantedMaterialName))
                return;

            var kept = new List<Stride.Assets.Models.ModelMaterial>();

            foreach (var mm in modelAsset.Materials)
            {
                // Try to get a reference object we can resolve via AttachedReferenceManager:
                // 1) ModelMaterial.Material (asset reference)
                // 2) ModelMaterial.MaterialInstance (runtime instance that still carries a reference)
                object materialRefObj = null;
                var mmType = mm.GetType();

                var propMaterial = mmType.GetProperty("Material");
                if (propMaterial != null)
                    materialRefObj = propMaterial.GetValue(mm);

                if (materialRefObj == null)
                {
                    var propMaterialInstance = mmType.GetProperty("MaterialInstance");
                    if (propMaterialInstance != null)
                        materialRefObj = propMaterialInstance.GetValue(mm);
                }

                // Resolve the attached reference (if any) to get the asset URL
                string assetUrl = null;
                if (materialRefObj != null)
                {
                    var aref = AttachedReferenceManager.GetAttachedReference(materialRefObj);
                    assetUrl = aref?.Url; // this is a string in your branch
                }

                // Compare by asset name (no extension)
                if (!string.IsNullOrEmpty(assetUrl))
                {
                    var nameNoExt = Path.GetFileNameWithoutExtension(assetUrl);
                    if (string.Equals(nameNoExt, wantedMaterialName, StringComparison.OrdinalIgnoreCase))
                    {
                        kept.Add(mm);
                    }
                }
            }

            if (kept.Count > 0)
            {
                modelAsset.Materials.Clear();
                modelAsset.Materials.AddRange(kept);
            }
            else if (modelAsset.Materials.Count > 1)
            {
                // Fallback so the asset stays valid if we couldn't match by name
                modelAsset.Materials.RemoveRange(1, modelAsset.Materials.Count - 1);
            }
        }

        private static AssetItem BuildPrefabForSplitHierarchy(string baseName, EntityInfo entityInfo, IList<AssetItem> perMeshModels, AssetItem allModelAsset, UDirectory targetLocation)
        {
            if (entityInfo?.Nodes == null || entityInfo.Nodes.Count == 0)
                return null;

            // 1) Create entities in pre-order and rebuild parent/child relations using Depth
            var entities = new List<Entity>(entityInfo.Nodes.Count);
            var stack = new Stack<Entity>();
            Entity root = null;

            for (int i = 0; i < entityInfo.Nodes.Count; i++)
            {
                var node = entityInfo.Nodes[i];
                var e = new Entity(string.IsNullOrEmpty(node.Name) ? $"Node_{i}" : node.Name);

                // Keep the stack at (node.Depth) entries so parent is at depth-1
                while (stack.Count > node.Depth)
                    stack.Pop();

                if (stack.Count == 0)
                {
                    // Depth 0 → root
                    root = e;
                }
                else
                {
                    stack.Peek().AddChild(e);
                }

                stack.Push(e);
                entities.Add(e);
            }

            // 2) Attach ModelComponent to nodes that host meshes (match by NodeName)
            if (entityInfo.Models != null && entityInfo.Models.Count > 0)
            {
                var nodeNameToIndex = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int i = 0; i < entityInfo.Nodes.Count; i++)
                {
                    var n = entityInfo.Nodes[i].Name;
                    if (!string.IsNullOrEmpty(n) && !nodeNameToIndex.ContainsKey(n))
                        nodeNameToIndex.Add(n, i);
                }

                for (int m = 0; m < entityInfo.Models.Count; m++)
                {
                    var meshInfo = entityInfo.Models[m];
                    if (string.IsNullOrEmpty(meshInfo.NodeName))
                        continue;

                    if (!nodeNameToIndex.TryGetValue(meshInfo.NodeName, out var nodeIndex))
                        continue;

                    var modelItem = (m >= 0 && m < perMeshModels.Count) ? perMeshModels[m] : null;
                    if (modelItem?.Asset is ModelAsset)
                    {
                        var mc = new ModelComponent
                        {
                            // Reference the imported Model asset (no duplication)
                            Model = AttachedReferenceManager.CreateProxyObject<Model>(modelItem.Id, modelItem.Location)
                        };
                        entities[nodeIndex].Components.Add(mc);
                    }
                }
            }

            root ??= entities[0];

            var firstNode = entityInfo.Nodes[0];
            var firstName = firstNode.Name ?? string.Empty;
            bool looksLikeWrapper =
                string.IsNullOrWhiteSpace(firstName)
                || firstName.Equals(baseName, StringComparison.OrdinalIgnoreCase)
                || firstName.Equals("RootNode", StringComparison.OrdinalIgnoreCase);

            // Use the constructed runtime tree to check children count
            if (looksLikeWrapper && root.Transform.Children.Count == 1)
            {
                var onlyChild = root.Transform.Children[0].Entity;
                // Detach so it becomes the actual root
                onlyChild.Transform.Parent = null;
                root = onlyChild;
            }

            // 3) Build Prefab: register ALL entities in Parts, add ROOT entity to RootParts
            var prefab = new PrefabAsset();

            // Parts: Guid -> EntityDesign (use the entity's Id as the key)
            foreach (var e in entities)
            {
                var design = new EntityDesign(e);
                prefab.Hierarchy.Parts.Add(e.Id, design);
            }
            // RootParts: list of Entity (your API expects Entity here)
            prefab.Hierarchy.RootParts.Add(root);

            prefab.Id = AssetId.New(); // <<—— ensure unique Id
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

        // Ensure a *file-name without extension* is unique within the current 'assets' list.
        // It yields: name, name-1, name-2, ...
        private static UFile MakeUniqueFileName(string desiredNameNoExt, List<AssetItem> assets)
        {
            var existing = new HashSet<string>(
                assets.Select(a => a.Location.GetFileNameWithoutExtension()),
                StringComparer.OrdinalIgnoreCase);

            var name = desiredNameNoExt;
            var i = 0;
            while (existing.Contains(name))
                name = $"{desiredNameNoExt}-{++i}";

            return new UFile(name); // filename only; caller will UPath.Combine with target dir
        }

    }
}
