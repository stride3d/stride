// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class CreateAssetTool
{
    [McpServerTool(Name = "create_asset"), Description("Creates a new asset, optionally importing from a source file. Without 'source': creates a blank asset of the given type. With 'source': imports the file (FBX, GLTF, OBJ, PNG, JPG, WAV, etc.) using the appropriate importer, which may create multiple assets (e.g. a model + materials + textures). When importing, 'assetType' is optional — the importer auto-detects it. Use query_assets to verify created assets.")]
    public static async Task<string> CreateAsset(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset type name (e.g. 'MaterialAsset', 'ModelAsset'). Required when creating blank assets. Optional when importing from source — the importer auto-detects the type.")] string? assetType = null,
        [Description("The name for the new asset. When importing, defaults to the source filename.")] string? name = null,
        [Description("Directory path within the package (e.g. 'Models/Characters'). Created if it doesn't exist.")] string? directory = null,
        [Description("Absolute path to a source file to import (e.g. 'C:/Models/character.fbx', 'C:/Textures/wall.png'). The file must exist on disk. Supported formats: FBX, GLTF, GLB, OBJ, DAE, 3DS, BLEND, PNG, JPG, TGA, DDS, BMP, PSD, TIF, WAV, MP3, OGG, MP4, and more.")] string? source = null,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            // Import from source file
            if (!string.IsNullOrEmpty(source))
            {
                return ImportFromSource(session, source, name, directory, assetType);
            }

            // Create blank asset
            if (string.IsNullOrEmpty(assetType))
            {
                return new { error = "Either 'assetType' or 'source' must be provided.", assets = (object?)null, asset = (object?)null };
            }

            if (string.IsNullOrEmpty(name))
            {
                return new { error = "'name' is required when creating a blank asset.", assets = (object?)null, asset = (object?)null };
            }

            return CreateBlankAsset(session, assetType, name, directory);
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    private static object ImportFromSource(SessionViewModel session, string sourcePath, string? name, string? directory, string? assetTypeFilter)
    {
        if (!File.Exists(sourcePath))
        {
            return new { error = $"Source file not found: {sourcePath}", assets = (object?)null, asset = (object?)null };
        }

        var importers = AssetRegistry.FindImporterForFile(sourcePath).ToList();
        if (importers.Count == 0)
        {
            return new { error = $"No importer found for file: {sourcePath}. Supported formats include: FBX, GLTF, GLB, OBJ, DAE, 3DS, PNG, JPG, TGA, DDS, WAV, MP3, OGG.", assets = (object?)null, asset = (object?)null };
        }

        var importer = importers.First();

        // Set up import parameters
        var importParameters = importer.GetDefaultParameters(false);
        importParameters.Logger = new LoggerResult();

        // If a type filter is specified, only enable that type
        if (!string.IsNullOrEmpty(assetTypeFilter))
        {
            var filterType = ResolveAssetType(assetTypeFilter);
            if (filterType != null)
            {
                foreach (var key in importParameters.SelectedOutputTypes.Keys.ToList())
                {
                    importParameters.SelectedOutputTypes[key] = key == filterType;
                }
            }
        }

        // Run the importer
        var sourceFile = new UFile(sourcePath);
        List<AssetItem> importedItems;
        try
        {
            importedItems = importer.Import(sourceFile, importParameters).ToList();
        }
        catch (Exception ex)
        {
            return new { error = $"Import failed: {ex.Message}", assets = (object?)null, asset = (object?)null };
        }

        if (importedItems.Count == 0)
        {
            return new { error = "Import produced no assets. The file may be empty or unsupported.", assets = (object?)null, asset = (object?)null };
        }

        // Find the target package
        var package = session.LocalPackages.FirstOrDefault(p => p.IsEditable);
        if (package == null)
        {
            return new { error = "No editable package found in the session.", assets = (object?)null, asset = (object?)null };
        }

        var dirPath = directory ?? "";
        var targetDir = string.IsNullOrEmpty(dirPath) ? "" : dirPath;

        // Add all imported assets to the package
        var undoRedoService = session.ServiceProvider.Get<IUndoRedoService>();
        var createdAssets = new List<object>();

        using (var transaction = undoRedoService.CreateTransaction())
        {
            var loggerResult = new LoggerResult();

            foreach (var item in importedItems)
            {
                // Compute the asset location
                var assetName = item.Location.GetFileNameWithoutExtension();

                // Override name for the first/primary asset if user specified one
                if (name != null && item == importedItems[0])
                {
                    assetName = name;
                }

                var assetUrl = string.IsNullOrEmpty(targetDir) ? assetName : $"{targetDir}/{assetName}";

                // Ensure unique name by appending suffix if collision
                var baseUrl = assetUrl;
                int suffix = 1;
                while (package.Package.Assets.Find(assetUrl) != null)
                {
                    assetUrl = $"{baseUrl}_{suffix++}";
                }

                var newItem = new AssetItem(assetUrl, item.Asset);
                AssetCollectionItemIdHelper.GenerateMissingItemIds(newItem.Asset);

                var directoryVm = package.GetOrCreateAssetDirectory(targetDir, canUndoRedoCreation: true);
                var assetVm = package.CreateAsset(directoryVm, newItem, true, loggerResult);

                if (assetVm != null)
                {
                    createdAssets.Add(new
                    {
                        id = assetVm.Id.ToString(),
                        name = assetVm.Name,
                        type = item.Asset.GetType().Name,
                        url = assetVm.Url,
                    });
                }
            }

            undoRedoService.SetName(transaction, $"Import from '{Path.GetFileName(sourcePath)}'");
        }

        if (createdAssets.Count == 0)
        {
            return new { error = "Import completed but no assets were added to the project.", assets = (object?)null, asset = (object?)null };
        }

        return new
        {
            error = (string?)null,
            assets = (object)createdAssets,
            // Also return 'asset' pointing to the first one for backwards compatibility
            asset = (object?)createdAssets[0],
        };
    }

    private static object CreateBlankAsset(SessionViewModel session, string assetType, string name, string? directory)
    {
        // Resolve asset type
        var resolvedType = ResolveAssetType(assetType);
        if (resolvedType == null)
        {
            var availableTypes = AssetRegistry.GetPublicTypes()
                .Select(t => t.Name)
                .OrderBy(n => n)
                .ToArray();
            return new
            {
                error = $"Asset type not found: '{assetType}'. Available types: {string.Join(", ", availableTypes.Take(20))}",
                assets = (object?)null,
                asset = (object?)null,
            };
        }

        // Find a factory for this type
        Asset? newAsset = null;
        foreach (var factory in AssetRegistry.GetAllAssetFactories())
        {
            if (factory.AssetType == resolvedType)
            {
                newAsset = factory.New();
                break;
            }
        }

        // Fallback to Activator if no factory found
        if (newAsset == null)
        {
            try
            {
                newAsset = (Asset)Activator.CreateInstance(resolvedType)!;
            }
            catch (Exception ex)
            {
                return new
                {
                    error = $"Cannot create instance of '{resolvedType.Name}': {ex.Message}",
                    assets = (object?)null,
                    asset = (object?)null,
                };
            }
        }

        // Find the target package
        var package = session.LocalPackages.FirstOrDefault(p => p.IsEditable);
        if (package == null)
        {
            return new { error = "No editable package found in the session.", assets = (object?)null, asset = (object?)null };
        }

        // Set up asset item
        AssetCollectionItemIdHelper.GenerateMissingItemIds(newAsset);
        var assetUrl = string.IsNullOrEmpty(directory) ? name : $"{directory}/{name}";
        var assetItem = new AssetItem(assetUrl, newAsset);

        // Create directory if needed
        var dirPath = directory ?? "";
        var directoryVm = package.GetOrCreateAssetDirectory(dirPath, canUndoRedoCreation: true);

        // Create with undo/redo
        var undoRedoService = session.ServiceProvider.Get<IUndoRedoService>();
        using (var transaction = undoRedoService.CreateTransaction())
        {
            var loggerResult = new LoggerResult();
            var assetVm = package.CreateAsset(directoryVm, assetItem, true, loggerResult);

            if (assetVm == null)
            {
                return new
                {
                    error = $"Failed to create asset. Log: {string.Join("; ", loggerResult.Messages.Select(m => m.Text))}",
                    assets = (object?)null,
                    asset = (object?)null,
                };
            }

            undoRedoService.SetName(transaction, $"Create {resolvedType.Name} '{name}'");

            var assetInfo = new
            {
                id = assetVm.Id.ToString(),
                name = assetVm.Name,
                type = resolvedType.Name,
                url = assetVm.Url,
            };

            return new
            {
                error = (string?)null,
                assets = (object?)null,
                asset = (object?)assetInfo,
            };
        }
    }

    private static Type? ResolveAssetType(string typeName)
    {
        var publicTypes = AssetRegistry.GetPublicTypes();

        // Try exact name match
        var match = publicTypes.FirstOrDefault(t => t.Name == typeName);
        if (match != null)
            return match;

        // Try full name match
        match = publicTypes.FirstOrDefault(t => t.FullName == typeName);
        if (match != null)
            return match;

        // Try case-insensitive match
        match = publicTypes.FirstOrDefault(t =>
            string.Equals(t.Name, typeName, StringComparison.OrdinalIgnoreCase));
        if (match != null)
            return match;

        // Try without "Asset" suffix
        if (!typeName.EndsWith("Asset", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveAssetType(typeName + "Asset");
        }

        return null;
    }
}
