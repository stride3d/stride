// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Core.Diagnostics;
using Xenko.Core.Mathematics;

namespace Xenko.UI.Panels
{
    /// <summary>
    /// Represents a grid control with adjustable columns, rows and layers.
    /// </summary>
    [DataContract(nameof(Grid))]
    [DebuggerDisplay("Grid - Name={Name}")]
    public class Grid : GridBase
    {
        private readonly Logger logger = GlobalLogger.GetLogger("UI");

        private readonly StripDefinitionCollection[] stripDefinitions = new StripDefinitionCollection[3];

        /// <summary>
        /// For each dimension and index of strip, return the list of UIElement that are contained only in auto-sized strips
        /// </summary>
        private readonly List<List<UIElement>>[] stripIndexToNoStarElements = 
            {
                new List<List<UIElement>>(),
                new List<List<UIElement>>(),
                new List<List<UIElement>>()
            };

        /// <summary>
        /// For each dimension and UIelement, returns the list of all the strip definition it is contained in ordered by increasing strip index
        /// </summary>
        private readonly Dictionary<UIElement, List<StripDefinition>>[] elementToStripDefinitions = 
            {
                new Dictionary<UIElement, List<StripDefinition>>(),
                new Dictionary<UIElement, List<StripDefinition>>(),
                new Dictionary<UIElement, List<StripDefinition>>()
            };    
        
        /// <summary>
        /// For each dimension and UIelement that is partially contained in star-sized strip, returns the list of all the strip definition it is contained in
        /// </summary>
        private readonly Dictionary<UIElement, List<StripDefinition>>[] partialStarElementToStripDefinitions = 
            {
                new Dictionary<UIElement, List<StripDefinition>>(),
                new Dictionary<UIElement, List<StripDefinition>>(),
                new Dictionary<UIElement, List<StripDefinition>>()
            };

        /// <summary>
        /// For each dimension and strip index, return the starting position of the strip.
        /// </summary>
        private readonly List<float>[] cachedStripIndexToStripPosition = 
            {
                new List<float>(),
                new List<float>(),
                new List<float>()
            };

        /// <summary>
        /// For each dimension, the list of the star definitions for the current dimension iteration (Ox, Oy or Oz).
        /// </summary>
        /// <remarks> This variable is declared as a field to avoid reallocations at each frame</remarks>
        private readonly List<StripDefinition>[] dimToStarDefinitions = 
            {
                new List<StripDefinition>(),
                new List<StripDefinition>(),
                new List<StripDefinition>()
            };

        /// <summary>
        /// A list use to make a copy of the star definition and then make a modification on this list.
        /// </summary>
        private readonly List<StripDefinition> starDefinitionsCopy = new List<StripDefinition>();

        /// <summary>
        /// The list of the star definitions of an element sorted by increasing minimum size wrt their star value
        /// </summary>
        /// <remarks> This variable is declared as a field to avoid reallocations at each frame</remarks>
        private readonly List<StripDefinition> minSortedStarDefinitions = new List<StripDefinition>();

        /// <summary>
        /// The list of the star definitions of an element sorted by increasing maximum size wrt their star value
        /// </summary>
        /// <remarks> This variable is declared as a field to avoid reallocations at each frame</remarks>
        private readonly List<StripDefinition> maxSortedStarDefinitions = new List<StripDefinition>();

        /// <summary>
        /// The list of the star definitions that were bounded by their maximum values in the current iteration step.
        /// </summary>
        /// <remarks> This variable is declared as a field to avoid reallocations at each frame</remarks>
        private readonly List<StripDefinition> maxBoundedStarDefinitions = new List<StripDefinition>();

        /// <summary>
        /// The list of the star definitions that were bounded by their minimum values in the current iteration step.
        /// </summary>
        /// <remarks> This variable is declared as a field to avoid reallocations at each frame</remarks>
        private readonly List<StripDefinition> minBoundedStarDefinitions = new List<StripDefinition>();

        /// <summary>
        /// The list of all elements contained in at least on auto-sized strip.
        /// </summary>
        private readonly HashSet<UIElement> autoDefinedElements = new HashSet<UIElement>();

        private readonly IComparer<StripDefinition> sortByIncreasingMaximumComparer = new StripDefinition.SortByIncreasingStarRelativeMaximumValues();
        private readonly IComparer<StripDefinition> sortByIncreasingMinimumComparer = new StripDefinition.SortByIncreasingStarRelativeMinimumValues();

        public Grid()
        {
            RowDefinitions.CollectionChanged += DefinitionCollectionChanged;
            ColumnDefinitions.CollectionChanged += DefinitionCollectionChanged;
            LayerDefinitions.CollectionChanged += DefinitionCollectionChanged;
        }

        private void DefinitionCollectionChanged(object sender, TrackingCollectionChangedEventArgs trackingCollectionChangedEventArgs)
        {
            var modifiedElement = (StripDefinition)trackingCollectionChangedEventArgs.Item;
            switch (trackingCollectionChangedEventArgs.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    modifiedElement.DefinitionChanged += OnStripDefinitionChanged;
                    break;
                case NotifyCollectionChangedAction.Remove:
                    modifiedElement.DefinitionChanged -= OnStripDefinitionChanged;
                    break;
                default:
                    throw new NotSupportedException();
            }
            InvalidateMeasure();
        }

        private void OnStripDefinitionChanged(object sender, EventArgs eventArgs)
        {
            InvalidateMeasure();
        }

        /// <summary>
        /// The actual definitions of the grid rows.
        /// </summary>
        /// <remarks>A grid always has at least one default row definition, even when <see cref="RowDefinitions"/> is empty.</remarks>
        [DataMemberIgnore]
        public StripDefinitionCollection ActualRowDefinitions  => stripDefinitions[1];

        /// <summary>
        /// The actual definitions of the grid columns.
        /// </summary>
        /// <remarks>A grid always has at least one default row definition, even when <see cref="ColumnDefinitions"/> is empty.</remarks>
        [DataMemberIgnore]
        public StripDefinitionCollection ActualColumnDefinitions => stripDefinitions[0];

        /// <summary>
        /// The actual definitions of the grid layers.
        /// </summary>
        /// <remarks>A grid always has at least one default row definition, even when <see cref="LayerDefinitions"/> is empty.</remarks>
        [DataMemberIgnore]
        public StripDefinitionCollection ActualLayerDefinitions => stripDefinitions[2];

        /// <summary>
        /// The definitions of the grid rows.
        /// </summary>
        /// <userdoc>The definitions of the grid rows.</userdoc>
        [DataMember]
        [Display(category: LayoutCategory)]
        public StripDefinitionCollection RowDefinitions { get; } = new StripDefinitionCollection();

        /// <summary>
        /// The definitions of the grid columns.
        /// </summary>
        /// <userdoc>The definitions of the grid columns.</userdoc>
        [DataMember]
        [Display(category: LayoutCategory)]
        public StripDefinitionCollection ColumnDefinitions { get; } = new StripDefinitionCollection();

        /// <summary>
        /// The definitions of the grid layers.
        /// </summary>
        /// <userdoc>The definitions of the grid layers.</userdoc>
        [DataMember]
        [Display(category: LayoutCategory)]
        public StripDefinitionCollection LayerDefinitions { get; } = new StripDefinitionCollection();

        protected override Vector3 MeasureOverride(Vector3 availableSizeWithoutMargins)
        {
            // This function is composed of 6 main parts:
            // 1. Add default strip definition to ensure that all elements are in the grid
            // 2. Rebuild the needed cache data to perform future calculations
            // 3. Measure all children that are not contained in star-sized strips with the best estimation possible.
            // 4. Determine the size of the auto-size strips.
            // 5. Determine the size of 1-star strip.
            // 6. Re-measure all children, this time with the exact strip size value.
            // 7. Calculate size required by the grid to contain all its children.

            // 1. Ensure that all child UI element are completely inside the grid by adding strip definitions
            CheckChildrenPositionsAndAdjustGridSize();

            // 2. Update the autoStripNumberToElements cache data structure for the next Measure and Arrange sequence
            RebuildMeasureCacheData();
            
            // 3. Measure all children that are contained in a least one auto-strip with the best estimation possible of the strips final size.
            // Note that only an estimation of the final strip size can be used at this point, since the final sizes can only be determined once all auto-children have been measured.
            //
            // We chose a kind of gross but simple/efficient algorithm. It works as follows:
            // - Initialize the strip sizes with the strip minimum size for star/auto strips and exact final size for fixed strips.
            // - Remove the minimal strip size as well as the fixed strip size.
            // - Use this same remaining size to measure all the auto elements.
            // This algorithm put all the auto elements on an equal footing, but propose more space to the children that the grid really have.
            //
            // For a better estimation of the size really available to the children, the following algorithm would be possible,
            // but it is much more complex and heavy not only in term of CPU usage but also in memory (cached data):
            // - Initialize the strip sizes with the strip minimum size for star/auto strips and exact final size for fixed strips.
            // - Measure elements by iterating on columns (left to right) then rows (top to bottom) and finally layers (back to front)
            // - Estimate the measure size by removing from the available size the size of all previous strips actual size (that is current size estimation).
            // - When going to the next strip iteration, refine the previous strip estimated size (ActualSize) by taking the max sized needed among all element ending in this strip. 

            // Initialize strip actual size with minimal values
            foreach (var definitions in stripDefinitions)
                InitializeStripDefinitionActualSize(definitions);

            // calculate size available for all auto elements.
            var autoElementAvailableSize = availableSizeWithoutMargins;
            for (var dim = 0; dim < 3; dim++)
            {
                foreach (var definition in stripDefinitions[dim])
                {
                    autoElementAvailableSize[dim] -= definition.Type == StripType.Fixed ? definition.ActualSize : definition.MinimumSize;
                }
            }

            // measure all the children
            foreach (var child in autoDefinedElements)
            {
                var childAvailableSize = Vector3.Zero;
                for (var dim = 0; dim < 3; dim++)
                {
                    var autoAvailableWithMin = autoElementAvailableSize[dim];
                    foreach (var definition in elementToStripDefinitions[dim][child])
                    {
                        autoAvailableWithMin += definition.Type == StripType.Fixed ? definition.ActualSize: definition.MinimumSize;
                        if (definition.Type == StripType.Fixed)
                            childAvailableSize[dim] += definition.ClampSizeByMinimumMaximum(definition.SizeValue);
                        else
                            childAvailableSize[dim] = Math.Min(autoAvailableWithMin, childAvailableSize[dim] + definition.MaximumSize);
                    }
                }
                child.Measure(childAvailableSize);
            }

            // 4. Determine the size of the auto-sized strips
            // -> Our algorithm here tries to minimize as long as possible the size of the Auto-strips during the iteration.
            //    By doing so, we postpone the increase in size of shared Auto-strips to the last strip shared.
            //    This method is way much easier than spreading space equally between all the shared auto-sized strips.
            //
            //   |<-    auto   ->|<-    auto   ->|
            //   _________________________________
            //   |element1       |element2       |  <-- spreads space equally between auto-sized strips -- very difficult (even WPF implementation is buggy and not optimal)
            //   |-----element3-with-span-of-2---|
            //
            //   |<-auto->|<-        auto      ->|
            //   _________________________________
            //   |element1|element2              |  <-- our algorithm always minimize auto-sized strips as long as possible -- simple and optimal
            //   |-----element3-with-span-of-2---|  
            //
            // -> There is an issue with elements contained both in auto and star strip definitions.
            //    Intuitively, we expect that those elements receive enough space to layout and that this space is perfectly divided into the auto / star strips.
            //    The problem is that it is not possible to determine the size of star strips as long as all auto strip size as not been determined,
            //    and that it is not possible determine missing space to include into the auto-sized strips for those elements as long as we don't know the size of star-sized strips.
            //    We are in a dead-end. There is basically two solutions: 
            //       1. Include all the missing size for those element into the auto strips
            //       2. Include none of the missing size into the auto strips and hope that the star strips will be big enough to contain those elements.
            //    Here we chose option (2), that is we ignore those elements during calculation of auto-sized strips.
            //    The reason between this choice is that (1) will tend to increase excessively the size of auto-sized strips (for nothing).
            //    Moreover, we consider most of the time elements included both auto and star-size strips are more elements that we want
            //    to be spread along several strips rather than elements that we want auto-sized.
            for (var dim = 0; dim < 3; dim++)
            {
                var definitions = stripDefinitions[dim];

                // reset the estimated size of the auto-sized strip calculated before.
                InitializeStripDefinitionActualSize(definitions);

                for (var index = 0; index < definitions.Count; index++)
                {
                    var currentDefinition = definitions[index];
                    if (currentDefinition.Type != StripType.Auto) // we are interested only in auto-sized strip here
                        continue;

                    // for each strip iterate all the elements (with no star definition) to determine the biggest space needed.
                    foreach (var element in stripIndexToNoStarElements[dim][index])
                    {
                        var currentDefinitionIndex = 0; // the index of 'currentDefinition' in 'elementToStripDefinitions[dim][element]'
                        var elementStripDefinitions = elementToStripDefinitions[dim][element];

                        // first determine the total space still needed for the element
                        var spaceAvailable = 0f;
                        for (var i = 0; i < elementStripDefinitions.Count; i++)
                        {
                            spaceAvailable += elementStripDefinitions[i].ActualSize;

                            if (elementStripDefinitions[i] == currentDefinition)
                                currentDefinitionIndex = i;
                        }
                        var spaceNeeded = Math.Max(0, element.DesiredSizeWithMargins[dim] - spaceAvailable);

                        // if no space is needed, go check the next element
                        if (spaceNeeded <= 0)
                            continue;

                        // look if the space needed can be found in next strip definitions
                        for (var i = currentDefinitionIndex + 1; i < elementStripDefinitions.Count; i++)
                        {
                            var def = elementStripDefinitions[i];

                            if (def.Type == StripType.Auto)
                                spaceNeeded = Math.Max(0, spaceNeeded - (def.MaximumSize - def.ActualSize));

                            if (spaceNeeded <= 0) // if no space is needed anymore, there is no need to continue the process
                                break;
                        }
                        // increase the strip size by the needed space
                        currentDefinition.ActualSize = currentDefinition.ClampSizeByMinimumMaximum(currentDefinition.ActualSize + spaceNeeded);
                    }
                }
            }

            // 5. Calculate the actual size of 1-star strip. 
            CalculateStarStripSize(availableSizeWithoutMargins);

            // 6. Re-measure all the children, this time with the exact available size.
            foreach (var child in VisualChildrenCollection)
            {
                var availableToChildWithMargin = Vector3.Zero;
                for (var dim = 0; dim < 3; dim++)
                    availableToChildWithMargin[dim] = SumStripCurrentSize(elementToStripDefinitions[dim][child]);

                child.Measure(availableToChildWithMargin);
            }

            // 7. Calculate the size needed by the grid in order to be able to contain all its children.
            // This consist at finding the size of 1-star strip so that elements enter into the star-size strips.
            //
            // For each grid dimension:
            // -> calculate the size required by 1-star
            // -> update the actual size of the star-sized elements
            // -> calculate the size needed by the grid
            //
            var neededSize = Vector3.Zero;
            for (var dim = 0; dim < 3; dim++)
            {
                var definitions = stripDefinitions[dim];

                // Determine the size needed by 1-star so that all the elements can enter the grid.
                // The task is greatly complicated by the existence of minimum and maximum size for the strips. 
                var oneStarSize = 0f;
                foreach (var element in partialStarElementToStripDefinitions[dim].Keys)
                {
                    var elementDefinitions = partialStarElementToStripDefinitions[dim][element];

                    // clear previous cached values
                    minSortedStarDefinitions.Clear();
                    maxSortedStarDefinitions.Clear();

                    // calculate the space missing for the element
                    var availableSpace = 0f;
                    foreach (var def in elementDefinitions)
                    {
                        if (def.Type == StripType.Star)
                        {
                            def.ActualSize = def.MinimumSize;
                            minSortedStarDefinitions.Add(def);
                        }
                        availableSpace += def.ActualSize;
                    }
                    var currentNeededSpace = Math.Max(0, element.DesiredSizeWithMargins[dim] - availableSpace);

                    // sort the star definition by increasing relative minimum and maximum values
                    minSortedStarDefinitions.Sort(sortByIncreasingMinimumComparer);

                    // calculate the size needed for 1-star for this element
                    // -> starting with the element with the smallest relative minimum size,
                    //    we progressively increase the star strip size until reaching the needed size
                    var neededOneStarSize = 0f;
                    for (var minIndex = 0; minIndex < minSortedStarDefinitions.Count && currentNeededSpace > 0; ++minIndex)
                    {
                        var minDefinition = minSortedStarDefinitions[minIndex];

                        maxSortedStarDefinitions.Add(minDefinition);  // add current definition to the list of definition that can have their size increased
                        maxSortedStarDefinitions.Sort(sortByIncreasingMaximumComparer);

                        var nextDefinitionRelativeMinSize = (minIndex == minSortedStarDefinitions.Count - 1) ? float.PositiveInfinity : minSortedStarDefinitions[minIndex + 1].ValueRelativeMinimum();
                        var minNextRelativeStepSizeIncrease = Math.Min(currentNeededSpace / SumValues(maxSortedStarDefinitions), nextDefinitionRelativeMinSize - minDefinition.ValueRelativeMinimum());

                        while (minNextRelativeStepSizeIncrease > 0 && maxSortedStarDefinitions.Count > 0)
                        {
                            var maxDefinition = maxSortedStarDefinitions[0];
                            var maxNextStepSizeIncrease = maxDefinition.SizeValue * minNextRelativeStepSizeIncrease;
                            var maxNextStepRelativeSizeIncreate = Math.Min(minNextRelativeStepSizeIncrease, (maxDefinition.MaximumSize - maxDefinition.ActualSize) / maxDefinition.SizeValue);

                            // remove the size of the max increase from the min target increase
                            minNextRelativeStepSizeIncrease -= maxNextStepRelativeSizeIncreate;

                            // determine if the current element has reached its maximum size
                            var maxDefinitionReachedItsMax = maxDefinition.ActualSize + maxNextStepSizeIncrease >= maxDefinition.MaximumSize;

                            // update the actual size of all the max definitions and reduce the needed size accordingly
                            foreach (var definition in maxSortedStarDefinitions)
                            {
                                var absoluteIncrease = maxNextStepRelativeSizeIncreate * definition.SizeValue;
                                currentNeededSpace -= absoluteIncrease;
                                definition.ActualSize += absoluteIncrease;
                            }

                            // if the element has reached its maximum -> we remove it from the list for the next iteration
                            if (maxDefinitionReachedItsMax)
                            {
                                var minNextStepSizeIncrease = minNextRelativeStepSizeIncrease * SumValues(maxSortedStarDefinitions);
                                maxSortedStarDefinitions.Remove(maxDefinition);
                                minNextRelativeStepSizeIncrease = minNextStepSizeIncrease / SumValues(maxSortedStarDefinitions);
                            }

                            // update the size needed for one star
                            neededOneStarSize = Math.Max(neededOneStarSize, maxDefinition.ActualSize / maxDefinition.SizeValue);
                        }
                    }

                    // update the grid dimension-global 1-star size
                    oneStarSize = Math.Max(oneStarSize, neededOneStarSize);
                }
                
                // Update all the star strip size
                foreach (var starDefinition in dimToStarDefinitions[dim])
                    starDefinition.ActualSize = starDefinition.ClampSizeByMinimumMaximum(oneStarSize * starDefinition.SizeValue);

                // determine to size needed by the grid
                neededSize[dim] += SumStripCurrentSize(definitions);
            }

            return neededSize;
        }

        /// <summary>
        /// Set the size of all the fix-sized strips, and initialize the size of auto/star-sized strips to their minimal size.
        /// </summary>
        /// <param name="definitions">The strip definitions</param>
        private static void InitializeStripDefinitionActualSize(StripDefinitionCollection definitions)
        {
            foreach (var definition in definitions)
            {
                var stripSize = 0f;

                if (definition.Type == StripType.Fixed)
                    stripSize = definition.SizeValue;

                definition.ActualSize = definition.ClampSizeByMinimumMaximum(stripSize);
            }
        }

        protected override Vector3 ArrangeOverride(Vector3 finalSizeWithoutMargins)
        {
            // determine the size of the star strips now that we have the final available size.
            CalculateStarStripSize(finalSizeWithoutMargins);

            // Update strip starting position cache data
            RebuildStripPositionCacheData();

            // calculate the final size of the grid.
            var gridFinalSize = Vector3.Zero;
            for (var dim = 0; dim < 3; dim++)
                gridFinalSize[dim] = Math.Max(cachedStripIndexToStripPosition[dim][stripDefinitions[dim].Count], finalSizeWithoutMargins[dim]);

            // arrange the children
            foreach (var child in VisualChildrenCollection)
            {
                // calculate child position
                var gridPosition = GetElementGridPositions(child);
                var position = new Vector3(
                    cachedStripIndexToStripPosition[0][gridPosition.X],
                    cachedStripIndexToStripPosition[1][gridPosition.Y],
                    cachedStripIndexToStripPosition[2][gridPosition.Z]);

                // set the arrange matrix values
                child.DependencyProperties.Set(PanelArrangeMatrixPropertyKey, Matrix.Translation(position - gridFinalSize / 2));

                // calculate the size provided to the child
                var providedSize = new Vector3(
                    SumStripCurrentSize(elementToStripDefinitions[0][child]),
                    SumStripCurrentSize(elementToStripDefinitions[1][child]),
                    SumStripCurrentSize(elementToStripDefinitions[2][child]));

                // arrange the child
                child.Arrange(providedSize, IsCollapsed);
            }

            return gridFinalSize;
        }

        private void CalculateStarStripSize(Vector3 finalSizeWithoutMargins)
        {
            // calculate the ActualSize of the start-sized strips. Possible minimum and maximum values have to be taken in account for that calculation.
            for (var dim = 0; dim < 3; dim++)
            {
                starDefinitionsCopy.Clear();
                starDefinitionsCopy.AddRange(dimToStarDefinitions[dim]);

                // compute the size taken by fixed and auto strips
                var spaceTakenByFixedAndAutoStrips = SumStripAutoAndFixedSize(stripDefinitions[dim]);

                // calculate the size remaining for the start-sized strips
                var spaceRemainingForStarStrips = Math.Max(0f, finalSizeWithoutMargins[dim] - spaceTakenByFixedAndAutoStrips);

                // calculate the total value of the stars.
                var starValuesSum = SumValues(starDefinitionsCopy);

                // calculate the space dedicated to one star
                var oneStarSpace = spaceRemainingForStarStrips / starValuesSum;

                // At this point we have the size of a 1-star-sized strip if none of strips are saturated by their min or max size values.
                // In following loop we progressively refine the value until reaching the final value for 1-star-sized strip.
                // Our algorithm works as follow:
                //   1. Finding saturated strips (by min or max)
                //   2. Determine if size taken by star-sized strip will increase or decrease due to saturated strips.
                //   3. Updating the total size dedicated of star-sized strips by removing the size taken by min (resp. max) saturated strips
                //   4. Updating the total remaining star value by removing the star-values of min (resp. max) saturated strips
                //   5. Updating size of 1-star-sized strip.
                //   6. Removing from the star-sized strip list the min (resp. max) saturated strips.
                //   7. As new strips can now reach min (resp. max) saturation with the decreased (resp. increase) of the 1-star-sized strip size, 
                //      repeat the process until none of the remaining strips are saturated anymore.
                //
                // Note that termination is ensured by the fact the set of star-sized to measure strictly decrease at each iteration.
                do
                {
                    maxBoundedStarDefinitions.Clear();
                    minBoundedStarDefinitions.Clear();

                    // find the min/max saturated strips.
                    foreach (var definition in starDefinitionsCopy)
                    {
                        definition.ActualSize = definition.SizeValue * oneStarSpace;
                        if (definition.ActualSize < definition.MinimumSize)
                        {
                            definition.ActualSize = definition.MinimumSize;
                            minBoundedStarDefinitions.Add(definition);
                        }
                        else if (definition.ActualSize > definition.MaximumSize)
                        {
                            definition.ActualSize = definition.MaximumSize;
                            maxBoundedStarDefinitions.Add(definition);
                        }
                    }

                    // re-calculate the size taken by star-sized strips (taking into account saturated strips)
                    var resultingSize = SumStripCurrentSize(starDefinitionsCopy);

                    // determine if we have to trim max or min saturated star strips
                    var strimList = resultingSize < spaceRemainingForStarStrips ? maxBoundedStarDefinitions : minBoundedStarDefinitions;

                    // update the size of 1-star-strip
                    spaceRemainingForStarStrips -= SumStripCurrentSize(strimList);
                    starValuesSum -= SumValues(strimList);
                    oneStarSpace = spaceRemainingForStarStrips / starValuesSum;

                    // remove definitions of star strip that will remain saturated until the end of the process from the to-measure list
                    foreach (var definition in strimList)
                        starDefinitionsCopy.Remove(definition);
                }
                    // stops the process if either there is no saturated strip or no star-sized strip to measure anymore.
                while ((maxBoundedStarDefinitions.Count != 0 || minBoundedStarDefinitions.Count != 0) && starDefinitionsCopy.Count != 0);
            }
        }

        protected override void OnLogicalChildRemoved(UIElement oldElement, int index)
        {
            base.OnLogicalChildRemoved(oldElement, index);

            for (var dim = 0; dim < 3; dim++)
            {
                // remove the strip definitions associated to the removed child
                elementToStripDefinitions[dim].Remove(oldElement);

                // remove the strip definitions associated to the removed child
                partialStarElementToStripDefinitions[dim].Remove(oldElement);

                autoDefinedElements.Remove(oldElement);
            }
        }

        protected override void OnLogicalChildAdded(UIElement newElement, int index)
        {
            base.OnLogicalChildAdded(newElement, index);

            for (var dim = 0; dim < 3; dim++)
            {
                // ensure that all children have a associate list strip definitions
                elementToStripDefinitions[dim][newElement] = new List<StripDefinition>();

                // ensure that all children have a associate list strip definitions
                partialStarElementToStripDefinitions[dim][newElement] = new List<StripDefinition>();
            }
        }

        private void RebuildMeasureCacheData()
        {
            // clear existing cache data
            for (var dim = 0; dim < 3; dim++)
            {
                // the 'stripIndexToNoStarElements' entries
                for (var index = 0; index < stripDefinitions[dim].Count; ++index)
                {
                    if (stripIndexToNoStarElements[dim].Count <= index)
                        stripIndexToNoStarElements[dim].Add(new List<UIElement>());

                    stripIndexToNoStarElements[dim][index].Clear();
                }

                // the 'elementToStripDefinitions' entries
                foreach (var list in elementToStripDefinitions[dim].Values)
                    list.Clear();
                
                // the 'partialStarElementToStripDefinitions' entries
                foreach (var list in partialStarElementToStripDefinitions[dim].Values)
                    list.Clear();
            }
            autoDefinedElements.Clear();

            // build 'elementToStripDefinitions', stripIndexToNoStarElements', 'partialStarElementToStripDefinitions' and 'autoDefinedElements'
            foreach (var child in VisualChildrenCollection)
            {
                var childPosition = GetElementGridPositions(child);
                var childSpan = GetElementSpanValues(child);

                for (var dim = 0; dim < 3; ++dim)
                {
                    var childHasNoStarDefinitions = true;

                    // fill 'elementToStripDefinitions'
                    for (var i = childPosition[dim]; i < childPosition[dim] + childSpan[dim]; i++)
                    {
                        if (stripDefinitions[dim][i].Type == StripType.Star)
                            childHasNoStarDefinitions = false;
                        else if (stripDefinitions[dim][i].Type == StripType.Auto)
                            autoDefinedElements.Add(child);

                        elementToStripDefinitions[dim][child].Add(stripDefinitions[dim][i]);
                    }

                    // fill 'stripIndexToNoStarElements' and 'partialStarElementToStripDefinitions'
                    if (childHasNoStarDefinitions)
                    {
                        for (var i = childPosition[dim]; i < childPosition[dim] + childSpan[dim]; i++)
                            stripIndexToNoStarElements[dim][i].Add(child);
                    }
                    else
                    {
                        for (var i = childPosition[dim]; i < childPosition[dim] + childSpan[dim]; i++)
                            partialStarElementToStripDefinitions[dim][child].Add(stripDefinitions[dim][i]);
                    }
                }
            }
            
            // build the star definitions cache
            for (var dim = 0; dim < 3; ++dim)
            {
                dimToStarDefinitions[dim].Clear();
                foreach (var definition in stripDefinitions[dim])
                    if (definition.Type == StripType.Star)
                        dimToStarDefinitions[dim].Add(definition);
            }
        }

        private void CheckChildrenPositionsAndAdjustGridSize()
        {
            // Setup strips (use a default entry if nothing is set)
            CreateDefaultStripIfNecessary(ref stripDefinitions[0], ColumnDefinitions);
            CreateDefaultStripIfNecessary(ref stripDefinitions[1], RowDefinitions);
            CreateDefaultStripIfNecessary(ref stripDefinitions[2], LayerDefinitions);

            // add default strip definitions as long as one element is partially outside of the grid
            foreach (var child in VisualChildrenCollection)
            {
                var childLastStripPlusOne = GetElementGridPositions(child) + GetElementSpanValues(child);
                for (var dim = 0; dim < 3; dim++)
                {
                    // TODO: We should reassign everything outside to last row or 0?
                    if (stripDefinitions[dim].Count < childLastStripPlusOne[dim])
                        logger.Warning($"Element 'Name={child}' is outside of the grid 'Name={Name}' definition for [{(dim == 0 ? "Column" : dim == 1 ? "Row" : "Layer")}].");
                }
            }
        }

        private static void CreateDefaultStripIfNecessary(ref StripDefinitionCollection computedCollection, StripDefinitionCollection userCollection)
        {
            if (userCollection.Count > 0)
            {
                computedCollection = userCollection;
            }
            else if (computedCollection == userCollection || computedCollection == null)
            {
                // Need to create default collection (it will be kept until user one is not empty again)
                computedCollection = new StripDefinitionCollection { new StripDefinition() };
            }
        }

        private void RebuildStripPositionCacheData()
        {
            // rebuild strip begin position cached data 
            for (var dim = 0; dim < 3; dim++)
            {
                //clear last cached data
                cachedStripIndexToStripPosition[dim].Clear();

                // calculate the strip start position
                var startPosition = 0f;
                for (var index = 0; index < stripDefinitions[dim].Count; index++)
                {
                    cachedStripIndexToStripPosition[dim].Add(startPosition);
                    startPosition += stripDefinitions[dim][index].ActualSize;
                }
                cachedStripIndexToStripPosition[dim].Add(startPosition);
            }
        }

        private static float SumStripCurrentSize(StripDefinitionCollection definitions)
        {
            var sum = 0f;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var def in definitions) // do not use linq to avoid allocations
                sum += def.ActualSize;

            return sum;
        }
        
        private static float SumStripCurrentSize(List<StripDefinition> definitions)
        {
            var sum = 0f;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var def in definitions) // do not use linq to avoid allocations
                sum += def.ActualSize;

            return sum;
        }

        private static float SumStripAutoAndFixedSize(StripDefinitionCollection definitions)
        {
            var sum = 0f;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var def in definitions) // do not use linq to avoid allocations
                if (def.Type != StripType.Star)
                    sum += def.ActualSize;

            return sum;
        }

        private static float SumValues(List<StripDefinition> definitions) // use List instead of IEnumerable in  order to avoid boxing in "foreach"
        {
            var sum = 0f;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var def in definitions) // do not use linq to avoid allocations
                sum += def.SizeValue;

            return sum;
        }

        private static void GetDistanceToSurroundingAnchors(List<float> stripPosition, float position, out Vector2 distances)
        {
            if (stripPosition.Count < 2)
            {
                distances = Vector2.Zero;
                return;
            }

            var validPosition = Math.Max(0, Math.Min(position, stripPosition[stripPosition.Count-1]));

            var index = 1;
            while (index < stripPosition.Count-1 && stripPosition[index] <= validPosition)
                ++index;

            distances = new Vector2(stripPosition[index - 1], stripPosition[index]);
            distances -= new Vector2(validPosition, validPosition);
        }

        public override Vector2 GetSurroudingAnchorDistances(Orientation direction, float position)
        {
            Vector2 distances;

            GetDistanceToSurroundingAnchors(cachedStripIndexToStripPosition[(int)direction], position, out distances);

            return distances;
        }

        protected override Int3 GetElementGridPositions(UIElement element)
        {
            var position = base.GetElementGridPositions(element);
            return Int3.Min(position, new Int3(stripDefinitions[0].Count - 1, stripDefinitions[1].Count - 1, stripDefinitions[2].Count - 1));
        }

        protected override Int3 GetElementSpanValues(UIElement element)
        {
            var position = GetElementGridPositions(element);
            var span = base.GetElementSpanValues(element);
            return Int3.Min(position + span, new Int3(stripDefinitions[0].Count, stripDefinitions[1].Count, stripDefinitions[2].Count)) - position;
        }
    }
}
