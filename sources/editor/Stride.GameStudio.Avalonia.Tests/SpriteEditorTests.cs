// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using Stride.Assets.Editor.Avalonia.Behaviors;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.ViewModels.SpriteEditor;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Sprite;
using Stride.GameStudio.Avalonia.Services;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;
using Image = Avalonia.Controls.Image;
using ImageFileType = Stride.Graphics.ImageFileType;
using PixelFormat = Stride.Graphics.PixelFormat;
using Color = Stride.Core.Mathematics.Color;
using Rectangle = Stride.Core.Mathematics.Rectangle;
using RectangleF = Stride.Core.Mathematics.RectangleF;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The sprite editor in the shell: the view shows the sprites of the sheet, the image and the region of the selected sprite with
/// its handles; the keyboard copies, pastes and deletes sprites through the Edit commands, the magic wand picks a region with a
/// click on the image and the handles resize it.
/// </summary>
public sealed class SpriteEditorTests
{
    private const int ImageWidth = 64;
    private const int ImageHeight = 32;
    private static readonly Rectangle FirstSprite = new(4, 4, 10, 8);
    private static readonly Rectangle SecondSprite = new(30, 10, 20, 12);

    [AvaloniaFact]
    public async Task TheViewShowsTheSpritesAndTheRegionOfTheSelectedOne()
    {
        var (fixture, window) = await OpenAsync();
        using (fixture)
        {
            try
            {
                var (editor, view) = await OpenEditorAsync(fixture, window);

                Assert.Equal(["Frame 0", "Frame 1"], SpriteList(view).Items.OfType<SpriteInfoViewModel>().Select(x => x.Name));
                Assert.True(Part<Control>(view, "SpritePanel").IsVisible);
                // The headless platform draws nothing: its bitmaps don't keep the size of the image
                Assert.IsAssignableFrom<Bitmap>(Part<Image>(view, "SpriteImage").Source);
                Assert.Equal(ImageWidth, editor.Viewport.ContentWidth);
                Assert.Equal(ImageHeight, editor.Viewport.ContentHeight);

                // The region and its handles follow the zoom of the viewport
                var region = editor.CurrentSprite!.TextureRegion;
                var panel = Part<Control>(view, "RegionPanel");
                Assert.Equal(region.ActualLeft - 6, Canvas.GetLeft(panel), 3);
                Assert.Equal(region.ActualTop - 6, Canvas.GetTop(panel), 3);
                Assert.Equal(region.ActualWidth + 12, panel.Width, 3);
                Assert.True(Part<Control>(view, "RegionHandles").IsVisible);
                Assert.False(Part<Control>(view, "BorderHandles").IsVisible);
                Assert.True(Part<Control>(view, "SpriteCenterMarker").IsVisible);
                Assert.False(Part<Control>(view, "SpriteBordersRectangle").IsVisible);

                editor.Viewport.ZoomInCommand.Execute();
                await WindowTestHelper.PumpAsync();
                Assert.Equal(region.ActualLeft - 6, Canvas.GetLeft(panel), 3);

                // The border tool of a UI sheet shows the borders and their handles
                editor.Type = SpriteSheetType.UI;
                editor.ToggleToolModeCommand.Execute(SpriteSheetEditorToolMode.Borders);
                await WindowTestHelper.PumpAsync();
                Assert.True(Part<Control>(view, "SpriteBordersRectangle").IsVisible);
                Assert.True(Part<Control>(view, "BorderHandles").IsVisible);
                Assert.False(Part<Control>(view, "RegionHandles").IsVisible);
                Assert.False(Part<Control>(view, "SpriteCenterMarker").IsVisible);

                // Several sprites selected: the image panel asks for a single one
                editor.SelectedSprites.Add(editor.Sprites[1]);
                await WindowTestHelper.PumpAsync();
                Assert.False(Part<Control>(view, "SpritePanel").IsVisible);
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task TheKeyboardCopiesPastesAndDeletesSpritesWithOneUndoEntryEach()
    {
        var (fixture, window) = await OpenAsync();
        using (fixture)
        {
            try
            {
                var (editor, view) = await OpenEditorAsync(fixture, window);
                var list = SpriteList(view);
                Assert.True(list.Focus(), "The sprite list does not take the focus");
                var names = RecordUndoNames(fixture);

                window.KeyPressQwerty(PhysicalKey.C, RawInputModifiers.Control);
                await WindowTestHelper.PumpAsync();
                window.KeyPressQwerty(PhysicalKey.V, RawInputModifiers.Control);
                await WindowTestHelper.WaitUntilAsync(() => editor.Sprites.Count == 3, "the paste of the sprite");

                Assert.Equal(["Frame 0", "Frame 1", "Frame 0"], editor.Sprites.Select(x => x.Name));
                Assert.Same(editor.Sprites[2], Assert.Single(editor.SelectedSprites));

                Assert.True(list.Focus());
                window.KeyPressQwerty(PhysicalKey.Delete, RawInputModifiers.None);
                await WindowTestHelper.PumpAsync();

                Assert.Equal(["Frame 0", "Frame 1"], editor.Sprites.Select(x => x.Name));
                Assert.Equal(["Paste 1 images", "Delete sprites"], names);

                window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);
                await WindowTestHelper.PumpAsync();
                Assert.Equal(3, editor.Sprites.Count);
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task TheMagicWandPicksTheRegionUnderTheClick()
    {
        var (fixture, window) = await OpenAsync();
        using (fixture)
        {
            try
            {
                var (editor, view) = await OpenEditorAsync(fixture, window);
                var sprite = editor.CurrentSprite!;
                var names = RecordUndoNames(fixture);
                editor.ToggleUseMagicWandCommand.Execute();
                await WindowTestHelper.PumpAsync();
                Assert.False(Part<Control>(view, "ToolsCanvas").IsHitTestVisible);

                // A click in the middle of the second sprite of the image
                var image = Part<Image>(view, "SpriteImage");
                var scale = editor.Viewport.ScaleFactor;
                var point = image.TranslatePoint(new Point((SecondSprite.X + 5.5) * scale, (SecondSprite.Y + 5.5) * scale), window)!.Value;
                window.MouseMove(point);
                window.MouseDown(point, MouseButton.Left);
                window.MouseUp(point, MouseButton.Left);
                await WindowTestHelper.PumpAsync();

                Assert.Equal((RectangleF)SecondSprite, sprite.TextureRegion.Region);
                Assert.Equal(["Update selected region"], names);
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task DraggingTheRightHandleResizesTheRegion()
    {
        var (fixture, window) = await OpenAsync();
        using (fixture)
        {
            try
            {
                var (editor, view) = await OpenEditorAsync(fixture, window);
                var region = editor.CurrentSprite!.TextureRegion;
                var names = RecordUndoNames(fixture);
                var scale = editor.Viewport.ScaleFactor;
                var handle = Part<Control>(view, "RegionHandles").GetVisualDescendants().OfType<Thumb>()
                    .Single(x => x.HorizontalAlignment == HorizontalAlignment.Right && x.VerticalAlignment == VerticalAlignment.Stretch);

                var start = handle.TranslatePoint(new Point(handle.Bounds.Width / 2, handle.Bounds.Height / 2), window)!.Value;
                var end = start + new Point(5 * scale, 0);
                window.MouseMove(start);
                window.MouseDown(start, MouseButton.Left);
                window.MouseMove(end, RawInputModifiers.LeftMouseButton);
                window.MouseUp(end, MouseButton.Left);
                await WindowTestHelper.PumpAsync();

                Assert.Equal(new RectangleF(FirstSprite.X, FirstSprite.Y, FirstSprite.Width + 5, FirstSprite.Height), region.Region);
                Assert.Equal(["Update region size"], names);
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task DroppingOnTheSpriteListMovesSpritesAndImportsFiles()
    {
        var (fixture, window) = await OpenAsync();
        using (fixture)
        {
            try
            {
                var (editor, view) = await OpenEditorAsync(fixture, window);
                var list = SpriteList(view);
                var dragDrop = Interaction.GetBehaviors(list).OfType<SpriteListDragDropBehavior>().Single();
                var names = RecordUndoNames(fixture);

                // The second sprite dropped on the upper half of the first one goes before it
                var first = list.ContainerFromIndex(0)!;
                var upperHalf = first.TranslatePoint(new Point(first.Bounds.Width / 2, first.Bounds.Height / 4), list)!.Value;
                Assert.True(dragDrop.Drop([editor.Sprites[1]], upperHalf, KeyModifiers.None));
                await WindowTestHelper.PumpAsync();

                Assert.Equal(["Frame 1", "Frame 0"], editor.Sprites.Select(x => x.Name));
                Assert.Same(editor.Sprites[0], Assert.Single(editor.SelectedSprites));

                // An image file dropped below the sprites adds a sprite at the end of the sheet
                var source = editor.Sprites[0].Source!;
                var below = new Point(list.Bounds.Width / 2, list.Bounds.Height - 5);
                Assert.True(dragDrop.Drop([source], below, KeyModifiers.None));
                await WindowTestHelper.PumpAsync();

                Assert.Equal(3, editor.Sprites.Count);
                Assert.Equal(source, editor.Sprites[2].Source);
                Assert.Same(editor.Sprites[2], Assert.Single(editor.SelectedSprites));
                Assert.Equal(["Move images", "Import images"], names);

                // Anything else is refused
                Assert.False(dragDrop.Drop(["text"], below, KeyModifiers.None));
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    private static async Task<(ShellSessionFixture Fixture, MainWindow Window)> OpenAsync()
    {
        // The sprites reference the image by its absolute path
        var imagePath = Path.Combine(Path.GetTempPath(), "Stride.GameStudio.Avalonia.Tests", "Sprites", Guid.NewGuid().ToString("N"), "sheet.png");
        WriteTestImage(imagePath);
        var asset = new SpriteSheetAsset();
        asset.Sprites.Add(new SpriteInfo { Name = "Frame 0", Source = new Core.IO.UFile(imagePath), TextureRegion = FirstSprite });
        asset.Sprites.Add(new SpriteInfo { Name = "Frame 1", Source = new Core.IO.UFile(imagePath), TextureRegion = SecondSprite });
        var fixture = ShellSessionFixture.Create(("Sheet", asset));
        var window = new MainWindow { DataContext = fixture.Main, Width = 1600, Height = 1000 };
        window.Show();
        // The shell registers the clipboard of its main window before the session opens; the headless platform keeps one in memory
        fixture.ServiceProvider.RegisterService(new ClipboardService(window.Clipboard!));
        Assert.True(await fixture.Main.OpenSession(fixture.PackagePath));
        await WindowTestHelper.PumpAsync();
        return (fixture, window);
    }

    /// <summary>
    /// Opens the editor of the sprite sheet, the way a double-click in the asset view does, with its first sprite selected.
    /// </summary>
    private static async Task<(SpriteSheetEditorViewModel Editor, SpriteSheetEditorView View)> OpenEditorAsync(ShellSessionFixture fixture, Window window)
    {
        var sheet = fixture.Session.AllAssets.OfType<SpriteSheetViewModel>().Single();
        fixture.Session.AssetCollection.SelectAssets([sheet]);
        await WindowTestHelper.PumpAsync();
        fixture.Session.EditSelectedContentCommand.Execute();
        await WindowTestHelper.WaitUntilAsync(() => window.GetVisualDescendants().OfType<SpriteSheetEditorView>().Any(), "the sprite editor");

        var editor = Assert.IsType<SpriteSheetEditorViewModel>(fixture.Session.EditorCollection.ActiveEditor);
        var view = window.GetVisualDescendants().OfType<SpriteSheetEditorView>().Single();
        editor.SelectedSprites.Clear();
        editor.SelectedSprites.Add(editor.Sprites[0]);
        await WindowTestHelper.WaitUntilAsync(() => editor.CurrentSprite == editor.Sprites[0], "the selected sprite");
        await WindowTestHelper.PumpAsync();
        return (editor, view);
    }

    private static List<string?> RecordUndoNames(ShellSessionFixture fixture)
    {
        var undoRedo = fixture.Session.UndoRedoService;
        var names = new List<string?>();
        // The property grid of a sprite completes empty transactions as it binds its editors; the history ignores them
        undoRedo.Done += (_, e) =>
        {
            if (e.Transaction.Operations.Count > 0)
                names.Add(undoRedo.GetName(e.Transaction));
        };
        return names;
    }

    private static ListBox SpriteList(SpriteSheetEditorView view) => Part<ListBox>(view, "SpriteList");

    private static T Part<T>(SpriteSheetEditorView view, string name) where T : Control
    {
        return view.GetVisualDescendants().OfType<T>().Single(x => x.Name == name);
    }

    /// <summary>
    /// Writes the test image: transparent, with two opaque sprites.
    /// </summary>
    private static void WriteTestImage(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image = Stride.Graphics.Image.New2D(ImageWidth, ImageHeight, 1, PixelFormat.R8G8B8A8_UNorm);
        var pixels = image.PixelBuffer[0];
        for (var y = 0; y < ImageHeight; ++y)
        {
            for (var x = 0; x < ImageWidth; ++x)
            {
                var color = FirstSprite.Contains(x, y) ? Color.Blue : SecondSprite.Contains(x, y) ? Color.Red : Color.Transparent;
                pixels.SetPixel(x, y, color);
            }
        }
        using var stream = File.Create(path);
        image.Save(stream, ImageFileType.Png);
    }
}
