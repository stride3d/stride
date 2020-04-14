// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;

namespace Stride.UI.Renderers
{
    /// <summary>
    /// The UI drawing context.
    /// It provides information about how to render <see cref="UIElement"/>s for drawing.
    /// </summary>
    public class UIRenderingContext
    {
        /// <summary>
        /// The active graphics context.
        /// </summary>
        public GraphicsContext GraphicsContext { get; set; }

        /// <summary>
        /// The current time.
        /// </summary>
        public GameTime Time { get; internal set; }

        /// <summary>
        /// The final render target to draw to.
        /// </summary>
        public Texture RenderTarget { get; set; }

        /// <summary>
        /// The final depth stencil buffer to draw to.
        /// </summary>
        public Texture DepthStencilBuffer { get; set; }

        /// <summary>
        /// The current reference value for the stencil test.
        /// </summary>
        public int StencilTestReferenceValue { get; set; }

        /// <summary>
        /// The value of the depth bias to use for draw call.
        /// </summary>
        public int DepthBias { get; set; }

        /// <summary>
        /// Gets or sets the value indicating if UI text should be snapped.
        /// </summary>
        public bool ShouldSnapText { get; set; }

        /// <summary>
        /// Gets the  virtual resolution of the UI.
        /// </summary>
        public Vector3 Resolution;

        /// <summary>
        /// Gets the view projection matrix of the UI.
        /// </summary>
        public Matrix ViewProjectionMatrix;
    }
}
