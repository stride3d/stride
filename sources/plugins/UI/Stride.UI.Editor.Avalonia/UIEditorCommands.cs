// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Translation;
using Stride.UI.Editor.ViewModels;

namespace Stride.UI.Editor.Avalonia;

/// <summary>
/// The shortcuts of the UI page and UI library editors, as commands of the registry scoped to them
/// (<see cref="CommandDescriptor.GestureScope"/>): they work only while the focus is in a UI editor, and the user can change them.
/// The arrow keys of the design area are not commands: the editor's game reads them.
/// </summary>
[Experimental("STREXP003")]
public static class UIEditorCommands
{
    /// <summary>
    /// The category of the commands of the UI editors.
    /// </summary>
    public const string Category = "UI";

    /// <summary>Duplicates the selected UI elements (Ctrl+D).</summary>
    public const string DuplicateId = "Stride.UI.Duplicate";

    /// <summary>
    /// Creates the commands, each time new descriptors (a descriptor is registered once).
    /// </summary>
    public static IReadOnlyList<CommandDescriptor> Create() =>
    [
        new CommandDescriptor
        {
            Id = DuplicateId,
            Category = Category,
            Title = () => Tr._p("Menu", "Duplicate selected elements"),
            DefaultGesture = "Ctrl+D",
            GestureScope = typeof(UIEditorBaseViewModel),
            Keywords = ["copy", "clone", "element"],
            CanExecute = context => context.Editor is UIEditorBaseViewModel editor && editor.DuplicateSelectionCommand.CanExecute(null),
            ExecuteAsync = context =>
            {
                if (context.Editor is UIEditorBaseViewModel editor && editor.DuplicateSelectionCommand.CanExecute(null))
                    editor.DuplicateSelectionCommand.Execute(null);
                return Task.CompletedTask;
            },
        },
    ];
}
