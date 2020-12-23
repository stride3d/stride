// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Quantum;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Quantum;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;
using Stride.Core.Quantum.References;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// An interface representing the view model of an <see cref="Asset"/>.
    /// </summary>
    /// <typeparam name="TAsset">The type of asset represented by this view model.</typeparam>
    public interface IAssetViewModel<out TAsset>
        where TAsset : Asset
    {
        /// <summary>
        /// Gets the asset object related to this view model.
        /// </summary>
        [NotNull]
        TAsset Asset { get; }
    }

    /// <summary>
    /// A generic version of the <see cref="AssetViewModel"/> class that allows to access directly the proper type of asset represented by this view model.
    /// </summary>
    /// <typeparam name="TAsset">The type of asset represented by this view model.</typeparam>
    public class AssetViewModel<TAsset> : AssetViewModel, IAssetViewModel<TAsset>
        where TAsset : Asset
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetViewModel{TAsset}"/> class.
        /// </summary>
        /// <param name="parameters">The constructor parameter for this asset view model.</param>
        public AssetViewModel([NotNull] AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
        }

        /// <inheritdoc />
        public new TAsset Asset => (TAsset)base.Asset;
    }

    /// <summary>
    /// A view model class that represents a single asset.
    /// </summary>
    public abstract class AssetViewModel : SessionObjectViewModel, IChildViewModel, ISessionObjectViewModel, IAssetPropertyProviderViewModel, IDisposable
    {
        protected internal IAssetObjectNode AssetRootNode => PropertyGraph?.RootNode;
        protected readonly ObservableList<MenuCommandInfo> assetCommands;
        protected readonly SessionNodeContainer NodeContainer;
        protected readonly bool Initializing;
        private readonly AnonymousCommand clearArchetypeCommand;
        private readonly AnonymousCommand createDerivedAssetCommand;
        private Package package;
        private string name;
        private DirectoryBaseViewModel directory;
        private bool updatingUrl;
        private ThumbnailData thumbnailData;
        private AssetItem assetItem;
        private IAssetEditorViewModel editor;
        private TaskCompletionSource<int> editorInitialized = new TaskCompletionSource<int>();
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetViewModel"/> class.
        /// </summary>
        protected AssetViewModel([NotNull] AssetViewModelConstructionParameters parameters)
            : base(parameters.SafeArgument(nameof(parameters)).Directory.Session)
        {
            Initializing = true;
            package = parameters.Package;
            directory = parameters.Directory;
            assetItem = parameters.AssetItem;
            HasBeenUpgraded = assetItem.IsDirty;
            var forcedRoot = AssetType.GetCustomAttribute<AssetDescriptionAttribute>()?.AlwaysMarkAsRoot ?? false;
            Dependencies = new AssetDependenciesViewModel(this, forcedRoot);
            Sources = new AssetSourcesViewModel(this);

            InitialUndelete(parameters.CanUndoRedoCreation);

            name = Path.GetFileName(assetItem.Location);

            Tags.AddRange(assetItem.Asset.Tags);

            RegisterMemberCollectionForActionStack(nameof(Tags), Tags);
            Tags.CollectionChanged += TagsCollectionChanged;

            assetCommands = new ObservableList<MenuCommandInfo>();
            createDerivedAssetCommand = new AnonymousCommand(ServiceProvider, CreateDerivedAsset) { IsEnabled = CanDerive };
            clearArchetypeCommand = new AnonymousCommand(ServiceProvider, ClearArchetype) { IsEnabled = Asset.Archetype != null };
            // TODO: make the view model independent of the view (ie. MenuCommandInfo.Icon and remove this dispatcher call.
            Dispatcher.InvokeAsync(() =>
            {
                assetCommands.Add(new MenuCommandInfo(ServiceProvider, createDerivedAssetCommand)
                {
                    DisplayName = "Create derived asset",
                    Icon = new Image { Source = new BitmapImage(new Uri("/Stride.Core.Assets.Editor;component/Resources/Icons/copy_link-32.png", UriKind.RelativeOrAbsolute)) },
                });
                assetCommands.Add(new MenuCommandInfo(ServiceProvider, clearArchetypeCommand)
                {
                    DisplayName = "Clear archetype",
                    Icon = new Image { Source = new BitmapImage(new Uri("/Stride.Core.Assets.Editor;component/Resources/Icons/delete_link-32.png", UriKind.RelativeOrAbsolute)) },
                });
            }).Forget();
            NodeContainer = parameters.Container;
            PropertyGraph = Session.GraphContainer.TryGetGraph(assetItem.Id);
            if (PropertyGraph != null)
            {
                PropertyGraph.BaseContentChanged += BaseContentChanged;
                PropertyGraph.Changed += AssetPropertyChanged;
                PropertyGraph.ItemChanged += AssetPropertyChanged;
            }
            // Add to directory after asset node has been created, so that listener to directory changes can retrieve it
            directory.AddAsset(this, parameters.CanUndoRedoCreation);
            Initializing = false;
        }

        /// <summary>
        /// Gets the url of this asset.
        /// </summary>
        public string Url => AssetItem.Location;

        /// <summary>
        /// Gets the unique identifier of this asset.
        /// </summary>
        public AssetId Id => AssetItem.Id;

        /// <summary>
        /// Gets or sets the name of this asset.
        /// </summary>
        public override string Name { get => name; set => Rename(value); }

        /// <summary>
        /// Gets or sets the collection of tags associated to this asset.
        /// </summary>
        public ObservableList<string> Tags { get; } = new ObservableList<string>();

        /// <summary>
        /// Gets or sets the directory containing this asset.
        /// </summary>
        public DirectoryBaseViewModel Directory { get => directory; set => SetValue(ref directory, value); }

        /// <summary>
        /// Gets whether this asset can provide an <see cref="GraphViewModel"/> representing its properties.
        /// </summary>
        public virtual bool CanProvidePropertiesViewModel => !IsDeleted && IsEditable;

        /// <summary>
        /// Gets the view model used in the editor of this asset. This property is null if the asset is not opened in an editor.
        /// </summary>
        public IAssetEditorViewModel Editor
        {
            get => editor;
            internal set
            {
                SetValueUncancellable(ref editor, value, () =>
                {
                    if (value != null)
                        editorInitialized.SetResult(1);
                    else
                        editorInitialized = new TaskCompletionSource<int>();
                    Session?.UpdateSessionState();
                });
            }
        }

        /// <summary>
        /// Gets whether this asset can be opened in an editor.
        /// </summary>
        public bool HasEditor => IsEditable && ServiceProvider.Get<IAssetsPluginService>().HasEditorView(Session, AssetType);

        /// <summary>
        /// Gets a task that completes when the editor is initialized and is reset when the editor is disposed.
        /// </summary>
        public Task EditorInitialized => editorInitialized.Task;

        /// <summary>
        /// Gets the type of this asset.
        /// </summary>
        [NotNull]
        public Type AssetType => AssetItem.Asset.GetType();

        /// <summary>
        /// Gets the <see cref="AssetItem"/> object.
        /// </summary>
        public AssetItem AssetItem { get => assetItem; private set => SetValueUncancellable(ref assetItem, value); }

        /// <summary>
        /// Gets the asset object related to this view model.
        /// </summary>
        [NotNull]
        public Asset Asset => AssetItem.Asset;

        public AssetPropertyGraph PropertyGraph { get; }

        /// <summary>
        /// Gets the <see cref="ThumbnailData"/> associated to this <see cref="AssetViewModel"/>.
        /// </summary>
        public ThumbnailData ThumbnailData { get => thumbnailData; private set => SetValueUncancellable(ref thumbnailData, value); }

        /// <summary>
        /// Gets the display name of the type of this asset.
        /// </summary>
        public override string TypeDisplayName { get { var desc = DisplayAttribute.GetDisplay(AssetType); return desc != null ? desc.Name : AssetType.Name; } }

        /// <summary>
        /// Gets the dependencies of this asset.
        /// </summary>
        public AssetDependenciesViewModel Dependencies { get; }

        /// <summary>
        /// Gets the view model of the sources of this asset.
        /// </summary>
        public AssetSourcesViewModel Sources { get; }

        /// <summary>
        /// Gets whether the properties of this asset can be edited.
        /// </summary>
        public override bool IsEditable => Directory?.Package?.IsEditable ?? false;

        /// <summary>
        /// Gets whether this asset is locked. A locked asset cannot be moved, renamed, nor deleted.
        /// </summary>
        public virtual bool IsLocked => !Directory.Package.IsEditable;

        /// <summary>
        /// Gets whether this asset has been upgraded while being loaded.
        /// </summary>
        public bool HasBeenUpgraded { get; }

        public bool CanDerive => AssetType.GetCustomAttribute<AssetDescriptionAttribute>()?.AllowArchetype ?? false;

        public IReadOnlyObservableCollection<MenuCommandInfo> AssetCommands => assetCommands;

        public override IEnumerable<IDirtiable> Dirtiables => Directory != null ? base.Dirtiables.Concat(Directory.Dirtiables) : base.Dirtiables;

        /// <inheritdoc/>
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(AssetViewModel));
            Cleanup();
            base.Destroy();
        }

        /// <summary>
        /// Initializes this asset. This method is guaranteed to be called once every other assets are loaded in the session.
        /// </summary>
        /// <remarks>
        /// Inheriting classes should override it when necessary, provided that they also call the base implementation.
        /// </remarks>
        protected internal virtual void Initialize()
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                PropertyGraph?.Initialize();
                UndoRedoService.SetName(transaction, $"Reconcile {Url} with its archetypes");
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{{{GetType().Name}: {Url}}}";
        }

        /// <summary>
        /// Moves this asset in a different directory of a different project.
        /// </summary>
        /// <param name="newPackage">The target project.</param>
        /// <param name="newDirectory">The view model of the target directory.</param>
        /// <returns></returns>
        public bool MoveAsset(Package newPackage, [NotNull] DirectoryBaseViewModel newDirectory)
        {
            if (!newDirectory.Package.Match(newPackage)) throw new ArgumentException("The given directory is not contained in the given package.");

            using (var transaction = UndoRedoService.CreateTransaction())
            {
                string previousDirectory = directory.Path;
                var result = UpdateUrl(newPackage, newDirectory, Name);
                UndoRedoService.SetName(transaction, $"Move asset '{Name}' from '{previousDirectory}' to '{newDirectory.Path}'");
                return result;
            }
        }

        [NotNull]
        public static HashSet<AssetViewModel> ComputeRecursiveReferencerAssets([NotNull] IEnumerable<AssetViewModel> assets)
        {
            var result = new HashSet<AssetViewModel>(assets.SelectMany(x => x.Dependencies.RecursiveReferencerAssets));
            return result;
        }

        [NotNull]
        public static HashSet<AssetViewModel> ComputeRecursiveReferencedAssets([NotNull] IEnumerable<AssetViewModel> assets)
        {
            var result = new HashSet<AssetViewModel>(assets.SelectMany(x => x.Dependencies.RecursiveReferencedAssets));
            return result;
        }

        /// <summary>
        /// Attempts to find the closest location to create an asset of the given type, if the given location does not accept it.
        /// </summary>
        /// <param name="assetType">The type of asset to check</param>
        /// <param name="initialLocation">The initial location where to create the asset.</param>
        /// <returns>A <see cref="DirectoryBaseViewModel"/> corresponding to a valid location to create the asset, if available. <c>Null</c> otherwise.</returns>
        [CanBeNull]
        public static DirectoryBaseViewModel FindValidCreationLocation(Type assetType, [NotNull] DirectoryBaseViewModel initialLocation, PackageViewModel currentPackage = null)
        {
            if (!AssetRegistry.IsAssetType(assetType)) throw new ArgumentException(@"The given type is not an asset type", nameof(AssetType));
            // If the mount point of the current folder does not support this type of asset, try to select the first mount point that support it.

            if (initialLocation.Root.AcceptAssetType(assetType) && initialLocation.Root.Package.IsEditable)
                return initialLocation;

            if (currentPackage != null && currentPackage.AssetMountPoint.AcceptAssetType(assetType) && currentPackage.AssetMountPoint.Package.IsEditable)
                return currentPackage.AssetMountPoint;

            return initialLocation.Root.Package.MountPoints.FirstOrDefault(x => x.AcceptAssetType(assetType) && x.Package.IsEditable);
        }

        internal void SetThumbnailData(ThumbnailData data)
        {
            ClearThumbnail();
            ThumbnailData = data;
        }

        internal void ClearThumbnail()
        {
            ThumbnailData = null;
        }

        public bool CanDelete()
        {
            string message;
            return CanDelete(out message);
        }

        public virtual bool CanDelete(out string message)
        {
            if (IsLocked)
            {
                message = "This asset cannot be deleted.";
                return false;
            }
            message = null;
            return true;
        }

        protected internal virtual Task UpdateAssetFromSource(Logger logger)
        {
            // Do nothing by default
            return Task.CompletedTask;
        }

        protected override void OnDirtyFlagSet()
        {
            // We write the dirty flag of the asset item even if it has not changed,
            // since it triggers some processes we want to do at each modification.
            assetItem.IsDirty = IsDirty;
        }

        [Obsolete]
        protected virtual void OnAssetPropertyChanged(string propertyName, IGraphNode node, NodeIndex index, object oldValue, object newValue)
        {
            clearArchetypeCommand.IsEnabled = Asset.Archetype != null;
        }

        protected virtual IObjectNode GetPropertiesRootNode()
        {
            return AssetRootNode;
        }

        [NotNull]
        protected virtual GraphNodePath GetPathToPropertiesRootNode()
        {
            return new GraphNodePath(AssetRootNode);
        }

        protected virtual bool ShouldConstructPropertyMember([NotNull] IMemberNode member) => true;

        protected virtual bool ShouldConstructPropertyItem([NotNull] IObjectNode collection, NodeIndex index) => true;

        protected virtual bool ShouldListenToTargetNode(IMemberNode member, IGraphNode targetNode) => true;

        protected internal virtual void OnSessionSaved()
        {
            // Do nothing by default.
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            PropertyGraph.Dispose();
        }

        private void BaseContentChanged(INodeChangeEventArgs e, IGraphNode node)
        {
            // Ignore base change if we are fixing up assets.
            if (Session.IsInFixupAssetContext)
                return;

            if (!UndoRedoService.UndoRedoInProgress)
            {
                // Ensure this asset will be marked as dirty
                UndoRedoService.PushOperation(new EmptyDirtyingOperation(Dirtiables));
            }
        }

        private void AssetPropertyChanged(object sender, INodeChangeEventArgs e)
        {
            // Ignore asset property change if we are fixing up assets.
            if (Session.IsInFixupAssetContext)
                return;

            var index = (e as ItemChangeEventArgs)?.Index ?? NodeIndex.Empty;
            var assetNodeChange = (IAssetNodeChangeEventArgs)e;
            var node = (IAssetNode)e.Node;
            var memberName = (node as IMemberNode)?.Name;
            if (!UndoRedoService.UndoRedoInProgress)
            {
                // Don't create action items if the change comes from the Base
                if (!PropertyGraph.UpdatingPropertyFromBase)
                {
                    var overrideChange = new AssetContentValueChangeOperation(node, e.ChangeType, index, e.OldValue, e.NewValue, assetNodeChange.PreviousOverride, assetNodeChange.NewOverride, assetNodeChange.ItemId, Dirtiables);
                    UndoRedoService.PushOperation(overrideChange);
                }
            }

            OnAssetPropertyChanged(memberName, node, index, e.OldValue, e.NewValue);
        }

        private void Rename(string newName)
        {
            string error;
            if (!IsNewNameValid(newName, out error))
            {
                ServiceProvider.Get<IDialogService>().BlockingMessageBox(error, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (newName == name)
                return;

            using (var transaction = UndoRedoService.CreateTransaction())
            {
                string previousName = name;
                UpdateUrl(package, directory, newName);
                UndoRedoService.SetName(transaction, $"Rename asset '{previousName}' to '{newName}'");
            }
        }

        private bool UpdateUrl(Package newPackage, DirectoryBaseViewModel newDirectory, string newName, bool updateParentDirectory = true)
        {
            if (updatingUrl)
                return true;

            updatingUrl = true;

            var newUrl = UFile.Combine(newDirectory.Path, newName);
            if (!UFile.IsValid(newUrl))
            {
                updatingUrl = false;
                return false;
            }

            bool urlChanged = newUrl != Url;
            bool nameChanged = newName != name;

            if (urlChanged)
                OnPropertyChanging(nameof(Url));
            if (nameChanged)
                OnPropertyChanging(nameof(Name));

            if (package != newPackage || urlChanged)
            {
                if (!UndoRedoService.UndoRedoInProgress && !nameChanged)
                {
                    UndoRedoService.PushOperation(
                        new AnonymousDirtyingOperation(this.Yield(),
                            () => UpdateUrl(package, Directory, name, false),
                            () => UpdateUrl(newPackage, newDirectory, newName, false)));
                }

                package.Assets.Remove(AssetItem);
                package = newPackage;

                var newAssetItem = new AssetItem(newUrl, AssetItem.Asset) { SourceFolder = AssetItem.SourceFolder, AlternativePath = AssetItem.AlternativePath };
                AssetItem = newAssetItem;
                package.Assets.Add(AssetItem);
            }

            // updateParentDirectory is a protection against double update because of the post of the whole UpdateUrl function as an undoable operation on the transactions stack.
            // The first time, we need to execute this part, to in fact change the directory; but later on, the undo/redo system will 'set' the property itself.
            if (updateParentDirectory && Directory != newDirectory)
            {
                Directory.RemoveAsset(this);
                Directory = newDirectory;
                Directory.AddAsset(this, true);
            }

            name = newName;

            if (nameChanged)
                OnPropertyChanged(nameof(Name));

            if (urlChanged)
            {
                // Update all asset references
                var assetReferenceAnalysis = new PackageSessionAnalysis(package.Session, new PackageAnalysisParameters
                {
                    // TODO: Check view model dirty flag after such an operation! Are we up-to-date?
                    IsProcessingAssetReferences = true, SetDirtyFlagOnAssetWhenFixingUFile = true, IsLoggingAssetNotFoundAsError = true,
                });
                var log = assetReferenceAnalysis.Run();
                // TODO: what should we do with this log?
                //log.CopyTo(Directory.Package.Session.AssetLog);

                OnPropertyChanged(nameof(Url));
            }
            updatingUrl = false;

            if (Session.ActiveAssetView.SelectedAssets.Contains(this))
            {
                if (Session.ActiveAssetView.SelectedAssets.Count == 1)
                {
                    // Refresh the preview so the built asset(s) are updated regarding to the url change
                    var previewService = ServiceProvider.TryGet<IAssetPreviewService>();
                    previewService?.SetAssetToPreview(this);
                }
            }
            return true;
        }

        protected override void UpdateIsDeletedStatus()
        {
            if (IsDeleted)
            {
                package.Assets.Remove(AssetItem);
                Session.UnregisterAsset(this);
                Directory.Package.DeletedAssetsList.Add(this);
                if (PropertyGraph != null)
                {
                    Session.GraphContainer.UnregisterGraph(Id);
                }
            }
            else
            {
                package.Assets.Add(AssetItem);
                Session.RegisterAsset(this);
                Directory.Package.DeletedAssetsList.Remove(this);
                if (!Initializing && PropertyGraph != null)
                {
                    Session.GraphContainer.RegisterGraph(PropertyGraph);
                }
            }
            AssetItem.IsDeleted = IsDeleted;
            Session.SourceTracker?.UpdateAssetStatus(this);
        }

        private bool IsNewNameValid(string newName, out string error)
        {
            if (Directory.Assets.Any(x => string.Equals(x.Name, newName, StringComparison.InvariantCultureIgnoreCase) && x != this))
            {
                error = string.Format(Tr._p("Message", "Unable to rename asset to '{0}' because an asset with the same name exists in the same directory"), newName);
                return false;
            }
            if (string.IsNullOrWhiteSpace(newName))
            {
                error = Tr._p("Message", "Unable to rename asset with an empty name");
                return false;
            }
            error = null;
            return true;
        }

        private void TagsCollectionChanged(object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                throw new InvalidOperationException("Reset is not supported on the tag collection.");
            }
            if (e.OldItems != null)
            {
                foreach (string oldItem in e.OldItems)
                {
                    assetItem.Asset.Tags.Remove(oldItem);
                }
            }
            if (e.NewItems != null)
            {
                foreach (string newItem in e.NewItems)
                {
                    assetItem.Asset.Tags.Add(newItem);
                }
            }
        }

        private void CreateDerivedAsset()
        {
            if (CanDerive)
            {
                var targetDirectory = FindValidCreationLocation(assetItem.Asset.GetType(), directory, Session.CurrentProject);

                if (targetDirectory == null)
                    return;

                var childName = NamingHelper.ComputeNewName(Name + "-Derived", targetDirectory.Assets, x => x.Name);
                var childUrl = UFile.Combine(targetDirectory.Path, childName);
                var childAsset = assetItem.CreateDerivedAsset();
                var childAssetItem = new AssetItem(childUrl, childAsset);
                targetDirectory.Package.CreateAsset(targetDirectory, childAssetItem, true, null);
            }
        }

        private void ClearArchetype()
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                // Clear the actual base
                PropertyGraph.RootNode[nameof(Asset.Archetype)].Update(null);

                // Remove all overridden properties
                var clearedOverrides = PropertyGraph.ClearAllOverrides();
                if (!UndoRedoService.UndoRedoInProgress)
                {
                    UndoRedoService.PushOperation(new AnonymousDirtyingOperation(Dirtiables, () => RestoreArchetype(clearedOverrides), ClearArchetype));
                }

                UndoRedoService.SetName(transaction, "Clear archetype");
            }

            // Force refreshing the property grid
            Session.AssetViewProperties.RefreshSelectedPropertiesAsync().Forget();
        }

        private void RestoreArchetype([NotNull] List<AssetPropertyGraph.NodeOverride> clearedOverrides)
        {
            AssetViewModel baseViewModel = null;
            if (Asset.Archetype != null)
            {
                baseViewModel = Session.GetAssetById(Asset.Archetype.Id);
                if (baseViewModel == null)
                    throw new InvalidOperationException($"Unable to find the base [{Asset.Archetype.Location}] of asset [{Url}].");
            }

            // Restore all overrides
            PropertyGraph?.RestoreOverrides(clearedOverrides, baseViewModel?.PropertyGraph);

            // Refresh the base to ensure everything is clean
            PropertyGraph?.RefreshBase();

            // Reconcile with base. This should not do anything!
            PropertyGraph?.ReconcileWithBase();

            // Force refreshing the property grid
            Session.AssetViewProperties.RefreshSelectedPropertiesAsync().Forget();
        }

        IChildViewModel IChildViewModel.GetParent()
        {
            return Directory;
        }

        string IChildViewModel.GetName()
        {
            return Name;
        }

        IObjectNode IPropertyProviderViewModel.GetRootNode()
        {
            return GetPropertiesRootNode();
        }

        GraphNodePath IAssetPropertyProviderViewModel.GetAbsolutePathToRootNode()
        {
            return GetPathToPropertiesRootNode();
        }

        bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => ShouldConstructPropertyMember(member);

        bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => ShouldConstructPropertyItem(collection, index);

        AssetViewModel IAssetPropertyProviderViewModel.RelatedAsset => this;

        public void PrepareSave(ILogger logger)
        {
            PropertyGraph?.PrepareForSave(logger, assetItem);
        }
    }
}
