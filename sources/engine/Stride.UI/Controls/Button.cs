// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.UI.Attributes;

namespace Stride.UI.Controls
{
    /// <summary>
    /// Represents a Windows button control, which reacts to the Click event.
    /// </summary>
    [DataContract(nameof(Button))]
    [DataContractMetadataType(typeof(ButtonMetadata))]
    [DebuggerDisplay("Button - Name={Name}")]
    public class Button : ButtonBase
    {
        private StretchType imageStretchType = StretchType.Uniform;
        private StretchDirection imageStretchDirection = StretchDirection.Both;
        private ISpriteProvider pressedImage;
        private ISpriteProvider notPressedImage;
        private ISpriteProvider mouseOverImage;
        private bool sizeToContent = true;

        public Button()
        {
            DrawLayerNumber += 1; // (button design image)
            Padding = new Thickness(10, 5, 10, 7);  // Warning: this must also match in ButtonMetadata

            MouseOverStateChanged += (sender, args) => InvalidateButtonImage();
        }

        /// <inheritdoc/>
        public override bool IsPressed
        {
            get { return base.IsPressed; }
            protected set
            {
                if (value == IsPressed)
                    return;

                base.IsPressed = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets the image displayed when the button is pressed.
        /// </summary>
        /// <userdoc>Image displayed when the button is pressed.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider PressedImage
        {
            get { return pressedImage; }
            set
            {
                if (pressedImage == value)
                    return;

                pressedImage = value;
                OnAspectImageInvalidated();
            }
        }

        /// <summary>
        /// Gets or sets the image displayed when the button is not pressed.
        /// </summary>
        /// <userdoc>Image displayed when the button is not pressed.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider NotPressedImage
        {
            get { return notPressedImage; }
            set
            {
                if (notPressedImage == value)
                    return;

                notPressedImage = value;
                OnAspectImageInvalidated();
            }
        }

        /// <summary>
        /// Gets or sets the image displayed when the mouse hovers over the button.
        /// </summary>
        /// <userdoc>Image displayed when the mouse hovers over the button.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider MouseOverImage
        {
            get { return mouseOverImage; }
            set
            {
                if (mouseOverImage == value)
                    return;

                mouseOverImage = value;
                OnAspectImageInvalidated();
            }
        }

        /// <summary>
        /// Gets or sets a value that describes how the button image should be stretched to fill the destination rectangle.
        /// </summary>
        /// <remarks>This property has no effect is <see cref="SizeToContent"/> is <c>true</c>.</remarks>
        /// <userdoc>Describes how the button image should be stretched to fill the destination rectangle.</userdoc>
        [DataMember]
        [Display(category: LayoutCategory)]
        [DefaultValue(StretchType.Uniform)]
        public StretchType ImageStretchType
        {
            get { return imageStretchType; }
            set
            {
                imageStretchType = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates how the button image is scaled.
        /// </summary>
        /// <remarks>This property has no effect is <see cref="SizeToContent"/> is <c>true</c>.</remarks>
        /// <userdoc>Indicates how the button image is scaled.</userdoc>
        [DataMember]
        [Display(category: LayoutCategory)]
        [DefaultValue(StretchDirection.Both)]
        public StretchDirection ImageStretchDirection
        {
            get { return imageStretchDirection; }
            set
            {
                imageStretchDirection = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets whether the size depends on the Content. The default is <c>true</c>.
        /// </summary>
        /// <userdoc>True if this button's size depends of its content, false otherwise.</userdoc>
        [DataMember]
        [Display(category: LayoutCategory)]
        [DefaultValue(true)]
        public bool SizeToContent
        {
            get { return sizeToContent; }
            set
            {
                if (sizeToContent == value)
                    return;

                sizeToContent = value;
                InvalidateMeasure();
            }
        }

        internal ISpriteProvider ButtonImageProvider => IsPressed ? PressedImage : (MouseOverState == MouseOverState.MouseOverElement && MouseOverImage != null ? MouseOverImage : NotPressedImage);

        internal Sprite ButtonImage => ButtonImageProvider?.GetSprite();

        /// <inheritdoc/>
        protected override Vector3 ArrangeOverride(Vector3 finalSizeWithoutMargins)
        {
            return sizeToContent
                ? base.ArrangeOverride(finalSizeWithoutMargins)
                : ImageSizeHelper.CalculateImageSizeFromAvailable(ButtonImage, finalSizeWithoutMargins, ImageStretchType, ImageStretchDirection, false);
        }

        /// <inheritdoc/>
        protected override Vector3 MeasureOverride(Vector3 availableSizeWithoutMargins)
        {
            return sizeToContent
                ? base.MeasureOverride(availableSizeWithoutMargins)
                : ImageSizeHelper.CalculateImageSizeFromAvailable(ButtonImage, availableSizeWithoutMargins, ImageStretchType, ImageStretchDirection, true);
        }

        /// <summary>
        /// Function triggered when one of the <see cref="PressedImage"/> and <see cref="NotPressedImage"/> images are invalidated.
        /// This function can be overridden in inherited classes.
        /// </summary>
        protected virtual void OnAspectImageInvalidated()
        {
            InvalidateButtonImage();
        }

        private void InvalidateButtonImage()
        {
            if (!sizeToContent)
                InvalidateMeasure();
        }

        private class ButtonMetadata
        {
            [DefaultThicknessValue(10, 5, 10, 7)]
            public Thickness Padding { get; }
        }
    }
}
