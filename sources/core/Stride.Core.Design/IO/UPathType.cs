// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.IO
{
    /// <summary>
    /// Describes if a <see cref="UPath"/> is relative or absolute.
    /// </summary>
    public enum UPathType
    {
        /// <summary>
        /// The path is absolute
        /// </summary>
        Absolute,

        /// <summary>
        /// The path is relative
        /// </summary>
        Relative,
    }
}
