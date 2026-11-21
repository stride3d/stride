// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Mathematics;
using Stride.GameStudio.Avalonia.Tests.Helpers;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The selection rectangle of the scene view: the game draws it from a left drag (<c>EditorViewToolSetTests</c>), the view
/// shows it over the view that has the input, and the toolbar turns it on.
/// </summary>
public sealed class SceneSelectionRectangleTests
{
    [AvaloniaFact]
    public async Task TheRectangleOfTheGameShowsOverTheView()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        // The headless editor has no game: a control stands for the view of the game
        var layout = scene.View.GetControl<EditorViewLayoutPanel>("GameViews");
        var gameView = new Border { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        layout.GameView = gameView;
        var rectangles = new FakeSelectionRectangle();
        var toolbar = AttachToolbar(scene, rectangles);
        try
        {
            // The toolbar of the view turns the rectangle selection on
            Assert.True(rectangles.IsRectangleSelectionEnabled);
            await WindowTestHelper.PumpAsync();
            var box = scene.View.GetControl<Border>("SelectionRectangleBox");
            Assert.False(box.IsVisible);

            rectangles.Raise(new RectangleF(0.25f, 0.5f, 0.5f, 0.25f));
            await WindowTestHelper.WaitUntilAsync(() => box.IsVisible, "the rectangle");

            var layer = scene.View.GetControl<Canvas>("SelectionRectangleLayer");
            var origin = gameView.TranslatePoint(default, layer)!.Value;
            var size = gameView.Bounds.Size;
            Assert.True(size.Width > 100 && size.Height > 100, $"The view is {size}.");
            Assert.Equal(origin.X + size.Width * 0.25, Canvas.GetLeft(box), 3);
            Assert.Equal(origin.Y + size.Height * 0.5, Canvas.GetTop(box), 3);
            Assert.Equal(size.Width * 0.5, box.Width, 3);
            Assert.Equal(size.Height * 0.25, box.Height, 3);
            // The pointer goes through it to the game
            Assert.False(layer.IsHitTestVisible);

            // Released or canceled
            rectangles.Raise(null);
            await WindowTestHelper.WaitUntilAsync(() => !box.IsVisible, "the rectangle to go");
        }
        finally
        {
            scene.Editor.ViewOptions = null;
            toolbar.Destroy();
        }
        // The toolbar turns it off when it goes
        Assert.False(rectangles.IsRectangleSelectionEnabled);
    }

    private static EditorViewToolbarViewModel AttachToolbar(SceneTreeDropTests.SceneFixture scene, FakeSelectionRectangle rectangles)
    {
        var editor = scene.Editor;
        var toolbar = new EditorViewToolbarViewModel(editor.ServiceProvider, new SceneViewContextMenuTests.FakeToolService(), null, null, selectionRectangle: rectangles);
        editor.ViewOptions = toolbar;
        return toolbar;
    }

    /// <summary>
    /// The rectangle selection of a view without a game.
    /// </summary>
    private sealed class FakeSelectionRectangle : IEditorGameSelectionRectangleService
    {
        public bool IsRectangleSelectionEnabled { get; set; }

        public event EventHandler<RectangleF?>? SelectionRectangleChanged;

        public void Raise(RectangleF? rectangle) => SelectionRectangleChanged?.Invoke(this, rectangle);
    }
}
