// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Editor.Components.Properties;
using Xenko.Core.Assets.Editor.Components.Transactions;
using Xenko.Core.Assets.Editor.Components.TemplateDescriptions;
using Xenko.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.Settings;
using Xenko.Core.Assets.Editor.ViewModel.Logs;
using Xenko.Core.Assets.Editor.ViewModel.Progress;
using Xenko.Core.Assets.Quantum;
using Xenko.Core.Assets.Templates;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.Core.VisualStudio;
using Xenko.Core.Presentation.Collections;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Dirtiables;
using Xenko.Core.Presentation.Quantum.ViewModels;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Core.Presentation.Windows;
using Xenko.Core.Translation;

namespace Xenko.Core.Assets.Editor.ViewModel
{
    public class SessionViewModel : DirtiableEditableViewModel, IAssetFinder
    {
        public static string StorePackageCategoryName = Tr._("External packages");
        public static string LocalPackageCategoryName = Tr._("Local packages");
        public const string SolutionExtension = ".sln";
        public const int SaveIrreversibleSourceFileOperationsMessageCount = 5;

        private readonly IUndoRedoService undoRedoService;
        private readonly Dictionary<string, PackageCategoryViewModel> packageCategories = new Dictionary<string, PackageCategoryViewModel>();
        private readonly Dictionary<PackageViewModel, PackageContainer> packageMap = new Dictionary<PackageViewModel, PackageContainer>();
        private readonly ConcurrentDictionary<AssetId, AssetViewModel> assetIdMap = new ConcurrentDictionary<AssetId, AssetViewModel>();
        private bool sessionStateUpdating;

        private ProjectViewModel currentProject;
        private readonly PackageSession session;

        /// <summary>
        /// Gets the current instance of <see cref="SessionViewModel"/>.
        /// </summary>
        /// <remarks>Accessing this property is allowed only from View-side code (WPF).</remarks>
        public static SessionViewModel Instance { get; private set; }

        [NotNull]
        public EditorViewModel Editor { get; }

        public bool IsEditorInitialized { get; private set; }

        [NotNull]
        public Task SaveCompletion => session.SaveCompletion;

        public UFile SolutionPath => session.SolutionPath;

        public UFile SessionFilePath => SolutionPath;

        public IObservableCollection<PackageViewModel> StorePackages => PackageCategories[StorePackageCategoryName].Content;

        public IObservableCollection<PackageViewModel> LocalPackages => PackageCategories[LocalPackageCategoryName].Content;

        public IReadOnlyDictionary<string, PackageCategoryViewModel> PackageCategories => packageCategories;

        public IEnumerable<PackageViewModel> AllPackages { get { return PackageCategories.Values.SelectMany(x => x.Content); } }

        public AssetCollectionViewModel ActiveAssetView { get; }

        /// <summary>
        /// Gets all assets contained in this session.
        /// </summary>
        /// <remarks>
        /// Some assets in the session might not be accessible to some other assets/packages if they are located in another package that is not a dependency
        /// to the asset/package. To safely retrieve all assets accessible from a specific package, use <see cref="PackageViewModel.AllAssets"/>.
        /// </remarks>
        [NotNull]
        public IEnumerable<AssetViewModel> AllAssets { get { return AllPackages.SelectMany(x => x.Assets); } }

        public TagsViewModel AssetTags { get; }

        public UpdatePackageTemplateCollectionViewModel UpdatePackageViewModel { get; }

        public AssetLogViewModel AssetLog { get; }

        public ActionHistoryViewModel ActionHistory { get; }

        public AssetSourceTrackerViewModel SourceTracker { get; private set; }

        public ReferencesViewModel References { get; }

        // TODO: Properly temporary, until unification of AssemblyContainer and AssemblyRegistry since we now have only a single session at a time
        public Core.Reflection.AssemblyContainer AssemblyContainer => session.AssemblyContainer;

        /// <summary>
        /// Gets the <see cref="SessionObjectViewModel"/> associated to the current selection in the <see cref="ActiveAssetView"/> collection.
        /// </summary>
        // TODO: Move this in AssetCollectionViewModel
        [NotNull]
        public SessionObjectPropertiesViewModel AssetViewProperties { get; }

        /// <summary>
        /// Gets the currently active <see cref="SessionObjectPropertiesViewModel"/>.
        /// </summary>
        public SessionObjectPropertiesViewModel ActiveProperties
        {
            get { return activeProperties; }
            set
            {
                if (SetValueUncancellable(ref activeProperties, value))
                {
                    ActiveAssetsChanged?.Invoke(this, new ActiveAssetsChangedArgs(value?.GetRelatedAssets().ToList()));
                }
            }
        }

        /// <summary>
        /// Gets the current active project for build/startup operations.
        /// </summary>
        // TODO: this property should become cancellable to maintain action stack consistency! Undoing a "mark as root" operation after changing the current package wouldn't work.
        public ProjectViewModel CurrentProject { get => currentProject; private set { var oldValue = currentProject;  SetValueUncancellable(ref currentProject, value, () => UpdateCurrentProject(oldValue, value)); } }

        [NotNull]
        public ThumbnailsViewModel Thumbnails { get; }

        public int ImportEffectLogPendingCount { get => importEffectLogPendingCount; set => SetValueUncancellable(ref importEffectLogPendingCount, value); }

        public IEditorDialogService Dialogs => ServiceProvider.Get<IEditorDialogService>();

        [NotNull]
        public AssetPropertyGraphContainer GraphContainer { get; }

        public bool SelectionIsRoot { get => selectionIsRoot; internal set { SetValueUncancellable(ref selectionIsRoot, value); UpdateSessionState(); } }

        [NotNull]
        public SessionNodeContainer AssetNodeContainer { get; }

        public ICommandBase SaveSessionCommand { get; }

        public ICommandBase NewProjectCommand { get; }

        public ICommandBase AddExistingProjectCommand { get; }

        public ICommandBase ActivatePackagePropertiesCommand { get; }

        public ICommandBase OpenInIDECommand { get; }

        public ICommandBase AddDependencyCommand { get; }

        public ICommandBase NewDirectoryCommand { get; }

        [NotNull]
        public ICommandBase RenameDirectoryOrPackageCommand { get; }

        public ICommandBase DeleteSelectedSolutionItemsCommand { get; }

        public ICommandBase ExploreCommand { get; }

        public ICommandBase OpenWithTextEditorCommand { get; }

        public ICommandBase OpenAssetFileCommand { get; }

        public ICommandBase OpenSourceFileCommand { get; }

        public ICommandBase SetCurrentProjectCommand { get; }

        public ICommandBase EditSelectedContentCommand { get; }

        public ICommandBase ToggleIsRootOnSelectedAssetCommand { get; }

        public ICommandBase ImportEffectLogCommand { get => importEffectLogCommand; set => SetValueUncancellable(ref importEffectLogCommand, value); }

        public ICommandBase NextSelectionCommand { get; }

        public ICommandBase PreviousSelectionCommand { get; }

        public bool IsUpdatePackageEnabled { get; private set; }

        /// <summary>
        /// Gets the dependency manager associated to this session.
        /// </summary>
        public IAssetDependencyManager DependencyManager => session.DependencyManager;

        /// <summary>
        /// Raised when some assets are modified.
        /// </summary>
        public event EventHandler<AssetChangedEventArgs> AssetPropertiesChanged;

        /// <summary>
        /// Raised when some assets are deleted or undeleted.
        /// </summary>
        public event EventHandler<NotifyCollectionChangedEventArgs> DeletedAssetsChanged;

        /// <summary>
        /// Raised when the session state changed (e.g. current package).
        /// </summary>
        public event EventHandler<SessionStateChangedEventArgs> SessionStateChanged;

        /// <summary>
        /// Raised when the active assets collection changed.
        /// </summary>
        public event EventHandler<ActiveAssetsChangedArgs> ActiveAssetsChanged;

        internal readonly IDictionary<Type, Type> AssetViewModelTypes = new Dictionary<Type, Type>();

        /// <summary>
        /// Gets whether the session is currently in a special context to fix up assets.
        /// </summary>
        /// <seealso cref="CreateAssetFixupContext"/>
        public bool IsInFixupAssetContext
        {
            get => isInFixupAssetContext;
            internal set
            {
                GraphContainer.PropagateChangesFromBase = !value;
                SetValue(ref isInFixupAssetContext, value);
                ActiveProperties?.RefreshSelectedPropertiesAsync().Forget();
            }
        }

        private SessionObjectPropertiesViewModel activeProperties;

        private readonly IDebugPage undoRedoStackPage;
        private readonly IDebugPage assetNodesDebugPage;
        private readonly IDebugPage quantumDebugPage;
        private ICommandBase importEffectLogCommand;
        private int importEffectLogPendingCount;
        private bool selectionIsRoot;
        private bool isInFixupAssetContext;

        public static async Task<SessionViewModel> CreateNewSession(EditorViewModel editor, IViewModelServiceProvider serviceProvider, NewSessionParameters newSessionParameters)
        {
            var loggerResult = new LoggerResult();
            var session = new PackageSession();

            var workProgress = new WorkProgressViewModel(serviceProvider, loggerResult)
            {
                Title = Tr._p("Title", "Creating session..."),
                KeepOpen = KeepOpen.OnWarningsOrErrors,
                IsIndeterminate = true,
                IsCancellable = false
            };
            workProgress.RegisterProgressStatus(loggerResult, true);

            // Solution dir is either the given solution location, or Output/Name
            var hasSolutionlocation = !string.IsNullOrWhiteSpace(newSessionParameters.SolutionLocation);
            var baseOutputDir = UPath.Combine<UDirectory>(newSessionParameters.OutputDirectory, newSessionParameters.OutputName);

            var solutionDir = hasSolutionlocation ? newSessionParameters.SolutionLocation : baseOutputDir;
            // Output dir is Output/Name/Name
            var outputDir = baseOutputDir;

            var parameters = new SessionTemplateGeneratorParameters
            {
                Name = newSessionParameters.OutputName,
                OutputDirectory = outputDir,
                Description = newSessionParameters.TemplateDescription,
                Session = session,
                Logger = loggerResult,
            };

            var generator = TemplateManager.FindTemplateGenerator(parameters);
            if (generator == null)
            {
                await serviceProvider.Get<IDialogService>().MessageBox(Tr._p("Message", "Unable to retrieve template generator for the selected template. Aborting."), MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            var solutionName = !string.IsNullOrWhiteSpace(newSessionParameters.SolutionName) ? newSessionParameters.SolutionName : newSessionParameters.OutputName;
            solutionName = solutionName.ToLowerInvariant().EndsWith(SolutionExtension) ? solutionName : solutionName + SolutionExtension;
            session.SolutionPath = UPath.Combine<UFile>(solutionDir, solutionName);
            session.VisualStudioVersion = PackageSession.DefaultVisualStudioVersion;

            if (!await TemplateGeneratorHelper.RunTemplateGeneratorSafe(generator, parameters, workProgress))
            {
                await workProgress.NotifyWorkFinished(true, loggerResult.HasErrors);
                return null;
            }

            SessionViewModel sessionViewModel;
            try
            {
                // Create the service that handles property documentation
                var documentationService = new UserDocumentationService();

                // Create the service that handles selection
                var selectionService = new SelectionService(serviceProvider.Get<IDispatcherService>());

                // Create the service that handles copy/paste
                var copyPasteService = new CopyPasteService();

                // Create the undo/redo service for this session. We use an initial size of 0 to prevent asset upgrade to be cancellable.
                var undoRedoService = new UndoRedoService(0);
                serviceProvider.RegisterService(undoRedoService);

                // Register session-specific services
                serviceProvider.RegisterService(documentationService);
                serviceProvider.RegisterService(selectionService);
                serviceProvider.RegisterService(copyPasteService);

                // Create the actual session view model.
                sessionViewModel = new SessionViewModel(serviceProvider, loggerResult, session, editor);

                // Register the node container to the copy/paste service.
                sessionViewModel.ServiceProvider.Get<CopyPasteService>().PropertyGraphContainer = sessionViewModel.GraphContainer;

                // Load assets from packages
                sessionViewModel.LoadAssetsFromPackages(loggerResult, workProgress);

                // Automatically select a start-up package.
                sessionViewModel.AutoSelectCurrentProject();

                // Copy the result of the asset loading to the log panel.
                sessionViewModel.AssetLog.AddLogger(LogKey.Get("Session"), loggerResult);

                // Now resize the undo stack to the correct size.
                undoRedoService.Resize(200);

                // And initialize the actions view model
                sessionViewModel.ActionHistory.Initialize();
            }
            finally
            {
                await workProgress.NotifyWorkFinished(false, loggerResult.HasErrors);
            }

            return sessionViewModel;
        }

        public static async Task<SessionViewModel> OpenSession(string path, IViewModelServiceProvider serviceProvider, EditorViewModel editor, PackageSessionResult sessionResult)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            // Create the service that handles property documentation
            serviceProvider.RegisterService(new UserDocumentationService());

            // Create the service that handles selection
            serviceProvider.RegisterService(new SelectionService(serviceProvider.Get<IDispatcherService>()));

            // Create the service that handles copy/paste
            serviceProvider.RegisterService(new CopyPasteService());

            // Create the undo/redo service for this session. We use an initial size of 0 to prevent asset upgrade to be cancellable.
            var undoRedoService = new UndoRedoService(0);
            serviceProvider.RegisterService(undoRedoService);

            var cancellationSource = new CancellationTokenSource();
            var workProgress = new WorkProgressViewModel(serviceProvider, sessionResult)
            {
                Title = Tr._p("Title", "Opening session..."),
                KeepOpen = KeepOpen.OnWarningsOrErrors,
                IsIndeterminate = true,
                IsCancellable = true,
                CancelCommand = new AnonymousCommand(serviceProvider, cancellationSource.Cancel)
            };
            workProgress.RegisterProgressStatus(sessionResult, true);

            serviceProvider.Get<IEditorDialogService>().ShowProgressWindow(workProgress, 500);

            // Ensure the loading is finished
            var sessionViewModel = await Task.Run(() =>
            {
                SessionViewModel result = null;
                try
                {
                    // Load the session file (package or solution)
                    PackageSession.Load(path, sessionResult, CreatePackageLoadParameters(workProgress, cancellationSource));
                    if (!cancellationSource.Token.IsCancellationRequested)
                    {
                        // Load references
                        sessionResult.Session.LoadMissingReferences(sessionResult);

                        // Create the session view model (in the UI thread)
                        var dispatcher = serviceProvider.Get<IDispatcherService>();
                        result = dispatcher.Invoke(() => new SessionViewModel(serviceProvider, sessionResult, sessionResult.Session, editor));

                        // Build asset view models
                        result.LoadAssetsFromPackages(sessionResult, workProgress, cancellationSource.Token);
                    }
                }
                catch (Exception e)
                {
                    sessionResult.Error(string.Format(Tr._p("Log", "There was a problem opening the solution.")), e);
                    result = null;
                }
                return result;
            }, cancellationSource.Token);

            if (sessionViewModel == null || cancellationSource.IsCancellationRequested)
            {
                sessionViewModel?.Destroy();
                sessionResult.OperationCancelled = cancellationSource.IsCancellationRequested;
                return null;
            }

            // Register the node container to the copy/paste service.
            sessionViewModel.ServiceProvider.Get<CopyPasteService>().PropertyGraphContainer = sessionViewModel.GraphContainer;

            sessionViewModel.AutoSelectCurrentProject();

            // Now resize the undo stack to the correct size.
            undoRedoService.Resize(200);

            // And initialize the actions view model
            sessionViewModel.ActionHistory.Initialize();

            // Copy the result of the asset loading to the log panel.
            sessionViewModel.AssetLog.AddLogger(LogKey.Get("Session"), sessionResult);

            sessionViewModel.CheckConsistency();

            sessionResult.OperationCancelled = cancellationSource.IsCancellationRequested;

            // Notify that the task is finished
            await workProgress.NotifyWorkFinished(cancellationSource.IsCancellationRequested, sessionResult.HasErrors);

            return sessionViewModel;
        }

        private static PackageLoadParameters CreatePackageLoadParameters(WorkProgressViewModel workProgress, CancellationTokenSource cancellationSource)
        {
            return new PackageLoadParameters
            {
                CancelToken = cancellationSource.Token,
                PackageUpgradeRequested = (package, pendingUpgrades) =>
                {
                    // Generate message (in markdown, so we need to double line feeds)
                    // Note: ** is markdown
                    var message = new StringBuilder();
                    message.AppendLine(string.Format(Tr._p("Message", "The following dependencies in the **{0}** package need to be upgraded:"), package.Meta.Name));
                    message.AppendLine();

                    foreach (var pendingUpgrade in pendingUpgrades)
                    {
                        message.AppendLine(string.Format(Tr._p("Message", "- Dependency to **{0}** must be upgraded from version **{1}** to **{2}**"), pendingUpgrade.Dependency.Name, pendingUpgrade.Dependency.Version, pendingUpgrade.PackageUpgrader.Attribute.UpdatedVersionRange.MinVersion));
                    }

                    message.AppendLine();
                    message.AppendLine(string.Format(Tr._p("Message", "Upgrading assets might break them. We recommend you make a manual backup of your project before you upgrade."), package.Meta.Name));

                    var buttons = new[]
                    {
                        new DialogButtonInfo(Tr._p("Button", "Upgrade"), (int)PackageUpgradeRequestedAnswer.Upgrade),
                        new DialogButtonInfo(Tr._p("Button", "Skip"), (int)PackageUpgradeRequestedAnswer.DoNotUpgrade),
                    };
                    var checkBoxMessage = Tr._p("Message", "Do this for every package in the solution");
                    var messageBoxResult = workProgress.ServiceProvider.Get<IDialogService>().CheckedMessageBox(message.ToString(), false, checkBoxMessage, buttons).Result;
                    var result = (PackageUpgradeRequestedAnswer)messageBoxResult.Result;
                    if (messageBoxResult.IsChecked == true)
                    {
                        result = result == PackageUpgradeRequestedAnswer.Upgrade ? PackageUpgradeRequestedAnswer.UpgradeAll : PackageUpgradeRequestedAnswer.DoNotUpgradeAny;
                    }
                    return result;
                }
            };
        }

        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(SessionViewModel));

            SourceTracker?.Destroy();
            ActionHistory.Destroy();
            Thumbnails.Destroy();
            AssetLog.Destroy();

            EditorDebugTools.UnregisterDebugPage(undoRedoStackPage);
            EditorDebugTools.UnregisterDebugPage(assetNodesDebugPage);
            EditorDebugTools.UnregisterDebugPage(quantumDebugPage);
            // Unregister collection
            ServiceProvider.Get<SelectionService>().UnregisterSelectionScope(ActiveAssetView.SelectedContent);
            ActiveAssetView.SelectedAssets.CollectionChanged -= SelectedAssetsCollectionChanged;

            base.Destroy();
        }

        private void AutoSelectCurrentProject()
        {
            var currentProject = LocalPackages.OfType<ProjectViewModel>().FirstOrDefault(x => x.Type == ProjectType.Executable && x.Platform == PlatformType.Windows) ?? LocalPackages.FirstOrDefault();
            if (currentProject != null)
            {
                SetCurrentProject(currentProject);
            }
        }

        private SessionViewModel(IViewModelServiceProvider serviceProvider, ILogger logger, [NotNull] PackageSession session, [NotNull] EditorViewModel editor)
            : base(serviceProvider)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (editor.Session != null) throw new InvalidOperationException("Unable to have two sessions at the same time");

            Editor = editor;
            this.session = session;

            if (Instance != null)
                throw new InvalidOperationException("The SessionViewModel class can be instanced only once.");

            Instance = this;

            // Initialize the dirtiable manager for all our dirtiable objects
            undoRedoService = ServiceProvider.Get<IUndoRedoService>();

            // Gather all data from plugins
            var pluginService = ServiceProvider.Get<PluginService>();
            pluginService.RegisterSession(this, logger);

            // Initialize the undo/redo debug view model
            undoRedoStackPage = EditorDebugTools.CreateUndoRedoDebugPage(undoRedoService, "Undo/redo stack");
            ActionHistory = new ActionHistoryViewModel(this);

            // Initialize the node container used for asset properties
            AssetNodeContainer = new SessionNodeContainer(this) { NodeBuilder = { NodeFactory = new AssetNodeFactory() } };

            // Initialize the view model that will manage the properties of the assets selected on the main asset view
            AssetViewProperties = new SessionObjectPropertiesViewModel(this);

            assetNodesDebugPage = EditorDebugTools.CreateAssetNodesDebugPage(this, "Asset nodes visualizer");
            var quantumLogger = GlobalLogger.GetLogger(GraphViewModel.DefaultLoggerName);
            quantumLogger.ActivateLog(LogMessageType.Debug);
            quantumDebugPage = EditorDebugTools.CreateLogDebugPage(quantumLogger, "Quantum log");
            ActiveProperties = AssetViewProperties;

            // Initialize the asset collection view model of the main asset view
            ActiveAssetView = new AssetCollectionViewModel(ServiceProvider, this, AssetCollectionViewModel.AllFilterCategories, AssetViewProperties);
            ServiceProvider.Get<SelectionService>().RegisterSelectionScope(id => GetAssetById(id.AssetId), o =>
            {
                var asset = o as AssetViewModel;
                if (asset != null)
                {
                    return new AbsoluteId(asset.Id, Guid.Empty);
                }
                return null;
            }, ActiveAssetView.SelectedContent);
            ActiveAssetView.SelectedAssets.CollectionChanged += SelectedAssetsCollectionChanged;
            ActiveAssetView.SelectedContent.CollectionChanged += (s, e) => UpdateSessionState();
            ActiveAssetView.SelectedLocations.CollectionChanged += (s, e) => UpdateSessionState();

            // Initialize logs
            AssetLog = new AssetLogViewModel(ServiceProvider, this);

            // Initialize the tag view model of the the asset view
            // TODO: can we merge this in the AssetPropertiesViewModel?
            AssetTags = new TagsViewModel(ActiveAssetView);

            // Initialize the reference view model related to the main asset view
            References = new ReferencesViewModel(this, ActiveAssetView);

            // Construct package categories
            var localPackageName = session.SolutionPath != null ? string.Format(Tr._(@"Solution '{0}'"), session.SolutionPath.GetFileNameWithoutExtension()) : LocalPackageCategoryName;
            packageCategories.Add(LocalPackageCategoryName, new PackageCategoryViewModel(localPackageName, this));
            packageCategories.Add(StorePackageCategoryName, new PackageCategoryViewModel(StorePackageCategoryName, this));
            UpdatePackageViewModel = new UpdatePackageTemplateCollectionViewModel(this);
            LocalPackages.CollectionChanged += LocalPackagesCollectionChanged;

            // Initialize commands
            SaveSessionCommand = new AnonymousTaskCommand(ServiceProvider, SaveSession);
            NewProjectCommand = new AnonymousTaskCommand(ServiceProvider, NewProject);
            AddExistingProjectCommand = new AnonymousTaskCommand(ServiceProvider, AddExistingProject);
            ActivatePackagePropertiesCommand = new AnonymousTaskCommand(ServiceProvider, async () => { var directory = ActiveAssetView.GetSelectedDirectories(false).FirstOrDefault(); if (directory != null) await directory.Package.Properties.GenerateSelectionPropertiesAsync(directory.Package.Yield()); });
            OpenInIDECommand = new AnonymousTaskCommand<IDEInfo>(ServiceProvider, OpenInIDE);
            AddDependencyCommand = new AnonymousTaskCommand(ServiceProvider, AddDependency);
            NewDirectoryCommand = new AnonymousTaskCommand<IEnumerable<object>>(ServiceProvider, CreateNewFolderInDirectories);
            RenameDirectoryOrPackageCommand = new AnonymousCommand<IEnumerable>(ServiceProvider, x => RenameDirectoryOrPackage(x.Cast<object>().LastOrDefault()));
            DeleteSelectedSolutionItemsCommand = new AnonymousTaskCommand(ServiceProvider, async () => await DeleteItems(ActiveAssetView.SelectedLocations));
            ExploreCommand = new AnonymousTaskCommand<object>(ServiceProvider, Explore);
            OpenWithTextEditorCommand = new AnonymousTaskCommand<AssetViewModel>(ServiceProvider, OpenWithTextEditor);
            OpenAssetFileCommand = new AnonymousTaskCommand<AssetViewModel>(ServiceProvider, OpenAssetFile);
            OpenSourceFileCommand = new AnonymousTaskCommand<AssetViewModel>(ServiceProvider, OpenSourceFile);
            SetCurrentProjectCommand = new AnonymousCommand(ServiceProvider, SetCurrentProject);
            EditSelectedContentCommand = new AnonymousCommand(ServiceProvider, EditSelectedAsset);
            ToggleIsRootOnSelectedAssetCommand = new AnonymousCommand(ServiceProvider, ToggleIsRootOnSelectedAsset);
            PreviousSelectionCommand = new AnonymousCommand(serviceProvider, () => { ServiceProvider.Get<SelectionService>().NavigateBackward(); UpdateSelectionCommands(); });
            NextSelectionCommand = new AnonymousCommand(serviceProvider, () => { ServiceProvider.Get<SelectionService>().NextSelection(); UpdateSelectionCommands(); });

            // This event must be subscribed before we create the package view models
            PackageCategories.ForEach(x => x.Value.Content.CollectionChanged += PackageCollectionChanged);

            // Create package view models
            session.Projects.ForEach(x => CreateProjectViewModel(x, true));

            // Initialize other sub view models
            Thumbnails = new ThumbnailsViewModel(this);

            GraphContainer = new AssetPropertyGraphContainer(AssetNodeContainer);
        }

        public void PluginsInitialized()
        {
            IsEditorInitialized = true;
        }

        private void EditSelectedAsset()
        {
            // Cannot edit multi-selection
            if (ActiveAssetView.SingleSelectedContent == null)
                return;

            // Asset
            var asset = ActiveAssetView.SingleSelectedAsset;
            if (asset != null)
            {
                // Temporary code while we don't have an integrated text editor.
                if (Editor.TextAssetTypes.Contains(asset.AssetType.Name))
                {
                    OpenWithTextEditor(asset, EditorSettings.ShaderEditor.GetValue());
                    return;
                }

                ServiceProvider.Get<IEditorDialogService>().AssetEditorsManager.OpenAssetEditorWindow(asset);
            }

            // Folder
            var folder = ActiveAssetView.SingleSelectedContent as DirectoryViewModel;
            if (folder != null)
            {
                ActiveAssetView.SelectedLocations.Clear();
                ActiveAssetView.SelectedLocations.Add(folder);
            }
        }

        private void LoadAssetsFromPackages(LoggerResult loggerResult, WorkProgressViewModel workProgress, CancellationToken? cancellationToken = null)
        {
            if (workProgress == null) throw new ArgumentNullException(nameof(workProgress));
            workProgress.Minimum = 0;
            workProgress.ProgressValue = 0;
            workProgress.Maximum = session.Packages.Sum(x => x.Assets.Count);

            // Create directory and asset view models for each project
            foreach (var package in PackageCategories.Values.SelectMany(x => x.Content))
            {
                if (cancellationToken.HasValue && cancellationToken.Value.IsCancellationRequested)
                    return;

                package.LoadPackageInformation(loggerResult, workProgress, cancellationToken);
            }

            // Create actions corresponding to potential upgrades/fixes that occurred during the loading process.
            var upgradedAssets = new List<IDirtiable>();
            upgradedAssets.AddRange(AllPackages.Where(x => x.HasBeenUpgraded));
            upgradedAssets.AddRange(AllPackages.SelectMany(x => x.Assets).Where(x => x.HasBeenUpgraded));
            if (upgradedAssets.Count > 0)
            {
                // This transaction is done on a zero-sized stack just to flag assets as dirty. It is made to be uncancellable.
                using (var transaction = undoRedoService.CreateTransaction())
                {
                    upgradedAssets.ForEach(x => undoRedoService.PushOperation(new AssetsUpgradeOperation(x)));
                    undoRedoService.SetName(transaction, $"Upgrade assets ({upgradedAssets.Count})");
                }
            }

            SourceTracker = new AssetSourceTrackerViewModel(ServiceProvider, session, this);
            UpdateSessionState();

            // This transaction is done to prevent action responding to undoRedoService.TransactionCompletion to occur during loading
            using (undoRedoService.CreateTransaction())
            {
                ProcessAddedPackages(AllPackages);
            }
        }

        private Task OpenWithTextEditor(AssetViewModel asset)
        {
            return OpenWithTextEditor(asset, EditorSettings.DefaultTextEditor.GetValue());
        }

        private async Task OpenWithTextEditor(AssetViewModel asset, string editorPath)
        {
            if (editorPath == null) throw new ArgumentNullException(nameof(editorPath));
            if (asset?.Directory?.Package == null)
                return;

            if (asset.IsDirty)
            {
                var buttons = DialogHelper.CreateButtons(new[]
                {
                    Tr._p("Button", "Save"),
                    Tr._p("Button", "Cancel")
                }, 1, 2);
                var result = await Dialogs.MessageBox(Tr._p("Message", "This asset has unsaved changes. To open it, you need to save the session first. Do you want to save now?"), buttons, MessageBoxImage.Information);
                if (result == 1)
                    await SaveSession();

                if (asset.IsDirty)
                    return;
            }

            editorPath = Environment.ExpandEnvironmentVariables(editorPath);
            try
            {
                var path = asset.AssetItem.FullPath.ToWindowsPath();
                if (!File.Exists(path))
                {
                    await Dialogs.MessageBox(Tr._p("Message", "You need to save the file before you can open it."), MessageBoxButton.OK, MessageBoxImage.Information);
                }

                var process = new Process { StartInfo = new ProcessStartInfo(editorPath, $"\"{path}\"") { UseShellExecute = true } };
                process.Start();
            }
            catch (Exception ex)
            {
                var message = $"{Tr._p("Message", "There was a problem starting the text editor. Make sure the path to the text editor in Settings is correct.")}{ex.FormatSummary(true)}";
                await Dialogs.MessageBox(message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task OpenAssetFile(AssetViewModel asset)
        {
            if (asset?.Directory?.Package == null)
                return;

            if (asset.IsDirty)
            {
                var buttons = DialogHelper.CreateButtons(new[]
                {
                    Tr._p("Button", "Save"),
                    Tr._p("Button", "Cancel")
                }, 1, 2);
                var result = await Dialogs.MessageBox(Tr._p("Message", "This asset has unsaved changes. To open it, you need to save it first. Do you want to save the session now?"), buttons, MessageBoxImage.Information);
                if (result == 1)
                    await SaveSession();

                if (asset.IsDirty)
                    return;
            }

            await Editor.OpenFile(asset.AssetItem.FullPath, true);
        }

        private async Task OpenSourceFile(AssetViewModel asset)
        {
            if (asset?.Directory?.Package == null)
                return;

            var fileToOpen = asset.Asset.MainSource;
            if (fileToOpen == null)
            {
                await Dialogs.MessageBox(Tr._p("Message", "This asset doesn't have a source file to open."), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await Editor.OpenFile(fileToOpen.FullPath, true);
        }

        private async Task Explore(object param)
        {
            if (param is IEnumerable)
                param = ((IEnumerable)param).Cast<object>().LastOrDefault();
            if (param == null)
                param = ActiveAssetView.SelectedLocations.LastOrDefault();

            var solution = param as PackageCategoryViewModel;
            var package = GetContainerPackage(param);
            var asset = param as AssetViewModel;
            var folder = param as DirectoryViewModel;
            var project = param as ProjectViewModel;
            bool fileSelection = false;

            UPath path;
            if (project != null)
            {
                path = project.ProjectPath;
                fileSelection = true;
            }
            else if (param is PackageViewModel)
            {
                path = package.PackagePath;
                fileSelection = true;
            }
            else if (package != null)
            {
                path = package.Package.GetDefaultAssetFolder();

                if (folder != null)
                {
                    var dir = folder.Root is AssetMountPointViewModel ? new UDirectory(path.FullPath) : package.Package.RootDirectory;
                    path = UPath.Combine(dir, new UDirectory(folder.Path));
                }
                else
                {
                    var attrib = (AssetDescriptionAttribute)asset?.AssetType.GetCustomAttributes(typeof(AssetDescriptionAttribute), true).FirstOrDefault();
                    if (attrib != null)
                    {
                        path = asset.AssetItem.FullPath;
                        fileSelection = true;
                    }
                }
            }
            else if (solution != null && solution.Content == LocalPackages && !string.IsNullOrEmpty(SolutionPath))
            {
                path = SolutionPath;
                fileSelection = true;
            }
            else
            {
                return;
            }
            try
            {
                var stringPath = path.ToString().Replace('/', '\\');
                if (asset != null && !File.Exists(stringPath))
                {
                    await Dialogs.MessageBox(Tr._p("Message", "You need to save the asset before you can explore it."), MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                ProcessStartInfo startInfo = fileSelection ? new ProcessStartInfo("explorer.exe", "/select," + stringPath) : new ProcessStartInfo(stringPath);
                startInfo.UseShellExecute = true;
                var explorer = new Process { StartInfo = startInfo };
                explorer.Start();
            }
            catch (Exception)
            {
                await Dialogs.MessageBox(Tr._p("Message", "There was a problem starting the file explorer."), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task<bool> SaveSession()
        {
            Dispatcher.EnsureAccess();

            bool success = false;

            CheckConsistency();

            // Ensure any edition is validated by triggering lost focus, etc.
            ServiceProvider.Get<IEditorDialogService>().ClearKeyboardFocus();

            // Prepare packages to be saved by setting their dirty flag correctly
            foreach (var package in LocalPackages)
            {
                package.PreparePackageForSaving();
            }

            var sessionResult = new LoggerResult();
            var workProgress = new WorkProgressViewModel(ServiceProvider, sessionResult)
            {
                Title = Tr._p("Title", "Saving session..."),
                KeepOpen = KeepOpen.OnWarningsOrErrors,
                IsIndeterminate = true,
                IsCancellable = false,
            };
            workProgress.RegisterProgressStatus(sessionResult, true);

            ServiceProvider.Get<IEditorDialogService>().ShowProgressWindow(workProgress, 500);

            // Ensure saving is finished
            await Task.Run(() =>
            {
                try
                {
                    var saveParameters = PackageSaveParameters.Default();

                    AllAssets.ForEach(x => x.PrepareSave(sessionResult));
                    session.Save(sessionResult, saveParameters);

                    success = true;
                }
                catch (Exception e)
                {
                    sessionResult.Error(string.Format(Tr._p("Log", "There was a problem saving the solution. {0}"), e.Message), e);
                }
            });

            // Fail on errors in the session result
            if (sessionResult.HasErrors)
                success = false;

            // Notify that the task is finished
            await workProgress.NotifyWorkFinished(false, sessionResult.HasErrors);

            // Update view model (in case "Source" is updated due to "Keep source side by side" feature)
            await ActiveProperties.RefreshSelectedPropertiesAsync();

            // Notify assets view model that their underlying assets has been saved
            foreach (var asset in AllPackages.Where(project => !project.Package.IsSystem).SelectMany(package => package.Assets))
            {
                // Note: we use AssetItem.IsDirty rather than AssetViewModel.IsDirty since OnSessionSaved() might be the place where we update AssetViewModel.IsDirty
                if (!asset.AssetItem.IsDirty)
                    asset.OnSessionSaved();
            }

            if (success)
            {
                ActionHistory.NotifySave();

                // Add entry to MRU: priority to the sln, then the first local package
                var mruPath = session.SolutionPath;
                if (mruPath == null && session.LocalPackages.Any())
                {
                    mruPath = session.LocalPackages.First().FullPath;
                }
                if (mruPath != null)
                {
                    Editor.MRU.AddFile(mruPath, Editor.EditorVersionMajor);
                }

                AllPackages.ForEach(x => x.OnSessionSaved());
            }

            UpdateSessionState();

            return success;
        }

        public void CheckConsistency()
        {
            AllPackages.ForEach(x => x.CheckConsistency());
        }

        /// <summary>
        /// Notifies the session that a property of some assets has been changed.
        /// </summary>
        /// <remarks>
        /// Since notifications will be raised asynchronously, <paramref name="assets"/> collection should not be modified after it has been passed to this method.
        /// If necessary, caller must provide a copy.
        /// </remarks>
        public async void NotifyAssetPropertiesChanged([ItemNotNull, NotNull]  IReadOnlyCollection<AssetViewModel> assets)
        {
            var tasks = assets.Select(x => AssetDependenciesViewModel.NotifyAssetChanged(x.Session, x)).ToList();
            await Task.WhenAll(tasks);
            // We raise this event from a task because it will trigger heavy work on the different subscribers
            Task.Run(() => AssetPropertiesChanged?.Invoke(this, new AssetChangedEventArgs(assets))).Forget();
        }

        private void NotifySessionStateChanged()
        {
            SessionStateChanged?.Invoke(this, new SessionStateChangedEventArgs());
        }

        /// <summary>
        /// Gets an <see cref="AssetViewModel"/> instance of the asset which as the given identifier, if available.
        /// </summary>
        /// <param name="id">The identifier of the asset to look for.</param>
        /// <returns>An <see cref="AssetViewModel"/> that matches the given identifier if available. Otherwise, <c>null</c>.</returns>
        [CanBeNull]
        public AssetViewModel GetAssetById(AssetId id)
        {
            AssetViewModel result;
            assetIdMap.TryGetValue(id, out result);
            return result;
        }

        /// <summary>
        /// Creates a <see cref="IDisposable"/> object that represents a context where most of the standard mechanisms relying on property changes are disabled, such as
        /// property changes notifications, creation of <see cref="ActionItem"/>, andpropagation of properties between a base and a derived asset.
        /// </summary>
        /// <returns>A disposable object disabling normal mechanisms relying on property change.</returns>
        /// <remarks>This method should be used only for advanced modifications of assets such as global fixups, that still need to reky on undo/redo.</remarks>
        public IDisposable CreateAssetFixupContext()
        {
            return new FixupAssetContext(this);
        }

        /// <summary>
        /// Register an asset so it can be found using the <see cref="GetAssetById"/> method. This method is intended to be invoked only by <see cref="AssetViewModel"/>.
        /// </summary>
        /// <param name="asset">The asset to register.</param>
        internal void RegisterAsset(AssetViewModel asset)
        {
            ((IDictionary<AssetId, AssetViewModel>)assetIdMap).Add(asset.Id, asset);
        }

        /// <summary>
        /// Unregister an asset previously registered with <see cref="RegisterAsset"/>. This method is intended to be invoked only by <see cref="AssetViewModel"/>.
        /// </summary>
        /// <param name="asset">The asset to register.</param>
        internal void UnregisterAsset(AssetViewModel asset)
        {
            ((IDictionary<AssetId, AssetViewModel>)assetIdMap).Remove(asset.Id);
        }

        /// <summary>
        /// Attempts to close the session. If the session has unsaved changes, ask the user wether to save it or not. If the user cancels
        /// </summary>
        public async Task<bool> Close()
        {
            // Check if either view model is dirty, or any LocalPackage.Assets is (since some view models such as scripts don't make package dirty)
            if (HasUnsavedAssets())
            {
                var buttons = DialogHelper.CreateButtons(new[]
                {
                    Tr._p("Button", "Save"),
                    Tr._p("Button", "Don't save"),
                    Tr._p("Button", "Cancel")
                }, 1, 3);
                var result = await Dialogs.MessageBox(Tr._p("Message", "The project has unsaved changes. Do you want to save it?"), buttons, MessageBoxImage.Question);
                switch (result)
                {
                    case 0:
                    case 3:
                        // Cancel
                        return false;

                    case 1:
                        await SaveSession();
                        // session saving has been cancelled, aborting
                        if (HasUnsavedAssets())
                        {
                            ServiceProvider.Get<IEditorDialogService>().BlockingMessageBox(Tr._p("Message", "Some assets couldn't be saved. Check the assets and try again."), MessageBoxButton.OK,
                                MessageBoxImage.Error);
                            return false;
                        }
                        break;
                }
            }

            return true;
        }

        private bool HasUnsavedAssets()
        {
            return IsDirty || LocalPackages.Any(package => package.IsDirty || package.Assets.Any(asset => asset.IsDirty));
        }

        private PackageViewModel CreateProjectViewModel(PackageContainer packageContainer, bool packageAlreadyInSession)
        {
            switch (packageContainer)
            {
                case SolutionProject project:
                {
                    var packageContainerViewModel = new ProjectViewModel(this, project, packageAlreadyInSession);
                    packageMap.Add(packageContainerViewModel, project);
                    if (!packageAlreadyInSession)
                        session.Projects.Add(project);
                    return packageContainerViewModel;
                }
                case StandalonePackage standalonePackage:
                {
                    var packageContainerViewModel = new PackageViewModel(this, standalonePackage, packageAlreadyInSession);
                    packageMap.Add(packageContainerViewModel, standalonePackage);
                    if (!packageAlreadyInSession)
                        session.Projects.Add(standalonePackage);
                    return packageContainerViewModel;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(packageContainer));
            }
        }

        private void SetCurrentProject(object selectedItem)
        {
            var project = selectedItem as ProjectViewModel;
            if (project == null)
            {
                // Editor.MessageBox(Resources.Strings.SessionViewModel.SelectExecutableAsCurrentProject, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            CurrentProject = project;
            AllAssets.ForEach(x => x.Dependencies.NotifyRootAssetChange(false));
            SelectionIsRoot = ActiveAssetView.SelectedAssets.All(x => x.Dependencies.IsRoot);
        }

        private void UpdateCurrentProject(ProjectViewModel oldValue, ProjectViewModel newValue)
        {
            if (oldValue != null)
            {
                oldValue.IsCurrentProject = false;
            }
            if (newValue != null)
            {
                newValue.IsCurrentProject = true;
            }
            ToggleIsRootOnSelectedAssetCommand.IsEnabled = CurrentProject != null;
            UpdateSessionState();
        }

        private void SelectedAssetsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SelectionIsRoot = ActiveAssetView.SelectedAssets.All(x => x.Dependencies.IsRoot);
        }

        private void LocalPackagesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                session.Projects.RemoveWhere(x => !x.Package.IsSystem);
            }
            if (e.NewItems != null)
            {
                // When a PackageViewModel is built, we will add it before the Package instance is added to the package map.
                // So we can't assume that the view model will always exists in the packageMap.
                packageMap.Where(x => e.NewItems.Cast<PackageViewModel>().Contains(x.Key)).ForEach(x => session.Projects.Add(x.Value));
            }
            e.OldItems?.Cast<PackageViewModel>().Select(x => packageMap[x]).ForEach(x => session.Projects.Remove(x));
        }

        private async Task NewProject()
        {
            var loggerResult = new LoggerResult();

            // Display the template dialog to let the user select which template he want to use.
            var templateDialog = ServiceProvider.Get<IEditorDialogService>().CreateNewProjectDialog(this);
            templateDialog.DefaultOutputDirectory = SolutionPath.GetFullDirectory();
            var dialogResult = await templateDialog.ShowModal();

            if (dialogResult == DialogResult.Cancel)
                return;

            var workProgress = new WorkProgressViewModel(ServiceProvider, loggerResult)
            {
                Title = Tr._p("Title", "Creating project..."),
                KeepOpen = KeepOpen.OnWarningsOrErrors,
                IsIndeterminate = true,
                IsCancellable = false
            };
            workProgress.RegisterProgressStatus(loggerResult, true);

            var parameters = new SessionTemplateGeneratorParameters
            {
                Name = templateDialog.Parameters.OutputName,
                OutputDirectory = templateDialog.Parameters.OutputDirectory,
                Description = templateDialog.Parameters.TemplateDescription,
                Session = session,
                Logger = loggerResult,
            };

            var generator = TemplateManager.FindTemplateGenerator(parameters);
            if (generator == null)
            {
                await Dialogs.MessageBox(Tr._p("Message", "Unable to retrieve template generator for the selected template. Aborting."), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            await TemplateGeneratorHelper.RunTemplateGeneratorSafe(generator, parameters, workProgress);

            // Set the range of the work in progress according to the created assets
            workProgress.Minimum = 0;
            workProgress.ProgressValue = 0;
            workProgress.Maximum = session.Projects.OfType<SolutionProject>().Where(x => !packageMap.ContainsValue(x)).Sum(x => x.Package.Assets.Count);

            // This action is uncancellable - In case of errors, we still try to create view models to match what is currently in the PackageSession, but the template has responsibility to clean itself up in case of failure.
            // TODO: check what is created here and try to avoid it
            ProcessAddedProjects(loggerResult, workProgress, true);

            if (CurrentProject == null)
                AutoSelectCurrentProject();

            await workProgress.NotifyWorkFinished(false, loggerResult.HasErrors);
        }

        private async Task AddExistingProject()
        {
            var fileDialog = ServiceProvider.Get<IEditorDialogService>().CreateFileOpenModalDialog();
            fileDialog.Filters.Add(new FileDialogFilter("Visual Studio C# project", "csproj"));
            fileDialog.InitialDirectory = session.SolutionPath;
            var result = await fileDialog.ShowModal();

            var projectPath = fileDialog.FilePaths.FirstOrDefault();
            if (result == DialogResult.Ok && projectPath != null)
            {
                var loggerResult = new LoggerResult();

                var workProgress = new WorkProgressViewModel(ServiceProvider, loggerResult)
                {
                    Title = Tr._p("Title", "Importing project..."),
                    KeepOpen = KeepOpen.OnWarningsOrErrors,
                    IsIndeterminate = true,
                    IsCancellable = false
                };
                workProgress.RegisterProgressStatus(loggerResult, true);
                // Note: this task is note safely cancellable
                // TODO: Remove the cancellation token if possible.
                var cancellationSource = new CancellationTokenSource();

                ServiceProvider.Get<IEditorDialogService>().ShowProgressWindow(workProgress, 500);

                await Task.Run(() =>
                {
                    try
                    {
                        session.AddExistingProject(projectPath, loggerResult, CreatePackageLoadParameters(workProgress, cancellationSource));
                    }
                    catch (Exception e)
                    {
                        loggerResult.Error(Tr._p("Log", "There was a problem importing the package."), e);
                    }

                }, cancellationSource.Token);

                // Set the range of the work in progress according to the created assets
                workProgress.Minimum = 0;
                workProgress.ProgressValue = 0;
                workProgress.Maximum = session.Projects.OfType<SolutionProject>().Where(x => !packageMap.ContainsValue(x)).Sum(x => x.Package.Assets.Count);

                using (var transaction = UndoRedoService.CreateTransaction())
                {
                    ProcessAddedProjects(loggerResult, workProgress, false);
                    UndoRedoService.SetName(transaction, $"Import project '{new UFile(projectPath).GetFileNameWithoutExtension()}'");
                }

                // Notify that the task is finished
                await workProgress.NotifyWorkFinished(cancellationSource.IsCancellationRequested, loggerResult.HasErrors);
            }
        }

        internal void ProcessAddedProjects(LoggerResult loggerResult, WorkProgressViewModel workProgress, bool packageAlreadyInSession)
        {
            var newPackages = new List<PackageViewModel>();
            foreach (var package in session.Projects.OfType<SolutionProject>().Where(x => !packageMap.ContainsValue(x)))
            {
                var viewModel = CreateProjectViewModel(package, packageAlreadyInSession);
                viewModel.LoadPackageInformation(loggerResult, workProgress);
                newPackages.Add(viewModel);
            }
            ProcessAddedPackages(newPackages);
        }

        internal void ProcessRemovedProjects()
        {
            foreach (var package in packageMap.Where(x => !session.Projects.Contains(x.Value)))
            {
                LocalPackages.Remove(package.Key);
            }
        }

        private async void ProcessAddedPackages(IEnumerable<PackageViewModel> packages)
        {
            var packageList = packages.ToList();
            // We must refresh asset bases after all packages have been added, because we might have cross-packages references here.
            packageList.SelectMany(x => x.Assets).ForEach(x => x.Initialize());
            await AssetDependenciesViewModel.TriggerInitialReferenceBuild(this);
            await Dispatcher.InvokeAsync(() => packageList.ForEach(x => Thumbnails.StartInitialBuild(x)));
        }

        private async Task<PackageViewModel> RequestSingleSelectedPackage()
        {
            var message = Tr._p("Message", "Please select a single package.");
            if (ActiveAssetView.SelectedLocations.Count != 1)
            {
                await Dialogs.MessageBox(message, MessageBoxButton.OK, MessageBoxImage.Information);
                return null;
            }

            PackageViewModel selectedPackage = GetContainerPackage(ActiveAssetView.SelectedLocations[0]);
            if (selectedPackage == null)
            {
                await Dialogs.MessageBox(message, MessageBoxButton.OK, MessageBoxImage.Information);
            }

            return selectedPackage;
        }

        public async Task UpdatePackageTemplate(TemplateDescription templateDescription)
        {
            var selectedPackage = await RequestSingleSelectedPackage();
            if (selectedPackage == null)
                return;

            await selectedPackage.UpdatePackageTemplate(templateDescription);
        }

        private async Task AddDependency()
        {
            var selectedPackage = await RequestSingleSelectedPackage();
            if (selectedPackage == null)
                return;

            var packagePicker = Dialogs.CreatePackagePickerDialog(this);
            // Filter out the selected package, packages that are dependent on the selected package
            // and packages that are already referenced.
            packagePicker.Filter = x =>
            {
                return !(x == selectedPackage || x.DependsOn(selectedPackage) ||
                         selectedPackage.Dependencies.Content.Select(r => r.Target).Contains(x));
            };

            if (AllPackages.All(x => !packagePicker.Filter(x)))
            {
                await Dialogs.MessageBox(Tr._p("Message", "There are no packages that can be added as dependencies to this package."), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = await packagePicker.ShowModal();
            if (result == DialogResult.Ok)
            {
                foreach (var package in packagePicker.SelectedPackages)
                {
                    selectedPackage.AddDependency(package);
                }
            }
        }

        private async Task CreateNewFolderInDirectories(IEnumerable<object> selectedItems)
        {
            var createdDirectories = new List<DirectoryViewModel>();

            DirectoryViewModel createdDirectory = null;
            bool invalidSelectedItem = false;
            var locations = selectedItems.ToList();
            foreach (var selectedItem in locations)
            {
                using (var transaction = UndoRedoService.CreateTransaction())
                {
                    var selectedDirectory = selectedItem as DirectoryBaseViewModel;
                    var selectedPackage = GetContainerPackage(selectedItem);
                    if (selectedDirectory != null)
                    {
                        if (selectedDirectory.Package.IsEditable)
                        {
                            createdDirectory = selectedDirectory.CreateSubDirectory(true);
                        }
                    }
                    else if (selectedPackage != null)
                    {
                        if (selectedPackage.IsEditable)
                        {
                            createdDirectory = selectedPackage.AssetMountPoint.CreateSubDirectory(true);
                        }
                    }
                    else
                    {
                        invalidSelectedItem = true;
                    }

                    if (createdDirectory != null)
                    {
                        createdDirectories.Add(createdDirectory);
                        var parentPath = !string.IsNullOrEmpty(createdDirectory.Parent.Path) ? createdDirectory.Parent.Path : createdDirectory.Parent.Name;
                        UndoRedoService.SetName(transaction, $@"Create folder ""{createdDirectory.Name}"" in ""{parentPath}""");
                    }
                }
            }

            // If an error occurred while creating folders, inform the user.
            if (invalidSelectedItem)
            {
                await Dialogs.MessageBox(locations.Count == 1
                    ? Tr._p("Message", "Folders can only be created in the Assets hierarchy of a package.")
                    : Tr._p("Message", "Game Studio can't create a folder in some of the locations you selected. Folders can only be created in the Assets hierarchy of a package."));
            }
            // If no directory are selected, create in the first package of the session.
            else if (createdDirectory == null)
            {
                var firstPackage = LocalPackages.FirstOrDefault();
                if (firstPackage != null)
                {
                    using (var transaction = UndoRedoService.CreateTransaction())
                    {
                        createdDirectory = firstPackage.AssetMountPoint.CreateSubDirectory(true);
                        createdDirectories.Add(createdDirectory);
                        UndoRedoService.SetName(transaction, $"Create directory '{createdDirectory.Name}' in '{createdDirectory.Parent.Path}'");
                    }
                }
            }

            // Select created directories
            if (createdDirectory != null)
            {
                ActiveAssetView.SelectedLocations.Clear();
                ActiveAssetView.SelectedLocations.AddRange(createdDirectories);
            }

            CheckConsistency();
        }

        private static PackageViewModel GetContainerPackage(object item)
        {
            var package = item as PackageViewModel;
            if (package != null)
                return package;

            var asset = item as AssetViewModel;
            var directory = item as DirectoryBaseViewModel;
            if (asset != null)
                directory = asset.Directory;

            if (directory != null)
                return directory.Package;

            var depsCategory = item as DependencyCategoryViewModel;
            if (depsCategory != null)
                return depsCategory.Parent;

            var reference = item as PackageReferenceViewModel;
            return reference?.Referencer;
        }

        private static void RenameDirectoryOrPackage(object selectedDirectoryOrPackage)
        {
            var package = selectedDirectoryOrPackage as PackageViewModel;
            var directory = selectedDirectoryOrPackage as DirectoryViewModel;
            package?.RenameCommand.Execute();
            directory?.RenameCommand.Execute();
        }

        private void UpdateSelectionCommands()
        {
            PreviousSelectionCommand.IsEnabled = ServiceProvider.Get<SelectionService>().CanGoBack;
            NextSelectionCommand.IsEnabled = ServiceProvider.Get<SelectionService>().CanGoForward;
        }

        internal void UpdateSessionState()
        {
            sessionStateUpdating = true;
            Dispatcher.InvokeAsync(DoUpdateSessionState);
        }

        private void DoUpdateSessionState()
        {
            if (!sessionStateUpdating)
                return;

            var packageSelected = false;
            var projectSelected = false;
            var directorySelected = false;
            var canAddDependency = false;
            var canDelete = ActiveAssetView.SelectedLocations.Count > 0;
            var canRename = ActiveAssetView.SelectedLocations.Count > 0;
            foreach (var location in ActiveAssetView.SelectedLocations.Cast<SessionObjectViewModel>())
            {
                if (location is PackageViewModel package && package.IsEditable)
                {
                    packageSelected = true;
                    canAddDependency = true;
                    projectSelected = package is ProjectViewModel;
                }
                if (location is DirectoryBaseViewModel)
                {
                    directorySelected = true;
                }
                if (location is DependencyCategoryViewModel dependencies)
                {
                    canAddDependency = dependencies.Parent.IsEditable;
                }
                if (location is PackageReferenceViewModel packageReference)
                {
                    canAddDependency = packageReference.Referencer.IsEditable;
                    canRename = false;
                }
                if (!location.IsEditable)
                {
                    canRename = false;
                    canDelete = false;
                }
            }

            var asset = ActiveAssetView.SingleSelectedAsset;

            NewProjectCommand.IsEnabled = !string.IsNullOrWhiteSpace(SolutionPath);
            IsUpdatePackageEnabled = projectSelected;
            AddDependencyCommand.IsEnabled = canAddDependency;
            SetCurrentProjectCommand.IsEnabled = projectSelected;
            DeleteSelectedSolutionItemsCommand.IsEnabled = canDelete;
            ExploreCommand.IsEnabled = ActiveAssetView.SelectedContent.Count > 0 || ActiveAssetView.SelectedLocations.Count == 1;
            RenameDirectoryOrPackageCommand.IsEnabled = canRename;
            NewDirectoryCommand.IsEnabled = packageSelected || directorySelected;
            ActivatePackagePropertiesCommand.IsEnabled = packageSelected || directorySelected;
            EditSelectedContentCommand.IsEnabled = ActiveAssetView.SingleSelectedContent is DirectoryViewModel || asset != null && asset.HasEditor;
            OpenWithTextEditorCommand.IsEnabled = OpenAssetFileCommand.IsEnabled = OpenSourceFileCommand.IsEnabled = asset != null;
            ToggleIsRootOnSelectedAssetCommand.IsEnabled = ActiveAssetView.SelectedAssets.Count > 0 && ActiveAssetView.SelectedAssets.All(x => !x.Dependencies.ForcedRoot);
            UpdateSelectionCommands();

            NotifySessionStateChanged();
            sessionStateUpdating = false;
        }

        public async Task<bool> DeleteItems(IReadOnlyCollection<object> locations, bool skipConfirmation = false)
        {
            // Skip empty
            if (locations.Count == 0)
                return true;

            var directoriesToDelete = locations.OfType<DirectoryBaseViewModel>().ToList();
            var assetsToDelete = locations.OfType<AssetViewModel>().ToList();
            var projectsToDelete = locations.OfType<ProjectViewModel>().ToList();
            var dependenciesToDelete = locations.OfType<PackageReferenceViewModel>().ToList();
            var packagesToDelete = locations.OfType<PackageViewModel>().ToList();

            // Ask confirmation (for assets and directories)
            if ((assetsToDelete.Count > 0 || directoriesToDelete.Count > 0) && !skipConfirmation && EditorSettings.AskBeforeDeletingAssets.GetValue())
            {
                var messageParts = new List<string>();
                if (assetsToDelete.Count == 1)
                    messageParts.Add("this asset");
                else if (assetsToDelete.Count > 1)
                    messageParts.Add($"these {assetsToDelete.Count} assets");
                if (directoriesToDelete.Count == 1)
                    messageParts.Add("this folder");
                else if (directoriesToDelete.Count > 1)
                    messageParts.Add($"these {directoriesToDelete.Count} folders");

                var message = $"Are you sure you want to delete {string.Join(" and ", messageParts)}?";
                var checkedMessage = Tr._p("Settings", "Always delete without asking");
                var buttons = DialogHelper.CreateButtons(new[] { Tr._p("Button", "Delete"), Tr._p("Button", "Cancel") }, 1, 2);
                var result = await ServiceProvider.Get<IDialogService>().CheckedMessageBox(message, false, checkedMessage, buttons, MessageBoxImage.Question);
                if (result.Result != 1)
                    return false;

                if (result.IsChecked == true)
                {
                    EditorSettings.AskBeforeDeletingAssets.SetValue(false);
                    EditorSettings.Save();
                }
            }

            // Collect actual assets to delete (including those from the sub directories)
            var allAssetsToDelete = new HashSet<AssetViewModel>(assetsToDelete);
            allAssetsToDelete.UnionWith(directoriesToDelete.BreadthFirst(x => x.SubDirectories).SelectMany(x => x.Assets));

            // Check deletion
            foreach (var asset in allAssetsToDelete)
            {
                string error;
                if (!asset.CanDelete(out error))
                {
                    error = string.Format(Tr._p("Message", "Xenko can't delete the {0} asset. {1}{2}"), asset.Url, Environment.NewLine, error);
                    await Dialogs.MessageBox(error, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            foreach (var directory in directoriesToDelete)
            {
                string error;
                if (!directory.CanDelete(out error))
                {
                    error = string.Format(Tr._p("Message", "Xenko can't delete the {0} folder. {1}{2}"), directory.Name, Environment.NewLine, error);
                    await Dialogs.MessageBox(error, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            using (var transaction = UndoRedoService.CreateTransaction())
            {
                var systemPackageWarningDisplayed = false;
                var confirmationPackageDisplayed = false;
                var confirmationProjectDisplayed = false;
                var confirmationDependencyDisplayed = false;
                var packagesDeleted = 0;
                var assetsDeleted = 0;
                var directoriesDeleted = 0;
                var projectsDeleted = 0;
                var dependencyDeleted = 0;
                var message = string.Empty;

                // Fix asset references
                if (allAssetsToDelete.Count > 0)
                {
                    var hashIds = new HashSet<AssetId>();
                    hashIds.AddRange(allAssetsToDelete.Select(x => x.Id));
                    var hasReferencesToFix =
                        allAssetsToDelete
                            .SelectMany(x => DependencyManager.ComputeDependencies(x.AssetItem.Id, AssetDependencySearchOptions.In, ContentLinkType.Reference)?.LinksIn ?? new AssetLink[0])
                            .Where(x => !hashIds.Contains(x.Item.Id))
                            .Any(x => GetAssetById(x.Item.Id) != null);
                    if (hasReferencesToFix)
                    {
                        var fixReferenceDialog = ServiceProvider.Get<IEditorDialogService>().CreateFixAssetReferencesDialog(ServiceProvider, allAssetsToDelete, DependencyManager);
                        var fixResult = await fixReferenceDialog.ShowModal();
                        if (fixResult == DialogResult.Cancel)
                            return false;

                        fixReferenceDialog.ApplyReferenceFixes();
                    }
                }

                // Delete selected packages
                foreach (var selectedPackage in packagesToDelete)
                {
                    // Note: this should never happen. UI rules should ensure that the user cannot attempt a system package deletion.
                    if (selectedPackage.Package.IsSystem && !systemPackageWarningDisplayed)
                    {
                        await Dialogs.MessageBox(Tr._p("Message", "Xenko can't delete the system package."), MessageBoxButton.OK, MessageBoxImage.Information);
                        systemPackageWarningDisplayed = true;
                        continue;
                    }

                    if (!confirmationPackageDisplayed)
                    {
                        var buttons = DialogHelper.CreateButtons(new[]
                        {
                            Tr._p("Button", "Delete"),
                            Tr._p("Button", "Cancel")
                        }, 1, 2);
                        var result = await Dialogs.MessageBox(Tr._p("Message", "Are you sure you want to delete this package? The package files will remain on the disk."), buttons, MessageBoxImage.Question);
                        if (result != 1)
                            break;
                    }
                    confirmationPackageDisplayed = true;
                    message = $"Delete package '{selectedPackage.Name}'";
                    selectedPackage.Delete();
                    ++packagesDeleted;
                }

                // Delete package dependencies for deleted package in first pass
                var referencesToDeletedPackages = AllPackages.Except(packagesToDelete)
                    .SelectMany(p => p.Dependencies.Content)
                    .Where(r => packagesToDelete.Contains(r.Target)).ToList();
                foreach (var dependency in referencesToDeletedPackages)
                {
                    message = $"Delete dependency '{dependency.Name}'";
                    dependency.Delete();
                    ++dependencyDeleted;
                }

                // Delete selected package dependencies
                foreach (var selectedDependency in dependenciesToDelete.Except(referencesToDeletedPackages))
                {
                    if (!confirmationDependencyDisplayed)
                    {
                        var buttons = DialogHelper.CreateButtons(new[]
                        {
                            Tr._p("Button", "Delete"),
                            Tr._p("Button", "Cancel")
                        }, 1, 2);
                        var result = await Dialogs.MessageBox(Tr._p("Message", "Are you sure you want to delete this dependency?"), buttons, MessageBoxImage.Question);
                        if (result != 1)
                            break;
                    }
                    confirmationDependencyDisplayed = true;
                    message = $"Delete dependency '{selectedDependency.Name}'";
                    selectedDependency.Delete();
                    ++dependencyDeleted;
                }

                // Delete assets
                if (assetsToDelete.Count > 0)
                {
                    ActiveAssetView.DeleteAssets(assetsToDelete);
                }

                // Delete directories
                foreach (var directory in directoriesToDelete)
                {
                    // We check if one of the recursive parent of the directory is in the list of things to delete.
                    // If so, we skip it, this it is/will be deleted via its parent.
                    var parent = directory.Parent;
                    while (parent != null)
                    {
                        if (locations.Contains(parent))
                            break;
                        parent = parent.Parent;
                    }

                    if (parent != null)
                        continue;

                    message = $"Delete directory '{directory.Path}'";
                    directory.Delete();
                    ++directoriesDeleted;
                }

                // Delete asset mount points
                if (locations.OfType<AssetMountPointViewModel>().Any())
                {
                    // We can't delete a root directory
                    await Dialogs.MessageBox(Tr._p("Message", "Asset root folders can't be deleted."), MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Delete projects
                foreach (var project in projectsToDelete)
                {
                    if (!confirmationProjectDisplayed)
                    {
                        var buttons = DialogHelper.CreateButtons(new[]
                        {
                            Tr._p("Button", "Delete"),
                            Tr._p("Button", "Cancel")
                        }, 1, 2);
                        var result = await Dialogs.MessageBox(Tr._p("Message", "Are you sure you want to delete these projects?"), buttons, MessageBoxImage.Question);
                        if (result != 1)
                            break;
                    }
                    confirmationProjectDisplayed = true;
                    message = $"Delete project '{project.Name}'";
                    project.Delete();
                    ++projectsDeleted;
                }

                // Set transaction name
                var totalDeleted = packagesDeleted + dependencyDeleted + assetsDeleted + directoriesDeleted + projectsDeleted;
                if (totalDeleted > 1)
                {
                    var parts = new List<string>();
                    if (packagesDeleted > 0)
                        parts.Add($"{packagesDeleted} package{(packagesDeleted > 1 ? "s" : "")}");
                    if (dependencyDeleted > 0)
                        parts.Add($"{dependencyDeleted} dependencie{(dependencyDeleted > 1 ? "s" : "")}");
                    if (directoriesDeleted > 0)
                        parts.Add($"{directoriesDeleted} directorie{(directoriesDeleted > 1 ? "s" : "")}");
                    if (assetsDeleted > 0)
                        parts.Add($"{assetsDeleted} asset{(assetsDeleted > 1 ? "s" : "")}");
                    if (projectsDeleted > 0)
                        parts.Add($"{projectsDeleted} project{(projectsDeleted > 1 ? "s" : "")}");
                    message = $"Delete {string.Join("/", parts)}";
                }
                UndoRedoService.SetName(transaction, message);
            }
            CheckConsistency();

            return true;
        }

        private void PackageCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            e.NewItems?.Cast<PackageViewModel>().ForEach(x => x.DeletedAssets.CollectionChanged += DeletedAssetChanged);
            e.OldItems?.Cast<PackageViewModel>().ForEach(x => x.DeletedAssets.CollectionChanged -= DeletedAssetChanged);
        }

        private void DeletedAssetChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DeletedAssetsChanged?.Invoke(this, e);
        }

        private Task OpenInIDE(IDEInfo ideInfo)
        {
            return VisualStudioService.StartOrToggleVisualStudio(this, ideInfo);
        }

        private void ToggleIsRootOnSelectedAsset()
        {
            if (CurrentProject?.Package == null)
                return;

            var currentValue = ActiveAssetView.SelectedAssets.All(x => x.Dependencies.IsRoot);
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                foreach (var selectedAsset in ActiveAssetView.SelectedAssets)
                {
                    if (CurrentProject.IsInScope(selectedAsset))
                        selectedAsset.Dependencies.IsRoot = !currentValue;
                }
                UndoRedoService.SetName(transaction, "Change root assets");
            }
        }

        /// <inheritdoc/>
        AssetItem IAssetFinder.FindAsset(AssetId assetId) => session.FindAsset(assetId);

        /// <inheritdoc/>
        AssetItem IAssetFinder.FindAsset(UFile location) => session.FindAsset(location);

        /// <inheritdoc/>
        AssetItem IAssetFinder.FindAssetFromProxyObject(object proxyObject) => session.FindAssetFromProxyObject(proxyObject);

        public IEnumerable<TemplateDescription> FindTemplates(TemplateScope asset)
        {
            return TemplateManager.FindTemplates(asset, session);
        }
    }
}
