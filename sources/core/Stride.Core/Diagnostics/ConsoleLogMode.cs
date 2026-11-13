// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Diagnostics;

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
    /// Messages are written in all builds. When the process has neither a console nor a redirected output, they go to the console of the parent process, or to a new one.
    /// </summary>
    Always,

    /// <summary>
    /// Messages are written in all builds to the standard output, and no console is ever opened for them.
    /// </summary>
    StandardOutput,
}
