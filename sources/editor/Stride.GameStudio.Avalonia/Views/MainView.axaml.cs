// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Stride.Core.Presentation.Avalonia.Commands;

namespace Stride.GameStudio.Avalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        EditRouter = new EditCommandRouter(this);
        // The items of the Edit menu run the router's commands. They are set here rather than bound in XAML: a binding to an
        // ancestor of the item resolves only while its menu is open, and the item is disabled until then.
        UndoMenuItem.Command = EditRouter.Undo;
        RedoMenuItem.Command = EditRouter.Redo;
        CutMenuItem.Command = EditRouter.Cut;
        CopyMenuItem.Command = EditRouter.Copy;
        PasteMenuItem.Command = EditRouter.Paste;
        DeleteMenuItem.Command = EditRouter.Delete;
        RenameMenuItem.Command = EditRouter.Rename;
    }

    /// <summary>
    /// Routes the commands of the Edit menu and their shortcuts to the panel that has the focus.
    /// </summary>
    public EditCommandRouter EditRouter { get; }

    /// <summary>
    /// Gets a platform-specific <see cref="KeyGesture"/> for the Save action: Ctrl+S, or Cmd+S on macOS.
    /// </summary>
    public static KeyGesture SaveGesture => new(Key.S, Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control);

    /// <summary>
    /// The shortcut of File > New (Ctrl+N), bound on the main window like <see cref="SaveGesture"/>.
    /// </summary>
    public static KeyGesture NewGesture => new(Key.N, Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control);

    /// <summary>
    /// The shortcut of Project > Build project (F6, as in WPF), bound on the main window like <see cref="SaveGesture"/>.
    /// </summary>
    public static KeyGesture BuildProjectGesture => new(Key.F6);

    /// <summary>
    /// The shortcut of Project > Start project (F5, as in WPF), bound on the main window like <see cref="SaveGesture"/>.
    /// </summary>
    public static KeyGesture StartProjectGesture => new(Key.F5);

    /// <summary>
    /// The shortcut of Project > Cancel build (Ctrl+F6, as in WPF), bound on the main window like <see cref="SaveGesture"/>.
    /// </summary>
    public static KeyGesture CancelBuildGesture => new(Key.F6, Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control);
}
