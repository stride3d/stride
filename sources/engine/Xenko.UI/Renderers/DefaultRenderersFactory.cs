// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Reflection;

using Xenko.Core;
using Xenko.UI.Controls;

namespace Xenko.UI.Renderers
{
    /// <summary>
    /// A factory that create the default renderer for each <see cref="UIElement"/> type.
    /// </summary>
    internal class DefaultRenderersFactory : IElementRendererFactory
    {
        private readonly ElementRenderer defaultRenderer;

        private readonly Dictionary<Type, ElementRenderer> typeToRenderers = new Dictionary<Type, ElementRenderer>();

        public DefaultRenderersFactory(IServiceRegistry services)
        {
            defaultRenderer = new ElementRenderer(services);
            typeToRenderers[typeof(Border)] = new DefaultBorderRenderer(services);
            typeToRenderers[typeof(Button)] = new DefaultButtonRenderer(services);
            typeToRenderers[typeof(ContentDecorator)] = new DefaultContentDecoratorRenderer(services);
            typeToRenderers[typeof(EditText)] = new DefaultEditTextRenderer(services);
            typeToRenderers[typeof(ImageElement)] = new DefaultImageRenderer(services);
            typeToRenderers[typeof(ModalElement)] = new DefaultModalElementRenderer(services);
            typeToRenderers[typeof(ScrollBar)] = new DefaultScrollBarRenderer(services);
            typeToRenderers[typeof(ScrollingText)] = new DefaultScrollingTextRenderer(services);
            typeToRenderers[typeof(Slider)] = new DefaultSliderRenderer(services);
            typeToRenderers[typeof(TextBlock)] = new DefaultTextBlockRenderer(services);
            typeToRenderers[typeof(ToggleButton)] = new DefaultToggleButtonRenderer(services);
        }

        public ElementRenderer TryCreateRenderer(UIElement element)
        {
            // try to get the renderer from the registered default renderer
            var currentType = element.GetType();
            while (currentType != null)
            {
                if (typeToRenderers.TryGetValue(currentType, out var renderer))
                    return renderer;

                currentType = currentType.GetTypeInfo().BaseType;
            }

            return defaultRenderer;
        }
    }
}
