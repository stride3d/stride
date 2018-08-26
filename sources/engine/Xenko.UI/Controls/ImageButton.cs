// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;

namespace Xenko.UI.Controls
{
    /// <summary>
    /// A <see cref="Button"/> whose <see cref="ContentControl.Content"/> are the <see cref="Button.PressedImage"/> and <see cref="Button.NotPressedImage"/>.
    /// </summary>
    [DebuggerDisplay("ImageButton - Name={Name}")]
    [Obsolete("Use Button with SizeToContent set to false.")]
    public class ImageButton : Button
    {
        private readonly ImageElement contentImageElement = new ImageElement();

        public ImageButton()
        {
            Padding = Thickness.UniformCuboid(0);
            base.Content = contentImageElement;

            MouseOverStateChanged += (sender, args) => UpdateContentImage();
        }

        protected override void OnAspectImageInvalidated()
        {
            UpdateContentImage();
        }

        private void UpdateContentImage()
        {
            contentImageElement.Source = ButtonImageProvider;
        }

        /// <summary>
        /// The current content of the <see cref="ImageButton"/>, that is the current image used.
        /// </summary>
        /// <remarks>The <see cref="Content"/> of a <see cref="ImageButton"/> is determined by its state (pressed/not pressed) and the value of
        /// <see cref="Button.PressedImage"/> and <see cref="Button.NotPressedImage"/>. 
        /// The <see cref="Content"/> cannot be set manually by the user.</remarks>
        /// <exception cref="InvalidOperationException">The user tried to modify the <see cref="ImageButton"/> content.</exception>
        public override UIElement Content
        {
            set { throw new InvalidOperationException("The content of an ImageButton cannot be modified by the user."); }
        }

        public override bool IsPressed
        {
            get { return base.IsPressed; }
            protected set
            {
                if (value == IsPressed)
                    return;

                base.IsPressed = value;

                UpdateContentImage();
            }
        }
    }
}
