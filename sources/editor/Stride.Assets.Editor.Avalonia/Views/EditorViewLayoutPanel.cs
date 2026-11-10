// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.ViewModels;
using Stride.Editor.Avalonia.Engine;
using Stride.Editor.Engine;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// Lays out the views of the game of a scene editor in a grid (prototype of several views): one view per pane of
/// <see cref="Layout"/>, with a header to change its camera, and splitters between them. Without a layout, it shows the
/// primary view alone.
/// </summary>
public sealed class EditorViewLayoutPanel : Grid
{
    /// <summary>
    /// Identifies the <see cref="GameView"/> property.
    /// </summary>
    public static readonly StyledProperty<object?> GameViewProperty = AvaloniaProperty.Register<EditorViewLayoutPanel, object?>(nameof(GameView));

    /// <summary>
    /// Identifies the <see cref="Viewport"/> property.
    /// </summary>
    public static readonly StyledProperty<IEditorGameViewport?> ViewportProperty = AvaloniaProperty.Register<EditorViewLayoutPanel, IEditorGameViewport?>(nameof(Viewport));

    /// <summary>
    /// Identifies the <see cref="Layout"/> property.
    /// </summary>
    public static readonly StyledProperty<EditorViewLayoutViewModel?> LayoutProperty = AvaloniaProperty.Register<EditorViewLayoutPanel, EditorViewLayoutViewModel?>(nameof(Layout));

    private const double SplitterSize = 4;

    private readonly Dictionary<int, PaneHost> hosts = [];
    private EditorViewLayoutViewModel? observedLayout;

    /// <summary>
    /// Gets or sets the control of the primary view of the game, or <c>null</c> while the game shows nothing.
    /// </summary>
    public object? GameView { get => GetValue(GameViewProperty); set => SetValue(GameViewProperty, value); }

    /// <summary>
    /// Gets or sets the viewport of the game, which creates the other views.
    /// </summary>
    public IEditorGameViewport? Viewport { get => GetValue(ViewportProperty); set => SetValue(ViewportProperty, value); }

    /// <summary>
    /// Gets or sets the views of the scene, or <c>null</c> to show the primary view alone.
    /// </summary>
    public EditorViewLayoutViewModel? Layout { get => GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LayoutProperty)
        {
            if (observedLayout is not null)
            {
                ((INotifyCollectionChanged)observedLayout.Panes).CollectionChanged -= PanesChanged;
                observedLayout.PropertyChanged -= LayoutPropertyChanged;
            }
            observedLayout = Layout;
            if (observedLayout is not null)
            {
                ((INotifyCollectionChanged)observedLayout.Panes).CollectionChanged += PanesChanged;
                observedLayout.PropertyChanged += LayoutPropertyChanged;
            }
            Rebuild();
        }
        else if (change.Property == GameViewProperty || change.Property == ViewportProperty)
        {
            Rebuild();
        }
    }

    private void PanesChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void LayoutPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditorViewLayoutViewModel.MaximizedPane) or nameof(EditorViewLayoutViewModel.Layout))
            Rebuild();
    }

    private void Rebuild()
    {
        Children.Clear();
        RowDefinitions.Clear();
        ColumnDefinitions.Clear();

        var multiViewport = Viewport as OffscreenGameViewport;
        if (GameView is not Control primaryView)
        {
            ReleaseHosts(keep: []);
            return;
        }

        var layout = Layout;
        if (layout is null || multiViewport is null)
        {
            ReleaseHosts(keep: []);
            Children.Add(primaryView);
            return;
        }

        var panes = layout.Panes.ToList();
        ReleaseHosts(keep: panes.Select(x => x.Id).ToHashSet());
        var shown = layout.MaximizedPane is { } maximized ? [maximized] : panes;
        var columns = shown.Count > 1 && layout.Layout != EditorViewLayout.TwoRows ? 2 : 1;
        var rows = (shown.Count + columns - 1) / columns;
        for (var c = 0; c < columns; c++)
        {
            if (c > 0)
                ColumnDefinitions.Add(new ColumnDefinition(SplitterSize, GridUnitType.Pixel));
            ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
        }
        for (var r = 0; r < rows; r++)
        {
            if (r > 0)
                RowDefinitions.Add(new RowDefinition(SplitterSize, GridUnitType.Pixel));
            RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
        }
        // The splitters between the panes: the user resizes them
        for (var c = 1; c < columns; c++)
        {
            var splitter = new GridSplitter { ResizeDirection = GridResizeDirection.Columns, Background = Brushes.Gray };
            SetColumn(splitter, c * 2 - 1);
            SetRowSpan(splitter, Math.Max(1, rows * 2 - 1));
            Children.Add(splitter);
        }
        for (var r = 1; r < rows; r++)
        {
            var splitter = new GridSplitter { ResizeDirection = GridResizeDirection.Rows, Background = Brushes.Gray };
            SetRow(splitter, r * 2 - 1);
            SetColumnSpan(splitter, Math.Max(1, columns * 2 - 1));
            Children.Add(splitter);
        }

        for (var i = 0; i < shown.Count; i++)
        {
            var pane = shown[i];
            if (!hosts.TryGetValue(pane.Id, out var host))
            {
                var view = pane.Id == IEditorGameMultiViewport.PrimaryView ? primaryView : multiViewport.CreateView(pane.Id);
                hosts.Add(pane.Id, host = new PaneHost(layout, pane, view));
            }
            host.ShowHeader = panes.Count > 1;
            SetRow(host, i / columns * 2);
            SetColumn(host, i % columns * 2);
            Children.Add(host);
        }
    }

    private void ReleaseHosts(HashSet<int> keep)
    {
        foreach (var (id, host) in hosts.ToList())
        {
            if (keep.Contains(id))
                continue;
            host.Release();
            hosts.Remove(id);
            if (id != IEditorGameMultiViewport.PrimaryView)
                (Viewport as OffscreenGameViewport)?.RemoveView(id);
        }
    }

    /// <summary>
    /// A pane: its view, a border that shows when it is the active one, and its header.
    /// </summary>
    private sealed class PaneHost : Border
    {
        private readonly EditorViewPaneViewModel pane;
        private readonly Panel content;
        private readonly Button header;
        private bool showsActive;

        public PaneHost(EditorViewLayoutViewModel layout, EditorViewPaneViewModel pane, Control view)
        {
            this.pane = pane;
            DataContext = pane;
            BorderThickness = new Thickness(1);
            var menu = new MenuFlyout();
            foreach (var camera in pane.Cameras)
                menu.Items.Add(new MenuItem { Header = camera.ToString(), Command = pane.SetCameraCommand, CommandParameter = camera });
            menu.Items.Add(new Separator());
            menu.Items.Add(new MenuItem { Header = "Maximize or restore", Command = layout.ToggleMaximizeCommand });
            header = new Button
            {
                Name = "PaneHeader",
                Focusable = false,
                Padding = new Thickness(6, 2),
                Margin = new Thickness(4),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Background = new SolidColorBrush(Color.FromArgb(160, 32, 32, 32)),
                Foreground = Brushes.White,
                Content = new TextBlock { [!TextBlock.TextProperty] = new Binding(nameof(EditorViewPaneViewModel.Title)) },
                Flyout = menu,
            };
            content = new Panel();
            content.Children.Add(view);
            content.Children.Add(header);
            Child = content;
            pane.PropertyChanged += PanePropertyChanged;
            UpdateActive();
        }

        /// <summary>
        /// Sets whether the layout has several panes: the header and the border of the active pane only show then.
        /// </summary>
        public bool ShowHeader
        {
            set
            {
                header.IsVisible = value;
                showsActive = value;
                UpdateActive();
            }
        }

        public void Release()
        {
            pane.PropertyChanged -= PanePropertyChanged;
            content.Children.Clear();
            Child = null;
        }

        private void PanePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditorViewPaneViewModel.IsActive))
                UpdateActive();
        }

        private void UpdateActive()
        {
            BorderBrush = showsActive && pane.IsActive ? Brushes.Orange : Brushes.Transparent;
        }
    }
}
