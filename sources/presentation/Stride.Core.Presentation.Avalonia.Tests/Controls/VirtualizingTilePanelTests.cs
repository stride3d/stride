// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Presentation.Avalonia.Controls;

namespace Stride.Core.Presentation.Avalonia.Tests.Controls;

/// <summary>
/// The tile panel: lines of tiles of one size, with the containers of the visible lines only.
/// </summary>
public sealed class VirtualizingTilePanelTests
{
    // A list of 350 x 300 with slots of 100 x 50, 10 apart: 3 tiles a line, 5 lines on screen
    private const int ItemCount = 1000;

    [AvaloniaFact]
    public void OnlyTheVisibleLinesHaveContainers()
    {
        var (window, list, panel) = Show(Enumerable.Range(0, ItemCount).Select(x => $"Item {x}"));
        try
        {
            Assert.Equal(3, panel.ItemsPerLine);
            var containers = Containers(list);
            // The 5 or 6 lines that the viewport shows, and one more below
            Assert.InRange(containers.Count, 3 * 5, 3 * 8);
            Assert.Equal(containers.Count, panel.RealizedRange.Last - panel.RealizedRange.First + 1);
            Assert.Equal(0, panel.RealizedRange.First);
            Assert.Equal("Item 0", containers[0].Content);

            // The tiles are in lines, 10 apart, sharing the width that the line leaves
            var first = list.ContainerFromIndex(0)!;
            var second = list.ContainerFromIndex(1)!;
            var below = list.ContainerFromIndex(3)!;
            Assert.Equal(new Size(100, 50), first.Bounds.Size);
            Assert.Equal(10, first.Bounds.Top);
            Assert.Equal(first.Bounds.Left, below.Bounds.Left);
            Assert.Equal(first.Bounds.Bottom + 10, below.Bounds.Top);
            Assert.True(second.Bounds.Left - first.Bounds.Right >= 10);

            // The extent holds every line
            var scrollViewer = list.GetVisualDescendants().OfType<ScrollViewer>().Single();
            Assert.Equal(10 + 334 * 60, scrollViewer.Extent.Height, 1);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ScrollingRecyclesTheContainers()
    {
        var (window, list, panel) = Show(Enumerable.Range(0, ItemCount).Select(x => $"Item {x}"));
        try
        {
            var created = list.GetVisualDescendants().OfType<ListBoxItem>().Count();
            var scrollViewer = list.GetVisualDescendants().OfType<ScrollViewer>().Single();

            scrollViewer.Offset = new Vector(0, scrollViewer.Extent.Height);
            window.UpdateLayout();

            // The last items are shown, with the containers of the first ones
            Assert.Equal(ItemCount - 1, panel.RealizedRange.Last);
            Assert.Equal("Item 999", ContentAt(list, ItemCount - 1));
            Assert.Null(list.ContainerFromIndex(0));
            Assert.InRange(list.GetVisualDescendants().OfType<ListBoxItem>().Count(), created, created + 6);

            scrollViewer.Offset = new Vector(0, 0);
            window.UpdateLayout();
            Assert.Equal("Item 0", ContentAt(list, 0));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ScrollIntoViewCreatesTheContainerOfAFarItem()
    {
        var (window, list, panel) = Show(Enumerable.Range(0, ItemCount).Select(x => $"Item {x}"));
        try
        {
            list.ScrollIntoView(600);
            window.UpdateLayout();

            var container = list.ContainerFromIndex(600);
            Assert.NotNull(container);
            Assert.Equal("Item 600", ContentAt(list, 600));
            var scrollViewer = list.GetVisualDescendants().OfType<ScrollViewer>().Single();
            var top = container.TranslatePoint(default, scrollViewer)!.Value.Y;
            Assert.InRange(top, 0, scrollViewer.Viewport.Height - 50);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheArrowKeysMoveByTileAndByLine()
    {
        var (window, list, _) = Show(Enumerable.Range(0, ItemCount).Select(x => $"Item {x}"));
        try
        {
            list.SelectedIndex = 4;
            list.ContainerFromIndex(4)!.Focus(NavigationMethod.Tab);
            Dispatcher.UIThread.RunJobs();

            Press(list, Key.Right);
            Assert.Equal(5, list.SelectedIndex);
            Press(list, Key.Down);
            Assert.Equal(8, list.SelectedIndex);
            Press(list, Key.Left);
            Assert.Equal(7, list.SelectedIndex);
            Press(list, Key.Up);
            Assert.Equal(4, list.SelectedIndex);

            // Down through the lines that are not on screen yet: their containers are created, and the selection follows
            for (var i = 0; i < 20; ++i)
                Press(list, Key.Down);
            Assert.Equal(64, list.SelectedIndex);
            Assert.NotNull(list.ContainerFromIndex(64));
            Assert.True(list.ContainerFromIndex(64)!.IsKeyboardFocusWithin);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheContainersFollowTheChangesOfTheItems()
    {
        var items = new ObservableCollection<string>(Enumerable.Range(0, 100).Select(x => $"Item {x}"));
        var (window, list, _) = Show(items);
        try
        {
            items.Insert(0, "New");
            window.UpdateLayout();
            Assert.Equal(["New", "Item 0", "Item 1"], [.. Enumerable.Range(0, 3).Select(x => ContentAt(list, x))]);

            items.RemoveAt(1);
            window.UpdateLayout();
            Assert.Equal(["New", "Item 1", "Item 2"], [.. Enumerable.Range(0, 3).Select(x => ContentAt(list, x))]);

            items[1] = "Replaced";
            window.UpdateLayout();
            Assert.Equal("Replaced", ContentAt(list, 1));

            items.Clear();
            window.UpdateLayout();
            Assert.Empty(Containers(list));

            items.Add("Alone");
            window.UpdateLayout();
            Assert.Equal("Alone", Assert.Single(Containers(list)).Content);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ALargerSlotPutsFewerTilesInALine()
    {
        var (window, list, panel) = Show(Enumerable.Range(0, ItemCount).Select(x => $"Item {x}"));
        try
        {
            var visible = Containers(list).Count;

            panel.ItemSlotSize = new Size(150, 100);
            window.UpdateLayout();

            Assert.Equal(2, panel.ItemsPerLine);
            Assert.Equal(new Size(150, 100), list.ContainerFromIndex(0)!.Bounds.Size);
            Assert.True(Containers(list).Count < visible);
        }
        finally
        {
            window.Close();
        }
    }

    private static (Window Window, ListBox List, VirtualizingTilePanel Panel) Show(IEnumerable<string> items)
    {
        var list = new ListBox
        {
            ItemsSource = items,
            ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingTilePanel { ItemSlotSize = new Size(100, 50), MinimumItemSpacing = 10, MaximumItemSpacing = 20 }),
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0),
        };
        ScrollViewer.SetHorizontalScrollBarVisibility(list, global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled);
        var window = new Window { Width = 350, Height = 300, Content = list };
        window.Show();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        var panel = (VirtualizingTilePanel)list.ItemsPanelRoot!;
        return (window, list, panel);
    }

    private static object? ContentAt(ListBox list, int index) => ((ListBoxItem)list.ContainerFromIndex(index)!).Content;

    private static List<ListBoxItem> Containers(ListBox list)
    {
        return [.. list.GetRealizedContainers().OfType<ListBoxItem>().OrderBy(list.IndexFromContainer)];
    }

    private static void Press(ListBox list, Key key)
    {
        var focused = TopLevel.GetTopLevel(list)!.FocusManager!.GetFocusedElement() as Control ?? list;
        focused.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, Source = focused });
        Dispatcher.UIThread.RunJobs();
        list.UpdateLayout();
    }
}
