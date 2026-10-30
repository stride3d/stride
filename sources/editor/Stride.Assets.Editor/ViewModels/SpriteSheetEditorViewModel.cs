// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Assets.Editor.ViewModels.SpriteEditor;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Sprite;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// The editor of a sprite sheet, ported from the WPF sprite editor: the list of the sprites (add, delete, duplicate, reorder,
/// copy and paste), and the region, borders and center of the selected sprite, which the tools of the view edit. Each change
/// is one entry of the undo history, with the names of the WPF editor.
/// </summary>
[AssetEditorViewModel<SpriteSheetViewModel>]
public sealed class SpriteSheetEditorViewModel : AssetEditorViewModel<SpriteSheetViewModel>, IAddChildViewModel
{
    private readonly ObservableList<SpriteInfoViewModel> sprites;
    private readonly IObjectNode spritesNode;
    private readonly MemberGraphNodeBinding<Color> colorKeyNodeBinding;
    private readonly MemberGraphNodeBinding<bool> colorKeyEnabledNodeBinding;
    private readonly MemberGraphNodeBinding<SpriteSheetType> typeNodeBinding;

    private Color4 borderColor = new(1.0f, 0.0f, 0.0f, 1.0f);
    private SpriteInfoViewModel? currentSprite;
    private int propertiesGeneration;
    private bool magicWandUseTransparency;
    private bool selectionHighlightEnabled;
    private SpriteSheetEditorToolMode toolMode;

    public SpriteSheetEditorViewModel(SpriteSheetViewModel spriteSheet)
        : base(spriteSheet)
    {
        Viewport = new ViewportViewModel(ServiceProvider);

        // Commands
        DisplaySpriteSheetPropertiesCommand = new AnonymousCommand(ServiceProvider, DisplaySpriteSheetProperties);
        RemoveImageCommand = new AnonymousCommand(ServiceProvider, RemoveSelectedImages, () => SelectedSprites.Count > 0);
        SelectNextSpriteCommand = new AnonymousCommand<int>(ServiceProvider, SelectNextSprite);
        MoveImageCommand = new AnonymousCommand<int>(ServiceProvider, MoveImage, _ => SelectedSprites.Count > 0);
        AddNewImageCommand = new AnonymousCommand(ServiceProvider, AddNewImage);
        UseWholeImageCommand = new AnonymousCommand(ServiceProvider, UseWholeImage, () => SelectedSprites.Count > 0);
        FocusOnRegionCommand = new AnonymousCommand(ServiceProvider, FocusOnRegion);
        DuplicateSelectedImagesCommand = new AnonymousCommand(ServiceProvider, DuplicateSelectedImages, () => SelectedSprites.Count > 0);
        CopyImageCommand = new AnonymousTaskCommand(ServiceProvider, CopyImages, CanCopyImages);
        PasteImageCommand = new AnonymousTaskCommand(ServiceProvider, PasteImages, CanPasteImages);
        RenameCommand = new AnonymousCommand(ServiceProvider, Rename, () => SelectedSprites.Count == 1);
        FindSpriteRegionCommand = new AnonymousCommand<SpriteImagePick>(ServiceProvider, FindSpriteRegion);
        ToggleSelectionHighlightCommand = new AnonymousCommand(ServiceProvider, () => SelectionHighlightEnabled = !SelectionHighlightEnabled);
        ToggleToolModeCommand = new AnonymousCommand<SpriteSheetEditorToolMode>(ServiceProvider, ToggleToolMode);
        ToggleUseMagicWandCommand = new AnonymousCommand(ServiceProvider, () => ToggleToolMode(SpriteSheetEditorToolMode.MagicWand));

        SelectedSprites = [];
        SelectedSprites.CollectionChanged += SelectedImagesCollectionChanged;
        UpdateSelectionCommands();
        ServiceProvider.TryGet<SelectionService>()?.RegisterSelectionScope(id => Sprites.FirstOrDefault(x => x.Id == id), obj => (obj as SpriteInfoViewModel)?.Id, SelectedSprites);
        sprites = new ObservableList<SpriteInfoViewModel>(Asset.Asset.Sprites.Select((x, i) => new SpriteInfoViewModel(this, x) { Index = i }));
        sprites.CollectionChanged += ImageCollectionChanged;
        DependentProperties.Add(nameof(ToolMode), [nameof(UsingPixelTool)]);
        magicWandUseTransparency = !Asset.Asset.ColorKeyEnabled;

        var rootNode = Session.AssetNodeContainer.GetNode(spriteSheet.Asset);
        spritesNode = rootNode[nameof(SpriteSheetAsset.Sprites)].Target!;
        colorKeyNodeBinding = new MemberGraphNodeBinding<Color>(rootNode[nameof(SpriteSheetAsset.ColorKeyColor)], nameof(ColorKey), OnPropertyChanging, OnPropertyChanged, UndoRedoService);
        colorKeyEnabledNodeBinding = new MemberGraphNodeBinding<bool>(rootNode[nameof(SpriteSheetAsset.ColorKeyEnabled)], nameof(ColorKeyEnabled), OnPropertyChanging, OnPropertyChanged, UndoRedoService);
        typeNodeBinding = new MemberGraphNodeBinding<SpriteSheetType>(rootNode[nameof(SpriteSheetAsset.Type)], nameof(Type), OnPropertyChanging, OnPropertyChanged, UndoRedoService);

        spritesNode.ItemChanged += SpritesContentChanged;
    }

    /// <summary>
    /// Raised when <see cref="Initialize"/> created the <see cref="Cache"/> of the images.
    /// </summary>
    internal event EventHandler<EventArgs>? Initialized;

    /// <summary>
    /// Gets the undo/redo service of the session.
    /// </summary>
    public IUndoRedoService UndoRedoService => ServiceProvider.Get<IUndoRedoService>();

    /// <summary>
    /// Gets or sets the type of the sprite sheet: 2D sprites have a center, UI sprites have borders.
    /// </summary>
    public SpriteSheetType Type { get => typeNodeBinding.Value; set => typeNodeBinding.Value = value; }

    /// <summary>
    /// Gets or sets whether the sprite sheet uses <see cref="ColorKey"/> as its transparent color. The magic wand then
    /// delimits the sprites with that color rather than with the transparent pixels.
    /// </summary>
    public bool ColorKeyEnabled
    {
        get => colorKeyEnabledNodeBinding.Value;
        set
        {
            colorKeyEnabledNodeBinding.Value = value;
            MagicWandUseTransparency = !value;
        }
    }

    /// <summary>
    /// Gets or sets whether the image is darkened outside the region of the selected sprite.
    /// </summary>
    public bool SelectionHighlightEnabled { get => selectionHighlightEnabled; set => SetValue(ref selectionHighlightEnabled, value); }

    /// <summary>
    /// Gets the sprites of the sprite sheet, in order.
    /// </summary>
    public IReadOnlyObservableList<SpriteInfoViewModel> Sprites => sprites;

    /// <summary>
    /// Gets the selected sprites, which the property grid shows. The view edits the region of the sprite when a single one is selected.
    /// </summary>
    public ObservableList<object> SelectedSprites { get; }

    /// <summary>
    /// Gets the sprite whose region, borders and center the view edits, when a single sprite is selected.
    /// </summary>
    public SpriteInfoViewModel? CurrentSprite { get => currentSprite; private set => SetValue(ref currentSprite, value); }

    /// <summary>
    /// Gets the zoom and the scroll offsets of the image.
    /// </summary>
    public ViewportViewModel Viewport { get; }

    /// <summary>
    /// Gets or sets the color of the region, borders and center of the selected sprite in the view.
    /// </summary>
    public Color4 BorderColor { get => borderColor; set => SetValue(ref borderColor, value); }

    /// <summary>
    /// Gets or sets the color key of the sprite sheet (see <see cref="ColorKeyEnabled"/>).
    /// </summary>
    public Color ColorKey { get => colorKeyNodeBinding.Value; set => colorKeyNodeBinding.Value = value; }

    /// <summary>
    /// Gets or sets whether the magic wand delimits the sprites with the transparent pixels rather than with the color key.
    /// </summary>
    public bool MagicWandUseTransparency { get => magicWandUseTransparency; set => SetValue(ref magicWandUseTransparency, value); }

    /// <summary>
    /// Gets or sets the tool that a click or a drag on the image uses.
    /// </summary>
    public SpriteSheetEditorToolMode ToolMode { get => toolMode; set => SetValue(ref toolMode, value); }

    /// <summary>
    /// Gets whether the tool picks a pixel of the image (color key picker, magic wand) rather than dragging handles.
    /// </summary>
    public bool UsingPixelTool => ToolMode is SpriteSheetEditorToolMode.ColorPick or SpriteSheetEditorToolMode.MagicWand;

    /// <summary>
    /// Gets the source images of the sprites, once the editor is initialized.
    /// </summary>
    public SpriteEditorImageCache? Cache { get; private set; }

    /// <summary>
    /// Gets the command that shows the properties of the sprite sheet rather than those of the selected sprites.
    /// </summary>
    public ICommandBase DisplaySpriteSheetPropertiesCommand { get; }

    /// <summary>
    /// Gets the command that deletes the selected sprites, as one undoable operation ("Delete sprites").
    /// </summary>
    public ICommandBase RemoveImageCommand { get; }

    /// <summary>
    /// Gets the command that selects the next sprite (parameter 1) or the previous one (-1) when a single sprite is selected.
    /// </summary>
    public ICommandBase SelectNextSpriteCommand { get; }

    /// <summary>
    /// Gets the command that moves the selected sprites down (parameter 1) or up (-1), as one undoable operation.
    /// </summary>
    public ICommandBase MoveImageCommand { get; }

    /// <summary>
    /// Gets the command that adds an empty sprite at the end and selects it, as one undoable operation.
    /// </summary>
    public ICommandBase AddNewImageCommand { get; }

    /// <summary>
    /// Gets the command that sets the region of the selected sprites to their whole image, as one undoable operation.
    /// </summary>
    public ICommandBase UseWholeImageCommand { get; }

    /// <summary>
    /// Gets the command that scrolls the view to the middle of the region of the selected sprite.
    /// </summary>
    public ICommandBase FocusOnRegionCommand { get; }

    /// <summary>
    /// Gets the command that shows or hides the darkening of the image outside the region.
    /// </summary>
    public ICommandBase ToggleSelectionHighlightCommand { get; }

    /// <summary>
    /// Gets the command that selects the tool of its parameter, or no tool when it is already selected.
    /// </summary>
    public ICommandBase ToggleToolModeCommand { get; }

    /// <summary>
    /// Gets the command that selects the magic wand, or no tool when it is already selected.
    /// </summary>
    public ICommandBase ToggleUseMagicWandCommand { get; }

    /// <summary>
    /// Gets the command that adds copies of the selected sprites at the end and selects them, as one undoable operation.
    /// </summary>
    public ICommandBase DuplicateSelectedImagesCommand { get; }

    /// <summary>
    /// Gets the command that copies the selected sprites to the clipboard.
    /// </summary>
    public ICommandBase CopyImageCommand { get; }

    /// <summary>
    /// Gets the command that adds the sprites of the clipboard at the end and selects them, as one undoable operation. The
    /// clipboard can only be read asynchronously: the command checks its content when it runs, and does nothing when it
    /// holds no sprite.
    /// </summary>
    public ICommandBase PasteImageCommand { get; }

    /// <summary>
    /// Gets the command that starts renaming the selected sprite in the list.
    /// </summary>
    public ICommandBase RenameCommand { get; }

    /// <summary>
    /// Gets the command of a click of the pixel tools on the image (<see cref="SpriteImagePick"/>): the magic wand sets the
    /// region of the selected sprite, the picker sets the color key.
    /// </summary>
    public ICommandBase FindSpriteRegionCommand { get; }

    /// <inheritdoc />
    public override Task<bool> Initialize()
    {
        Cache = new SpriteEditorImageCache();
        OnPropertyChanged(nameof(Cache));
        Initialized?.Invoke(this, EventArgs.Empty);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(SpriteSheetEditorViewModel));
        spritesNode.ItemChanged -= SpritesContentChanged;
        colorKeyNodeBinding.Dispose();
        colorKeyEnabledNodeBinding.Dispose();
        typeNodeBinding.Dispose();

        // Clear the property grid if any of our items was selected.
        if (Session.ActiveProperties.Selection?.Any(x => x is SpriteInfoViewModel sprite && Sprites.Contains(sprite)) ?? false)
        {
            Session.ActiveProperties.GenerateSelectionPropertiesAsync([]).Forget();
        }

        // Unregister collection
        ServiceProvider.TryGet<SelectionService>()?.UnregisterSelectionScope(SelectedSprites);
        SelectedSprites.CollectionChanged -= SelectedImagesCollectionChanged;
        // Clear cache
        Cache?.Dispose();
        // Destroy related view models
        Sprites.ForEach(x => x.Destroy());
        Viewport.Destroy();

        base.Destroy();
    }

    internal void AddImage(SpriteInfo spriteInfo)
    {
        spritesNode.Add(spriteInfo);
    }

    internal void ImportFiles(IEnumerable<UFile> children, int index = -1)
    {
        List<SpriteInfo> addedImages;
        using (var transaction = UndoRedoService.CreateTransaction())
        {
            addedImages = [];
            foreach (var filePath in children)
            {
                var image = CreateNewImage();
                image.Source = filePath;
                // The sprite takes the whole image (the WPF editor left the region empty)
                if (Cache?.GetPixelSize(filePath) is { } size)
                    image.TextureRegion = new Rectangle(0, 0, size.Width, size.Height);
                if (index <= 0)
                    AddImage(image);
                else
                    InsertImage(image, index++);
                addedImages.Add(image);
            }
            UndoRedoService.SetName(transaction, "Import images");
        }
        SelectedSprites.Clear();
        SelectedSprites.AddRange(addedImages.Select(FindViewModel).NotNull());
    }

    /// <summary>
    /// Selects the view models of the given sprites, as after a reorder.
    /// </summary>
    internal void SelectSprites(IEnumerable<SpriteInfo> spriteInfos)
    {
        SelectedSprites.Clear();
        SelectedSprites.AddRange(spriteInfos.Select(FindViewModel).NotNull());
    }

    internal void InsertImage(SpriteInfo spriteinfo, int index)
    {
        spritesNode.Add(spriteinfo, new NodeIndex(index));
    }

    internal void RemoveImage(SpriteInfo spriteInfo, int imageIndex)
    {
        spritesNode.Remove(spriteInfo, new NodeIndex(imageIndex));
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(params string[] propertyNames)
    {
        base.OnPropertyChanged(propertyNames);

        if (propertyNames.Any(p => p is nameof(ColorKey) or nameof(ColorKeyEnabled) or nameof(Type)))
        {
            RefreshPropertyGrid();
        }
    }

    private SpriteInfoViewModel? FindViewModel(SpriteInfo spriteInfo)
    {
        return sprites.FirstOrDefault(sivm => ReferenceEquals(spriteInfo, sivm.GetSpriteInfo()));
    }

    private void RemoveSelectedImages()
    {
        var index = int.MaxValue;
        using (var transaction = UndoRedoService.CreateTransaction())
        {
            foreach (var image in SelectedSprites.Cast<SpriteInfoViewModel>().ToList())
            {
                if (index > image.Index)
                    index = image.Index;

                RemoveImage(image.GetSpriteInfo(), image.Index);
            }
            UndoRedoService.SetName(transaction, "Delete sprites");
        }
        if (Sprites.Count > 0)
        {
            SelectedSprites.Add(Sprites[Math.Min(index, Sprites.Count - 1)]);
        }
    }

    private bool CanCopyImages()
    {
        return SelectedSprites.Count > 0 && ServiceProvider.TryGet<ICopyPasteService>() is not null && ServiceProvider.TryGet<IClipboardService>() is not null;
    }

    private async Task CopyImages()
    {
        if (ServiceProvider.TryGet<ICopyPasteService>() is not { } copyPasteService || ServiceProvider.TryGet<IClipboardService>() is not { } clipboard)
            return;

        try
        {
            var spriteInfos = SelectedSprites.Cast<SpriteInfoViewModel>().Select(s => s.GetSpriteInfo()).ToList();
            var text = copyPasteService.CopyFromAsset(Asset.PropertyGraph, Asset.Id, spriteInfos, false);
            if (!string.IsNullOrEmpty(text))
                await clipboard.SetTextAsync(text);
        }
        catch (SystemException e)
        {
            // We don't provide feedback when copying fails.
            e.Ignore();
        }
    }

    private bool CanPasteImages()
    {
        return ServiceProvider.TryGet<ICopyPasteService>() is not null && ServiceProvider.TryGet<IClipboardService>() is not null;
    }

    private async Task PasteImages()
    {
        if (ServiceProvider.TryGet<ICopyPasteService>() is not { } copyPasteService || ServiceProvider.TryGet<IClipboardService>() is not { } clipboard)
            return;

        var text = await clipboard.GetTextAsync();
        if (string.IsNullOrEmpty(text) || !copyPasteService.CanPaste(text, typeof(List<SpriteInfo>), typeof(List<SpriteInfo>), typeof(List<SpriteInfo>)))
            return;

        if (copyPasteService.DeserializeCopiedData(text, Asset.Asset, typeof(List<SpriteInfo>)).Items.FirstOrDefault()?.Data is not List<SpriteInfo> { Count: > 0 } spriteInfos)
            return;

        var imagesToSelect = new List<SpriteInfo>();
        using (var transaction = UndoRedoService.CreateTransaction())
        {
            foreach (var spriteInfo in spriteInfos)
            {
                AddImage(spriteInfo);
                imagesToSelect.Add(spriteInfo);
            }

            UndoRedoService.SetName(transaction, $"Paste {spriteInfos.Count} images");
        }
        SelectedSprites.Clear();
        SelectedSprites.AddRange(imagesToSelect.Select(FindViewModel).NotNull());
    }

    private void Rename()
    {
        if (SelectedSprites.Count == 1 && SelectedSprites[0] is SpriteInfoViewModel { IsEditable: true } sprite)
            sprite.IsEditing = true;
    }

    private void SelectNextSprite(int offset)
    {
        if (SelectedSprites.Count == 1)
        {
            var index = Sprites.IndexOf(SelectedSprites.Cast<SpriteInfoViewModel>().First());
            index = (Sprites.Count + index + offset) % Sprites.Count;
            SelectedSprites.Clear();
            SelectedSprites.Add(Sprites[index]);
        }
    }

    private void MoveImage(int offset)
    {
        if (offset < -1 || offset > 1)
            throw new ArgumentException("The offset must be either -1, 0 or 1");

        var imagesToMove = new List<SpriteInfoViewModel>(SelectedSprites.Cast<SpriteInfoViewModel>());
        var toReselect = imagesToMove.Select(sivm => sivm.GetSpriteInfo()).ToList();
        foreach (var selectedImage in SelectedSprites.Cast<SpriteInfoViewModel>().OrderBy(x => x.Index * -offset))
        {
            if (selectedImage.Index + offset < 0 || selectedImage.Index + offset >= Sprites.Count)
            {
                imagesToMove.Remove(selectedImage);
                continue;
            }
            var targetImage = Sprites[selectedImage.Index + offset];
            if (SelectedSprites.Contains(targetImage) && !imagesToMove.Contains(targetImage))
            {
                imagesToMove.Remove(selectedImage);
            }
        }
        if (imagesToMove.Count > 0)
        {
            imagesToMove.Sort((x, y) => x.Index.CompareTo(y.Index) * -offset);

            using var transaction = UndoRedoService.CreateTransaction();
            foreach (var selectedImage in imagesToMove)
            {
                var spriteInfo = selectedImage.GetSpriteInfo();
                RemoveImage(spriteInfo, selectedImage.Index);
                InsertImage(spriteInfo, selectedImage.Index + offset);
            }
            UndoRedoService.SetName(transaction, "Reorder images");
        }
        SelectSprites(toReselect);
    }

    private void FocusOnRegion()
    {
        var region = CurrentSprite?.TextureRegion;
        if (region == null)
            return;

        Viewport.HorizontalOffset = region.ActualLeft + (region.ActualWidth * 0.5) - (Viewport.ViewportWidth * 0.5);
        Viewport.VerticalOffset = region.ActualTop + (region.ActualHeight * 0.5) - (Viewport.ViewportHeight * 0.5);
    }

    private async void RefreshPropertyGrid()
    {
        if (SelectedSprites.Count == 0)
        {
            DisplaySpriteSheetProperties();
        }
        else
        {
            await GenerateProperties();
        }
    }

    private async Task GenerateProperties()
    {
        var selectedSprites = SelectedSprites.Cast<SpriteInfoViewModel>().ToList();
        EditorProperties.UpdateTypeAndName(selectedSprites, x => "Sprite", x => x.Name ?? string.Empty, "sprites");
        var generation = Interlocked.Increment(ref propertiesGeneration);
        var viewModel = await EditorProperties.GenerateSelectionPropertiesAsync(selectedSprites);
        // A later selection or refresh of the grid (a change of the color key, for instance) supersedes this one, and its
        // grid comes back null: only the latest one sets the current sprite. Otherwise the current sprite went null for a
        // while, or for good when the superseded grid finished last, and the tools of the image did nothing.
        if (generation != Volatile.Read(ref propertiesGeneration))
            return;

        CurrentSprite = viewModel != null && SelectedSprites.Count == 1 ? selectedSprites.First() : null;
    }

    private async void SelectedImagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateSelectionCommands();

        var horizontalOffset = Viewport.HorizontalOffset;
        var verticalOffset = Viewport.VerticalOffset;

        await GenerateProperties();

        if (SelectedSprites.Count == 1)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                Viewport.HorizontalOffset = horizontalOffset;
                Viewport.VerticalOffset = verticalOffset;
            });
        }
    }

    private void UpdateSelectionCommands()
    {
        var hasSelection = SelectedSprites.Count > 0;
        RemoveImageCommand.IsEnabled = hasSelection;
        MoveImageCommand.IsEnabled = hasSelection;
        UseWholeImageCommand.IsEnabled = hasSelection;
        DuplicateSelectedImagesCommand.IsEnabled = hasSelection;
        CopyImageCommand.IsEnabled = CanCopyImages();
        RenameCommand.IsEnabled = SelectedSprites.Count == 1;
    }

    private void DisplaySpriteSheetProperties()
    {
        SelectedSprites.Clear();
        EditorProperties.TypeDescription = Asset.TypeDisplayName;
        EditorProperties.Name = Asset.Name;
        EditorProperties.GenerateSelectionPropertiesAsync(Asset.Yield()).Forget();
    }

    private void AddNewImage()
    {
        SpriteInfo spriteInfo;
        using (var transaction = UndoRedoService.CreateTransaction())
        {
            spriteInfo = new SpriteInfo { Name = NamingHelper.ComputeNewName("New sprite", Sprites, x => x.Name) };
            AddImage(spriteInfo);
            UndoRedoService.SetName(transaction, "Add new sprite");
        }
        SelectedSprites.Clear();
        SelectedSprites.Add(FindViewModel(spriteInfo)!);
    }

    private void UseWholeImage()
    {
        using var transaction = UndoRedoService.CreateTransaction();
        foreach (var sprite in SelectedSprites.Cast<SpriteInfoViewModel>())
        {
            sprite.TextureRegion.UseWholeImage();
        }
        UndoRedoService.SetName(transaction, "Use whole sprite");
    }

    private void DuplicateSelectedImages()
    {
        var imageCount = SelectedSprites.Count;
        var imagesToSelect = new List<SpriteInfo>(imageCount);
        using (var transaction = UndoRedoService.CreateTransaction())
        {
            foreach (var imageVm in SelectedSprites.Cast<SpriteInfoViewModel>().ToList())
            {
                var spriteInfo = AssetCloner.Clone(imageVm.GetSpriteInfo());
                AddImage(spriteInfo);
                imagesToSelect.Add(spriteInfo);
            }
            UndoRedoService.SetName(transaction, $"Duplicate {imageCount} images");
        }
        SelectedSprites.Clear();
        SelectedSprites.AddRange(imagesToSelect.Select(FindViewModel).NotNull());
    }

    private void FindSpriteRegion(SpriteImagePick pick)
    {
        if (CurrentSprite is not { } sprite || Cache == null)
            return;

        switch (ToolMode)
        {
            case SpriteSheetEditorToolMode.None:
                // nothing to do
                break;

            case SpriteSheetEditorToolMode.ColorPick:
                var color = Cache.PickPixelColor(sprite.GetSpriteInfo().Source, pick.Position);
                if (color.HasValue)
                {
                    ColorKey = color.Value;
                }
                ToolMode = SpriteSheetEditorToolMode.None;
                break;

            case SpriteSheetEditorToolMode.MagicWand:
                var spriteRegion = Cache.FindSpriteRegion(sprite.GetSpriteInfo().Source, pick.Position, MagicWandUseTransparency, Asset.Asset.ColorKeyColor);
                if (spriteRegion.HasValue && spriteRegion.Value.Width > 0 && spriteRegion.Value.Height > 0)
                {
                    var isUnion = pick.AddToRegion;
                    if (isUnion)
                    {
                        // Merge the two regions
                        var initialRect = (Rectangle)sprite.TextureRegion.Region;
                        spriteRegion = Rectangle.Union(initialRect, spriteRegion.Value);
                    }
                    using var transaction = UndoRedoService.CreateTransaction();
                    sprite.TextureRegion.Region = new RectangleF
                    {
                        Left = spriteRegion.Value.Left,
                        Top = spriteRegion.Value.Top,
                        Width = spriteRegion.Value.Width,
                        Height = spriteRegion.Value.Height
                    };
                    if (!isUnion)
                    {
                        // It's a new region, reset the sprite borders
                        sprite.SpriteBorders.Borders = Vector4.Zero;
                    }
                    UndoRedoService.SetName(transaction, "Update selected region");
                }
                break;

            case SpriteSheetEditorToolMode.SpriteCenter:
            case SpriteSheetEditorToolMode.Borders:
                // Nothing to do
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ToggleToolMode(SpriteSheetEditorToolMode newMode)
    {
        ToolMode = ToolMode != newMode ? newMode : SpriteSheetEditorToolMode.None;
    }

    private void ImageCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace)
        {
            // Properly destroy the view model
            e.OldItems?.Cast<SpriteInfoViewModel>().ForEach(x =>
            {
                SelectedSprites.Remove(x);
                x.Destroy();
            });
        }

        // Consolidate the indices
        var index = 0;
        Sprites.ForEach(x => x.Index = index++);
    }

    private void SpritesContentChanged(object? sender, ItemChangeEventArgs e)
    {
        var index = e.Index.Int;
        switch (e.ChangeType)
        {
            case ContentChangeType.CollectionAdd:
                sprites.Insert(index, new SpriteInfoViewModel(this, (SpriteInfo)e.NewValue));
                break;

            case ContentChangeType.CollectionRemove:
                sprites.RemoveAt(index);
                break;

            case ContentChangeType.CollectionUpdate:
                sprites[index] = new SpriteInfoViewModel(this, (SpriteInfo)e.NewValue);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private SpriteInfo CreateNewImage()
    {
        return new SpriteInfo { Name = $"Frame {Asset.Asset.Sprites.Count}" };
    }

    /// <inheritdoc />
    bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
    {
        if (children.Any(x => x is not UFile))
        {
            message = DragDropBehavior.InvalidDropAreaMessage;
            return false;
        }
        message = "Add images using the selected files";
        return true;
    }

    /// <inheritdoc />
    void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
    {
        if (children.Any(x => x is not UFile))
            return;

        ImportFiles(children.OfType<UFile>());
    }
}
