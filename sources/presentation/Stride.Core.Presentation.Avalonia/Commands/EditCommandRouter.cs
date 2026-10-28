// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Stride.Core.Presentation.Avalonia.Commands;

/// <summary>
/// Routes the Edit commands (Cut, Copy, Paste, Delete, Rename, Undo and Redo) to the panel that has the focus, as the routed
/// commands of WPF do: each command walks up from the focused control to the first control that handles it, a text box or a
/// control with an <see cref="EditCommands.BindingsProperty"/> for that command.
/// </summary>
/// <remarks>
/// <para>Avalonia has no routed commands. The router remembers the last focused control of its root that is not in a menu or a
/// popup, so that an item of the Edit menu, which takes the focus when its menu opens, still acts on the panel the user was in.</para>
/// <para>The shortcuts are handled by <see cref="TryHandleKey"/>, which the window calls from its key handler, after the focused
/// control: a key binding of the window would run before a text box sees its own Ctrl+C.</para>
/// <para>This is a small, local mechanism; a command registry shared by the editors may replace it later.</para>
/// </remarks>
public sealed class EditCommandRouter : IDisposable
{
    private readonly InputElement root;
    private readonly RoutedEditCommand[] commands;
    private readonly List<ICommand> watchedCommands = [];
    private Visual? target;
    private bool disposed;

    /// <summary>
    /// Creates a router for the controls under <paramref name="root"/>.
    /// </summary>
    /// <param name="root">The control whose descendants the router follows; its own bindings are the fallback of every command.</param>
    public EditCommandRouter(InputElement root)
    {
        ArgumentNullException.ThrowIfNull(root);
        this.root = root;
        commands = [.. Enum.GetValues<EditCommandKind>().Select(kind => new RoutedEditCommand(this, kind))];
        root.AddHandler(InputElement.GotFocusEvent, OnGotFocus, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <summary>
    /// The control the Edit commands act on: the last focused control of the root outside its menus and popups, or the root
    /// itself when there is none or it has gone.
    /// </summary>
    public Visual Target => target is not null && target.IsAttachedToVisualTree() ? target : root;

    /// <summary>
    /// The command the Edit menu and the shortcuts bind for an Edit command.
    /// </summary>
    public ICommand this[EditCommandKind kind] => commands[(int)kind];

    public ICommand Undo => this[EditCommandKind.Undo];

    public ICommand Redo => this[EditCommandKind.Redo];

    public ICommand Cut => this[EditCommandKind.Cut];

    public ICommand Copy => this[EditCommandKind.Copy];

    public ICommand Paste => this[EditCommandKind.Paste];

    public ICommand Delete => this[EditCommandKind.Delete];

    public ICommand Rename => this[EditCommandKind.Rename];

    /// <summary>
    /// Finds the handler of an Edit command: the first control, from <see cref="Target"/> up, that handles it.
    /// </summary>
    /// <returns>The handler, or null when no control handles the command (it is disabled).</returns>
    public IEditCommandHandler? Resolve(EditCommandKind kind)
    {
        for (var visual = Target; visual is not null; visual = visual.GetVisualParent())
        {
            if (visual is TextBox textBox)
            {
                var handler = new TextBoxEditCommandHandler(textBox);
                if (handler.Handles(kind))
                    return handler;
                continue;
            }
            if (visual is IEditCommandHandler control && control.Handles(kind))
                return control;
            if (visual is Control { } owner && owner.GetValue(EditCommands.BindingsProperty) is { } bindings)
            {
                foreach (var binding in bindings)
                {
                    if (binding.Handles(kind))
                        return binding;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Executes the Edit command whose shortcut the key is, if its handler can.
    /// </summary>
    /// <returns><c>true</c> when the key was handled.</returns>
    public bool TryHandleKey(KeyEventArgs e)
    {
        if (e.Handled)
            return false;

        foreach (var kind in Enum.GetValues<EditCommandKind>())
        {
            if (!EditCommands.GesturesOf(kind).Any(gesture => gesture.Matches(e)))
                continue;

            var handler = Resolve(kind);
            if (handler is null || !handler.CanExecute(kind))
                return false;

            handler.Execute(kind);
            e.Handled = true;
            return true;
        }
        return false;
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        root.RemoveHandler(InputElement.GotFocusEvent, OnGotFocus);
        WatchResolvedCommands(false);
    }

    private void OnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.Source is not Visual visual || ReferenceEquals(visual, target))
            return;
        // A menu item takes the focus when its menu opens, and gives it back when the menu closes: the panel the user was in
        // stays the target. The content of a popup (a context menu, a drop-down) is not a panel either.
        if (TopLevel.GetTopLevel(visual) is PopupRoot || visual.FindLogicalAncestorOfType<MenuBase>(includeSelf: true) is not null)
            return;

        target = visual;
        WatchResolvedCommands(true);
        RaiseCanExecuteChanged();
    }

    /// <summary>
    /// Follows the commands the Edit commands resolve to for the current target, so that their changes reach the Edit menu.
    /// </summary>
    private void WatchResolvedCommands(bool watch)
    {
        foreach (var command in watchedCommands)
        {
            command.CanExecuteChanged -= OnResolvedCanExecuteChanged;
        }
        watchedCommands.Clear();
        if (!watch)
            return;

        foreach (var kind in Enum.GetValues<EditCommandKind>())
        {
            if (Resolve(kind) is EditCommandBinding { Command: { } command } && !watchedCommands.Contains(command))
            {
                command.CanExecuteChanged += OnResolvedCanExecuteChanged;
                watchedCommands.Add(command);
            }
        }
    }

    private void OnResolvedCanExecuteChanged(object? sender, EventArgs e) => RaiseCanExecuteChanged();

    private void RaiseCanExecuteChanged()
    {
        foreach (var command in commands)
        {
            command.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// An Edit command as the Edit menu binds it: it resolves its handler when it is evaluated or executed.
    /// </summary>
    private sealed class RoutedEditCommand : ICommand
    {
        private readonly EditCommandRouter router;
        private readonly EditCommandKind kind;

        public RoutedEditCommand(EditCommandRouter router, EditCommandKind kind)
        {
            this.router = router;
            this.kind = kind;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => router.Resolve(kind)?.CanExecute(kind) ?? false;

        public void Execute(object? parameter) => router.Resolve(kind)?.Execute(kind);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// A text box keeps the commands it handles itself: it undoes, cuts, copies, pastes and deletes its own text.
    /// </summary>
    private sealed class TextBoxEditCommandHandler : IEditCommandHandler
    {
        private readonly TextBox textBox;

        public TextBoxEditCommandHandler(TextBox textBox)
        {
            this.textBox = textBox;
        }

        public bool Handles(EditCommandKind kind) => kind != EditCommandKind.Rename;

        public bool CanExecute(EditCommandKind kind)
        {
            return kind switch
            {
                EditCommandKind.Undo => textBox.CanUndo,
                EditCommandKind.Redo => textBox.CanRedo,
                EditCommandKind.Cut => textBox.CanCut,
                EditCommandKind.Copy => textBox.CanCopy,
                EditCommandKind.Paste => textBox.CanPaste,
                EditCommandKind.Delete => !textBox.IsReadOnly && !string.IsNullOrEmpty(textBox.SelectedText),
                _ => false,
            };
        }

        public void Execute(EditCommandKind kind)
        {
            switch (kind)
            {
                case EditCommandKind.Undo:
                    textBox.Undo();
                    break;
                case EditCommandKind.Redo:
                    textBox.Redo();
                    break;
                case EditCommandKind.Cut:
                    textBox.Cut();
                    break;
                case EditCommandKind.Copy:
                    textBox.Copy();
                    break;
                case EditCommandKind.Paste:
                    textBox.Paste();
                    break;
                case EditCommandKind.Delete:
                    textBox.SelectedText = string.Empty;
                    break;
            }
        }
    }
}
