// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko
{
    /// <summary>
    /// An enum that describes the compilation mode used to compile a game.
    /// </summary>
    public enum CompilationMode
    {
        /// <summary>
        /// The game was compiled in debug mode. 
        /// </summary>
        /// <remarks>
        /// Impact on compilation on other components:
        /// - Shaders are compiled in debug mode with debug information.
        /// </remarks>
        Debug,

        /// <summary>
        /// The game was compiled in release mode.
        /// </summary>
        /// <remarks>
        /// Impact on compilation on other components:
        /// - Shaders are compiled in optimization level 2 with <c>no debug</c> information.
        /// </remarks>
        Release,

        /// <summary>
        /// The game was compiled in release mode for app-store (for end-user release)
        /// </summary>
        /// <remarks>
        /// Impact on compilation on other components:
        /// - Shaders are compiled in optimization level 2 with <c>no debug</c> information.
        /// </remarks>
        AppStore,

        /// <summary>
        /// The game was compiled in testing mode. 
        /// </summary>
        /// <remarks>
        /// Impact on compilation on other components:
        /// - Shaders are compiled in debug mode with debug information.
        /// </remarks>
        Testing,
    }
}
