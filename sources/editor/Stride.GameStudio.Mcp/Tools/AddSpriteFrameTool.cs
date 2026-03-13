// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Assets.Sprite;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class AddSpriteFrameTool
{
    [McpServerTool(Name = "add_sprite_frame"), Description("Adds a new sprite frame (SpriteInfo) to a SpriteSheetAsset's Sprites collection. This operation supports undo/redo.")]
    public static async Task<string> AddSpriteFrame(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The asset ID of the SpriteSheetAsset")] string assetId,
        [Description("Name for the sprite frame")] string name,
        [Description("Optional source image file path")] string? sourceFile = null,
        [Description("Optional texture region X position")] int? textureRegionX = null,
        [Description("Optional texture region Y position")] int? textureRegionY = null,
        [Description("Optional texture region width")] int? textureRegionWidth = null,
        [Description("Optional texture region height")] int? textureRegionHeight = null,
        [Description("Optional pixels per unit (default 100)")] float? pixelsPerUnit = null,
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeOnUIThread(() =>
        {
            if (!AssetId.TryParse(assetId, out var id))
            {
                return new { error = "Invalid asset ID format. Expected a GUID.", frame = (object?)null };
            }

            var assetVm = session.GetAssetById(id);
            if (assetVm == null)
            {
                return new { error = $"Asset not found: {assetId}", frame = (object?)null };
            }

            if (assetVm.Asset is not SpriteSheetAsset spriteSheet)
            {
                return new { error = $"Asset is not a SpriteSheetAsset: {assetVm.Name} ({assetVm.AssetType.Name})", frame = (object?)null };
            }

            // Create new SpriteInfo
            var spriteInfo = new SpriteInfo { Name = name };

            if (!string.IsNullOrEmpty(sourceFile))
            {
                spriteInfo.Source = new UFile(sourceFile);
            }

            if (textureRegionX.HasValue || textureRegionY.HasValue || textureRegionWidth.HasValue || textureRegionHeight.HasValue)
            {
                spriteInfo.TextureRegion = new Rectangle(
                    textureRegionX ?? 0,
                    textureRegionY ?? 0,
                    textureRegionWidth ?? 0,
                    textureRegionHeight ?? 0);
            }

            if (pixelsPerUnit.HasValue)
            {
                spriteInfo.PixelsPerUnit = pixelsPerUnit.Value;
            }

            // Generate collection IDs
            AssetCollectionItemIdHelper.GenerateMissingItemIds(spriteInfo);

            // Get the property graph node for the Sprites collection
            var rootNode = assetVm.PropertyGraph?.RootNode;
            if (rootNode == null)
            {
                return new { error = "Cannot access property graph for this asset.", frame = (object?)null };
            }

            var spritesMember = rootNode.TryGetChild(nameof(SpriteSheetAsset.Sprites));
            if (spritesMember?.Target == null)
            {
                return new { error = "Cannot access Sprites collection in property graph.", frame = (object?)null };
            }

            var spritesNode = spritesMember.Target;

            // Add within undo/redo transaction
            var undoRedoService = session.ServiceProvider.Get<IUndoRedoService>();
            using (var transaction = undoRedoService.CreateTransaction())
            {
                spritesNode.Add(spriteInfo);
                undoRedoService.SetName(transaction, $"Add sprite frame '{name}'");
            }

            var newIndex = spriteSheet.Sprites.Count - 1;

            return new
            {
                error = (string?)null,
                frame = (object)new
                {
                    index = newIndex,
                    name,
                    totalFrames = spriteSheet.Sprites.Count,
                },
            };
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }
}
