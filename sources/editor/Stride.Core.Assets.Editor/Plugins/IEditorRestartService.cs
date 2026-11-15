// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Stride.Core.Assets.Editor.Plugins;

/// <summary>
/// Restarts the editor into the session it has open, for what applies only when a session opens: the plugins switched off or
/// on, and safe mode. The cross-platform Game Studio starts a new process, as it does to open another project.
/// </summary>
[Experimental("STREXP002")]
public interface IEditorRestartService
{
    /// <summary>
    /// Closes the session, after asking whether to save it, and opens it again in a fresh editor, in safe mode or not. Without
    /// a session, the next one opens in that mode.
    /// </summary>
    /// <param name="safeMode">Whether the fresh editor runs in safe mode (<c>--safe-mode</c>).</param>
    /// <returns>True when the editor restarts (or, without a session, changed its mode); false when the user kept the session open.</returns>
    Task<bool> RestartAsync(bool safeMode);
}
