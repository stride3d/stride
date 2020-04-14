// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Games;
using Stride.Graphics;

namespace Stride.UI.Renderers
{
    /// <summary>
    /// Base class for UI element renderers
    /// </summary>
    public class ElementRenderer : ComponentBase
    {
        private static Color blackColor;

        private UISystem UI;

        /// <summary>
        /// A reference to the game asset manager.
        /// </summary>
        public IContentManager Content { get; private set; }

        private IGraphicsDeviceService GraphicsDeviceService { get; }

        /// <summary>
        /// A reference to the game graphic device.
        /// </summary>
        public GraphicsDevice GraphicsDevice => GraphicsDeviceService?.GraphicsDevice;

        /// <summary>
        /// Gets a reference to the UI image drawer.
        /// </summary>
        public UIBatch Batch => UI.Batch;

        /// <summary>
        /// A depth stencil state that keep the stencil value in any cases.
        /// </summary>
        public DepthStencilStateDescription KeepStencilValueState => UI.KeepStencilValueState;

        /// <summary>
        /// A depth stencil state that increase the stencil value if the stencil test passes.
        /// </summary>
        public DepthStencilStateDescription IncreaseStencilValueState => UI.IncreaseStencilValueState;

        /// <summary>
        /// A depth stencil state that decrease the stencil value if the stencil test passes.
        /// </summary>
        public DepthStencilStateDescription DecreaseStencilValueState => UI.DecreaseStencilValueState;

        /// <summary>
        /// Create an instance of an UI element renderer.
        /// </summary>
        /// <param name="services">The list of registered services</param>
        public ElementRenderer(IServiceRegistry services)
        {
            Content = services.GetSafeServiceAs<IContentManager>();
            GraphicsDeviceService = services.GetSafeServiceAs<IGraphicsDeviceService>();

            UI = services.GetService<UISystem>();
            if (UI == null)
            {
                UI = new UISystem(services);
                services.AddService(UI);
                var gameSystems = services.GetService<IGameSystemCollection>();
                gameSystems?.Add(UI);
            }
        }

        /// <summary>
        /// Render the provided <see cref="UIElement"/>.
        /// </summary>
        /// <param name="element">The element to render.</param>
        /// <param name="context">The rendering context containing information how to draw the element.</param>
        /// <remarks>The render target, the depth stencil buffer and the depth stencil state are already correctly set when entering this function. 
        /// If the user wants to perform some intermediate rendering, it is his responsibility to bind them back correctly before the final rendering.</remarks>
        public virtual void RenderColor(UIElement element, UIRenderingContext context)
        {
            var backgroundColor = element.RenderOpacity * element.BackgroundColor;

            // optimization: don't draw the background if transparent
            if (backgroundColor == new Color())
                return;

            // Default implementation: render an back-face cube with background color
            Batch.DrawBackground(ref element.WorldMatrixInternal, ref element.RenderSizeInternal, ref backgroundColor, context.DepthBias);

            // increase depth bias value so that next elements renders on top of it.
            context.DepthBias += 1;
        }

        /// <summary>
        /// Render the clipping region of the provided <see cref="UIElement"/>.
        /// </summary>
        /// <param name="element">The element to render.</param>
        /// <param name="context">The rendering context containing information how to draw the element.</param>
        /// <remarks>The render target, the depth stencil buffer and the depth stencil state are already correctly set when entering this function. 
        /// If the user wants to perform some intermediate rendering, it is his responsibility to bind them back correctly before the final rendering.</remarks>
        public virtual void RenderClipping(UIElement element, UIRenderingContext context)
        {
            // Default implementation: render an back-face cube
            Batch.DrawBackground(ref element.WorldMatrixInternal, ref element.RenderSizeInternal, ref blackColor, context.DepthBias);

            // increase the context depth bias for next elements.
            context.DepthBias += 1;
        }
    }
}
