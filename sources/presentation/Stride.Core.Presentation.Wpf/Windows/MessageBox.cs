// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Stride.Core.Presentation.Interop;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Windows
{
    using MessageBoxImage = Services.MessageBoxImage;

    public class MessageBox : MessageDialogBase
    {
        /// <summary>
        /// Identifies the <see cref="Image"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register(nameof(Image), typeof(ImageSource), typeof(MessageBox));

        protected MessageBox()
        {
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, (_, __) => SafeClipboard.SetDataObject(Content ?? string.Empty, true)));
        }

        public ImageSource Image
        {
            get { return (ImageSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        internal static void SetImage([NotNull] MessageBox messageBox, MessageBoxImage image)
        {
            string imageKey;
            switch (image)
            {
                case MessageBoxImage.None:
                    imageKey = null;
                    break;

                case MessageBoxImage.Error:
                    imageKey = "ImageErrorDialog";
                    break;

                case MessageBoxImage.Question:
                    imageKey = "ImageQuestionDialog";
                    break;

                case MessageBoxImage.Warning:
                    imageKey = "ImageWarningDialog";
                    break;

                case MessageBoxImage.Information:
                    imageKey = "ImageInformationDialog";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(image), image, null);
            }
            messageBox.Image = imageKey != null ? (ImageSource)messageBox.TryFindResource(imageKey) : null;
        }

        /// <summary>
        /// Displays a <see cref="MessageBox"/> an returns the <see cref="MessageBoxResult"/> depending on the user's choice.
        /// </summary>
        /// <param name="message">A <see cref="string"/> that specifies the text to display.</param>
        /// <param name="caption">A <see cref="string"/> that specifies the title bar caption to display.</param>
        /// <param name="buttons">A n enumeration of <see cref="DialogButtonInfo"/> that specifies buttons to display</param>
        /// <param name="image">A <see cref="MessageBoxImage"/> value that specifies the icon to display.</param>
        /// <returns>A <see cref="MessageBoxResult"/> value that specifies which message box button is clicked by the user.</returns>
        public static async Task<int> Show(string message, string caption, [NotNull] IEnumerable<DialogButtonInfo> buttons, MessageBoxImage image)
        {
            var buttonList = buttons.ToList();
            var messageBox = new MessageBox
            {
                Title = caption,
                Content = message,
                ButtonsSource = buttonList,
            };
            SetImage(messageBox, image);
            SetKeyBindings(messageBox, buttonList);
            await messageBox.ShowModal();
            return messageBox.ButtonResult;
        }

        internal static void SetKeyBindings(MessageBox messageBox, [NotNull] IEnumerable<DialogButtonInfo> buttons)
        {
            foreach (var button in buttons)
            {
                Key key;
                if (!Enum.TryParse(button.Key, out key))
                    continue;

                var binding = new KeyBinding(messageBox.ButtonCommand, key, ModifierKeys.Alt)
                {
                    CommandParameter = button.Result,
                    Modifiers = ModifierKeys.None, // because KeyBinding doesn't allow it in the constructor!
                };
                messageBox.InputBindings.Add(binding);
            }
        }
    }
}
