// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using static Stride.Assets.Editor.Tests.EntityHierarchyEditorTests;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The Edit commands of the scene editor (cut, copy, paste, delete and duplicate), as in the WPF scene editor: each one
/// is a single entry of the undo history.
/// </summary>
public sealed class EntityHierarchyEditCommandsTests
{
    [Fact]
    public async Task TheCommandsNeedASelectionAndTheRootCannotBeDeleted()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);

        Assert.False(editor.CopyCommand.IsEnabled);
        Assert.False(editor.CutCommand.IsEnabled);
        Assert.False(editor.DeleteCommand.IsEnabled);
        Assert.False(editor.DuplicateSelectionCommand.IsEnabled);

        editor.SelectedContent.Add(FindEntity(editor, "Box"));

        Assert.True(editor.CopyCommand.IsEnabled);
        Assert.True(editor.CutCommand.IsEnabled);
        Assert.True(editor.DeleteCommand.IsEnabled);
        Assert.True(editor.DuplicateSelectionCommand.IsEnabled);

        editor.SelectedContent.Add(editor.HierarchyRoot);

        Assert.False(editor.DeleteCommand.IsEnabled);
        Assert.False(editor.CutCommand.IsEnabled);
        editor.Destroy();
    }

    [Fact]
    public async Task CopyThenPasteIntoTheRootAddsACopyAsOneUndoEntry()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var parent = FindEntity(editor, "Parent");
        editor.SelectedContent.Add(parent);
        await editor.PropertiesRefreshCompletion;
        var names = RecordUndoNames(fixture);

        editor.CopyCommand.Execute();
        Assert.False(string.IsNullOrEmpty(Clipboard(fixture).Text));
        editor.PasteCommand.Execute(true);

        Assert.Equal(["Box", "Parent", "Parent"], editor.HierarchyRoot.Children.Select(x => x.Name));
        var copy = (EntityViewModel)editor.HierarchyRoot.Children[2];
        Assert.NotEqual(parent.Id, copy.Id);
        Assert.Equal(["Child"], copy.Children.Select(x => x.Name));
        Assert.Equal(["Paste into MainScene"], names);

        fixture.Session.UndoRedoService.Undo();

        Assert.Equal(["Box", "Parent"], editor.HierarchyRoot.Children.Select(x => x.Name));
        editor.Destroy();
    }

    [Fact]
    public async Task PasteIntoTheSelectionAddsChildrenToTheSelectedEntity()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var box = FindEntity(editor, "Box");
        var parent = FindEntity(editor, "Parent");
        editor.SelectedContent.Add(box);
        editor.CopyCommand.Execute();
        editor.SelectedContent.Clear();
        editor.SelectedContent.Add(parent);
        await editor.PropertiesRefreshCompletion;
        var names = RecordUndoNames(fixture);

        editor.PasteCommand.Execute(false);

        Assert.Equal(["Child", "Box"], parent.Children.Select(x => x.Name));
        Assert.Equal(["Paste into selection"], names);
        editor.Destroy();
    }

    [Fact]
    public async Task PasteIntoAFolderPutsTheEntitiesInIt()
    {
        await using var fixture = await OpenSceneAsync();
        EntityHierarchyFolderTests.AddEntity(fixture, "Sun", "Lights", parent: null, index: 2);
        var editor = await OpenEditorAsync(fixture);
        editor.SelectedContent.Add(FindEntity(editor, "Box"));
        editor.CopyCommand.Execute();
        editor.SelectedContent.Clear();
        var lights = (EntityFolderViewModel)editor.HierarchyRoot.Children.Single(x => x.Name == "Lights");
        editor.SelectedContent.Add(lights);
        await editor.PropertiesRefreshCompletion;

        editor.PasteCommand.Execute(false);

        Assert.Equal(["Sun", "Box"], lights.Children.Select(x => x.Name));
        Assert.Equal("Lights", ((EntityViewModel)lights.Children[1]).EntityDesign.Folder);
        editor.Destroy();
    }

    [Fact]
    public async Task ACopiedFolderComesWithItsSubFolders()
    {
        await using var fixture = await OpenSceneAsync();
        EntityHierarchyFolderTests.AddEntity(fixture, "Tree", "Props/Nature", parent: null, index: 2);
        var editor = await OpenEditorAsync(fixture);
        editor.SelectedContent.Add(editor.HierarchyRoot.Children.Single(x => x.Name == "Props"));
        editor.CopyCommand.Execute();
        editor.SelectedContent.Clear();
        editor.SelectedContent.Add(FindEntity(editor, "Box"));
        await editor.PropertiesRefreshCompletion;

        editor.PasteCommand.Execute(false);

        // The folder paths are relative to the parent of the copied folder: the folder itself comes along
        var box = FindEntity(editor, "Box");
        var props = Assert.IsType<EntityFolderViewModel>(Assert.Single(box.Children));
        var nature = Assert.IsType<EntityFolderViewModel>(Assert.Single(props.Children));
        Assert.Equal("Props/Nature", nature.Path);
        Assert.Equal(["Tree"], nature.Children.Select(x => x.Name));
        editor.Destroy();
    }

    [Fact]
    public async Task CutRemovesTheSelectionAsOneUndoEntryAndPasteBringsItBack()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        editor.SelectedContent.Add(FindEntity(editor, "Parent"));
        await editor.PropertiesRefreshCompletion;
        var names = RecordUndoNames(fixture);

        editor.CutCommand.Execute();

        Assert.Equal(["Box"], editor.HierarchyRoot.Children.Select(x => x.Name));
        Assert.Empty(editor.SelectedContent);
        Assert.Equal(["Cut selection"], names);

        editor.PasteCommand.Execute(true);

        Assert.Equal(["Box", "Parent"], editor.HierarchyRoot.Children.Select(x => x.Name));
        Assert.Equal(["Child"], FindEntity(editor, "Parent").Children.Select(x => x.Name));

        fixture.Session.UndoRedoService.Undo();
        fixture.Session.UndoRedoService.Undo();

        Assert.Equal(["Box", "Parent"], editor.HierarchyRoot.Children.Select(x => x.Name));
        Assert.Equal(["Child"], FindEntity(editor, "Parent").Children.Select(x => x.Name));
        editor.Destroy();
    }

    [Fact]
    public async Task DeleteAsksFirstThenDeletesAsOneUndoEntry()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var dialogs = (FakeEditorDialogService)fixture.ServiceProvider.Get<IDialogService>();
        editor.SelectedContent.Add(FindEntity(editor, "Parent"));
        await editor.PropertiesRefreshCompletion;
        var names = RecordUndoNames(fixture);

        dialogs.Answer = MessageBoxResult.Cancel;
        editor.DeleteCommand.Execute();

        Assert.Equal(["Are you sure you want to delete this entity?"], dialogs.Messages);
        Assert.Equal(["Box", "Parent"], editor.HierarchyRoot.Children.Select(x => x.Name));

        dialogs.Answer = MessageBoxResult.OK;
        editor.DeleteCommand.Execute();

        Assert.Equal(["Box"], editor.HierarchyRoot.Children.Select(x => x.Name));
        Assert.Empty(editor.SelectedContent);
        Assert.Equal(["Delete selected entities"], names);

        fixture.Session.UndoRedoService.Undo();

        Assert.Equal(["Box", "Parent"], editor.HierarchyRoot.Children.Select(x => x.Name));
        Assert.Equal(["Child"], FindEntity(editor, "Parent").Children.Select(x => x.Name));
        editor.Destroy();
    }

    [Fact]
    public async Task DeletingAFolderDeletesItsEntitiesAndTheUndoBringsBothBack()
    {
        await using var fixture = await OpenSceneAsync();
        EntityHierarchyFolderTests.AddEntity(fixture, "Sun", "Lights", parent: null, index: 2);
        var editor = await OpenEditorAsync(fixture);
        editor.SelectedContent.Add(editor.HierarchyRoot.Children.Single(x => x.Name == "Lights"));
        await editor.PropertiesRefreshCompletion;
        var names = RecordUndoNames(fixture);

        editor.DeleteCommand.Execute();

        Assert.Equal(["Box", "Parent"], editor.HierarchyRoot.Children.Select(x => x.Name));
        Assert.Equal(["Delete selected entities"], names);

        fixture.Session.UndoRedoService.Undo();

        var lights = Assert.IsType<EntityFolderViewModel>(editor.HierarchyRoot.Children[0]);
        Assert.Equal(["Sun"], lights.Children.Select(x => x.Name));
        editor.Destroy();
    }

    [Fact]
    public async Task DuplicateAddsACopyNextToEachSelectedEntityAndSelectsTheCopies()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var box = FindEntity(editor, "Box");
        var parent = FindEntity(editor, "Parent");
        editor.SelectedContent.Add(box);
        editor.SelectedContent.Add(parent);
        // The child goes with its parent: it is not duplicated twice
        editor.SelectedContent.Add(FindEntity(editor, "Child"));
        await editor.PropertiesRefreshCompletion;
        var names = RecordUndoNames(fixture);

        editor.DuplicateSelectionCommand.Execute();

        Assert.Equal(["Box", "Box", "Parent", "Parent"], editor.HierarchyRoot.Children.Select(x => x.Name));
        var copies = editor.HierarchyRoot.Children.OfType<EntityViewModel>().Where(x => x != box && x != parent).ToList();
        Assert.Equal(copies.OrderBy(x => x.Name), editor.SelectedItems.OrderBy(x => x.Name));
        Assert.Equal(["Child"], copies[1].Children.Select(x => x.Name));
        Assert.Equal(["Duplicate entities"], names);

        fixture.Session.UndoRedoService.Undo();

        Assert.Equal(["Box", "Parent"], editor.HierarchyRoot.Children.Select(x => x.Name));
        editor.Destroy();
    }

    [Fact]
    public async Task ADuplicatedEntityStaysInItsFolder()
    {
        await using var fixture = await OpenSceneAsync();
        EntityHierarchyFolderTests.AddEntity(fixture, "Sun", "Lights", parent: null, index: 2);
        var editor = await OpenEditorAsync(fixture);
        editor.SelectedContent.Add(FindEntity(editor, "Sun"));
        await editor.PropertiesRefreshCompletion;

        editor.DuplicateSelectionCommand.Execute();

        var lights = (EntityFolderViewModel)editor.HierarchyRoot.Children.Single(x => x.Name == "Lights");
        Assert.Equal(["Sun", "Sun"], lights.Children.Select(x => x.Name));
        editor.Destroy();
    }

    [Fact]
    public async Task ACtrlDragDuplicationStopsWaitingForTheGameAfterItsTimeout()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var box = FindEntity(editor, "Box");
        editor.SelectedContent.Add(box);
        // No game selects the copies: the gizmo would wait for them forever
        var selection = new EditorGameEntitySelectionService(null!, editor) { DuplicateSelectionTimeout = TimeSpan.FromMilliseconds(100) };

        var duplicate = selection.DuplicateSelection();

        Assert.Same(duplicate, await Task.WhenAny(duplicate, Task.Delay(TimeSpan.FromSeconds(10))));
        Assert.False(await duplicate);
        // The copy stays in the scene (undo removes it): the gizmo cancels the drag, and nothing moves
        Assert.Equal(["Box", "Box", "Parent"], editor.HierarchyRoot.Children.Select(x => x.Name));
        editor.Destroy();
    }

    [Fact]
    public async Task ACanceledCtrlDragDuplicationStopsWaitingForTheGame()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        editor.SelectedContent.Add(FindEntity(editor, "Box"));
        var selection = new EditorGameEntitySelectionService(null!, editor);
        using var cancellation = new CancellationTokenSource();

        var duplicate = selection.DuplicateSelection(cancellation.Token);
        cancellation.Cancel();

        Assert.Same(duplicate, await Task.WhenAny(duplicate, Task.Delay(TimeSpan.FromSeconds(10))));
        Assert.False(await duplicate);
        editor.Destroy();
    }

    private static FakeClipboardService Clipboard(AssetsSessionFixture fixture)
    {
        return (FakeClipboardService)fixture.ServiceProvider.Get<Core.Assets.Editor.Services.IClipboardService>();
    }

    private static List<string?> RecordUndoNames(AssetsSessionFixture fixture)
    {
        var undoRedo = fixture.Session.UndoRedoService;
        var names = new List<string?>();
        undoRedo.Done += (_, e) => names.Add(undoRedo.GetName(e.Transaction));
        return names;
    }
}
