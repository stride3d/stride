// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.UI.Controls;

namespace Stride.UI
{
    /// <summary>
    /// Interface providing anchor information to its <see cref="ScrollViewer"/>.
    /// </summary>
    public interface IScrollAnchorInfo
    {
        /// <summary>
        /// Indicate whether the <see cref="ScrollViewer"/> managing this element should snap scrolling to anchors in the provided direction.
        /// </summary>
        /// <param name="direction">The direction in which to anchor</param>
        bool ShouldAnchor(Orientation direction);

        /// <summary>
        /// Get the distances to the previous and next anchors in the provided direction and from given position.
        /// </summary>
        /// <param name="position">The current scrolling position</param>
        /// <param name="direction">The direction in which to anchor</param>
        /// <remarks>The distance contained in the X component of the returned vector is inferior or equal to 0 
        /// and the distance contained in the Y component is superior or equal to 0.</remarks>
        /// <returns>The distances to previous and next anchors from to current scroll position</returns>
        Vector2 GetSurroudingAnchorDistances(Orientation direction, float position);

        /// <summary>
        /// Gets or sets a <see cref="ScrollViewer"/> element that controls scrolling behavior.
        /// </summary>
        ScrollViewer ScrollOwner { get; set; }
    }
}
