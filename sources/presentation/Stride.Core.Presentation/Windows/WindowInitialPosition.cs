// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Core.Presentation.Windows
{
    /// <summary>
    /// An enum representing the initial position of a window shown with the <see cref="WindowManager"/>.
    /// </summary>
    public enum WindowInitialPosition
    {
        /// <summary>
        /// The window will be displayed centered relative to its owner.
        /// </summary>
        CenterOwner,
        /// <summary>
        /// The window will be displayed centered relative to the screen.
        /// </summary>
        CenterScreen,
        /// <summary>
        /// The window will be displayed close to the mouse cursor.
        /// </summary>
        MouseCursor,
    };
}
