// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.ViewModels.SpriteEditor;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Sprite;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModel.CopyPasteProcessors;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The sprite sheet editor, ported from the WPF sprite editor: the commands on the sprites, and the region, borders and center
/// of the selected sprite. Each change is one entry of the undo history, with the names of the WPF editor.
/// </summary>
public sealed class SpriteSheetEditorTests
{
    /// <summary>
    /// The size of the test image.
    /// </summary>
    private const int ImageWidth = 64;
    private const int ImageHeight = 32;

    /// <summary>
    /// The two opaque sprites of the test image, on transparent pixels; the second one is red.
    /// </summary>
    private static readonly Rectangle FirstSprite = new(4, 4, 10, 8);
    private static readonly Rectangle SecondSprite = new(30, 10, 20, 12);

    [Fact]
    public async Task TheEditorListsTheSpritesInOrderWithTheirImageSize()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var editor = test.Editor;

        Assert.Equal(["Frame 0", "Frame 1", "Frame 2"], editor.Sprites.Select(x => x.Name));
        Assert.Equal([0, 1, 2], editor.Sprites.Select(x => x.Index));
        Assert.Equal(ImageWidth, editor.Sprites[0].TextureRegion.ImageWidth);
        Assert.Equal(ImageHeight, editor.Sprites[0].TextureRegion.ImageHeight);
        Assert.Equal((RectangleF)FirstSprite, editor.Sprites[0].TextureRegion.Region);
        Assert.Null(editor.CurrentSprite);
    }

    [Fact]
    public async Task SelectingASingleSpriteMakesItTheCurrentSprite()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var editor = test.Editor;

        editor.SelectedSprites.Add(editor.Sprites[1]);
        await test.WaitForCurrentSprite(editor.Sprites[1]);

        editor.SelectedSprites.Add(editor.Sprites[2]);
        await test.WaitForCurrentSprite(null);
    }

    [Fact]
    public async Task TheCommandsOnTheSelectionNeedASelection()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var editor = test.Editor;

        Assert.False(editor.RemoveImageCommand.IsEnabled);
        Assert.False(editor.DuplicateSelectedImagesCommand.IsEnabled);
        Assert.False(editor.CopyImageCommand.IsEnabled);
        Assert.False(editor.UseWholeImageCommand.IsEnabled);
        Assert.False(editor.MoveImageCommand.IsEnabled);
        Assert.False(editor.RenameCommand.IsEnabled);
        Assert.True(editor.AddNewImageCommand.IsEnabled);
        Assert.True(editor.PasteImageCommand.IsEnabled);

        editor.SelectedSprites.Add(editor.Sprites[0]);

        Assert.True(editor.RemoveImageCommand.IsEnabled);
        Assert.True(editor.DuplicateSelectedImagesCommand.IsEnabled);
        Assert.True(editor.CopyImageCommand.IsEnabled);
        Assert.True(editor.UseWholeImageCommand.IsEnabled);
        Assert.True(editor.MoveImageCommand.IsEnabled);
        Assert.True(editor.RenameCommand.IsEnabled);

        editor.SelectedSprites.Add(editor.Sprites[1]);

        Assert.False(editor.RenameCommand.IsEnabled);
    }

    [Fact]
    public async Task AddNewSpriteAddsAndSelectsItAsOneUndoEntry()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var editor = test.Editor;
        var names = test.RecordUndoNames();

        editor.AddNewImageCommand.Execute();

        Assert.Equal(["Frame 0", "Frame 1", "Frame 2", "New sprite"], editor.Sprites.Select(x => x.Name));
        Assert.Same(editor.Sprites[3], Assert.Single(editor.SelectedSprites));
        Assert.Equal(["Add new sprite"], names);

        test.Undo();

        Assert.Equal(3, editor.Sprites.Count);
        Assert.Equal(3, test.Asset.Asset.Sprites.Count);
        Assert.Empty(editor.SelectedSprites);

        test.Redo();

        Assert.Equal("New sprite", editor.Sprites[3].Name);
    }

    [Fact]
    public async Task DeleteRemovesTheSelectedSpritesAndSelectsTheNextOneAsOneUndoEntry()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var editor = test.Editor;
        editor.SelectedSprites.Add(editor.Sprites[0]);
        editor.SelectedSprites.Add(editor.Sprites[1]);
        var names = test.RecordUndoNames();

        editor.RemoveImageCommand.Execute();

        Assert.Equal(["Frame 2"], editor.Sprites.Select(x => x.Name));
        Assert.Equal([0], editor.Sprites.Select(x => x.Index));
        Assert.Same(editor.Sprites[0], Assert.Single(editor.SelectedSprites));
        Assert.Equal(["Delete sprites"], names);

        test.Undo();

        Assert.Equal(["Frame 0", "Frame 1", "Frame 2"], editor.Sprites.Select(x => x.Name));
        Assert.Equal((RectangleF)FirstSprite, editor.Sprites[0].TextureRegion.Region);
    }

    [Fact]
    public async Task DuplicateAddsCopiesAtTheEndAndSelectsThemAsOneUndoEntry()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var editor = test.Editor;
        editor.SelectedSprites.Add(editor.Sprites[1]);
        var names = test.RecordUndoNames();

        editor.DuplicateSelectedImagesCommand.Execute();

        Assert.Equal(["Frame 0", "Frame 1", "Frame 2", "Frame 1"], editor.Sprites.Select(x => x.Name));
        var copy = editor.Sprites[3];
        Assert.Same(copy, Assert.Single(editor.SelectedSprites));
        Assert.NotSame(editor.Sprites[1].GetSpriteInfo(), copy.GetSpriteInfo());
        Assert.Equal(editor.Sprites[1].TextureRegion.Region, copy.TextureRegion.Region);
        Assert.Equal(["Duplicate 1 images"], names);

        test.Undo();

        Assert.Equal(3, editor.Sprites.Count);
    }

    [Fact]
    public async Task MoveReordersTheSelectedSpritesAsOneUndoEntry()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var editor = test.Editor;
        editor.SelectedSprites.Add(editor.Sprites[0]);
        var names = test.RecordUndoNames();

        editor.MoveImageCommand.Execute(1);

        Assert.Equal(["Frame 1", "Frame 0", "Frame 2"], editor.Sprites.Select(x => x.Name));
        Assert.Equal("Frame 0", Assert.IsType<SpriteInfoViewModel>(Assert.Single(editor.SelectedSprites)).Name);

        // The first sprite cannot go up: nothing changes
        editor.SelectedSprites.Clear();
        editor.SelectedSprites.Add(editor.Sprites[0]);
        editor.MoveImageCommand.Execute(-1);

        Assert.Equal(["Frame 1", "Frame 0", "Frame 2"], editor.Sprites.Select(x => x.Name));
        Assert.Equal(["Reorder images"], names);

        test.Undo();

        Assert.Equal(["Frame 0", "Frame 1", "Frame 2"], editor.Sprites.Select(x => x.Name));
    }

    [Fact]
    public async Task DroppingSpritesOnASpriteMovesThemAsOneUndoEntry()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var editor = test.Editor;
        var names = test.RecordUndoNames();
        IInsertChildViewModel target = editor.Sprites[0];

        Assert.True(target.CanInsertChildren([editor.Sprites[2]], InsertPosition.After, AddChildModifiers.None, out _));
        Assert.False(target.CanInsertChildren(["text"], InsertPosition.After, AddChildModifiers.None, out _));
        target.InsertChildren([editor.Sprites[2]], InsertPosition.After, AddChildModifiers.None);

        Assert.Equal(["Frame 0", "Frame 2", "Frame 1"], editor.Sprites.Select(x => x.Name));
        Assert.Equal([0, 1, 2], editor.Sprites.Select(x => x.Index));
        Assert.Same(editor.Sprites[1], Assert.Single(editor.SelectedSprites));
        Assert.Equal(["Move images"], names);

        test.Undo();

        Assert.Equal(["Frame 0", "Frame 1", "Frame 2"], editor.Sprites.Select(x => x.Name));
    }

    [Fact]
    public async Task DroppingImageFilesAddsSpritesAsOneUndoEntry()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var editor = test.Editor;
        var source = editor.Sprites[0].Source!;
        var names = test.RecordUndoNames();
        IAddChildViewModel target = editor;

        Assert.True(target.CanAddChildren([source, source], AddChildModifiers.None, out _));
        target.AddChildren([source, source], AddChildModifiers.None);

        Assert.Equal(5, editor.Sprites.Count);
        Assert.All(editor.Sprites.Skip(3), x => Assert.Equal(source, x.Source));
        // As in WPF, the sprite of a dropped file starts with an empty region
        Assert.Equal(default, editor.Sprites[3].TextureRegion.Region);
        Assert.Equal([editor.Sprites[3], editor.Sprites[4]], editor.SelectedSprites);
        Assert.Equal(["Import images"], names);
    }

    [Fact]
    public async Task CopyThenPasteAddsTheCopiedSpritesAsOneUndoEntry()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var editor = test.Editor;
        editor.SelectedSprites.Add(editor.Sprites[0]);
        editor.SelectedSprites.Add(editor.Sprites[2]);
        var names = test.RecordUndoNames();

        editor.CopyImageCommand.Execute();
        Assert.False(string.IsNullOrEmpty(test.Clipboard.Text));
        editor.PasteImageCommand.Execute();

        Assert.Equal(["Frame 0", "Frame 1", "Frame 2", "Frame 0", "Frame 2"], editor.Sprites.Select(x => x.Name));
        Assert.Equal((RectangleF)FirstSprite, editor.Sprites[3].TextureRegion.Region);
        Assert.Equal([editor.Sprites[3], editor.Sprites[4]], editor.SelectedSprites);
        Assert.Equal(["Paste 2 images"], names);

        test.Undo();

        Assert.Equal(3, editor.Sprites.Count);
    }

    [Fact]
    public async Task PasteIgnoresAClipboardWithoutSprites()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var editor = test.Editor;
        test.Clipboard.Text = "Some text";
        var names = test.RecordUndoNames();

        editor.PasteImageCommand.Execute();

        Assert.Equal(3, editor.Sprites.Count);
        Assert.Empty(names);
    }

    [Fact]
    public async Task RenameEditsTheNameOfTheSelectedSprite()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var editor = test.Editor;
        var sprite = editor.Sprites[1];
        editor.SelectedSprites.Add(sprite);

        editor.RenameCommand.Execute();

        Assert.True(sprite.IsEditing);
        // The keys of the editor go to the name while it is edited
        Assert.False(editor.FocusOnRegionCommand.IsEnabled);
        Assert.False(editor.ToggleUseMagicWandCommand.IsEnabled);

        var names = test.RecordUndoNames();
        sprite.Name = "Walk";
        sprite.IsEditing = false;

        Assert.Equal("Walk", test.Asset.Asset.Sprites[1].Name);
        Assert.True(editor.FocusOnRegionCommand.IsEnabled);
        Assert.Single(names);
    }

    [Fact]
    public async Task UseWholeImageSetsTheRegionOfTheSelectedSpritesAsOneUndoEntry()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var editor = test.Editor;
        editor.SelectedSprites.Add(editor.Sprites[0]);
        editor.SelectedSprites.Add(editor.Sprites[1]);
        var names = test.RecordUndoNames();

        editor.UseWholeImageCommand.Execute();

        Assert.Equal(new RectangleF(0, 0, ImageWidth, ImageHeight), editor.Sprites[0].TextureRegion.Region);
        Assert.Equal(new RectangleF(0, 0, ImageWidth, ImageHeight), editor.Sprites[1].TextureRegion.Region);
        Assert.Equal(["Use whole sprite"], names);

        test.Undo();

        Assert.Equal((RectangleF)FirstSprite, editor.Sprites[0].TextureRegion.Region);
    }

    [Fact]
    public async Task TheRegionIsClampedToTheImage()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var region = test.Editor.Sprites[0].TextureRegion;

        region.Region = new RectangleF(-5, 20, 100, 30);

        Assert.Equal(new RectangleF(0, 20, ImageWidth, ImageHeight - 20), region.Region);
    }

    [Fact]
    public async Task DraggingAHandleResizesTheRegionAsOneUndoEntry()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var editor = test.Editor;
        var region = editor.Sprites[0].TextureRegion;
        // The view is zoomed twice: a change of 2 in the view is one pixel of the image
        editor.Viewport.ScaleFactor = 2;
        var names = test.RecordUndoNames();
        IResizingTarget target = region;

        target.OnResizingStarted(ResizingDirection.BottomRight);
        target.OnResizingDelta(ResizingDirection.BottomRight, 4, 2);
        target.OnResizingDelta(ResizingDirection.BottomRight, 2, 2);
        target.OnResizingCompleted(ResizingDirection.BottomRight, 6, 4);

        Assert.Equal(new RectangleF(FirstSprite.X, FirstSprite.Y, FirstSprite.Width + 3, FirstSprite.Height + 2), region.Region);
        Assert.Equal(["Update region size"], names);
        Assert.Equal(2 * FirstSprite.X, region.ActualLeft);
        Assert.Equal(2 * (FirstSprite.Width + 3), region.ActualWidth);

        test.Undo();

        Assert.Equal((RectangleF)FirstSprite, region.Region);
    }

    [Fact]
    public async Task DraggingTheRegionMovesItInsideTheImage()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var region = test.Editor.Sprites[0].TextureRegion;
        var names = test.RecordUndoNames();
        IResizingTarget target = region;

        target.OnResizingStarted(ResizingDirection.Center);
        target.OnResizingDelta(ResizingDirection.Center, -100, 5);
        target.OnResizingCompleted(ResizingDirection.Center, -100, 5);

        // The region keeps its size
        Assert.Equal(new RectangleF(0, FirstSprite.Y + 5, FirstSprite.Width, FirstSprite.Height), region.Region);
        Assert.Equal(["Move region"], names);
    }

    [Fact]
    public async Task DraggingABorderHandleResizesTheBordersAsOneUndoEntry()
    {
        using var test = await SpriteSheetTest.OpenAsync(SpriteSheetType.UI);
        var borders = test.Editor.Sprites[1].SpriteBorders;
        var names = test.RecordUndoNames();
        IResizingTarget target = borders;

        target.OnResizingStarted(ResizingDirection.TopLeft);
        target.OnResizingDelta(ResizingDirection.TopLeft, 3, 2);
        target.OnResizingCompleted(ResizingDirection.TopLeft, 3, 2);
        target.OnResizingStarted(ResizingDirection.Right);
        target.OnResizingDelta(ResizingDirection.Right, -4, 0);
        target.OnResizingCompleted(ResizingDirection.Right, -4, 0);

        Assert.Equal(new Vector4(3, 2, 4, 0), borders.Borders);
        Assert.Equal(["Update sprite TopLeft borders.", "Update sprite Right border"], names);

        test.Undo();

        Assert.Equal(new Vector4(3, 2, 0, 0), borders.Borders);
    }

    [Fact]
    public async Task TheBordersAreClampedToTheRegion()
    {
        using var test = await SpriteSheetTest.OpenAsync(SpriteSheetType.UI);
        var borders = test.Editor.Sprites[1].SpriteBorders;

        borders.Borders = new Vector4(15.4f, -2, 0, 3);
        Assert.Equal(new Vector4(15, 0, 0, 3), borders.Borders);

        // The second sprite is 20 wide: the left border leaves 5 pixels to the right one
        borders.Borders = borders.Borders with { Z = 10 };
        Assert.Equal(new Vector4(15, 0, 5, 3), borders.Borders);
    }

    [Fact]
    public async Task LockedBordersKeepTheirDistanceToTheSidesWhenTheRegionIsResized()
    {
        using var test = await SpriteSheetTest.OpenAsync(SpriteSheetType.UI);
        var sprite = test.Editor.Sprites[1];
        sprite.SpriteBorders.Borders = new Vector4(2, 2, 2, 2);
        sprite.SpriteBorders.Locked = true;

        // The left side moves 3 pixels to the left: the left border grows by 3
        sprite.TextureRegion.Region = new RectangleF(SecondSprite.X - 3, SecondSprite.Y, SecondSprite.Width + 3, SecondSprite.Height);

        Assert.Equal(new Vector4(5, 2, 2, 2), sprite.SpriteBorders.Borders);

        sprite.SpriteBorders.Locked = false;
        sprite.TextureRegion.Region = SecondSprite;

        Assert.Equal(new Vector4(5, 2, 2, 2), sprite.SpriteBorders.Borders);
    }

    [Fact]
    public async Task DraggingTheCenterMovesItAsOneUndoEntry()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var center = test.Editor.Sprites[0].SpriteCenter;
        var names = test.RecordUndoNames();
        IResizingTarget target = center;

        target.OnResizingStarted(ResizingDirection.Center);
        target.OnResizingDelta(ResizingDirection.Center, 3, -2);
        target.OnResizingCompleted(ResizingDirection.Center, 3, -2);

        Assert.Equal(new Vector2(3, -2), center.Center);
        Assert.Equal(["Move sprite center"], names);
        // From the middle of the region (10 x 8) by default
        Assert.True(center.CenterFromMiddle);
        Assert.Equal(new Vector2(3 + 5, -2 + 4), center.ActualCenter);

        test.Undo();

        Assert.Equal(Vector2.Zero, center.Center);
    }

    [Fact]
    public async Task TheMagicWandSetsTheRegionToTheSpriteUnderTheMouseAsOneUndoEntry()
    {
        using var test = await SpriteSheetTest.OpenAsync(SpriteSheetType.UI);
        var editor = test.Editor;
        var sprite = editor.Sprites[0];
        sprite.SpriteBorders.Borders = new Vector4(1, 1, 1, 1);
        editor.SelectedSprites.Add(sprite);
        await test.WaitForCurrentSprite(sprite);
        editor.ToggleUseMagicWandCommand.Execute();
        Assert.Equal(SpriteSheetEditorToolMode.MagicWand, editor.ToolMode);
        Assert.True(editor.UsingPixelTool);
        var names = test.RecordUndoNames();

        editor.FindSpriteRegionCommand.Execute(new SpriteImagePick(new Vector2(35.5f, 15.2f), AddToRegion: false));

        Assert.Equal((RectangleF)SecondSprite, sprite.TextureRegion.Region);
        // A new region resets the borders
        Assert.Equal(Vector4.Zero, sprite.SpriteBorders.Borders);
        Assert.Equal(["Update selected region"], names);

        // Ctrl adds the sprite under the mouse to the region
        editor.FindSpriteRegionCommand.Execute(new SpriteImagePick(new Vector2(5, 5), AddToRegion: true));

        Assert.Equal((RectangleF)Rectangle.Union(FirstSprite, SecondSprite), sprite.TextureRegion.Region);

        // A transparent pixel finds no sprite
        editor.FindSpriteRegionCommand.Execute(new SpriteImagePick(new Vector2(0, 0), AddToRegion: false));

        Assert.Equal((RectangleF)Rectangle.Union(FirstSprite, SecondSprite), sprite.TextureRegion.Region);
        Assert.Equal(2, names.Count);
    }

    [Fact]
    public async Task TheMagicWandCanDelimitTheSpritesWithTheColorKey()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var editor = test.Editor;
        var sprite = editor.Sprites[0];
        editor.SelectedSprites.Add(sprite);
        await test.WaitForCurrentSprite(sprite);
        // The red sprite is the color key: the rest of the image (transparent included) is one sprite
        editor.ColorKey = Color.Red;
        editor.ColorKeyEnabled = true;
        Assert.False(editor.MagicWandUseTransparency);
        editor.ToolMode = SpriteSheetEditorToolMode.MagicWand;

        editor.FindSpriteRegionCommand.Execute(new SpriteImagePick(new Vector2(1, 1), AddToRegion: false));

        Assert.Equal(new RectangleF(0, 0, ImageWidth, ImageHeight), sprite.TextureRegion.Region);
    }

    [Fact]
    public async Task ThePickerSetsTheColorKeyAndEndsTheTool()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var editor = test.Editor;
        editor.SelectedSprites.Add(editor.Sprites[0]);
        await test.WaitForCurrentSprite(editor.Sprites[0]);
        editor.ToggleToolModeCommand.Execute(SpriteSheetEditorToolMode.ColorPick);
        var names = test.RecordUndoNames();

        editor.FindSpriteRegionCommand.Execute(new SpriteImagePick(new Vector2(SecondSprite.X + 1, SecondSprite.Y + 1), AddToRegion: false));

        Assert.Equal(Color.Red, editor.ColorKey);
        Assert.Equal(Color.Red, test.Asset.Asset.ColorKeyColor);
        Assert.Equal(SpriteSheetEditorToolMode.None, editor.ToolMode);
        Assert.Single(names);

        // A second toggle of a tool ends it
        editor.ToggleToolModeCommand.Execute(SpriteSheetEditorToolMode.Borders);
        editor.ToggleToolModeCommand.Execute(SpriteSheetEditorToolMode.Borders);
        Assert.Equal(SpriteSheetEditorToolMode.None, editor.ToolMode);
    }

    [Fact]
    public async Task SelectNextSpriteCyclesThroughTheSprites()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var editor = test.Editor;
        editor.SelectedSprites.Add(editor.Sprites[2]);

        editor.SelectNextSpriteCommand.Execute(1);

        Assert.Same(editor.Sprites[0], Assert.Single(editor.SelectedSprites));

        editor.SelectNextSpriteCommand.Execute(-1);

        Assert.Same(editor.Sprites[2], Assert.Single(editor.SelectedSprites));
    }

    [Fact]
    public async Task TheSpriteSheetPropertiesAreUndoable()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var editor = test.Editor;
        var names = test.RecordUndoNames();

        editor.Type = SpriteSheetType.UI;

        Assert.Equal(SpriteSheetType.UI, test.Asset.Asset.Type);
        Assert.Single(names);

        test.Undo();

        Assert.Equal(SpriteSheetType.Sprite2D, editor.Type);
    }

    [Fact]
    public void TheViewportZoomsAroundAPoint()
    {
        var viewport = new ViewportViewModel(new Stride.Core.Presentation.ViewModels.ViewModelServiceProvider([new Stride.Core.Presentation.Services.NullDispatcherService()]))
        {
            ViewportWidth = 200,
            ViewportHeight = 100,
            ContentWidth = 64,
            ContentHeight = 32,
        };

        viewport.ZoomIn(100, 50);

        Assert.True(viewport.ScaleFactor > 1);
        // The point under the mouse stays where it is
        Assert.Equal(viewport.ScaleFactor * 100 - 100, viewport.HorizontalOffset, 6);
        Assert.Equal(viewport.ScaleFactor * 64, viewport.ActualContentWidth, 6);

        viewport.ScaleToRealSize();

        Assert.Equal(1, viewport.ScaleFactor);

        viewport.FitOnScreen();

        // (100 - 20 margins) / 32
        Assert.Equal(2.5, viewport.ScaleFactor, 6);
    }

    [Fact]
    public async Task FocusOnRegionScrollsToTheMiddleOfTheRegion()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var editor = test.Editor;
        editor.SelectedSprites.Add(editor.Sprites[1]);
        await test.WaitForCurrentSprite(editor.Sprites[1]);
        editor.Viewport.ViewportWidth = 20;
        editor.Viewport.ViewportHeight = 10;

        editor.FocusOnRegionCommand.Execute();

        Assert.Equal(SecondSprite.Center.X - 10, editor.Viewport.HorizontalOffset);
        Assert.Equal(SecondSprite.Center.Y - 5, editor.Viewport.VerticalOffset);
    }

    [Fact]
    public async Task TheImageCacheGivesAnRgbaImageAndReloadsAChangedFile()
    {
        using var test = await SpriteSheetTest.OpenAsync();
        var cache = test.Editor.Cache!;
        var source = test.Editor.Sprites[0].Source;

        var image = cache.RetrieveImage(source);

        Assert.NotNull(image);
        // The cache keeps 8-bit RGBA and BGRA images, and converts the other formats
        Assert.Contains(image.Description.Format, new[] { PixelFormat.R8G8B8A8_UNorm, PixelFormat.B8G8R8A8_UNorm });
        Assert.Same(image, cache.RetrieveImage(source));
        Assert.Equal(new Size2(ImageWidth, ImageHeight), cache.GetPixelSize(source));
        Assert.Null(cache.PickPixelColor(source, new Vector2(ImageWidth, 0)));

        File.SetLastWriteTime(source!.ToOSPath(), DateTime.Now.AddMinutes(1));

        Assert.NotSame(image, cache.RetrieveImage(source));
        Assert.Null(cache.RetrieveImage(new Stride.Core.IO.UFile("/no/such/file.png")));
    }

    /// <summary>
    /// A sprite sheet of three sprites on the test image, opened in its editor.
    /// </summary>
    private sealed class SpriteSheetTest : IDisposable
    {
        private readonly AssetsSessionFixture fixture;
        private readonly string imageDirectory;

        private SpriteSheetTest(AssetsSessionFixture fixture, string imageDirectory, SpriteSheetEditorViewModel editor)
        {
            this.fixture = fixture;
            this.imageDirectory = imageDirectory;
            Editor = editor;
        }

        public SpriteSheetEditorViewModel Editor { get; }

        public SpriteSheetViewModel Asset => Editor.Asset;

        public Stride.Core.Presentation.Services.IUndoRedoService UndoRedo => fixture.Session.UndoRedoService;

        public FakeClipboardService Clipboard => (FakeClipboardService)fixture.ServiceProvider.Get<IClipboardService>();

        public static async Task<SpriteSheetTest> OpenAsync(SpriteSheetType type = SpriteSheetType.Sprite2D)
        {
            var imageDirectory = Path.Combine(Path.GetTempPath(), "Stride.Assets.Editor.Tests", "Sprites", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(imageDirectory);
            var imagePath = Path.Combine(imageDirectory, "sheet.png");
            WriteTestImage(imagePath);

            var asset = new SpriteSheetAsset { Type = type };
            for (var i = 0; i < 3; ++i)
            {
                asset.Sprites.Add(new SpriteInfo
                {
                    Name = $"Frame {i}",
                    Source = new Stride.Core.IO.UFile(imagePath),
                    TextureRegion = i == 1 ? SecondSprite : FirstSprite,
                });
            }
            var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: true, ("Sheet", asset));
            Assert.False(fixture.Result.HasErrors, fixture.Log);
            // The editor plugin of the view registers this processor in the application
            fixture.ServiceProvider.Get<ICopyPasteService>().RegisterProcessor(new AssetPropertyPasteProcessor());
            var editor = new SpriteSheetEditorViewModel(fixture.Asset<SpriteSheetViewModel>("Sheet"));
            Assert.True(await editor.Initialize());
            var test = new SpriteSheetTest(fixture, imageDirectory, editor);
            test.WaitForProperties();
            return test;
        }

        public List<string?> RecordUndoNames()
        {
            WaitForProperties();
            var undoRedo = UndoRedo;
            var names = new List<string?>();
            undoRedo.Done += (_, e) => names.Add(undoRedo.GetName(e.Transaction));
            return names;
        }

        public void Undo()
        {
            WaitForProperties();
            UndoRedo.Undo();
        }

        public void Redo()
        {
            WaitForProperties();
            UndoRedo.Redo();
        }

        /// <summary>
        /// Waits for the property grid of the selection: it builds its nodes on another thread, which must not run while the
        /// test changes the asset (the dispatcher of the test session has no thread of its own).
        /// </summary>
        public void WaitForProperties()
        {
            var properties = Editor.EditorProperties;
            for (var i = 0; i < 250 && !properties.CanDisplayProperties && properties.FallbackMessage == "Loading properties..."; ++i)
            {
                Thread.Sleep(20);
            }
        }

        /// <summary>
        /// Waits for the property grid of the selection, which sets the current sprite.
        /// </summary>
        public async Task WaitForCurrentSprite(SpriteInfoViewModel? expected)
        {
            for (var i = 0; i < 100 && Editor.CurrentSprite != expected; ++i)
            {
                await Task.Delay(20);
            }
            Assert.Same(expected, Editor.CurrentSprite);
        }

        public void Dispose()
        {
            Editor.Destroy();
            fixture.Dispose();
            try
            {
                Directory.Delete(imageDirectory, true);
            }
            catch (IOException)
            {
                // Best effort
            }
        }

        /// <summary>
        /// Writes the test image: transparent, with two opaque sprites, the second one red.
        /// </summary>
        private static void WriteTestImage(string path)
        {
            using var image = Image.New2D(ImageWidth, ImageHeight, 1, PixelFormat.R8G8B8A8_UNorm);
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
}
