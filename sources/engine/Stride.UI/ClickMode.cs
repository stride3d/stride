// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.UI
{
    /// <summary>
    /// Specifies when the Click event should be raised.
    /// </summary>
    public enum ClickMode
    {
        /// <summary>
        /// Specifies that the Click event should be raised as soon as a button is pressed.
        /// </summary>
        /// <userdoc>Specifies that the Click event should be raised as soon as a button is pressed.</userdoc>
        Press,
        /// <summary>
        /// Specifies that the Click event should be raised when a button is pressed and released.
        /// </summary>
        /// <userdoc>Specifies that the Click event should be raised when a button is pressed and released.</userdoc>
        Release,
    }
}
