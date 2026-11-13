// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace Stride.Core.Presentation.Avalonia.Behaviors;

public abstract class CloseWindowBehavior<T> : StyledElementBehavior<T>
    where T : Visual
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<CloseWindowBehavior<T>, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<CloseWindowBehavior<T>, object?>(nameof(CommandParameter));

    static CloseWindowBehavior()
    {
        CommandProperty.Changed.AddClassHandler<CloseWindowBehavior<T>, ICommand?>(OnCommandChanged);
        CommandParameterProperty.Changed.AddClassHandler<CloseWindowBehavior<T>, object?>(OnCommandParameterChanged);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();
        if (Command is not null)
        {
            UpdateIsEnabled();
        }
    }

    protected void CloseWindow()
    {
        if (Command is not null && Command.CanExecute(CommandParameter))
        {
            Command.Execute(CommandParameter);
        }

        if (TopLevel.GetTopLevel(AssociatedObject) is not Window window)
        {
            throw new InvalidOperationException("The button attached to this behavior is not in a window");
        }

        window.Close();
    }

    private static void OnCommandChanged(CloseWindowBehavior<T> sender, AvaloniaPropertyChangedEventArgs<ICommand?> e)
    {
        if (e.OldValue.Value is { } oldCommand)
        {
            oldCommand.CanExecuteChanged -= CommandCanExecuteChanged;
        }
        if (e.NewValue.Value is { } newCommand)
        {
            newCommand.CanExecuteChanged += CommandCanExecuteChanged;
        }
        return;

        void CommandCanExecuteChanged(object? _, EventArgs __)
        {
            sender.UpdateIsEnabled();
        }
    }

    private static void OnCommandParameterChanged(CloseWindowBehavior<T> sender, AvaloniaPropertyChangedEventArgs<object?> e)
    {
        if (sender.Command is not null)
        {
            sender.UpdateIsEnabled();
        }
    }

    private void UpdateIsEnabled()
    {
        Debug.Assert(Command is not null);

        AssociatedObject?.SetCurrentValue(InputElement.IsEnabledProperty, Command.CanExecute(CommandParameter));
    }
}
