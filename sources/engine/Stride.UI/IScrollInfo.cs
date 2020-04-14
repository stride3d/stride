// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.UI.Controls;

namespace Stride.UI
{
    /// <summary>
    /// Represents the main scrollable region inside a <see cref="ScrollViewer"/> control.
    /// </summary>
    public interface IScrollInfo
    {
        /// <summary>
        /// Gets a value that indicates if the <see cref="UIElement"/> can scroll in the provided direction.
        /// </summary>
        /// <param name="direction">The direction in which perform the scrolling</param>
        bool CanScroll(Orientation direction);

        /// <summary>
        /// Gets the size of the extent. That is the virtual total size of the <see cref="UIElement"/>.
        /// </summary>
        Vector3 Extent { get; }

        /// <summary>
        /// Gets the offset of the scrolled content.
        /// </summary>
        Vector3 Offset { get; }

        /// <summary>
        /// Gets the size of the viewport for this content.
        /// </summary>
        Vector3 Viewport { get; }

        /// <summary>
        /// Gets or sets a <see cref="ScrollViewer"/> element that controls scrolling behavior.
        /// </summary>
        ScrollViewer ScrollOwner { get; set; }

        /// <summary>
        /// Get the position of the horizontal, vertical and in depth scroll bars.
        /// </summary>
        /// <returns>A value between <value>0</value> and <value>1</value> for each component indicating the position of the scroll bar</returns>
        /// <remarks>Return <value>0</value> for each direction the element cannot scroll</remarks>
        Vector3 ScrollBarPositions { get; }

        /// <summary>
        /// Go to the next line in the given the direction.
        /// </summary>
        /// <param name="direction">The direction in which to scroll</param>
        void ScrollToNextLine(Orientation direction);

        /// <summary>
        /// Go to the previous line in the given the direction.
        /// </summary>
        /// <param name="direction">The direction in which to scroll</param>
        void ScrollToPreviousLine(Orientation direction);

        /// <summary>
        /// Go to the next page in the given the direction.
        /// </summary>
        /// <param name="direction">The direction in which to scroll</param>
        void ScrollToNextPage(Orientation direction);

        /// <summary>
        /// Go to the previous page in the given the direction.
        /// </summary>
        /// <param name="direction">The direction in which to scroll</param>
        void ScrollToPreviousPage(Orientation direction);

        /// <summary>
        /// Go to the beginning of the element in the given the direction.
        /// </summary>
        /// <param name="direction">The direction in which to scroll</param>
        void ScrollToBeginning(Orientation direction);

        /// <summary>
        /// Go to the end of the element in the given the direction.
        /// </summary>
        /// <param name="direction">The direction in which to scroll</param>
        void ScrollToEnd(Orientation direction);

        /// <summary>
        /// Increase the amount of offset from the current scrolling position.
        /// </summary>
        /// <param name="offsets"></param>
        void ScrollOf(Vector3 offsets);
    }
}
