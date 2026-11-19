// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Stride.Assets.Editor.Commands;
using Stride.Assets.Presentation.ViewModels;
using Stride.GameStudio.Avalonia.Tests.Helpers;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The scene editing commands in the window: their shortcuts in the scene editor, and the palette.
/// </summary>
public sealed class SceneEditingShortcutsTests
{
    [AvaloniaFact]
    public async Task CtrlGGroupsTheSelectionUnderANewParentInOneHistoryEntry()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var editor = scene.Editor;
        var history = scene.Fixture.Session.ActionHistory;
        var count = history.Transactions.Count;
        editor.SelectedContent.Clear();
        editor.SelectedContent.Add(scene.Entity("Box"));
        editor.SelectedContent.Add(scene.Entity("Parent"));
        Assert.True(scene.Row("Box").Focus());

        Press(scene.Window, PhysicalKey.G, RawInputModifiers.Control);

        await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == count + 1, "the history entry");
        var group = Assert.Single(editor.HierarchyRoot.InnerSubEntities.OfType<EntityViewModel>());
        Assert.Equal("Group", group.Name);
        Assert.Equal(["Box", "Parent"], group.TransformChildren.Select(x => x.Name));
        Assert.Equal([group], editor.SelectedItems);

        history.UndoCommand.Execute();
        Assert.Equal(["Box", "Parent"], editor.HierarchyRoot.InnerSubEntities.OfType<EntityViewModel>().Select(x => x.Name));
    }

    [AvaloniaFact]
    public async Task ThePaletteListsTheSceneEditingCommands()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();

        var ids = scene.Window.View.CommandSlots!.GetPaletteItems(null).Select(x => x.Descriptor.Id).ToList();

        Assert.Contains(SceneEditorCommands.MoveToViewId, ids);
        Assert.Contains(SceneEditorCommands.AlignWithViewId, ids);
        Assert.Contains(SceneEditorCommands.AlignViewToSelectionId, ids);
        Assert.Contains(SceneEditorCommands.GroupUnderNewParentId, ids);
        Assert.Contains(SceneEditorCommands.SnapToFloorId, ids);
    }

    private static void Press(TopLevel topLevel, PhysicalKey key, RawInputModifiers modifiers)
    {
        topLevel.KeyPressQwerty(key, modifiers);
        topLevel.KeyReleaseQwerty(key, modifiers);
    }
}
