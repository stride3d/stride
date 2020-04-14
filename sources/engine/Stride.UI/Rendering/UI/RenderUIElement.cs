// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.UI;

namespace Stride.Rendering.UI
{
    public class RenderUIElement : RenderObject
    {
        public RenderUIElement()
        {
        }

        public Matrix WorldMatrix;

        // UIComponent values
        public UIPage Page;
        public bool IsFullScreen;
        public Vector3 Resolution;
        public Vector3 Size;
        public ResolutionStretch ResolutionStretch;
        public bool IsBillboard;
        public bool SnapText;
        public bool IsFixedSize;

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
