// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Assets.Editor.View.Behaviors
{
    /// <summary>
    /// Splits a row into the drop zones of a drag and drop operation: an insert band along its top edge,
    /// an insert band along its bottom edge, and a drop zone in between.
    /// </summary>
    public static class DropZoneCalculator
    {
        /// <summary>
        /// The fraction of the height of a row covered by each insert band.
        /// </summary>
        public const double InsertBandRatio = 0.3;

        /// <summary>
        /// The minimum height of an insert band, in device-independent pixels.
        /// </summary>
        public const double MinInsertBandHeight = 3.0;

        /// <summary>
        /// The maximum height of an insert band, in device-independent pixels.
        /// </summary>
        public const double MaxInsertBandHeight = 10.0;

        /// <summary>
        /// The largest fraction of the height of a row that an insert band can cover.
        /// </summary>
        public const double MaxInsertBandRatio = 0.4;

        /// <summary>
        /// Computes the height of an insert band for a row of the given height.
        /// </summary>
        /// <param name="rowHeight">The height of the row.</param>
        /// <returns>The height of an insert band, or zero if the row height is not a usable value.</returns>
        /// <remarks>
        /// A band is never more than <see cref="MaxInsertBandRatio"/> of the row height. Therefore a drop zone always
        /// stays between the two bands, even if <see cref="MinInsertBandHeight"/> applies.
        /// </remarks>
        public static double GetInsertBandHeight(double rowHeight)
        {
            if (double.IsNaN(rowHeight) || double.IsInfinity(rowHeight) || rowHeight <= 0)
                return 0;

            var band = Math.Min(Math.Max(rowHeight * InsertBandRatio, MinInsertBandHeight), MaxInsertBandHeight);
            return Math.Min(band, rowHeight * MaxInsertBandRatio);
        }

        /// <summary>
        /// Determines the insert position matching a pointer position inside a row.
        /// </summary>
        /// <param name="positionY">The vertical position of the pointer, relative to the top of the row.</param>
        /// <param name="rowHeight">The height of the row.</param>
        /// <param name="allowAfter">True if the bottom band inserts after the row, false if it drops into the row.</param>
        /// <returns>The insert position, or <c>null</c> if the pointer is in the drop zone between the two bands.</returns>
        public static InsertPosition? GetInsertPosition(double positionY, double rowHeight, bool allowAfter)
        {
            var band = GetInsertBandHeight(rowHeight);
            if (band <= 0 || double.IsNaN(positionY) || positionY < 0 || positionY > rowHeight)
                return null;

            if (positionY < band)
                return InsertPosition.Before;

            if (allowAfter && positionY > rowHeight - band)
                return InsertPosition.After;

            return null;
        }
    }
}
