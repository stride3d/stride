// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Stride.Core.Assets;
using Stride.Core.Assets.Quantum;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Serialization;
using Stride.Core.Quantum;
using Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Particles.Materials;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels
{
    public class GameEditorChangePropagator<TAssetPartDesign, TAssetPart, TItemViewModel>
        where TAssetPart : class, IIdentifiable
        where TItemViewModel : AssetCompositeItemViewModel, IEditorGamePartViewModel where TAssetPartDesign : class, IAssetPartDesign<TAssetPart>
    {
        private const string GameSideContentKey = "Game";
        private readonly GraphNodeLinker gameObjectLinker;
        private readonly GraphNodeChangeListener listener;
        private readonly BufferBlock<INodeChangeEventArgs> propagatorQueue = new BufferBlock<INodeChangeEventArgs>();
        private readonly TaskCompletionSource<int> gameSideReady = new TaskCompletionSource<int>();
        private readonly TaskCompletionSource<int> initialized = new TaskCompletionSource<int>();
        private bool running = true;

        public GameEditorChangePropagator([NotNull] AssetCompositeEditorViewModel editor, [NotNull] TItemViewModel owner, [NotNull] TAssetPart assetSidePart)
        {
            Editor = editor;
            Owner = owner;
            // We want to start listening to change right away, in case a change occurs immedately after creating/adding the entity
            var rootNode = (IAssetNode)Editor.NodeContainer.GetNode(assetSidePart);
            listener = new AssetGraphNodeChangeListener(rootNode, owner.Asset.PropertyGraph.Definition);
            listener.Initialize();
            listener.ValueChanged += AssetSideNodeChanged;
            listener.ItemChanged += AssetSideNodeChanged;
            gameObjectLinker = new AssetGraphNodeLinker(owner.Asset.PropertyGraph.Definition) { LinkAction = LinkNode };
            Task.Run(() => PropagatorTask(assetSidePart));
        }

        public Task Initialized => initialized.Task;

        [NotNull]
        protected AssetCompositeEditorViewModel Editor { get; }

        [NotNull]
        protected TItemViewModel Owner { get; }

        /// <inheritdoc/>
        public void Destroy()
        {
            listener.ValueChanged -= AssetSideNodeChanged;
            listener.ItemChanged -= AssetSideNodeChanged;

            running = false;
            // This will resume the propagator task if it's awaiting, and allow it to complete.
            propagatorQueue.Post(null);
        }

        public void NotifyGameSidePartAdded()
        {
            gameSideReady.SetResult(0);
        }

        private async Task Initialize([NotNull] TAssetPart assetSideInstance)
        {
            var partId = new AbsoluteId(Owner.Id.AssetId, assetSideInstance.Id);
            var gameSideInstance = await Editor.Controller.InvokeAsync(() => Editor.Controller.FindGameSidePart(partId));
            if (gameSideInstance == null)
                throw new InvalidOperationException("Unable to reach the game-side instance");

            using (await DispatcherLock.Lock(true, Editor.Controller, Editor.Dispatcher))
            {
                Editor.Controller.Logger.Debug($"Initial linking for part {assetSideInstance.Id}");
                var assetNode = (IAssetNode)Editor.NodeContainer.GetNode(assetSideInstance);
                var gameNode = Editor.Controller.GameSideNodeContainer.GetOrCreateNode(gameSideInstance);
                gameObjectLinker.LinkGraph(assetNode, gameNode);
            }
        }

        private static void LinkNode(IGraphNode assetNode, IGraphNode gameNode)
        {
            var node = (IAssetNode)assetNode;
            node.SetContent(GameSideContentKey, gameNode);
        }

        private void AssetSideNodeChanged(object sender, INodeChangeEventArgs e)
        {
            propagatorQueue.Post(e);
        }

        protected virtual async Task<bool> PropagatePartReference(IGraphNode gameSideNode, object value, INodeChangeEventArgs e)
        {
            // If the change is an addition or removal of a part in the asset, it shouldn't be handled by the propagator.
            var index = (e as ItemChangeEventArgs)?.Index ?? NodeIndex.Empty;
            if (((AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart>)Owner.Asset.PropertyGraph).IsChildPartReference(e.Node, index))
            {
                // But we should still return that the change correspond to a part reference.
                return true;
            }
            var assetPart = value as TAssetPart;
            if (assetPart != null)
            {
                var id = new AbsoluteId(Owner.Id.AssetId, assetPart.Id);
                await Editor.Controller.InvokeAsync(() =>
                {
                    var instance = Editor.Controller.FindGameSidePart(id);
                    UpdateGameSideContent(gameSideNode, instance, e.ChangeType, index);
                });
                return true;
            }
            return false;
        }

        protected virtual object CloneObjectForGameSide(object assetSideObject, IAssetNode assetNode, IGraphNode gameSideNode)
        {
            // Null references, they will be handled by the editor asset loader
            var gameSideValue = AssetCloner.Clone(assetSideObject, AssetClonerFlags.ReferenceAsNull);
            return gameSideValue;
        }

        private async Task PropagatorTask(TAssetPart assetSidePart)
        {
            // Ensure we are ready game-side before doing any operation
            await gameSideReady.Task;

            await Initialize(assetSidePart);
            var contentReferenceCollector = new ContentReferenceCollector(Owner.Asset.PropertyGraph.Definition);
            await Editor.Controller.InvokeTask(() =>
            {
                Editor.Controller.Logger.Debug($"Registering reference to: {Owner.Id}");
                return Editor.Controller.Loader.Manager.RegisterReferencer(Owner.Id);
            });
            var rootNode = Editor.NodeContainer.GetOrCreateNode(assetSidePart);
            contentReferenceCollector.Visit(rootNode);

            await Editor.Controller.InvokeTask(async () =>
            {
                using (await Editor.Controller.Loader.LockDatabaseAsynchronously())
                {
                    foreach (var contentReference in contentReferenceCollector.ContentReferences)
                    {
                        // Ok to await inside the loop, this call should never yield because we already own the lock.
                        await Editor.Controller.Loader.Manager.PushContentReference(Owner.Id, contentReference.ContentId, contentReference.Node, contentReference.Index);
                    }
                }
            });

            initialized.SetResult(0);

            while (running)
            {
                var e = await propagatorQueue.ReceiveAsync();
                // This allow to post null when disposing so we evaluate running again to exit this task
                if (e == null)
                    continue;

                var assetNode = (IAssetNode)e.Node;
                var gameSideNode = assetNode.GetContent(GameSideContentKey);
                if (gameSideNode == null)
                    throw new InvalidOperationException("Unable to retrieve the game-side node");

                var index = (e as ItemChangeEventArgs)?.Index ?? NodeIndex.Empty;
                if (!AssetRegistry.IsContentType(e.Node.Descriptor.GetInnerCollectionType()))
                {
                    if (e.Node.Type.IsValueType)
                    {
                        // No need to retrieve and/or duplicate the value for value type, we just propagate the change
                        await Editor.Controller.InvokeAsync(() => UpdateGameSideContent(gameSideNode, e.NewValue, e.ChangeType, index));
                    }
                    else
                    {
                        var value = RetrieveValue(assetNode, e.NewValue, e.ChangeType, index);
                        var isReference = await PropagatePartReference(gameSideNode, value, e);
                        if (!isReference)
                        {
                            await Editor.Controller.InvokeAsync(() =>
                            {
                                var gameSideValue = CloneObjectForGameSide(value, assetNode, gameSideNode);
                                UpdateGameSideContent(gameSideNode, gameSideValue, e.ChangeType, index);
                            });
                        }
                    }
                }
                else
                {
                    await UpdateGameSideReference(Editor, gameSideNode, e.ChangeType, e.OldValue, e.NewValue, index);
                }

                // Critical section, both Asset thread and Game thread must be locked
                using (await DispatcherLock.Lock(true, Editor.Controller, Editor.Dispatcher))
                {
                    // Link nodes of the asset-side objects with the game-side objects.
                    gameObjectLinker.LinkGraph(assetNode, gameSideNode);
                    // Also collect content reference (this requires only lock on the Asset thread)
                    contentReferenceCollector.Reset();
                    // If the change is a remove, the content reference should have been cleared by UpdateGameSideReference and there's nothing to collect
                    if (e.ChangeType != ContentChangeType.CollectionRemove)
                    {
                        contentReferenceCollector.Visit(assetNode, index);
                    }
                }

                // Update the content reference list on the game-side now that we have push the change.
                await Editor.Controller.InvokeTask(async () =>
                {
                    using (await Editor.Controller.Loader.LockDatabaseAsynchronously())
                    {
                        foreach (var contentReference in contentReferenceCollector.ContentReferences)
                        {
                            // Ok to await inside the loop, this call should never yield because we already own the lock.
                            await Editor.Controller.Loader.Manager.PushContentReference(Owner.Id, contentReference.ContentId, contentReference.Node, contentReference.Index);
                        }
                    }

                    Editor.Controller.TriggerActiveRenderStageReevaluation();
                });
            }

            await Editor.Controller.InvokeTask(() =>
            {
                Editor.Controller.Logger.Debug($"Unregistering reference to: {Owner.Id}");
                return Editor.Controller.Loader.Manager.RemoveReferencer(Owner.Id);
            });
        }

        private static object RetrieveValue(IGraphNode node, object value, ContentChangeType changeType, NodeIndex index)
        {
            switch (changeType)
            {
                case ContentChangeType.ValueChange:
                    return node.Retrieve();
                case ContentChangeType.CollectionUpdate:
                    return node.Retrieve(index);
                case ContentChangeType.CollectionAdd:
                case ContentChangeType.CollectionRemove:
                    return value;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected static void UpdateGameSideContent(IGraphNode gameSideNode, object value, ContentChangeType changeType, NodeIndex index)
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
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task UpdateGameSideReference([NotNull] AssetCompositeEditorViewModel editor, [NotNull] IGraphNode gameSideNode, ContentChangeType changeType, object oldValue, object newValue, NodeIndex index)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));

            if (!AssetRegistry.IsContentType(gameSideNode.Descriptor.GetInnerCollectionType()))
                return;

            // Grab the old referenced object if it's not null
            AttachedReference reference = null;
            if (!ReferenceEquals(oldValue, null))
            {
                reference = AttachedReferenceManager.GetAttachedReference(oldValue);
            }

            // Switch to game thread to actually update objects
            await editor.Controller.InvokeTask(() =>
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
                if (reference != null)
                {
                    return editor.Controller.Loader.Manager.ClearContentReference(Owner.Id, reference.Id, gameSideNode, index);
                }
                return Task.CompletedTask;
            });
        }
    }
}
