// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Avalonia.ViewModels;
using Stride.Core.IO;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.GameStudio.Avalonia.Services;

namespace Stride.GameStudio.Avalonia.ViewModels;

internal sealed class MainViewModel : ViewModelBase
{
    private string? message;
    private SessionViewModel? session;

    public MainViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        AboutCommand = new AnonymousTaskCommand(ServiceProvider, OnAbout, () => Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime);
        ExitCommand = new AnonymousCommand(ServiceProvider, OnExit, () => Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime);
        OpenCommand = new AnonymousTaskCommand(ServiceProvider, OnOpen);
    }

    public string? Message
    {
        get => message;
        set => SetValue(ref message, value);
    }
    
    public SessionViewModel? Session
    {
        get => session;
        set => SetValue(ref session, value);
    }

    public ICommandBase AboutCommand { get; }

    public ICommandBase ExitCommand { get; }

    public ICommandBase OpenCommand { get; }

    private EditorDialogService DialogService => ServiceProvider.Get<EditorDialogService>();
    
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
        await DialogService.ShowAboutWindowAsync();
    }

    private void OnExit()
    {
        DialogService.Exit();
    }

    private Task OnOpen()
    {
        return OpenSession(null);
    }
}
