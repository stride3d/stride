// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Materials;
using Stride.Assets.Models;
using Stride.Assets.Textures;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Settings;
using Stride.Rendering;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// The options of the last model import of a project (see <see cref="ModelFromFileTemplateGenerator"/>), in the user settings of
/// the package; the WPF editor shares the keys.
/// </summary>
public static class ModelFromFileTemplateSettings
{
    public static SettingsKey<bool> ImportMaterials { get; } = new("Templates/ModelFromFile/ImportMaterials", PackageUserSettings.SettingsContainer, true);
    public static SettingsKey<bool> DeduplicateMaterials { get; } = new("Templates/ModelFromFile/DeduplicateMaterials", PackageUserSettings.SettingsContainer, true);
    public static SettingsKey<bool> ImportTextures { get; } = new("Templates/ModelFromFile/ImportTextures", PackageUserSettings.SettingsContainer, true);
    public static SettingsKey<bool> ImportAnimations { get; } = new("Templates/ModelFromFile/ImportAnimations", PackageUserSettings.SettingsContainer, true);
    public static SettingsKey<bool> ImportSkeleton { get; } = new("Templates/ModelFromFile/ImportSkeleton", PackageUserSettings.SettingsContainer, true);
    public static SettingsKey<AssetId> DefaultSkeleton { get; } = new("Templates/ModelFromFile/DefaultSkeleton", PackageUserSettings.SettingsContainer, AssetId.Empty);
}

/// <summary>
/// Imports a model file (and its materials, textures, animations and skeleton as the user chooses, see
/// <see cref="IModelImportPrompt"/>).
/// </summary>
public sealed class ModelFromFileTemplateGenerator : AssetFromFileTemplateGenerator
{
    public static readonly Guid Id = new("3B778954-54C4-4FF3-97EF-4CD7AEA0B97D");

    private static readonly PropertyKey<bool> ImportMaterialsKey = new("ImportMaterials", typeof(ModelFromFileTemplateGenerator));
    private static readonly PropertyKey<bool> DeduplicateMaterialsKey = new("DeduplicateMaterials", typeof(ModelFromFileTemplateGenerator));
    private static readonly PropertyKey<bool> ImportTexturesKey = new("ImportTextures", typeof(ModelFromFileTemplateGenerator));
    private static readonly PropertyKey<bool> ImportAnimationsKey = new("ImportAnimations", typeof(ModelFromFileTemplateGenerator));
    private static readonly PropertyKey<bool> ImportSkeletonKey = new("ImportSkeleton", typeof(ModelFromFileTemplateGenerator));
    private static readonly PropertyKey<Skeleton?> SkeletonToUseKey = new("SkeletonToUse", typeof(ModelFromFileTemplateGenerator));

    private readonly IModelImportPrompt prompt;

    /// <param name="prompt">The dialog asking the options of the import.</param>
    public ModelFromFileTemplateGenerator(IModelImportPrompt prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        this.prompt = prompt;
    }

    /// <inheritdoc/>
    public override bool IsSupportingTemplate(TemplateDescription templateDescription)
    {
        ArgumentNullException.ThrowIfNull(templateDescription);
        return templateDescription.Id == Id;
    }

    /// <inheritdoc/>
    protected override async Task<bool> PrepareAssetCreation(AssetTemplateGeneratorParameters parameters)
    {
        if (!await base.PrepareAssetCreation(parameters))
            return false;

        var files = parameters.Tags.Get(SourceFilesPathKey);
        if (files is null)
            return true;

        var showDeduplicateMaterialsCheckBox = files.Any(x => ImportThreeDCommand.IsSupportingExtensions(x.GetFileExtension()));
        // Load settings from the last time this template was used for this project
        var profile = parameters.Package.UserSettings.Profile;
        var session = Session;
        var options = new ImportModelFromFileViewModel(session)
        {
            ImportMaterials = ModelFromFileTemplateSettings.ImportMaterials.GetValue(profile, true),
            ShowDeduplicateMaterialsCheckBox = showDeduplicateMaterialsCheckBox,
            ShowFbxDedupeNotSupportedWarning = false,
            DeduplicateMaterials = ModelFromFileTemplateSettings.DeduplicateMaterials.GetValue(profile, true),
            ImportTextures = ModelFromFileTemplateSettings.ImportTextures.GetValue(profile, true),
            ImportAnimations = ModelFromFileTemplateSettings.ImportAnimations.GetValue(profile, true),
            ImportSkeleton = ModelFromFileTemplateSettings.ImportSkeleton.GetValue(profile, true),
        };

        var skeletonId = ModelFromFileTemplateSettings.DefaultSkeleton.GetValue(profile, true);
        var skeleton = skeletonId != AssetId.Empty ? session.GetAssetById(skeletonId) : null;
        if (skeleton is not null)
        {
            options.SkeletonToReuse = skeleton;
            options.ReuseSkeleton = true;
        }

        try
        {
            if (!await prompt.PromptAsync(options))
                return false;

            // Apply settings
            var skeletonToReuse = options.ReuseSkeleton && options.SkeletonToReuse is not null ? ContentReferenceHelper.CreateReference<Skeleton>(options.SkeletonToReuse) : null;
            parameters.Tags.Set(ImportMaterialsKey, options.ImportMaterials);
            parameters.Tags.Set(DeduplicateMaterialsKey, options.DeduplicateMaterials);
            parameters.Tags.Set(ImportTexturesKey, options.ImportTextures);
            parameters.Tags.Set(ImportAnimationsKey, options.ImportAnimations);
            parameters.Tags.Set(ImportSkeletonKey, options.ImportSkeleton);
            parameters.Tags.Set(SkeletonToUseKey, skeletonToReuse);

            // Save settings
            ModelFromFileTemplateSettings.ImportMaterials.SetValue(options.ImportMaterials, profile);
            ModelFromFileTemplateSettings.DeduplicateMaterials.SetValue(options.DeduplicateMaterials, profile);
            ModelFromFileTemplateSettings.ImportTextures.SetValue(options.ImportTextures, profile);
            ModelFromFileTemplateSettings.ImportAnimations.SetValue(options.ImportAnimations, profile);
            ModelFromFileTemplateSettings.ImportSkeleton.SetValue(options.ImportSkeleton, profile);
            skeletonId = AttachedReferenceManager.GetAttachedReference(skeletonToReuse)?.Id ?? AssetId.Empty;
            ModelFromFileTemplateSettings.DefaultSkeleton.SetValue(skeletonId, profile);
            parameters.Package.UserSettings.Save();
            return true;
        }
        finally
        {
            options.Destroy();
        }
    }

    /// <inheritdoc/>
    protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
    {
        var files = parameters.Tags.Get(SourceFilesPathKey);
        if (files is null)
            return base.CreateAssets(parameters);

        var importMaterials = parameters.Tags.Get(ImportMaterialsKey);
        var deduplicateMaterials = parameters.Tags.Get(DeduplicateMaterialsKey);
        var importTextures = parameters.Tags.Get(ImportTexturesKey);
        var importAnimations = parameters.Tags.Get(ImportAnimationsKey);
        var importSkeleton = parameters.Tags.Get(ImportSkeletonKey);
        var skeletonToReuse = parameters.Tags.Get(SkeletonToUseKey);

        var importParameters = new AssetImporterParameters { Logger = parameters.Logger };
        importParameters.InputParameters.Set(ModelAssetImporter.DeduplicateMaterialsKey, deduplicateMaterials);
        importParameters.SelectedOutputTypes.Add(typeof(ModelAsset), true);
        importParameters.SelectedOutputTypes.Add(typeof(MaterialAsset), importMaterials);
        importParameters.SelectedOutputTypes.Add(typeof(TextureAsset), importTextures);
        importParameters.SelectedOutputTypes.Add(typeof(AnimationAsset), importAnimations);
        importParameters.SelectedOutputTypes.Add(typeof(SkeletonAsset), importSkeleton);

        return ImportFiles(parameters, files, importParameters, skeletonToReuse);
    }

    /// <summary>
    /// Imports model files with the model importer of each file, into the target folder of the parameters.
    /// </summary>
    /// <param name="parameters">The parameters of the template.</param>
    /// <param name="files">The model files.</param>
    /// <param name="importParameters">What to import.</param>
    /// <param name="skeletonToReuse">The skeleton the imported models use instead of their own, or <c>null</c>.</param>
    internal static List<AssetItem> ImportFiles(AssetTemplateGeneratorParameters parameters, IEnumerable<UFile> files, AssetImporterParameters importParameters, Skeleton? skeletonToReuse = null)
    {
        var importedAssets = new List<AssetItem>();
        foreach (var file in files)
        {
            // TODO: should we allow to select the importer?
            var importer = AssetRegistry.FindImporterForFile(file).OfType<ModelAssetImporter>().FirstOrDefault();
            if (importer is null)
            {
                parameters.Logger.Warning($"No importer found for file \"{file}\"");
                continue;
            }

            var assets = importer.Import(file, importParameters).Select(x => new AssetItem(UPath.Combine(parameters.TargetLocation, x.Location), x.Asset)).ToList();

            if (skeletonToReuse is not null)
            {
                foreach (var model in assets.Select(x => x.Asset).OfType<ModelAsset>())
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
