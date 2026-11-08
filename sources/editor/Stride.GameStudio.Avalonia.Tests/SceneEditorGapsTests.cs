// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets.Editor.Avalonia.DragDrop;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Quantum;
using Stride.Engine;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The scene editor gaps of the parity study (G3, G4): "Set as active scene" on a child scene, and the error pane of the editor
/// game.
/// </summary>
public sealed class SceneEditorGapsTests
{
    [AvaloniaFact]
    public async Task SetAsActiveSceneShowsOnAChildSceneAndMakesItTheActiveRoot()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var main = (SceneViewModel)scene.Fixture.Asset("MainScene");
        using (scene.Fixture.Session.UndoRedoService.CreateTransaction())
        {
            main.Children.Add((SceneViewModel)scene.Fixture.Asset("ChildScene"));
        }
        var root = (SceneRootViewModel)scene.Editor.HierarchyRoot;
        await WindowTestHelper.WaitUntilAsync(() => scene.TryRow("ChildScene") is not null, "the row of the child scene");
        var child = root.ChildScenes.Single();
        var menu = (ContextMenu)scene.View.Resources["ItemContextMenu"]!;
        var setActive = menu.Items.OfType<MenuItem>().Single(x => Equals(x.Tag, "SetActiveRoot"));

        // On an entity: hidden
        scene.Editor.SelectedContent.Clear();
        scene.Editor.SelectedContent.Add(scene.Entity("Box"));
        scene.Row("Box").RaiseEvent(new ContextRequestedEventArgs());
        await WindowTestHelper.PumpAsync();
        Assert.False(setActive.IsVisible);
        menu.Close();

        // On the child scene: shown, not checked; it makes the child scene the active one
        scene.Editor.SelectedContent.Clear();
        scene.Editor.SelectedContent.Add(child);
        scene.Row("ChildScene").RaiseEvent(new ContextRequestedEventArgs());
        await WindowTestHelper.PumpAsync();
        Assert.True(setActive.IsVisible);
        Assert.False(setActive.IsChecked);
        menu.Close();
        setActive.Command!.Execute(null);
        Assert.Same(child, scene.Editor.ActiveRoot);

        scene.Row("ChildScene").RaiseEvent(new ContextRequestedEventArgs());
        await WindowTestHelper.PumpAsync();
        Assert.True(setActive.IsChecked);
        menu.Close();
        await WindowTestHelper.PumpAsync();
    }

    [AvaloniaFact]
    public async Task TheErrorPaneShowsTheExceptionOfTheGameUntilItResumes()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var pane = scene.View.GetVisualDescendants().OfType<Panel>().Single(x => x.Name == "GameErrorPane");
        var gameView = scene.View.GetVisualDescendants().OfType<ContentControl>().Single(x => x.Name == "GameViewHost");
        Assert.False(pane.IsVisible);

        scene.Editor.GameHost.LastException = new InvalidOperationException("The compositor failed.");
        await WindowTestHelper.PumpAsync();

        Assert.True(pane.IsVisible);
        Assert.False(gameView.IsVisible);
        Assert.Contains(pane.GetVisualDescendants().OfType<SelectableTextBlock>(), x => x.Text == "Error: The compositor failed.");
        var buttons = pane.GetVisualDescendants().OfType<Button>().ToList();
        Assert.Same(scene.Editor.GameHost.ResumeCommand, buttons.Single(x => x.Name == "ResumeButton").Command);
        Assert.Same(scene.Editor.GameHost.CopyErrorToClipboardCommand, buttons.Single(x => x.Name == "CopyErrorButton").Command);

        // Resume clears the exception (EditorGameRecoveryService, with the game running): the view comes back
        scene.Editor.GameHost.LastException = null;
        await WindowTestHelper.PumpAsync();
        Assert.False(pane.IsVisible);
        Assert.True(gameView.IsVisible);
    }
}
