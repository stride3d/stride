// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// Arranges the items of an <see cref="ItemsControl"/> as tiles of the same size, in lines from left to right and from top to
/// bottom, and creates the containers of the visible lines only. Use it in a control that scrolls vertically, such as a
/// <see cref="ListBox"/>.
/// </summary>
/// <remarks>
/// Each item has a slot of <see cref="ItemSlotSize"/>. The lines are <see cref="MinimumItemSpacing"/> apart. The tiles of a
/// line share the width that the line leaves, at most <see cref="MaximumItemSpacing"/> between two tiles. The panel keeps the
/// containers of one more line above and below the viewport, and recycles the others; it keeps the container that has the
/// keyboard focus, so that the keyboard navigation goes on from it.
/// </remarks>
public sealed class VirtualizingTilePanel : VirtualizingPanel
{
    /// <summary>
    /// Identifies the <see cref="ItemSlotSize"/> property.
    /// </summary>
    public static readonly StyledProperty<Size> ItemSlotSizeProperty =
        AvaloniaProperty.Register<VirtualizingTilePanel, Size>(nameof(ItemSlotSize), new Size(64, 64), validate: IsValidSlotSize);

    /// <summary>
    /// Identifies the <see cref="MinimumItemSpacing"/> property.
    /// </summary>
    public static readonly StyledProperty<double> MinimumItemSpacingProperty =
        AvaloniaProperty.Register<VirtualizingTilePanel, double>(nameof(MinimumItemSpacing), 0.0, validate: IsValidSpacing);

    /// <summary>
    /// Identifies the <see cref="MaximumItemSpacing"/> property.
    /// </summary>
    public static readonly StyledProperty<double> MaximumItemSpacingProperty =
        AvaloniaProperty.Register<VirtualizingTilePanel, double>(nameof(MaximumItemSpacing), double.MaxValue, validate: x => x >= 0.0);

    // The containers that show an item, by the index of their item
    private readonly Dictionary<int, Control> realized = [];
    // The hidden containers that wait for an item, by their recycle key
    private readonly Dictionary<object, Stack<Control>> recyclePool = [];
    // The recycle key of each container that the panel created
    private readonly Dictionary<Control, object> recycleKeys = [];
    // The visible part of the panel, in its own coordinates; empty until the layout gives it
    private Rect viewport;

    static VirtualizingTilePanel()
    {
        AffectsMeasure<VirtualizingTilePanel>(ItemSlotSizeProperty, MinimumItemSpacingProperty, MaximumItemSpacingProperty);
    }

    public VirtualizingTilePanel()
    {
        EffectiveViewportChanged += OnEffectiveViewportChanged;
    }

    /// <summary>
    /// Gets or sets the size of the slot of each item.
    /// </summary>
    public Size ItemSlotSize { get => GetValue(ItemSlotSizeProperty); set => SetValue(ItemSlotSizeProperty, value); }

    /// <summary>
    /// Gets or sets the space between two lines, around the tiles, and the least space between two tiles of a line.
    /// </summary>
    public double MinimumItemSpacing { get => GetValue(MinimumItemSpacingProperty); set => SetValue(MinimumItemSpacingProperty, value); }

    /// <summary>
    /// Gets or sets the most space between two tiles of a line, when the line has room to spare.
    /// </summary>
    public double MaximumItemSpacing { get => GetValue(MaximumItemSpacingProperty); set => SetValue(MaximumItemSpacingProperty, value); }

    /// <summary>
    /// Gets the number of tiles in a line, from the last measure.
    /// </summary>
    public int ItemsPerLine { get; private set; } = 1;

    /// <summary>
    /// Gets the index of the first and of the last item whose container is shown; <c>(0, -1)</c> when none is.
    /// </summary>
    public (int First, int Last) RealizedRange { get; private set; } = (0, -1);

    /// <summary>
    /// Raised after a measure that shows other items than before (<see cref="RealizedRange"/>).
    /// </summary>
    public event EventHandler? RealizedRangeChanged;

    private double LineHeight => ItemSlotSize.Height + MinimumItemSpacing;

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var items = Items;
        var slot = ItemSlotSize;
        var spacing = MinimumItemSpacing;
        var width = double.IsFinite(availableSize.Width) ? availableSize.Width : viewport.Width;
        ItemsPerLine = Math.Max(1, (int)((width - spacing) / (slot.Width + spacing)));
        var lineCount = (items.Count + ItemsPerLine - 1) / ItemsPerLine;

        var (first, last) = GetRangeToRealize(items.Count);
        RecycleOutside(first, last);
        for (var index = first; index <= last; ++index)
        {
            if (!realized.TryGetValue(index, out var container))
            {
                container = Realize(items[index], index);
                realized[index] = container;
            }
            container.Measure(slot);
        }
        // The kept container of the focus is measured too, wherever it is
        foreach (var (index, container) in realized)
        {
            if (index < first || index > last)
                container.Measure(slot);
        }

        var range = last >= first ? (first, last) : (0, -1);
        if (range != RealizedRange)
        {
            RealizedRange = range;
            RealizedRangeChanged?.Invoke(this, EventArgs.Empty);
        }

        var desiredWidth = Math.Max(2 * spacing + slot.Width, double.IsFinite(availableSize.Width) ? availableSize.Width : 0);
        return new Size(desiredWidth, lineCount == 0 ? 0 : spacing + lineCount * LineHeight);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var space = ComputeItemSpacing(finalSize.Width);
        foreach (var (index, container) in realized)
        {
            container.Arrange(GetItemBounds(index, space));
        }
        return finalSize;
    }

    /// <inheritdoc />
    protected override Control? ScrollIntoView(int index)
    {
        var items = Items;
        if (index < 0 || index >= items.Count)
            return null;

        if (realized.TryGetValue(index, out var container) && container.IsVisible)
        {
            container.BringIntoView();
            return container;
        }

        // The line of the item comes into view, then the next layout creates its container
        this.BringIntoView(GetItemBounds(index, ComputeItemSpacing(Bounds.Width)));
        UpdateLayout();
        if (realized.TryGetValue(index, out container))
            return container;

        // Not shown by any viewport (no scroll owner, or not laid out yet): the item still gets a container
        container = Realize(items[index], index);
        realized[index] = container;
        container.Measure(ItemSlotSize);
        container.Arrange(GetItemBounds(index, ComputeItemSpacing(Bounds.Width)));
        InvalidateMeasure();
        return container;
    }

    /// <inheritdoc />
    protected override Control? ContainerFromIndex(int index) => realized.GetValueOrDefault(index);

    /// <inheritdoc />
    protected override int IndexFromContainer(Control container)
    {
        foreach (var (index, realizedContainer) in realized)
        {
            if (ReferenceEquals(realizedContainer, container))
                return index;
        }
        return -1;
    }

    /// <inheritdoc />
    protected override IEnumerable<Control>? GetRealizedContainers() => realized.OrderBy(x => x.Key).Select(x => x.Value).ToList();

    /// <inheritdoc />
    protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
    {
        var count = Items.Count;
        if (count == 0)
            return null;

        var fromIndex = from is Control control ? IndexFromContainer(control) : -1;
        var visibleLines = Math.Max(1, (int)(viewport.Height / LineHeight));
        var target = direction switch
        {
            NavigationDirection.First => 0,
            NavigationDirection.Last => count - 1,
            _ when fromIndex < 0 => 0,
            NavigationDirection.Left or NavigationDirection.Previous => fromIndex - 1,
            NavigationDirection.Right or NavigationDirection.Next => fromIndex + 1,
            NavigationDirection.Up => fromIndex - ItemsPerLine,
            NavigationDirection.Down => fromIndex + ItemsPerLine,
            NavigationDirection.PageUp => Math.Max(0, fromIndex - visibleLines * ItemsPerLine),
            NavigationDirection.PageDown => Math.Min(count - 1, fromIndex + visibleLines * ItemsPerLine),
            _ => -1,
        };
        if (target < 0 || target >= count)
        {
            if (!wrap || direction is not (NavigationDirection.Next or NavigationDirection.Previous))
                return null;
            target = target < 0 ? count - 1 : 0;
        }
        return ScrollIntoView(target);
    }

    /// <inheritdoc />
    protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(items, e);
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewStartingIndex >= 0:
                Shift(e.NewStartingIndex, e.NewItems?.Count ?? 0);
                break;
            case NotifyCollectionChangedAction.Remove when e.OldStartingIndex >= 0:
                var removedCount = e.OldItems?.Count ?? 0;
                RecycleRange(e.OldStartingIndex, e.OldStartingIndex + removedCount - 1);
                Shift(e.OldStartingIndex + removedCount, -removedCount);
                break;
            case NotifyCollectionChangedAction.Replace when e.OldStartingIndex >= 0:
                RecycleRange(e.OldStartingIndex, e.OldStartingIndex + (e.OldItems?.Count ?? 0) - 1);
                break;
            default:
                RecycleRange(int.MinValue, int.MaxValue);
                break;
        }
        InvalidateMeasure();
    }

    private static bool IsValidSlotSize(Size size) => size.Width >= 1 && size.Height >= 1 && double.IsFinite(size.Width) && double.IsFinite(size.Height);

    private static bool IsValidSpacing(double spacing) => spacing >= 0 && double.IsFinite(spacing);

    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
        var oldRange = GetRangeToRealize(Items.Count);
        viewport = e.EffectiveViewport;
        if (GetRangeToRealize(Items.Count) != oldRange)
            InvalidateMeasure();
    }

    /// <summary>
    /// Gets the items of the lines that the viewport shows, with one more line above and below.
    /// </summary>
    private (int First, int Last) GetRangeToRealize(int count)
    {
        if (count == 0 || viewport.Width <= 0 || viewport.Height <= 0)
            return (0, -1);

        var lineHeight = LineHeight;
        var lineCount = (count + ItemsPerLine - 1) / ItemsPerLine;
        var firstLine = Math.Max(0, (int)Math.Floor((viewport.Top - MinimumItemSpacing) / lineHeight) - 1);
        var lastLine = Math.Min(lineCount - 1, (int)Math.Floor((viewport.Bottom - MinimumItemSpacing) / lineHeight) + 1);
        if (lastLine < firstLine)
            return (0, -1);
        return (firstLine * ItemsPerLine, Math.Min(count - 1, (lastLine + 1) * ItemsPerLine - 1));
    }

    private double ComputeItemSpacing(double width)
    {
        if (ItemsPerLine <= 1)
            return MinimumItemSpacing;
        var innerSpace = Math.Max(0, width - ItemsPerLine * ItemSlotSize.Width - 2 * MinimumItemSpacing);
        return Math.Max(MinimumItemSpacing, Math.Min(innerSpace / (ItemsPerLine - 1), MaximumItemSpacing));
    }

    private Rect GetItemBounds(int index, double space)
    {
        var line = index / ItemsPerLine;
        var column = index % ItemsPerLine;
        var x = MinimumItemSpacing + column * (ItemSlotSize.Width + space);
        var y = MinimumItemSpacing + line * LineHeight;
        return new Rect(new Point(x, y), ItemSlotSize);
    }

    private Control Realize(object? item, int index)
    {
        var generator = ItemContainerGenerator!;
        if (!generator.NeedsContainer(item, index, out var recycleKey))
        {
            // The item is its own container
            var own = (Control)item!;
            generator.PrepareItemContainer(own, item, index);
            AddInternalChild(own);
            generator.ItemContainerPrepared(own, item, index);
            return own;
        }

        recycleKey ??= typeof(Control);
        if (recyclePool.TryGetValue(recycleKey, out var pool) && pool.TryPop(out var container))
        {
            container.SetCurrentValue(IsVisibleProperty, true);
            generator.PrepareItemContainer(container, item, index);
            generator.ItemContainerPrepared(container, item, index);
            return container;
        }

        container = generator.CreateContainer(item, index, recycleKey);
        recycleKeys[container] = recycleKey;
        generator.PrepareItemContainer(container, item, index);
        AddInternalChild(container);
        generator.ItemContainerPrepared(container, item, index);
        return container;
    }

    private void RecycleOutside(int first, int last)
    {
        foreach (var index in realized.Keys.Where(x => x < first || x > last).ToList())
        {
            if (realized[index].IsKeyboardFocusWithin)
                continue;
            Recycle(index);
        }
    }

    private void RecycleRange(int first, int last)
    {
        foreach (var index in realized.Keys.Where(x => x >= first && x <= last).ToList())
            Recycle(index);
    }

    private void Recycle(int index)
    {
        var container = realized[index];
        realized.Remove(index);
        ItemContainerGenerator!.ClearItemContainer(container);
        if (recycleKeys.TryGetValue(container, out var recycleKey))
        {
            container.SetCurrentValue(IsVisibleProperty, false);
            if (!recyclePool.TryGetValue(recycleKey, out var pool))
                recyclePool[recycleKey] = pool = new Stack<Control>();
            pool.Push(container);
        }
        else
        {
            // An item that is its own container leaves the panel with its item
            RemoveInternalChild(container);
        }
    }

    /// <summary>
    /// Moves the containers of the items from <paramref name="start"/> on by <paramref name="offset"/>, after an insertion or a
    /// removal.
    /// </summary>
    private void Shift(int start, int offset)
    {
        if (offset == 0)
            return;

        var moved = realized.Where(x => x.Key >= start).ToList();
        foreach (var (index, _) in moved)
            realized.Remove(index);
        foreach (var (index, container) in moved)
        {
            realized[index + offset] = container;
            ItemContainerGenerator!.ItemContainerIndexChanged(container, index, index + offset);
        }
    }
}
