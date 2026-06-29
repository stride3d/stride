// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Windows
{
    using MessageBoxImage = Services.MessageBoxImage;

    /// <summary>
    /// A message box that shows any number of independent check boxes below the message. Each
    /// <see cref="DialogCheckBoxInfo"/> carries its own label and check state; the same instances are read
    /// back after the dialog closes to get the user's choices.
    /// </summary>
    public class MultiCheckedMessageBox : MessageBox
    {
        /// <summary>
        /// Identifies the <see cref="CheckBoxes"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CheckBoxesProperty =
            DependencyProperty.Register(nameof(CheckBoxes), typeof(IEnumerable<DialogCheckBoxInfo>), typeof(MultiCheckedMessageBox));

        public IEnumerable<DialogCheckBoxInfo> CheckBoxes
        {
            get { return (IEnumerable<DialogCheckBoxInfo>)GetValue(CheckBoxesProperty); }
            set { SetValue(CheckBoxesProperty, value); }
        }

        /// <summary>
        /// Shows the dialog. The <paramref name="checkBoxes"/> instances are updated in place with the user's
        /// choices; the return value is the result of the button used to close it.
        /// </summary>
        [NotNull]
        public static async Task<int> Show(string message, string caption, [NotNull] IEnumerable<DialogButtonInfo> buttons, MessageBoxImage image, [NotNull] IEnumerable<DialogCheckBoxInfo> checkBoxes)
        {
            var buttonList = buttons.ToList();
            var messageBox = new MultiCheckedMessageBox
            {
                Title = caption,
                Content = message,
                ButtonsSource = buttonList,
                CheckBoxes = checkBoxes.ToList(),
            };
            SetImage(messageBox, image);
            SetKeyBindings(messageBox, buttonList);

            await messageBox.ShowModal();
            return messageBox.ButtonResult;
        }
    }
}
