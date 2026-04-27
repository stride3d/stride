// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MarkView.Avalonia;
using MarkView.Avalonia.Rendering;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Launcher.ViewModels;
using Stride.Launcher.Views;

namespace Stride.Launcher;

public partial class App : Application
{
    internal readonly CancellationTokenSource cts = new();

    internal MainWindow? MainWindow { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        InitializeMarkdownViewer();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = MainWindow = new()
            {
                DataContext = InitializeMainViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            // don't remove; also used by visual designer.
            singleViewPlatform.MainView = new MainView
            {
                DataContext = InitializeMainViewModel()
            };
        }
    }

    private static MainViewModel InitializeMainViewModel()
    {
        return new(InitializeServiceProvider());
    }

    private static void InitializeMarkdownViewer()
    {
        // Global pipeline — applies to every MarkdownViewer in the app
        MarkdownViewerDefaults.Pipeline = new Markdig.MarkdownPipelineBuilder()
            .UseSupportedExtensions()
            .UseAbbreviations()
            .UseAlertBlocks()
            .UseFigures()
            .UseFootnotes()
            .UseMediaLinks()
            .Build();

        // Global extensions — applies to every MarkdownViewer in the app
        MarkdownViewerDefaults.Extensions.AddTextMateHighlighting();
        MarkdownViewerDefaults.Extensions.AddSvg();
        MarkdownViewerDefaults.Extensions.AddMermaid();

        // Global link handler — handles external links for every MarkdownViewer in the app
        MarkdownViewer.LinkClickedEvent.AddClassHandler<MarkdownViewer>(OnLinkClicked);

        static void OnLinkClicked(MarkdownViewer sender, LinkClickedEventArgs e)
        {
            try
            {
                var url = e.Url.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                    ? e.Url[..^3] + ".html"
                    : e.Url;
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
                // Ignore failures to open browser
            }
        }
    }

    private static ViewModelServiceProvider InitializeServiceProvider()
    {
        var dispatcherService = DispatcherService.Create();
        var services = new object[]
        {
            dispatcherService,
            new DialogService(dispatcherService) { ApplicationName = Launcher.ApplicationName }
        };
        return new ViewModelServiceProvider(services);
    }
}

// This app is used for the crash report or for the notification when an instance is already running
internal sealed class MinimalApp : App
{
    public override void OnFrameworkInitializationCompleted() { }
}
