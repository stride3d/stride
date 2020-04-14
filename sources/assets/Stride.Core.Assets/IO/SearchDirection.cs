// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Assets
{
    /// <summary>
    /// A direction to search for files in directories
    /// </summary>
    public enum SearchDirection
    {
        /// <summary>
        /// Search files in all sub-directories.
        /// </summary>
        Down,

        /// <summary>
        /// Searchg files going upward in the directory hierarchy.
        /// </summary>
        Up,
    }
}
