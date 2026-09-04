// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Input;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Avalonia.Commands;

internal class RelayCommand : ICommand
{
    private readonly Func<bool>? canExecute;
    private readonly Action<object> execute;

    internal RelayCommand(Action<object> execute, Func<bool> canExecute = null)
    {
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    public bool CanExecute(object parameter)
    {
        return this.canExecute?.Invoke() != false;
    }

    public void Execute(object parameter)
    {
        execute(parameter);
    }

    // We provide an empty `add' and `remove' to avoid a warning about unused events that we have
    // to implement as they are part of the ICommand definition.
    public event EventHandler CanExecuteChanged { add { } remove { } }
}
