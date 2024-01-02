// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Launcher.Assets.Localization;

namespace Stride.Launcher.ViewModels;

public sealed class MainViewModel : DispatcherViewModel
{
    private AnnouncementViewModel? announcement;
    private bool isOffline = true;

    public MainViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        OpenUrlCommand = new AnonymousTaskCommand<string>(ServiceProvider, OpenUrl);
        ReconnectCommand = new AnonymousTaskCommand(ServiceProvider, async () =>
        {
            // We are back online (or so we think)
            IsOffline = false;
            await FetchOnlineData();
        });
    }

    public AnnouncementViewModel? Announcement
    {
        get => announcement;
        set => SetValue(ref announcement, value);
    }

    public string CurrentToolTip { get; } = Strings.ToolTipDefault;

    public bool IsOffline
    {
        get => isOffline;
        set => SetValue(ref isOffline, value);
    }

    public ICommandBase OpenUrlCommand { get; }

    public ICommandBase ReconnectCommand { get; }

    private async Task FetchOnlineData()
    {
        // TODO
        await Task.Delay(2000);
        IsOffline = true;
    }

    private async Task OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        // FIXME: catch only specific exceptions?
        catch (Exception)
        {
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync(Strings.ErrorOpeningBrowser, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
