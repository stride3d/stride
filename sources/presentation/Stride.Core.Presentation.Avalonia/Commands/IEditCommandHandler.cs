// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Avalonia.Commands;

/// <summary>
/// A control that handles some of the Edit commands itself when it has the focus, as a text box handles Cut, Copy and Paste.
/// The <see cref="EditCommandRouter"/> asks the focused control and its ancestors in turn; a control that does not handle a
/// command (<see cref="Handles"/>) passes it on to its ancestors, one that handles it but cannot execute it keeps it disabled.
/// </summary>
public interface IEditCommandHandler
{
    /// <summary>
    /// Whether this handler takes the command, rather than leaving it to its ancestors.
    /// </summary>
    bool Handles(EditCommandKind kind);

    /// <summary>
    /// Whether the command can be executed now.
    /// </summary>
    bool CanExecute(EditCommandKind kind);

    /// <summary>
    /// Executes the command.
    /// </summary>
    void Execute(EditCommandKind kind);
}
