// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Assets.Presentation.ViewModel
{
    [Flags]
    public enum ResizingDirection
    {
        Center = 1,
        Left = 2,
        Top = 4,
        Right = 8,
        Bottom = 16,
        TopLeft = Left | Top,
        TopRight = Right | Top,
        BottomLeft = Left | Bottom,
        BottomRight = Right | Bottom
    }

    public interface IResizingTarget
    {
        /// <summary>
        /// Notify the target once the resizing operation is completed.
        /// </summary>
        /// <param name="direction">The direction of the resizing.</param>
        /// <param name="horizontalChange">The total horizontal change during the dragging.</param>
        /// <param name="verticalChange">The total vertical change during the dragging.</param>
        void OnResizingCompleted(ResizingDirection direction, double horizontalChange, double verticalChange);

        /// <summary>
        /// Called on the target during the resizing operation.
        ///  </summary>
        /// <param name="direction">The direction of the resizing.</param>
        /// <param name="horizontalChange">The horizontal delta of the resizing.</param>
        /// <param name="verticalChange">The vertical delta of the resizing.</param>
        void OnResizingDelta(ResizingDirection direction, double horizontalChange, double verticalChange);

        /// <summary>
        /// Notify the target that the resizing operation started.
        /// </summary>
        /// <param name="direction">The direction of the resizing.</param>
        void OnResizingStarted(ResizingDirection direction);
    }
}
