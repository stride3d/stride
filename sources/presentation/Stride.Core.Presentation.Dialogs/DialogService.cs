// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Stride.Core.Annotations;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using MessageBoxButton = Stride.Core.Presentation.Services.MessageBoxButton;
using MessageBoxImage = Stride.Core.Presentation.Services.MessageBoxImage;
using MessageBoxResult = Stride.Core.Presentation.Services.MessageBoxResult;

namespace Stride.Core.Presentation.Dialogs
{
    public class DialogService : IDialogService2
    {
        private Action onClosedAction;

        public DialogService([NotNull] IDispatcherService dispatcher, string applicationName)
        {
            if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));

            Dispatcher = dispatcher;
            ApplicationName = applicationName;
        }

        public string ApplicationName { get; }

        protected IDispatcherService Dispatcher { get; }

        public IFileOpenModalDialog CreateFileOpenModalDialog()
        {
            return new FileOpenModalDialog(Dispatcher);
        }

        public IFolderOpenModalDialog CreateFolderOpenModalDialog()
        {
            return new FolderOpenModalDialog(Dispatcher);
        }

        public IFileSaveModalDialog CreateFileSaveModalDialog()
        {
            return new FileSaveModalDialog(Dispatcher);
        }

        public Task<MessageBoxResult> MessageBoxAsync(string message, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            return DialogHelper.MessageBox(Dispatcher, message, ApplicationName, buttons, image);
        }

        public Task<int> MessageBox(string message, IEnumerable<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
        {
            return DialogHelper.MessageBox(Dispatcher, message, ApplicationName, buttons, image);
        }

        public Task<CheckedMessageBoxResult> CheckedMessageBox(string message, bool? isChecked, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            return DialogHelper.CheckedMessageBox(Dispatcher, message, ApplicationName, isChecked, button, image);
        }

        public Task<CheckedMessageBoxResult> CheckedMessageBox(string message, bool? isChecked, string checkboxMessage, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            return DialogHelper.CheckedMessageBox(Dispatcher, message, ApplicationName, isChecked, checkboxMessage, button, image);
        }

        public Task<CheckedMessageBoxResult> CheckedMessageBox(string message, bool? isChecked, string checkboxMessage, IEnumerable<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
        {
            return DialogHelper.CheckedMessageBox(Dispatcher, message, ApplicationName, isChecked, checkboxMessage, buttons, image);
        }

        public MessageBoxResult BlockingMessageBox(string message, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            return DialogHelper.BlockingMessageBox(Dispatcher, message, ApplicationName, buttons, image);
        }

        public int BlockingMessageBox(string message, IEnumerable<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
        {
            return DialogHelper.BlockingMessageBox(Dispatcher, message, ApplicationName, buttons, image);
        }

        public CheckedMessageBoxResult BlockingCheckedMessageBox(string message, bool? isChecked, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            return DialogHelper.BlockingCheckedMessageBox(Dispatcher, message, ApplicationName, isChecked, button, image);
        }

        public CheckedMessageBoxResult BlockingCheckedMessageBox(string message, bool? isChecked, string checkboxMessage, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            return DialogHelper.BlockingCheckedMessageBox(Dispatcher, message, ApplicationName, isChecked, checkboxMessage, button, image);
        }

        public CheckedMessageBoxResult BlockingCheckedMessageBox(string message, bool? isChecked, string checkboxMessage, IEnumerable<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
        {
            return DialogHelper.BlockingCheckedMessageBox(Dispatcher, message, ApplicationName, isChecked, checkboxMessage, buttons, image);
        }

        public async Task CloseMainWindow(Action onClosed)
        {
            if (Application.Current.MainWindow is { } window)
            {
                if (window is IAsyncClosableWindow asyncClosable)
                {
                    var closed = await asyncClosable.TryClose();
                    if (closed)
                    {
                        onClosed?.Invoke();
                    }
                }
                else
                {
                    onClosedAction = onClosed;
                    window.Closing -= MainWindowClosing;
                    window.Closing += MainWindowClosing;
                    window.Closed -= MainWindowClosed;
                    window.Closed += MainWindowClosed;
                    window.Close();
                }
            }
        }

        private void MainWindowClosing(object sender, CancelEventArgs e)
        {
            ((Window)sender).Closing -= MainWindowClosing;
            if (e.Cancel)
            {
                ((Window)sender).Closed -= MainWindowClosed;
            }
        }

        private void MainWindowClosed(object sender, EventArgs e)
        {
            ((Window)sender).Closed -= MainWindowClosed;
            onClosedAction?.Invoke();
        }

        bool IDialogService.HasMainWindow => Application.Current.MainWindow is not null;

        void IDialogService.Exit(int exitCode)
        {
            if (Application.Current is { } app)
            {
                app.Shutdown(exitCode);
            }
            else
            {
                Environment.Exit(exitCode);
            }
        }

        async Task<UFile> IDialogService.OpenFilePickerAsync(UPath initialPath)
        {
            var dialog = CreateFileOpenModalDialog();
            dialog.InitialDirectory = initialPath;

            var result = await dialog.ShowModal();
            return result == DialogResult.Ok
                ? dialog.FilePaths.First()
                : null;
        }

        async Task<UDirectory> IDialogService.OpenFolderPickerAsync(UDirectory initialPath)
        {
            var dialog = CreateFolderOpenModalDialog();
            dialog.InitialDirectory = initialPath;

            var result = await dialog.ShowModal();
            return result == DialogResult.Ok
                ? dialog.Directory
                : null;
        }
    }
}
