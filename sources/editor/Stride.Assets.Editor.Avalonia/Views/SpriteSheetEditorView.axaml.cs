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
/// The view of the sprite sheet editor, ported from the WPF <c>SpriteEditorView</c>: the sprites on the left, the image of the
/// selected sprite on the right with its region, borders and center and their handles, and the toolbar of the tools.
/// </summary>
[AssetEditorView<SpriteSheetEditorViewModel>]
public sealed partial class SpriteSheetEditorView : UserControl, IAssetEditorView
{
    public SpriteSheetEditorView()
    {
        InitializeComponent();
        // The size of the image gives the size of the content of the viewport, as the actual size of the WPF image
        SpriteImage.PropertyChanged += OnSpriteImagePropertyChanged;
        // The pixel tools pick before the handles: the WPF image used the preview event
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
        // The editor takes the focus so that its keys work, as in WPF
        if (!IsKeyboardFocusWithin)
            Focus();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // The keys of the WPF sprite editor. They reach the view model after the focused control, so typing in a text box
        // (a name, the color key) doesn't trigger them.
        if (e.Handled || e.Source is TextBox || Editor is not { } editor)
            return;

        var (command, parameter) = (e.Key, e.KeyModifiers) switch
        {
            (Key.J, KeyModifiers.Control) => (editor.SelectNextSpriteCommand, -1),
            (Key.K, KeyModifiers.Control) => (editor.SelectNextSpriteCommand, 1),
            (Key.U, KeyModifiers.Control) => (editor.AddNewImageCommand, (object?)null),
            (Key.D, KeyModifiers.Control) => (editor.DuplicateSelectedImagesCommand, null),
            (Key.W, KeyModifiers.Control) => (editor.UseWholeImageCommand, null),
            (Key.M, KeyModifiers.None) => (editor.ToggleUseMagicWandCommand, null),
            (Key.F, KeyModifiers.None) => (editor.FocusOnRegionCommand, null),
            _ => ((ICommandBase?)null, null),
        };
        if (command is not null && command.CanExecute(parameter))
        {
            command.Execute(parameter);
            e.Handled = true;
        }
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
        // A double click renames the sprite, as in the WPF list
        if ((sender as Control)?.DataContext is SpriteInfoViewModel { IsEditable: true } sprite)
        {
            sprite.IsEditing = true;
            e.Handled = true;
        }
    }
}
