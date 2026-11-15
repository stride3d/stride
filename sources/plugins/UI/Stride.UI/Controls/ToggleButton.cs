// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using System.Diagnostics;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.UI.Attributes;
using Stride.UI.Events;

namespace Stride.UI.Controls
{
    /// <summary>
    /// Represent a UI toggle button. A toggle but can have two or three states depending on the <see cref="IsThreeState"/> property.
    /// </summary>
    [DataContract(nameof(ToggleButton))]
    [DataContractMetadataType(typeof(ToggleButtonMetadata))]
    [DebuggerDisplay("ToggleButton - Name={Name}")]
    public class ToggleButton : ButtonBase
    {
        /// <summary>
        /// Function triggered when one of the <see cref="CheckedImage"/>, <see cref="IndeterminateImage"/> and <see cref="UncheckedImage"/> images are invalidated.
        /// This function can be overridden in inherited classes.
        /// </summary>
        protected virtual void OnToggleImageInvalidated()
        {
        }

        private bool isThreeState;

        private ToggleState state = ToggleState.UnChecked;
        private ISpriteProvider checkedImage;
        private ISpriteProvider indeterminateImage;
        private ISpriteProvider uncheckedImage;
        private ISpriteProvider checkedMouseOverImage;
        private ISpriteProvider indeterminateMouseOverImage;
        private ISpriteProvider uncheckedMouseOverImage;

        public ToggleButton()
        {
            DrawLayerNumber += 1; // (toggle design image)
            Padding = new Thickness(10, 5, 10, 7);  // Warning: this must also match in ToggleButtonMetadata

            MouseOverStateChanged += (sender, args) => InvalidateToggleImage();
        }

        /// <summary>
        /// Gets or sets the image displayed when the button is checked.
        /// </summary>
        /// <userdoc>The image displayed when the button is checked.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider CheckedImage
        {
            get { return checkedImage; }
            set
            {
                if (checkedImage == value)
                    return;

                checkedImage = value;
                OnToggleImageInvalidated();
            }
        }

        /// <summary>
        /// Gets or sets the image displayed when the button is unchecked.
        /// </summary>
        /// <userdoc>The image displayed when the button is unchecked.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider UncheckedImage
        {
            get { return uncheckedImage; }
            set
            {
                if (uncheckedImage == value)
                    return;

                uncheckedImage = value;
                OnToggleImageInvalidated();
            }
        }

        /// <summary>
        /// Gets or sets the image displayed when the button state is undeterminate.
        /// </summary>
        /// <userdoc>The image displayed when the button state is undeterminate.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider IndeterminateImage
        {
            get { return indeterminateImage; }
            set
            {
                if (indeterminateImage == value)
                    return;

                indeterminateImage = value;
                OnToggleImageInvalidated();
            }
        }

        /// <summary>
        /// Gets or sets the image displayed when the mouse hovers over the button and it is checked.
        /// </summary>
        /// <userdoc>The image displayed when the mouse hovers over the button and it is checked.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider CheckedMouseOverImage
        {
            get { return checkedMouseOverImage; }
            set
            {
                if (checkedMouseOverImage == value)
                    return;

                checkedMouseOverImage = value;
                OnToggleImageInvalidated();
            }
        }

        /// <summary>
        /// Gets or sets the image displayed when the mouse hovers over the button and it is unchecked.
        /// </summary>
        /// <userdoc>The image displayed when the mouse hovers over the button and it is unchecked.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider UncheckedMouseOverImage
        {
            get { return uncheckedMouseOverImage; }
            set
            {
                if (uncheckedMouseOverImage == value)
                    return;

                uncheckedMouseOverImage = value;
                OnToggleImageInvalidated();
            }
        }

        /// <summary>
        /// Gets or sets the image displayed when the mouse hovers over the button and its state is indeterminate.
        /// </summary>
        /// <userdoc>The image displayed when the mouse hovers over the button and its state is indeterminate.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider IndeterminateMouseOverImage
        {
            get { return indeterminateMouseOverImage; }
            set
            {
                if (indeterminateMouseOverImage == value)
                    return;

                indeterminateMouseOverImage = value;
                OnToggleImageInvalidated();
            }
        }

        /// <summary>
        /// Gets the sprite provider for the current toggle state, taking into account mouse over state.
        /// </summary>
        internal ISpriteProvider ToggleButtonImageProvider
        {
            get
            {
                var isMouseOver = MouseOverState == MouseOverState.MouseOverElement;
                
                switch (State)
                {
                    case ToggleState.Checked:
                        return (isMouseOver && CheckedMouseOverImage != null) ? CheckedMouseOverImage : CheckedImage;
                    case ToggleState.Indeterminate:
                        return (isMouseOver && IndeterminateMouseOverImage != null) ? IndeterminateMouseOverImage : IndeterminateImage;
                    case ToggleState.UnChecked:
                        return (isMouseOver && UncheckedMouseOverImage != null) ? UncheckedMouseOverImage : UncheckedImage;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Gets the sprite for the current toggle state, taking into account mouse over state.
        /// </summary>
        internal Sprite ToggleButtonImage => ToggleButtonImageProvider?.GetSprite();

        /// <summary>
        /// Gets or set the color used to tint the image. Default value is White.
        /// </summary>
        /// <remarks>The initial image color is multiplied by this color.</remarks>
        /// <userdoc>The color used to tint the image. The default value is white.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        public Color Color { get; set; } = Color.White;

        /// <summary>
        /// Determines whether the control supports two or three states.
        /// </summary>
        /// <remarks>Setting <see cref="IsThreeState"/> to false changes the <see cref="State"/> of the toggle button if currently set to <see cref="ToggleState.Indeterminate"/></remarks>
        /// <userdoc>True if the control support three states, false otherwise.</userdoc>
        [DataMember]
        [Display(category: BehaviorCategory)]
        [DefaultValue(false)]
        public bool IsThreeState
        {
            get { return isThreeState; }
            set
            {
                if (value == false && State == ToggleState.Indeterminate)
                    GoToNextState();

                isThreeState = value;
            }
        }

        /// <summary>
        /// Gets or sets the state of the <see cref="ToggleButton"/>
        /// </summary>
        /// <remarks>Setting the state of the toggle button to <see cref="ToggleState.Indeterminate"/> sets <see cref="IsThreeState"/> to true.</remarks>
        /// <userdoc>The state of the button.</userdoc>
        [DataMember]
        [Display(category: BehaviorCategory)]
        [DefaultValue(ToggleState.UnChecked)]
        public ToggleState State
        {
            get { return state; }
            set
            {
                if (state == value)
                    return;

                state = value;

                switch (value)
                {
                    case ToggleState.Checked:
                        RaiseEvent(new RoutedEventArgs(CheckedEvent));
                        break;
                    case ToggleState.Indeterminate:
                        IsThreeState = true;
                        RaiseEvent(new RoutedEventArgs(IndeterminateEvent));
                        break;
                    case ToggleState.UnChecked:
                        RaiseEvent(new RoutedEventArgs(UncheckedEvent));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value));
                }
            }
        }

        /// <summary>
        /// Occurs when a <see cref="ToggleButton"/> is checked.
        /// </summary>
        /// <remarks>A checked event is bubbling</remarks>
        public event EventHandler<RoutedEventArgs> Checked
        {
            add { AddHandler(CheckedEvent, value); }
            remove { RemoveHandler(CheckedEvent, value); }
        }

        /// <summary>
        /// Occurs when a <see cref="ToggleButton"/> is Indeterminate.
        /// </summary>
        /// <remarks>A Indeterminate event is bubbling</remarks>
        public event EventHandler<RoutedEventArgs> Indeterminate
        {
            add { AddHandler(IndeterminateEvent, value); }
            remove { RemoveHandler(IndeterminateEvent, value); }
        }

        /// <summary>
        /// Occurs when a <see cref="ToggleButton"/> is Unchecked.
        /// </summary>
        /// <remarks>A Unchecked event is bubbling</remarks>
        public event EventHandler<RoutedEventArgs> Unchecked
        {
            add { AddHandler(UncheckedEvent, value); }
            remove { RemoveHandler(UncheckedEvent, value); }
        }

        /// <summary>
        /// Move the state of the toggle button to the next state. States order is: Unchecked -> Checked [-> Indeterminate] -> Unchecked -> ...
        /// </summary>
        protected void GoToNextState()
        {
            switch (State)
            {
                case ToggleState.Checked:
                    State = IsThreeState ? ToggleState.Indeterminate : ToggleState.UnChecked;
                    break;
                case ToggleState.Indeterminate:
                    State = ToggleState.UnChecked;
                    break;
                case ToggleState.UnChecked:
                    State = ToggleState.Checked;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Identifies the <see cref="Checked"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> CheckedEvent = EventManager.RegisterRoutedEvent<RoutedEventArgs>(
            "Checked",
            RoutingStrategy.Bubble,
            typeof(ToggleButton));

        /// <summary>
        /// Identifies the <see cref="Indeterminate"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> IndeterminateEvent = EventManager.RegisterRoutedEvent<RoutedEventArgs>(
            "Indeterminate",
            RoutingStrategy.Bubble,
            typeof(ToggleButton));

        /// <summary>
        /// Identifies the <see cref="Unchecked"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> UncheckedEvent = EventManager.RegisterRoutedEvent<RoutedEventArgs>(
            "Unchecked",
            RoutingStrategy.Bubble,
            typeof(ToggleButton));

        protected override void OnClick(RoutedEventArgs args)
        {
            base.OnClick(args);

            GoToNextState();
        }

        private void InvalidateToggleImage()
        {
            OnToggleImageInvalidated();
        }

        private class ToggleButtonMetadata
        {
            [DefaultThicknessValue(10, 5, 10, 7)]
            public Thickness Padding { get; }
        }
    }
}
