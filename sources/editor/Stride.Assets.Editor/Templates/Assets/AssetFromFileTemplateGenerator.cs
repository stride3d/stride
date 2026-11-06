// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Internal;
using Stride.Core.Assets.IO;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Reflection;
using Stride.Core.Translation;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// Creates assets from source files (textures, sounds, raw assets...): one per file, through the importer of the file when there
/// is one, with the file as the source otherwise. Without files, the user is asked for some, or to create the asset without a
/// source.
/// </summary>
public class AssetFromFileTemplateGenerator : AssetFactoryTemplateGenerator
{
    /// <summary>
    /// The source files picked for the assets (see <see cref="PrepareAssetCreation"/>); none when the asset is created without one.
    /// </summary>
    protected static readonly PropertyKey<IEnumerable<UFile>?> SourceFilesPathKey = new("SourceFilesPathKey", typeof(AssetFromFileTemplateGenerator));

    /// <inheritdoc/>
    public override bool IsSupportingTemplate(TemplateDescription templateDescription)
    {
        if (templateDescription is not TemplateAssetFactoryDescription description)
            return false;
        var assetType = description.GetAssetType();
        return base.IsSupportingTemplate(templateDescription) && description.ImportSource && typeof(IAssetWithSource).IsAssignableFrom(assetType);
    }

    /// <inheritdoc/>
    protected override async Task<bool> PrepareAssetCreation(AssetTemplateGeneratorParameters parameters)
    {
        var description = (TemplateAssetDescription)parameters.Description;
        var assetType = description.GetAssetType();
        var files = parameters.SourceFiles.Count > 0 ? parameters.SourceFiles : await BrowseForSourceFilesAsync(description, true);
        while (files is null)
        {
            var assetTypeName = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(assetType)?.Name ?? assetType.Name;
            var buttons = DialogButtons.Create(
            [
                Tr._p("Button", "Create"),
                Tr._p("Button", "Select a source..."),
                Tr._p("Button", "Cancel")
            ], 1, 3);
            var result = await Session.ServiceProvider.Get<IDialogService>().MessageBoxAsync(
                string.Format(Tr._p("Message", "Do you want to create this {0} without a source file?"), assetTypeName),
                buttons, MessageBoxImage.Question);

            // Close without clicking a button or Cancel
            if (result is 0 or 3)
                return false;

            // Create without source
            if (result == 1)
                break;

            // Display the file picker again
            files = await BrowseForSourceFilesAsync(description, true);
        }

        parameters.Tags.Add(SourceFilesPathKey, files);
        return await base.PrepareAssetCreation(parameters);
    }

    /// <inheritdoc/>
    protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
    {
        var sources = parameters.Tags.Get(SourceFilesPathKey);
        if (sources is null)
            return base.CreateAssets(parameters);

        var assets = new List<AssetItem>();
        var defaultName = parameters.Name;
        foreach (var source in sources)
        {
            // Use the source file name for the asset name if it is valid
            parameters.Name = source.GetFileNameWithoutExtension()!;
            if (string.IsNullOrWhiteSpace(parameters.Name))
                parameters.Name = defaultName;

            var assetImport = base.CreateAssets(parameters).First();

            // The importer for this file and asset type reads what it can from the file
            var importer = AssetRegistry.FindImporterForFile(source.ToOSPath()).OfType<IRawAssetImporter>()
                .FirstOrDefault(x => x.RootAssetTypes.Any(t => t.IsInstanceOfType(assetImport.Asset)));
            if (importer is not null)
            {
                assets.AddRange(importer.Import(source, assetImport.Location, assetImport.Asset));
            }
            else
            {
                ((IAssetWithSource)assetImport.Asset).Source = source;
                assets.Add(assetImport);
            }
        }
        return MakeUniqueNames(assets);
    }

    /// <summary>
    /// Asks the user for the source files of the template's asset type, remembering their folder for the next time.
    /// </summary>
    /// <returns>The files, or <c>null</c> when the user cancelled.</returns>
    protected virtual async Task<IEnumerable<UFile>?> BrowseForSourceFilesAsync(TemplateAssetDescription description, bool allowMultiSelection)
    {
        var extensions = description.GetSupportedExtensions();
        return await BrowseForSourceFilesAsync(extensions, allowMultiSelection);
    }

    /// <summary>
    /// Asks the user for source files of the given extensions, remembering their folder for the next time.
    /// </summary>
    /// <returns>The files, or <c>null</c> when the user cancelled.</returns>
    protected static async Task<IEnumerable<UFile>?> BrowseForSourceFilesAsync(FileExtensionCollection extensions, bool allowMultiSelection)
    {
        var result = await BrowseForFilesAsync(extensions, true, allowMultiSelection, InternalSettings.FileDialogLastImportDirectory.GetValue());
        if (result is null)
            return null;

        var list = result.ToList();
        InternalSettings.FileDialogLastImportDirectory.SetValue(list[0].GetFullDirectory());
        InternalSettings.SaveProfile();
        return list;
    }

    /// <summary>
    /// Renames the assets whose location another one of the list has.
    /// </summary>
    protected static List<AssetItem> MakeUniqueNames(IEnumerable<AssetItem> assets)
    {
        var result = new List<AssetItem>();
        foreach (var asset in assets)
        {
            var uniqueName = NamingHelper.ComputeNewName(asset.Location, x => result.Any(y => y.Asset != asset.Asset && x == y.Location), "{0}_{1}");
            result.Add(new AssetItem(uniqueName, asset.Asset));
        }
        return result;
    }
}
