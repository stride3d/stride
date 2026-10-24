// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Rendering;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Quantum;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
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

namespace Stride.Assets.Presentation.AssemblyReloading
{
    /// <summary>
    /// Helper to reload game assemblies within the editor.
    /// </summary>
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

                // Read while they are in the container: the copies that go, and those loaded before this call
                var outgoingEditorCode = DeclaresGameEditorCode(session, loadedAssembliesSet.Concat(addedAssemblies ?? new HashSet<Assembly>()));

                // Serialize types from unloaded assemblies as Yaml, and unset them
                var unloadingVisitor = new UnloadingVisitor(log, loadedAssembliesSet);
                Dictionary<AssetViewModel, List<ItemToReload>> assetItemsToReload;
                try
                {
                    assetItemsToReload = PrepareAssemblyReloading(session, unloadingVisitor, loadedAssembliesSet, outgoingEditorCode, session.UndoRedoService, log);
                }
                catch (Exception e)
                {
                    log.Error( "Could not prepare asset for assembly reload", e);
                    throw;
                }

                var reloadOperation = new ReloadAssembliesOperation(assemblyContainer, modifiedAssemblies, []);
                session.UndoRedoService.SetName(reloadOperation, "Reload assemblies");

                // Reload assemblies. The editors that can't follow close before the swap, on a redo as above
                var newCopies = new HashSet<Assembly>();
                var incomingEditorCode = false;
                PushCloseEditorsBeforeSwap(session, null, () => CloseEditorsThatCannotFollow(session, loadedAssembliesSet, outgoingEditorCode || incomingEditorCode, [], null));
                reloadOperation.Execute(log);
                session.UndoRedoService.PushOperation(reloadOperation);
                newCopies.UnionWith(modifiedAssemblies.Keys.Select(x => x.Assembly).NotNull());
                // The new copies can bring gizmos or render features the old ones didn't have
                incomingEditorCode = DeclaresGameEditorCode(session, newCopies);
                if (incomingEditorCode)
                    CloseEditorsThatCannotFollow(session, new HashSet<Assembly>(), true, [], log);
                // On an undo, the reloaded copies are the ones that go
                PushCloseEditorsBeforeSwap(session, () => CloseEditorsThatCannotFollow(session, newCopies, outgoingEditorCode || incomingEditorCode, [], null), null);

                postReloadAction();
                var postReloadOperation = new AnonymousDirtyingOperation([], postReloadAction, postReloadAction);
                session.UndoRedoService.PushOperation(postReloadOperation);
                session.UndoRedoService.SetName(postReloadOperation, "Post reload action");

                // Restore deserialized objects (or IUnloadable if it didn't work)
                try
                {
                    PostAssemblyReloading(session.UndoRedoService, session.AssetNodeContainer, log, assetItemsToReload);
                }
                catch (Exception e)
                {
                    log.Error("Could not restore asset after assembly reload", e);
                    throw;
                }

                var undoOperation = new AnonymousDirtyingOperation(Enumerable.Empty<IDirtiable>(), undoAction, null);
                session.UndoRedoService.PushOperation(undoOperation);
                session.UndoRedoService.SetName(undoOperation, "Undo action");

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

                // Serialize live instances of the assembly's types as Yaml, and unset them
                var unloadingVisitor = new UnloadingVisitor(log, unloadedAssembliesSet);
                Dictionary<AssetViewModel, List<ItemToReload>> assetItemsToReload;
                try
                {
                    assetItemsToReload = PrepareAssemblyReloading(session, unloadingVisitor, unloadedAssembliesSet, editorCode, session.UndoRedoService, log);
                }
                catch (Exception e)
                {
                    log.Error("Could not prepare asset for assembly unload", e);
                    throw;
                }

                var unloadOperation = new UnloadAssembliesOperation(session.AssemblyContainer, package, unloadedAssembly, []);
                session.UndoRedoService.SetName(unloadOperation, "Unload assemblies");
                PushCloseEditorsBeforeSwap(session, null, () => CloseEditorsThatCannotFollow(session, unloadedAssembliesSet, editorCode, [], null));
                unloadOperation.Execute(log);
                session.UndoRedoService.PushOperation(unloadOperation);
                // On an undo, the assembly comes back with what it declares
                PushCloseEditorsBeforeSwap(session, () => CloseEditorsThatCannotFollow(session, new HashSet<Assembly>(), editorCode, [], null), null);

                postUnloadAction();
                var postUnloadOperation = new AnonymousDirtyingOperation([], postUnloadAction, postUnloadAction);
                session.UndoRedoService.PushOperation(postUnloadOperation);
                session.UndoRedoService.SetName(postUnloadOperation, "Post unload action");

                // Restore the cleared items: with the assembly gone they deserialize as IUnloadable
                try
                {
                    PostAssemblyReloading(session.UndoRedoService, session.AssetNodeContainer, log, assetItemsToReload);
                }
                catch (Exception e)
                {
                    log.Error("Could not restore asset after assembly unload", e);
                    throw;
                }

                var undoOperation = new AnonymousDirtyingOperation(Enumerable.Empty<IDirtiable>(), undoAction, null);
                session.UndoRedoService.PushOperation(undoOperation);
                session.UndoRedoService.SetName(undoOperation, "Undo action");
            }

            session.ActiveProperties.RefreshSelectedPropertiesAsync().Forget();
        }

        private static Dictionary<AssetViewModel, List<ItemToReload>> PrepareAssemblyReloading(SessionViewModel session, UnloadingVisitor visitor, IReadOnlySet<Assembly> outgoingAssemblies, bool editorCode, IUndoRedoService actionService, ILogger log)
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

            // An asset restored as a whole gets a new property graph, which its editor isn't bound to
            var wholeAssets = assetItemsToReload.Where(x => x.Value.Any(item => item.ExpectedType == typeof(Asset))).Select(x => x.Key);
            CloseEditorsThatCannotFollow(session, outgoingAssemblies, editorCode, wholeAssets, log);

            foreach (var (asset, itemsToReload) in assetItemsToReload)
            {
                var rootNode = session.AssetNodeContainer.GetNode(asset.Asset);

                // We apply changes in opposite visit order so that indices remains valid when we remove objects while iterating
                for (int index = itemsToReload.Count - 1; index >= 0; index--)
                {
                    var itemToReload = itemsToReload[index];

                    // Transform path to Quantum
                    itemToReload.GraphPath = GraphNodePath.From(rootNode, itemToReload.Path, out itemToReload.GraphPathIndex);

                    // Remove (collections) or replace nodes with null (members) so that we can unload assemblies
                    ClearNode(actionService, asset, itemToReload);
                }
            }

            return assetItemsToReload;
        }

        // An open editor follows an assembly change in place: its asset's objects are swapped through the property graph,
        // its game replaces the components and drops the processors and systems of the unloaded types (EditorServiceGame).
        // It closes only when it can't follow: its asset is restored as a whole, or the editor, its controller, its game or
        // an editor game service runs code of an outgoing assembly (nothing replaces those yet), or an outgoing or incoming
        // assembly declares gizmos (the existing ones aren't recreated) or render features (its compositor isn't rebuilt):
        // editorCode. Its changes are the asset's, which stay in the session.
        private static void CloseEditorsThatCannotFollow(SessionViewModel session, IReadOnlySet<Assembly> outgoingAssemblies, bool editorCode, IEnumerable<AssetViewModel> wholeAssets, ILogger log)
        {
            var editorsManager = session.ServiceProvider.TryGet<IAssetEditorsManager>();
            if (editorsManager is null)
                return;

            var toClose = new HashSet<AssetViewModel>(wholeAssets);
            foreach (var asset in session.AllAssets)
            {
                if (editorsManager.TryGetAssetEditor<IAssetEditorViewModel>(asset, out var editor) && RunsSwappedCode(editor, outgoingAssemblies, editorCode))
                    toClose.Add(asset);
            }

            var closed = new List<string>();
            foreach (var asset in toClose)
            {
                if (editorsManager.TryGetAssetEditor<IAssetEditorViewModel>(asset, out _) && editorsManager.CloseAssetEditorWindow(asset, false))
                    closed.Add(asset.Url);
            }
            if (closed.Count > 0)
                log?.Warning($"Closed the editors of {string.Join(", ", closed)}: they can't follow the reloaded assemblies (the asset's own type, the editor, an editor game service, gizmos or render features). Their changes are kept; open them again to continue.");
        }

        private static bool RunsSwappedCode(IAssetEditorViewModel editor, IReadOnlySet<Assembly> swappedAssemblies, bool editorCode)
        {
            if (swappedAssemblies.Contains(editor.GetType().Assembly))
                return true;
            if (editor is not GameEditorViewModel { Controller: IEditorGameAccess { EditorGame: { } game } controller })
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
                // Not a readable assembly: nothing to close for
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
        /// On an undo or redo of an assembly change, closes the editors that can't follow it before the assemblies are
        /// swapped. Pushed before the swap for a redo, after it for an undo, as operations run in reverse on an undo.
        /// </summary>
        private static void PushCloseEditorsBeforeSwap(SessionViewModel session, Action beforeUndo, Action beforeRedo)
        {
            session.UndoRedoService.PushOperation(new AnonymousDirtyingOperation([], beforeUndo, beforeRedo));
        }

        private static void PostAssemblyReloading(IUndoRedoService actionService, SessionNodeContainer nodeContainer, ILogger log, Dictionary<AssetViewModel, List<ItemToReload>> assetItemsToReload)
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
                    ReplaceNode(actionService, asset.Key, itemToReload);
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

        private static void ClearNode(IUndoRedoService actionService, AssetViewModel asset, ItemToReload itemToReload)
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
                PushAssetChange(actionService, asset, previousAsset, unloadedAsset, $"Unload object {oldValue.GetType().Name} in asset {asset.Url}", Enumerable.Empty<IDirtiable>());
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

        private static void ReplaceNode(IUndoRedoService actionService, AssetViewModel asset, ItemToReload itemToReload)
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
                PushAssetChange(actionService, asset, previousAsset, updatedAsset, $"Reload object {updatedAsset.GetType().Name} in asset {asset.Url}", asset.Dirtiables);
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

        private static void PushAssetChange(IUndoRedoService actionService, AssetViewModel asset, Asset previousAsset, Asset newAsset, string operationName, IEnumerable<IDirtiable> dirtiables)
        {
            var operation = new AssetChangeOperation(asset, previousAsset, newAsset, dirtiables);
            actionService.PushOperation(operation);
            actionService.SetName(operation, operationName);
        }

        /// <summary>
        /// The replacement of an asset as a whole, as <see cref="AssetViewModel.UpdateAsset"/> applies it. The asset's
        /// root node holds no value of its own, so a content change on it cannot be undone.
        /// </summary>
        private class AssetChangeOperation : DirtyingOperation
        {
            private AssetViewModel asset;
            private Asset previousAsset;
            private Asset newAsset;

            public AssetChangeOperation(AssetViewModel asset, Asset previousAsset, Asset newAsset, IEnumerable<IDirtiable> dirtiables)
                : base(dirtiables)
            {
                this.asset = asset;
                this.previousAsset = previousAsset;
                this.newAsset = newAsset;
            }

            protected override void FreezeContent()
            {
                asset = null;
                previousAsset = null;
                newAsset = null;
            }

            protected override void Undo()
            {
                CloseEditor();
                asset.UpdateAsset(previousAsset, new LoggerResult());
            }

            protected override void Redo()
            {
                CloseEditor();
                asset.UpdateAsset(newAsset, new LoggerResult());
            }

            // The asset gets a new property graph, which its editor isn't bound to
            private void CloseEditor()
            {
                if (asset.Session.ServiceProvider.TryGet<IAssetEditorsManager>() is { } editorsManager && editorsManager.TryGetAssetEditor<IAssetEditorViewModel>(asset, out _))
                    editorsManager.CloseAssetEditorWindow(asset, false);
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
