// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Avalonia.Views;
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
        OpenWebPageCommand = new AnonymousTaskCommand<string>(serviceProvider, OnOpenWebPage);

        Status = new StatusViewModel(ServiceProvider);
        Status.PushStatus("Ready");

        // FIXME xplat-editor move to plugin
        foreach (var (_, value) in new DefaultPropertyTemplateProviders())
        {
            if (value is ITemplateProvider provider1)
            {
                RegisterDefaultTemplateProvider(provider1);
            }
        }

        return;

        void RegisterDefaultTemplateProvider(ITemplateProvider provider)
        {
            if (provider is not AvaloniaObject avaloniaObject)
                return;

            var category = PropertyViewHelper.GetTemplateCategory(avaloniaObject);
            switch (category)
            {
                case PropertyViewHelper.Category.PropertyHeader:
                    PropertyViewHelper.HeaderProviders.RegisterTemplateProvider(provider);
                    break;
                case PropertyViewHelper.Category.PropertyFooter:
                    PropertyViewHelper.FooterProviders.RegisterTemplateProvider(provider);
                    break;
                case PropertyViewHelper.Category.PropertyEditor:
                    PropertyViewHelper.EditorProviders.RegisterTemplateProvider(provider);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
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

    public ICommandBase OpenWebPageCommand { get; }

    private EditorDialogService DialogService => ServiceProvider.Get<EditorDialogService>();

    public ICommandBase OpenDebugWindowCommand { get; }

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

    private async Task OnOpen(UFile? initialPath)
    {
        await OpenSession(initialPath);
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

    private async Task OnOpenDebugWindow()
    {
        await DialogService.ShowDebugWindowAsync();
    }
}
