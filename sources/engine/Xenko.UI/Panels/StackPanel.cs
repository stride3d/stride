// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;

namespace Xenko.UI.Panels
{
    /// <summary>
    /// Arranges child elements into a single line that can be oriented horizontally or vertically.
    /// </summary>
    [DataContract(nameof(StackPanel))]
    [DebuggerDisplay("StackPanel - Name={Name}")]
    public class StackPanel : Panel, IScrollInfo
    {
        /// <summary>
        /// Indicate the first index of Vector3 to use to maximize depending on the stack panel orientation.
        /// </summary>
        protected static readonly int[] OrientationToMaximizeIndex1 = { 1, 0, 0 };
        /// <summary>
        /// Indicate the second index of Vector3 to use to accumulate depending on the stack panel orientation.
        /// </summary>
        protected static readonly int[] OrientationToMaximizeIndex2 = { 2, 2, 1 };
        /// <summary>
        /// Indicate the axis along which the measure zone is infinite depending on the scroll owner scrolling mode.
        /// </summary>
        protected static readonly List<int[]> ScrollingModeToInfiniteAxis = new List<int[]>
            {
                new int[0],
                new []{ 0 },
                new []{ 1 },
                new []{ 2 },
                new []{ 0, 1 },
                new []{ 1, 2 },
                new []{ 2, 0 },
            };

        private Vector3 offset;
        private Orientation orientation = Orientation.Vertical;

        /// <summary>
        /// The current scroll position of the top/left corner.
        /// </summary>
        private float scrollPosition;

        /// <summary>
        /// The current scroll position of the left/top corner of the stack panel.
        /// </summary>
        /// <remarks>The stack panel scroll position is expressed element index units.
        /// For example: 0 represents the first element, 1 represents the second element, 1.33 represents a third of the second element, etc...</remarks>
        public float ScrollPosition => scrollPosition;

        private bool itemVirtualizationEnabled;

        /// <summary>
        /// The list of the visible children having the same order as in <see cref="Panel.Children"/>. 
        /// </summary>
        /// <remarks>This list is valid on when <see cref="ItemVirtualizationEnabled"/> is <value>true</value></remarks>
        private readonly FastCollection<UIElement> visibleChildren = new FastCollection<UIElement>();

        private readonly FastCollection<UIElement> cachedVisibleChildren = new FastCollection<UIElement>();

        private Vector3 extent;

        private readonly List<float> elementBounds = new List<float>();

        /// <summary>
        /// The 0-based index of the last element that can be scrolled to. This value is valid only if <see cref="ItemVirtualizationEnabled"/> is false.
        /// </summary>
        private int indexElementMaxScrolling;

        /// <summary>
        /// Gets or sets the value indicating if the <see cref="StackPanel"/> children must be virtualized or not.
        /// When children virtualization is activated, hidden children's measurement, arrangement and draw are avoided.
        /// </summary>
        /// <userdoc>True if the Stack Panel's children must be virtualized, false otherwise.</userdoc>
        [DataMember]
        [Display(category: BehaviorCategory)]
        [DefaultValue(false)]
        public bool ItemVirtualizationEnabled
        {
            get { return itemVirtualizationEnabled; }
            set
            {
                if (itemVirtualizationEnabled == value)
                    return;

                itemVirtualizationEnabled = value;

                // recreate the visual children collection with all the stack children if virtualization is disabled
                if (!itemVirtualizationEnabled)
                {
                    // remove the partial list of visible children
                    while (VisualChildrenCollection.Count > 0)
                        SetVisualParent(VisualChildrenCollection[0], null);

                    // add all of them back
                    foreach (var child in Children)
                        SetVisualParent(child, this);

                    // resort the children by z-order
                    VisualChildrenCollection.Sort(PanelChildrenSorter);
                }
                else
                {
                    visibleChildren.Clear();
                }

                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates the orientation by which child elements are stacked.
        /// </summary>
        /// <userdoc>The orientation by which children are stacked.</userdoc>
        [DataMember]
        [Display(category: LayoutCategory)]
        [DefaultValue(Orientation.Vertical)]
        public Orientation Orientation
        {
            get { return orientation; }
            set
            {
                if (orientation == value)
                    return;

                orientation = value;
                InvalidateMeasure();
            }
        }

        private enum ScrollRequestType
        {
            AbsolutePosition,
            RelativeElement,
            RelativePosition,
        }

        private struct ScrollRequest
        {
            public float ScrollValue;
            public ScrollRequestType Type;
        }

        private readonly List<ScrollRequest> scrollingRequets = new List<ScrollRequest>();

        /// <summary>
        /// Estimate the length of the extent from the visible elements.
        /// </summary>
        /// <remarks>This should be used only when item virtualization is enabled</remarks>
        /// <returns>The estimated size of the extent</returns>
        private float EstimateExtentLength()
        {
            var scrollAxis = (int)Orientation;

            // estimate the size of the extent using the last elements of the list
            // we use always those elements in order to have a constant extent size estimation and because those sizes are also pre-calculated by ScrollBarPosition

            var indexElement = Children.Count;
            var accumulatedSize = 0f;
            while (indexElement > 0 && accumulatedSize < Viewport[scrollAxis])
            {
                --indexElement;
                accumulatedSize += GetSafeChildSize(indexElement, scrollAxis);
            }

            // calculate the size taken by all elements if proportional
            return accumulatedSize / (Children.Count - indexElement) * Children.Count;
        }

        protected override void OnLogicalChildRemoved(UIElement oldElement, int index)
        {
            base.OnLogicalChildRemoved(oldElement, index);

            if (index < (int)Math.Floor(scrollPosition))
                --scrollPosition;
        }

        protected override void OnLogicalChildAdded(UIElement newElement, int index)
        {
            base.OnLogicalChildAdded(newElement, index);

            if (index < (int)Math.Floor(scrollPosition))
                ++scrollPosition;
        }

        protected override Vector3 MeasureOverride(Vector3 availableSizeWithoutMargins)
        {
            Viewport = availableSizeWithoutMargins;

            // update the visible children if item virtualization is enabled.
            if (ItemVirtualizationEnabled)
                AdjustOffsetsAndVisualChildren(scrollPosition);

            var accumulatorIndex = (int)Orientation;
            var maximizeIndex1 = OrientationToMaximizeIndex1[(int)Orientation];
            var maximizeIndex2 = OrientationToMaximizeIndex2[(int)Orientation];

            // compute the size available to the children depending on the stack orientation
            var childAvailableSizeWithMargins = availableSizeWithoutMargins;
            childAvailableSizeWithMargins[accumulatorIndex] = float.PositiveInfinity;

            // add infinite bounds depending on scroll owner scrolling mode
            if (ScrollOwner != null)
            {
                foreach (var i in ScrollingModeToInfiniteAxis[(int)ScrollOwner.ScrollMode])
                    childAvailableSizeWithMargins[i] = float.PositiveInfinity;
            }

            // measure all the children
            var children = ItemVirtualizationEnabled ? visibleChildren : Children;
            foreach (var child in children)
                child.Measure(childAvailableSizeWithMargins);

            // calculate the stack panel desired size
            var desiredSize = Vector3.Zero;
            foreach (var child in children)
            {
                desiredSize[accumulatorIndex] += child.DesiredSizeWithMargins[accumulatorIndex];
                desiredSize[maximizeIndex1] = Math.Max(desiredSize[maximizeIndex1], child.DesiredSizeWithMargins[maximizeIndex1]);
                desiredSize[maximizeIndex2] = Math.Max(desiredSize[maximizeIndex2], child.DesiredSizeWithMargins[maximizeIndex2]);
            }

            return desiredSize;
        }

        protected override Vector3 ArrangeOverride(Vector3 finalSizeWithoutMargins)
        {
            visibleChildren.Clear(); // children's children may have changed we need to force the rearrangement.

            Viewport = finalSizeWithoutMargins;

            // determine the stack panel axis
            var stackAxis = (int)Orientation;

            // re-arrange all children and update item position cache data when virtualization is off
            if (!ItemVirtualizationEnabled)
            {
                ArrangeChildren();

                // determine the index of the last element that we can scroll to
                indexElementMaxScrolling = elementBounds.Count - 2;
                while (indexElementMaxScrolling > 0 && elementBounds[indexElementMaxScrolling] > elementBounds[elementBounds.Count - 1] - Viewport[stackAxis])
                    --indexElementMaxScrolling;
            }

            // update the extent (extent need to be valid before updating scrolling)
            extent = finalSizeWithoutMargins;
            if (ItemVirtualizationEnabled)
                extent[stackAxis] = EstimateExtentLength();
            else
                extent[stackAxis] = elementBounds[elementBounds.Count - 1];

            // Update the scrolling
            if (scrollingRequets.Count > 0) // perform scroll requests
            {
                foreach (var request in scrollingRequets)
                {
                    switch (request.Type)
                    {
                        case ScrollRequestType.AbsolutePosition:
                            ScrolllToElement(request.ScrollValue);
                            break;
                        case ScrollRequestType.RelativeElement:
                            ScrolllToNeigbourElement(Orientation, request.ScrollValue);
                            break;
                        case ScrollRequestType.RelativePosition:
                            ScrollOf(request.ScrollValue);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            else // update children and scrolling info (mainly offsets)
            {
                AdjustOffsetsAndVisualChildren(scrollPosition);
            }
            scrollingRequets.Clear();

            // invalidate anchor info
            ScrollOwner?.InvalidateAnchorInfo();

            return finalSizeWithoutMargins;
        }

        private void ArrangeChildren()
        {
            // reset the anchor bounds
            elementBounds.Clear();

            // cache the accumulator and maximize indices 
            var accumulatorIndex = (int)Orientation;
            var maximizeIndex1 = OrientationToMaximizeIndex1[(int)Orientation];
            var maximizeIndex2 = OrientationToMaximizeIndex2[(int)Orientation];

            // add the first element bound
            elementBounds.Add(0);

            // arrange all the children
            var children = ItemVirtualizationEnabled ? visibleChildren : Children;
            foreach (var child in children)
            {
                var startBound = elementBounds[elementBounds.Count - 1];

                // compute the child origin
                var childOrigin = -Viewport / 2; // correspond to (left, top, back) parent corner
                childOrigin[accumulatorIndex] += startBound;

                // set the arrange matrix of the child
                child.DependencyProperties.Set(PanelArrangeMatrixPropertyKey, Matrix.Translation(childOrigin));

                // compute the size given to the child
                var childSizeWithMargins = child.DesiredSizeWithMargins;
                childSizeWithMargins[maximizeIndex1] = Viewport[maximizeIndex1];
                childSizeWithMargins[maximizeIndex2] = Viewport[maximizeIndex2];

                // arrange the child
                child.Arrange(childSizeWithMargins, IsCollapsed);

                // add the next element bound
                if (child.IsCollapsed)
                    elementBounds.Add(startBound);
                else
                    elementBounds.Add(startBound + child.RenderSize[accumulatorIndex] + child.MarginInternal[accumulatorIndex] + child.MarginInternal[3 + accumulatorIndex]);
            }
        }

        public bool CanScroll(Orientation direction)
        {
            return direction == Orientation;
        }

        public Vector3 Extent => extent;

        public Vector3 Offset => offset;

        public Vector3 Viewport { get; private set; }

        public override Vector2 GetSurroudingAnchorDistances(Orientation direction, float position)
        {
            if (direction != Orientation)
                return base.GetSurroudingAnchorDistances(direction, position);

            Vector2 distances;

            GetDistanceToSurroundingAnchors((int)direction, out distances);

            return distances;
        }

        private void GetDistanceToSurroundingAnchors(int axisIndex, out Vector2 distances)
        {
            var currentElementIndex = (int)Math.Floor(scrollPosition);
            var currentElementRatio = scrollPosition - currentElementIndex;
            var currentElementSize = GetSafeChildSize(currentElementIndex, axisIndex);
            var elementSizeRatio = currentElementRatio * currentElementSize;

            distances = new Vector2(-elementSizeRatio, currentElementSize - elementSizeRatio);
        }

        /// <summary>
        /// Jump to the element having the provided index.
        /// </summary>
        /// <param name="elementIndex">The index (0-based) of the element in the stack panel to jump to</param>
        public void ScrolllToElement(float elementIndex)
        {
            if (IsArrangeValid)
            {
                AdjustOffsetsAndVisualChildren(elementIndex);
            }
            else
            {
                InvalidateArrange(); // force next arrange so that requests can be processed.
                scrollingRequets.Clear(); // optimization delete previous requests when the request is absolute
                scrollingRequets.Add(new ScrollRequest { ScrollValue = elementIndex });
            }
        }

        private void ScrolllToElement(Orientation orientation, float elementIndex)
        {
            if (Orientation != orientation)
                return;

            ScrolllToElement(elementIndex);
        }

        public void ScrollOf(Vector3 offsetsToApply)
        {
            ScrollOf(offsetsToApply[(int)Orientation]);
        }

        /// <summary>
        /// Scroll of the provided offset from the current position in the direction given by the stack panel <see cref="Orientation"/> .
        /// </summary>
        /// <param name="offsetToApply">The value to scroll off</param>
        public void ScrollOf(float offsetToApply)
        {
            var axis = (int)Orientation;

            var absOffsetToApply = Math.Abs(offsetToApply);

            if (absOffsetToApply < MathUtil.ZeroTolerance)
                return;

            if (IsArrangeValid)
            {
                // perform the scrolling request is arrange (Viewport mainly) is still valid.
                var newElementIndex = (int)Math.Floor(scrollPosition);
                var currentPositionChildSize = GetSafeChildSize(newElementIndex, axis);
                var currentOffsetInChild = (scrollPosition - newElementIndex) * currentPositionChildSize;

                var scrollForward = offsetToApply > 0;
                var previousElementAccumulatedSize = scrollForward ? -currentOffsetInChild : currentOffsetInChild - currentPositionChildSize;
                var newElementSize = currentPositionChildSize;

                while (previousElementAccumulatedSize + newElementSize < absOffsetToApply && (scrollForward ? newElementIndex < Children.Count - 1 : newElementIndex > 0))
                {
                    newElementIndex += Math.Sign(offsetToApply);
                    previousElementAccumulatedSize += newElementSize;
                    newElementSize = GetSafeChildSize(newElementIndex, axis);
                }

                var offsetToApplyRemainder = absOffsetToApply - previousElementAccumulatedSize;
                var partialChildSize = scrollForward ? offsetToApplyRemainder : newElementSize - offsetToApplyRemainder;
                var newScrollPosition = newElementIndex + partialChildSize / newElementSize;

                AdjustOffsetsAndVisualChildren(newScrollPosition);
            }
            else
            {
                // delay the scrolling request to next draw when arrange info (mainly Viewport) will be valid again.
                InvalidateArrange(); // force next arrange so that requests can be processed.
                scrollingRequets.Add(new ScrollRequest { ScrollValue = offsetToApply, Type = ScrollRequestType.RelativePosition });
            }
        }

        public Vector3 ScrollBarPositions
        {
            get
            {
                var positionRatio = Vector3.Zero;
                var scrollAxis = (int)Orientation;

                if (Children.Count == 0)
                    return positionRatio;

                var extentMinusViewport = extent[scrollAxis] - Viewport[scrollAxis];
                if (extentMinusViewport <= MathUtil.ZeroTolerance)
                    return positionRatio;

                if (ItemVirtualizationEnabled)
                {
                    // determine the last scroll position reachable
                    var indexElement = Children.Count;
                    var accumulatedSize = 0f;
                    while (indexElement > 0 && accumulatedSize < Viewport[scrollAxis])
                    {
                        --indexElement;
                        accumulatedSize += GetSafeChildSize(indexElement, scrollAxis);
                    }
                    var maxScrollPosition = Math.Max(0, indexElement + (accumulatedSize - Viewport[scrollAxis]) / GetSafeChildSize(indexElement, scrollAxis));
                    positionRatio[scrollAxis] = scrollPosition / maxScrollPosition;
                }
                else
                {
                    var elementIndex = (int)Math.Floor(scrollPosition);
                    var elementRemainder = scrollPosition - elementIndex;

                    if (elementBounds.Count > elementIndex + 1)
                    {
                        var previousPosition = elementBounds[elementIndex];
                        var nextPosition = elementBounds[elementIndex + 1];
                        positionRatio[scrollAxis] = Math.Min(1, (previousPosition + elementRemainder * (nextPosition - previousPosition)) / extentMinusViewport);
                    }
                }

                return positionRatio;
            }
        }

        /// <summary>
        /// Scroll to the next element of the <see cref="StackPanel"/>.
        /// </summary>
        /// <remarks>Equivalent to <see cref="ScrollToNextLine(UI.Orientation)"/> called with the value <see cref="Orientation"/></remarks>
        public void ScrollToNextLine()
        {
            ScrolllToNeigbourElement(Orientation, 1);
        }

        /// <summary>
        /// Scroll to the previous element of the <see cref="StackPanel"/>.
        /// </summary>
        /// <remarks>Equivalent to <see cref="ScrollToPreviousLine(UI.Orientation)"/> called with the value <see cref="Orientation"/></remarks>
        public void ScrollToPreviousLine()
        {
            ScrolllToNeigbourElement(Orientation, -1);
        }

        public void ScrollToNextLine(Orientation direction)
        {
            ScrolllToNeigbourElement(direction, 1);
        }

        public void ScrollToPreviousLine(Orientation direction)
        {
            ScrolllToNeigbourElement(direction, -1);
        }

        private void ScrolllToNeigbourElement(Orientation direction, float side)
        {
            if (direction != Orientation)
                return;

            if (IsArrangeValid)
            {
                AdjustOffsetsAndVisualChildren((float)(side > 0 ? Math.Floor(scrollPosition + 1) : Math.Ceiling(scrollPosition - 1)));
            }
            else
            {
                InvalidateArrange(); // force next arrange so that requests can be processed.
                scrollingRequets.Add(new ScrollRequest { ScrollValue = side, Type = ScrollRequestType.RelativeElement });
            }
        }

        /// <summary>
        /// Scroll to the next page of elements of the <see cref="StackPanel"/>.
        /// </summary>
        /// <remarks>Equivalent to <see cref="ScrollToNextPage(UI.Orientation)"/> called with the value <see cref="Orientation"/></remarks>
        public void ScrollToNextPage()
        {
            ScrollPages(Orientation, 1);
        }

        /// <summary>
        /// Scroll to the previous page of elements of the <see cref="StackPanel"/>.
        /// </summary>
        /// <remarks>Equivalent to <see cref="ScrollToPreviousPage(UI.Orientation)"/> called with the value <see cref="Orientation"/></remarks>
        public void ScrollToPreviousPage()
        {
            ScrollPages(Orientation, -1);
        }

        public void ScrollToNextPage(Orientation direction)
        {
            ScrollPages(direction, 1);
        }

        public void ScrollToPreviousPage(Orientation direction)
        {
            ScrollPages(direction, -1);
        }

        /// <summary>
        /// Scroll to the beginning of the <see cref="StackPanel"/>.
        /// </summary>
        /// <remarks>Equivalent to <see cref="ScrollToBeginning(UI.Orientation)"/> called with the value <see cref="Orientation"/></remarks>
        public void ScrollToBeginning()
        {
            ScrolllToElement(Orientation, 0);
        }

        /// <summary>
        /// Scroll to the end of the <see cref="StackPanel"/>.
        /// </summary>
        /// <remarks>Equivalent to <see cref="ScrollToEnd(UI.Orientation)"/> called with the value <see cref="Orientation"/></remarks>
        public void ScrollToEnd()
        {
            ScrolllToElement(Orientation, int.MaxValue);
        }

        public void ScrollToBeginning(Orientation direction)
        {
            ScrolllToElement(direction, 0);
        }

        public void ScrollToEnd(Orientation direction)
        {
            ScrolllToElement(direction, int.MaxValue);
        }

        private void ScrollPages(Orientation direction, float numberOfPages)
        {
            if (direction != Orientation)
                return;

            ScrollOf(numberOfPages * Viewport[(int)Orientation]);
        }

        /// <summary>
        /// This function adjust the current first element index, the current offsets and the current visual children collection to have a valid stack display.
        /// </summary>
        private void AdjustOffsetsAndVisualChildren(float desiredNewScrollPosition)
        {
            offset = Vector3.Zero;
            var axis = (int)Orientation;

            if (ItemVirtualizationEnabled)
            {
                UpdateScrollPosition(desiredNewScrollPosition);
                UpdateAndArrangeVisibleChildren();
            }
            else // no item virtualization
            {
                if (elementBounds.Count < 2) // no children
                {
                    scrollPosition = 0;
                    offset = Vector3.Zero;
                }
                else
                {
                    var viewportSize = Viewport[axis];
                    var inferiorBound = elementBounds[indexElementMaxScrolling];
                    var superiorBound = elementBounds[indexElementMaxScrolling + 1];
                    var boundDifference = superiorBound - inferiorBound;

                    // calculate the maximum scroll position
                    float maxScrollPosition = indexElementMaxScrolling;
                    if (boundDifference > MathUtil.ZeroTolerance)
                        maxScrollPosition += Math.Min(1 - MathUtil.ZeroTolerance, (extent[axis] - viewportSize - inferiorBound) / boundDifference);

                    // set a valid scroll position
                    scrollPosition = Math.Max(0, Math.Min(maxScrollPosition, desiredNewScrollPosition));

                    // add the first element start bound as initial scroll offset
                    var firstElementIndex = (int)Math.Floor(scrollPosition);
                    offset[axis] = -elementBounds[firstElementIndex];

                    // update the visible element list for hit tests
                    visibleChildren.Clear();
                    for (var i = firstElementIndex; i < Children.Count; i++)
                    {
                        visibleChildren.Add(Children[i]);
                        if (elementBounds[i + 1] - elementBounds[firstElementIndex + 1] > viewportSize)
                            break;
                    }
                }
            }

            // adjust the offset of the children
            var scrollPositionIndex = (int)Math.Floor(scrollPosition);
            var scrollPositionRemainder = scrollPosition - scrollPositionIndex;
            offset[axis] -= scrollPositionRemainder * GetSafeChildSize(scrollPositionIndex, axis);

            // force the scroll owner to update the scroll info
            ScrollOwner?.InvalidateScrollInfo();
        }

        private void UpdateAndArrangeVisibleChildren()
        {
            var axis = (int)Orientation;

            // cache the initial list of children
            cachedVisibleChildren.Clear();
            foreach (var child in visibleChildren)
                cachedVisibleChildren.Add(child);

            // reset the list
            visibleChildren.Clear();

            // remove all the current visual children 
            while (VisualChildrenCollection.Count > 0)
                SetVisualParent(VisualChildrenCollection[0], null);

            // determine first element index and size
            var elementIndex = (int)Math.Floor(scrollPosition);
            var firstChildSize = GetSafeChildSize(elementIndex, axis);

            // create the next visual children collection to display
            var currentSize = -(scrollPosition - elementIndex) * firstChildSize;
            while (elementIndex < Children.Count && currentSize <= Viewport[axis])
            {
                currentSize += GetSafeChildSize(elementIndex, axis);

                var child = Children[elementIndex];
                visibleChildren.Add(child);
                SetVisualParent(child, this);
                ++elementIndex;
            }

            // reorder visual children by z-Order
            VisualChildrenCollection.Sort(PanelChildrenSorter);

            // re-arrange the children if they changed
            if (visibleChildren.Count > 0)
            {
                var shouldRearrangeChildren = cachedVisibleChildren.Count == 0 || cachedVisibleChildren.Count != visibleChildren.Count;

                // determine if the two list are equals
                if (!shouldRearrangeChildren)
                {
                    // ReSharper disable once LoopCanBeConvertedToQuery
                    for (var i = 0; i < visibleChildren.Count; i++)
                    {
                        if (cachedVisibleChildren[i] != visibleChildren[i])
                        {
                            shouldRearrangeChildren = true;
                            break;
                        }
                    }
                }
                if (shouldRearrangeChildren)
                    ArrangeChildren();
            }
        }

        private void UpdateScrollPosition(float newScrollPosition)
        {
            var axis = (int)Orientation;
            var viewportSize = Viewport[axis];

            // determine a valid scroll position
            var validNextScrollPosition = Math.Max(0, Math.Min(Children.Count - MathUtil.ZeroTolerance, newScrollPosition));
            var firstElementIndex = (int)Math.Floor(validNextScrollPosition);
            var startOffset = (validNextScrollPosition - firstElementIndex) * GetSafeChildSize(firstElementIndex, axis);
            var currentSize = -startOffset;

            // check if there are enough element after to fill the viewport
            var currentElementIndex = firstElementIndex;
            while (currentElementIndex < Children.Count && currentSize < viewportSize)
            {
                currentSize += GetSafeChildSize(currentElementIndex, axis);
                ++currentElementIndex;
            }

            // move the valid scroll position backward if not event space to fill the viewport
            if (currentSize < viewportSize)
            {
                currentSize += startOffset - GetSafeChildSize(firstElementIndex, axis); // remove partial size of first element
                while (firstElementIndex >= 0)
                {
                    var elementSize = GetSafeChildSize(firstElementIndex, axis);
                    currentSize += elementSize;

                    if (currentSize >= viewportSize)
                        break;

                    --firstElementIndex;
                }

                if (firstElementIndex < 0) // all the elements of the stack panel together are smaller than the viewport.
                {
                    validNextScrollPosition = 0;
                }
                else
                {
                    var firstElementSize = GetSafeChildSize(firstElementIndex, axis);
                    validNextScrollPosition = firstElementIndex + (currentSize - viewportSize) / firstElementSize;
                }
            }

            // update the current scroll position
            scrollPosition = validNextScrollPosition;
        }

        private float GetSafeChildSize(int childIndex, int dimension)
        {
            if (childIndex >= Children.Count)
                return 0;

            var child = Children[childIndex];

            child.LayoutingContext = LayoutingContext;

            if (child.IsCollapsed)
                return 0f;

            if (!child.IsMeasureValid)
            {
                var childProvidedSize = Viewport;

                if (ScrollOwner != null)
                {
                    foreach (var i in ScrollingModeToInfiniteAxis[(int)ScrollOwner.ScrollMode])
                        childProvidedSize[i] = float.PositiveInfinity;
                }

                child.Measure(childProvidedSize);
            }

            if (!child.IsArrangeValid)
            {
                var childProvidedSize = Viewport;
                childProvidedSize[(int)Orientation] = child.DesiredSizeWithMargins[(int)Orientation];

                child.Arrange(childProvidedSize, Parent != null && Parent.IsCollapsed);
            }

            return child.RenderSize[dimension] + child.Margin[dimension] + child.Margin[dimension + 3];
        }

        protected internal override FastCollection<UIElement> HitableChildren => visibleChildren;
    }
}
