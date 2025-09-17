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

                if (splitHierarchy)
                {
                    var entityInfo = importer.GetEntityInfo(file, parameters.Logger, importParameters);
                    if (entityInfo != null)
                    {
                        // Build a mapping: (Mesh i) asset by index, plus the optional "(All)".
                        // We rely on your current naming convention created by the importer:
                        //   Base model (Mesh 1, at base name),
                        //   "(Mesh k)" for k >= 2,
                        //   and "(All)" if there were multiple meshes.
                        var baseName = file.GetFileNameWithoutExtension();

                        // Collect model assets we just imported
                        var modelAssetsByName = assets
        .Where(a => a.Asset is ModelAsset)
        .ToDictionary(a => a.Location.GetFileNameWithoutExtension(), a => a);


                        // Build an array aligned with entityInfo.Models order:
                        // entityInfo.Models[0] -> base model (Mesh 1),
                        // entityInfo.Models[k] -> "(Mesh k+1)"
                        var perMeshModels = new List<AssetItem>();
                        for (int i = 0; i < (entityInfo.Models?.Count ?? 0); i++)
                        {
                            string name = i == 0 ? baseName : $"{baseName} (Mesh {i + 1})";
                            if (modelAssetsByName.TryGetValue(name, out var item))
                                perMeshModels.Add(item);
                            else
                                perMeshModels.Add(null); // defensive
                        }

                        // Optional combined "(All)" model if it exists
                        modelAssetsByName.TryGetValue($"{baseName} (All)", out var allModelAsset);

                        // Actually build the prefab
                        var prefabAssetItem = BuildPrefabForSplitHierarchy(
      baseName,
      entityInfo,
      perMeshModels,
      allModelAsset,
      parameters.TargetLocation);

                        if (prefabAssetItem != null)
                        {
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

                // Create unique names amongst the list of assets
                importedAssets.AddRange(MakeUniqueNames(assets));
            }

            return importedAssets;
        }

        private static AssetItem BuildPrefabForSplitHierarchy(
       string baseName,
       EntityInfo entityInfo,
       IList<AssetItem> perMeshModels,   // index-aligned with entityInfo.Models
       AssetItem allModelAsset,          // currently unused (kept for future)
       UDirectory targetLocation)
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
                while (stack.Count > 0 && (stack.Count - 1) > node.Depth)
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

            // Done
            var prefabUrl = new UFile($"{baseName} Prefab");
            return new AssetItem(UPath.Combine(targetLocation, prefabUrl), prefab);
        }





    }
}
