// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Diagnostics
{
    /// <summary>
    /// Defines how the console is opened.
    /// </summary>
    public enum ConsoleLogMode
    {
        /// <summary>
        /// The console should be visible only in debug and if there is a message, otherwise it is not visible.
        /// </summary>
        Auto,

        /// <summary>
        /// Same as <see cref="Auto"/>
        /// </summary>
        Default = Auto,

        /// <summary>
        /// The console should not be visible.
        /// </summary>
        None,

        /// <summary>
        /// The console should be always visible
        /// </summary>
        Always,
    }
}
