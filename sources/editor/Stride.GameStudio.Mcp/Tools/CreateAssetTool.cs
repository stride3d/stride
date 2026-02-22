// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class CreateAssetTool
{
    [McpServerTool(Name = "create_asset"), Description("Creates a new asset of a given type with sensible defaults. Use query_assets to verify the asset was created. Supported types include: MaterialAsset, SceneAsset, PrefabAsset, TextureAsset, SkyboxAsset, EffectShaderAsset, RawAsset, and more. The asset type can be specified as a short name (e.g. 'MaterialAsset') or fully qualified.")]
    public static async Task<string> CreateAsset(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset type name (e.g. 'MaterialAsset', 'PrefabAsset', 'SceneAsset')")] string assetType,
        [Description("The name for the new asset")] string name,
        [Description("Directory path within the package (e.g. 'Materials/Environment'). Created if it doesn't exist.")] string? directory = null,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
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
                        asset = (object?)null,
                    };
                }
            }

            // Find the target package
            var package = session.LocalPackages.FirstOrDefault(p => p.IsEditable);
            if (package == null)
            {
                return new { error = "No editable package found in the session.", asset = (object?)null };
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
                        asset = (object?)null,
                    };
                }

                undoRedoService.SetName(transaction, $"Create {resolvedType.Name} '{name}'");

                return new
                {
                    error = (string?)null,
                    asset = (object)new
                    {
                        id = assetVm.Id.ToString(),
                        name = assetVm.Name,
                        type = resolvedType.Name,
                        url = assetVm.Url,
                    },
                };
            }
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
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
