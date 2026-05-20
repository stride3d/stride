// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Text;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using xunit.runner.stride.ViewModels;
using xunit.runner.stride.Views;

namespace xunit.runner.stride;

public partial class App : Application
{
    internal readonly CancellationTokenSource cts = new();
    internal Action<bool>? setInteractiveMode;
    internal Action<bool>? setForceSaveImage;
    internal Action<string?>? setRenderDocMode;
    internal Action<Action<ImageCompareResult>>? subscribeImageComparison;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = new MainViewModel
            {
                Tests =
                {
                    SetInteractiveMode = setInteractiveMode,
                    SetForceSaveImage = setForceSaveImage,
                    SetRenderDocMode = setRenderDocMode,
                }
            };
            // Subscribe once for the process lifetime; the launcher wires this to
            // ImageTester.ImageComparisonCompleted.
            subscribeImageComparison?.Invoke(vm.Tests.OnImageComparison);
            desktop.MainWindow = new MainWindow { Title = ComputeWindowTitle(), DataContext = vm };
            desktop.MainWindow.Closed += (_, __) => cts.Cancel();
            desktop.MainWindow.Show();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            // don't remove; also used by visual designer.
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel
                {
                    Tests =
                    {
                        SetInteractiveMode = setInteractiveMode,
                        SetForceSaveImage = setForceSaveImage,
                        SetRenderDocMode = setRenderDocMode,
                    }
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    // Build a window title that identifies what this runner instance is testing:
    //   <TestAssembly> — <GraphicsApi> — Stride <Version> [<repoDir>]
    // Pieces are dropped when they can't be resolved so the title stays meaningful even when
    // launched from an unusual layout (NuGet consumer, etc.).
    static string ComputeWindowTitle()
    {
        const string Fallback = "XUnit Runner (Stride)";
        var asm = Assembly.GetEntryAssembly();
        if (asm is null) return Fallback;

        var testName = asm.GetName().Name ?? "tests";
        var path = asm.Location;
        var api = InferGraphicsApi(path);
        var version = InferStrideVersion();
        var repo = InferRepoName(path);

        var sb = new StringBuilder(testName);
        if (api is not null) sb.Append(" — ").Append(api);
        if (version is not null) sb.Append(" — Stride ").Append(version);
        if (repo is not null) sb.Append("  [").Append(repo).Append(']');
        return sb.ToString();
    }

    // Bin layout for tests is bin/Tests/<TestProject>/<Platform>-<Api>/<Config>/<asm>.dll, so
    // walk upward looking for the "<Platform>-<Api>" segment.
    static string? InferGraphicsApi(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var dir = Path.GetDirectoryName(path);
        while (!string.IsNullOrEmpty(dir))
        {
            var name = Path.GetFileName(dir);
            int dash = name.IndexOf('-');
            if (dash > 0 && (name.StartsWith("Windows-") || name.StartsWith("macOS-")
                || name.StartsWith("Linux-") || name.StartsWith("Android-")
                || name.StartsWith("iOS-")))
                return name[(dash + 1)..];
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    // Pick up the Stride engine assembly's informational version (the dev1/dev2 suffix is
    // useful, plain Version drops it). The Stride assembly is loaded by the entry assembly
    // during test discovery, which happens before the window is shown.
    static string? InferStrideVersion()
    {
        var strideAsm = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Stride");
        if (strideAsm is null) return null;
        var info = strideAsm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return info ?? strideAsm.GetName().Version?.ToString();
    }

    // Walk up from the binary looking for a .git entry (worktrees use a .git FILE, not a
    // directory). The containing directory name is the repo "name" — works for renamed
    // clones and worktrees alike (e.g. "stride2", "stride-worktree1").
    static string? InferRepoName(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var dir = Path.GetDirectoryName(path);
        while (!string.IsNullOrEmpty(dir))
        {
            var gitPath = Path.Combine(dir, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
                return Path.GetFileName(dir);
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }
}
