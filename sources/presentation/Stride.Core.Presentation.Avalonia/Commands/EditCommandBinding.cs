// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;

namespace Stride.Core.Presentation.Avalonia.Commands;

/// <summary>
/// The command a panel executes for one of the Edit commands while it has the focus, declared in <see cref="EditCommands.BindingsProperty"/>
/// of the panel, as a command binding of WPF.
/// </summary>
/// <remarks>
/// A binding whose <see cref="Command"/> is null is ignored, so that a panel can bind a command of a view model that is not there yet.
/// </remarks>
public sealed class EditCommandBinding : AvaloniaObject, IEditCommandHandler
{
    public static readonly StyledProperty<EditCommandKind> KindProperty =
        AvaloniaProperty.Register<EditCommandBinding, EditCommandKind>(nameof(Kind));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<EditCommandBinding, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<EditCommandBinding, object?>(nameof(CommandParameter));

    /// <summary>
    /// The Edit command this binding handles.
    /// </summary>
    public EditCommandKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    /// <summary>
    /// The command of the panel to execute.
    /// </summary>
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>
    /// The parameter of <see cref="Command"/>.
    /// </summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <inheritdoc/>
    public bool Handles(EditCommandKind kind) => kind == Kind && Command is not null;

    /// <inheritdoc/>
    public bool CanExecute(EditCommandKind kind) => Handles(kind) && Command!.CanExecute(CommandParameter);

    /// <inheritdoc/>
    public void Execute(EditCommandKind kind)
    {
        if (Handles(kind))
            Command!.Execute(CommandParameter);
    }
}

/// <summary>
/// The Edit command bindings of a panel.
/// </summary>
public sealed class EditCommandBindingCollection : AvaloniaList<EditCommandBinding>;
