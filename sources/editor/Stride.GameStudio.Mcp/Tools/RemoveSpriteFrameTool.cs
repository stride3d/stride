// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Assets.Sprite;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class RemoveSpriteFrameTool
{
    [McpServerTool(Name = "remove_sprite_frame"), Description("Removes a sprite frame (SpriteInfo) from a SpriteSheetAsset by index. This operation supports undo/redo.")]
    public static async Task<string> RemoveSpriteFrame(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the SpriteSheetAsset")] string assetId,
        [Description("0-based index of the sprite frame to remove")] int index,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(assetId, out var id))
            {
                return new { error = "Invalid asset ID format. Expected a GUID.", removed = (object?)null };
            }

            var assetVm = session.GetAssetById(id);
            if (assetVm == null)
            {
                return new { error = $"Asset not found: {assetId}", removed = (object?)null };
            }

            if (assetVm.Asset is not SpriteSheetAsset spriteSheet)
            {
                return new { error = $"Asset is not a SpriteSheetAsset: {assetVm.Name} ({assetVm.AssetType.Name})", removed = (object?)null };
            }

            if (index < 0 || index >= spriteSheet.Sprites.Count)
            {
                return new { error = $"Index {index} is out of range. Sprites collection has {spriteSheet.Sprites.Count} items (valid range: 0-{spriteSheet.Sprites.Count - 1}).", removed = (object?)null };
            }

            var spriteInfo = spriteSheet.Sprites[index];
            var frameName = spriteInfo.Name;

            // Get the property graph node for the Sprites collection
            var rootNode = assetVm.PropertyGraph?.RootNode;
            if (rootNode == null)
            {
                return new { error = "Cannot access property graph for this asset.", removed = (object?)null };
            }

            var spritesMember = rootNode.TryGetChild(nameof(SpriteSheetAsset.Sprites));
            if (spritesMember?.Target == null)
            {
                return new { error = "Cannot access Sprites collection in property graph.", removed = (object?)null };
            }

            var spritesNode = spritesMember.Target;

            // Remove within undo/redo transaction
            var undoRedoService = session.ServiceProvider.Get<IUndoRedoService>();
            using (var transaction = undoRedoService.CreateTransaction())
            {
                spritesNode.Remove(spriteInfo, new NodeIndex(index));
                undoRedoService.SetName(transaction, $"Remove sprite frame '{frameName}'");
            }

            return new
            {
                error = (string?)null,
                removed = (object)new
                {
                    index,
                    name = frameName,
                    remainingCount = spriteSheet.Sprites.Count,
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
