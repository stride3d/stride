// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Quantum.Visitors;
using Stride.Core.Extensions;
using Stride.Core.Quantum;
using Stride.Core.Reflection;
using Stride.Core.Serialization;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// Ties a part of an asset (an entity) to its clone on the game side: the nodes of the asset-side part are linked to
/// the game-side ones, the content the part references (models, materials...) is loaded into the game, and every change
/// of the asset-side part (property grid, undo and redo) is replayed on the game-side clone.
/// </summary>
/// <remarks>
/// The cross-platform counterpart of the WPF <c>GameEditorChangePropagator</c>. The addition and the removal of parts
/// are not changes of a part: the editor adds and removes the game-side clones itself.
/// </remarks>
public abstract class GameEditorChangePropagator
{
    /// <summary>
    /// The key of the content of an asset-side node that holds the game-side node it is linked to.
    /// </summary>
    internal const string GameSideContentKey = "Game";

    private readonly GraphNodeLinker gameObjectLinker;
    private readonly GraphNodeChangeListener listener;
    private readonly BufferBlock<INodeChangeEventArgs?> propagatorQueue = new();
    private readonly TaskCompletionSource<int> gameSideReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<int> initialized = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile bool destroyed;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameEditorChangePropagator"/> class (UI thread). It listens to the
    /// changes of the part right away, but waits for <see cref="NotifyGameSidePartAdded"/> before replaying them.
    /// </summary>
    /// <param name="editor">The editor of the asset.</param>
    /// <param name="controller">The controller of the game of the editor.</param>
    /// <param name="asset">The asset the part belongs to.</param>
    /// <param name="ownerId">The identifier of the part, which references the content it loads.</param>
    /// <param name="assetSidePart">The asset-side part.</param>
    protected GameEditorChangePropagator(AssetEditorViewModel editor, IEditorGameController controller, AssetViewModel asset, AbsoluteId ownerId, IIdentifiable assetSidePart)
    {
        Editor = editor;
        Controller = controller;
        Asset = asset;
        OwnerId = ownerId;
        PropertyGraphDefinition = asset.PropertyGraph.Definition;

        // Listen to the changes right away, in case one occurs immediately after the part was created or added
        var rootNode = (IAssetNode)NodeContainer.GetOrCreateNode(assetSidePart);
        listener = new AssetGraphNodeChangeListener(rootNode, PropertyGraphDefinition);
        listener.Initialize();
        listener.ValueChanged += AssetSideNodeChanged;
        listener.ItemChanged += AssetSideNodeChanged;
        gameObjectLinker = new AssetGraphNodeLinker(PropertyGraphDefinition) { LinkAction = LinkNode };
        Task.Run(() => PropagatorTask(assetSidePart));
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
    /// Gets the editor of the asset.
    /// </summary>
    protected AssetEditorViewModel Editor { get; }

    /// <summary>
    /// Gets the controller of the game of the editor.
    /// </summary>
    protected IEditorGameController Controller { get; }

    /// <summary>
    /// Gets the asset the part belongs to.
    /// </summary>
    protected AssetViewModel Asset { get; }

    /// <summary>
    /// Gets the definition of the property graph of the asset.
    /// </summary>
    protected AssetPropertyGraphDefinition PropertyGraphDefinition { get; }

    private SessionNodeContainer NodeContainer => Editor.Session.AssetNodeContainer;

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

        listener.ValueChanged -= AssetSideNodeChanged;
        listener.ItemChanged -= AssetSideNodeChanged;
        listener.Dispose();
        gameSideReady.TrySetCanceled();
        // Resumes the propagator task if it waits for a change, so that it completes
        propagatorQueue.Post(null);
    }

    /// <summary>
    /// Updates the game side when a change is about a part reference (game thread is not required: the implementation
    /// posts to it).
    /// </summary>
    /// <param name="gameSideNode">The game-side node linked to the changed asset-side node.</param>
    /// <param name="value">The new value of the asset-side node.</param>
    /// <param name="e">The change.</param>
    /// <returns><c>true</c> when the change is about a part reference, handled or deliberately ignored.</returns>
    protected abstract Task<bool> PropagatePartReference(IGraphNode gameSideNode, object? value, INodeChangeEventArgs e);

    /// <summary>
    /// Clones an asset-side value for the game side (game thread).
    /// </summary>
    protected virtual object? CloneObjectForGameSide(object? assetSideObject, IAssetNode assetNode, IGraphNode gameSideNode)
    {
        // Null references, they will be handled by the editor asset loader
        return AssetCloner.Clone(assetSideObject, AssetClonerFlags.ReferenceAsNull);
    }

    /// <summary>
    /// Applies a change to a game-side node (game thread).
    /// </summary>
    protected static void UpdateGameSideContent(IGraphNode gameSideNode, object? value, ContentChangeType changeType, NodeIndex index)
    {
        switch (changeType)
        {
            case ContentChangeType.ValueChange:
                ((IMemberNode)gameSideNode).Update(value);
                break;
            case ContentChangeType.CollectionUpdate:
                ((IObjectNode)gameSideNode).Update(value, index);
                break;
            case ContentChangeType.CollectionAdd:
                ((IObjectNode)gameSideNode).Add(value, index);
                break;
            case ContentChangeType.CollectionRemove:
                var item = gameSideNode.Retrieve(index);
                ((IObjectNode)gameSideNode).Remove(item, index);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(changeType), changeType, null);
        }
    }

    private void AssetSideNodeChanged(object? sender, INodeChangeEventArgs e)
    {
        propagatorQueue.Post(e);
    }

    private async Task PropagatorTask(IIdentifiable assetSidePart)
    {
        try
        {
            // Ensure we are ready game-side before doing any operation
            await gameSideReady.Task;
            await Initialize(assetSidePart);
            initialized.SetResult(0);
        }
        catch (OperationCanceledException) when (destroyed)
        {
            initialized.TrySetCanceled();
            return;
        }
        catch (Exception e)
        {
            if (!destroyed)
                Controller.Logger.Error($"Unable to link the part {OwnerId} to the game side", e);
            initialized.TrySetException(e);
            return;
        }

        var contentReferenceCollector = new ContentReferenceCollector(PropertyGraphDefinition);
        while (!destroyed)
        {
            var e = await propagatorQueue.ReceiveAsync();
            // Destroy posts null, so that the loop evaluates destroyed again and exits
            if (e == null)
                continue;

            try
            {
                await Propagate(e, contentReferenceCollector);
            }
            catch (OperationCanceledException) when (destroyed)
            {
                // The game went away with the editor
            }
            catch (Exception ex)
            {
                if (!destroyed)
                    Controller.Logger.Error($"Unable to propagate a change of the part {OwnerId} to the game side", ex);
            }
        }

        try
        {
            await Controller.InvokeTask(() =>
            {
                Controller.Logger.Debug($"Unregistering reference to: {OwnerId}");
                return Controller.Loader.Manager.RemoveReferencer(OwnerId);
            });
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
        {
            // The game is gone, with its content
        }
    }

    private async Task Initialize(IIdentifiable assetSideInstance)
    {
        var partId = new AbsoluteId(OwnerId.AssetId, assetSideInstance.Id);
        var gameSideInstance = await Controller.InvokeAsync(() => Controller.FindGameSidePart(partId))
                               ?? throw new InvalidOperationException("Unable to reach the game-side instance");

        using (await DispatcherLock.Lock(true, Controller, Editor.Dispatcher))
        {
            Controller.Logger.Debug($"Initial linking for part {assetSideInstance.Id}");
            var assetNode = (IAssetNode?)NodeContainer.GetNode(assetSideInstance)
                            ?? throw new InvalidOperationException("Unable to reach the asset-side node of the part");
            var gameNode = Controller.GameSideNodeContainer.GetOrCreateNode(gameSideInstance);
            gameObjectLinker.LinkGraph(assetNode, gameNode);
        }

        var contentReferenceCollector = new ContentReferenceCollector(PropertyGraphDefinition);
        await Controller.InvokeTask(() =>
        {
            Controller.Logger.Debug($"Registering reference to: {OwnerId}");
            return Controller.Loader.Manager.RegisterReferencer(OwnerId);
        });
        var rootNode = NodeContainer.GetOrCreateNode(assetSideInstance);
        contentReferenceCollector.Visit(rootNode);
        await PushContentReferences(contentReferenceCollector);
    }

    private async Task Propagate(INodeChangeEventArgs e, ContentReferenceCollector contentReferenceCollector)
    {
        var assetNode = (IAssetNode)e.Node;
        var gameSideNode = assetNode.GetContent(GameSideContentKey)
                           ?? throw new InvalidOperationException("Unable to retrieve the game-side node");

        var index = (e as ItemChangeEventArgs)?.Index ?? NodeIndex.Empty;
        var innerType = e.Node.Descriptor.GetInnerCollectionType();
        if (!AssetRegistry.CanBeAssignedToContentTypes(innerType, checkIsUrlType: false))
        {
            if (NodeContainer.NodeBuilder.PrimitiveTypeFilter.IsPrimitiveType(innerType))
            {
                // No need to retrieve and/or duplicate the value for value type, we just propagate the change
                await Controller.InvokeAsync(() => UpdateGameSideContent(gameSideNode, e.NewValue, e.ChangeType, index));
            }
            else
            {
                var value = RetrieveValue(assetNode, e.NewValue, e.ChangeType, index);
                if (value == null && assetNode.Descriptor.Category == DescriptorCategory.Set)
                {
                    value = e.NewValue;
                }

                var isReference = await PropagatePartReference(gameSideNode, value, e);
                if (!isReference)
                {
                    // If necessary, unregister old content references that might be referenced by children nodes
                    if (e.OldValue != null)
                    {
                        var removedNodes = new HashSet<IGraphNode>();
                        if (NodeContainer.GetNode(e.OldValue) is { } oldNode)
                        {
                            var visitor = new AssetGraphVisitorBase(PropertyGraphDefinition);
                            // The root node can be an object node with a non-empty index: its game-side node might already be collected
                            visitor.Visiting += (node, _) =>
                            {
                                if (((IAssetNode)node).GetContent(GameSideContentKey) is { } removedNode)
                                    removedNodes.Add(removedNode);
                            };
                            visitor.Visit(oldNode);
                        }

                        await Controller.InvokeTask(() => Controller.Loader.Manager.ClearContentReferencesFromNodes(OwnerId, removedNodes));
                    }

                    await Controller.InvokeAsync(() =>
                    {
                        var gameSideValue = CloneObjectForGameSide(value, assetNode, gameSideNode);
                        UpdateGameSideContent(gameSideNode, gameSideValue, e.ChangeType, index);
                    });
                }
            }
        }
        else
        {
            await UpdateGameSideReference(gameSideNode, e.ChangeType, e.OldValue, e.NewValue, index);
        }

        // Critical section, both the asset thread and the game thread must be locked
        using (await DispatcherLock.Lock(true, Controller, Editor.Dispatcher))
        {
            // Link the nodes of the asset-side objects with the game-side objects
            gameObjectLinker.LinkGraph(assetNode, gameSideNode);
            // Also collect the content references (this requires only the lock on the asset thread)
            contentReferenceCollector.Reset();
            // A removed content reference was cleared by UpdateGameSideReference: there's nothing to collect
            if (e.ChangeType != ContentChangeType.CollectionRemove)
            {
                contentReferenceCollector.Visit(assetNode, index);
            }
        }

        // Update the content reference list on the game side now that the change is pushed
        await PushContentReferences(contentReferenceCollector);
        await Controller.InvokeAsync(Controller.TriggerActiveRenderStageReevaluation);
    }

    private Task PushContentReferences(ContentReferenceCollector contentReferenceCollector)
    {
        return Controller.InvokeTask(async () =>
        {
            using (await Controller.Loader.LockDatabaseAsynchronously())
            {
                foreach (var contentReference in contentReferenceCollector.ContentReferences)
                {
                    // Ok to await inside the loop, this call should never yield because we already own the lock.
                    await Controller.Loader.Manager.PushContentReference(OwnerId, contentReference.ContentId, contentReference.Node, contentReference.Index);
                }
            }
        });
    }

    private static object? RetrieveValue(IGraphNode node, object? value, ContentChangeType changeType, NodeIndex index)
    {
        return changeType switch
        {
            ContentChangeType.ValueChange => node.Retrieve(),
            ContentChangeType.CollectionUpdate => node.Retrieve(index),
            ContentChangeType.CollectionAdd or ContentChangeType.CollectionRemove => value,
            _ => throw new ArgumentOutOfRangeException(nameof(changeType), changeType, null),
        };
    }

    private async Task UpdateGameSideReference(IGraphNode gameSideNode, ContentChangeType changeType, object? oldValue, object? newValue, NodeIndex index)
    {
        if (!AssetRegistry.CanBeAssignedToContentTypes(gameSideNode.Descriptor.GetInnerCollectionType(), checkIsUrlType: false))
            return;

        // Grab the old referenced object if it's not null
        var reference = oldValue != null ? AttachedReferenceManager.GetAttachedReference(oldValue) : null;

        // Switch to the game thread to actually update the objects
        await Controller.InvokeTask(() =>
        {
            // For references, push null instead of the real value, the editor asset loader will set the actual value later
            switch (changeType)
            {
                case ContentChangeType.ValueChange:
                    ((IMemberNode)gameSideNode).Update(null);
                    break;
                case ContentChangeType.CollectionUpdate:
                    ((IObjectNode)gameSideNode).Update(null, index);
                    break;
                case ContentChangeType.CollectionAdd:
                    ((IObjectNode)gameSideNode).Add(null, index);
                    break;
                case ContentChangeType.CollectionRemove:
                    var oldValueGameSide = gameSideNode.Retrieve(index);
                    ((IObjectNode)gameSideNode).Remove(oldValueGameSide, index);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(changeType), changeType, null);
            }

            if (oldValue == newValue)
                return Task.CompletedTask;

            // Unregister the previous value
            return reference != null ? Controller.Loader.Manager.ClearContentReference(OwnerId, reference.Id, gameSideNode, index) : Task.CompletedTask;
        });
    }

    private static void LinkNode(IGraphNode assetNode, IGraphNode? gameNode)
    {
        var node = (IAssetNode)assetNode;
        node.SetContent(GameSideContentKey, gameNode!);
    }
}
