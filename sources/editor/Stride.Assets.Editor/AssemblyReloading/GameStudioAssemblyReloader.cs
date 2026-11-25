// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Rendering;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Quantum;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Quantum.Visitors;
using Stride.Core.Assets.Serializers;
using Stride.Core.Assets.Visitors;
using Stride.Core.Assets.Yaml;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;
using Stride.Engine.Gizmos;
using Stride.Core.Assets.Presentation.Quantum;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Editor.AssemblyReloading
{
    /// <summary>
    /// Reloads the game assemblies of a session in the editor's process: the live objects of the outgoing copies go through
    /// YAML to the incoming ones, as undoable changes of their assets.
    /// </summary>
    /// <remarks>
    /// An open editor follows an assembly change in place: its asset's objects are swapped through the property graph, its
    /// game replaces the components and drops the processors and systems of the unloaded types (EditorServiceGame). It is
    /// reopened at its place, with the new code, when it can't follow: its asset is restored as a whole, or the editor, its
    /// controller, its game or an editor game service runs code of an outgoing assembly (nothing replaces those), or an
    /// outgoing or incoming assembly declares gizmos (the existing ones aren't recreated) or render features (its compositor
    /// isn't rebuilt). Its changes are the asset's, which stay in the session.
    /// </remarks>
    public static class GameStudioAssemblyReloader
    {
        /// <param name="addedAssemblies">Assemblies loaded before this call (a project the editor loads now): they bring no
        /// swap, but an open editor may not follow what they declare.</param>
        public static void Reload([NotNull] SessionViewModel session, ILogger log, Action postReloadAction, Action undoAction, [NotNull] Dictionary<PackageLoadedAssembly, string> modifiedAssemblies, IReadOnlySet<Assembly> addedAssemblies = null)
        {
            var assemblyContainer = session.AssemblyContainer;

            using (session.CreateAssetFixupContext())
            {
                // Every assembly the operation swaps: an object of one left out would keep the types of the unloaded copy
                var loadedAssembliesSet = new HashSet<Assembly>(modifiedAssemblies.Select(x => x.Key.Assembly).NotNull());

                // Read from the files before the swap: the copies that go, those loaded before this call, and the ones that come
                var editorCode = DeclaresGameEditorCode(session, loadedAssembliesSet.Concat(addedAssemblies ?? new HashSet<Assembly>()))
                                 || modifiedAssemblies.Values.Any(DeclaresGameEditorCode);

                // Serialize types from unloaded assemblies as Yaml
                var unloadingVisitor = new UnloadingVisitor(log, loadedAssembliesSet);
                Dictionary<AssetViewModel, List<ItemToReload>> assetItemsToReload;
                try
                {
                    assetItemsToReload = CollectItemsToReload(session, unloadingVisitor);
                }
                catch (Exception e)
                {
                    log.Error( "Could not prepare asset for assembly reload", e);
                    throw;
                }

                var editors = new ReloadedEditors(session);
                var reopened = FindEditorsThatCannotFollow(session, loadedAssembliesSet, editorCode, WholeAssets(assetItemsToReload));
                editors.ReopenAround(reopened, log, () =>
                {
                    // On an undo, the reloaded copies are the ones that go
                    var newCopies = new HashSet<Assembly>();
                    editors.PushCloseBeforeSwap(loadedAssembliesSet, newCopies, editorCode);

                    // Unset the objects of the unloaded assemblies
                    try
                    {
                        ClearNodes(session, editors, assetItemsToReload);
                    }
                    catch (Exception e)
                    {
                        log.Error("Could not prepare asset for assembly reload", e);
                        throw;
                    }

                    // Reload assemblies
                    var reloadOperation = new ReloadAssembliesOperation(assemblyContainer, modifiedAssemblies, []);
                    session.UndoRedoService.SetName(reloadOperation, "Reload assemblies");
                    reloadOperation.Execute(log);
                    session.UndoRedoService.PushOperation(reloadOperation);
                    newCopies.UnionWith(modifiedAssemblies.Keys.Select(x => x.Assembly).NotNull());

                    postReloadAction();
                    var postReloadOperation = new AnonymousDirtyingOperation([], postReloadAction, postReloadAction);
                    session.UndoRedoService.PushOperation(postReloadOperation);
                    session.UndoRedoService.SetName(postReloadOperation, "Post reload action");

                    // Restore deserialized objects (or IUnloadable if it didn't work)
                    try
                    {
                        PostAssemblyReloading(session.UndoRedoService, session.AssetNodeContainer, editors, log, assetItemsToReload);
                    }
                    catch (Exception e)
                    {
                        log.Error("Could not restore asset after assembly reload", e);
                        throw;
                    }

                    var undoOperation = new AnonymousDirtyingOperation(Enumerable.Empty<IDirtiable>(), undoAction, null);
                    session.UndoRedoService.PushOperation(undoOperation);
                    session.UndoRedoService.SetName(undoOperation, "Undo action");

                    editors.PushReopenAfterSwap();
                });
            }

            session.ActiveProperties.RefreshSelectedPropertiesAsync().Forget();
        }

        /// <summary>
        /// Unloads a game assembly within the editor (no reload): live instances of its types are serialized
        /// to Yaml and restored as <see cref="IUnloadable"/>, preserving their content for a later reload.
        /// </summary>
        public static void Unload([NotNull] SessionViewModel session, ILogger log, Action postUnloadAction, Action undoAction, [NotNull] Package package, [NotNull] PackageLoadedAssembly unloadedAssembly)
        {
            using (session.CreateAssetFixupContext())
            {
                var unloadedAssembliesSet = new HashSet<Assembly>();
                if (unloadedAssembly.Assembly != null)
                    unloadedAssembliesSet.Add(unloadedAssembly.Assembly);

                var editorCode = DeclaresGameEditorCode(session, unloadedAssembliesSet);

                // Serialize live instances of the assembly's types as Yaml
                var unloadingVisitor = new UnloadingVisitor(log, unloadedAssembliesSet);
                Dictionary<AssetViewModel, List<ItemToReload>> assetItemsToReload;
                try
                {
                    assetItemsToReload = CollectItemsToReload(session, unloadingVisitor);
                }
                catch (Exception e)
                {
                    log.Error("Could not prepare asset for assembly unload", e);
                    throw;
                }

                var editors = new ReloadedEditors(session);
                var reopened = FindEditorsThatCannotFollow(session, unloadedAssembliesSet, editorCode, WholeAssets(assetItemsToReload));
                editors.ReopenAround(reopened, log, () =>
                {
                    // On an undo, the assembly comes back with what it declares
                    editors.PushCloseBeforeSwap(unloadedAssembliesSet, new HashSet<Assembly>(), editorCode);

                    // Unset the objects of the unloaded assembly
                    try
                    {
                        ClearNodes(session, editors, assetItemsToReload);
                    }
                    catch (Exception e)
                    {
                        log.Error("Could not prepare asset for assembly unload", e);
                        throw;
                    }

                    var unloadOperation = new UnloadAssembliesOperation(session.AssemblyContainer, package, unloadedAssembly, []);
                    session.UndoRedoService.SetName(unloadOperation, "Unload assemblies");
                    unloadOperation.Execute(log);
                    session.UndoRedoService.PushOperation(unloadOperation);

                    postUnloadAction();
                    var postUnloadOperation = new AnonymousDirtyingOperation([], postUnloadAction, postUnloadAction);
                    session.UndoRedoService.PushOperation(postUnloadOperation);
                    session.UndoRedoService.SetName(postUnloadOperation, "Post unload action");

                    // Restore the cleared items: with the assembly gone they deserialize as IUnloadable
                    try
                    {
                        PostAssemblyReloading(session.UndoRedoService, session.AssetNodeContainer, editors, log, assetItemsToReload);
                    }
                    catch (Exception e)
                    {
                        log.Error("Could not restore asset after assembly unload", e);
                        throw;
                    }

                    var undoOperation = new AnonymousDirtyingOperation(Enumerable.Empty<IDirtiable>(), undoAction, null);
                    session.UndoRedoService.PushOperation(undoOperation);
                    session.UndoRedoService.SetName(undoOperation, "Undo action");

                    editors.PushReopenAfterSwap();
                });
            }

            session.ActiveProperties.RefreshSelectedPropertiesAsync().Forget();
        }

        private static Dictionary<AssetViewModel, List<ItemToReload>> CollectItemsToReload(SessionViewModel session, UnloadingVisitor visitor)
        {
            var assetItemsToReload = new Dictionary<AssetViewModel, List<ItemToReload>>();

            // Serialize old objects from unloaded assemblies to Yaml streams
            foreach (var asset in session.LocalPackages.SelectMany(x => x.Assets))
            {
                // Asset has no quantum graph (eg. text asset), let's skip it
                if (asset.PropertyGraph == null)
                    continue;

                // Generate missing ids for safetly
                AssetCollectionItemIdHelper.GenerateMissingItemIds(asset.Asset);

                // Serialize objects with types to unload to Yaml
                // Objects that were already IUnloadable will also be included
                var itemsToReload = visitor.Run(asset.PropertyGraph);

                // Add to list of items to reload for PostAssemblyReloading
                if (itemsToReload.Count > 0)
                    assetItemsToReload.Add(asset, new List<ItemToReload>(itemsToReload));
            }

            foreach (var (asset, itemsToReload) in assetItemsToReload)
            {
                var rootNode = session.AssetNodeContainer.GetNode(asset.Asset);
                foreach (var itemToReload in itemsToReload)
                {
                    // Transform path to Quantum
                    itemToReload.GraphPath = GraphNodePath.From(rootNode, itemToReload.Path, out itemToReload.GraphPathIndex);
                }
            }

            return assetItemsToReload;
        }

        // An asset restored as a whole gets a new property graph, which its editor isn't bound to
        private static IEnumerable<AssetViewModel> WholeAssets(Dictionary<AssetViewModel, List<ItemToReload>> assetItemsToReload)
            => assetItemsToReload.Where(x => x.Value.Any(item => item.ExpectedType == typeof(Asset))).Select(x => x.Key);

        private static void ClearNodes(SessionViewModel session, ReloadedEditors editors, Dictionary<AssetViewModel, List<ItemToReload>> assetItemsToReload)
        {
            foreach (var (asset, itemsToReload) in assetItemsToReload)
            {
                // We apply changes in opposite visit order so that indices remains valid when we remove objects while iterating
                for (int index = itemsToReload.Count - 1; index >= 0; index--)
                {
                    // Remove (collections) or replace nodes with null (members) so that we can unload assemblies
                    ClearNode(session.UndoRedoService, editors, asset, itemsToReload[index]);
                }
            }
        }

        /// <summary>
        /// The assets whose open editor can't follow the change (see the remarks of the class): those restored as a whole, and
        /// those whose editor runs code of an outgoing assembly, or any game editor when <paramref name="editorCode"/>.
        /// </summary>
        private static List<AssetViewModel> FindEditorsThatCannotFollow(SessionViewModel session, IReadOnlySet<Assembly> outgoingAssemblies, bool editorCode, IEnumerable<AssetViewModel> wholeAssets)
        {
            var result = new List<AssetViewModel>();
            if (session.ServiceProvider.TryGet<IAssetEditorsManager>() is not { } editorsManager)
                return result;

            foreach (var asset in wholeAssets.Concat(session.AllAssets).Distinct())
            {
                if (editorsManager.TryGetAssetEditor<IAssetEditorViewModel>(asset, out var editor)
                    && (wholeAssets.Contains(asset) || RunsSwappedCode(editor, outgoingAssemblies, editorCode)))
                {
                    result.Add(asset);
                }
            }
            return result;
        }

        private static bool RunsSwappedCode(IAssetEditorViewModel editor, IReadOnlySet<Assembly> swappedAssemblies, bool editorCode)
        {
            if (swappedAssemblies.Contains(editor.GetType().Assembly))
                return true;
            if (editor is not IGameEditorViewModel { GameHost.Controller: { } controller } || controller.EditorGame is not { } game)
                return false;

            return editorCode
                || swappedAssemblies.Contains(controller.GetType().Assembly)
                || swappedAssemblies.Contains(game.GetType().Assembly)
                || game.EditorServices?.Services.Any(x => swappedAssemblies.Contains(x.GetType().Assembly)) == true;
        }

        // Gizmos and render features: an editor game uses them, but takes them once. Read from the assembly's file, as
        // loading its types would resolve its references, and bring back as a new copy (outside the registry) an assembly
        // the change just unloaded.
        private static bool DeclaresGameEditorCode(SessionViewModel session, IEnumerable<Assembly> assemblies)
        {
            var paths = session.AssemblyContainer.LoadedAssemblies.ToDictionary(x => x.Assembly, x => x.Path);
            return assemblies.Any(x => DeclaresGameEditorCode(paths.GetValueOrDefault(x) is { Length: > 0 } path ? path : x.Location));
        }

        private static bool DeclaresGameEditorCode(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;
            try
            {
                using var stream = File.OpenRead(path);
                using var peReader = new PEReader(stream);
                if (!peReader.HasMetadata)
                    return false;
                var metadata = peReader.GetMetadataReader();
                foreach (var typeHandle in metadata.TypeDefinitions)
                {
                    var type = metadata.GetTypeDefinition(typeHandle);
                    if ((type.Attributes & TypeAttributes.Abstract) != 0)
                        continue;
                    foreach (var interfaceHandle in type.GetInterfaceImplementations())
                    {
                        if (IsType(metadata, metadata.GetInterfaceImplementation(interfaceHandle).Interface, typeof(IRenderFeatureProvider)))
                            return true;
                    }
                    foreach (var attributeHandle in type.GetCustomAttributes())
                    {
                        var constructor = metadata.GetCustomAttribute(attributeHandle).Constructor;
                        var attributeType = constructor.Kind == HandleKind.MemberReference ? metadata.GetMemberReference((MemberReferenceHandle)constructor).Parent : default;
                        if (IsType(metadata, attributeType, typeof(GizmoComponentAttribute)))
                            return true;
                    }
                }
            }
            catch (Exception)
            {
                // Not a readable assembly: nothing to reopen for
            }
            return false;
        }

        private static bool IsType(MetadataReader metadata, EntityHandle handle, Type type)
        {
            if (handle.Kind != HandleKind.TypeReference)
                return false;
            var reference = metadata.GetTypeReference((TypeReferenceHandle)handle);
            return metadata.StringComparer.Equals(reference.Name, type.Name) && metadata.StringComparer.Equals(reference.Namespace, type.Namespace!);
        }

        /// <summary>
        /// The editors an assembly change reopens. The change itself reopens each at its place
        /// (<see cref="IAssetEditorsManager.ReopenAssetEditorAsync"/>); its undo and redo close them before the swap and open
        /// them again after it.
        /// </summary>
        private sealed class ReloadedEditors
        {
            private readonly SessionViewModel session;
            private readonly List<AssetViewModel> closed = [];
            // What an undo swaps out, for the editors it closes
            private IReadOnlySet<Assembly> undoOutgoingAssemblies = new HashSet<Assembly>();
            private bool undoEditorCode;

            public ReloadedEditors(SessionViewModel session)
            {
                this.session = session;
            }

            private IAssetEditorsManager EditorsManager => session.ServiceProvider.TryGet<IAssetEditorsManager>();

            /// <summary>
            /// Runs the change with the editors of these assets closed, and opens them again at their place afterwards, even
            /// when the change failed.
            /// </summary>
            public void ReopenAround(IReadOnlyList<AssetViewModel> assets, ILogger log, Action change)
            {
                if (EditorsManager is not { } editorsManager || assets.Count == 0)
                {
                    change();
                    return;
                }

                void Reopen(int index)
                {
                    if (index == assets.Count)
                        change();
                    else
                        editorsManager.ReopenAssetEditorAsync(assets[index], () => Reopen(index + 1)).Forget();
                }
                Reopen(0);
                log?.Info($"Reopened the editors of {string.Join(", ", assets.Select(x => x.Url))}: they can't follow the reloaded assemblies in place (the asset's own type, the editor, an editor game service, gizmos or render features).");
            }

            /// <summary>
            /// Pushed before the swap: a redo closes the editors that can't follow the outgoing copies, an undo opens again the
            /// ones it closed (operations run in reverse on an undo).
            /// </summary>
            public void PushCloseBeforeSwap(IReadOnlySet<Assembly> redoOutgoing, IReadOnlySet<Assembly> undoOutgoing, bool editorCode)
            {
                var operation = new AnonymousDirtyingOperation([], ReopenClosed, () => CloseThatCannotFollow(redoOutgoing, editorCode));
                session.UndoRedoService.PushOperation(operation);
                session.UndoRedoService.SetName(operation, "Close the editors that can't follow");
                undoOutgoingAssemblies = undoOutgoing;
                undoEditorCode = editorCode;
            }

            /// <summary>
            /// Pushed after the swap: an undo closes the editors that can't follow its swap back, a redo opens again the ones
            /// closed before its swap.
            /// </summary>
            public void PushReopenAfterSwap()
            {
                var operation = new AnonymousDirtyingOperation([], () => CloseThatCannotFollow(undoOutgoingAssemblies, undoEditorCode), ReopenClosed);
                session.UndoRedoService.PushOperation(operation);
                session.UndoRedoService.SetName(operation, "Open the editors again");
            }

            /// <summary>
            /// Closes the editor of an asset replaced as a whole, on an undo or a redo; it opens again with the others.
            /// </summary>
            public void Close(AssetViewModel asset)
            {
                if (EditorsManager is not { } editorsManager || !editorsManager.TryGetAssetEditor<IAssetEditorViewModel>(asset, out _))
                    return;

                // Without saving, an editor of a game closes at once
                var closing = editorsManager.CloseAssetEditorAsync(asset, false);
                if (closing.IsCompletedSuccessfully && closing.Result && !closed.Contains(asset))
                    closed.Add(asset);
            }

            private void CloseThatCannotFollow(IReadOnlySet<Assembly> outgoingAssemblies, bool editorCode)
            {
                foreach (var asset in FindEditorsThatCannotFollow(session, outgoingAssemblies, editorCode, []))
                    Close(asset);
            }

            private void ReopenClosed()
            {
                if (EditorsManager is not { } editorsManager)
                    return;

                foreach (var asset in closed)
                    editorsManager.OpenAssetEditorAsync(asset).Forget();
                closed.Clear();
            }
        }

        private static void PostAssemblyReloading(IUndoRedoService actionService, SessionNodeContainer nodeContainer, ReloadedEditors editors, ILogger log, Dictionary<AssetViewModel, List<ItemToReload>> assetItemsToReload)
        {
            log?.Info("Updating components with newly loaded assemblies");

            // Recreate new objects from Yaml streams, each asset on its own: one that fails to restore is reported, the
            // others are still restored
            foreach (var asset in assetItemsToReload)
            {
                try
                {
                    RestoreAsset(asset);
                }
                catch (Exception e)
                {
                    log?.Error($"Could not restore [{asset.Key.Url}] after the assembly change", e);
                }
            }

            void RestoreAsset(KeyValuePair<AssetViewModel, List<ItemToReload>> asset)
            {
                // Deserialize objects with reloaded types from Yaml
                var objectReferences = new YamlAssetMetadata<Guid>();
                foreach (var itemToReload in asset.Value)
                {
                    var expectedPath = itemToReload.Path.Decompose().LastOrDefault()?.GetIndex() != null ? itemToReload.ParentPath : itemToReload.Path;
                    if (expectedPath.TryGetValue(asset.Key.Asset, out _) == false)
                        continue;

                    // The asset as a whole goes through its upgraders first, as when its package loads
                    var parsingEvents = itemToReload.GraphPath.IsEmpty && itemToReload.GraphPathIndex == NodeIndex.Empty
                        ? MigrateAsset(asset.Key, itemToReload, log)
                        : itemToReload.ParsingEvents;
                    var eventReader = new EventReader(new MemoryParser(parsingEvents));
                    var settings = log != null ? new SerializerContextSettings { Logger = log } : null;
                    PropertyContainer properties;
                    itemToReload.UpdatedObject = AssetYamlSerializer.Default.Deserialize(eventReader, null, itemToReload.ExpectedType, out properties, settings);

                    // We will have broken references here because we are deserializing objects individually, so we don't pass any logger to discard warnings
                    var metadata = YamlAssetSerializer.CreateAndProcessMetadata(properties, itemToReload.UpdatedObject, false);

                    itemToReload.Overrides = metadata.RetrieveMetadata(AssetObjectSerializerBackend.OverrideDictionaryKey);

                    var references = metadata.RetrieveMetadata(AssetObjectSerializerBackend.ObjectReferencesKey);
                    if (references != null)
                    {
                        var basePath = YamlAssetPath.FromMemberPath(expectedPath, asset.Key.Asset);
                        foreach (var reference in references)
                        {
                            var basePathWithIndex = basePath.Clone();
                            if (itemToReload.GraphPathIndex != NodeIndex.Empty)
                            {
                                if (itemToReload.ItemId == ItemId.Empty)
                                    basePathWithIndex.PushIndex(itemToReload.GraphPathIndex.Value);
                                else
                                    basePathWithIndex.PushItemId(itemToReload.ItemId);
                            }

                            var actualPath = basePathWithIndex.Append(reference.Key);
                            objectReferences.Set(actualPath, reference.Value);
                        }
                    }
                }

                // Set new values
                var overrides = new YamlAssetMetadata<OverrideType>();
                foreach (var itemToReload in asset.Value)
                {
                    // Set (members) or add nodes (collections) with values created using newly loaded assemblies
                    ReplaceNode(actionService, editors, asset.Key, itemToReload);
                    if (itemToReload.Overrides != null)
                    {
                        var extendedPath = itemToReload.GraphPath.Clone();
                        if (itemToReload.GraphPathIndex != NodeIndex.Empty)
                            extendedPath.PushIndex(itemToReload.GraphPathIndex);

                        var pathToPrepend = AssetNodeMetadataCollectorBase.ConvertPath(extendedPath);

                        foreach (var entry in itemToReload.Overrides)
                        {
                            var path = pathToPrepend.Append(entry.Key);
                            overrides.Set(path, entry.Value);
                        }
                    }
                }

                FixupObjectReferences.RunFixupPass(asset.Key.Asset, objectReferences, true, false, log);

                var rootNode = (IAssetNode)nodeContainer.GetNode(asset.Key.Asset);            
                AssetPropertyGraph.ApplyOverrides(rootNode, overrides);
            }
        }

        private static void ClearNode(IUndoRedoService actionService, ReloadedEditors editors, AssetViewModel asset, ItemToReload itemToReload)
        {
            // Get the node we want to change
            var index = itemToReload.GraphPathIndex;
            var node = itemToReload.GraphPath.GetNode();
            var oldValue = node.Retrieve(index);

            // Apply the change
            // TODO: Share this code with ContentValueChangeOperation?
            // TODO: How to better detect CollectionAdd vs ValueChange?
            ContentChangeType operationType;
            if (index != NodeIndex.Empty)
            {
                CollectionItemIdentifiers ids;
                if (CollectionItemIdHelper.TryGetCollectionItemIds(node.Retrieve(), out ids))
                {
                    itemToReload.ItemId = ids[index.Value];
                }
                operationType = ContentChangeType.CollectionRemove;
                ((IObjectNode)node).Remove(oldValue, index);
            }
            else if (node is IMemberNode memberNode)
            {
                operationType = ContentChangeType.ValueChange;
                memberNode.Update(null);
            }
            else if (node is IObjectNode)
            {
                // The asset itself: it is replaced as a whole, so the change is undone the same way
                var unloadedAsset = (Asset)UnloadableObjectInstantiator.CreateUnloadableObject(typeof(Asset), itemToReload.ExpectedType.Name, "Test", "Reloading", itemToReload.ParsingEvents);
                unloadedAsset.Id = asset.Id;
                var previousAsset = asset.Asset;
                asset.UpdateAsset(unloadedAsset, new LoggerResult());
                PushAssetChange(actionService, editors, asset, previousAsset, unloadedAsset, $"Unload object {oldValue.GetType().Name} in asset {asset.Url}", Enumerable.Empty<IDirtiable>());
                return;
            }
            else
            {
                throw new NotImplementedException();
            }

            // Save the change on the stack
            var operation = new ContentValueChangeOperation(node, operationType, index, oldValue, null, Enumerable.Empty<IDirtiable>());
            actionService.PushOperation(operation);
            string operationName = $"Unload object {oldValue.GetType().Name} in asset {asset.Url}";
            actionService.SetName(operation, operationName);
        }

        private static void ReplaceNode(IUndoRedoService actionService, ReloadedEditors editors, AssetViewModel asset, ItemToReload itemToReload)
        {
            // Get the node we want to change
            var graphPath = itemToReload.GraphPath;
            var index = itemToReload.GraphPathIndex;
            var node = graphPath.GetNode();

            // Apply the change
            // TODO: Share this code with ContentValueChangeOperation?
            // TODO: How to better detect CollectionAdd vs ValueChange?
            ContentChangeType operationType;
            if (index != NodeIndex.Empty)
            {
                operationType = ContentChangeType.CollectionAdd;
                ((IAssetObjectNode)node).Restore(itemToReload.UpdatedObject, index, itemToReload.ItemId);
            }
            else if (node is IMemberNode memberNode)
            {
                operationType = ContentChangeType.ValueChange;
                memberNode.Update(itemToReload.UpdatedObject);
            }
            else if (node is IObjectNode)
            {
                // The asset itself: it is replaced as a whole, so the change is undone the same way
                var updatedAsset = (Asset)itemToReload.UpdatedObject;
                var previousAsset = asset.Asset;
                asset.UpdateAsset(updatedAsset, new LoggerResult());
                PushAssetChange(actionService, editors, asset, previousAsset, updatedAsset, $"Reload object {updatedAsset.GetType().Name} in asset {asset.Url}", asset.Dirtiables);
                return;
            }
            else
            {
                throw new NotImplementedException();
            }

            // Save the change on the stack
            var operation = new ContentValueChangeOperation(node, operationType, index, null, itemToReload.UpdatedObject, asset.Dirtiables);
            actionService.PushOperation(operation);
            string operationName = $"Reload object {itemToReload.UpdatedObject.GetType().Name} in asset {asset.Url}";
            actionService.SetName(operation, operationName);
        }

        /// <summary>
        /// Runs the upgraders of an asset that comes back as a whole on the YAML it was kept as. An asset that was
        /// unloadable kept the YAML read from its file, which its upgraders could not run on without its type; a reloaded
        /// assembly can also declare a newer format for its assets.
        /// </summary>
        private static List<ParsingEvent> MigrateAsset(AssetViewModel asset, ItemToReload itemToReload, ILogger log)
        {
            var assetItem = asset.AssetItem;
            var context = new AssetMigrationContext(assetItem.Package, assetItem.ToReference(), assetItem.FullPath.ToOSPath(), log ?? new LoggerResult());
            return AssetMigration.MigrateAssetIfNeeded(context, itemToReload.ParsingEvents, assetItem.FullPath, itemToReload.ExpectedType) ?? itemToReload.ParsingEvents;
        }

        private static void PushAssetChange(IUndoRedoService actionService, ReloadedEditors editors, AssetViewModel asset, Asset previousAsset, Asset newAsset, string operationName, IEnumerable<IDirtiable> dirtiables)
        {
            var operation = new AssetChangeOperation(editors, asset, previousAsset, newAsset, dirtiables);
            actionService.PushOperation(operation);
            actionService.SetName(operation, operationName);
        }

        /// <summary>
        /// The replacement of an asset as a whole, as <see cref="AssetViewModel.UpdateAsset"/> applies it. The asset's
        /// root node holds no value of its own, so a content change on it cannot be undone.
        /// </summary>
        private sealed class AssetChangeOperation : DirtyingOperation
        {
            private ReloadedEditors editors;
            private AssetViewModel asset;
            private Asset previousAsset;
            private Asset newAsset;

            public AssetChangeOperation(ReloadedEditors editors, AssetViewModel asset, Asset previousAsset, Asset newAsset, IEnumerable<IDirtiable> dirtiables)
                : base(dirtiables)
            {
                this.editors = editors;
                this.asset = asset;
                this.previousAsset = previousAsset;
                this.newAsset = newAsset;
            }

            protected override void FreezeContent()
            {
                editors = null;
                asset = null;
                previousAsset = null;
                newAsset = null;
            }

            // The asset gets a new property graph, which its editor isn't bound to: the editor opens again after the swap
            protected override void Undo()
            {
                editors.Close(asset);
                asset.UpdateAsset(previousAsset, new LoggerResult());
            }

            protected override void Redo()
            {
                editors.Close(asset);
                asset.UpdateAsset(newAsset, new LoggerResult());
            }
        }

        /// <summary>
        /// Serializes and deserializes part of assets that needs reloading.
        /// </summary>
        private abstract class ReloaderVisitorBase : AssetVisitorBase
        {
            protected readonly HashSet<Assembly> UnloadedAssemblies;
            protected readonly ILogger Log;
            protected List<ItemToReload> ItemsToReload;

            protected ReloaderVisitorBase(ILogger log, HashSet<Assembly> unloadedAssemblies)
            {
                Log = log;
                UnloadedAssemblies = unloadedAssemblies;
            }

            public override void VisitObject(object obj, ObjectDescriptor descriptor, bool visitMembers)
            {
                // For asset roots (not reference), we want the base type rather than the real one (which will be unloaded)
                var expectedType = descriptor.Type;
                if (obj is Asset)
                    expectedType = typeof(Asset);

                if (ProcessObject(obj, expectedType)) return;

                base.VisitObject(obj, descriptor, visitMembers);
            }

            public override void VisitCollectionItem(IEnumerable collection, CollectionDescriptor descriptor, int index, object item, ITypeDescriptor itemDescriptor)
            {
                if (ProcessObject(item, descriptor.ElementType)) return;

                base.VisitCollectionItem(collection, descriptor, index, item, itemDescriptor);
            }

            public override void VisitArrayItem(Array array, ArrayDescriptor descriptor, int index, object item, ITypeDescriptor itemDescriptor)
            {
                if (ProcessObject(item, descriptor.ElementType)) return;

                base.VisitArrayItem(array, descriptor, index, item, itemDescriptor);
            }

            public override void VisitDictionaryKeyValue(object dictionary, DictionaryDescriptor descriptor, object key, ITypeDescriptor keyDescriptor, object value, ITypeDescriptor valueDescriptor)
            {
                // TODO: CurrentPath is valid only for value, not key
                //if (ProcessObject(key, keyDescriptor.Type)) key = null;
                if (ProcessObject(value, valueDescriptor.Type)) return;

                Visit(value, valueDescriptor);
                //base.VisitDictionaryKeyValue(dictionary, descriptor, key, keyDescriptor, value, valueDescriptor);
            }

            public override void VisitSetItem(IEnumerable set, SetDescriptor descriptor, object item, ITypeDescriptor itemDescriptor)
            {
                if (ProcessObject(item, itemDescriptor.Type)) return;

                base.VisitSetItem(set, descriptor, item, itemDescriptor);
            }

            protected abstract bool ProcessObject(object obj, Type expectedType);
        }

        private class UnloadingVisitor : ReloaderVisitorBase
        {
            private AssetPropertyGraph propertyGraph;

            public UnloadingVisitor(ILogger log, HashSet<Assembly> unloadedAssemblies)
                : base(log, unloadedAssemblies)
            {
            }

            [NotNull]
            public List<ItemToReload> Run([NotNull] AssetPropertyGraph assetPropertyGraph)
            {
                if (assetPropertyGraph == null) throw new ArgumentNullException(nameof(assetPropertyGraph));
                Reset();
                propertyGraph = assetPropertyGraph;
                var result = ItemsToReload = new List<ItemToReload>();
                Visit(assetPropertyGraph.RootNode.Retrieve());
                ItemsToReload = null;
                return result;
            }

            protected override bool ProcessObject(object obj, Type expectedType)
            {
                // TODO: More advanced checks if IUnloadable is supposed to be a type from the unloaded assembly (this would avoid processing unecessary IUnloadable)
                if (obj != null && (UnloadedAssemblies.Contains(obj.GetType().Assembly) || obj is IUnloadable))
                {
                    NodeIndex index;
                    var settings = new SerializerContextSettings(Log);
                    var path = GraphNodePath.From(propertyGraph.RootNode, CurrentPath, out index);

                    var overrides = AssetPropertyGraph.GenerateOverridesForSerialization(path.GetNode());
                    overrides = RemoveFirstIndexInYamlPath(overrides, index);
                    if (overrides != null)
                        settings.Properties.Add(AssetObjectSerializerBackend.OverrideDictionaryKey, overrides);

                    var objectReferences = propertyGraph.GenerateObjectReferencesForSerialization(path.GetNode());
                    objectReferences = RemoveFirstIndexInYamlPath(objectReferences, index);
                    if (objectReferences != null)
                        settings.Properties.Add(AssetObjectSerializerBackend.ObjectReferencesKey, objectReferences);

                    var parsingEvents = new List<ParsingEvent>();
                    AssetYamlSerializer.Default.Serialize(new ParsingEventListEmitter(parsingEvents), obj, expectedType, settings);

                    // Buid parent path
                    var parentPath = CurrentPath.Clone();
                    parentPath.Pop();

                    ItemsToReload.Add(new ItemToReload(parentPath, CurrentPath.Clone(), parsingEvents, expectedType));

                    // Don't recurse inside
                    return true;
                }
                return false;
            }

            private static YamlAssetMetadata<T> RemoveFirstIndexInYamlPath<T>([CanBeNull] YamlAssetMetadata<T> metadata, NodeIndex index)
            {
                if (metadata != null)
                {
                    // If we had an index we need to remove it from our override paths
                    if (index != NodeIndex.Empty)
                    {
                        var fixedMetadata = new YamlAssetMetadata<T>();
                        foreach (var entry in metadata)
                        {
                            // We're filling a new dictionary because we are destroying hash codes by muting the keys
                            var newPath = entry.Key.Elements.Count > 0 ? new YamlAssetPath(entry.Key.Elements.Skip(1)) : entry.Key;
                            fixedMetadata.Set(newPath, entry.Value);
                        }
                        metadata = fixedMetadata;
                    }
                }
                return metadata;
            }
        }

        /// <summary>
        /// Part of an asset to reload.
        /// </summary>
        private class ItemToReload
        {
            /// <summary>
            /// Path of the node containing the item.
            /// </summary>
            public readonly MemberPath ParentPath;

            /// <summary>
            /// Path of the item.
            /// </summary>
            public readonly MemberPath Path;

            /// <summary>
            /// The Yaml events.
            /// </summary>
            public readonly List<ParsingEvent> ParsingEvents;

            /// <summary>
            /// The expected type of the item (i.e. parent collection type or member type).
            /// </summary>
            public readonly Type ExpectedType;

            /// <summary>
            /// The converted graph path.
            /// </summary>
            public GraphNodePath GraphPath;

            /// <summary>
            /// The additional index to apply on the graph path to reach the item.
            /// </summary>
            public NodeIndex GraphPathIndex;

            /// <summary>
            /// The identifier of the item to reload, if relevant.
            /// </summary>
            public ItemId ItemId;

            /// <summary>
            /// The newly deserialized object with updated assemblies.
            /// </summary>
            public object UpdatedObject;

            public ItemToReload(MemberPath parentPath, MemberPath path, List<ParsingEvent> parsingEvents, Type expectedType)
            {
                ParentPath = parentPath;
                Path = path;
                ParsingEvents = parsingEvents;
                ExpectedType = expectedType;
            }

            public YamlAssetMetadata<OverrideType> Overrides { get; set; }

            public override string ToString() => $"[{Path}] {ExpectedType}";
        }
    }
}
