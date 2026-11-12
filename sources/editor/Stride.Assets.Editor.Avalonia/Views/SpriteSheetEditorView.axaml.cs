// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.ViewModels.SpriteEditor;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Commands;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// The view of the sprite sheet editor: the sprites on the left, the image of the
/// selected sprite on the right with its region, borders and center and their handles, and the toolbar of the tools.
/// </summary>
[AssetEditorView<SpriteSheetEditorViewModel>]
public sealed partial class SpriteSheetEditorView : UserControl, IAssetEditorView
{
    public SpriteSheetEditorView()
    {
        InitializeComponent();
        // The size of the image gives the size of the content of the viewport
        SpriteImage.PropertyChanged += OnSpriteImagePropertyChanged;
        // The pixel tools pick before the handles: they handle the tunnelling event
        SpriteImage.AddHandler(PointerPressedEvent, OnSpriteImagePointerPressed, RoutingStrategies.Tunnel);
        SpriteList.AddHandler(ContextRequestedEvent, OnSpriteListContextRequested, RoutingStrategies.Tunnel);
        SpriteList.AddHandler(KeyDownEvent, OnSpriteListKeyDown, RoutingStrategies.Tunnel);
    }

    private SpriteSheetEditorViewModel? Editor => DataContext as SpriteSheetEditorViewModel;

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        UpdateContentSize();
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        // The editor takes the focus so that its shortcuts work (commands of the registry scoped to this editor)
        if (!IsKeyboardFocusWithin)
            Focus();
    }

    private void OnSpriteImagePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Image.SourceProperty)
            UpdateContentSize();
    }

    private void UpdateContentSize()
    {
        if (Editor is not { } editor)
            return;

        // The size of the image file (the size of a bitmap is not the one of the image on every rendering platform)
        if (editor.Cache?.GetPixelSize(editor.CurrentSprite?.Source) is { } size)
        {
            editor.Viewport.ContentWidth = size.Width;
            editor.Viewport.ContentHeight = size.Height;
        }
        else
        {
            var pixelSize = (SpriteImage.Source as Bitmap)?.PixelSize ?? default;
            editor.Viewport.ContentWidth = pixelSize.Width;
            editor.Viewport.ContentHeight = pixelSize.Height;
        }
    }

    private void OnSpriteImagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Editor is not { } editor || !e.GetCurrentPoint(SpriteImage).Properties.IsLeftButtonPressed)
            return;

        // The pixel of the image under the mouse: the image is shown at the zoom of the viewport
        var position = e.GetPosition(SpriteImage);
        var scale = editor.Viewport.ScaleFactor;
        var pick = new SpriteImagePick(new Vector2((float)(position.X / scale), (float)(position.Y / scale)), e.KeyModifiers.HasFlag(KeyModifiers.Control));
        if (editor.FindSpriteRegionCommand.CanExecute(pick))
            editor.FindSpriteRegionCommand.Execute(pick);
    }

    private void OnSpriteListContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        var menu = (ContextMenu)Resources["SpriteContextMenu"]!;
        menu.DataContext = DataContext;
        // Opened here rather than through the ContextMenu property: a menu set while the event is routed would only open
        // at the next request
        menu.Open(SpriteList);
        e.Handled = true;
    }

    private void OnSpriteListKeyDown(object? sender, KeyEventArgs e)
    {
        // Enter and Escape end a rename: the keyboard goes back to the renamed sprite, as it would otherwise stay with the
        // hidden text box and the keys of the list (Delete, F2) would do nothing
        if (e.Source is TextBox && e.Key is Key.Enter or Key.Escape)
            Dispatcher.UIThread.Post(() => (SpriteList.ContainerFromItem(SpriteList.SelectedItem!) ?? SpriteList).Focus());
    }

    private void OnSpriteDoubleTapped(object? sender, TappedEventArgs e)
    {
        // A double click renames the sprite
        if ((sender as Control)?.DataContext is SpriteInfoViewModel { IsEditable: true } sprite)
        {
            sprite.IsEditing = true;
            e.Handled = true;
        }
    }
}
