// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Stride.Core.UnsafeExtensions;

namespace Stride.Rendering;

public partial class ParameterCollection
{
    /// <summary>
    ///   Represents a specialized copier that provides an efficient mechanism to copy a logical composition
    ///   of parameters from a source to a destination parameter collection, based on a shared key root.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Unlike <see cref="Copier"/>, which copies all parameters between collections (optimizing for layout compatibility),
    ///     <b><see cref="CompositionCopier"/></b> is specialized for copying only those parameters
    ///     whose keys match a given suffix (the <c>keyRoot</c>).
    ///     This is useful for scenarios where only a subset of parameters —such as those belonging to a material, light,
    ///     or effect group— need to be transferred.
    ///   </para>
    ///   <para>
    ///     The copier analyzes the destination layout and compiles contiguous copy ranges for matching parameters,
    ///     assuming the destination layout is sequential. It then copies data and resources efficiently using these ranges.
    ///   </para>
    /// </remarks>
    public struct CompositionCopier
    {
        // A compiled copy range that contains information about how to copy elements (data or resources).
        // It is null if the layouts match and a fast copy can be performed.
        private List<CopyRange> ranges;

        /// <summary>
        ///   Gets a value indicating whether the current composition copier is in a valid state,
        ///   i.e., whether it has been successfully compiled with a valid destination layout.
        /// </summary>
        public readonly bool IsValid => ranges is not null;

        private ParameterCollection destination;


        /// <summary>
        ///   Copies data and object values from a source parameter collection to the destination parameter collection
        ///   whose layout and copy ranges have been compiled previously by the <see cref="CompileCopyRanges"/>.
        /// </summary>
        /// <param name="source">The source <see cref="ParameterCollection"/> from which data and resources will be copied.</param>
        public readonly void Copy(ParameterCollection source)
        {
            scoped ReadOnlySpan<CopyRange> copyRanges = CollectionsMarshal.AsSpan(ranges);

            for (int i = 0; i < copyRanges.Length; i++)
            {
                scoped ref readonly var range = ref copyRanges[i];

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
        ///   Analyzes the source and destination parameter collection layouts to determine
        ///   how to copy elements and compiles a list of copy ranges.
        /// </summary>
        /// <param name="destination">The destination <see cref="ParameterCollection"/> whose layout will be used for the copy operation.
        /// </param>
        /// <param name="source"></param>
        /// <param name="keyRoot"></param>
        /// <remarks>
        ///   <para>
        ///     This method updates the source layout if it is not already set and matches elements
        ///     between the source and destination layouts.
        ///     It creates a list of copy ranges based on the matched elements.
        ///   </para>
        ///   <para>
        ///     This method <strong>assumes that the destination layout is sequential</strong>,
        ///     i.e., that the destination parameters are laid out in a contiguous manner.
        ///   </para>
        /// </remarks>
        public void CompileCopyRanges(ParameterCollection destination, ParameterCollection source, string keyRoot)
        {
            ranges = [];
            this.destination = destination;
            var sourceLayout = new ParameterCollectionLayout();

            // Helper structures to try to keep range contiguous and have as few copy operations as possible (NOTE: There can be some padding)
            var currentDataRange = new CopyRange { Type = CopyRangeType.Data, DestStart = INVALID };
            var currentResourceRange = new CopyRange { Type = CopyRangeType.Resource, DestStart = INVALID };

            // Iterate over each variable in destination.
            // If they match keyRoot, create the equivalent layout in source
            foreach (var parameterKeyInfo in destination.Layout.LayoutParameterKeyInfos)
            {
                bool isResource = parameterKeyInfo.IsResourceParameter;
                bool isData = parameterKeyInfo.IsValueParameter;

                if (parameterKeyInfo.Key.Name.EndsWith(keyRoot, StringComparison.Ordinal))
                {
                    // That's a match
                    var subkeyName = parameterKeyInfo.Key.Name[..^keyRoot.Length];
                    var subkey = ParameterKeys.FindByName(subkeyName);

                    if (isData)
                    {
                        // First time since range reset, let's setup destination offset
                        if (currentDataRange.DestStart == INVALID)
                            currentDataRange.DestStart = parameterKeyInfo.Offset;

                        // Might be some empty space (padding)
                        currentDataRange.Size = parameterKeyInfo.Offset - currentDataRange.DestStart;

                        var offset = currentDataRange.SourceStart + currentDataRange.Size;
                        var pki = new ParameterKeyInfo(subkey, offset, parameterKeyInfo.Count);
                        sourceLayout.LayoutParameterKeyInfos.Add(pki);

                        var size = ComputeAlignedSizeMinusTrailingPadding(
                            elementSize: parameterKeyInfo.Key.Size,
                            elementCount: parameterKeyInfo.Count);

                        currentDataRange.Size += size;
                    }
                    else if (isResource)
                    {
                        // First time since range reset, let's setup destination offset
                        if (currentResourceRange.DestStart == INVALID)
                            currentResourceRange.DestStart = parameterKeyInfo.BindingSlot;

                        // Might be some empty space (padding) (probably unlikely for resources...)
                        currentResourceRange.Size = parameterKeyInfo.BindingSlot - currentResourceRange.DestStart;

                        var pki = new ParameterKeyInfo(subkey, currentResourceRange.SourceStart + currentResourceRange.Size);
                        sourceLayout.LayoutParameterKeyInfos.Add(pki);

                        currentResourceRange.Size += parameterKeyInfo.Count;
                    }
                }
                else // Not a match with `keyRoot`
                {
                    // Found one item not part of the range, let's finish it
                    if (isData)
                        FlushRangeIfNecessary(ref currentDataRange);
                    else if (isResource)
                        FlushRangeIfNecessary(ref currentResourceRange);
                }
            }

            // Finish ranges
            FlushRangeIfNecessary(ref currentDataRange);
            FlushRangeIfNecessary(ref currentResourceRange);

            // Update sizes
            sourceLayout.BufferSize = currentDataRange.SourceStart;
            sourceLayout.ResourceCount = currentResourceRange.SourceStart;

            source.UpdateLayout(sourceLayout);
        }

        /// <summary>
        ///   Adds the specified range to the list of ranges to copy if it contains data,
        ///   and resets the range to be reused.
        /// </summary>
        /// <param name="currentRange">
        ///   The range to be evaluated and potentially added to the collection.
        ///   The range is reset if added.
        /// </param>
        private readonly void FlushRangeIfNecessary(scoped ref CopyRange currentRange)
        {
            if (currentRange.Size > 0)
            {
                ranges.Add(currentRange);

                currentRange.SourceStart += currentRange.Size;
                currentRange.DestStart = INVALID;
                currentRange.Size = 0;
            }
        }


        #region CopyRange structure

        private const int INVALID = -1; // Marks an invalid copy range

        private enum CopyRangeType : byte
        {
            Resource = 1,
            Data = 2
        }

        private record struct CopyRange(CopyRangeType Type, int SourceStart, int DestStart, int Size)
        {
            public required CopyRangeType Type { get; init; } = Type;

            public readonly bool IsResource => Type is CopyRangeType.Resource;
            public readonly bool IsData => Type is CopyRangeType.Data;
        }

        #endregion
    }
}
