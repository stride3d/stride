// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class ManageAssetTool
{
    [McpServerTool(Name = "manage_asset"), Description("Performs organizational operations on existing assets: rename, move, or delete. For delete, the tool checks for inbound references first and returns an error if the asset is still referenced (use get_asset_dependencies to check). All operations support undo/redo.")]
    public static async Task<string> ManageAsset(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID (GUID from query_assets)")] string assetId,
        [Description("The action to perform: 'rename', 'move', or 'delete'")] string action,
        [Description("For 'rename': the new name for the asset")] string? newName = null,
        [Description("For 'move': the target directory path (e.g. 'Materials/Environment')")] string? newDirectory = null,
        CancellationToken cancellationToken = default)
    {
        // Delete uses async UI operations, so handle it specially
        if (action.Equals("delete", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleDelete(session, dispatcher, assetId, cancellationToken);
        }

        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(assetId, out var id))
            {
                return new { error = "Invalid asset ID format. Expected a GUID.", result = (object?)null };
            }

            var assetVm = session.GetAssetById(id);
            if (assetVm == null)
            {
                return new { error = $"Asset not found: {assetId}", result = (object?)null };
            }

            switch (action.ToLowerInvariant())
            {
                case "rename":
                    return HandleRename(assetVm, newName);
                case "move":
                    return HandleMove(session, assetVm, newDirectory);
                default:
                    return new { error = $"Unknown action: '{action}'. Expected 'rename', 'move', or 'delete'.", result = (object?)null };
            }
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    private static object HandleRename(AssetViewModel assetVm, string? newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return new { error = "newName is required for 'rename' action.", result = (object?)null };
        }

        var oldName = assetVm.Name;
        assetVm.Name = newName;

        return new
        {
            error = (string?)null,
            result = (object)new
            {
                action = "renamed",
                oldName,
                newName = assetVm.Name,
                url = assetVm.Url,
            },
        };
    }

    private static object HandleMove(SessionViewModel session, AssetViewModel assetVm, string? newDirectory)
    {
        if (newDirectory == null)
        {
            return new { error = "newDirectory is required for 'move' action.", result = (object?)null };
        }

        var package = assetVm.Directory?.Package;
        if (package == null)
        {
            return new { error = "Cannot determine the asset's package.", result = (object?)null };
        }

        var oldUrl = assetVm.Url;
        var targetDir = package.GetOrCreateAssetDirectory(newDirectory, canUndoRedoCreation: true);
        assetVm.MoveAsset(package.Package, targetDir);

        return new
        {
            error = (string?)null,
            result = (object)new
            {
                action = "moved",
                oldUrl,
                newUrl = assetVm.Url,
                newDirectory,
            },
        };
    }

    private static async Task<string> HandleDelete(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        string assetId,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.InvokeTaskOnUIThread(async () =>
        {
            if (!AssetId.TryParse(assetId, out var id))
            {
                return new { error = "Invalid asset ID format. Expected a GUID.", result = (object?)null };
            }

            var assetVm = session.GetAssetById(id);
            if (assetVm == null)
            {
                return new { error = $"Asset not found: {assetId}", result = (object?)null };
            }

            // Check if asset can be deleted
            if (!assetVm.CanDelete(out var deleteError))
            {
                return new { error = $"Cannot delete asset: {deleteError}", result = (object?)null };
            }

            // Pre-check for inbound references to avoid the fix-references dialog
            var deps = session.DependencyManager.ComputeDependencies(id, AssetDependencySearchOptions.In, ContentLinkType.Reference);
            if (deps != null)
            {
                var referencers = deps.LinksIn
                    .Where(link => session.GetAssetById(link.Item.Id) != null)
                    .Select(link =>
                    {
                        var refVm = session.GetAssetById(link.Item.Id);
                        return new { id = link.Item.Id.ToString(), name = refVm?.Name, type = refVm?.Asset.GetType().Name };
                    })
                    .ToList();

                if (referencers.Count > 0)
                {
                    return new
                    {
                        error = $"Cannot delete: asset is referenced by {referencers.Count} other asset(s). Use get_asset_dependencies to see the full list, and remove references first.",
                        result = (object)new { referencedBy = referencers },
                    };
                }
            }

            var assetName = assetVm.Name;
            var assetType = assetVm.Asset.GetType().Name;

            var success = await session.DeleteItems(new object[] { assetVm }, skipConfirmation: true);

            if (!success)
            {
                return new { error = "Delete operation was cancelled or failed.", result = (object?)null };
            }

            return new
            {
                error = (string?)null,
                result = (object)new
                {
                    action = "deleted",
                    name = assetName,
                    type = assetType,
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
