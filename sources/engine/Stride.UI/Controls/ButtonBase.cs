// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using System.Diagnostics;

using Stride.Core;
using Stride.UI.Events;

namespace Stride.UI.Controls
{
    /// <summary>
    /// Represents the base primitive for all the button-like controls
    /// </summary>
    [DataContract(nameof(ButtonBase))]
    [DataContractMetadataType(typeof(ButtonBaseMetadata))]
    [DebuggerDisplay("ButtonBase - Name={Name}")]
    [Display(category: InputCategory)]
    public abstract class ButtonBase : ContentControl
    {
        static ButtonBase()
        {
            EventManager.RegisterClassHandler(typeof(ButtonBase), ClickEvent, ClickClassHandler);
        }

        /// <summary>
        /// Create an instance of button.
        /// </summary>
        protected ButtonBase()
        {
            CanBeHitByUser = true;  // Warning: this must also match in ButtonBaseMetadata
        }

        /// <summary>
        /// Gets or sets when the Click event occurs.
        /// </summary>
        /// <userdoc>Indicates when the click event occurs.</userdoc>
        [DataMember]
        [Display(category: BehaviorCategory)]
        [DefaultValue(ClickMode.Release)]
        public ClickMode ClickMode { get; set; } = ClickMode.Release;

        /// <summary>
        /// Gets a value that indicates whether the button is currently down.
        /// </summary>
        [DataMemberIgnore]
        public virtual bool IsPressed { get; protected set; }

        /// <summary>
        /// Occurs when a <see cref="Button"/> is clicked.
        /// </summary>
        /// <remarks>A click event is bubbling</remarks>
        public event EventHandler<RoutedEventArgs> Click
        {
            add { AddHandler(ClickEvent, value); }
            remove { RemoveHandler(ClickEvent, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Click"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> ClickEvent = EventManager.RegisterRoutedEvent<RoutedEventArgs>(
            "Click",
            RoutingStrategy.Bubble,
            typeof(Button));


        protected override void OnTouchDown(TouchEventArgs args)
        {
            base.OnTouchDown(args);

            IsPressed = true;

            if (ClickMode == ClickMode.Press)
                RaiseEvent(new RoutedEventArgs(ClickEvent));
        }

        protected override void OnTouchUp(TouchEventArgs args)
        {
            base.OnTouchUp(args);

            if (IsPressed && ClickMode == ClickMode.Release)
                RaiseEvent(new RoutedEventArgs(ClickEvent));

            IsPressed = false;
        }

        protected override void OnTouchLeave(TouchEventArgs args)
        {
            base.OnTouchLeave(args);

            IsPressed = false;
        }

        /// <summary>
        /// The class handler of the event <see cref="Click"/>.
        /// This method can be overridden in inherited classes to perform actions common to all instances of a class.
        /// </summary>
        /// <param name="args">The arguments of the event</param>
        protected virtual void OnClick(RoutedEventArgs args)
        {

        }

        private static void ClickClassHandler(object sender, RoutedEventArgs args)
        {
            var buttonBase = (ButtonBase)sender;

            buttonBase.OnClick(args);
        }

        private class ButtonBaseMetadata
        {
            [DefaultValue(true)]
            public bool CanBeHitByUser { get; set; }
        }
    }
}
