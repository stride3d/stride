// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.UI;

namespace Xenko.Rendering.UI
{
    public class RenderUIElement : RenderObject
    {
        public RenderUIElement(UIComponent uiComponent, TransformComponent transformComponent)
        {
            UIComponent = uiComponent;
            TransformComponent = transformComponent;
        }

        public readonly UIComponent UIComponent;

        public readonly TransformComponent TransformComponent;

        /// <summary>
        /// Last registered position of teh mouse
        /// </summary>
        public Vector2 LastMousePosition;

        /// <summary>
        /// Last element over which the mouse cursor was registered
        /// </summary>
        public UIElement LastMouseOverElement;

        /// <summary>
        /// Last element which received a touch/click event
        /// </summary>
        public UIElement LastTouchedElement;

        public Vector3 LastIntersectionPoint;

        public Matrix LastRootMatrix;
    }
}
