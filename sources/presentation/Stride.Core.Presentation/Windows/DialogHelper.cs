// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Threading;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Services;
using Stride.Core.Translation;

namespace Stride.Core.Presentation.Windows
{
    public static class DialogHelper
    {
        /// <summary>
        /// Don't ask again
        /// </summary>
        public static readonly string DontAskAgain = Tr._("Don't ask again");

        [NotNull]
        public static Task<MessageBoxResult> MessageBox([NotNull] IDispatcherService dispatcher, string message, string caption, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            return dispatcher.InvokeTask(() => Windows.MessageBox.Show(message, caption, button, image));
        }

        [NotNull]
        public static Task<int> MessageBox([NotNull] IDispatcherService dispatcher, string message, string caption, IEnumerable<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
        {
            return dispatcher.InvokeTask(() => Windows.MessageBox.Show(message, caption, buttons, image));
        }

        [NotNull]
        public static Task<CheckedMessageBoxResult> CheckedMessageBox([NotNull] IDispatcherService dispatcher, string message, string caption, bool? isChecked, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            return dispatcher.InvokeTask(() => Windows.CheckedMessageBox.Show(message, caption, button, image, DontAskAgain, isChecked));
        }

        [NotNull]
        public static Task<CheckedMessageBoxResult> CheckedMessageBox([NotNull] IDispatcherService dispatcher, string message, string caption, bool? isChecked, string checkboxMessage, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            return dispatcher.InvokeTask(() => Windows.CheckedMessageBox.Show(message, caption, button, image, checkboxMessage, isChecked));
        }

        [NotNull]
        public static Task<CheckedMessageBoxResult> CheckedMessageBox([NotNull] IDispatcherService dispatcher, string message, string caption, bool? isChecked, string checkboxMessage, IEnumerable<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
        {
            return dispatcher.InvokeTask(() => Windows.CheckedMessageBox.Show(message, caption, buttons, image, checkboxMessage, isChecked));
        }

        public static MessageBoxResult BlockingMessageBox([NotNull] IDispatcherService dispatcher, string message, string caption, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            return PushFrame(dispatcher, () => MessageBox(dispatcher, message, caption, button, image));
        }

        public static int BlockingMessageBox([NotNull] IDispatcherService dispatcher, string message, string caption, IEnumerable<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
        {
            return PushFrame(dispatcher, () => MessageBox(dispatcher, message, caption, buttons, image));
        }

        public static CheckedMessageBoxResult BlockingCheckedMessageBox([NotNull] IDispatcherService dispatcher, string message, string caption, bool? isChecked, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            return PushFrame(dispatcher, () => CheckedMessageBox(dispatcher, message, caption, isChecked, button, image));
        }

        public static CheckedMessageBoxResult BlockingCheckedMessageBox([NotNull] IDispatcherService dispatcher, string message, string caption, bool? isChecked, string checkboxMessage, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            return PushFrame(dispatcher, () => CheckedMessageBox(dispatcher, message, caption, isChecked, checkboxMessage, button, image));
        }

        public static CheckedMessageBoxResult BlockingCheckedMessageBox([NotNull] IDispatcherService dispatcher, string message, string caption, bool? isChecked, string checkboxMessage, IEnumerable<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
        {
            return PushFrame(dispatcher, () => CheckedMessageBox(dispatcher, message, caption, isChecked, checkboxMessage, buttons, image));
        }

        /// <summary>
        /// Create buttons corresponding to the provided button <paramref name="captions"/>
        /// </summary>
        /// <param name="captions">An enumeration of button captions.</param>
        /// <param name="defaultIndex">The 1-based index of the button that is the default button, or <c>null</c> if there is no default button.</param>
        /// <param name="cancelIndex">The 1-based index of the button that is the cancel button, or <c>null</c> if there is no cancel button.</param>
        /// <returns>An enumeration of buttons corresponding to the provided button <paramref name="captions"/>, which associated value are successived indices starting at <c>1</c>.</returns>
        /// <remarks>
        /// Index <c>0</c> is reserved for when the dialog is closed without clicking on a button.
        /// </remarks>
        [ItemNotNull, NotNull]
        public static IList<DialogButtonInfo> CreateButtons([NotNull] IEnumerable<string> captions, int? defaultIndex = null, int? cancelIndex = null)
        {
            var i = 1;
            var buttons = captions.Select(s =>
            {
                var buttonInfo = new DialogButtonInfo(s, i, defaultIndex == i, cancelIndex == i);
                i++;
                return buttonInfo;
            });
            return buttons.ToList();
        }

        private static TResult PushFrame<TResult>([NotNull] IDispatcherService dispatcher, Func<Task<TResult>> task)
        {
            return dispatcher.Invoke(() =>
            {
                var frame = new DispatcherFrame();
                var frameTask = task().ContinueWith(x => { frame.Continue = false; return x.Result; });
                ComponentDispatcher.PushModal();
                Dispatcher.PushFrame(frame);
                ComponentDispatcher.PopModal();
                return frameTask.Result;
            });
        }
    }
}
