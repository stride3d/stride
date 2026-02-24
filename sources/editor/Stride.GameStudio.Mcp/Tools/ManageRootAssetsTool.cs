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
using Stride.Core.Presentation.Services;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class ManageRootAssetsTool
{
    [McpServerTool(Name = "manage_root_assets"), Description("Manages which assets are included in the game build. Root assets (and their dependencies) are compiled when building. Use 'list' to see current root assets, 'add' to mark an asset for build inclusion, 'remove' to exclude it. Scenes must typically be added as root assets to be included in the built game.")]
    public static async Task<string> ManageRootAssets(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The action to perform: 'list', 'add', or 'remove'")] string action,
        [Description("The asset ID (GUID) — required for 'add' and 'remove' actions")] string? assetId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            switch (action.ToLowerInvariant())
            {
                case "list":
                    return HandleList(session);
                case "add":
                    return HandleAdd(session, assetId);
                case "remove":
                    return HandleRemove(session, assetId);
                default:
                    return new { error = $"Unknown action: '{action}'. Expected 'list', 'add', or 'remove'.", assets = (object?)null, result = (object?)null };
            }
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    private static object HandleList(SessionViewModel session)
    {
        var currentProject = session.CurrentProject;
        if (currentProject == null)
        {
            return new { error = "No active project.", assets = (object?)null, result = (object?)null };
        }

        var rootAssets = currentProject.RootAssets
            .Where(a => a != null)
            .Select(a => new
            {
                id = a.Id.ToString(),
                name = a.Name,
                type = a.AssetType?.Name ?? "Unknown",
                url = a.Url,
            })
            .ToList();

        return new
        {
            error = (string?)null,
            assets = (object?)rootAssets,
            result = (object?)null,
        };
    }

    private static object HandleAdd(SessionViewModel session, string? assetId)
    {
        if (string.IsNullOrWhiteSpace(assetId))
        {
            return new { error = "assetId is required for 'add' action.", assets = (object?)null, result = (object?)null };
        }

        if (!AssetId.TryParse(assetId, out var id))
        {
            return new { error = "Invalid asset ID format. Expected a GUID.", assets = (object?)null, result = (object?)null };
        }

        var currentProject = session.CurrentProject;
        if (currentProject == null)
        {
            return new { error = "No active project.", assets = (object?)null, result = (object?)null };
        }

        var assetVm = session.GetAssetById(id);
        if (assetVm == null)
        {
            return new { error = $"Asset not found: {assetId}", assets = (object?)null, result = (object?)null };
        }

        if (!currentProject.IsInScope(assetVm))
        {
            return new { error = $"Asset '{assetVm.Name}' is not in scope for the current project '{currentProject.Name}'.", assets = (object?)null, result = (object?)null };
        }

        if (currentProject.RootAssets.Contains(assetVm))
        {
            return new { error = $"Asset '{assetVm.Name}' is already a root asset.", assets = (object?)null, result = (object?)null };
        }

        var undoRedoService = session.ServiceProvider.Get<IUndoRedoService>();
        using (var transaction = undoRedoService.CreateTransaction())
        {
            assetVm.Dependencies.IsRoot = true;
            undoRedoService.SetName(transaction, "Add root asset");
        }

        return new
        {
            error = (string?)null,
            assets = (object?)null,
            result = (object)new
            {
                action = "added",
                id = assetVm.Id.ToString(),
                name = assetVm.Name,
                type = assetVm.AssetType.Name,
                url = assetVm.Url,
            },
        };
    }

    private static object HandleRemove(SessionViewModel session, string? assetId)
    {
        if (string.IsNullOrWhiteSpace(assetId))
        {
            return new { error = "assetId is required for 'remove' action.", assets = (object?)null, result = (object?)null };
        }

        if (!AssetId.TryParse(assetId, out var id))
        {
            return new { error = "Invalid asset ID format. Expected a GUID.", assets = (object?)null, result = (object?)null };
        }

        var currentProject = session.CurrentProject;
        if (currentProject == null)
        {
            return new { error = "No active project.", assets = (object?)null, result = (object?)null };
        }

        var assetVm = session.GetAssetById(id);
        if (assetVm == null)
        {
            return new { error = $"Asset not found: {assetId}", assets = (object?)null, result = (object?)null };
        }

        if (!currentProject.RootAssets.Contains(assetVm))
        {
            return new { error = $"Asset '{assetVm.Name}' is not a root asset.", assets = (object?)null, result = (object?)null };
        }

        var undoRedoService = session.ServiceProvider.Get<IUndoRedoService>();
        using (var transaction = undoRedoService.CreateTransaction())
        {
            assetVm.Dependencies.IsRoot = false;
            undoRedoService.SetName(transaction, "Remove root asset");
        }

        return new
        {
            error = (string?)null,
            assets = (object?)null,
            result = (object)new
            {
                action = "removed",
                id = assetVm.Id.ToString(),
                name = assetVm.Name,
                type = assetVm.AssetType.Name,
                url = assetVm.Url,
            },
        };
    }
}
