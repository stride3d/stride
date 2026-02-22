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

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class AssetDetailsTool
{
    [McpServerTool(Name = "get_asset_details"), Description("Returns detailed information about a specific asset, including all its serializable properties (via [DataMember] reflection), source file, archetype, tags, directory, and dirty state. Use query_assets to find asset IDs first.")]
    public static async Task<string> GetAssetDetails(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID (GUID from query_assets)")] string assetId,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(assetId, out var id))
            {
                return new { error = "Invalid asset ID format. Expected a GUID.", asset = (object?)null };
            }

            var assetVm = session.GetAssetById(id);
            if (assetVm == null)
            {
                return new { error = $"Asset not found: {assetId}", asset = (object?)null };
            }

            var asset = assetVm.Asset;
            var properties = JsonTypeConverter.SerializeDataMembers(asset);

            string? sourceFile = null;
            if (asset is AssetWithSource aws && aws.Source != null)
            {
                sourceFile = aws.Source.ToString();
            }

            string? archetypeId = null;
            string? archetypeUrl = null;
            if (asset.Archetype != null)
            {
                archetypeId = asset.Archetype.Id.ToString();
                archetypeUrl = asset.Archetype.Location?.ToString();
            }

            return new
            {
                error = (string?)null,
                asset = (object)new
                {
                    id = assetVm.Id.ToString(),
                    name = assetVm.Name,
                    type = asset.GetType().Name,
                    url = assetVm.Url,
                    directory = assetVm.Directory?.Path ?? "",
                    package = assetVm.Directory?.Package?.Name ?? "",
                    isDirty = assetVm.IsDirty,
                    sourceFile,
                    archetype = archetypeId != null ? new { id = archetypeId, url = archetypeUrl } : null,
                    tags = assetVm.Tags.ToArray(),
                    properties,
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
