// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModels;
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
        AboutCommand = new AnonymousTaskCommand(serviceProvider, OnAbout, () => DialogService.HasMainWindow);
        ExitCommand = new AnonymousCommand(serviceProvider, OnExit, () => DialogService.HasMainWindow);
        OpenCommand = new AnonymousTaskCommand(serviceProvider, OnOpen);
        OpenDebugWindowCommand = new AnonymousTaskCommand(serviceProvider, OnOpenDebugWindow, () => DialogService.HasMainWindow);
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
    
    public ICommandBase OpenDebugWindowCommand { get; }

    public async Task<bool?> OpenSession(UFile? filePath, CancellationToken token = default)
    {
        if (session != null)
            throw new InvalidOperationException("A session is already open in this instance.");

        if (filePath == null || !File.Exists(filePath))
        {
            filePath = await DialogService.OpenFilePickerAsync();
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

    private async Task OnOpenDebugWindow()
    {
        await DialogService.ShowDebugWindowAsync();
    }
}
