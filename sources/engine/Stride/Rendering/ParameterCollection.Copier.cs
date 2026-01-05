// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Stride.Core.UnsafeExtensions;

namespace Stride.Rendering;

public partial class ParameterCollection
{
    /// <summary>
    ///   Represents a copier designed to efficiently copy data and resources from a source
    ///   to a destination parameter collection, optimizing for layout compatibility and efficiency.
    /// </summary>
    /// <param name="destination">
    ///   The destination <see cref="ParameterCollection"/> where data and resources will be copied to.
    /// </param>
    /// <param name="source">
    ///   The source <see cref="ParameterCollection"/> from which data and resources will be copied.
    /// </param>
    /// <param name="subKey">An optional subkey used to match specific elements between the source and destination.</param>
    public struct Copier(ParameterCollection destination, ParameterCollection source, string subKey = null)
    {
        // A compiled copy range that contains information about how to copy elements (data or resources).
        // It is null if the layouts match and a fast copy can be performed.
        private CopyRange[] ranges = null;

        private int sourceLayoutCounter = 0;


        /// <summary>
        ///   Copies data from the source to the destination parameter collections, optimizing for layout compatibility.
        /// </summary>
        /// <exception cref="NotImplementedException">Thrown if the destination layout is not defined.</exception>
        /// <remarks>
        ///   This method attempts to perform a fast copy if the source and destination layouts are identical.
        ///   If the layouts differ, it compiles the necessary ranges for copying and retries the fast copy.
        ///   If a fast copy is not possible, it falls back to a slower element-by-element copy.
        /// </remarks>
        public void Copy()
        {
            // TODO: We should provide a slow version for first copy during Extract (layout isn't known yet)
            var destinationLayout = destination.Layout ?? throw new NotImplementedException();
            if (destinationLayout == source.Layout)
            {
                // Easy, let's do a full copy!
                PerformFastCopy(destinationLayout);
                return;
            }

            if (ranges is null || sourceLayoutCounter != source.LayoutCounter)
            {
                CompileCopyRanges();

                // Try again in case fast copy is possible
                if (destinationLayout == source.Layout)
                {
                    PerformFastCopy(destinationLayout);
                    return;
                }
            }

            // Slower path: copy element by element
            PerformRangesCopy();
        }

        /// <summary>
        ///   Copies data and object values from the source to the destination directly
        ///   when it is known the layouts match.
        /// </summary>
        /// <param name="destinationLayout">
        ///   The layout of the destination buffer, specifying the data size and resource count for the copy operation.
        /// </param>
        private readonly void PerformFastCopy(ParameterCollectionLayout destinationLayout)
        {
            source.DataValues[..destinationLayout.BufferSize].CopyTo(destination.DataValues.AsSpan());
            source.ObjectValues[..destinationLayout.ResourceCount].CopyTo(destination.ObjectValues.AsSpan());
        }

        /// <summary>
        ///   Copies data and object values from the source to the destination based on the ranges
        ///   compiled by the <see cref="CompileCopyRanges"/> method.
        /// </summary>
        private readonly void PerformRangesCopy()
        {
            for (int i = 0; i < ranges.Length; i++)
            {
                scoped ref var range = ref ranges[i];

                if (range.IsResource)
                {
                    if (range.Size == 1)
                    {
                        destination.objectValues[range.DestStart] = source.objectValues[range.SourceStart];
                    }
                    else
                    {
                        var sourceSpan = source.ObjectValues.Slice(range.SourceStart, range.Size);
                        var destSpan = destination.ObjectValues[range.DestStart..].AsSpan();
                        sourceSpan.CopyTo(destSpan);
                    }
                }
                else if (range.IsData)
                {
                    var sourceSpan = source.DataValues.Slice(range.SourceStart, range.Size);
                    var destSpan = destination.DataValues[range.DestStart..].AsSpan();
                    sourceSpan.CopyTo(destSpan);
                }
            }
        }

        /// <summary>
        ///   Analyzes the source and destination layouts to determine how to copy elements
        ///   and compiles a list of copy ranges.
        /// </summary>
        /// <remarks>
        ///   This method updates the source layout if it is not already set and matches elements
        ///   between the source and destination layouts.
        ///   It creates a list of copy ranges based on the matched elements.
        /// </remarks>
        private void CompileCopyRanges()
        {
            // If we are first, let's apply our layout!
            if (source.Layout is null && subKey is null)
            {
                source.UpdateLayout(destination.Layout);
                return;
            }
            else
            {
                // TODO GRAPHICS REFACTOR optim: check if layout are the same
                //if (source.Layout.LayoutParameterKeyInfos == destination.Layout.LayoutParameterKeyInfos)
            }

            var copyRanges = new List<CopyRange>();

            // Try to match elements (both source and destination should have a layout by now)
            foreach (var parameterKeyInfo in destination.Layout.LayoutParameterKeyInfos)
            {
                var sourceKey = parameterKeyInfo.Key;

                if (subKey is not null && sourceKey.Name.EndsWith(subKey, StringComparison.Ordinal))
                {
                    // That's a match
                    var subkeyName = parameterKeyInfo.Key.Name[..^subKey.Length];
                    sourceKey = ParameterKeys.FindByName(subkeyName);
                }

                if (parameterKeyInfo.Key.Type == ParameterKeyType.Value)
                {
                    var sourceAccessor = source.GetValueAccessorHelper(sourceKey, parameterKeyInfo.Count);
                    var destAccessor = destination.GetValueAccessorHelper(parameterKeyInfo.Key, parameterKeyInfo.Count);

                    var size = ComputeAlignedSizeMinusTrailingPadding(
                        elementSize: parameterKeyInfo.Key.Size,
                        elementCount: Math.Min(sourceAccessor.Count, destAccessor.Count));

                    copyRanges.Add(new CopyRange(CopyRangeType.Data, sourceAccessor.Offset, destAccessor.Offset, size));
                }
                else
                {
                    var sourceAccessor = source.GetObjectParameterHelper(sourceKey);
                    var destAccessor = destination.GetObjectParameterHelper(parameterKeyInfo.Key);

                    var elementCount = Math.Min(sourceAccessor.Count, destAccessor.Count);

                    copyRanges.Add(new CopyRange(CopyRangeType.Resource, sourceAccessor.Offset, destAccessor.Offset, elementCount));
                }
            }

            ranges = copyRanges.ToArray();
            sourceLayoutCounter = source.LayoutCounter;
        }

        #region CopyRange structure

        private enum CopyRangeType : byte
        {
            Resource = 1,
            Data = 2
        }

        private readonly record struct CopyRange(CopyRangeType Type, int SourceStart, int DestStart, int Size)
        {
            public readonly bool IsResource => Type is CopyRangeType.Resource;
            public readonly bool IsData => Type is CopyRangeType.Data;
        }

        #endregion
    }
}
