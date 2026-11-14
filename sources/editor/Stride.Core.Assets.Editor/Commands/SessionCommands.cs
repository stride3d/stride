// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Commands;

/// <summary>
/// The editor's own commands that act on the session, for the command registry: the shortcuts of the asset view, the
/// selection history, the package commands of the Project menu and the items of the context menus of the asset view and the
/// solution explorer. Each one runs the session's existing command, as its menu item or shortcut does.
/// </summary>
/// <remarks>
/// The editor's default plugin registers them (<see cref="Services.IEditorPluginContext.Commands"/> of
/// <c>Stride.Assets.Editor.StrideEditorPlugin</c>). They have no placement: the menus of the shells show them by their
/// identifier. A command of a context menu acts on the selection the menu opened on (<see cref="CommandContext.Selection"/>),
/// and on the asset view's from a shortcut or the command palette.
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

    /// <summary>Includes the selected assets in the build as root assets of their package, or takes them out (asset view).</summary>
    public const string ToggleRootId = "Stride.Asset.ToggleRoot";

    /// <summary>Copies the URL of the selected asset (asset view).</summary>
    public const string CopyUrlId = "Stride.Asset.CopyUrl";

    /// <summary>Creates a folder in the selected folder (asset view, solution explorer).</summary>
    public const string NewFolderId = "Stride.Asset.NewFolder";

    /// <summary>Opens the file of the selected asset in the text editor of the settings (asset view).</summary>
    public const string OpenWithTextEditorId = "Stride.Asset.OpenWithTextEditor";

    /// <summary>Opens the file of the selected asset with the application of the system (asset view).</summary>
    public const string OpenAssetFileId = "Stride.Asset.OpenFile";

    /// <summary>Opens the source file of the selected asset (asset view).</summary>
    public const string OpenSourceFileId = "Stride.Asset.OpenSourceFile";

    /// <summary>Shows the selected item in the file browser of the system (asset view, solution explorer).</summary>
    public const string ShowInExplorerId = "Stride.Asset.ShowInExplorer";

    /// <summary>Shows the source file of the selected asset in the file browser of the system (asset view).</summary>
    public const string ShowSourceInExplorerId = "Stride.Asset.ShowSourceInExplorer";

    /// <summary>Makes the selected project the current one, which the editor builds and runs (solution explorer).</summary>
    public const string SetCurrentProjectId = "Stride.Project.SetCurrent";

    /// <summary>Shows the properties of the selected package (solution explorer).</summary>
    public const string PackagePropertiesId = "Stride.Project.PackageProperties";

    /// <summary>Adds a new project to the solution (solution explorer).</summary>
    public const string NewProjectId = "Stride.Project.NewProject";

    /// <summary>Adds an existing project to the solution (solution explorer).</summary>
    public const string AddExistingProjectId = "Stride.Project.AddExistingProject";

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
        // The context menus of the asset view and the solution explorer
        new CommandDescriptor
        {
            Id = ToggleRootId,
            Category = CommandCategories.Asset,
            Title = () => Tr._p("Menu", "Include in build as root asset"),
            // "Include in build as root asset (in MyGame)", or "Don't include..." when the selection already is
            ContextTitle = context => context.Session.RootAssetToggleHeader,
            Keywords = ["root", "build", "package", "exclude"],
            CanExecute = context => context.Session.ToggleIsRootOnSelectedAssetCommand.CanExecute(null),
            ExecuteAsync = context => Run(context.Session.ToggleIsRootOnSelectedAssetCommand, null),
        },
        WrapWith(CopyUrlId, CommandCategories.Asset, () => Tr._p("Menu", "Copy asset URL"), x => x.AssetCollection.CopyAssetUrlCommand, null, "clipboard", "address", "path"),
        WrapWith(NewFolderId, CommandCategories.Asset, () => Tr._p("Menu", "Create folder"), x => x.NewDirectoryCommand, context => context.Session.AssetCollection.SelectedLocations, "directory", "new"),
        WrapWith(OpenWithTextEditorId, CommandCategories.Asset, () => Tr._p("Menu", "Open with text editor"), x => x.OpenWithTextEditorCommand, SingleAsset, "yaml", "file"),
        WrapWith(OpenAssetFileId, CommandCategories.Asset, () => Tr._p("Menu", "Open asset file"), x => x.OpenAssetFileCommand, SingleAsset, "yaml", "file"),
        WrapWith(OpenSourceFileId, CommandCategories.Asset, () => Tr._p("Menu", "Open source file"), x => x.OpenSourceFileCommand, SingleAsset, "file", "import"),
        WrapWith(ShowInExplorerId, CommandCategories.Asset, () => Tr._p("Menu", "Show in Explorer"), x => x.ExploreCommand, Selection, "folder", "file", "browse", "reveal"),
        WrapWith(ShowSourceInExplorerId, CommandCategories.Asset, () => Tr._p("Menu", "Show source file in Explorer"), x => x.ExploreSourceFileCommand, SingleAsset, "folder", "file", "browse", "reveal"),
        WrapWith(SetCurrentProjectId, CommandCategories.Project, () => Tr._p("Menu", "Set as current project"), x => x.SetCurrentProjectCommand, Locations, "executable", "startup", "run"),
        WrapWith(PackagePropertiesId, CommandCategories.Project, () => Tr._p("Menu", "Package properties"), x => x.ActivatePackagePropertiesCommand, null, "settings", "package"),
        WrapWith(NewProjectId, CommandCategories.Project, () => Tr._p("Menu", "New project..."), x => x.NewProjectCommand, null, "solution", "add", "template"),
        WrapWith(AddExistingProjectId, CommandCategories.Project, () => Tr._p("Menu", "Add existing project..."), x => x.AddExistingProjectCommand, null, "solution", "import"),
    ];

    // What a command of the asset view acts on: the selection its menu opened on, or the asset view's for a shortcut
    private static IReadOnlyList<object> Selection(CommandContext context)
    {
        return context.Location == CommandLocations.Shortcut ? [.. context.Session.AssetCollection.SelectedContent] : context.Selection;
    }

    // The asset a command of the asset view acts on: the one selected asset, or none
    private static object? SingleAsset(CommandContext context) => Selection(context) is [AssetViewModel asset] ? asset : null;

    // The items of the solution explorer a command acts on: those its menu opened on, else the selected ones
    private static IReadOnlyList<object> Locations(CommandContext context)
    {
        return context.Location == CommandLocations.SolutionExplorerContext ? context.Selection : [.. context.Session.AssetCollection.SelectedLocations];
    }

    // A command of the session, run as its menu item does, without waiting for what it starts
    private static Task Run(ICommand? command, object? parameter)
    {
        if (command is not null && command.CanExecute(parameter))
            command.Execute(parameter);
        return Task.CompletedTask;
    }

    /// <summary>
    /// A command of the registry that runs a command of the session with a parameter taken from its context (the selection
    /// of the menu it was invoked from), as the menu item did; it has no default shortcut.
    /// </summary>
    private static CommandDescriptor WrapWith(string id, string category, Func<string> title, Func<SessionViewModel, ICommand?> command, Func<CommandContext, object?>? parameter, params string[] keywords) => new()
    {
        Id = id,
        Category = category,
        Title = title,
        Keywords = keywords,
        CanExecute = context => command(context.Session)?.CanExecute(parameter?.Invoke(context)) ?? false,
        ExecuteAsync = context => Run(command(context.Session), parameter?.Invoke(context)),
    };

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
