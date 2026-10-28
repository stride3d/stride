// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Editor.Engine;
using Stride.Engine;

namespace Stride.Assets.Editor.ViewModels;

public abstract class EntityHierarchyEditorViewModel : AssetCompositeHierarchyEditorViewModel<EntityDesign, Entity, EntityHierarchyViewModel, EntityHierarchyItemViewModel>
{
    private static readonly Logger Log = GlobalLogger.GetLogger("EntityHierarchyEditor");

    private readonly List<GameEditorChangePropagator> propagators = [];
    private object? gameView;

    protected EntityHierarchyEditorViewModel(EntityHierarchyViewModel asset)
        : base(asset)
    {
    }

    public EntityHierarchyRootViewModel HierarchyRoot => (EntityHierarchyRootViewModel)RootPart;

    /// <summary>
    /// Gets the control that shows the game of the editor, or <c>null</c> while the game has not started, or when the
    /// editor has no game (the session has no game viewport, the game could not start).
    /// </summary>
    public object? GameView { get => gameView; private set => SetValue(ref gameView, value); }

    /// <summary>
    /// Gets the controller of the game of the editor, or <c>null</c> when the editor has no game.
    /// </summary>
    protected EntityHierarchyEditorController? Controller { get; private set; }

    /// <summary>
    /// Starts the game of the editor and loads the hierarchy into it, when the session can show a game.
    /// </summary>
    /// <returns><c>false</c> when the game could not start; the editor still works without it.</returns>
    public override async Task<bool> Initialize()
    {
        if (!await base.Initialize())
            return false;

        // Without a viewport (no graphics device, previews turned off) the editor has no game view
        if (ServiceProvider.TryGet<IEditorGameViewportService>() is not { } viewportService)
            return true;

        try
        {
            if (CreateController(viewportService) is not { } controller)
                return true;

            Controller = controller;
            if (!await controller.StartGame())
                return false;
            GameView = controller.View;

            // Wait for the game to be fully initialized (including services).
            await controller.GameContentLoaded;
            if (IsDestroyed)
                return false;

            if (!await controller.CreateScene())
                return false;

            await OnGameContentLoaded();
            return true;
        }
        catch (Exception e) when (!IsDestroyed)
        {
            Log.Error($"The game of the editor of {Asset.Url} could not start.", e);
            GameView = null;
            return false;
        }
        catch (Exception) when (IsDestroyed)
        {
            // The editor was closed while its game started
            return false;
        }
    }

    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(EntityHierarchyEditorViewModel));

        foreach (var propagator in propagators)
            propagator.Destroy();
        propagators.Clear();
        GameView = null;
        Controller?.Destroy();

        base.Destroy();
    }

    /// <summary>
    /// Lets the game render: its view is shown.
    /// </summary>
    public void ShowGame()
    {
        Controller?.OnShowGame();
    }

    /// <summary>
    /// Throttles the game: its view is hidden.
    /// </summary>
    public void HideGame()
    {
        Controller?.OnHideGame();
    }

    /// <summary>
    /// Creates the controller of the game of the editor (UI thread).
    /// </summary>
    /// <returns>The controller, or <c>null</c> when this editor has no game.</returns>
    protected virtual EntityHierarchyEditorController? CreateController(IEditorGameViewportService viewportService)
    {
        return null;
    }

    /// <summary>
    /// Called once the game has loaded its content and created its scene: the hierarchy can be loaded into it.
    /// </summary>
    protected virtual Task OnGameContentLoaded()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Links the given entities, whose game-side clones were just added to the game, to the game side, and loads the
    /// content they reference.
    /// </summary>
    /// <returns>A task that completes once every entity is linked.</returns>
    protected Task LinkToGameSide(IEnumerable<EntityViewModel> entities)
    {
        if (Controller is not { } controller)
            return Task.CompletedTask;

        var added = new List<GameEditorChangePropagator>();
        foreach (var entity in entities)
        {
            var propagator = new GameEditorChangePropagator(this, controller, entity.Asset, entity.Id, entity.AssetSideEntity);
            propagators.Add(propagator);
            added.Add(propagator);
            propagator.NotifyGameSidePartAdded();
        }
        return Task.WhenAll(added.Select(x => x.Initialized));
    }

    protected override async Task RefreshEditorProperties()
    {
        EditorProperties.UpdateTypeAndName(SelectedItems, x => "Entity", x => x.Name, "entities");
        await EditorProperties.GenerateSelectionPropertiesAsync(SelectedItems.OfType<EntityViewModel>());
    }

    protected override void SelectedContentCollectionChanged(NotifyCollectionChangedAction action)
    {
        SelectedItems.Clear();
        SelectedItems.AddRange(SelectedContent.Cast<EntityHierarchyItemViewModel>().SelectMany(x => x.InnerSubEntities));
    }
}
