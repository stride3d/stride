// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls.Recycling;
using Avalonia.Input;
using Dock.Avalonia.Controls;
using Stride.Core.Presentation.Avalonia.Commands;
using Stride.GameStudio.Avalonia.ViewModels;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Docking;

/// <summary>
/// A floating window of panels or editors. The shortcuts of the main window work in it: the shortcuts of the commands that the
/// editor and the plugins register (Save, Build, Start...), and the Edit commands, which go to the focused panel of this window,
/// and to the session's Undo and Redo when the panel does not handle them, then a key binding added to the main window.
/// </summary>
public sealed class StrideHostWindow : HostWindow
{
    private readonly MainWindow? mainWindow;
    private EditCommandRouter? editRouter;

    /// <param name="mainWindow">The main window, whose shortcuts this window keeps; null outside the editor (a test of the layout alone).</param>
    public StrideHostWindow(MainWindow? mainWindow)
    {
        this.mainWindow = mainWindow;
        if (mainWindow is not null)
        {
            Icon = mainWindow.Icon;
            // The fallback of the Edit commands, as the main view's: Undo and Redo of the session
            var bindings = EditCommands.GetBindings(this);
            foreach (var binding in EditCommands.GetBindings(mainWindow.View))
                bindings.Add(new EditCommandBinding { Kind = binding.Kind, Command = binding.Command, CommandParameter = binding.CommandParameter });
            // The views of the panels and editors are those of the main window: they move with their panel
            if (mainWindow.DataContext is MainViewModel mainViewModel)
                ControlRecyclingDataTemplate.SetControlRecycling(this, mainViewModel.Panels.Views);
        }
        editRouter = new EditCommandRouter(this);
    }

    /// <summary>
    /// Routes the Edit commands of this window to its focused panel.
    /// </summary>
    public EditCommandRouter? EditRouter => editRouter;

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        // After the focused control, as in the main window: a text box keeps its own Ctrl+Z and Ctrl+C. The shortcuts of the
        // commands, the Edit commands acting on this window's focused panel, then the platform's other Edit shortcuts
        if (e.Handled)
            return;
        if (mainWindow?.View.CommandSlots is { } slots)
        {
            if (!slots.TryHandleKey(e))
                editRouter?.TryHandleOtherKey(e);
        }
        else
        {
            editRouter?.TryHandleKey(e);
        }
        if (e.Handled || mainWindow is null)
            return;

        // A key binding that something added to the main window at run time (the editor itself has none)
        foreach (var binding in mainWindow.KeyBindings)
        {
            if (binding.Gesture is not { } gesture || !gesture.Matches(e) || binding.Command is not { } command)
                continue;
            if (command.CanExecute(binding.CommandParameter))
            {
                command.Execute(binding.CommandParameter);
                e.Handled = true;
            }
            return;
        }
    }

    /// <inheritdoc />
    protected override void OnClosed(EventArgs e)
    {
        editRouter?.Dispose();
        editRouter = null;
        base.OnClosed(e);
    }
}
