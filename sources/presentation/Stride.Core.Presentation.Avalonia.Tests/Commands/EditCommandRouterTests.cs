// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Stride.Core.Presentation.Avalonia.Commands;

namespace Stride.Core.Presentation.Avalonia.Tests.Commands;

/// <summary>
/// The routing of the Edit commands to the focused panel, as WPF routes its application commands.
/// </summary>
public sealed class EditCommandRouterTests
{
    private static (TestWindow Window, EditCommandsView View, EditCommandsViewModel ViewModel) Show()
    {
        var viewModel = new EditCommandsViewModel();
        var view = new EditCommandsView { DataContext = viewModel };
        var window = new TestWindow(view);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, view, viewModel);
    }

    [AvaloniaFact]
    public void ACommandGoesToTheFocusedPanel()
    {
        var (window, view, viewModel) = Show();
        try
        {
            // Nothing focused: only the bindings of the root
            Assert.False(view.Router.Copy.CanExecute(null));
            Assert.True(view.Router.Undo.CanExecute(null));

            Assert.True(view.Assets.Focus());
            Assert.True(view.Router.Copy.CanExecute(null));
            view.Router.Copy.Execute(null);
            Assert.Single(viewModel.CopyAssets.Executions);
            Assert.Empty(viewModel.CopyLocations.Executions);

            Assert.True(view.Explorer.Focus());
            view.Router.Copy.Execute(null);
            Assert.Single(viewModel.CopyLocations.Executions);
            Assert.Single(viewModel.CopyAssets.Executions);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ACommandAPanelDoesNotHandleGoesToItsAncestors()
    {
        var (window, view, viewModel) = Show();
        try
        {
            // The button is inside the assets panel, which has no Rename and no Undo: the root handles them, with the parameter of its binding
            Assert.True(view.AssetButton.Focus());
            Assert.True(view.Router.Rename.CanExecute(null));
            view.Router.Rename.Execute(null);
            Assert.Same(viewModel.RenameParameter, Assert.Single(viewModel.RenameAnywhere.Executions));
            view.Router.Undo.Execute(null);
            Assert.Single(viewModel.Undo.Executions);

            // Nobody handles Cut
            Assert.False(view.Router.Cut.CanExecute(null));
            view.Router.Cut.Execute(null);

            // A binding without a command is ignored: no ancestor handles Paste either
            Assert.False(view.Router.Paste.CanExecute(null));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ADisabledCommandDisablesTheEditCommand()
    {
        var (window, view, viewModel) = Show();
        try
        {
            Assert.True(view.Assets.Focus());
            var changes = 0;
            view.Router.Copy.CanExecuteChanged += (_, _) => changes++;

            viewModel.CopyAssets.IsEnabled = false;
            Assert.False(view.Router.Copy.CanExecute(null));
            Assert.Equal(1, changes);

            viewModel.CopyAssets.IsEnabled = true;
            Assert.True(view.Router.Copy.CanExecute(null));
            Assert.Equal(2, changes);

            // A change of focus is a change too
            Assert.True(view.Explorer.Focus());
            Assert.Equal(3, changes);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void AMenuDoesNotTakeTheTargetAway()
    {
        var (window, view, viewModel) = Show();
        try
        {
            Assert.True(view.Assets.Focus());
            Assert.Same(view.Assets, view.Router.Target);

            // The menu items take the focus when the menu opens
            view.EditMenu.Open();
            Dispatcher.UIThread.RunJobs();
            Assert.True(view.CopyMenuItem.Focus());
            Assert.Same(view.Assets, view.Router.Target);
            view.Router.Copy.Execute(null);
            Assert.Single(viewModel.CopyAssets.Executions);
            view.EditMenu.Close();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ATextBoxKeepsTheCommandsOnItsText()
    {
        var (window, view, viewModel) = Show();
        try
        {
            view.Filter.Text = "filter";
            Assert.True(view.Filter.Focus());
            view.Filter.SelectAll();

            // Delete acts on the selected text, not on the assets
            Assert.True(view.Router.Delete.CanExecute(null));
            view.Router.Delete.Execute(null);
            Assert.Equal("", view.Filter.Text);
            Assert.Empty(viewModel.DeleteAssets.Executions);

            // With nothing to cut, the command is disabled rather than passed to the panel
            Assert.False(view.Router.Cut.CanExecute(null));
            Assert.False(view.Router.Copy.CanExecute(null));
            Assert.True(view.Router.Paste.CanExecute(null));

            // The text box undoes its own text
            Assert.True(view.Router.Undo.CanExecute(null));
            view.Router.Undo.Execute(null);
            Assert.Equal("filter", view.Filter.Text);
            Assert.Empty(viewModel.Undo.Executions);

            // Rename is not a command of a text box: the root handles it
            Assert.True(view.Router.Rename.CanExecute(null));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheShortcutsReachTheFocusedPanelAfterTheFocusedControl()
    {
        var (window, view, viewModel) = Show();
        try
        {
            Assert.True(view.Assets.Focus());
            window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Control);
            window.KeyPressQwerty(PhysicalKey.Delete, RawInputModifiers.None);
            window.KeyPressQwerty(PhysicalKey.F2, RawInputModifiers.None);
            // Not a shortcut of a handled command
            window.KeyPressQwerty(PhysicalKey.X, RawInputModifiers.Control);
            Assert.Single(viewModel.CopyAssets.Executions);
            Assert.Single(viewModel.DeleteAssets.Executions);
            Assert.Single(viewModel.RenameAnywhere.Executions);
            Assert.Equal([Key.X], window.UnhandledKeys);

            // The text box handles Ctrl+C and Delete itself: the panel does not see them
            view.Filter.Text = "filter";
            Assert.True(view.Filter.Focus());
            view.Filter.SelectAll();
            window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Control);
            window.KeyPressQwerty(PhysicalKey.Delete, RawInputModifiers.None);
            Assert.Equal("", view.Filter.Text);
            Assert.Single(viewModel.CopyAssets.Executions);
            Assert.Single(viewModel.DeleteAssets.Executions);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheGesturesAreThoseOfThePlatform()
    {
        Assert.Equal(new KeyGesture(Key.X, KeyModifiers.Control), EditCommands.CutGesture);
        Assert.Equal(new KeyGesture(Key.C, KeyModifiers.Control), EditCommands.CopyGesture);
        Assert.Equal(new KeyGesture(Key.V, KeyModifiers.Control), EditCommands.PasteGesture);
        Assert.Equal(new KeyGesture(Key.Z, KeyModifiers.Control), EditCommands.UndoGesture);
        Assert.Equal(new KeyGesture(Key.Y, KeyModifiers.Control), EditCommands.RedoGesture);
        Assert.Equal(new KeyGesture(Key.Delete), EditCommands.DeleteGesture);
        Assert.Equal(new KeyGesture(Key.F2), EditCommands.RenameGesture);
    }

    /// <summary>
    /// A window that gives the router the keys its content left alone, as the main window of the editor does.
    /// </summary>
    private sealed class TestWindow : Window
    {
        private readonly EditCommandsView view;

        public TestWindow(EditCommandsView view)
        {
            this.view = view;
            Content = view;
            Width = 400;
            Height = 300;
        }

        public List<Key> UnhandledKeys { get; } = [];

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!view.Router.TryHandleKey(e))
                UnhandledKeys.Add(e.Key);
        }
    }
}
