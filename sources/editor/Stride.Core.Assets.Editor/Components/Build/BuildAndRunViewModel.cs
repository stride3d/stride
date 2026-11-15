// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Collections.Specialized;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.Build;

/// <summary>
/// Builds the current project of the session and starts its game, as the debugging view model of the WPF editor does: the Build
/// project, Start project and Cancel build commands, the log of the build in the Output pane and the build job in the status bar.
/// </summary>
/// <remarks>
/// The build goes through the <see cref="IGameProjectBuilder"/> service. A game of the editor's own platform starts through the
/// <see cref="IGameLauncher"/> service; the games of the platforms with a deploy step (<see cref="RegisterDeployer"/>) are deployed by
/// it. Live scripting is not part of it; the reload of the game assemblies is <see cref="GameCodeReloadViewModel"/>, which builds the
/// changed code with <see cref="BuildCodeAsync"/>.
/// </remarks>
public sealed class BuildAndRunViewModel : DispatcherViewModel
{
    /// <summary>
    /// The number of messages the Output pane keeps: a game that logs every frame would otherwise grow it for as long as it runs.
    /// </summary>
    internal const int MaximumOutputMessages = 10_000;

    private readonly string outputTitleBase = Tr._p("Title", "Output");
    private readonly StatusViewModel? status;
    // The console output of the games started from the editor: it stays in the Output pane until they exit, across builds
    private readonly List<Logger> gameOutputs = [];
    private readonly Dictionary<PlatformType, IGameDeployer> deployers = [];
    private SessionViewModel? session;
    private IGameProjectBuild? currentBuild;
    // Canceled by Cancel build: before the build starts (while the session saves), it does not start at all
    private CancellationTokenSource cancellation = new();
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
        BuildLog = new LoggerViewModel(serviceProvider)
        {
            MaximumMessages = MaximumOutputMessages,
            MaximumMessagesNotice = string.Format(Tr._p("Message", "Older messages were removed: the Output pane keeps the last {0}."), MaximumOutputMessages),
        };
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

    /// <summary>
    /// The deploy steps of the platforms whose games are deployed to a device or another machine, by platform.
    /// </summary>
    public IReadOnlyDictionary<PlatformType, IGameDeployer> Deployers => deployers;

    private IDialogService Dialogs => ServiceProvider.Get<IDialogService>();

    /// <summary>
    /// Registers the deploy step of the games of a platform: it prepares their builds, and deploys and starts them instead of the
    /// <see cref="IGameLauncher"/>.
    /// </summary>
    /// <param name="platform">The platform of the games.</param>
    /// <param name="deployer">The deploy step; it replaces the one registered before for this platform.</param>
    public void RegisterDeployer(PlatformType platform, IGameDeployer deployer)
    {
        ArgumentNullException.ThrowIfNull(deployer);
        deployers[platform] = deployer;
    }

    /// <inheritdoc/>
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(BuildAndRunViewModel));
        CancelBuild();
        cancellation.Dispose();
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
    internal async Task<GameBuildOutcome> BuildAsync(bool startProject) => (await RunAsync(startProject)).Outcome;

    /// <summary>
    /// Saves the session, then builds its current project and, when asked, starts its game.
    /// </summary>
    /// <param name="startProject">Whether to start the game once it is built.</param>
    /// <param name="start">Starts the game instead of the deploy step of its platform (for example in a debugging mode): it gets the
    /// project, the log of the build and the token of Cancel build, and tells whether the game started; <c>null</c> for the deploy step.</param>
    /// <returns>How the build went, and the game started on this machine if any.</returns>
    internal async Task<GameRunResult> RunAsync(bool startProject, Func<ProjectViewModel, ILogger, CancellationToken, Task<bool>>? start = null)
    {
        if (BuildInProgress || session is not { } buildSession)
            return new GameRunResult(GameBuildOutcome.NotBuilt, null);

        IGameProcess? game = null;
        var outcome = await RunBuildAsync(() => PrepareBuildAsync(buildSession), async () =>
        {
            var (buildOutcome, startedGame) = await BuildCoreAsync(buildSession, startProject, start);
            game = startedGame;
            return buildOutcome;
        });
        return new GameRunResult(outcome, game);
    }

    /// <summary>
    /// The global properties of the build of the game code before a reload, the ones the WPF editor sets.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> ReloadBuildProperties = new Dictionary<string, string>
    {
        ["SolutionPlatform"] = "Any CPU",
        ["StrideBuildEngineLogVerbose"] = "true",
    };

    /// <summary>
    /// Builds projects of the game code, one after the other, before the editor loads them again: their errors show in the Output
    /// pane, and a failure stops the reload. The session is not saved: nothing it holds goes into these builds.
    /// </summary>
    /// <param name="projectPaths">The project files to build, dependencies first.</param>
    /// <returns>How the builds went: the outcome of the first one that did not succeed; <see cref="GameBuildOutcome.NotBuilt"/> when
    /// a build is in progress already, or without a session or a project.</returns>
    internal async Task<GameBuildOutcome> BuildCodeAsync(IReadOnlyList<string> projectPaths) => (await BuildCodeAssembliesAsync(projectPaths)).Outcome;

    /// <summary>
    /// Builds projects of the game code, as <see cref="BuildCodeAsync"/> does, and gives the assemblies they produced.
    /// </summary>
    /// <param name="projectPaths">The project files to build, dependencies first.</param>
    /// <returns>How the builds went, and the assembly of each project that was built, by project file.</returns>
    internal async Task<GameCodeBuildResult> BuildCodeAssembliesAsync(IReadOnlyList<string> projectPaths)
    {
        ArgumentNullException.ThrowIfNull(projectPaths);
        var assemblies = new Dictionary<string, string>();
        if (BuildInProgress || session is null || projectPaths.Count == 0)
            return new GameCodeBuildResult(GameBuildOutcome.NotBuilt, assemblies);

        var outcome = await RunBuildAsync(() =>
        {
            ShowNewBuildLog();
            return Task.FromResult(true);
        }, async () =>
        {
            var logger = new LoggerResult();
            BuildLog.AddLogger(logger);
            foreach (var projectPath in projectPaths)
            {
                // As the WPF editor builds the game code it reloads in place
                var request = new GameProjectBuildRequest(projectPath, "AnyCPU", ReloadBuildProperties);
                var (outcome, build) = await BuildProjectAsync(Path.GetFileNameWithoutExtension(projectPath), request, logger);
                if (outcome != GameBuildOutcome.Succeeded)
                    return outcome;
                assemblies[projectPath] = build!.AssemblyPath;
            }
            return GameBuildOutcome.Succeeded;
        });
        return new GameCodeBuildResult(outcome, assemblies);
    }

    /// <summary>
    /// Runs a build: the commands and the status bar follow it, and it can be cancelled.
    /// </summary>
    /// <param name="prepare">Prepares the build; <c>false</c> stops it before it starts.</param>
    /// <param name="build">The build itself.</param>
    private async Task<GameBuildOutcome> RunBuildAsync(Func<Task<bool>> prepare, Func<Task<GameBuildOutcome>> build)
    {
        cancellation.Dispose();
        cancellation = new CancellationTokenSource();
        BuildInProgress = true;
        try
        {
            if (!await prepare())
                return GameBuildOutcome.NotBuilt;

            var jobToken = status?.NotifyBackgroundJobStarted(Tr._p("Status", "Building..."), JobPriority.Compile);
            var outcome = GameBuildOutcome.Failed;
            try
            {
                return outcome = await build();
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
    /// platform the editor does not build. The deploy step of the platform, if any, completes it (<see cref="IGameDeployer.PrepareBuildAsync"/>).
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
                case PlatformType.Android:
                    platform = project.Platform.ToString();
                    properties["SolutionPlatform"] = platform;
                    break;
                case PlatformType.iOS:
                    platform = "iPhone";
                    properties["SolutionPlatform"] = platform;
                    break;
                default:
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

        ShowNewBuildLog();
        return true;
    }

    /// <summary>
    /// Clears the Output pane for a new build, but the output of the games still running, and shows it.
    /// </summary>
    private void ShowNewBuildLog()
    {
        // The messages still pending would show after the clear
        BuildLog.Flush();
        BuildLog.ClearMessages();
        BuildLog.ClearLoggers();
        foreach (var gameOutput in gameOutputs)
            BuildLog.AddLogger(gameOutput);
        IsOutputVisible = true;
    }

    private async Task<(GameBuildOutcome Outcome, IGameProcess? Game)> BuildCoreAsync(SessionViewModel buildSession, bool startProject, Func<ProjectViewModel, ILogger, CancellationToken, Task<bool>>? start)
    {
        var logger = new LoggerResult();
        BuildLog.AddLogger(logger);
        var cancellationToken = cancellation.Token;

        var project = buildSession.CurrentProject!;
        if (project.Type != ProjectType.Executable)
        {
            logger.Error(string.Format(Tr._p("Message", "The current project {0} is a library: set the executable project of a platform as the current project in the solution explorer."), project.Name));
            return (GameBuildOutcome.Failed, null);
        }

        var request = CreateBuildRequest(project, buildSession.SolutionPath?.ToOSPath());
        if (request is null)
        {
            logger.Error(string.Format(Tr._p("Message", "Platform {0} isn't supported for execution."), project.Platform));
            return (GameBuildOutcome.Failed, null);
        }

        // A game started another way (start) is not deployed
        var deployer = start is null ? deployers.GetValueOrDefault(project.Platform) : null;
        if (deployer is not null)
        {
            var prepared = await RunDeployStepAsync(() => deployer.PrepareBuildAsync(project, request, startProject, logger, cancellationToken), logger);
            if (cancellationToken.IsCancellationRequested)
                return (Canceled(project.Name, logger), null);
            if (prepared is null)
                return (GameBuildOutcome.Failed, null);
            request = prepared;
        }

        var (outcome, build) = await BuildProjectAsync(project.Name, request, logger);
        if (outcome != GameBuildOutcome.Succeeded || !startProject)
            return (outcome, null);
        if (cancellationToken.IsCancellationRequested)
            return (Canceled(project.Name, logger), null);

        if (start is not null || deployer is not null)
        {
            var started = await RunDeployStepAsync(() => start is not null ? start(project, logger, cancellationToken) : deployer!.DeployAsync(project, build!, logger, cancellationToken), logger);
            if (cancellationToken.IsCancellationRequested)
                return (Canceled(project.Name, logger), null);
            if (!started)
                return (GameBuildOutcome.StartFailed, null);
            if (deployer is not null)
                logger.Info(string.Format(Tr._p("Message", "Deployment of {0} successful."), project.Name));
            return (GameBuildOutcome.Succeeded, null);
        }

        // An iOS game is signed and installed by Xcode
        if (project.Platform == PlatformType.iOS)
        {
            logger.Warning(string.Format(Tr._p("Message", "{0} was built: the editor doesn't deploy iOS games, start it from your IDE."), project.Name));
            return (GameBuildOutcome.Succeeded, null);
        }

        var game = StartGame(project, build!.AssemblyPath, logger);
        return (game is not null ? GameBuildOutcome.Succeeded : GameBuildOutcome.StartFailed, game);
    }

    /// <summary>
    /// Runs a step of a deployment or of a start, and logs the exception it throws, unless the step was canceled.
    /// </summary>
    private static async Task<T?> RunDeployStepAsync<T>(Func<Task<T>> step, ILogger logger)
    {
        try
        {
            return await step();
        }
        catch (OperationCanceledException)
        {
            return default;
        }
        catch (Exception e)
        {
            logger.Error(Tr._p("Message", "An exception occurred during the deployment."), e);
            return default;
        }
    }

    /// <summary>
    /// Builds a project, unless the build was cancelled already, and logs how it went.
    /// </summary>
    /// <param name="name">The name of the project in the log.</param>
    /// <param name="request">The build.</param>
    /// <param name="logger">The log of the build, in the Output pane.</param>
    /// <returns>How the build went, and the build itself when it succeeded.</returns>
    private async Task<(GameBuildOutcome Outcome, IGameProjectBuild? Build)> BuildProjectAsync(string name, GameProjectBuildRequest request, LoggerResult logger)
    {
        try
        {
            if (cancellation.IsCancellationRequested)
                return (Canceled(name, logger), null);

            logger.Info(string.Format(Tr._p("Message", "Building {0}..."), name));
            var builder = ServiceProvider.Get<IGameProjectBuilder>();
            // Loading the project evaluates it, which can take a while
            var build = await Task.Run(() => builder.StartBuild(request, logger));
            if (build is null)
            {
                logger.Error(string.Format(Tr._p("Message", "Unable to load and compile project {0}"), request.ProjectPath));
                return (GameBuildOutcome.Failed, null);
            }

            currentBuild = build;
            if (cancellation.IsCancellationRequested)
                build.Cancel();
            var succeeded = await build.Completion;
            currentBuild = null;
            if (build.IsCanceled)
                return (Canceled(name, logger), null);
            if (!succeeded || logger.HasErrors)
            {
                var errorCount = logger.Messages.Count(x => x.IsAtLeast(LogMessageType.Error));
                logger.Error(errorCount > 0
                    ? string.Format(Tr._p("Message", "Build of {0} failed with {1} error(s)."), name, errorCount)
                    : string.Format(Tr._p("Message", "Build of {0} failed."), name));
                return (GameBuildOutcome.Failed, null);
            }

            logger.Info(string.Format(Tr._p("Message", "Build of {0} succeeded."), name));
            return (GameBuildOutcome.Succeeded, build);
        }
        catch (Exception e)
        {
            logger.Error(Tr._p("Message", "An exception occurred during compilation."), e);
            await Dialogs.MessageBoxAsync(string.Format(Tr._p("Message", "An exception occurred while compiling the project: {0}"), e.FormatSummary(true)), MessageBoxButton.OK, MessageBoxImage.Information);
            return (GameBuildOutcome.Failed, null);
        }
    }

    private static GameBuildOutcome Canceled(string name, ILogger logger)
    {
        logger.Warning(string.Format(Tr._p("Message", "Build of {0} cancelled."), name));
        return GameBuildOutcome.Canceled;
    }

    /// <summary>
    /// Starts the game that was just built, when it runs on this machine, and reports its exit in the Output pane.
    /// </summary>
    /// <returns>The game, or <c>null</c> when it did not start.</returns>
    private IGameProcess? StartGame(ProjectViewModel project, string assemblyPath, LoggerResult logger)
    {
        // The games of the other platforms go to a device or another machine, through their deploy step (none registered for this one)
        if (project.Platform != Platform.Type)
        {
            logger.Error(string.Format(Tr._p("Message", "{0} is a {1} game: this editor can only start the games of its own platform ({2})."), project.Name, project.Platform, Platform.Type));
            return null;
        }

        // The game's own output is logged under its name, and only kept in the Output pane
        var output = new GameOutputLogger(project.Name);
        IGameProcess game;
        try
        {
            game = ServiceProvider.Get<IGameLauncher>().Start(assemblyPath, output);
        }
        catch (Exception e)
        {
            logger.Error(string.Format(Tr._p("Message", "Unable to start {0}."), assemblyPath), e);
            return null;
        }

        gameOutputs.Add(output);
        BuildLog.AddLogger(output);
        logger.Info(string.Format(Tr._p("Message", "{0} started (process {1})."), project.Name, game.Id));
        ReportExitAsync(project.Name, game, output).Forget();
        return game;
    }

    private async Task ReportExitAsync(string name, IGameProcess game, Logger output)
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

        // Before the build starts (while the session saves), it does not start at all; a deploy step stops
        cancellation.Cancel();
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
    /// The project was built but its game could not be deployed or start.
    /// </summary>
    StartFailed,
}

/// <summary>
/// How a build of <see cref="BuildAndRunViewModel.RunAsync"/> went, and the game it started on this machine.
/// </summary>
/// <param name="Outcome">How the build went.</param>
/// <param name="Game">The game started on this machine; <c>null</c> when none was, or when it was deployed elsewhere or started another way.</param>
internal readonly record struct GameRunResult(GameBuildOutcome Outcome, IGameProcess? Game);

/// <summary>
/// How a build of <see cref="BuildAndRunViewModel.BuildCodeAssembliesAsync"/> went.
/// </summary>
/// <param name="Outcome">How the builds went: the outcome of the first one that did not succeed.</param>
/// <param name="AssemblyPaths">The assembly of each project that was built, by project file.</param>
internal sealed record GameCodeBuildResult(GameBuildOutcome Outcome, IReadOnlyDictionary<string, string> AssemblyPaths);

/// <summary>
/// The output of a game started from the editor: it keeps no message of its own, the Output pane (<see cref="LoggerViewModel"/>)
/// shows them as they come.
/// </summary>
internal sealed class GameOutputLogger : Logger
{
    public GameOutputLogger(string module)
    {
        Module = module;
        ActivateLog(LogMessageType.Debug);
    }

    /// <inheritdoc/>
    protected override void LogRaw(ILogMessage logMessage)
    {
    }
}
