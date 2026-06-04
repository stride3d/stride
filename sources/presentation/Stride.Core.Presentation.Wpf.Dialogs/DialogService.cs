// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
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

        public async Task<MessageBoxResult> MessageBoxAsync(string message, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            return (MessageBoxResult)await DialogHelper.MessageBox(Dispatcher, message, ApplicationName, IDialogService.GetButtons(buttons), image);
        }

        public Task<int> MessageBoxAsync(string message, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
        {
            return DialogHelper.MessageBox(Dispatcher, message, ApplicationName, buttons, image);
        }

        public async Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            return await DialogHelper.CheckedMessageBox(Dispatcher, message, ApplicationName, isChecked, checkboxMessage, IDialogService.GetButtons(button), image);
        }

        public Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
        {
            return DialogHelper.CheckedMessageBox(Dispatcher, message, ApplicationName, isChecked, checkboxMessage, buttons, image);
        }

        public MessageBoxResult BlockingMessageBox(string message, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            return (MessageBoxResult)DialogHelper.BlockingMessageBox(Dispatcher, message, ApplicationName, IDialogService.GetButtons(buttons), image);
        }

        public int BlockingMessageBox(string message, IEnumerable<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
        {
            return DialogHelper.BlockingMessageBox(Dispatcher, message, ApplicationName, buttons, image);
        }

        public CheckedMessageBoxResult BlockingCheckedMessageBox(string message, bool? isChecked, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            return BlockingCheckedMessageBox(message, isChecked, DialogHelper.DontAskAgain, button, image);
        }

        public CheckedMessageBoxResult BlockingCheckedMessageBox(string message, bool? isChecked, string checkboxMessage, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        {
            return DialogHelper.BlockingCheckedMessageBox(Dispatcher, message, ApplicationName, isChecked, checkboxMessage, IDialogService.GetButtons(button), image);
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

        async Task<UFile> IDialogService.OpenFilePickerAsync(UDirectory initialPath, IReadOnlyList<FilePickerFilter> filters)
        {
            var dialog = CreateFileOpenModalDialog();
            dialog.AllowMultiSelection = false;
            dialog.InitialDirectory = initialPath.ToOSPath();
            if (filters is not null)
                dialog.Filters.AddRange(filters?.Select(x => new FileDialogFilter(x.Name, string.Join(';', x.Patterns))));

            var result = await dialog.ShowModal();
            return result == DialogResult.Ok
                ? dialog.FilePaths.First()
                : null;
        }

        async Task<IReadOnlyList<UFile>> IDialogService.OpenMultipleFilesPickerAsync(UDirectory initialPath, IReadOnlyList<FilePickerFilter> filters)
        {
            var dialog = CreateFileOpenModalDialog();
            dialog.AllowMultiSelection = true;
            dialog.InitialDirectory = initialPath.ToOSPath();
            if (filters is not null)
                dialog.Filters.AddRange(filters?.Select(x => new FileDialogFilter(x.Name, string.Join(';', x.Patterns))));

            var result = await dialog.ShowModal();
            return dialog.FilePaths.Select(x => (UFile)x).ToList();
        }

        async Task<UDirectory> IDialogService.OpenFolderPickerAsync(UDirectory initialPath)
        {
            var dialog = CreateFolderOpenModalDialog();
            dialog.InitialDirectory = initialPath.ToOSPath();

            var result = await dialog.ShowModal();
            return result == DialogResult.Ok
                ? dialog.Directory
                : null;
        }

        async Task<UFile> IDialogService.SaveFilePickerAsync(UDirectory initialPath, IReadOnlyList<FilePickerFilter> filters, string defaultExtension, string defaultFileName)
        {
            var dialog = CreateFileSaveModalDialog();
            dialog.DefaultExtension = defaultExtension;
            dialog.DefaultFileName = defaultFileName;
            dialog.InitialDirectory = initialPath.ToOSPath();
            if (filters is not null)
                dialog.Filters.AddRange(filters?.Select(x => new FileDialogFilter(x.Name, string.Join(';', x.Patterns))));

            var result = await dialog.ShowModal();
            return result == DialogResult.Ok
                ? dialog.FilePath
                : null;
        }
    }
}
