// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Behaviors
{
    /// <summary>
    /// This behavior allows more convenient editing of the value of a char using a TextBox.
    /// </summary>
    public class CharInputBehavior : Behavior<TextBox>
    {
        private bool updatingText;

        protected override void OnAttached()
        {
            AssociatedObject.TextChanged += TextChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.TextChanged -= TextChanged;
        }

        private void TextChanged(object sender, [NotNull] TextChangedEventArgs e)
        {
            if (updatingText)
                return;

            char newChar = default(char);
            foreach (var change in e.Changes.Where(change => change.AddedLength > 0))
            {
                newChar = AssociatedObject.Text[change.Offset + change.AddedLength - 1];
            }
            if (newChar != default(char))
            {
                updatingText = true;
                AssociatedObject.Text = newChar.ToString(CultureInfo.InvariantCulture);
                updatingText = false;
            }

            // Update the binding source on each change
            var expression = AssociatedObject.GetBindingExpression(TextBox.TextProperty);
            expression?.UpdateSource();
        }
    }
}
