// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Editor.Engine;

/// <summary>
/// Creates the viewports the editor games render into. The UI toolkit of the editor registers it in the services of
/// the session; a session without it (no graphics device, previews turned off) has no game view.
/// </summary>
public interface IEditorGameViewportService
{
    /// <summary>
    /// Creates a viewport for one game (UI thread).
    /// </summary>
    /// <param name="logger">The logger of the game, which the viewport reports to.</param>
    IEditorGameViewport CreateViewport(Logger logger);
}
