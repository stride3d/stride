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
public sealed class AssetDependenciesTool
{
    [McpServerTool(Name = "get_asset_dependencies"), Description("Returns the dependency graph for an asset: what assets reference it (inbound/referencedBy) and what assets it references (outbound/references). Also reports broken references. Critical for understanding impact before deleting or modifying an asset.")]
    public static async Task<string> GetAssetDependencies(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID (GUID from query_assets)")] string assetId,
        [Description("Direction to search: 'in' (who references me), 'out' (what I reference), or 'both' (default)")] string direction = "both",
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(assetId, out var id))
            {
                return new { error = "Invalid asset ID format. Expected a GUID.", dependencies = (object?)null };
            }

            var assetVm = session.GetAssetById(id);
            if (assetVm == null)
            {
                return new { error = $"Asset not found: {assetId}", dependencies = (object?)null };
            }

            var searchOptions = direction.ToLowerInvariant() switch
            {
                "in" => AssetDependencySearchOptions.In,
                "out" => AssetDependencySearchOptions.Out,
                _ => AssetDependencySearchOptions.InOut,
            };

            var deps = session.DependencyManager.ComputeDependencies(id, searchOptions, ContentLinkType.Reference);
            if (deps == null)
            {
                return new { error = $"Could not compute dependencies for asset: {assetId}", dependencies = (object?)null };
            }

            var referencedBy = deps.LinksIn.Select(link =>
            {
                var refVm = session.GetAssetById(link.Item.Id);
                return new
                {
                    id = link.Item.Id.ToString(),
                    name = refVm?.Name ?? link.Item.Location.ToString(),
                    type = refVm?.Asset.GetType().Name ?? "Unknown",
                    url = link.Item.Location.ToString(),
                };
            }).ToList();

            var references = deps.LinksOut.Select(link =>
            {
                var refVm = session.GetAssetById(link.Item.Id);
                return new
                {
                    id = link.Item.Id.ToString(),
                    name = refVm?.Name ?? link.Item.Location.ToString(),
                    type = refVm?.Asset.GetType().Name ?? "Unknown",
                    url = link.Item.Location.ToString(),
                };
            }).ToList();

            var brokenRefs = deps.BrokenLinksOut.Select(link => new
            {
                id = link.Element.Id.ToString(),
                url = link.Element.Location?.ToString() ?? "unknown",
            }).ToList();

            return new
            {
                error = (string?)null,
                dependencies = (object)new
                {
                    assetId = assetVm.Id.ToString(),
                    assetName = assetVm.Name,
                    assetType = assetVm.Asset.GetType().Name,
                    referencedBy,
                    references,
                    brokenReferences = brokenRefs,
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
