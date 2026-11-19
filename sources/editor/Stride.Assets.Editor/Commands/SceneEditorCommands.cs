// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Translation;

namespace Stride.Assets.Editor.Commands;

/// <summary>
/// The commands of the scene and prefab editors, for the command registry: the items of the context menus of the entity tree
/// and of the scene view, and the editors' shortcuts. Each one runs the existing command of the editor it was invoked in
/// (<see cref="CommandContext.Editor"/>), as its menu item or its key did.
/// </summary>
/// <remarks>
/// The editor's default plugin registers them (<c>StrideEditorPlugin</c>). They have no placement: the menus of the editors bind
/// them by their identifier. Their shortcuts, their own or the user's, are scoped to the editors
/// (<see cref="CommandDescriptor.GestureScope"/>): they work only while the focus is in a scene or prefab editor, before the main
/// window's. The tool keys of the scene view (move,
/// rotate, scale...) and the camera's keys are not commands: the editor's game reads them.
/// </remarks>
[Experimental("STREXP003")]
public static class SceneEditorCommands
{
    /// <summary>Frames the selected entities in the scene view (F).</summary>
    public const string FocusOnSelectionId = "Stride.Scene.FocusOnSelection";

    /// <summary>Duplicates the selected entities (Ctrl+D).</summary>
    public const string DuplicateId = "Stride.Scene.Duplicate";

    /// <summary>Creates a folder in the active scene (the context menu of the tree's empty area).</summary>
    public const string NewFolderInRootId = "Stride.Scene.NewFolderInRoot";

    /// <summary>Creates a folder in the selected entity or folder (the context menu of an item of the tree).</summary>
    public const string NewFolderId = "Stride.Scene.NewFolder";

    /// <summary>Creates an empty entity in the active scene (the context menu of the tree's empty area).</summary>
    public const string NewEntityInRootId = "Stride.Scene.NewEntityInRoot";

    /// <summary>Creates an empty entity in the selected entity or folder (the context menu of an item of the tree).</summary>
    public const string NewEntityId = "Stride.Scene.NewEntity";

    /// <summary>Creates an empty entity where the scene view's context menu opened.</summary>
    public const string NewEntityAtCursorId = "Stride.Scene.NewEntityAtCursor";

    /// <summary>Makes the child scene the menu opened on the active one, which new entities go to.</summary>
    public const string SetActiveSceneId = "Stride.Scene.SetActiveScene";

    /// <summary>Opens the prefab of the selected entity in its editor.</summary>
    public const string OpenPrefabId = "Stride.Scene.OpenPrefab";

    /// <summary>Selects the prefab of the selected entity in the asset view.</summary>
    public const string SelectPrefabId = "Stride.Scene.SelectPrefab";

    /// <summary>Breaks the link of the selected entities to their prefab.</summary>
    public const string BreakLinkToPrefabId = "Stride.Scene.BreakLinkToPrefab";

    /// <summary>Creates a prefab from the selected entities.</summary>
    public const string CreatePrefabFromSelectionId = "Stride.Scene.CreatePrefabFromSelection";

    /// <summary>Pastes the copied entities in the active scene (the context menu of the tree's empty area).</summary>
    public const string PasteInRootId = "Stride.Scene.PasteInRoot";

    /// <summary>Moves the selected entities in front of the camera of the view (Ctrl+Alt+F).</summary>
    public const string MoveToViewId = "Stride.Scene.MoveToView";

    /// <summary>Gives the selected entities the position and the rotation of the camera of the view (Ctrl+Shift+F).</summary>
    public const string AlignWithViewId = "Stride.Scene.AlignWithView";

    /// <summary>Moves the camera of the view to the selected entity, looking where it looks (Shift+F).</summary>
    public const string AlignViewToSelectionId = "Stride.Scene.AlignViewToSelection";

    /// <summary>Makes a new empty entity the parent of the selected entities, without moving them (Ctrl+G).</summary>
    public const string GroupUnderNewParentId = "Stride.Scene.GroupUnderNewParent";

    /// <summary>Drops the selected entities onto the surface below them, or the ground plane (End).</summary>
    public const string SnapToFloorId = "Stride.Scene.SnapToFloor";

    /// <summary>
    /// Creates the commands, each time new descriptors (a descriptor is registered once).
    /// </summary>
    public static IReadOnlyList<CommandDescriptor> Create() =>
    [
        Wrap(FocusOnSelectionId, () => Tr._p("Menu", "Focus on selection"), "F", x => x.FocusOnSelectionCommand, null, true, "frame", "camera", "zoom", "entity"),
        Wrap(DuplicateId, () => Tr._p("Menu", "Duplicate"), "Ctrl+D", x => x.DuplicateSelectionCommand, null, true, "copy", "clone", "entity"),
        Wrap(NewFolderInRootId, () => Tr._p("Menu", "New folder"), null, x => x.CreateFolderInRootCommand, null, true, "create", "folder"),
        Wrap(NewFolderId, () => Tr._p("Menu", "New folder in selection"), null, x => x.CreateFolderInSelectionCommand, null, false, "create", "folder"),
        Wrap(NewEntityInRootId, () => Tr._p("Menu", "New empty entity"), null, x => x.CreateEntityInRootCommand, null, true, "create", "add", "entity"),
        Wrap(NewEntityId, () => Tr._p("Menu", "New empty entity in selection"), null, x => x.CreateEntityInSelectionCommand, null, false, "create", "add", "entity", "child"),
        Wrap(NewEntityAtCursorId, () => Tr._p("Menu", "New empty entity at the cursor"), null, x => x.CreateEntityCommand, null, false, "create", "add", "entity"),
        Wrap(SetActiveSceneId, () => Tr._p("Menu", "Set as active scene"), null, x => x.SetActiveRootCommand, null, false, "child", "scene"),
        Wrap(OpenPrefabId, () => Tr._p("Menu", "Open prefab in editor"), null, x => x.OpenPrefabEditorCommand, null, true, "prefab", "edit"),
        Wrap(SelectPrefabId, () => Tr._p("Menu", "Select prefab in asset view"), null, x => x.SelectPrefabCommand, null, true, "prefab", "find"),
        Wrap(BreakLinkToPrefabId, () => Tr._p("Menu", "Break link to prefab"), null, x => x.BreakLinkToPrefabCommand, null, true, "prefab", "unlink"),
        Wrap(CreatePrefabFromSelectionId, () => Tr._p("Menu", "Create prefab from selection"), null, x => x.CreatePrefabFromSelectionCommand, null, true, "prefab", "new"),
        Wrap(PasteInRootId, () => Tr._p("Menu", "Paste into the active scene"), null, x => x.PasteCommand, true, false, "clipboard", "root"),
        Wrap(MoveToViewId, () => Tr._p("Menu", "Move to view"), "Ctrl+Alt+F", x => x.MoveToViewCommand, null, true, "camera", "place", "entity"),
        Wrap(AlignWithViewId, () => Tr._p("Menu", "Align with view"), "Ctrl+Shift+F", x => x.AlignWithViewCommand, null, true, "camera", "rotation", "entity"),
        Wrap(AlignViewToSelectionId, () => Tr._p("Menu", "Align view to selection"), "Shift+F", x => x.AlignViewToSelectionCommand, null, true, "camera", "look", "entity"),
        Wrap(GroupUnderNewParentId, () => Tr._p("Menu", "Group under a new parent"), "Ctrl+G", x => x.GroupUnderNewParentCommand, null, true, "parent", "empty", "reparent", "entity"),
        Wrap(SnapToFloorId, () => Tr._p("Menu", "Snap to floor"), "End", x => x.SnapToFloorCommand, null, true, "drop", "ground", "surface", "entity"),
    ];

    // A command of the editor it was invoked in, run with the parameter its menu item or key gave it; it is in the command
    // palette unless it only makes sense where its menu places it
    private static CommandDescriptor Wrap(string id, Func<string> title, string? gesture, Func<EntityHierarchyEditorViewModel, ICommand> command, object? parameter, bool showInPalette, params string[] keywords) => new()
    {
        Id = id,
        Category = CommandCategories.Scene,
        Title = title,
        DefaultGesture = gesture,
        // A shortcut, its own or the user's, works in the scene and prefab editors only
        GestureScope = typeof(EntityHierarchyEditorViewModel),
        Keywords = keywords,
        ShowInPalette = showInPalette,
        CanExecute = context => context.Editor is EntityHierarchyEditorViewModel editor && command(editor).CanExecute(parameter),
        ExecuteAsync = context =>
        {
            if (context.Editor is EntityHierarchyEditorViewModel editor && command(editor) is { } target && target.CanExecute(parameter))
                target.Execute(parameter);
            return Task.CompletedTask;
        },
    };
}
