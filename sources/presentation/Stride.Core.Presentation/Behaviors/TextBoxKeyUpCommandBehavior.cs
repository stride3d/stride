// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Controls;

namespace Stride.Core.Presentation.Behaviors
{
    public class TextBoxKeyUpCommandBehavior : Behavior<TextBoxBase>
    {
        /// <summary>
        /// Identifies the <see cref="Command"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command", typeof(ICommand), typeof(TextBoxKeyUpCommandBehavior));

        /// <summary>
        /// Identifies the <see cref="Key"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty KeyProperty = DependencyProperty.Register("Key", typeof(Key), typeof(TextBoxKeyUpCommandBehavior), new PropertyMetadata(Key.Enter));

        /// <summary>
        /// Gets or sets the command to invoke.
        /// </summary>
        public ICommand Command { get { return (ICommand)GetValue(CommandProperty); } set { SetValue(CommandProperty, value); } }

        /// <summary>
        /// Gets or sets the key that should trigger this behavior. The default is <see cref="System.Windows.Input.Key.Enter"/>.
        /// </summary>
        public Key Key { get { return (Key)GetValue(KeyProperty); } set { SetValue(KeyProperty, value); } }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.KeyUp += KeyUp;
        }

        /// <inheritdoc/>
        protected override void OnDetaching()
        {
            AssociatedObject.KeyUp -= KeyUp;
            base.OnDetaching();
        }

        private void KeyUp(object sender, [NotNull] KeyEventArgs e)
        {
            if (e.Key != Key || AssociatedObject.HasChangesToValidate)
            {
                return;
            }

            Command?.Execute(AssociatedObject.Text);
        }
    }
}
