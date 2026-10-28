// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Input;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Presentation.Avalonia.Commands;

/// <summary>
/// Begins the editing in place of the name of the view model given as parameter (the Rename command, F2), when it is editable.
/// The view shows the editor while the view model <see cref="IIsEditableViewModel.IsEditing"/>; see <see cref="Behaviors.InlineEditBehavior"/>.
/// </summary>
public sealed class BeginEditCommand : ICommand
{
    public static BeginEditCommand Instance { get; } = new();

    private BeginEditCommand()
    {
    }

    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    /// <inheritdoc/>
    public bool CanExecute(object? parameter) => parameter is IIsEditableViewModel { IsEditable: true, IsEditing: false };

    /// <inheritdoc/>
    public void Execute(object? parameter)
    {
        if (parameter is IIsEditableViewModel { IsEditable: true } viewModel)
            viewModel.IsEditing = true;
    }
}
