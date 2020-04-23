// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Templates;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Settings;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using Stride.Assets.Materials;
using Stride.Assets.Models;
using Stride.Assets.Textures;
using Stride.Rendering;

namespace Stride.Assets.Presentation.Templates
{
    public static class ModelFromFileTemplateSettings
    {
        public static SettingsKey<bool> ImportMaterials = new SettingsKey<bool>("Templates/ModelFromFile/ImportMaterials", PackageUserSettings.SettingsContainer, true);
        public static SettingsKey<bool> DeduplicateMaterials = new SettingsKey<bool>("Templates/ModelFromFile/DeduplicateMaterials", PackageUserSettings.SettingsContainer, true);
        public static SettingsKey<bool> ImportTextures = new SettingsKey<bool>("Templates/ModelFromFile/ImportTextures", PackageUserSettings.SettingsContainer, true);
        public static SettingsKey<bool> ImportSkeleton = new SettingsKey<bool>("Templates/ModelFromFile/ImportSkeleton", PackageUserSettings.SettingsContainer, true);
        public static SettingsKey<AssetId> DefaultSkeleton = new SettingsKey<AssetId>("Templates/ModelFromFile/DefaultSkeleton", PackageUserSettings.SettingsContainer, AssetId.Empty);
    }

    public class ModelFromFileTemplateGenerator : AssetFromFileTemplateGenerator
    {
        public new static readonly ModelFromFileTemplateGenerator Default = new ModelFromFileTemplateGenerator();

        public static Guid Id = new Guid("3B778954-54C4-4FF3-97EF-4CD7AEA0B97D");

        protected static readonly PropertyKey<bool> ImportMaterialsKey = new PropertyKey<bool>("ImportMaterials", typeof(ModelFromFileTemplateGenerator));
        protected static readonly PropertyKey<bool> DeduplicateMaterialsKey = new PropertyKey<bool>("DeduplicateMaterials", typeof(ModelFromFileTemplateGenerator));
        protected static readonly PropertyKey<bool> ImportTexturesKey = new PropertyKey<bool>("ImportTextures", typeof(ModelFromFileTemplateGenerator));
        protected static readonly PropertyKey<bool> ImportSkeletonKey = new PropertyKey<bool>("ImportSkeleton", typeof(ModelFromFileTemplateGenerator));
        protected static readonly PropertyKey<Skeleton> SkeletonToUseKey = new PropertyKey<Skeleton>("SkeletonToUse", typeof(ModelFromFileTemplateGenerator));

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

            var showDeduplicateMaterialsCheckBox = files.Any(x => ImportAssimpCommand.IsSupportingExtensions(x.GetFileExtension()));
            var showFbxDedupeNotSupportedWarning = showDeduplicateMaterialsCheckBox && files.Any(x => ImportFbxCommand.IsSupportingExtensions(x.GetFileExtension()));
            // Load settings from the last time this template was used for this project
            var profile = parameters.Package.UserSettings.Profile;
            var window = new ModelAssetTemplateWindow
            {
                Parameters =
                {
                    ImportMaterials = ModelFromFileTemplateSettings.ImportMaterials.GetValue(profile, true),
                    ShowDeduplicateMaterialsCheckBox = showDeduplicateMaterialsCheckBox,
                    ShowFbxDedupeNotSupportedWarning = showFbxDedupeNotSupportedWarning,
                    DeduplicateMaterials = ModelFromFileTemplateSettings.DeduplicateMaterials.GetValue(profile, true),
                    ImportTextures = ModelFromFileTemplateSettings.ImportTextures.GetValue(profile, true),
                    ImportSkeleton = ModelFromFileTemplateSettings.ImportSkeleton.GetValue(profile, true)
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
            parameters.Tags.Set(ImportSkeletonKey, window.Parameters.ImportSkeleton);
            parameters.Tags.Set(SkeletonToUseKey, skeletonToReuse);

            // Save settings
            ModelFromFileTemplateSettings.ImportMaterials.SetValue(window.Parameters.ImportMaterials, profile);
            ModelFromFileTemplateSettings.DeduplicateMaterials.SetValue(window.Parameters.DeduplicateMaterials, profile);
            ModelFromFileTemplateSettings.ImportTextures.SetValue(window.Parameters.ImportTextures, profile);
            ModelFromFileTemplateSettings.ImportSkeleton.SetValue(window.Parameters.ImportSkeleton, profile);
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
            var importSkeleton = parameters.Tags.Get(ImportSkeletonKey);
            var skeletonToReuse = parameters.Tags.Get(SkeletonToUseKey);

            var importParameters = new AssetImporterParameters { Logger = parameters.Logger };
            importParameters.InputParameters.Set(ModelAssetImporter.DeduplicateMaterialsKey, deduplicateMaterials);
            importParameters.SelectedOutputTypes.Add(typeof(ModelAsset), true);
            importParameters.SelectedOutputTypes.Add(typeof(MaterialAsset), importMaterials);
            importParameters.SelectedOutputTypes.Add(typeof(TextureAsset), importTextures);
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

                var assets = importer.Import(file, importParameters).Select(x => new AssetItem(UPath.Combine(parameters.TargetLocation, x.Location), x.Asset)).ToList();

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
    }
}
