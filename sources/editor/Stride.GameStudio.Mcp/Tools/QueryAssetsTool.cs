// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class QueryAssetsTool
{
    [McpServerTool(Name = "query_assets"), Description("Search and filter assets in the current project. Returns asset metadata including ID, name, type, and URL. Use filter for name substring matching, type for asset type filtering (e.g. 'SceneAsset', 'MaterialAsset', 'ModelAsset'), and folder for URL path prefix matching.")]
    public static async Task<string> QueryAssets(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("Optional name substring filter (case-insensitive)")] string? filter = null,
        [Description("Optional asset type name filter (e.g. 'SceneAsset', 'MaterialAsset')")] string? type = null,
        [Description("Optional URL path prefix filter")] string? folder = null,
        [Description("Maximum number of results to return (default 100)")] int maxResults = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            var query = session.AllAssets.AsEnumerable();

            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(a => a.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(a => a.AssetType.Name.Equals(type, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(folder))
            {
                query = query.Where(a => a.Url != null && a.Url.StartsWith(folder, StringComparison.OrdinalIgnoreCase));
            }

            var assets = query
                .Take(maxResults)
                .Select(a => new
                {
                    id = a.Id.ToString(),
                    name = a.Name,
                    type = a.AssetType.Name,
                    url = a.Url,
                })
                .ToList();

            return new
            {
                totalCount = session.AllAssets.Count(),
                returnedCount = assets.Count,
                assets,
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
