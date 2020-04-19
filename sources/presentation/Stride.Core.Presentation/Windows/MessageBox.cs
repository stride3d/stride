// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
using Stride.Core.Translation;

namespace Stride.Core.Presentation.Windows
{
    using MessageBoxButton = Services.MessageBoxButton;
    using MessageBoxImage = Services.MessageBoxImage;
    using MessageBoxResult = Services.MessageBoxResult;

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

        /// <summary>
        /// Gets a new instance of <see cref="DialogButtonInfo"/> to serve as 'Cancel' button.
        /// </summary>
        /// <remarks>
        /// <see cref="DialogButtonInfo.IsCancel"/> is set to <see langword="true"/>.
        /// <see cref="DialogButtonInfo.Result"/> is set to <see cref="MessageBoxResult.Cancel"/>.</remarks>
        [NotNull]
        public static DialogButtonInfo ButtonCancel => new DialogButtonInfo
        {
            IsCancel = true,
            Result = (int)MessageBoxResult.Cancel,
            Content = "Cancel",
        };

        /// <summary>
        /// Gets a new instance of <see cref="DialogButtonInfo"/> to serve as 'No' button.
        /// </summary>
        /// <remarks>
        /// <see cref="DialogButtonInfo.Result"/> is set to <see cref="MessageBoxResult.No"/>.</remarks>
        [NotNull]
        public static DialogButtonInfo ButtonNo => new DialogButtonInfo
        {
            Result = (int)MessageBoxResult.No,
            Content = "No",
            Key = Tr._p("KeyGesture", "N"),
        };

        /// <summary>
        /// Gets a new instance of <see cref="DialogButtonInfo"/> to serve as 'OK' button.
        /// </summary>
        /// <remarks>
        /// <see cref="DialogButtonInfo.IsDefault"/> is set to <see langword="true"/>.
        /// <see cref="DialogButtonInfo.Result"/> is set to <see cref="MessageBoxResult.OK"/>.</remarks>
        [NotNull]
        public static DialogButtonInfo ButtonOK => new DialogButtonInfo
        {
            IsDefault = true,
            Result = (int)MessageBoxResult.OK,
            Content = "OK",
        };

        /// <summary>
        /// Gets a new instance of <see cref="DialogButtonInfo"/> to serve as 'Yes' button.
        /// </summary>
        /// <remarks>
        /// <see cref="DialogButtonInfo.IsDefault"/> is set to <see langword="true"/>.
        /// <see cref="DialogButtonInfo.Result"/> is set to <see cref="MessageBoxResult.Yes"/>.</remarks>
        [NotNull]
        public static DialogButtonInfo ButtonYes => new DialogButtonInfo
        {
            IsDefault = true,
            Result = (int)MessageBoxResult.Yes,
            Content = "Yes",
            Key = Tr._p("KeyGesture", "Y"),
        };

        [NotNull]
        internal static ICollection<DialogButtonInfo> GetButtons(MessageBoxButton button)
        {
            ICollection<DialogButtonInfo> buttons;
            switch (button)
            {
                case MessageBoxButton.OK:
                    var buttonOk = ButtonOK;
                    buttonOk.IsCancel = true;
                    buttons = new[] { buttonOk };
                    break;

                case MessageBoxButton.OKCancel:
                    buttons = new[] { ButtonOK, ButtonCancel };
                    break;

                case MessageBoxButton.YesNoCancel:
                    buttons = new[] { ButtonYes, ButtonNo, ButtonCancel };
                    break;

                case MessageBoxButton.YesNo:
                    var buttonNo = ButtonNo;
                    buttonNo.IsCancel = true;
                    buttons = new[] { ButtonYes, buttonNo };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(button), button, null);
            }
            return buttons;
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
        /// <param name="button">A <see cref="MessageBoxButton"/> value that specifies which button or buttons to display</param>
        /// <param name="image">A <see cref="MessageBoxImage"/> value that specifies the icon to display.</param>
        /// <returns>A <see cref="MessageBoxResult"/> value that specifies which message box button is clicked by the user.</returns>
        [NotNull]
        public static async Task<MessageBoxResult> Show(string message, string caption, MessageBoxButton button, MessageBoxImage image)
        {
            return (MessageBoxResult)await Show(message, caption, GetButtons(button), image);
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
