// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Core.Assets
{
    // REMARK: Beware of the order of values in this enum, it is used for sorting.

    /// <summary>
    /// Type of the project.
    /// </summary>
    [DataContract("ProjectType")]
    public enum ProjectType
    {
        /// <summary>
        /// A library.
        /// </summary>
        Library,

        /// <summary>
        /// An executable.
        /// </summary>
        Executable,
    }
}
