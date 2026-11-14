// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Graphics;

namespace Stride.Assets.Editor.Services;

/// <summary>
/// Captures the graphics frame of an editor game step for a frame debugger: the cubemap capture of the scene view options and
/// the light probe computation start a capture before they render and end it after.
/// </summary>
/// <remarks>
/// The WPF Game Studio registers one when it is started with <c>/RenderDoc</c> (a RenderDoc capture). The Avalonia editor
/// registers none, and these steps then capture nothing.
/// </remarks>
[Experimental("STREXP007")]
public interface IGraphicsFrameCaptureService
{
    /// <summary>
    /// Starts capturing the frame, on the game thread.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device of the editor game.</param>
    void StartFrameCapture(GraphicsDevice graphicsDevice);

    /// <summary>
    /// Ends the frame capture started by <see cref="StartFrameCapture"/>, on the game thread.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device of the editor game.</param>
    void EndFrameCapture(GraphicsDevice graphicsDevice);
}
