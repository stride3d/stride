// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.UI
{
    /// <summary>
    /// The different types of strip possible of a grid.
    /// </summary>
    public enum StripType
    {
        /// <summary>
        /// A strip having fixed size expressed in number of virtual pixels.
        /// </summary>
        /// <userdoc>A strip having fixed size expressed in number of virtual pixels.</userdoc>
        Fixed,

        /// <summary>
        /// A strip that occupies exactly the size required by its content. 
        /// </summary>
        /// <userdoc>A strip that occupies exactly the size required by its content. </userdoc>
        Auto,

        /// <summary>
        /// A strip that occupies the maximum available size, dispatched among the other stared-size columns.
        /// </summary>
        /// <userdoc>A strip that occupies the maximum available size, dispatched among the other stared-size columns.</userdoc>
        Star,
    }
}
