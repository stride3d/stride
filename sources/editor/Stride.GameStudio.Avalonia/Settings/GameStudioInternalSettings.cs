// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Stride.Core.Assets.Editor;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.IO;
using Stride.Core.MostRecentlyUsedFiles;
using Stride.Core.Presentation.Avalonia.Extensions;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Settings;
using Stride.Core.Yaml;

namespace Stride.GameStudio.Avalonia.Settings;

/// <summary>
/// The internal (not user-facing) settings of the Avalonia Game Studio, in the same file as the WPF Game Studio's: the main
/// window placement and the panel layouts of the sessions, under keys of its own. The recent sessions are shared with WPF
/// (<see cref="RecentSessionsSettings"/>).
/// </summary>
internal static class GameStudioInternalSettings
{
    public static SettingsContainer SettingsContainer => InternalSettings.SettingsContainer;

    /// <summary>
    /// The value of <see cref="WindowLeft"/> and <see cref="WindowTop"/> before the window was ever closed.
    /// </summary>
    public const int NoPosition = int.MinValue;

    // The window and the panels of this editor have their own keys: the other editors keep their window under Internal/Window*
    // and their panels in the docking layouts of each session, so no editor changes another's
    public static readonly SettingsKey<bool> WindowMaximized = new("Internal/Avalonia/WindowMaximized", SettingsContainer, false);
    public static readonly SettingsKey<int> WindowWidth = new("Internal/Avalonia/WindowWidth", SettingsContainer, (int)WorkArea.Value.Width);
    public static readonly SettingsKey<int> WindowHeight = new("Internal/Avalonia/WindowHeight", SettingsContainer, (int)WorkArea.Value.Height);
    /// <summary>
    /// The position of the window, in pixels of the desktop; <see cref="NoPosition"/> centers it.
    /// </summary>
    public static readonly SettingsKey<int> WindowLeft = new("Internal/Avalonia/WindowLeft", SettingsContainer, NoPosition);
    /// <inheritdoc cref="WindowLeft"/>
    public static readonly SettingsKey<int> WindowTop = new("Internal/Avalonia/WindowTop", SettingsContainer, NoPosition);
    public static readonly SettingsKey<int> WorkAreaWidth = new("Internal/Avalonia/WorkAreaWidth", SettingsContainer, (int)WorkArea.Value.Width);
    public static readonly SettingsKey<int> WorkAreaHeight = new("Internal/Avalonia/WorkAreaHeight", SettingsContainer, (int)WorkArea.Value.Height);

    /// <summary>
    /// The panel layouts of the recent sessions (with and without editors), in the editor's own format: the other editors
    /// keep their docking layouts in <see cref="RecentSessionsSettings.MostRecentlyUsedSessionsData"/>, and never read this key.
    /// </summary>
    /// <remarks>The key <c>Internal/Avalonia/PanelSizes</c> of earlier versions is no longer read.</remarks>
    public static readonly SettingsKey<List<SessionPanelLayouts>> SessionLayouts = new("Internal/Avalonia/SessionLayouts", SettingsContainer, () => new List<SessionPanelLayouts>());

    static GameStudioInternalSettings()
    {
        // Written at once by each change (SessionPanelLayoutStore); another editor instance may have changed it since
        InternalSettings.KeepFileValueOnSave(SessionLayouts);
    }

    /// <summary>
    /// Creates the store of the panel layouts of the recent sessions, over the settings file.
    /// </summary>
    public static SessionPanelLayoutStore CreateSessionPanelLayouts()
    {
        return new SessionPanelLayoutStore(InternalSettings.LoadProfileCopy, SessionLayouts, InternalSettings.SaveProfile);
    }

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
