// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Presentation.Avalonia.Extensions;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Settings;

namespace Stride.GameStudio.Avalonia.Settings;

internal static class GameStudioInternalSettings
{
    public static SettingsContainer SettingsContainer => InternalSettings.SettingsContainer;

    public static readonly SettingsKey<bool> WindowMaximized = new("Internal/WindowMaximized", SettingsContainer, false);
    public static readonly SettingsKey<int> WindowWidth = new("Internal/WindowWidth", SettingsContainer, (int)WorkArea.Value.Width);
    public static readonly SettingsKey<int> WindowHeight = new("Internal/WindowHeight", SettingsContainer, (int)WorkArea.Value.Height);
    public static readonly SettingsKey<int> WorkAreaWidth = new("Internal/WorkAreaWidth", SettingsContainer, (int)WorkArea.Value.Width);
    public static readonly SettingsKey<int> WorkAreaHeight = new("Internal/WorkAreaHeight", SettingsContainer, (int)WorkArea.Value.Height);

    private static Lazy<Rect> WorkArea => new(() =>
    {
        if (DialogService.MainWindow is { } mainWindow)
        {
            return mainWindow.GetWorkingArea().area;
        }
        
        // fallback to a reasonable value
        return new Rect(0, 0, 800, 600);
    });
}
