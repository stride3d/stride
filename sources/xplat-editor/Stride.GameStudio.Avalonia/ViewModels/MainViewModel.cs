// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;
using Stride.GameStudio.Avalonia.Services;

namespace Stride.GameStudio.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private string? message;

    public MainViewModel()
    {
        var dispatcherService = DispatcherService.Create();
        var services = new object[]
        {
            dispatcherService,
        };
        ServiceProvider = new ViewModelServiceProvider(services);
        ServiceProvider.RegisterService(new EditorDialogService(ServiceProvider));

        AboutCommand = new AnonymousTaskCommand(ServiceProvider, OnAbout, () => Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime);
        ExitCommand = new AnonymousCommand(ServiceProvider, OnExit, () => Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime);
        OpenCommand = new AnonymousCommand(ServiceProvider, OnOpen);
    }

    public string? Message
    {
        get => message;
        set => SetValue(ref message, value);
    }

    public ICommandBase AboutCommand { get; }
    public ICommandBase ExitCommand { get; }
    public ICommandBase OpenCommand { get; }

    private EditorDialogService DialogService => ServiceProvider.Get<EditorDialogService>();

    private async Task OnAbout()
    {
        await DialogService.ShowAboutWindowAsync();
    }

    private void OnExit()
    {
        DialogService.Exit();
    }

    private void OnOpen()
    {
        Message = "Clicked on Open";
    }
}
