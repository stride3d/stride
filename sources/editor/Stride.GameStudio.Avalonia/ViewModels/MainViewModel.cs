// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
//using Microsoft.Build.Utilities;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;
using Stride.GameStudio.Avalonia.Services;

namespace Stride.GameStudio.Avalonia.ViewModels;

internal sealed class MainViewModel : ViewModelBase, IMainViewModel
{
    private static readonly string baseTitle = $"Stride Game Studio {StrideVersion.NuGetVersion} ({RuntimeInformation.FrameworkDescription})";
    private SessionViewModel? session;
    private string title = baseTitle;

#if DEBUG
    // Note: only required for the Avalonia designer
    public MainViewModel()
        : this(ViewModelServiceProvider.NullServiceProvider)
    { }
#endif

    public MainViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        AboutCommand = new AnonymousTaskCommand(serviceProvider, OnAbout, () => DialogService.HasMainWindow);
        CloseCommand = new AnonymousCommand(serviceProvider, OnClose);
#if DEBUG
        CrashCommand = new AnonymousCommand(serviceProvider, () => throw new Exception("Boom!"));
#else
        CrashCommand = DisabledCommand.Instance;
#endif
        ExitCommand = new AnonymousCommand(serviceProvider, OnExit, () => DialogService.HasMainWindow);
        OpenCommand = new AnonymousTaskCommand<UFile?>(serviceProvider, OnOpen);
        OpenDebugWindowCommand = new AnonymousTaskCommand(serviceProvider, OnOpenDebugWindow, () => DialogService.HasMainWindow);
        OpenSettingsWindowCommand = new AnonymousTaskCommand(serviceProvider, OnOpenSettingsWindow, () => DialogService.HasMainWindow);
        OpenWebPageCommand = new AnonymousTaskCommand<string>(serviceProvider, OnOpenWebPage);
        RunCurrentProjectCommand = new AnonymousTaskCommand(serviceProvider, RunCurrentProject);

        Status = new StatusViewModel(ServiceProvider);
        Status.PushStatus("Ready");
    }

    public SessionViewModel? Session
    {
        get => session;
        set => SetValue(ref session, value);
    }

    public StatusViewModel Status { get; }

    public string Title
    {
        get => title;
        set => SetValue(ref title, value);
    }

    public ICommandBase AboutCommand { get; }

    public ICommandBase CloseCommand { get; }

    public ICommandBase CrashCommand { get; }

    public ICommandBase ExitCommand { get; }

    public ICommandBase OpenCommand { get; }

    public ICommandBase OpenDebugWindowCommand { get; }

    public ICommandBase OpenSettingsWindowCommand { get; }

    public ICommandBase OpenWebPageCommand { get; }

    private EditorDialogService DialogService => ServiceProvider.Get<EditorDialogService>();

    public ICommandBase RunCurrentProjectCommand { get; }

    public async Task<bool?> OpenSession(UFile? filePath, CancellationToken token = default)
    {
        if (filePath == null || !File.Exists(filePath))
        {
            filePath = await DialogService.OpenFilePickerAsync();
        }

        if (filePath == null) return false;

        // We have a session, let's restart cleanly
        if (session is not null)
        {
            session = null;
            (Application.Current as App)?.Restart(filePath);
            return true;
        }

        var sessionResult = new PackageSessionResult();
        var loadedSession = await SessionViewModel.OpenSessionAsync(filePath, sessionResult, this, ServiceProvider, token);

        // Loading has failed
        if (loadedSession == null)
        {
            if (sessionResult.OperationCancelled)
            {
                // The cancelled session might have registered plugins or services, let's restart cleanly
                (Application.Current as App)?.Restart();

                // Null means the user has cancelled the loading operation.
                return null;
            }
            return false;
        }

        Session = loadedSession;
        Title = $"{baseTitle} - {Session.SolutionPath.GetFileNameWithoutExtension()}";
        return true;
    }

    private async Task OnAbout()
    {
        await DialogService.ShowAboutWindowAsync();
    }

    private void OnClose()
    {
        // We have a session, let's restart empty
        if (session is not null && Application.Current is App app)
        {
            session = null;
            app.Restart();
        }
    }

    private void OnExit()
    {
        DialogService.Exit();
    }

    private bool BuildProject(string projectPath, string framework, string workingDirectory)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{projectPath}\" --framework {framework}",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            }
        };

        process.OutputDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine("[build] " + e.Data); };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) Console.Error.WriteLine("[build-err] " + e.Data); };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Build process failed: " + ex);
            return false;
        }
    }



    private async Task RunCurrentProject()
    {
        var mainProjectPath = Session?.CurrentProject?.RootDirectory;
        if (mainProjectPath == null) return;

        var projectDir = Path.GetDirectoryName(mainProjectPath.FullPath);
        var projectBaseName = Path.GetFileNameWithoutExtension(mainProjectPath.FullPath);

        string platformSuffix, framework, platformRuntime;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            platformSuffix = "Windows";
            platformRuntime = "win-x64";
            framework = "net8.0-windows";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            platformSuffix = "Linux";
            platformRuntime = "linux-x64";
            framework = "net8.0-linux";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            platformSuffix = "macOS";
            platformRuntime = "osx-x64";
            framework = "net8.0-macos";
        }
        else
        {
            await ShowError("Unsupported OS platform");
            return;
        }

        var platformProjectName = $"{projectBaseName}.{platformSuffix}.csproj";
        var platformProjectPath = Path.Combine(projectDir, $"{projectBaseName}.{platformSuffix}", platformProjectName);
        var ExecPath = Path.Combine(projectDir, "Bin", platformSuffix, "Debug", platformRuntime);
        var dllPath = Path.Combine(ExecPath, $"{projectBaseName}.{platformSuffix}.dll");


        if (!File.Exists(platformProjectPath))
        {
            await ShowError($"Platform-specific project not found: {platformProjectPath}");
            return;
        }

        Status.PushStatus("Building project...");
        Console.WriteLine("Building project...");
        bool buildSuccess = await Task.Run(() => BuildProject(platformProjectPath, framework, projectDir));
        if (!buildSuccess)
        {
            Status.PushStatus("Build failed.");
            Console.WriteLine("Build failed.");
            await ShowError("Build failed. See output for details.");
            return;
        }

        Status.PushStatus("Running project...");
        Console.WriteLine("Running project...");
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{dllPath}\"",
                WorkingDirectory = projectDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false,
            }
        };

        process.OutputDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine("[run] " + e.Data); };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) Console.Error.WriteLine("[run-err] " + e.Data); };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Run process failed: " + ex);
            await ShowError("Failed to start the game process. See output for details.");
        }
    }




    private async Task ShowError(string message)
    {
        await ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Error);
    }



    private async Task OnOpen(UFile? initialPath)
    {
        await OpenSession(initialPath);
    }

    private async Task OnOpenDebugWindow()
    {
        await DialogService.ShowDebugWindowAsync();
    }

    private async Task OnOpenSettingsWindow()
    {
        await DialogService.ShowSettingsWindowAsync();
    }

    private async Task OnOpenWebPage(string url)
    {
        try
        {
            var process = new Process { StartInfo = new ProcessStartInfo(url) { UseShellExecute = true } };
            process.Start();
        }
        catch (Exception ex)
        {
            var message = $"{Tr._p("Message", "An error occurred while opening the file.")}{ex.FormatSummary(true)}";
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
