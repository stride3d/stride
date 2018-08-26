// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#define DEBUGVIRTUALIZATIONno

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Xenko.Core.Annotations;

namespace Xenko.Core.Presentation.Controls
{
    public class VirtualizingTreePanel : VirtualizingPanel, IScrollInfo
    {
        internal class VerticalArea
        {
            public double Top { get; set; }

            public double Bottom { get; set; }

            public bool Overlaps([NotNull] VerticalArea area)
            {
                return Top <= area.Bottom && area.Top <= Bottom;
            }
        }

        internal class SizesCache
        {
            readonly Dictionary<int, List<CachedSize>> cache;

            public SizesCache()
            {
                cache = new Dictionary<int, List<CachedSize>>();
            }

            public void AddOrChange(int level, double size)
            {
                List<CachedSize> levelList;
                if (cache.ContainsKey(level)) { levelList = cache[level]; }
                else
                {
                    levelList = new List<CachedSize>(5);
                    cache.Add(level, levelList);
                }

                CachedSize cachedSize = null;
                foreach (var s in levelList)
                {
                    if (s.IsEqual(size))
                    {
                        cachedSize = s;
                        break;
                    }
                }

                if (cachedSize == null)
                {
                    // if list is full, replace item with lowest count, to give other items a chance
                    if (levelList.Count > 4)
                    {
                        cachedSize = new CachedSize { OccuranceCounter = int.MaxValue, Size = size };
                        var indexToReplace = 0;
                        var smallestCounter = int.MaxValue;
                        for (var i = 0; i < 5; i++)
                        {
                            if (levelList[i].OccuranceCounter < smallestCounter) indexToReplace = i;
                        }
                        levelList[indexToReplace].OccuranceCounter = 1;
                        levelList[indexToReplace].Size = size;
                        cachedSize = levelList[indexToReplace];
                    }
                    else
                    {
                        // add new size to list
                        cachedSize = new CachedSize { OccuranceCounter = 1, Size = size };
                        levelList.Add(cachedSize);
                    }
                }
                else
                {
                    // prevent overflow
                    if (cachedSize.OccuranceCounter == int.MaxValue)
                    {
                        foreach (var s in levelList)
                        {
                            s.OccuranceCounter = s.OccuranceCounter / 2;
                        }
                    }

                    // count occurance up
                    cachedSize.OccuranceCounter++;
                }
            }

            public bool ContainsItems(int level)
            {
                if (cache.ContainsKey(level))
                {
                    return cache[level].Count > 0;
                }

                return false;
            }

            public void CleanUp(int level)
            {
                cache.Remove(level);
            }

            public double GetEstimate(int level)
            {
                if (cache.ContainsKey(level))
                {
                    var maxUsedSize = new CachedSize { OccuranceCounter = 0 };
                    foreach (var s in cache[level])
                    {
                        if (maxUsedSize.OccuranceCounter < s.OccuranceCounter) maxUsedSize = s;
                    }

                    return maxUsedSize.Size;
                }

                return 0;
            }

            class CachedSize
            {
                public double Size { get; set; }
                public int OccuranceCounter { get; set; }

                public bool IsEqual(double size)
                {
                    return Math.Abs(Size - size) < 1;
                }
            }
        }

        private const double ScrollLineDelta = 16.0;
        private readonly SizesCache cachedSizes;
        private Size extent = new Size(0, 0);
        private Size viewport = new Size(0, 0);

        public VirtualizingTreePanel()
        {
            cachedSizes = new SizesCache();
            CanHorizontallyScroll = true;
            CanVerticallyScroll = true;
        }

        /// <summary>
        /// Measure the children
        /// </summary>
        /// <param name="availableSize">Size available</param>
        /// <returns>Size desired</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (ScrollOwner != null)
            {
                if (ScrollOwner.ScrollableWidth < HorizontalOffset) SetHorizontalOffset(ScrollOwner.ScrollableWidth);
                if (ScrollOwner.ScrollableHeight < VerticalOffset) SetVerticalOffset(ScrollOwner.ScrollableHeight);
            }

            // We need to access InternalChildren before the generator to work around a bug
            var children = InternalChildren;
            var generator = ItemContainerGenerator;
            var itemsControl = ItemsControl.GetItemsOwner(this);
            var treeViewItem = itemsControl as TreeViewItem;
            var treeView = itemsControl as TreeView ?? treeViewItem.ParentTreeView;
            Debug(treeViewItem, "Measuring");
            double maxWidth = 0;
            double currentYinItemSystem = 0;

            if (treeView.IsVirtualizing)
            {
                // never forget: virtualization of a tree is an approximation. there are some use cases which theoretically work and others
                // we try to get it working by estimations. See GetCachedOrEstimatedHeight for more infos.

                var itemCount = itemsControl.Items.Count;
                var firstVisibleItemIndex = 0;
                var lastVisibleItemIndex = itemCount;

                double itemTop;
                if (treeViewItem != null)
                {
                    itemTop = treeViewItem.ItemTopInTreeSystem + GetHeightOfHeader(itemsControl);
                }
                else
                {
                    // get the area where items have to be visualized. This is from top to bottom of the visible space in tree system. 
                    // We add a little of offset. It seems like it improves estimation of heights.
                    double predictionOffset = 50;
                    var top = VerticalOffset - predictionOffset;
                    if (top < 0) top = 0;
                    treeView.RealizationSpace.Top = top;
                    treeView.RealizationSpace.Bottom = VerticalOffset + availableSize.Height + predictionOffset;

                    itemTop = GetHeightOfHeader(itemsControl);
                }

                var itemGeneratorIndex = 0;
                var isPreviousItemVisible = false;
                IDisposable generatorRun = null;
                currentYinItemSystem = 0;
                var childHierarchyLevel = 0;
                if (treeViewItem != null) childHierarchyLevel = treeViewItem.HierachyLevel + 1;
                try
                {
                    // iterate child items
                    for (var i = 0; i < itemCount; i++)
                    {
                        var estimatedHeight = GetCachedOrEstimatedHeight(treeView, childHierarchyLevel);
                        var childSpace = new VerticalArea();
                        childSpace.Top = itemTop + currentYinItemSystem;
                        childSpace.Bottom = childSpace.Top + estimatedHeight;

                        // check if item is possibly visible or could become visible if someone changes expanding of siblings
                        var isVisibleItem = treeView.RealizationSpace.Overlaps(childSpace);

                        if (isVisibleItem)
                        {
                            // we have found a visible item, lets check if its the first visible item.
                            if (!isPreviousItemVisible)
                            {
                                // we found a visible item, lets initialize the visible item section of the loop
                                isPreviousItemVisible = true;
                                firstVisibleItemIndex = i;
                                var startPos = generator.GeneratorPositionFromIndex(i);
                                itemGeneratorIndex = (startPos.Offset == 0) ? startPos.Index : startPos.Index + 1;
                                generatorRun = generator.StartAt(startPos, GeneratorDirection.Forward, true);
                            }
                            else
                            {
                                itemGeneratorIndex++;
                            }

                            // Get or create the child
                            bool newlyRealized;
                            var child = generator.GenerateNext(out newlyRealized) as TreeViewItem;
                            Debug(treeViewItem, "Found visible child: " + child.DataContext);

                            if (newlyRealized)
                            {
                                // Figure out if we need to insert the child at the end or somewhere in the middle
                                AddOrInsertItemToInternalChildren(itemGeneratorIndex, child);
                                child.ParentTreeView = treeView;
                                generator.PrepareItemContainer(child);
                            }
                            else
                            {
                                // The child has already been created, let's be sure it's in the right spot
                                if (child != children[itemGeneratorIndex]) throw new InvalidOperationException("Wrong child was generated");
                            }

                            if (treeViewItem != null)
                            {
                                child.ItemTopInTreeSystem = currentYinItemSystem + itemTop;
                                child.HierachyLevel = treeViewItem.HierachyLevel + 1;
                            }
                            else
                            {
                                child.ItemTopInTreeSystem = currentYinItemSystem;
                                child.HierachyLevel = 1;
                            }

                            InvalidateMeasure(child);
                            child.Measure(new Size(double.MaxValue, double.MaxValue));

                            // add real height to cache
                            var heightOfChild = child.DesiredSize.Height;
                            RegisterHeight(treeView, childHierarchyLevel, heightOfChild);
                            currentYinItemSystem += child.DesiredSize.Height;
                            // save the maximum needed width
                            maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
                        }
                        else
                        {
                            //Debug(treeViewItem, "Found invisible child: " + i);
                            if (isPreviousItemVisible)
                            {
                                // set last visible index. this is important, to cleanup not anymore used items
                                lastVisibleItemIndex = i;
                                isPreviousItemVisible = false;
                            }

                            // dispose generator run. if we do it after the whole loop, we run in multithreading issues
                            if (generatorRun != null)
                            {
                                generatorRun.Dispose();
                                generatorRun = null;
                            }

                            currentYinItemSystem += GetCachedOrEstimatedHeight(treeView, childHierarchyLevel);
                        }
                        //Debug(treeViewItem, "Current y for " + i + ": " + currentYinItemSystem);
                    }
                }
                finally
                {
                    //just for safety
                    if (generatorRun != null)
                    {
                        generatorRun.Dispose();
                        generatorRun = null;
                    }
                }

                //Debug("Cleaning all items but " + firstVisibleItemIndex + " to " + lastVisibleItemIndex + " for element " + itemsControl.DataContext);
                CleanUpItems(firstVisibleItemIndex, lastVisibleItemIndex);
            }
            else
            {
                //Debug("Virtualization is OFF.");
                var startPos = generator.GeneratorPositionFromIndex(0);
                using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
                {
                    for (var i = (startPos.Offset == 0) ? startPos.Index : startPos.Index + 1; i < itemsControl.Items.Count; i++)
                    {
                        // Get or create the child
                        bool newlyRealized;
                        var child = generator.GenerateNext(out newlyRealized) as TreeViewItem;
                        if (newlyRealized)
                        {
                            // Figure out if we need to insert the child at the end or somewhere in the middle
                            AddOrInsertItemToInternalChildren(i, child);
                            child.ParentTreeView = treeView ?? treeViewItem.ParentTreeView;
                            generator.PrepareItemContainer(child);
                        }

                        child.Measure(new Size(double.MaxValue, double.MaxValue));
                        // now get the real height
                        var height = child.DesiredSize.Height;
                        // add real height to current position
                        currentYinItemSystem += height;
                        // save the maximum needed width
                        maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
                    }
                }
            }

            if (double.IsPositiveInfinity(maxWidth) || double.IsPositiveInfinity(currentYinItemSystem))
            {
                throw new InvalidOperationException("???");
            }

            Extent = new Size(maxWidth, currentYinItemSystem);
            Viewport = availableSize;
            Debug(treeViewItem, "Desired height: " + Extent.Height);
            return Extent;
        }

        private static void InvalidateMeasure([NotNull] TreeViewItem child)
        {
            var itemsPresenter = child.Template.FindName("itemsPresenter", child) as FrameworkElement;
            if (itemsPresenter != null)
            {
                var virtualizingTreePanel = VisualTreeHelper.GetChild(itemsPresenter, 0) as UIElement;
                virtualizingTreePanel.InvalidateMeasure();
            }
        }

        private static double GetHeightOfHeader([NotNull] ItemsControl itemsControl)
        {
            var border = (FrameworkElement)itemsControl.Template.FindName("border", itemsControl);
            return border.DesiredSize.Height;
        }

        /// <summary>
        /// Arrange the children
        /// </summary>
        /// <param name="finalSize">Size available</param>
        /// <returns>Size used</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var itemsControl = ItemsControl.GetItemsOwner(this);
            var treeViewItem = itemsControl as TreeViewItem;
            var treeView = itemsControl as TreeView ?? treeViewItem.ParentTreeView;
            var generator = ItemContainerGenerator;

            //Extent = finalSize;
            var foundVisibleItem = false; ;
            double currentY = 0;
            if (treeView.IsVirtualizing)
            {
                //Debug("Arrange-" + itemsControl.DataContext);
                for (var i = 0; i < itemsControl.Items.Count; i++)
                {
                    var child = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
                    var childHierarchyLevel = 0;
                    if (child != null) childHierarchyLevel = child.HierachyLevel;

                    if (foundVisibleItem)
                    {
                        if (child == null)
                        {
                            // other items are not visible / virtualized
                            break;
                        }
                    }
                    else
                    {
                        if (child != null)
                        {
                            // found first visible item
                            foundVisibleItem = true;
                        }
                    }

                    if (child != null)
                    {
                        child.Arrange(new Rect(-HorizontalOffset, currentY - VerticalOffset, finalSize.Width, child.DesiredSize.Height));
                        currentY += child.ActualHeight;
                    }
                    else
                    {
                        currentY += GetCachedOrEstimatedHeight(treeView, childHierarchyLevel);
                    }
                }
            }
            else
            {
                for (var i = 0; i < itemsControl.Items.Count; i++)
                {
                    var child = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as UIElement;

                    child?.Arrange(new Rect(-HorizontalOffset, currentY - VerticalOffset, finalSize.Width, child.DesiredSize.Height));
                    currentY += child.DesiredSize.Height;
                }
            }

            return finalSize;
        }

        private void AddOrInsertItemToInternalChildren(int itemGeneratorIndex, [NotNull] TreeViewItem child)
        {
            if (itemGeneratorIndex >= InternalChildren.Count)
            {
                AddInternalChild(child);
            }
            else
            {
                InsertInternalChild(itemGeneratorIndex, child);
            }
        }

        /// <summary>
        /// Revirtualize items that are no longer visible
        /// </summary>
        /// <param name="minDesiredGenerated">first item index that should be visible</param>
        /// <param name="maxDesiredGenerated">last item index that should be visible</param>
        private void CleanUpItems(int minDesiredGenerated, int maxDesiredGenerated)
        {
            var children = InternalChildren;
            var generator = ItemContainerGenerator;

            for (var i = children.Count - 1; i >= 0; i--)
            {
                var childGeneratorPos = new GeneratorPosition(i, 0);
                var itemIndex = generator.IndexFromGeneratorPosition(childGeneratorPos);
                if (itemIndex < minDesiredGenerated || itemIndex > maxDesiredGenerated)
                {
                    generator.Remove(childGeneratorPos, 1);
                    RemoveInternalChildRange(i, 1);
                }
            }

            cachedSizes.CleanUp(maxDesiredGenerated);
        }

        /// <summary>
        /// When items are removed, remove the corresponding UI if necessary
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
                    break;
                case NotifyCollectionChangedAction.Move:
                    RemoveInternalChildRange(args.OldPosition.Index, args.ItemUICount);
                    break;
            }
        }

        #region Layout specific code

        /// <summary>
        /// Returns the size of the container for a given item.  The size can come from the container, a lookup, or a guess depending
        /// on the virtualization state of the item.
        /// </summary>
        /// <returns>The cached or estimated size.</returns>
        /// <remarks>This estimation looks if the given index is cached. If not it returns the maximum height of the cached
        /// containers. If no container is cached, returns zero. 
        /// One case it fails is, if all cached items are bigger
        /// than the estimated items. This leads to jumping scrollbars. The effect is not that bad, if many items will be visualized.</remarks>
        private double GetCachedOrEstimatedHeight(TreeView tree, int level)
        {
            if (cachedSizes.ContainsItems(0)) return cachedSizes.GetEstimate(0);

            return tree.CachedSizes.GetEstimate(level);
        }

        private void RegisterHeight([NotNull] TreeView tree, int level, double size)
        {
            cachedSizes.AddOrChange(0, size);
            tree.CachedSizes.AddOrChange(level, size);
        }
        #endregion

        public Size Extent
        {
            get
            {
                return extent;
            }
            set
            {
                if (extent == value) return;
                extent = value;

                ScrollOwner?.InvalidateScrollInfo();
            }
        }

        public Size Viewport
        {
            get
            {
                return viewport;
            }
            set
            {
                if (viewport == value) return;
                viewport = value;

                ScrollOwner?.InvalidateScrollInfo();
            }
        }

        private double GetScrollLineHeightY()
        {
            var itemsControl = ItemsControl.GetItemsOwner(this);
            if (GetScrollUnit(itemsControl) == ScrollUnit.Item)
            {
                var treeViewItem = itemsControl as TreeViewItem;
                var treeView = itemsControl as TreeView ?? treeViewItem?.ParentTreeView;
                if (treeView != null)
                {
                    if (treeView.IsVirtualizing)
                        return GetCachedOrEstimatedHeight(treeView, treeViewItem?.HierachyLevel ?? 0);

                    if (treeViewItem != null)
                        return GetHeightOfHeader(treeViewItem);

                    if (treeView.HasItems)
                    {
                        var firstItem = itemsControl.ItemContainerGenerator.ContainerFromIndex(0) as TreeViewItem;
                        if (firstItem != null)
                            return GetHeightOfHeader(firstItem);
                    }
                }
            }
            return ScrollLineDelta;
        }

        private double GetScrollLineHeightX()
        {
            return ScrollLineDelta;
        }

        [Conditional("DEBUGVIRTUALIZATION")]
        private void Debug(TreeViewItem item, [NotNull] string message)
        {
            if (item != null)
            {
                System.Diagnostics.Debug.Write($"{item.DataContext,15}--");
                var indent = GetHierarchyLevel();
                for (var i = 0; i < indent; i++)
                {
                    System.Diagnostics.Debug.Write("--");
                }
            }
            else { System.Diagnostics.Debug.Write("               --"); }
            System.Diagnostics.Debug.Write(">");

            System.Diagnostics.Debug.WriteLine(message);
        }

        private int GetHierarchyLevel()
        {
            var treeViewItem = ItemsControl.GetItemsOwner(this) as TreeViewItem;
            if (treeViewItem == null) return 0;
            return treeViewItem.HierachyLevel;
        }

        #region IScrollInfo implementation

        public ScrollViewer ScrollOwner { get; set; }

        public bool CanHorizontallyScroll { get; set; }

        public bool CanVerticallyScroll { get; set; }

        public double HorizontalOffset { get; private set; }

        public double VerticalOffset { get; private set; }

        public double ExtentHeight => Extent.Height;

        public double ExtentWidth => Extent.Width;

        public double ViewportHeight => Viewport.Height;

        public double ViewportWidth => Viewport.Width;

        public void LineUp()
        {
            SetVerticalOffset(VerticalOffset - GetScrollLineHeightY());
        }

        public void LineDown()
        {
            SetVerticalOffset(VerticalOffset + GetScrollLineHeightY());
        }

        public void PageUp()
        {
            SetVerticalOffset(VerticalOffset - viewport.Height + 10);
        }

        public void PageDown()
        {
            SetVerticalOffset(VerticalOffset + viewport.Height - 10);
        }

        public void MouseWheelUp()
        {
            var lines = SystemParameters.WheelScrollLines;
            SetVerticalOffset(VerticalOffset - lines * GetScrollLineHeightY());
        }

        public void MouseWheelDown()
        {
            var lines = SystemParameters.WheelScrollLines;
            SetVerticalOffset(VerticalOffset + lines * GetScrollLineHeightY());
        }

        public void LineLeft()
        {
            SetHorizontalOffset(HorizontalOffset - GetScrollLineHeightX());
        }

        public void LineRight()
        {
            SetHorizontalOffset(HorizontalOffset + GetScrollLineHeightX());
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            if (rectangle.IsEmpty || visual == null || ReferenceEquals(visual, this) || !IsAncestorOf(visual))
            {
                return Rect.Empty;
            }

            var treeViewItem = visual as TreeViewItem;
            FrameworkElement element;
            if (treeViewItem != null)
            {
                element = treeViewItem.Template.FindName("border", treeViewItem) as FrameworkElement;
            }
            else
            {
                element = visual as FrameworkElement;
            }

            var transform = visual.TransformToAncestor(this);
            var p = transform.Transform(new Point(0, 0));
            var rect = new Rect(p, element.RenderSize);

            if (rect.X < 0)
            {
                SetHorizontalOffset(HorizontalOffset + rect.X);
            }
            else if (treeViewItem != null && treeViewItem.ParentTreeView.ActualWidth < rect.X)
            {
                SetHorizontalOffset(HorizontalOffset + rect.X);
            }

            if (rect.Y < 0)
            {
                SetVerticalOffset(VerticalOffset + rect.Y);
            }
            else if (treeViewItem != null && treeViewItem.ParentTreeView.ActualHeight < rect.Y + rect.Height)
            {
                // set 5 more, so the next item is realized for sure.
                var verticalOffset = rect.Y + rect.Height + VerticalOffset - treeViewItem.ParentTreeView.ActualHeight + 5;
                SetVerticalOffset(verticalOffset);
            }

            return new Rect(HorizontalOffset, VerticalOffset, ViewportWidth, ViewportHeight);
        }

        public void MouseWheelLeft()
        {
            var lines = SystemParameters.WheelScrollLines;
            SetHorizontalOffset(HorizontalOffset - lines * GetScrollLineHeightX());
        }

        public void MouseWheelRight()
        {
            var lines = SystemParameters.WheelScrollLines;
            SetHorizontalOffset(HorizontalOffset + lines * GetScrollLineHeightX());
        }

        public void PageLeft()
        {
            SetHorizontalOffset(HorizontalOffset - viewport.Width + 10);
        }

        public void PageRight()
        {
            SetHorizontalOffset(HorizontalOffset + viewport.Width - 10);
        }

        public void SetHorizontalOffset(double offset)
        {
            if (offset < 0 || viewport.Width >= extent.Width)
            {
                offset = 0;
            }
            else
            {
                if (offset + viewport.Width >= extent.Width)
                {
                    offset = extent.Width - viewport.Width;
                }
            }

            HorizontalOffset = offset;

            ScrollOwner?.InvalidateScrollInfo();

            // Force us to realize the correct children
            InvalidateMeasure();
        }

        public void SetVerticalOffset(double offset)
        {
            if (offset < 0 || viewport.Height >= extent.Height)
            {
                offset = 0;
            }
            else
            {
                if (offset + viewport.Height >= extent.Height)
                {
                    offset = extent.Height - viewport.Height;
                }
            }

            VerticalOffset = offset;

            ScrollOwner?.InvalidateScrollInfo();

            // Force us to realize the correct children
            InvalidateMeasure();
        }

        #endregion
    }
}
