// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Text;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModel.Logs;
using Stride.Core.Assets.Editor.ViewModel.Progress;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.ViewModels;

partial class SessionViewModel
{
    private static SessionViewModel? instance;
    private static readonly SemaphoreSlim semaphore = new(1, 1);

    /// <summary>
    /// The current instance of <see cref="SessionViewModel"/>.
    /// </summary>
    /// <remarks>Accessing this property is allowed only from view-side code, in places it would be hard to get otherwise.</remarks>
    public static SessionViewModel Instance
    {
        get
        {
            semaphore.Wait();
            try
            {
                // This getter should only be legitimately called when the session has been created.
                // Therefore we assume isn't null here.
                Debug.Assert(instance is not null);
                return instance!;
            }
            finally
            {
                semaphore.Release();
            }
        }
    }

    /// <summary>
    /// The current instance of <see cref="SessionViewModel"/>, or null when no session is open.
    /// </summary>
    /// <remarks>For view-side code that also runs without a session, such as a converter.</remarks>
    public static SessionViewModel? CurrentInstance
    {
        get
        {
            semaphore.Wait();
            try
            {
                return instance;
            }
            finally
            {
                semaphore.Release();
            }
        }
    }

    /// <summary>
    /// Creates a new session from a template (a new project), and opens it.
    /// </summary>
    /// <returns>The session, or <c>null</c> when the template could not be run.</returns>
    public static async Task<SessionViewModel?> CreateNewSessionAsync(NewSessionParameters newSessionParameters, IMainViewModel main, IViewModelServiceProvider serviceProvider)
    {
        var loggerResult = new LoggerResult();
        var session = new PackageSession { LoadEditorPackages = true, EditorToolkit = EditorToolkitName };

        var workProgress = new WorkProgressViewModel(serviceProvider, loggerResult)
        {
            Title = Tr._p("Title", "Creating session..."),
            KeepOpen = KeepOpen.OnWarningsOrErrors,
            IsIndeterminate = true,
            IsCancellable = false
        };
        workProgress.RegisterProgressStatus(loggerResult, true);

        // Solution dir is either the given solution location, or Output/Name
        var hasSolutionLocation = !string.IsNullOrWhiteSpace(newSessionParameters.SolutionLocation);
        var baseOutputDir = UPath.Combine<UDirectory>(newSessionParameters.OutputDirectory, newSessionParameters.OutputName);

        var solutionDir = hasSolutionLocation ? newSessionParameters.SolutionLocation : baseOutputDir;
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
            await serviceProvider.Get<IDialogService>().MessageBoxAsync(Tr._p("Message", "Unable to retrieve template generator for the selected template. Aborting."), MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }

        var solutionName = !string.IsNullOrWhiteSpace(newSessionParameters.SolutionName) ? newSessionParameters.SolutionName : newSessionParameters.OutputName;
        solutionName = solutionName.EndsWith(SolutionExtension, StringComparison.OrdinalIgnoreCase) ? solutionName : solutionName + SolutionExtension;
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
            var undoRedoService = RegisterSessionServices(serviceProvider);

            // Create the actual session view model.
            sessionViewModel = new SessionViewModel(serviceProvider, session, main, loggerResult);

            // Load assets from packages
            sessionViewModel.LoadAssetsFromPackages(loggerResult, workProgress);

            // Automatically select a start-up package.
            sessionViewModel.AutoSelectCurrentProject();

            sessionViewModel.LoadDocumentation();

            // Copy the result of the asset loading to the log panel.
            sessionViewModel.AssetLog.AddLogger(LogKey.Get(AssetLogViewModel.SessionLogName), loggerResult);

            // Now resize the undo stack to the correct size.
            undoRedoService.Resize(200);

            // And initialize the actions view model
            sessionViewModel.ActionHistory.Initialize();
        }
        finally
        {
            await workProgress.NotifyWorkFinished(false, loggerResult.HasErrors);
        }

        await SetInstanceAsync(sessionViewModel);
        return sessionViewModel;
    }

    /// <summary>
    /// Opens the session of the given solution or package file.
    /// </summary>
    /// <returns>The session, or <c>null</c> when it could not be opened or the opening was cancelled.</returns>
    public static async Task<SessionViewModel?> OpenSessionAsync(UFile path, PackageSessionResult sessionResult, IMainViewModel main, IViewModelServiceProvider serviceProvider, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(path);

        var undoRedoService = RegisterSessionServices(serviceProvider);

        var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        token = cancellationSource.Token;
        var workProgress = new WorkProgressViewModel(serviceProvider, sessionResult)
        {
            Title = Tr._p("Title", "Opening session..."),
            KeepOpen = KeepOpen.OnWarningsOrErrors,
            IsIndeterminate = true,
            IsCancellable = true,
            CancelCommand = new AnonymousCommand(serviceProvider, cancellationSource.Cancel)
        };
        workProgress.RegisterProgressStatus(sessionResult, true);

        serviceProvider.Get<IEditorDialogService>().ShowProgressWindow(workProgress);

        // Ensure the loading is finished
        SessionViewModel? sessionViewModel;
        try
        {
            sessionViewModel = await Task.Run(() =>
            {
                SessionViewModel? result = null;
                try
                {
                    // Load the session file (package or solution)
                    PackageSession.Load(path, sessionResult, CreatePackageLoadParameters(serviceProvider, token));
                    if (!token.IsCancellationRequested)
                    {
                        // Load references
                        sessionResult.Session.LoadMissingReferences(sessionResult);

                        // Create the session view model (in the UI thread)
                        var dispatcher = serviceProvider.Get<IDispatcherService>();
                        result = dispatcher.Invoke(() => new SessionViewModel(serviceProvider, sessionResult.Session, main, sessionResult));

                        // Build asset view models
                        result.LoadAssetsFromPackages(sessionResult, workProgress, token);
                    }
                }
                catch (Exception ex)
                {
                    sessionResult.Error(Tr._p("Log", "There was a problem opening the solution."), ex);
                    result = null;
                }
                return result;
            }, token);

            if (sessionViewModel is null || token.IsCancellationRequested)
            {
                sessionViewModel?.Destroy();
                sessionResult.OperationCancelled = token.IsCancellationRequested;
                return null;
            }

            sessionViewModel.AutoSelectCurrentProject();

            sessionViewModel.LoadDocumentation();

            // Now resize the undo stack to the correct size.
            undoRedoService.Resize(200);

            // And initialize the actions view model
            sessionViewModel.ActionHistory.Initialize();

            // Copy the result of the asset loading to the log panel.
            sessionViewModel.AssetLog.AddLogger(LogKey.Get(AssetLogViewModel.SessionLogName), sessionResult);

            sessionViewModel.CheckConsistency();
            sessionViewModel.CheckAssetReplacements();

            sessionResult.OperationCancelled = token.IsCancellationRequested;
        }
        finally
        {
            // Always notify completion so the progress window can close, even if loading failed,
            // was cancelled, or threw. Otherwise it stays open as a blocking window and leaves the
            // main window disabled.
            await workProgress.NotifyWorkFinished(token.IsCancellationRequested, sessionResult.HasErrors);
        }

        await SetInstanceAsync(sessionViewModel);

        // An upgrade (package or pure asset-format) dirties the freshly-loaded session.
        // Persist it now so the upgrade and its backup complete now instead of manual save later.
        var loadedSession = sessionResult.Session;
        if (loadedSession.LocalPackages.Any(package => package.IsDirty || package.Assets.IsDirty))
            await sessionViewModel.SaveSessionAsync();    // save persists the upgrade and disarms the backup
        else
            loadedSession.DisarmUpgradeBackup();         // clean load: no save runs, so disarm here

        sessionViewModel.UpdateHasUnsavedChanges();
        return sessionViewModel;
    }

    /// <summary>
    /// Registers the services that live as long as the session: selection, copy/paste and undo/redo.
    /// </summary>
    /// <returns>The undo/redo service, sized to 0 so that the loading (asset upgrades) cannot be undone.</returns>
    private static UndoRedoService RegisterSessionServices(IViewModelServiceProvider serviceProvider)
    {
        // Create the service that handles selection
        var selectionService = new SelectionService(serviceProvider.Get<IDispatcherService>());

        // Create the service that handles copy/paste
        var copyPasteService = new CopyPasteService();

        // Create the undo/redo service for this session. We use an initial size of 0 to prevent asset upgrade to be cancellable.
        var undoRedoService = new UndoRedoService(0);

        // Register the session-specific services
        serviceProvider.RegisterService(undoRedoService);
        serviceProvider.RegisterService(copyPasteService);
        serviceProvider.RegisterService(selectionService);
        return undoRedoService;
    }

    private static async Task SetInstanceAsync(SessionViewModel sessionViewModel)
    {
        await semaphore.WaitAsync();
        try
        {
            instance = sessionViewModel;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static PackageLoadParameters CreatePackageLoadParameters(IViewModelServiceProvider serviceProvider, CancellationToken token)
    {
        // Snapshot every file the upgrade overwrites into a timestamped backup folder under the solution.
        // Copy-on-write, so it costs nothing unless an upgrade actually runs and modifies files. The dialog
        // checkbox below can opt out; the callback updates this flag, which the session honors. The choice
        // covers the whole upgrade: only the first dialog can change it, later ones show it greyed out.
        var loadParameters = new PackageLoadParameters
        {
            CancelToken = token,
            BackupBeforeUpgrade = true,
            LoadEditorPackages = true,
            EditorToolkit = EditorToolkitName,
            // An asset of a missing plugin stays in the session as an unloadable asset
            LoadAssetsOfUnknownType = true,
        };
        var backupChoiceMade = false;
        loadParameters.PackageUpgradeRequested = (package, pendingUpgrades) =>
        {
            // Generate message (in markdown, so we need to double line feeds)
            // Note: ** is markdown
            var message = new StringBuilder();
            message.AppendLine(string.Format(Tr._p("Message", "The following dependencies in the **{0}** package need to be upgraded:"), package.Meta.Name));
            message.AppendLine();

            foreach (var pendingUpgrade in pendingUpgrades)
            {
                message.AppendLine(string.Format(Tr._p("Message", "- Dependency to **{0}** must be upgraded from version **{1}** to **{2}**"), pendingUpgrade.Dependency.Name, pendingUpgrade.Dependency.Version, pendingUpgrade.PackageUpgrader.Attribute.UpdatedVersionRange!.MinVersion));
            }

            message.AppendLine();
            message.AppendLine(Tr._p("Message", "Upgrading assets might break them. We also recommend committing or backing up your project first."));

            var buttons = new[]
            {
                new DialogButtonInfo { Content = Tr._p("Button", "Upgrade"), Result = (int)PackageUpgradeRequestedAnswer.Upgrade },
                new DialogButtonInfo { Content = Tr._p("Button", "Skip"), Result = (int)PackageUpgradeRequestedAnswer.DoNotUpgrade },
            };
            var applyToAll = new DialogCheckBoxInfo { Content = Tr._p("Message", "Do this for every package in the solution"), IsChecked = false };
            var backup = new DialogCheckBoxInfo { Content = Tr._p("Message", "Back up each modified file to a timestamped folder under the solution"), IsChecked = loadParameters.BackupBeforeUpgrade, IsEnabled = !backupChoiceMade };
            // The callback runs on the loading thread, not on the UI thread: waiting for the dialog is safe
            var buttonResult = serviceProvider.Get<IDialogService>().CheckedMessageBoxAsync(message.ToString(), [applyToAll, backup], buttons).Result;

            loadParameters.BackupBeforeUpgrade = backup.IsChecked == true;
            backupChoiceMade = true;
            var result = (PackageUpgradeRequestedAnswer)buttonResult;
            if (applyToAll.IsChecked == true)
            {
                result = result == PackageUpgradeRequestedAnswer.Upgrade ? PackageUpgradeRequestedAnswer.UpgradeAll : PackageUpgradeRequestedAnswer.DoNotUpgradeAny;
            }
            return result;
        };
        return loadParameters;
    }
}
