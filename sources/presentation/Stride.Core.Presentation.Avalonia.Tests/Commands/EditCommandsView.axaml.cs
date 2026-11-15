// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Input;
using Avalonia.Controls;
using Stride.Core.Presentation.Avalonia.Commands;

namespace Stride.Core.Presentation.Avalonia.Tests.Commands;

public partial class EditCommandsView : UserControl
{
    public EditCommandsView()
    {
        InitializeComponent();
        Router = new EditCommandRouter(this);
    }

    public EditCommandRouter Router { get; }
}

/// <summary>
/// The commands the panels of <see cref="EditCommandsView"/> bind; each records its executions.
/// </summary>
public sealed class EditCommandsViewModel
{
    public RecordingCommand Undo { get; } = new();

    public RecordingCommand CopyAssets { get; } = new();

    public RecordingCommand DeleteAssets { get; } = new();

    public RecordingCommand CopyLocations { get; } = new();

    /// <summary>
    /// Bound but null, as a command of a view model that is not there yet.
    /// </summary>
    public RecordingCommand? PasteAssets { get; set; }

    public RecordingCommand RenameAnywhere { get; } = new();

    public object RenameParameter { get; } = new();
}

public sealed class RecordingCommand : ICommand
{
    private bool canExecute = true;

    public List<object?> Executions { get; } = [];

    public bool IsEnabled
    {
        get => canExecute;
        set
        {
            canExecute = value;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute;

    public void Execute(object? parameter) => Executions.Add(parameter);
}
