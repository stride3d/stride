// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Stride.Assets.Sprite;
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
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Quantum;
using Stride.Core.Translation;

namespace Stride.Assets.Editor.ViewModels.SpriteEditor;

/// <summary>
/// A single <see cref="SpriteInfo"/> of a <see cref="SpriteSheetEditorViewModel"/>: its name, its source image, its region,
/// borders and center. It provides the properties of the sprite to the property grid.
/// </summary>
[DebuggerDisplay("SpriteInfo - Name={Name}")]
public sealed class SpriteInfoViewModel : DispatcherViewModel, IIsEditableViewModel, IInsertChildViewModel, IAddChildViewModel, IAssetPropertyProviderViewModel
{
    private readonly SpriteInfo sprite;
    private readonly IMemberNode sourceNode;
    private readonly MemberGraphNodeBinding<UFile> sourceNodeBinding;
    private readonly MemberGraphNodeBinding<string> nameNodeBinding;
    private FileSystemWatcher? sourceFileWatcher;
    private UFile? sourceFileWatcherPath;
    private bool isEditable = true;
    private bool isEditing;
    private int index;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpriteInfoViewModel"/> class.
    /// </summary>
    /// <param name="editor">The <see cref="SpriteSheetEditorViewModel"/> containing this image.</param>
    /// <param name="sprite">The <see cref="SpriteInfo"/> represented by this view model.</param>
    public SpriteInfoViewModel(SpriteSheetEditorViewModel editor, SpriteInfo sprite)
        : base(editor.ServiceProvider)
    {
        Editor = editor;
        Id = new AbsoluteId(editor.Asset.Id, Guid.NewGuid());
        this.sprite = sprite;
        Index = -1;

        // Retrieve nodes
        var spriteNode = editor.Session.AssetNodeContainer.GetOrCreateNode(sprite);
        var nameNode = spriteNode[nameof(SpriteInfo.Name)];
        sourceNode = spriteNode[nameof(SpriteInfo.Source)];
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

    /// <summary>
    /// Gets the identifier of this sprite in the selection history.
    /// </summary>
    public AbsoluteId Id { get; }

    /// <summary>
    /// Gets or sets the name of the sprite.
    /// </summary>
    public string? Name { get => nameNodeBinding.Value; set => nameNodeBinding.Value = value; }

    /// <summary>
    /// Gets the index of the sprite in the sprite sheet.
    /// </summary>
    /// <remarks>This property is updated by the <see cref="SpriteSheetEditorViewModel"/> when it alters the order.</remarks>
    public int Index { get => index; internal set => SetValue(ref index, value); }

    /// <summary>
    /// Gets or sets the source file of this sprite.
    /// </summary>
    public UFile? Source { get => sourceNodeBinding.Value; set => sourceNodeBinding.Value = value; }

    /// <inheritdoc/>
    public bool IsEditable { get => isEditable; set => SetValue(ref isEditable, value); }

    /// <inheritdoc/>
    public bool IsEditing
    {
        get => isEditing;
        set
        {
            SetValue(ref isEditing, value);
            // The keys of the editor (F, M) go to the name while it is edited
            Editor.FocusOnRegionCommand.IsEnabled = !value;
            Editor.ToggleUseMagicWandCommand.IsEnabled = !value;
        }
    }

    /// <summary>
    /// Gets the borders of the sprite (UI sprite sheets).
    /// </summary>
    public SpriteBordersViewModel SpriteBorders { get; }

    /// <summary>
    /// Gets the center of the sprite (2D sprite sheets).
    /// </summary>
    public SpriteCenterViewModel SpriteCenter { get; }

    /// <summary>
    /// Gets the region of the image that the sprite uses.
    /// </summary>
    public TextureRegionViewModel TextureRegion { get; }

    /// <summary>
    /// Gets the instance of <see cref="SpriteSheetEditorViewModel"/> related to this object.
    /// </summary>
    public SpriteSheetEditorViewModel Editor { get; }

    /// <summary>
    /// Gets a command that will open the source file for edition.
    /// </summary>
    public ICommandBase EditImageCommand { get; }

    /// <summary>
    /// Gets a command that will show the source file in the file manager.
    /// </summary>
    public ICommandBase ExploreCommand { get; }

    /// <inheritdoc/>
    bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

    /// <inheritdoc/>
    AssetViewModel IAssetPropertyProviderViewModel.RelatedAsset => Editor.Asset;

    /// <inheritdoc />
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(SpriteInfoViewModel));
        sourceNode.ValueChanged -= SourceValueChanged;
        sourceFileWatcher?.Dispose();
        sourceFileWatcher = null;
        nameNodeBinding.Dispose();
        sourceNodeBinding.Dispose();
        TextureRegion.Destroy();
        SpriteCenter.Destroy();
        SpriteBorders.Destroy();
        base.Destroy();
    }

    /// <summary>
    /// Retrieves the <see cref="SpriteInfo"/> represented by this view model.
    /// </summary>
    /// <returns>The <see cref="SpriteInfo"/> represented by this view model.</returns>
    public SpriteInfo GetSpriteInfo()
    {
        return sprite;
    }

    private async Task EditImage()
    {
        try
        {
            var path = GetSpriteInfo().Source?.ToOSPath() ?? "";
            if (!File.Exists(path))
            {
                await ServiceProvider.Get<IDialogService>().MessageBoxAsync(Tr._p("Message", "Couldn't find the file"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var startInfo = new ProcessStartInfo(path) { UseShellExecute = true };
            // Windows opens the image in its editor rather than its viewer; the other platforms open it with the default application
            if (OperatingSystem.IsWindows())
                startInfo.Verb = startInfo.Verbs.FirstOrDefault(x => x.ToLowerInvariant() == "edit");
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            var message = string.Format(Tr._p("Message", "There was a problem while editing the image.{0}"), ex.FormatSummary(true));
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task Explore()
    {
        try
        {
            var source = GetSpriteInfo().Source;
            var path = source?.ToOSPath() ?? "";
            if (!File.Exists(path))
            {
                await ServiceProvider.Get<IDialogService>().MessageBoxAsync(Tr._p("Message", "Couldn't find the file"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Windows' explorer selects the file; the other file managers open its folder
            var startInfo = OperatingSystem.IsWindows()
                ? new ProcessStartInfo("explorer.exe", "/select," + path) { UseShellExecute = true }
                : new ProcessStartInfo(source!.GetFullDirectory().ToOSPath()) { UseShellExecute = true };
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            var message = string.Format(Tr._p("Message", "There was a problem opening Explorer.{0}"), ex.FormatSummary(true));
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Error);
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
        sourceFileWatcherPath = null;

        // Create a new watcher if the path is valid.
        if (Source is not null && File.Exists(Source))
        {
            sourceFileWatcher = new FileSystemWatcher(Source.GetFullDirectory(), Source.GetFileName());
            sourceFileWatcher.Changed += SourceFileChanged;
            sourceFileWatcher.EnableRaisingEvents = true;
            sourceFileWatcherPath = Source;
        }
    }

    private async void SourceFileChanged(object? sender, FileSystemEventArgs e)
    {
        // Loading sometimes fail if we try too soon.
        await Task.Delay(100);
        Dispatcher.InvokeAsync(() =>
        {
            if (IsDestroyed)
                return;

            OnPropertyChanging(nameof(Source));
            OnPropertyChanged(nameof(Source));

            TextureRegion.RefreshImageSize();
        }).Forget();
    }

    private void SourceValueChanged(object? sender, MemberNodeChangeEventArgs e)
    {
        UpdateSourceFileWatcher();
        TextureRegion.RefreshImageSize();
        if (string.IsNullOrEmpty(e.OldValue?.ToString()) && e.NewValue != null && !Editor.UndoRedoService.UndoRedoInProgress)
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

        message = DragDropBehavior.InvalidDropAreaMessage;
        if (children.Any(x => x is not SpriteInfoViewModel))
            return false;

        message = string.Format(position == InsertPosition.Before ? "Insert before {0}" : "Insert after {0}", Name);
        return true;
    }

    void IInsertChildViewModel.InsertChildren(IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers)
    {
        if (((IAddChildViewModel)this).CanAddChildren(children, modifiers, out _) && children.All(x => x is UFile))
        {
            Editor.ImportFiles(children.OfType<UFile>(), index);
            return;
        }

        var images = children.OfType<SpriteInfoViewModel>().ToList();
        var spriteInfos = images.Select(x => x.GetSpriteInfo()).ToList();
        using (var transaction = Editor.UndoRedoService.CreateTransaction())
        {
            foreach (var image in images)
            {
                // The editor updates the indices of the sprites after each removal, this one's included
                image.Editor.RemoveImage(image.GetSpriteInfo(), image.Index);
            }

            var insertIndex = position == InsertPosition.After ? index + 1 : index;
            insertIndex = MathUtil.Clamp(insertIndex, 0, Editor.Sprites.Count);

            foreach (var spriteInfo in spriteInfos)
            {
                Editor.InsertImage(spriteInfo, insertIndex++);
            }
            Editor.UndoRedoService.SetName(transaction, "Move images");
        }
        // The moved sprites stay selected, as after the reorder buttons
        Editor.SelectSprites(spriteInfos);
    }

    IObjectNode IPropertyProviderViewModel.GetRootNode()
    {
        return Editor.Session.AssetNodeContainer.GetNode(GetSpriteInfo());
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
