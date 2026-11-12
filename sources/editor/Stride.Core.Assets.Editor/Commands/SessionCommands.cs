// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Commands;

/// <summary>
/// The editor's own commands that act on the session, for the command registry: the shortcuts of the asset view, the
/// selection history and the package commands of the Project menu. Each one runs the session's existing command, as its menu item or shortcut does.
/// </summary>
/// <remarks>
/// The editor's default plugin registers them (<see cref="Services.IEditorPluginContext.Commands"/> of
/// <c>Stride.Assets.Editor.StrideEditorPlugin</c>). They have no placement: the menus of the shells show the session's commands
/// themselves.
/// </remarks>
[Experimental("STREXP003")]
public static class SessionCommands
{
    /// <summary>Opens the editor of the selected asset (Ctrl+Enter).</summary>
    public const string EditSelectedId = "Stride.Asset.EditSelected";

    /// <summary>Opens the list of asset templates to add an asset (Ctrl+I).</summary>
    public const string AddAssetId = "Stride.Asset.Add";

    /// <summary>Copies the selected assets with the assets they reference (Ctrl+Shift+C).</summary>
    public const string CopyAssetsRecursivelyId = "Stride.Asset.CopyRecursively";

    /// <summary>Updates the selected assets from their source files (Ctrl+Shift+R).</summary>
    public const string UpdateSelectedFromSourceId = "Stride.Asset.UpdateSelectedFromSource";

    /// <summary>Updates every asset whose source file changed (Ctrl+R).</summary>
    public const string UpdateAllFromSourceId = "Stride.Asset.UpdateAllFromSource";

    /// <summary>Goes back to the previous selection (Alt+Left, the property grid's arrow).</summary>
    public const string PreviousSelectionId = "Stride.Edit.PreviousSelection";

    /// <summary>Goes forward to the next selection (Alt+Right, the property grid's arrow).</summary>
    public const string NextSelectionId = "Stride.Edit.NextSelection";

    /// <summary>Project > Add dependency.</summary>
    public const string AddDependencyId = "Stride.Project.AddDependency";

    /// <summary>Project > Open in IDE.</summary>
    public const string OpenInIdeId = "Stride.Project.OpenInIDE";

    /// <summary>
    /// Creates the commands, each time new descriptors (a descriptor is registered once).
    /// </summary>
    public static IReadOnlyList<CommandDescriptor> Create() =>
    [
        Wrap(EditSelectedId, CommandCategories.Asset, () => Tr._p("Menu", "Edit asset..."), "Ctrl+Enter", x => x.EditSelectedContentCommand, "open"),
        Wrap(AddAssetId, CommandCategories.Asset, () => Tr._p("Menu", "Add asset..."), "Ctrl+I", x => x.AssetCollection.ShowAddAssetDialogCommand, "new", "create", "template"),
        Wrap(CopyAssetsRecursivelyId, CommandCategories.Asset, () => Tr._p("Menu", "Copy with dependencies"), "Ctrl+Shift+C", x => x.AssetCollection.CopyAssetsRecursivelyCommand, "clipboard", "references"),
        Wrap(UpdateSelectedFromSourceId, CommandCategories.Asset, () => Tr._p("Menu", "Update selected assets from their source"), "Ctrl+Shift+R", x => x.SourceTracker?.UpdateSelectedAssetsFromSourceCommand, "reimport", "file"),
        Wrap(UpdateAllFromSourceId, CommandCategories.Asset, () => Tr._p("Menu", "Update all assets with modified source"), "Ctrl+R", x => x.SourceTracker?.UpdateAllAssetsWithModifiedSourceCommand, "reimport", "file"),
        Wrap(PreviousSelectionId, CommandCategories.Edit, () => Tr._p("Menu", "Previous selection"), "Alt+Left", x => x.PreviousSelectionCommand, "back", "history", "navigate"),
        Wrap(NextSelectionId, CommandCategories.Edit, () => Tr._p("Menu", "Next selection"), "Alt+Right", x => x.NextSelectionCommand, "forward", "history", "navigate"),
        Wrap(AddDependencyId, CommandCategories.Project, () => Tr._p("Menu", "Add dependency..."), null, x => x.AddDependencyCommand, "package", "reference"),
        Wrap(OpenInIdeId, CommandCategories.Project, () => Tr._p("Menu", "Open in IDE"), null, x => x.OpenInIDECommand, "code", "visual studio", "rider", "solution"),
    ];

    /// <summary>
    /// A command of the registry that runs a command of the session: it can run when the session's command can, and runs it
    /// as its menu item does (with no parameter), without waiting for what it starts.
    /// </summary>
    internal static CommandDescriptor Wrap(string id, string category, Func<string> title, string? gesture, Func<SessionViewModel, ICommand?> command, params string[] keywords) => new()
    {
        Id = id,
        Category = category,
        Title = title,
        DefaultGesture = gesture,
        Keywords = keywords,
        CanExecute = context => command(context.Session)?.CanExecute(null) ?? false,
        ExecuteAsync = context =>
        {
            if (command(context.Session) is { } target && target.CanExecute(null))
                target.Execute(null);
            return Task.CompletedTask;
        },
    };
}
