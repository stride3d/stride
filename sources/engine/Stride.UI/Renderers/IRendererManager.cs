// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.UI.Renderers
{
    /// <summary>
    /// The interface for managing UI element renderers.
    /// </summary>
    public interface IRendererManager
    {
        /// <summary>
        /// Get the renderer of the corresponding <see cref="UIElement"/>.
        /// </summary>
        /// <param name="element">The element to render.</param>
        /// <returns>The renderer to render the element.</returns>
        ElementRenderer GetRenderer(UIElement element);

        /// <summary>
        /// Associate a renderer factory to an UI element type.
        /// </summary>
        /// <param name="uiElementType">The type of ui elements to which associate the factory.</param>
        /// <param name="factory">The renderer factory to associate to the UI element type <paramref name="uiElementType"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="uiElementType"/> or <paramref name="factory"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="uiElementType"/> is not a descendant of <see cref="UIElement"/>.</exception>
        /// <remarks>A factory associated to the type "MyType" is also be used to create renderer from descendant of "MyType" excepted if a factory is directly associated to the descendant type.</remarks>
        void RegisterRendererFactory(Type uiElementType, IElementRendererFactory factory);
        
        /// <summary>
        /// Associate a renderer to an <see cref="UIElement"/>.
        /// </summary>
        /// <param name="element">The element to which associate the renderer</param>
        /// <param name="renderer">The renderer to associate to the UI element.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="element"/> or the <paramref name="renderer"/> is null.</exception>
        void RegisterRenderer(UIElement element, ElementRenderer renderer);
    }
}
