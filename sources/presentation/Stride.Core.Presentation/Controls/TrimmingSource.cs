// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Presentation.Controls
{
    /// <summary>
    /// Represents the position at which the text will be trimmed and the ellipsis will be inserted.
    /// </summary>
    public enum TrimmingSource
    {
        /// <summary>
        /// The text will be trimmed from the beginning.
        /// </summary>
        Begin,
        /// <summary>
        /// The text will be trimmed from the middle.
        /// </summary>
        Middle,
        /// <summary>
        /// The text will be trimmed from the end.
        /// </summary>
        End
    }
}
