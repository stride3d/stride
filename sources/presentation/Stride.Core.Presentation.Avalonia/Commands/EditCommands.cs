// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Stride.Core.Presentation.Avalonia.Commands;

/// <summary>
/// Declares, on a panel, the commands it executes for the Edit commands while it has the focus (see <see cref="EditCommandRouter"/>).
/// </summary>
/// <example>
/// <code>
/// &lt;ListBox&gt;
///   &lt;sd:EditCommands.Bindings&gt;
///     &lt;sd:EditCommandBinding Kind="Copy" Command="{Binding CopyCommand}"/&gt;
///   &lt;/sd:EditCommands.Bindings&gt;
/// &lt;/ListBox&gt;
/// </code>
/// </example>
public static class EditCommands
{
    /// <summary>
    /// The Edit command bindings of a control.
    /// </summary>
    public static readonly AttachedProperty<EditCommandBindingCollection?> BindingsProperty =
        AvaloniaProperty.RegisterAttached<Control, EditCommandBindingCollection?>("Bindings", typeof(EditCommands));

    public static EditCommandBindingCollection GetBindings(Control control)
    {
        var bindings = control.GetValue(BindingsProperty);
        if (bindings is null)
        {
            bindings = [];
            control.SetValue(BindingsProperty, bindings);
        }
        return bindings;
    }

    public static void SetBindings(Control control, EditCommandBindingCollection? value)
    {
        control.SetValue(BindingsProperty, value);
    }

    /// <summary>
    /// The key gestures of an Edit command on this platform: those of the text boxes for Undo, Redo, Cut, Copy and Paste,
    /// Delete and F2 for the others.
    /// </summary>
    public static IReadOnlyList<KeyGesture> GesturesOf(EditCommandKind kind)
    {
        var hotkeys = Application.Current?.PlatformSettings?.HotkeyConfiguration;
        var commandModifiers = hotkeys?.CommandModifiers ?? KeyModifiers.Control;
        return kind switch
        {
            EditCommandKind.Undo => hotkeys?.Undo ?? [new KeyGesture(Key.Z, commandModifiers)],
            EditCommandKind.Redo => hotkeys?.Redo ?? [new KeyGesture(Key.Y, commandModifiers)],
            EditCommandKind.Cut => hotkeys?.Cut ?? [new KeyGesture(Key.X, commandModifiers)],
            EditCommandKind.Copy => hotkeys?.Copy ?? [new KeyGesture(Key.C, commandModifiers)],
            EditCommandKind.Paste => hotkeys?.Paste ?? [new KeyGesture(Key.V, commandModifiers)],
            EditCommandKind.Delete => [new KeyGesture(Key.Delete)],
            EditCommandKind.Rename => [new KeyGesture(Key.F2)],
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    /// <summary>
    /// The key gesture shown for an Edit command on this platform (the first of <see cref="GesturesOf"/>).
    /// </summary>
    public static KeyGesture GestureOf(EditCommandKind kind) => GesturesOf(kind)[0];

    /// <inheritdoc cref="GestureOf"/>
    public static KeyGesture UndoGesture => GestureOf(EditCommandKind.Undo);

    /// <inheritdoc cref="GestureOf"/>
    public static KeyGesture RedoGesture => GestureOf(EditCommandKind.Redo);

    /// <inheritdoc cref="GestureOf"/>
    public static KeyGesture CutGesture => GestureOf(EditCommandKind.Cut);

    /// <inheritdoc cref="GestureOf"/>
    public static KeyGesture CopyGesture => GestureOf(EditCommandKind.Copy);

    /// <inheritdoc cref="GestureOf"/>
    public static KeyGesture PasteGesture => GestureOf(EditCommandKind.Paste);

    /// <inheritdoc cref="GestureOf"/>
    public static KeyGesture DeleteGesture => GestureOf(EditCommandKind.Delete);

    /// <inheritdoc cref="GestureOf"/>
    public static KeyGesture RenameGesture => GestureOf(EditCommandKind.Rename);
}
