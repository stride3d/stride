// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Interactivity;
using Stride.Core.CodeEditorSupport.VisualStudio;
using Stride.Launcher.Services;

namespace Stride.Launcher.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void FrameworkChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FrameworkSelector.SelectedItem is string framework && LauncherSettings.PreferredFramework != framework)
        {
            LauncherSettings.PreferredFramework = framework;
            LauncherSettings.Save();
        }
    }

    private void VisualStudioDownloadPage_Button_Loaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && VisualStudioVersions.AvailableInstances
            .Any(ide => ide.InstallationVersion.Major == 16 || ide.InstallationVersion.Major == 17))
        {
            button.IsVisible = false;
        }
    }
}
