// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using Stride.Assets.Editor.ViewModels.SpriteEditor;
using Stride.Core.Mathematics;

namespace Stride.Assets.Presentation.View.Behaviors
{
    /// <summary>
    /// Gives a click on the image of the sprite editor to the pixel tools (magic wand, color key picker) as a
    /// <see cref="SpriteImagePick"/>: the position of the mouse in the image, and whether Ctrl adds the sprite to the region.
    /// </summary>
    public sealed class SpriteImagePickBehavior : Behavior<FrameworkElement>
    {
        /// <summary>
        /// Identifies the <see cref="Command"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(SpriteImagePickBehavior));

        /// <summary>
        /// Gets or sets the command that receives the <see cref="SpriteImagePick"/>.
        /// </summary>
        public ICommand Command { get => (ICommand)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseDown += OnPreviewMouseDown;
        }

        /// <inheritdoc/>
        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseDown -= OnPreviewMouseDown;
            base.OnDetaching();
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // The position in the element, before its zoom (layout transform): in pixels of the image
            var position = e.GetPosition(AssociatedObject);
            var pick = new SpriteImagePick(new Vector2((float)position.X, (float)position.Y), Keyboard.Modifiers.HasFlag(ModifierKeys.Control));
            var command = Command;
            if (command != null && command.CanExecute(pick))
                command.Execute(pick);
        }
    }
}
