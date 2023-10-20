// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.GameStudio.Avalonia.Views;
using Stride.Core.Assets;
using Stride.Core.IO;

namespace Stride.GameStudio.Avalonia.ViewModels;

internal sealed class MainViewModel : ViewModelBase
{
    private string? message;
    private SessionViewModel? session;

    public MainViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        AboutCommand = new AsyncRelayCommand(OnAbout, () => DialogService.MainWindow != null);
        ExitCommand = new RelayCommand(OnExit, () => DialogService.MainWindow != null);
        OpenCommand = new AsyncRelayCommand(OnOpen);
    }

    public string? Message
    {
        get => message;
        set => SetProperty(ref message, value);
    }

    public SessionViewModel? Session
    {
        get => session;
        set => SetProperty(ref session, value);
    }

    public IRelayCommand AboutCommand { get; }

    public IRelayCommand ExitCommand { get; }

    public IRelayCommand OpenCommand { get; }

    public async Task<bool?> OpenSession(UFile? filePath, CancellationToken token = default)
    {
        if (session != null)
            throw new InvalidOperationException("A session is already open in this instance.");

        if (filePath == null || !File.Exists(filePath))
        {
            filePath = await ServiceProvider.Get<IDialogService>().OpenFilePickerAsync();
        }

        if (filePath == null) return false;

        var sessionResult = new PackageSessionResult();
        var loadedSession = await SessionViewModel.OpenSessionAsync(filePath, sessionResult, ServiceProvider, token);
        
        // Loading has failed
        if (loadedSession == null)
        {
            // Null means the user has cancelled the loading operation.
            return sessionResult.OperationCancelled ? null : false;
        }

        Session = loadedSession;
        return true;
    }

    private async Task OnAbout()
    {
        // FIXME: hide implementation details through a dialog service
        var window = new AboutWindow();
        await window.ShowDialog(((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).MainWindow!);
    }

    private void OnExit()
    {
        ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).TryShutdown();
    }

    private Task OnOpen()
    {
        return OpenSession(null);
    }
}
