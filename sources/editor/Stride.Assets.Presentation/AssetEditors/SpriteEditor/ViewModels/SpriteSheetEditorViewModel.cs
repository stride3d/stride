// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Extensions;
using Stride.Core.Presentation.Interop;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;
using Stride.Assets.Presentation.AssetEditors.SpriteEditor.Services;
using Stride.Assets.Presentation.AssetEditors.SpriteEditor.Views;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Sprite;

namespace Stride.Assets.Presentation.AssetEditors.SpriteEditor.ViewModels
{
    using WindowsPoint = System.Windows.Point;

    public enum SpriteSheetEditorToolMode
    {
        None,
        ColorPick,
        MagicWand,
        SpriteCenter,
        Borders,
    };

    [AssetEditorViewModel(typeof(SpriteSheetAsset), typeof(SpriteEditorView))]
    public sealed class SpriteSheetEditorViewModel : AssetEditorViewModel, IAddChildViewModel
    {
        private readonly ObservableList<SpriteInfoViewModel> sprites;
        private readonly IObjectNode spritesNode;
        private readonly MemberGraphNodeBinding<Color> colorKeyNodeBinding;
        private readonly MemberGraphNodeBinding<bool> colorKeyEnabledNodeBinding;
        private readonly MemberGraphNodeBinding<SpriteSheetType> typeNodeBinding;

        private Color4 borderColor = new Color4(1.0f, 0.0f, 0.0f, 1.0f);
        private SpriteInfoViewModel currentSprite;
        private bool magicWandUseTransparency;
        private bool selectionHighlightEnabled;
        private SpriteSheetEditorToolMode toolMode;

        private readonly FuncClipboardMonitor<bool> pasteMonitor = new FuncClipboardMonitor<bool>();

        public SpriteSheetEditorViewModel([NotNull] SpriteSheetViewModel spriteSheet)
            : base(spriteSheet)
        {
            Viewport = new ViewportViewModel(ServiceProvider);

            // Commands
            DisplaySpriteSheetPropertiesCommand = new AnonymousCommand(ServiceProvider, DisplaySpriteSheetProperties);
            RemoveImageCommand = new AnonymousCommand(ServiceProvider, RemoveSelectedImages);
            SelectNextSpriteCommand = new AnonymousCommand<int>(ServiceProvider, SelectNextSprite);
            MoveImageCommand = new AnonymousCommand<int>(ServiceProvider, MoveImage);
            AddNewImageCommand = new AnonymousCommand(ServiceProvider, AddNewImage);
            UseWholeImageCommand = new AnonymousCommand(ServiceProvider, UseWholeImage);
            FocusOnRegionCommand = new AnonymousCommand(ServiceProvider, FocusOnRegion);
            DuplicateSelectedImagesCommand = new AnonymousCommand(ServiceProvider, DuplicateSelectedImages);
            CopyImageCommand = new AnonymousCommand(ServiceProvider, CopyImages, CanCopyImages);
            PasteImageCommand = new AnonymousCommand(ServiceProvider, PasteImages, () => pasteMonitor.Get(CanPasteImages));
            FindSpriteRegionCommand = new AnonymousCommand<WindowsPoint>(ServiceProvider, FindSpriteRegion);
            ToggleSelectionHighlightCommand = new AnonymousCommand(ServiceProvider, () => SelectionHighlightEnabled = !SelectionHighlightEnabled);
            ToggleToolModeCommand = new AnonymousCommand<SpriteSheetEditorToolMode>(ServiceProvider, ToggleToolMode);
            ToggleUseMagicWandCommand = new AnonymousCommand(ServiceProvider, () => ToggleToolMode(SpriteSheetEditorToolMode.MagicWand));

            SelectedSprites = new ObservableList<object>();
            SelectedSprites.CollectionChanged += SelectedImagesCollectionChanged;
            ServiceProvider.Get<SelectionService>().RegisterSelectionScope(id => Sprites.FirstOrDefault(x => x.Id == id), obj => (obj as SpriteInfoViewModel)?.Id, SelectedSprites);
            sprites = new ObservableList<SpriteInfoViewModel>(Asset.Asset.Sprites.Select((x, i) => new SpriteInfoViewModel(this, x) { Index = i }));
            Sprites.CollectionChanged += ImageCollectionChanged;
            DependentProperties.Add(nameof(ToolMode), new[] { nameof(UsingPixelTool) });
            magicWandUseTransparency = !Asset.Asset.ColorKeyEnabled;

            var rootNode = Session.AssetNodeContainer.GetNode(spriteSheet.Asset);
            spritesNode = rootNode[nameof(SpriteSheetAsset.Sprites)].Target;
            colorKeyNodeBinding = new MemberGraphNodeBinding<Color>(rootNode[nameof(SpriteSheetAsset.ColorKeyColor)], nameof(ColorKey), OnPropertyChanging, OnPropertyChanged, UndoRedoService);
            colorKeyEnabledNodeBinding = new MemberGraphNodeBinding<bool>(rootNode[nameof(SpriteSheetAsset.ColorKeyEnabled)], nameof(ColorKeyEnabled), OnPropertyChanging, OnPropertyChanged, UndoRedoService);
            typeNodeBinding = new MemberGraphNodeBinding<SpriteSheetType>(rootNode[nameof(SpriteSheetAsset.Type)], nameof(Type), OnPropertyChanging, OnPropertyChanged, UndoRedoService);

            // TODO: dispose this
            spritesNode.ItemChanged += SpritesContentChanged;
        }

        public new SpriteSheetViewModel Asset => (SpriteSheetViewModel)base.Asset;

        public SpriteSheetType Type { get => typeNodeBinding.Value; set => typeNodeBinding.Value = value; }

        public bool ColorKeyEnabled
        {
            get => colorKeyEnabledNodeBinding.Value;
            set
            {
                colorKeyEnabledNodeBinding.Value = value;
                MagicWandUseTransparency = !value;
            }
        }

        public bool SelectionHighlightEnabled { get => selectionHighlightEnabled; set => SetValue(ref selectionHighlightEnabled, value); }

        public IReadOnlyObservableList<SpriteInfoViewModel> Sprites => sprites;

        public ObservableList<object> SelectedSprites { get; }

        public SpriteInfoViewModel CurrentSprite { get => currentSprite; set => SetValue(ref currentSprite, value); }

        public ViewportViewModel Viewport { get; }

        public Color4 BorderColor { get => borderColor; set => SetValue(ref borderColor, value); }

        public Color ColorKey { get => colorKeyNodeBinding.Value; set => colorKeyNodeBinding.Value = value; }

        public bool MagicWandUseTransparency { get => magicWandUseTransparency; set => SetValue(ref magicWandUseTransparency, value); }

        public SpriteSheetEditorToolMode ToolMode { get => toolMode; set => SetValue(ref toolMode, value); }

        public bool UsingPixelTool => ToolMode == SpriteSheetEditorToolMode.ColorPick || ToolMode == SpriteSheetEditorToolMode.MagicWand;

        public SpriteEditorImageCache Cache { get; private set; }

        [NotNull]
        public ICommandBase DisplaySpriteSheetPropertiesCommand { get; }

        [NotNull]
        public ICommandBase RemoveImageCommand { get; }

        [NotNull]
        public ICommandBase SelectNextSpriteCommand { get; }

        [NotNull]
        public ICommandBase MoveImageCommand { get; }

        [NotNull]
        public ICommandBase AddNewImageCommand { get; }

        [NotNull]
        public ICommandBase UseWholeImageCommand { get; }

        [NotNull]
        public ICommandBase FocusOnRegionCommand { get; }

        [NotNull]
        public ICommandBase ToggleSelectionHighlightCommand { get; }

        [NotNull]
        public ICommandBase ToggleToolModeCommand { get; }

        [NotNull]
        public ICommandBase ToggleUseMagicWandCommand { get; }

        [NotNull]
        public ICommandBase DuplicateSelectedImagesCommand { get; }

        [NotNull]
        public ICommandBase CopyImageCommand { get; }

        [NotNull]
        public ICommandBase PasteImageCommand { get; }

        [NotNull]
        public ICommandBase FindSpriteRegionCommand { get; }

        internal event EventHandler<EventArgs> Initialized;

        /// <inheritdoc />
        public override Task<bool> Initialize()
        {
            Cache = new SpriteEditorImageCache();
            Initialized?.Invoke(this, EventArgs.Empty);
            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(SpriteSheetEditorViewModel));
            spritesNode.ItemChanged -= SpritesContentChanged;

            pasteMonitor.Destroy();
            // Clear the property grid in any of our items was selected.
            if (Session.ActiveProperties.Selection.Any(x => Sprites.Contains(x)))
            {
                Session.ActiveProperties.GenerateSelectionPropertiesAsync(Enumerable.Empty<IPropertyProviderViewModel>()).Forget();
            }

            // Unregister collection
            ServiceProvider.Get<SelectionService>().UnregisterSelectionScope(SelectedSprites);
            SelectedSprites.CollectionChanged -= SelectedImagesCollectionChanged;
            // Clear cache
            Cache.Dispose();
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
                addedImages = new List<SpriteInfo>();
                foreach (var filePath in children)
                {
                    var image = CreateNewImage();
                    image.Source = filePath;
                    if (index <= 0)
                        AddImage(image);
                    else
                        InsertImage(image, index++);
                    addedImages.Add(image);
                }
                UndoRedoService.SetName(transaction, "Import images");
            }
            SelectedSprites.Clear();
            SelectedSprites.AddRange(addedImages.Select(FindViewModel));
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

            if (propertyNames.Any(p => string.Equals(p, nameof(ColorKey)) || string.Equals(p, nameof(ColorKeyEnabled)) || string.Equals(p, nameof(Type))))
            {
                RefreshPropertyGrid();
            }
        }

        [CanBeNull]
        private SpriteInfoViewModel FindViewModel(SpriteInfo spriteInfo)
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
            return SelectedSprites.Count > 0 && ServiceProvider.TryGet<ICopyPasteService>() != null;
        }

        private void CopyImages()
        {
            var copyPasteService = ServiceProvider.TryGet<ICopyPasteService>();
            if (copyPasteService == null)
                return;

            AssetPropertyGraph propertyGraph = null;
            try
            {
                var spriteInfos = SelectedSprites.Cast<SpriteInfoViewModel>().Select(s => s.GetSpriteInfo()).ToList();
                // create a temporary asset containing only this hierarchy
                var tmpAsset = new SpriteSheetAsset();
                tmpAsset.Id = Asset.Id; // necessary to identify the proper source id
                tmpAsset.Sprites.AddRange(spriteInfos);
                // clone the asset so we have new objects that we can modify before serializing
                var overrides = AssetPropertyGraph.GenerateOverridesForSerialization(Session.GraphContainer.NodeContainer.GetOrCreateNode(tmpAsset));
                tmpAsset = AssetCloner.Clone(tmpAsset);
                propertyGraph = AssetQuantumRegistry.ConstructPropertyGraph(Session.GraphContainer, new AssetItem("", tmpAsset), null);
                AssetPropertyGraph.ApplyOverrides(propertyGraph.RootNode, overrides);
                var memberPath = new MemberPath();
                memberPath.Push(TypeDescriptorFactory.Default.Find(typeof(SpriteSheetAsset)).Members.First(m => m.Name == nameof(SpriteSheetAsset.Sprites)));
                var text = copyPasteService.CopyFromAsset(Asset.PropertyGraph, Asset.Id, spriteInfos, false);
                if (!string.IsNullOrEmpty(text))
                    SafeClipboard.SetText(text);
            }
            catch (SystemException e)
            {
                // We don't provide feedback when copying fails.
                e.Ignore();
            }
            finally
            {
                propertyGraph?.Dispose();
            }
        }

        private bool CanPasteImages()
        {
            return ServiceProvider.TryGet<ICopyPasteService>()?.CanPaste(SafeClipboard.GetText(), typeof(List<SpriteInfo>), typeof(List<SpriteInfo>), typeof(List<SpriteInfo>)) ?? false;
        }

        private void PasteImages()
        {
            var text = SafeClipboard.GetText();
            if (string.IsNullOrEmpty(text))
                return;

            var spriteInfos = ServiceProvider.TryGet<ICopyPasteService>()?.DeserializeCopiedData(text, Asset.Asset, typeof(List<SpriteInfo>)).Items.FirstOrDefault()?.Data as List<SpriteInfo>;
            if (spriteInfos == null || spriteInfos.Count == 0)
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
            SelectedSprites.AddRange(imagesToSelect.Select(FindViewModel));
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

                using (var transaction = UndoRedoService.CreateTransaction())
                {
                    foreach (var selectedImage in imagesToMove)
                    {
                        var spriteInfo = selectedImage.GetSpriteInfo();
                        RemoveImage(spriteInfo, selectedImage.Index);
                        InsertImage(spriteInfo, selectedImage.Index + offset);
                    }
                    UndoRedoService.SetName(transaction, "Reorder images");
                }
            }
            SelectedSprites.Clear();
            SelectedSprites.AddRange(toReselect.Select(FindViewModel));
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
            EditorProperties.UpdateTypeAndName(selectedSprites, x => "Sprite", x => x.Name, "sprites");
            var viewModel = await EditorProperties.GenerateSelectionPropertiesAsync(selectedSprites);
            CurrentSprite = viewModel != null && SelectedSprites.Count == 1 ? selectedSprites.First() : null;
        }

        private async void SelectedImagesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
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

        private void DisplaySpriteSheetProperties()
        {
            SelectedSprites.Clear();
            ShowAssetProperties();
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
            SelectedSprites.Add(FindViewModel(spriteInfo));
        }

        private void UseWholeImage()
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                foreach (var sprite in SelectedSprites.Cast<SpriteInfoViewModel>())
                {
                    sprite.TextureRegion.UseWholeImage();
                }
                UndoRedoService.SetName(transaction, "Use whole sprite");
            }
        }

        private void DuplicateSelectedImages()
        {
            var imageCount = SelectedSprites.Count;
            var imagesToSelect = new List<SpriteInfo>(imageCount);
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                foreach (SpriteInfoViewModel imageVm in SelectedSprites)
                {
                    var spriteInfo = AssetCloner.Clone(imageVm.GetSpriteInfo());
                    AddImage(spriteInfo);
                    imagesToSelect.Add(spriteInfo);
                }
                UndoRedoService.SetName(transaction, $"Duplicate {imageCount} images");
            }
            SelectedSprites.Clear();
            SelectedSprites.AddRange(imagesToSelect.Select(FindViewModel));
        }

        private void FindSpriteRegion(WindowsPoint initialPoint)
        {
            if (CurrentSprite == null)
                return;

            switch (ToolMode)
            {
                case SpriteSheetEditorToolMode.None:
                    // nothing to do
                    break;

                case SpriteSheetEditorToolMode.ColorPick:
                    var color = Cache.PickPixelColor(CurrentSprite.GetSpriteInfo().Source, initialPoint.ToVector2());
                    if (color.HasValue)
                    {
                        ColorKey = color.Value;
                    }
                    ToolMode = SpriteSheetEditorToolMode.None;
                    break;

                case SpriteSheetEditorToolMode.MagicWand:
                    var spriteRegion = Cache.FindSpriteRegion(CurrentSprite.GetSpriteInfo().Source, initialPoint.ToVector2(), MagicWandUseTransparency, Asset.Asset.ColorKeyColor);
                    if (spriteRegion.HasValue && spriteRegion.Value.Width > 0 && spriteRegion.Value.Height > 0)
                    {
                        // TODO: pass the ctrl state to the command somehow, we shouldn't access to Keyboard from the VM
                        var isUnion = System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control);
                        if (isUnion)
                        {
                            // Merge the two regions
                            var initialRect = (Rectangle)CurrentSprite.TextureRegion.Region;
                            spriteRegion = Rectangle.Union(initialRect, spriteRegion.Value);
                        }
                        using (var transaction = UndoRedoService.CreateTransaction())
                        {
                            CurrentSprite.TextureRegion.Region = new RectangleF
                            {
                                Left = spriteRegion.Value.Left,
                                Top = spriteRegion.Value.Top,
                                Width = spriteRegion.Value.Width,
                                Height = spriteRegion.Value.Height
                            };
                            if (!isUnion)
                            {
                                // It's a new region, reset the sprite borders
                                CurrentSprite.SpriteBorders.Borders = Vector4.Zero;
                            }
                            UndoRedoService.SetName(transaction, "Update selected region");
                        }
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

        private void ImageCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace)
            {
                // Properly destroy the view model
                e.OldItems?.Cast<SpriteInfoViewModel>().ForEach(x => x.Destroy());
            }

            // Consolidate the indices
            var index = 0;
            Sprites.ForEach(x => x.Index = index++);
        }

        private void SpritesContentChanged(object sender, ItemChangeEventArgs e)
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

        [NotNull]
        private SpriteInfo CreateNewImage()
        {
            return new SpriteInfo { Name = $"Frame {Asset.Asset.Sprites.Count}" };
        }

        /// <inheritdoc />
        bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            if (children.Any(x => !(x is UFile)))
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
            if (children.Any(x => !(x is UFile)))
                return;

            ImportFiles(children.OfType<UFile>());
        }
    }
}
