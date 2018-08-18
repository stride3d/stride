// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using System.Diagnostics;

using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.UI.Events;

namespace Xenko.UI.Controls
{
    /// <summary>
    /// Represents a modal element that puts an overlay upon the underneath elements and freeze their input.
    /// </summary>
    [DataContract(nameof(ModalElement))]
    [DebuggerDisplay("ModalElement - Name={Name}")]
    [Display(category: null)]
    public class ModalElement : ButtonBase
    {
        internal Color OverlayColorInternal;

        /// <summary>
        /// Occurs when the element is modal and the user click outside of the modal element.
        /// </summary>
        /// <remarks>A click event is bubbling</remarks>
        public event EventHandler<RoutedEventArgs> OutsideClick
        {
            add { AddHandler(OutsideClickEvent, value); }
            remove { RemoveHandler(OutsideClickEvent, value); }
        }

        /// <summary>
        /// Identifies the <see cref="OutsideClick"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> OutsideClickEvent = EventManager.RegisterRoutedEvent<RoutedEventArgs>(
            "OutsideClick",
            RoutingStrategy.Bubble,
            typeof(ModalElement));

        public ModalElement()
        {
            OverlayColorInternal = new Color(0, 0, 0, 0.6f);
            DrawLayerNumber += 1; // (overlay)
            VerticalAlignment = VerticalAlignment.Center;
            HorizontalAlignment = HorizontalAlignment.Center;
        }

        /// <summary>
        /// The color of the overlay drawn upon underneath elements.
        /// </summary>
        /// <userdoc>he color of the overlay drawn upon underneath elements.</userdoc>
        /// <userdoc>he color of the overlay drawn upon underneath elements.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        public Color OverlayColor
        {
            get { return OverlayColorInternal; }
            set { OverlayColorInternal = value; }
        }

        /// <summary>
        /// Determine if the control should block the input of underneath elements or not.
        /// </summary>
        /// <userdoc>True if the control should block the input of underneath elements, false otherwise.</userdoc>
        [DataMember]
        [Display(category: BehaviorCategory)]
        [DefaultValue(true)]
        public bool IsModal { get; set; } = true;

        protected override void OnTouchUp(TouchEventArgs args)
        {
            base.OnTouchUp(args);

            if (!IsModal || args.Source != this)
                return;

            var position = args.WorldPosition - new Vector3(WorldMatrixInternal.M41, WorldMatrixInternal.M42, WorldMatrixInternal.M43);
            if (position.X < 0 || position.X > RenderSize.X
                || position.Y < 0 || position.Y > RenderSize.Y)
            {
                var eventArgs = new RoutedEventArgs(OutsideClickEvent);
                RaiseEvent(eventArgs);
            }
        }

        protected internal override bool Intersects(ref Ray ray, out Vector3 intersectionPoint)
        {
            if (!IsModal)
                return base.Intersects(ref ray, out intersectionPoint);

            if (LayoutingContext == null)
            {
                intersectionPoint = Vector3.Zero;
                return false;
            }

            var virtualResolution = LayoutingContext.VirtualResolution;
            var worldmatrix = Matrix.Identity;
            
            return CollisionHelper.RayIntersectsRectangle(ref ray, ref worldmatrix, ref virtualResolution, 2, out intersectionPoint);
        }
    }
}
