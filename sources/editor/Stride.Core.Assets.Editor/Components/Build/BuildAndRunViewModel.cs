// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Collections.Specialized;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Components.Build;

/// <summary>
/// Builds the current project of the session and starts its game, as the debugging view model of the WPF editor does: the Build
/// project, Start project and Cancel build commands, the log of the build in the Output pane and the build job in the status bar.
/// </summary>
/// <remarks>
/// The build goes through the <see cref="IGameProjectBuilder"/> service and the game starts through the <see cref="IGameLauncher"/>
/// service. Live scripting and the reload of the game assemblies of the WPF editor are not part of it.
/// </remarks>
public sealed class BuildAndRunViewModel : DispatcherViewModel
{
    private readonly string outputTitleBase = Tr._p("Title", "Output");
    private readonly StatusViewModel? status;
    // The console output of the games started from the editor: it stays in the Output pane until they exit, across builds
    private readonly List<LoggerResult> gameOutputs = [];
    private SessionViewModel? session;
    private IGameProjectBuild? currentBuild;
    private bool cancelRequested;
    private bool buildInProgress;
    private bool isOutputVisible;
    private string outputTitle;

    /// <param name="serviceProvider">The services of the editor: the builder, the game launcher and the dialogs.</param>
    /// <param name="status">The status bar that shows the build; <c>null</c> for none.</param>
    public BuildAndRunViewModel(IViewModelServiceProvider serviceProvider, StatusViewModel? status = null)
        : base(serviceProvider)
    {
        this.status = status;
        outputTitle = outputTitleBase;
        BuildLog = new LoggerViewModel(serviceProvider);
        BuildLog.Messages.CollectionChanged += BuildLogChanged;
        BuildProjectCommand = new AnonymousTaskCommand(serviceProvider, () => BuildAsync(false));
        StartProjectCommand = new AnonymousTaskCommand(serviceProvider, () => BuildAsync(true));
        CancelBuildCommand = new AnonymousCommand(serviceProvider, CancelBuild);
        UpdateCommands();
    }

    /// <summary>
    /// The session whose current project the commands build; <c>null</c> when none is open.
    /// </summary>
    public SessionViewModel? Session
    {
        get => session;
        set
        {
            var oldValue = session;
            SetValue(ref session, value, () => OnSessionChanged(oldValue));
        }
    }

    /// <summary>
    /// The log of the last build and the output of the games started from the editor, shown in the Output pane.
    /// </summary>
    public LoggerViewModel BuildLog { get; }

    /// <summary>
    /// Whether a build is in progress, from the save that precedes it to the start of its game.
    /// </summary>
    public bool BuildInProgress
    {
        get => buildInProgress;
        private set => SetValue(ref buildInProgress, value, UpdateCommands);
    }

    /// <summary>
    /// Whether the Output pane is shown: a build shows it, and it shows new messages until then with a <c>*</c> in its title.
    /// </summary>
    public bool IsOutputVisible
    {
        get => isOutputVisible;
        set => SetValue(ref isOutputVisible, value, () =>
        {
            if (value)
                OutputTitle = outputTitleBase;
        });
    }

    /// <summary>
    /// The title of the Output pane, with a <c>*</c> when it has new messages that were not seen, as in the WPF editor.
    /// </summary>
    public string OutputTitle
    {
        get => outputTitle;
        private set => SetValue(ref outputTitle, value);
    }

    /// <summary>
    /// Saves the session and builds its current project (WPF's Build project, F6).
    /// </summary>
    public ICommandBase BuildProjectCommand { get; }

    /// <summary>
    /// Saves the session, builds its current project and starts its game (WPF's Start project, F5).
    /// </summary>
    public ICommandBase StartProjectCommand { get; }

    /// <summary>
    /// Stops the build in progress (WPF's Cancel build, Ctrl+F6).
    /// </summary>
    public ICommandBase CancelBuildCommand { get; }

    private IDialogService Dialogs => ServiceProvider.Get<IDialogService>();

    /// <inheritdoc/>
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(BuildAndRunViewModel));
        CancelBuild();
        Session = null;
        BuildLog.Messages.CollectionChanged -= BuildLogChanged;
        BuildLog.Destroy();
        base.Destroy();
    }

    /// <summary>
    /// Saves the session, then builds its current project and, when asked, starts its game.
    /// </summary>
    /// <param name="startProject">Whether to start the game once it is built.</param>
    /// <returns>How the build went; <see cref="GameBuildOutcome.NotBuilt"/> when it did not start.</returns>
    internal async Task<GameBuildOutcome> BuildAsync(bool startProject)
    {
        if (BuildInProgress || session is not { } buildSession)
            return GameBuildOutcome.NotBuilt;

        cancelRequested = false;
        BuildInProgress = true;
        try
        {
            if (!await PrepareBuildAsync(buildSession))
                return GameBuildOutcome.NotBuilt;

            var jobToken = status?.NotifyBackgroundJobStarted(Tr._p("Status", "Building..."), JobPriority.Compile);
            var outcome = GameBuildOutcome.Failed;
            try
            {
                return outcome = await BuildCoreAsync(buildSession, startProject);
            }
            finally
            {
                if (jobToken is { } token)
                    status!.NotifyBackgroundJobFinished(token);
                status?.PushDiscardableStatus(outcome switch
                {
                    GameBuildOutcome.Succeeded or GameBuildOutcome.StartFailed => Tr._p("Status", "Build successful"),
                    GameBuildOutcome.Canceled => Tr._p("Status", "Build cancelled"),
                    _ => Tr._p("Status", "Build failed"),
                });
            }
        }
        finally
        {
            currentBuild = null;
            BuildInProgress = false;
        }
    }

    /// <summary>
    /// The MSBuild platform and global properties of the build of a project, as the WPF editor sets them; <c>null</c> for a
    /// platform this editor does not build.
    /// </summary>
    /// <param name="project">The project to build.</param>
    /// <param name="solutionPath">The solution of the session, if any.</param>
    internal static GameProjectBuildRequest? CreateBuildRequest(ProjectViewModel project, string? solutionPath)
    {
        var platform = "AnyCPU";
        var properties = new Dictionary<string, string>
        {
            ["StrideBuildEngineLogVerbose"] = "true",
        };
        if (project.Platform != PlatformType.Shared)
        {
            if (!string.IsNullOrEmpty(solutionPath))
            {
                var solution = Path.GetFullPath(solutionPath);
                properties["SolutionPath"] = solution;
                properties["SolutionDir"] = Path.GetDirectoryName(solution) + Path.DirectorySeparatorChar;
            }

            switch (project.Platform)
            {
                case PlatformType.Windows:
                    properties["SolutionPlatform"] = "Any CPU";
                    break;
                case PlatformType.Linux:
                case PlatformType.macOS:
                    platform = project.Platform.ToString();
                    properties["SolutionPlatform"] = platform;
                    break;
                default:
                    // Android and iOS: the WPF editor installs the game on a device, which this editor does not do yet
                    return null;
            }
        }
        return new GameProjectBuildRequest(project.ProjectPath.ToOSPath(), platform, properties);
    }

    private async Task<bool> PrepareBuildAsync(SessionViewModel buildSession)
    {
        if (buildSession.CurrentProject is null)
        {
            await Dialogs.MessageBoxAsync(Tr._p("Message", "To process the build, set an executable project as the current project in the solution explorer."), MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        // The build reads the files of the session, as in the WPF editor
        if (!await buildSession.SaveSessionAsync())
        {
            await Dialogs.MessageBoxAsync(Tr._p("Message", "To build, save the project first."), MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        // The messages still pending would show after the clear
        BuildLog.Flush();
        BuildLog.ClearMessages();
        BuildLog.ClearLoggers();
        foreach (var gameOutput in gameOutputs)
            BuildLog.AddLogger(gameOutput);
        IsOutputVisible = true;
        return true;
    }

    private async Task<GameBuildOutcome> BuildCoreAsync(SessionViewModel buildSession, bool startProject)
    {
        var logger = new LoggerResult();
        BuildLog.AddLogger(logger);

        var project = buildSession.CurrentProject!;
        try
        {
            if (project.Type != ProjectType.Executable)
            {
                logger.Error(string.Format(Tr._p("Message", "The current project {0} is a library: set the executable project of a platform as the current project in the solution explorer."), project.Name));
                return GameBuildOutcome.Failed;
            }

            var request = CreateBuildRequest(project, buildSession.SolutionPath?.ToOSPath());
            if (request is null)
            {
                logger.Error(string.Format(Tr._p("Message", "Platform {0} isn't supported for execution."), project.Platform));
                return GameBuildOutcome.Failed;
            }

            if (cancelRequested)
                return Canceled();

            logger.Info(string.Format(Tr._p("Message", "Building {0}..."), project.Name));
            var builder = ServiceProvider.Get<IGameProjectBuilder>();
            // Loading the project evaluates it, which can take a while
            var build = await Task.Run(() => builder.StartBuild(request, logger));
            if (build is null)
            {
                logger.Error(string.Format(Tr._p("Message", "Unable to load and compile project {0}"), project.ProjectPath));
                return GameBuildOutcome.Failed;
            }

            currentBuild = build;
            if (cancelRequested)
                build.Cancel();
            var succeeded = await build.Completion;
            if (build.IsCanceled)
                return Canceled();
            if (!succeeded || logger.HasErrors)
            {
                var errorCount = logger.Messages.Count(x => x.IsAtLeast(LogMessageType.Error));
                logger.Error(errorCount > 0
                    ? string.Format(Tr._p("Message", "Build of {0} failed with {1} error(s)."), project.Name, errorCount)
                    : string.Format(Tr._p("Message", "Build of {0} failed."), project.Name));
                return GameBuildOutcome.Failed;
            }

            logger.Info(string.Format(Tr._p("Message", "Build of {0} succeeded."), project.Name));
            if (!startProject)
                return GameBuildOutcome.Succeeded;

            return StartGame(project, build.AssemblyPath, logger) ? GameBuildOutcome.Succeeded : GameBuildOutcome.StartFailed;
        }
        catch (Exception e)
        {
            logger.Error(Tr._p("Message", "An exception occurred during compilation."), e);
            await Dialogs.MessageBoxAsync(string.Format(Tr._p("Message", "An exception occurred while compiling the project: {0}"), e.FormatSummary(true)), MessageBoxButton.OK, MessageBoxImage.Information);
            return GameBuildOutcome.Failed;
        }

        GameBuildOutcome Canceled()
        {
            logger.Warning(string.Format(Tr._p("Message", "Build of {0} cancelled."), project.Name));
            return GameBuildOutcome.Canceled;
        }
    }

    /// <summary>
    /// Starts the game that was just built, when it runs on this machine, and reports its exit in the Output pane.
    /// </summary>
    private bool StartGame(ProjectViewModel project, string assemblyPath, LoggerResult logger)
    {
        // The WPF editor deploys a Linux or macOS game to a remote machine, and an Android game to a device: this editor starts
        // the games of its own platform only
        if (project.Platform != Platform.Type)
        {
            logger.Error(string.Format(Tr._p("Message", "{0} is a {1} game: this editor can only start the games of its own platform ({2})."), project.Name, project.Platform, Platform.Type));
            return false;
        }

        // The game's own output is logged under its name
        var output = new LoggerResult(project.Name);
        IGameProcess game;
        try
        {
            game = ServiceProvider.Get<IGameLauncher>().Start(assemblyPath, output);
        }
        catch (Exception e)
        {
            logger.Error(string.Format(Tr._p("Message", "Unable to start {0}."), assemblyPath), e);
            return false;
        }

        gameOutputs.Add(output);
        BuildLog.AddLogger(output);
        logger.Info(string.Format(Tr._p("Message", "{0} started (process {1})."), project.Name, game.Id));
        ReportExitAsync(project.Name, game, output).Forget();
        return true;
    }

    private async Task ReportExitAsync(string name, IGameProcess game, LoggerResult output)
    {
        int? exitCode = null;
        Exception? error = null;
        try
        {
            exitCode = await game.Exited;
        }
        catch (Exception e)
        {
            error = e;
        }

        await Dispatcher.InvokeAsync(() =>
        {
            gameOutputs.Remove(output);
            if (error is not null)
                output.Error(string.Format(Tr._p("Message", "Lost track of {0}."), name), error);
            else if (exitCode == 0)
                output.Info(string.Format(Tr._p("Message", "{0} exited with code {1}."), name, exitCode));
            else
                output.Error(string.Format(Tr._p("Message", "{0} exited with code {1}."), name, exitCode));
        });
    }

    private void CancelBuild()
    {
        if (!BuildInProgress)
            return;

        // Before the build starts (while the session saves), it does not start at all
        cancelRequested = true;
        currentBuild?.Cancel();
    }

    private void OnSessionChanged(SessionViewModel? oldValue)
    {
        if (oldValue is not null)
            oldValue.PropertyChanged -= SessionPropertyChanged;
        if (session is not null)
            session.PropertyChanged += SessionPropertyChanged;
        UpdateCommands();
    }

    private void SessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SessionViewModel.CurrentProject))
            UpdateCommands();
    }

    private void BuildLogChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && !IsOutputVisible)
            OutputTitle = outputTitleBase + '*';
    }

    private void UpdateCommands()
    {
        BuildProjectCommand.IsEnabled = session is not null && !BuildInProgress;
        StartProjectCommand.IsEnabled = session?.CurrentProject is not null && !BuildInProgress;
        CancelBuildCommand.IsEnabled = BuildInProgress;
    }
}

/// <summary>
/// How a build of <see cref="BuildAndRunViewModel"/> went.
/// </summary>
internal enum GameBuildOutcome
{
    /// <summary>
    /// Nothing was built: no session, no current project, or the session could not be saved.
    /// </summary>
    NotBuilt,

    /// <summary>
    /// The project was built, and its game started when asked.
    /// </summary>
    Succeeded,

    /// <summary>
    /// The build failed.
    /// </summary>
    Failed,

    /// <summary>
    /// The build was cancelled.
    /// </summary>
    Canceled,

    /// <summary>
    /// The project was built but its game could not start.
    /// </summary>
    StartFailed,
}
