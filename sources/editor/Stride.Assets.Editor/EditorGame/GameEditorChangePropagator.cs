// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Core;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Quantum;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// Ties a part of an asset (an entity) to its clone on the game side: the nodes of the asset-side part are linked to
/// the game-side ones, and the content the part references (models, materials...) is loaded into the game.
/// </summary>
/// <remarks>
/// The cross-platform counterpart of the WPF <c>GameEditorChangePropagator</c>, without the propagation yet: the changes
/// of the asset-side part don't reach the game. The propagation goes into this class, on top of the linking it does.
/// </remarks>
public sealed class GameEditorChangePropagator
{
    /// <summary>
    /// The key of the content of an asset-side node that holds the game-side node it is linked to.
    /// </summary>
    internal const string GameSideContentKey = "Game";

    private readonly AssetEditorViewModel editor;
    private readonly IEditorGameController controller;
    private readonly AssetPropertyGraphDefinition propertyGraphDefinition;
    private readonly GraphNodeLinker gameObjectLinker;
    private readonly TaskCompletionSource<int> gameSideReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<int> initialized = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile bool destroyed;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameEditorChangePropagator"/> class (UI thread). It waits for
    /// <see cref="NotifyGameSidePartAdded"/> before doing anything.
    /// </summary>
    /// <param name="editor">The editor of the asset.</param>
    /// <param name="controller">The controller of the game of the editor.</param>
    /// <param name="asset">The asset the part belongs to.</param>
    /// <param name="ownerId">The identifier of the part, which references the content it loads.</param>
    /// <param name="assetSidePart">The asset-side part.</param>
    public GameEditorChangePropagator(AssetEditorViewModel editor, IEditorGameController controller, AssetViewModel asset, AbsoluteId ownerId, IIdentifiable assetSidePart)
    {
        this.editor = editor;
        this.controller = controller;
        OwnerId = ownerId;
        propertyGraphDefinition = asset.PropertyGraph.Definition;
        gameObjectLinker = new AssetGraphNodeLinker(propertyGraphDefinition) { LinkAction = LinkNode };
        Task.Run(() => Initialize(assetSidePart));
    }

    /// <summary>
    /// Gets the identifier of the part.
    /// </summary>
    public AbsoluteId OwnerId { get; }

    /// <summary>
    /// Gets a task that completes once the part is linked to the game side and its content references are pushed.
    /// </summary>
    public Task Initialized => initialized.Task;

    /// <summary>
    /// Notifies that the game-side clone of the part is in the game: the linking can start.
    /// </summary>
    public void NotifyGameSidePartAdded()
    {
        gameSideReady.TrySetResult(0);
    }

    /// <summary>
    /// Stops this propagator, and releases the content the part referenced (UI thread).
    /// </summary>
    public void Destroy()
    {
        if (destroyed)
            return;
        destroyed = true;
        gameSideReady.TrySetCanceled();
        if (Initialized.IsCompletedSuccessfully)
        {
            try
            {
                // Posted before the exit of a game that is being destroyed
                controller.InvokeTask(() => controller.Loader.Manager.RemoveReferencer(OwnerId));
            }
            catch (ObjectDisposedException)
            {
                // The game is gone, with its content
            }
        }
    }

    private async Task Initialize(IIdentifiable assetSideInstance)
    {
        try
        {
            // Ensure we are ready game-side before doing any operation
            await gameSideReady.Task;

            var partId = new AbsoluteId(OwnerId.AssetId, assetSideInstance.Id);
            var gameSideInstance = await controller.InvokeAsync(() => controller.FindGameSidePart(partId))
                                   ?? throw new InvalidOperationException("Unable to reach the game-side instance");

            var assetNodeContainer = editor.Session.AssetNodeContainer;
            using (await DispatcherLock.Lock(true, controller, editor.Dispatcher))
            {
                controller.Logger.Debug($"Initial linking for part {assetSideInstance.Id}");
                var assetNode = (IAssetNode?)assetNodeContainer.GetNode(assetSideInstance)
                                ?? throw new InvalidOperationException("Unable to reach the asset-side node of the part");
                var gameNode = controller.GameSideNodeContainer.GetOrCreateNode(gameSideInstance);
                gameObjectLinker.LinkGraph(assetNode, gameNode);
            }

            var contentReferenceCollector = new ContentReferenceCollector(propertyGraphDefinition);
            await controller.InvokeTask(() =>
            {
                controller.Logger.Debug($"Registering reference to: {OwnerId}");
                return controller.Loader.Manager.RegisterReferencer(OwnerId);
            });
            var rootNode = assetNodeContainer.GetOrCreateNode(assetSideInstance);
            contentReferenceCollector.Visit(rootNode);

            await controller.InvokeTask(async () =>
            {
                using (await controller.Loader.LockDatabaseAsynchronously())
                {
                    foreach (var contentReference in contentReferenceCollector.ContentReferences)
                    {
                        // Ok to await inside the loop, this call should never yield because we already own the lock.
                        await controller.Loader.Manager.PushContentReference(OwnerId, contentReference.ContentId, contentReference.Node, contentReference.Index);
                    }
                }
            });

            initialized.SetResult(0);
        }
        catch (OperationCanceledException) when (destroyed)
        {
            initialized.TrySetCanceled();
        }
        catch (Exception e)
        {
            if (!destroyed)
                controller.Logger.Error($"Unable to link the part {OwnerId} to the game side", e);
            initialized.TrySetException(e);
        }
    }

    private static void LinkNode(IGraphNode assetNode, IGraphNode? gameNode)
    {
        var node = (IAssetNode)assetNode;
        node.SetContent(GameSideContentKey, gameNode!);
    }
}
