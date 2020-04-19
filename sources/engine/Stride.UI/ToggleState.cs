// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.UI
{
    /// <summary>
    /// Describe the different possible states of an <see cref="Controls.ToggleButton"/>.
    /// </summary>
    public enum ToggleState
    {
        /// <summary>
        /// The toggle button is checked.
        /// </summary>
        /// <userdoc>The toggle button is checked.</userdoc>
        Checked,
        /// <summary>
        /// The state of the toggle button is undetermined
        /// </summary>
        /// <userdoc>The state of the toggle button is undetermined</userdoc>
        Indeterminate,
        /// <summary>
        /// The toggle button is unchecked.
        /// </summary>
        /// <userdoc>The toggle button is unchecked.</userdoc>
        UnChecked,
    }
}
