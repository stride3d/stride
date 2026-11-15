// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Assets.Editor.Services;
using Stride.Graphics;

namespace Stride.Assets.Presentation.SceneEditor.Services
{
    /// <summary>
    /// The frame capture of the WPF Game Studio started with <c>/RenderDoc</c>: a RenderDoc capture of the cubemap capture and of
    /// the light probe computation, as before these moved to the shared editor.
    /// </summary>
    public sealed class RenderDocFrameCaptureService : IGraphicsFrameCaptureService
    {
        private readonly RenderDocManager renderDocManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderDocFrameCaptureService"/> class.
        /// </summary>
        /// <param name="renderDocManager">The RenderDoc manager of the Game Studio, initialized.</param>
        public RenderDocFrameCaptureService(RenderDocManager renderDocManager)
        {
            ArgumentNullException.ThrowIfNull(renderDocManager);
            this.renderDocManager = renderDocManager;
        }

        /// <inheritdoc/>
        public void StartFrameCapture(GraphicsDevice graphicsDevice)
        {
            renderDocManager.StartFrameCapture(graphicsDevice, IntPtr.Zero);
        }

        /// <inheritdoc/>
        public void EndFrameCapture(GraphicsDevice graphicsDevice)
        {
            renderDocManager.EndFrameCapture(graphicsDevice, IntPtr.Zero);
        }
    }
}
