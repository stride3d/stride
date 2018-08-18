// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

using System.Reflection;

namespace Xenko.UI.Renderers
{
    /// <summary>
    /// The class in charge to manage the renderer of the different <see cref="UIElement"/>s.
    /// Once registered into the manager, a renderer is owned by the manager.
    /// </summary>
    public class RendererManager: IRendererManager, IDisposable
    {
        private readonly IElementRendererFactory defaultFactory;

        private readonly Dictionary<Type, IElementRendererFactory> typesToUserFactories = new Dictionary<Type, IElementRendererFactory>();

        // Note: use Id instead of element instance in order to avoid to keep dead UIelement alive.
        private readonly Dictionary<Guid, ElementRenderer> elementIdToRenderer = new Dictionary<Guid, ElementRenderer>();

        /// <summary> 
        /// Create a new instance of <see cref="RendererManager"/> with provided DefaultFactory
        /// </summary>
        /// <param name="defaultFactory"></param>
        public RendererManager(IElementRendererFactory defaultFactory)
        {
            this.defaultFactory = defaultFactory;
        }

        public ElementRenderer GetRenderer(UIElement element)
        {
            ElementRenderer elementRenderer;
            elementIdToRenderer.TryGetValue(element.Id, out elementRenderer);
            if (elementRenderer == null)
            {
                // try to get the renderer from the user registered class factory
                var currentType = element.GetType();
                while (elementRenderer == null && currentType != null)
                {
                    if (typesToUserFactories.ContainsKey(currentType))
                        elementRenderer = typesToUserFactories[currentType].TryCreateRenderer(element);

                    currentType = currentType.GetTypeInfo().BaseType;
                }

                // try to get the renderer from the default renderer factory.
                if (elementRenderer == null && defaultFactory != null)
                    elementRenderer = defaultFactory.TryCreateRenderer(element);

                if (elementRenderer == null)
                    throw new InvalidOperationException($"No renderer found for element {element}");

                // cache the renderer for future uses.
                elementIdToRenderer[element.Id] = elementRenderer;
            }

            return elementRenderer;
        }

        public void RegisterRendererFactory(Type uiElementType, IElementRendererFactory factory)
        {
            if (uiElementType == null) throw new ArgumentNullException(nameof(uiElementType));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            if (!typeof(UIElement).GetTypeInfo().IsAssignableFrom(uiElementType.GetTypeInfo()))
                throw new InvalidOperationException(uiElementType + " is not a descendant of UIElement.");

            typesToUserFactories[uiElementType] = factory;
        }

        public void RegisterRenderer(UIElement element, ElementRenderer renderer)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));

            elementIdToRenderer[element.Id] = renderer;
        }

        public void Dispose()
        {
            foreach (var renderer in elementIdToRenderer.Values)
            {
                if (!renderer.IsDisposed)
                    renderer.Dispose();
            }
            elementIdToRenderer.Clear();
        }
    }
}
