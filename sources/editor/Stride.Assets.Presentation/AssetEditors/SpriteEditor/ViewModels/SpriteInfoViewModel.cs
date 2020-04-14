// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Quantum;
using Stride.Core.Translation;
using Stride.Assets.Sprite;

namespace Stride.Assets.Presentation.AssetEditors.SpriteEditor.ViewModels
{
    /// <summary>
    /// The class represents a single <see cref="SpriteInfo"/> into an <see cref="SpriteSheetEditorViewModel"/>.
    /// </summary>
    [DebuggerDisplay("SpriteInfo - Name={Name}")]
    public sealed class SpriteInfoViewModel : DispatcherViewModel, IIsEditableViewModel, IInsertChildViewModel, IAddChildViewModel, IAssetPropertyProviderViewModel
    {
        private readonly SpriteInfo sprite;
        private readonly MemberGraphNodeBinding<UFile> sourceNodeBinding;
        private readonly MemberGraphNodeBinding<string> nameNodeBinding;
        private FileSystemWatcher sourceFileWatcher;
        private UFile sourceFileWatcherPath;
        private bool isEditable = true;
        private bool isEditing;
        private int index;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteInfoViewModel"/> class.
        /// </summary>
        /// <param name="editor">The <see cref="SpriteSheetEditorViewModel"/> containing this image.</param>
        /// <param name="sprite">The <see cref="SpriteInfo"/> represented by this view model.</param>
        public SpriteInfoViewModel(SpriteSheetEditorViewModel editor, SpriteInfo sprite) : base(editor.SafeArgument(nameof(editor)).ServiceProvider)
        {
            Editor = editor;
            Id = new AbsoluteId(editor.Asset.Id, Guid.NewGuid());
            this.sprite = sprite;
            Index = -1;

            // Retrieve nodes
            var spriteNode = editor.Asset.Session.AssetNodeContainer.GetOrCreateNode(sprite);
            var nameNode = spriteNode[nameof(SpriteInfo.Name)];
            // TODO: dispose this!
            var sourceNode = spriteNode[nameof(SpriteInfo.Source)];
            sourceNode.ValueChanged += SourceValueChanged;
            var textureRegionNode = spriteNode[nameof(SpriteInfo.TextureRegion)];

            // Create bindings
            nameNodeBinding = new MemberGraphNodeBinding<string>(nameNode, nameof(Name), OnPropertyChanging, OnPropertyChanged, Editor.UndoRedoService);
            sourceNodeBinding = new MemberGraphNodeBinding<UFile>(sourceNode, nameof(Source), OnPropertyChanging, OnPropertyChanged, Editor.UndoRedoService);

            UpdateSourceFileWatcher();

            // Initialize view models and commands
            SpriteBorders = new SpriteBordersViewModel(this, spriteNode);
            SpriteCenter = new SpriteCenterViewModel(this, spriteNode);
            TextureRegion = new TextureRegionViewModel(this, textureRegionNode);
            EditImageCommand = new AnonymousTaskCommand(ServiceProvider, EditImage);
            ExploreCommand = new AnonymousTaskCommand(ServiceProvider, Explore);
        }

        public AbsoluteId Id { get; }

        /// <summary>
        /// Gets or sets the name of the sprite.
        /// </summary>
        public string Name { get { return nameNodeBinding.Value; } set { nameNodeBinding.Value = value; } }

        /// <summary>
        /// Gets the index of the sprite in the sprite sheet.
        /// </summary>
        /// <remarks>This property is updated by the <see cref="SpriteSheetEditorViewModel"/> when it alters the order.</remarks>
        public int Index { get { return index; } internal set { SetValue(ref index, value); } }

        /// <summary>
        /// Gets or sets the source file of this sprite.
        /// </summary>
        public UFile Source { get { return sourceNodeBinding.Value; } set { sourceNodeBinding.Value = value; } }

        /// <inheritdoc/>
        public bool IsEditable { get { return isEditable; } set { SetValue(ref isEditable, value); } }

        /// <inheritdoc/>
        public bool IsEditing
        {
            get { return isEditing; }
            set
            {
                SetValue(ref isEditing, value);
                Editor.FocusOnRegionCommand.IsEnabled = !value;
                Editor.ToggleUseMagicWandCommand.IsEnabled = !value;
            }
        }

        public SpriteBordersViewModel SpriteBorders { get; }

        public SpriteCenterViewModel SpriteCenter { get; }

        public TextureRegionViewModel TextureRegion { get; }

        /// <summary>
        /// Gets the instance of <see cref="SpriteSheetEditorViewModel"/> related to this object.
        /// </summary>
        public SpriteSheetEditorViewModel Editor { get; }

        /// <summary>
        /// Gets a command that will open the source file for edition.
        /// </summary>
        public CommandBase EditImageCommand { get; private set; }

        /// <summary>
        /// Gets a command that will open the source file in the Explorer.
        /// </summary>
        public CommandBase ExploreCommand { get; private set; }

        /// <inheritdoc/>
        bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

        /// <inheritdoc/>
        AssetViewModel IAssetPropertyProviderViewModel.RelatedAsset => Editor.Asset;

        /// <inheritdoc />
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(SpriteInfoViewModel));
            TextureRegion.Destroy();
            SpriteCenter.Destroy();
            SpriteBorders.Destroy();
            base.Destroy();
        }

        /// <summary>
        /// Retrieves the <see cref="SpriteInfo"/> represented by this view model.
        /// </summary>
        /// <returns>The <see cref="SpriteInfo"/> represented by this view model.</returns>
        internal SpriteInfo GetSpriteInfo()
        {
            return sprite;
        }

        private async Task EditImage()
        {
            try
            {
                var path = GetSpriteInfo().Source;
                var stringPath = path?.ToString().Replace('/', '\\') ?? "";
                if (!File.Exists(stringPath))
                {
                    await ServiceProvider.Get<IDialogService>().MessageBox(Tr._p("Message", "Couldn't find the file"), MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var process = new Process { StartInfo = new ProcessStartInfo(stringPath) { UseShellExecute = true } };
                process.StartInfo.Verb = process.StartInfo.Verbs.FirstOrDefault(x => x.ToLowerInvariant() == "edit");
                process.Start();
            }
            catch (Exception ex)
            {
                var message = string.Format(Tr._p("Message", "There was a problem while editing the image.{0}"), ex.FormatSummary(true));
                await ServiceProvider.Get<IDialogService>().MessageBox(message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task Explore()
        {
            try
            {
                var path = GetSpriteInfo().Source;
                var stringPath = path?.ToString().Replace('/', '\\') ?? "";
                if (!File.Exists(stringPath))
                {
                    await ServiceProvider.Get<IDialogService>().MessageBox(Tr._p("Message", "Couldn't find the file"), MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var startInfo = new ProcessStartInfo("explorer.exe", "/select," + stringPath) { UseShellExecute = true };
                var explorer = new Process { StartInfo = startInfo };
                explorer.Start();
            }
            catch (Exception ex)
            {
                var message = string.Format(Tr._p("Message", "There was a problem opening Explorer.{0}"), ex.FormatSummary(true));
                await ServiceProvider.Get<IDialogService>().MessageBox(message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSourceFileWatcher()
        {
            // No change, do nothing.
            if (Source == sourceFileWatcherPath)
                return;

            // Dispose previous watcher if it exists.
            sourceFileWatcher?.Dispose();

            sourceFileWatcher = null;

            // Create a new watcher if the path is valid.
            if (File.Exists(Source))
            {
                sourceFileWatcher = new FileSystemWatcher(Source.GetFullDirectory(), Source.GetFileName());
                sourceFileWatcher.Changed += SourceFileChanged;
                sourceFileWatcher.EnableRaisingEvents = true;
                sourceFileWatcherPath = Source;
            }
        }

        private async void SourceFileChanged(object sender, FileSystemEventArgs e)
        {
            // Loading sometimes fail if we try too soon.
            // TODO: this could be avoided if the cache was managed view-model side. Then we could try immediately to load, but add a retry count.
            await Task.Delay(100);
            Dispatcher.InvokeAsync(() =>
            {
                OnPropertyChanging(nameof(Source));
                OnPropertyChanged(nameof(Source));

                TextureRegion.RefreshImageSize();
            }).Forget();
        }

        private void SourceValueChanged(object sender, MemberNodeChangeEventArgs e)
        {
            UpdateSourceFileWatcher();
            TextureRegion.RefreshImageSize();
            if (string.IsNullOrEmpty(e.OldValue?.ToString()) && e.NewValue != null && !ServiceProvider.Get<IUndoRedoService>().UndoRedoInProgress)
            {
                // If the source path was null or empty before, we set the texture to the whole image by default
                TextureRegion.UseWholeImage();
            }
        }

        bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            return ((IAddChildViewModel)Editor).CanAddChildren(children, modifiers, out message);
        }

        void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
        {
            ((IAddChildViewModel)Editor).AddChildren(children, modifiers);
        }

        bool IInsertChildViewModel.CanInsertChildren(IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers, out string message)
        {
            if (((IAddChildViewModel)this).CanAddChildren(children, modifiers, out message))
                return true;

            message = "Invalid object";
            foreach (var child in children)
            {
                var image = child as SpriteInfoViewModel;
                if (image == null)
                    return false;
            }
            message = string.Format(position == InsertPosition.Before ? "Insert before {0}" : "Insert after {0}", Name);
            return true;
        }

        void IInsertChildViewModel.InsertChildren(IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers)
        {
            string message;
            if (((IAddChildViewModel)this).CanAddChildren(children, modifiers, out message) && children.All(x => x is UFile))
            {
                Editor.ImportFiles(children.OfType<UFile>(), index);
                return;
            }

            using (var transaction = Editor.UndoRedoService.CreateTransaction())
            {
                foreach (var image in children.OfType<SpriteInfoViewModel>())
                {
                    image.Editor.RemoveImage(image.GetSpriteInfo(), image.index);
                }

                var insertIndex = position == InsertPosition.After ? index + 1 : index;
                insertIndex = MathUtil.Clamp(insertIndex, 0, Editor.Sprites.Count);

                foreach (var image in children.OfType<SpriteInfoViewModel>())
                {
                    Editor.InsertImage(image.GetSpriteInfo(), insertIndex++);
                }
                Editor.UndoRedoService.SetName(transaction, "Move images");
            }
        }

        IObjectNode IPropertyProviderViewModel.GetRootNode()
        {
            var image = GetSpriteInfo();
            return Editor.Session.AssetNodeContainer.GetNode(image);
        }

        GraphNodePath IAssetPropertyProviderViewModel.GetAbsolutePathToRootNode()
        {
            var path = new GraphNodePath(Editor.Session.AssetNodeContainer.GetNode(Editor.Asset.Asset));
            path.PushMember(nameof(SpriteSheetAsset.Sprites));
            path.PushIndex(new NodeIndex(Index));
            path.PushTarget();
            return path;
        }

        bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => ((IPropertyProviderViewModel)Editor.Asset).ShouldConstructMember(member);

        bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => ((IPropertyProviderViewModel)Editor.Asset).ShouldConstructItem(collection, index);
    }
}
