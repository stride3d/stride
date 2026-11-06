// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Serialization;
using Stride.Editor.Build;
using Stride.Editor.EditorGame.Game;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// Renders the game of an entity hierarchy editor with the graphics compositor of the game settings, as the game does
/// (its editor part), and follows the changes of the game settings and of the compositor asset.
/// </summary>
[EditorGameService(typeof(EntityHierarchyEditorController), Order = 100)]
public sealed class EditorGameGraphicsCompositorService : EditorGameServiceBase
{
    private readonly IEditorGameController controller;
    private readonly AssetEditorViewModel editor;
    private readonly GameSettingsProviderService settingsProvider;
    private EditorServiceGame? game;
    private GraphicsCompositorViewModel? currentGraphicsCompositorAsset;
    private GraphicsCompositor? loadedGraphicsCompositor;

    public EditorGameGraphicsCompositorService(IEditorGameController controller, AssetEditorViewModel editor)
    {
        this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
        this.editor = editor ?? throw new ArgumentNullException(nameof(editor));
        settingsProvider = editor.ServiceProvider.Get<GameSettingsProviderService>();
    }

    /// <inheritdoc />
    protected override Task<bool> Initialize(EditorServiceGame editorGame)
    {
        game = editorGame;

        editor.Dispatcher.InvokeAsync(async () =>
        {
            // Be notified of future changes to game settings
            settingsProvider.GameSettingsChanged += GameSettingsChanged;
            editor.Session.AssetPropertiesChanged += AssetPropertyChanged;
            await ReloadGraphicsCompositor(true);
        });

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public override ValueTask DisposeAsync()
    {
        settingsProvider.GameSettingsChanged -= GameSettingsChanged;
        editor.Session.AssetPropertiesChanged -= AssetPropertyChanged;

        return base.DisposeAsync();
    }

    private void GameSettingsChanged(object? sender, GameSettingsChangedEventArgs e)
    {
        ReloadGraphicsCompositor(false).Forget();
    }

    private void AssetPropertyChanged(object? sender, AssetChangedEventArgs e)
    {
        if (currentGraphicsCompositorAsset != null && e.Assets.Any(x => x.Asset == currentGraphicsCompositorAsset.Asset))
        {
            ReloadGraphicsCompositor(true).Forget();
        }
    }

    private async Task ReloadGraphicsCompositor(bool forceIfSame)
    {
        try
        {
            await LoadGraphicsCompositor(forceIfSame);
        }
        catch (Exception e)
        {
            // The game keeps rendering with the compositor it had
            controller.Logger.Error("Unable to load the graphics compositor of the game settings", e);
        }
    }

    private async Task LoadGraphicsCompositor(bool forceIfSame)
    {
        var graphicsCompositorId = AttachedReferenceManager.GetAttachedReference(settingsProvider.CurrentGameSettings.GraphicsCompositor)?.Id;
        var graphicsCompositorAsset = graphicsCompositorId.HasValue ? editor.Session.GetAssetById(graphicsCompositorId.Value) as GraphicsCompositorViewModel : null;

        // Same compositor as before?
        if (graphicsCompositorAsset == currentGraphicsCompositorAsset && !forceIfSame)
            return;

        currentGraphicsCompositorAsset = graphicsCompositorAsset;

        // Without a compositor in the game settings, the game keeps the default one
        if (graphicsCompositorAsset == null || game == null)
            return;

        var database = editor.ServiceProvider.Get<GameStudioDatabase>();
        await database.Build(graphicsCompositorAsset.AssetItem);

        await controller.InvokeTask(async () =>
        {
            using (await database.MountInCurrentMicroThread())
            {
                // Unload previous graphics compositor
                if (loadedGraphicsCompositor != null)
                {
                    game.Content.Unload(loadedGraphicsCompositor);
                    loadedGraphicsCompositor = null;
                }
                else
                {
                    // Should only happen when graphics compositor is fallback one (i.e. first load or failure)
                    game.SceneSystem.GraphicsCompositor?.Dispose();
                }

                game.SceneSystem.GraphicsCompositor = null;

                // Load and set new graphics compositor
                loadedGraphicsCompositor = game.Content.Load<GraphicsCompositor>(graphicsCompositorAsset.AssetItem.Location);
                game.UpdateGraphicsCompositor(loadedGraphicsCompositor);
            }
        });
    }
}
