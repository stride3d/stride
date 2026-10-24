// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;

namespace Stride.Rendering.UI
{
    public class UIProcessor : EntityProcessor<UIComponent>
    {
        protected override void OnEntityComponentAdding(Entity entity, UIComponent uiComponent, UIComponent _)
        {
            uiComponent.Services = Services;
        }

        protected override void OnEntityComponentRemoved(Entity entity, UIComponent uiComponent, UIComponent _)
        {
        }
    }
}
