// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Diagnostics;
using Xenko.Core.Assets.Editor.Components.Properties;
using Xenko.Core.Assets.Editor.Components.TemplateDescriptions;
using Xenko.Core.Assets.Editor.Extensions;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel.Logs;
using Xenko.Core.Assets.Editor.ViewModel.Progress;
using Xenko.Core.Assets.Templates;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.Core.Presentation.Collections;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Dirtiables;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Core.Quantum;
using Xenko.Core.Quantum.References;
using Xenko.Core.Translation;

namespace Xenko.Core.Assets.Editor.ViewModel
{
    public class PackageViewModel : SessionObjectViewModel, IComparable<PackageViewModel>, IAddChildViewModel, IChildViewModel, IPropertyProviderViewModel
    {
        internal readonly ObservableList<AssetViewModel> DeletedAssetsList = new ObservableList<AssetViewModel>();
        private readonly PackageSettingsWrapper packageSettingsWrapper = new PackageSettingsWrapper();
        // TODO: When anything become renamable in the content of the package, this must be turn into an auto-updating sorted collection
        protected readonly SortedObservableCollection<DirtiableEditableViewModel> content = new SortedObservableCollection<DirtiableEditableViewModel>(ComparePackageContent);
        private readonly List<AssetViewModel> deletedAssetsSinceLastSave = new List<AssetViewModel>();

        public PackageViewModel(SessionViewModel session, PackageContainer packageContainer, bool packageAlreadyInSession)
            : base(session)
        {
            PackageContainer = packageContainer;
            Package = PackageContainer.Package;
            HasBeenUpgraded = Package.IsDirty;
            DependentProperties.Add(nameof(PackagePath), new[] { nameof(Name), nameof(RootDirectory) });
            Dependencies = new DependencyCategoryViewModel(nameof(Dependencies), this, session, Package.RootAssets);
            AssetMountPoint = new AssetMountPointViewModel(this);
            content.Add(AssetMountPoint);
            content.Add(Dependencies);
            RenameCommand = new AnonymousCommand(ServiceProvider, () => IsEditing = true);
            IsLoaded = Package.State >= PackageState.AssetsReady;

            // IsDeleted will make the package added to Session.LocalPackages, so let's do it last
            InitialUndelete(!packageAlreadyInSession);

            DeletedAssets.CollectionChanged += DeletedAssetsChanged;
        }

        /// <summary>
        /// Gets or sets the name of this package.
        /// </summary>
        /// <remarks>Modifying this property also modify the <see cref="PackagePath"/> property if the package has already been saved once.</remarks>
        public override string Name { get { return PackagePath.GetFileNameWithoutExtension(); } set { Rename(value); } }

        public bool IsLoaded { get; }

        public PackageContainer PackageContainer { get; }

        /// <summary>
        /// Gets the underlying <see cref="Package"/> used as a model for this view.
        /// </summary>
        public Package Package { get; }

        /// <summary>
        /// Gets or sets the path of this package.
        /// </summary>
        /// <remarks>Modifying this property also modify the <see cref="Name"/> property.</remarks>
        public UFile PackagePath { get { return Package.FullPath; } set { SetValue(() => Package.FullPath = value); } }

        public UDirectory RootDirectory => Package.RootDirectory;

        /// <summary>
        /// Gets all assets contained in this package.
        /// </summary>
        public IEnumerable<AssetViewModel> Assets
        {
            get
            {
                return MountPoints.SelectMany(x => x.GetDirectoryHierarchy().SelectMany(y => y.Assets));
            }
        }

        /// <summary>
        /// Gets all assets contained in this package, and the asset contained in packages referenced by this package.
        /// </summary>
        // TODO: we MUST guarantee that whatever the user do, the assets in this enumeration all have DIFFERENT urls!
        public IEnumerable<AssetViewModel> AllAssets { get { return Assets.Concat(Dependencies.Content.Select(x => x.Target).NotNull().SelectMany(x => x.Assets)); } }

        /// <summary>
        /// Gets the properties of the package.
        /// </summary>
        public SessionObjectPropertiesViewModel Properties => Session.AssetViewProperties;

        /// <summary>
        /// Gets whether this package is editable.
        /// </summary>
        public override bool IsEditable => !Package.IsSystem && IsLoaded;

        /// <inheritdoc/>
        public override bool IsEditing { get { return false; } set { base.IsEditing = value; } }

        /// <summary>
        /// Gets the root directory of this package.
        /// </summary>
        public AssetMountPointViewModel AssetMountPoint { get; }

        /// <summary>
        /// Gets all the mount points in this package.
        /// </summary>
        public IEnumerable<MountPointViewModel> MountPoints => Content.OfType<MountPointViewModel>();

        /// <summary>
        /// Gets the command that initiates the renaming of this package.
        /// </summary>
        [NotNull]
        public ICommandBase RenameCommand { get; }

        /// <summary>
        /// Gets the list of child item to be used to display in a hierachical view.
        /// </summary>
        /// <remarks>This collection usually contains categories and root folders.</remarks>
        public IReadOnlyObservableCollection<DirtiableEditableViewModel> Content => content;

        /// <summary>
        /// Gets the container category for dependencies referenced in this package.
        /// </summary>
        public DependencyCategoryViewModel Dependencies { get; }

        /// <summary>
        /// Gets the collection of root assets for this package.
        /// </summary>
        public ObservableSet<AssetViewModel> RootAssets { get; } = new ObservableSet<AssetViewModel>();

        /// <summary>
        /// Gets the <see cref="PackageUserSettings"/> of this package.
        /// </summary>
        public PackageUserSettings UserSettings => Package.UserSettings;

        /// <summary>
        /// Gets whether this package has been upgraded while being loaded.
        /// </summary>
        public bool HasBeenUpgraded { get; private set; }

        /// <summary>
        /// Gets the list of assets that have been deleted by the user since the beginning of the session.
        /// </summary>
        public IReadOnlyObservableList<AssetViewModel> DeletedAssets => DeletedAssetsList;

        // TODO: Might want to hide this
        public List<PackageLoadedAssembly> LoadedAssemblies => Package.LoadedAssemblies;

        public override string TypeDisplayName => "Package";

        /// <inheritdoc/>
        public override IEnumerable<IDirtiable> Dirtiables => base.Dirtiables.Concat(Session.Dirtiables);

        bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

        /// <inheritdoc/>
        public int CompareTo(PackageViewModel other)
        {
            return other != null ? string.Compare(Name, other.Name, StringComparison.InvariantCultureIgnoreCase) : -1;
        }

        /// <summary>
        /// Indicates whether the given asset in within the scope of this package, either by being part of this package or part of
        /// one of its dependencies.
        /// </summary>
        /// <param name="asset">The asset for which to check if it's in the scope of this package</param>
        /// <returns><c>True</c> if the asset is in scope, <c>False</c> otherwise.</returns>
        public bool IsInScope(AssetViewModel asset)
        {
            var assetPackage = asset.Directory.Package;
            // Note: Would be better to switch to Dependencies view model as soon as we have FlattenedDependencies in those
            return assetPackage == this || Package.Container.FlattenedDependencies.Any(x => x.Package == assetPackage.Package);
        }

        /// <summary>
        /// Creates the view models for each asset, directory, profile, project and reference of this package.
        /// </summary>
        /// <param name="loggerResult">The logger result of the current operation.</param>
        /// <param name="workProgress">A <see cref="WorkProgressViewModel"/> instance to update on progresses. Can be <c>null</c>.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the load process. Can be <c>null</c>.</param>
        internal void LoadPackageInformation(LoggerResult loggerResult, WorkProgressViewModel workProgress, CancellationToken? cancellationToken = null)
        {
            if (workProgress == null) throw new ArgumentNullException(nameof(workProgress));
            var progress = workProgress.ProgressValue;
            workProgress.UpdateProgressAsync($"Processing asset {progress + 1}/{workProgress.Maximum}...", progress);

            if (cancellationToken.HasValue && cancellationToken.Value.IsCancellationRequested)
                return;

            foreach (var dependency in Package.Container.DirectDependencies)
            {
                new DirectDependencyReferenceViewModel(dependency, this, Dependencies, false);
            }

            foreach (var asset in Package.Assets.ToList())
            {
                if (cancellationToken.HasValue && cancellationToken.Value.IsCancellationRequested)
                    return;

                var message = $"Processing asset {progress + 1}/{workProgress.Maximum}...";
                workProgress.UpdateProgressAsync(message, progress);

                var url = asset.Location;
                DirectoryBaseViewModel directory;
                var projectSourceCodeAsset = asset.Asset as IProjectAsset;
                // TODO CSPROJ=XKPKG override rather than cast to subclass
                if (projectSourceCodeAsset != null && this is ProjectViewModel project)
                {
                    directory = project.GetOrCreateProjectDirectory(url.GetFullDirectory() ?? "", false);
                }
                else
                {
                    directory = GetOrCreateAssetDirectory(url.GetFullDirectory() ?? "", false);
                }
                CreateAsset(directory, asset, false, loggerResult, true);
                ++progress;
            }

            FillRootAssetCollection();

            workProgress.UpdateProgressAsync("Package processed", progress);

            foreach (var explicitDirectory in Package.ExplicitFolders)
            {
                GetOrCreateAssetDirectory(explicitDirectory, false);
            }

            var pluginService = Session.ServiceProvider.Get<IAssetsPluginService>();
            foreach (var plugin in pluginService.Plugins)
            {
                foreach (var property in plugin.ProfileSettings)
                {
                    RegisterSettings(property);
                }
            }
        }

        private void RegisterSettings(PackageSettingsEntry settings)
        {
            if ((settings.TargetPackage & TargetPackage.Executable) == TargetPackage.Executable)
            {
                packageSettingsWrapper.ExecutableUserSettings.Add(settings.SettingsKey.DisplayName, PackageSettingsWrapper.SettingsKeyWrapper.Create(settings.SettingsKey, Package.UserSettings.Profile));
            }
            if ((settings.TargetPackage & TargetPackage.NonExecutable) == TargetPackage.NonExecutable)
            {
                packageSettingsWrapper.NonExecutableUserSettings.Add(settings.SettingsKey.DisplayName, PackageSettingsWrapper.SettingsKeyWrapper.Create(settings.SettingsKey, Package.UserSettings.Profile));
            }
        }

        private void FillRootAssetCollection()
        {
            RootAssets.Clear();
            RootAssets.AddRange(Package.RootAssets.Select(x => Session.GetAssetById(x.Id)));
            foreach (var dependency in PackageContainer.FlattenedDependencies)
            {
                if (dependency.Package != null)
                    RootAssets.AddRange(dependency.Package.RootAssets.Select(x => Session.GetAssetById(x.Id)));
            }
            RegisterMemberCollectionForActionStack(nameof(RootAssets), RootAssets);
            RootAssets.CollectionChanged += RootAssetsCollectionChanged;
        }

        private void RootAssetsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Replicate changes
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var newItem in e.NewItems.Cast<AssetViewModel>())
                    {
                        // Shouldn't happen, but make sure an item with same id is not already added
                        Package.RootAssets.Remove(newItem.Id);
                        Package.RootAssets.Add(new AssetReference(newItem.Id, newItem.Url));
                        Package.IsDirty = true;
                        newItem.Dependencies.NotifyRootAssetChange(true);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var oldItem in e.OldItems.Cast<AssetViewModel>())
                    {
                        Package.RootAssets.Remove(oldItem.Id);
                        Package.IsDirty = true;
                        oldItem.Dependencies.NotifyRootAssetChange(true);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Reset:
                    // Let's do a full resync
                    Package.RootAssets.Clear();
                    Package.RootAssets.AddRange(RootAssets.Select(x => new AssetReference(x.Id, x.Url)));
                    AllAssets.ForEach(x =>
                    {
                        x.Dependencies.NotifyRootAssetChange(false);
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Session.SelectionIsRoot = Session.ActiveAssetView.SelectedAssets.All(x => x.Dependencies.IsRoot);
        }

        public AssetViewModel CreateAsset(DirectoryBaseViewModel directory, AssetItem assetItem, bool canUndoRedoCreation, LoggerResult loggerResult)
        {
            return CreateAsset(directory, assetItem, canUndoRedoCreation, loggerResult, false);
        }

        private AssetViewModel CreateAsset(DirectoryBaseViewModel directory, AssetItem assetItem, bool canUndoRedoCreation, LoggerResult loggerResult, bool isLoading)
        {
            if (directory == null)
                throw new ArgumentNullException(nameof(directory));
            if (assetItem == null)
                throw new ArgumentNullException(nameof(assetItem));

            AssetCollectionItemIdHelper.GenerateMissingItemIds(assetItem.Asset);
            var parameters = new AssetViewModelConstructionParameters(ServiceProvider, directory, Package, assetItem, directory.Session.AssetNodeContainer, canUndoRedoCreation);
            Session.GraphContainer.InitializeAsset(assetItem, loggerResult);
            var assetType = assetItem.Asset.GetType();
            var assetViewModelType = typeof(AssetViewModel<>);
            while (assetType != null)
            {
                if (Session.AssetViewModelTypes.TryGetValue(assetType, out assetViewModelType))
                    break;

                assetViewModelType = typeof(AssetViewModel<>);
                assetType = assetType.BaseType;
            }
            if (assetViewModelType.IsGenericType)
            {
                assetViewModelType = assetViewModelType.MakeGenericType(assetItem.Asset.GetType());
            }
            var asset = (AssetViewModel)Activator.CreateInstance(assetViewModelType, parameters);
            if (!isLoading)
            {
                asset.Initialize();
            }
            return asset;
        }

        public bool Match(Package package)
        {
            return Package == package;
        }

        /// <summary>
        /// Checks whether this package has a direct or indirect dependency on the given other package.
        /// </summary>
        /// <param name="otherPackage">The target package of the dependency check.</param>
        /// <returns><c>true</c> if this package depends on the given package, <c>false</c> otherwise.</returns>
        public bool DependsOn(PackageViewModel otherPackage)
        {
            var visitedPackages = new List<PackageViewModel>();
            return CheckDependsOnRecursively(this, otherPackage, visitedPackages);
        }

        private static bool CheckDependsOnRecursively(PackageViewModel source, PackageViewModel target, List<PackageViewModel> visitedPackages)
        {
            visitedPackages.Add(source);
            return source.Dependencies.Content.Any(x => x.Target == target) || source.Dependencies.Content.Select(x => x.Target).NotNull().Where(x => !visitedPackages.Contains(x)).Any(x => CheckDependsOnRecursively(x, target, visitedPackages));
        }

        public void MoveAsset(AssetViewModel asset, DirectoryBaseViewModel directory)
        {
            asset.MoveAsset(Package, directory);
        }

        public void Delete()
        {
            if (Package.IsSystem)
            {
                // Note: this should never happen (see comments in method SessionViewModel.DeleteSelectedSolutionItems)
                throw new InvalidOperationException("System packages cannot be deleted.");
            }
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                string message = $"Delete package '{Name}'";
                IsDeleted = true;
                UndoRedoService.SetName(transaction, message);
            }
        }

        public void CheckConsistency()
        {
            var assetList = Package.Assets.Where(item => item.Asset.GetType().GetCustomAttribute<AssetDescriptionAttribute>()?.Referenceable ?? true).ToDictionary(x => x.Location.FullPath);
            var assetViewModels = Assets.ToList();
            var logger = Session.AssetLog.GetLogger(LogKey.Get("Consistency"));

            foreach (var asset in assetViewModels)
            {
                if (!assetList.ContainsKey(asset.Url))
                {
                    logger.Log(new AssetSerializableLogMessage(asset.Id, asset.Url, LogMessageType.Fatal, $"The asset {asset.Url} is missing or incorrectly indexed in the package. Please report this issue."));
                }
                else
                {
                    assetList.Remove(asset.Url);
                }
            }

            foreach (var asset in assetList.Values)
            {
                logger.Log(new AssetSerializableLogMessage(asset.Id, asset.Location, LogMessageType.Fatal, $"The asset {asset.Location} is incorrectly indexed in the package. Please report this issue."));
            }
        }

        public async Task UpdatePackageTemplate(TemplateDescription template)
        {
            var loggerResult = new LoggerResult();

            var workProgress = new WorkProgressViewModel(ServiceProvider, loggerResult)
            {
                Title = "Updating package...",
                KeepOpen = KeepOpen.OnWarningsOrErrors,
                IsIndeterminate = true,
                IsCancellable = false
            };
            workProgress.RegisterProgressStatus(loggerResult, true);

            var parameters = new PackageTemplateGeneratorParameters
            {
                Name = Package.Meta.Name,
                OutputDirectory = Package.FullPath.GetFullDirectory(),
                Description = template,
                Package = Package,
                Logger = loggerResult,
            };

            var generator = TemplateManager.FindTemplateGenerator(parameters);
            if (generator == null)
            {
                await ServiceProvider.Get<IDialogService>().MessageBox(Tr._p("Message", "Unable to retrieve template generator for the selected template. Aborting."), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Errors might occur when generating the template. For the moment we consider them non-fatal and allow to open the project anyway.
            await TemplateGeneratorHelper.RunTemplateGeneratorSafe(generator, parameters, workProgress);

            Session.ProcessAddedProjects(loggerResult, workProgress, true);
            Session.ProcessRemovedProjects();

            await workProgress.NotifyWorkFinished(false, loggerResult.HasErrors);
        }

        public async Task AddExistingProject()
        {
            var fileDialog = ServiceProvider.Get<IEditorDialogService>().CreateFileOpenModalDialog();
            fileDialog.Filters.Add(new FileDialogFilter("Visual Studio C# project", "csproj"));
            fileDialog.InitialDirectory = Session.SolutionPath;
            var result = await fileDialog.ShowModal();

            var projectPath = fileDialog.FilePaths.FirstOrDefault();
            if (result == DialogResult.Ok && projectPath != null)
            {
                var loggerResult = new LoggerResult();
                var cancellationSource = new CancellationTokenSource();
                var workProgress = new WorkProgressViewModel(ServiceProvider, loggerResult)
                {
                    Title = "Importing package...",
                    KeepOpen = KeepOpen.OnWarningsOrErrors,
                    IsIndeterminate = true,
                    IsCancellable = false
                };

                using (var transaction = UndoRedoService.CreateTransaction())
                {
                    workProgress.RegisterProgressStatus(loggerResult, true);

                    ServiceProvider.Get<IEditorDialogService>().ShowProgressWindow(workProgress, 500);

                	await Task.Run(() =>
                    {
                        try
                        {
                            Package.AddExistingProject(projectPath, loggerResult);
                        }
                        catch (Exception e)
                        {
                            loggerResult.Error("An exception occurred while importing the project", e);
                        }

                    }, cancellationSource.Token);

                    RefreshPackageReferences();

                    UndoRedoService.SetName(transaction, $"Import project '{new UFile(projectPath).GetFileNameWithoutExtension()}'");
                }

                // Notify that the task is finished
                await workProgress.NotifyWorkFinished(cancellationSource.IsCancellationRequested, loggerResult.HasErrors);
            }
        }

        public void AddDependency(PackageViewModel packageViewModel)
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                DependencyRange dependency;
                if (packageViewModel.Package.Container is SolutionProject project)
                {
                    dependency = new DependencyRange(packageViewModel.Name, new PackageVersionRange(packageViewModel.Package.Meta.Version, true), DependencyType.Project)
                    {
                        MSBuildProject = project.FullPath,
                    };
                }
                else
                {
                    dependency = new DependencyRange(packageViewModel.Name, new PackageVersionRange(packageViewModel.Package.Meta.Version, true), DependencyType.Package);
                }
                var reference = new DirectDependencyReferenceViewModel(dependency, this, Dependencies, true);
                UndoRedoService.SetName(transaction, $"Add dependency to package '{reference.Name}'");
            }
        }

        public List<AssetViewModel> PasteAssets(List<AssetItem> assets, [CanBeNull] ProjectViewModel project)
        {
            var viewModels = new List<AssetViewModel>();

            // Don't touch the action stack in this case.
            if (assets.Count == 0)
                return viewModels;

            var fixedAssets = new List<AssetItem>();

            using (var transaction = UndoRedoService.CreateTransaction())
            {
                // Clean collision by renaming pasted asset if an asset with the same name already exists in that location.
                AssetCollision.Clean(null, assets, fixedAssets, AssetResolver.FromPackage(Package), false, false);

                // Temporarily add the new asset to the package
                fixedAssets.ForEach(x => Package.Assets.Add(x));

                // Find which assets are referencing the pasted assets in order to fix the reference link.
                var assetsToFix = GetReferencers(Session.DependencyManager, Session, fixedAssets);

                // Remove temporarily added assets - they will be properly re-added with the correct action stack entry when creating the view model
                fixedAssets.ForEach(x => Package.Assets.Remove(x));

                // Create directories and view models, actually add assets to package.
                foreach (var asset in fixedAssets)
                {
                    var location = asset.Location.GetFullDirectory() ?? "";
                    var assetDirectory = project == null ?
                        GetOrCreateAssetDirectory(location, true) :
                        project.GetOrCreateProjectDirectory(location, true);
                    var assetViewModel = CreateAsset(assetDirectory, asset, true, null);
                    viewModels.Add(assetViewModel);
                }

                // Fix references in the assets that references what we pasted.
                // We wrap this operation in an action item so the action stack can properly re-execute it.
                var fixReferencesAction = new FixAssetReferenceOperation(assetsToFix, false, true);
                fixReferencesAction.FixAssetReferences();
                UndoRedoService.PushOperation(fixReferencesAction);

                UndoRedoService.SetName(transaction, "Paste assets");
            }
            return viewModels;
        }

        internal virtual void OnSessionSaved()
        {
            deletedAssetsSinceLastSave.Clear();
        }

        // TODO: Move this in an utility class
        internal static List<AssetViewModel> GetReferencers(IAssetDependencyManager dependencyManager, SessionViewModel session, IEnumerable<AssetItem> assets)
        {
            var result = new List<AssetViewModel>();

            // Find which assets are referencing the pasted assets in order to fix the reference link.
            foreach (var asset in assets)
            {
                var referencers = dependencyManager.ComputeDependencies(asset.Id, AssetDependencySearchOptions.In, ContentLinkType.Reference);
                if (referencers != null)
                {
                    foreach (var referencerLink in referencers.LinksIn)
                    {
                        AssetViewModel assetViewModel = session.GetAssetById(referencerLink.Item.Id);
                        if (assetViewModel != null)
                        {
                            if (!result.Contains(assetViewModel))
                                result.Add(assetViewModel);
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Updates dirty flags on package and asset collection according to the actual dirty state of objects.
        /// </summary>
        internal void PreparePackageForSaving()
        {
            if (Content.Where(x => x != AssetMountPoint).Any(x => x.IsDirty))
                Package.IsDirty = true;

            if (deletedAssetsSinceLastSave.Count > 0)
                Package.Assets.IsDirty = true;

            Package.UserSettings.Save();
        }

        protected override void UpdateIsDeletedStatus()
        {
            var collection = Package.IsSystem ? Session.StorePackages : Session.LocalPackages;

            if (IsDeleted)
            {
                collection.Remove(this);
            }
            else
            {
                collection.Add(this);
            }
        }

        private void Rename(string newName)
        {
            string error;
            if (!IsValidName(newName, out error))
            {
                ServiceProvider.Get<IDialogService>().BlockingMessageBox(string.Format(Tr._p("Message", "This package couldn't be renamed. {0}"), error), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var newPath = UFile.Combine(PackagePath.GetFullDirectory(), newName + PackagePath.GetFileExtension());
            Rename(newPath);
        }

        private void Rename(UFile packagePath)
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                var previousPath = PackagePath;
                PackagePath = packagePath;

                if (previousPath != PackagePath)
                {
                    IsEditing = false;
                }
                UndoRedoService.SetName(transaction, $"Rename package '{previousPath.GetFileNameWithoutExtension()}' to '{packagePath.GetFileNameWithoutExtension()}'");
            }
        }

        protected override bool IsValidName(string value, out string error)
        {
            if (!base.IsValidName(value, out error))
            {
                return false;
            }

            if (Session.AllPackages.Any(x => x != this && string.Equals(x.Name, value, StringComparison.InvariantCultureIgnoreCase)))
            {
                error = Tr._p("Message", "A package with the same name already exists in the session.");
                return false;
            }

            return true;
        }

        private void DeletedAssetsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                deletedAssetsSinceLastSave.AddRange(e.NewItems.Cast<AssetViewModel>());
            }
            e.OldItems?.Cast<AssetViewModel>().ForEach(x => deletedAssetsSinceLastSave.Remove(x));
        }

        private bool RefreshPackageReferences()
        {
            // TODO CSPROJ=XKPKG
            return false;
        }

        /// <summary>
        /// Gets asset directory view model for a given path and creates all missing parts.
        /// </summary>
        /// <param name="assetDirectory">Asset directory path.</param>
        /// <param name="canUndoRedoCreation">True if register UndoRedo operation for missing path parts.</param>
        /// <returns>Given directory view model.</returns>
        [NotNull]
        public DirectoryBaseViewModel GetOrCreateAssetDirectory(string assetDirectory, bool canUndoRedoCreation)
        {
            return AssetMountPoint.GetOrCreateDirectory(assetDirectory, canUndoRedoCreation);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{{{GetType().Name}: {Name}}}";
        }

        bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            return ((IAddChildViewModel)AssetMountPoint).CanAddChildren(children, modifiers, out message);
        }

        void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
        {
            ((IAddChildViewModel)AssetMountPoint).AddChildren(children, modifiers);
        }

        IChildViewModel IChildViewModel.GetParent()
        {
            return Session.PackageCategories.Values.First(x => x.Content.Contains(this));
        }

        string IChildViewModel.GetName()
        {
            return Name;
        }

        IObjectNode IPropertyProviderViewModel.GetRootNode()
        {
            packageSettingsWrapper.HasExecutables = (this as ProjectViewModel)?.Type == ProjectType.Executable;
            return Session.AssetNodeContainer.GetOrCreateNode(packageSettingsWrapper);
        }

        bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => true;

        bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => true;

        private static int ComparePackageContent(DirtiableEditableViewModel x, DirtiableEditableViewModel y)
        {
            var xAssets = x as AssetMountPointViewModel;
            var yAssets = y as AssetMountPointViewModel;
            var xProject = x as ProjectViewModel;
            var yProject = y as ProjectViewModel;
            var xDependencies = x as DependencyCategoryViewModel;
            var yDependencies = y as DependencyCategoryViewModel;

            if (xAssets != null)
            {
                if (yAssets != null)
                    return string.Compare(xAssets.Name, yAssets.Name, StringComparison.InvariantCultureIgnoreCase);
                return -1;
            }
            if (xProject != null)
            {
                if (yProject != null)
                {
                    return xProject.CompareTo(yProject);
                }
                return yAssets != null ? 1 : -1;
            }
            if (xDependencies != null)
            {
                if (yDependencies != null)
                    throw new InvalidOperationException("A PackageViewModel cannot contain two isntances of DependencyCategoryViewModel");
                return 1;
            }
            throw new InvalidOperationException("Unable to sort the given items for the Content collection of PackageViewModel");
        }
    }
}
