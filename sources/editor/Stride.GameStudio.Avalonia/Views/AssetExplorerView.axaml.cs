// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Avalonia.Controls;

namespace Stride.GameStudio.Avalonia.Views;

public partial class AssetExplorerView : UserControl
{
    /// <summary>
    /// The smallest size of the thumbnails.
    /// </summary>
    public const double MinimumThumbnailSize = 16.0;

    /// <summary>
    /// The largest size of the thumbnails.
    /// </summary>
    public const double MaximumThumbnailSize = 128.0;

    /// <summary>
    /// How much a zoom step changes the size of the thumbnails.
    /// </summary>
    public const double ThumbnailZoomIncrement = 16.0;

    /// <summary>
    /// Identifies the <see cref="ThumbnailSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ThumbnailSizeProperty =
        AvaloniaProperty.Register<AssetExplorerView, double>(nameof(ThumbnailSize), 96.0, coerce: (_, value) => Math.Clamp(value, MinimumThumbnailSize, MaximumThumbnailSize));

    // The room that a tile takes around its thumbnail: its border and the padding of its item, then its name and its type
    private const double TileHorizontalRoom = 12.0;
    private const double TileVerticalRoom = 64.0;

    private readonly List<MenuItem> assetCommandItems = [];
    private VirtualizingTilePanel? tilePanel;
    private bool thumbnailPriorityPending;

    public AssetExplorerView()
    {
        InitializeComponent();
        // The zoom of the last session
        ThumbnailSize = InternalSettings.AssetViewTileThumbnailZoom.GetValue();
        ApplyThumbnailSize();
        AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
        AssetList.Loaded += OnAssetListLoaded;
    }

    /// <summary>
    /// Gets or sets the size of the thumbnails of the tiles, between <see cref="MinimumThumbnailSize"/> and
    /// <see cref="MaximumThumbnailSize"/>. The editor keeps it for the next sessions.
    /// </summary>
    public double ThumbnailSize { get => GetValue(ThumbnailSizeProperty); set => SetValue(ThumbnailSizeProperty, value); }

    /// <summary>
    /// Makes the thumbnails larger by one step.
    /// </summary>
    public void ZoomIn() => ThumbnailSize += ThumbnailZoomIncrement;

    /// <summary>
    /// Makes the thumbnails smaller by one step.
    /// </summary>
    public void ZoomOut() => ThumbnailSize -= ThumbnailZoomIncrement;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ThumbnailSizeProperty)
        {
            ApplyThumbnailSize();
            InternalSettings.AssetViewTileThumbnailZoom.SetValue(ThumbnailSize);
        }
    }

    /// <summary>
    /// Sizes the tiles from the thumbnail size: the resources of the tile styles and of the tile panel.
    /// </summary>
    private void ApplyThumbnailSize()
    {
        var size = ThumbnailSize;
        Resources["TileThumbnailSize"] = size;
        Resources["TileWidth"] = size + 4;
        Resources["TileSlotSize"] = new Size(size + TileHorizontalRoom, size + TileVerticalRoom);
        Resources["TileMaximumSpacing"] = size * 0.375;
    }

    private void OnZoomInClick(object? sender, RoutedEventArgs e) => ZoomIn();

    private void OnZoomOutClick(object? sender, RoutedEventArgs e) => ZoomOut();

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Ctrl+wheel zooms instead of scrolling
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.Delta.Y == 0)
            return;

        if (e.Delta.Y > 0)
            ZoomIn();
        else
            ZoomOut();
        e.Handled = true;
    }

    private void OnAssetListLoaded(object? sender, RoutedEventArgs e)
    {
        if (tilePanel is not null)
            tilePanel.RealizedRangeChanged -= OnRealizedRangeChanged;
        tilePanel = AssetList.ItemsPanelRoot as VirtualizingTilePanel;
        if (tilePanel is not null)
            tilePanel.RealizedRangeChanged += OnRealizedRangeChanged;
    }

    /// <summary>
    /// Builds the thumbnails of the tiles on screen first, once the scrolling stops creating others.
    /// </summary>
    private void OnRealizedRangeChanged(object? sender, EventArgs e)
    {
        if (thumbnailPriorityPending)
            return;
        thumbnailPriorityPending = true;
        Dispatcher.UIThread.Post(() =>
        {
            thumbnailPriorityPending = false;
            if (tilePanel is not { RealizedRange: var (first, last) } || DataContext is not AssetCollectionViewModel viewModel)
                return;

            var items = AssetList.Items;
            var visible = new List<AssetViewModel>();
            for (var index = first; index <= last && index < items.Count; ++index)
            {
                if (items[index] is AssetViewModel asset)
                    visible.Add(asset);
            }
            if (visible.Count > 0)
                viewModel.Session.Thumbnails.IncreaseThumbnailPriority(visible);
        }, DispatcherPriority.Background);
    }

    /// <summary>
    /// Wires the Add asset flyout to the templates of the asset view (built on the first opening) and its commands.
    /// </summary>
    private void OnAddAssetFlyoutOpening(object? sender, EventArgs e)
    {
        if (DataContext is not AssetCollectionViewModel viewModel)
            return;

        AddAssetItems.DataContext = viewModel.AddAssetTemplateCollection;
        AddAssetItems.AddItemCommand = viewModel.RunAssetTemplateCommand;
        AddAssetItems.SelectFilesToCreateItemCommand = viewModel.SelectFilesToCreateAssetCommand;
    }

    /// <summary>
    /// Lists every template again and focuses the search box, once the flyout can take the focus.
    /// </summary>
    private void OnAddAssetFlyoutOpened(object? sender, EventArgs e)
    {
        AddAssetItems.Reset();
    }

    /// <summary>
    /// Closes the flyout when a template is validated (its command then runs).
    /// </summary>
    private void OnAddAssetItemValidated(object? sender, TemplateValidatedEventArgs e)
    {
        AddAssetButton.Flyout?.Hide();
    }

    private void OnAddAssetImportRequested(object? sender, EventArgs e)
    {
        // The file picker takes over: the list is not needed behind it
        AddAssetButton.Flyout?.Hide();
    }

    /// <summary>
    /// Inserts the commands of the selected asset (<see cref="AssetViewModel.AssetCommands"/>: create a derived asset...) after the
    /// asset items.
    /// </summary>
    private void OnAssetContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        foreach (var item in assetCommandItems)
        {
            AssetContextMenu.Items.Remove(item);
        }
        assetCommandItems.Clear();

        if (DataContext is not AssetCollectionViewModel { SingleSelectedAsset: { } asset })
            return;

        var index = AssetContextMenu.Items.IndexOf(AssetCommandsSeparator) + 1;
        foreach (var command in asset.AssetCommands)
        {
            var item = new MenuItem
            {
                Header = command.DisplayName,
                Command = command.Command,
                CommandParameter = command,
                InputGesture = string.IsNullOrEmpty(command.Gesture) ? null : KeyGesture.Parse(command.Gesture),
            };
            if (!string.IsNullOrEmpty(command.Tooltip))
                ToolTip.SetTip(item, command.Tooltip);
            AssetContextMenu.Items.Insert(index++, item);
            assetCommandItems.Add(item);
        }
    }
}
