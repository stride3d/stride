// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;

namespace Xenko.Core.Assets
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

        /// <summary>
        /// A plugin.
        /// </summary>
        Plugin,
    }
}
