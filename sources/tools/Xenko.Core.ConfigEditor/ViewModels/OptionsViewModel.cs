// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.IO;
using System.Windows;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.ConfigEditor.ViewModels
{
    public class OptionsViewModel : ViewModelBase
    {
        public Options Options { get; private set; }

        public OptionsViewModel()
        {
            Options = Options.Load() ?? new Options();

            XenkoPath = Options.XenkoPath;
            XenkoConfigFilename = Options.XenkoConfigFilename;

            CheckXenkoPath();
            CheckXenkoConfigFilename();

            BrowsePathCommand = new AnonymousCommand(BrowsePath);
            BrowseConfigFileCommand = new AnonymousCommand(BrowseConfigFile);
        }

        public void SetOptionsWindow(Window window)
        {
            CloseCommand = new AnonymousCommand(window.Close);
        }

        public ICommand CloseCommand { get; private set; }
        public ICommand BrowsePathCommand { get; private set; }
        public ICommand BrowseConfigFileCommand { get; private set; }

        private void BrowsePath()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Xenko base directory",
                ShowNewFolderButton = true,
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                XenkoPath = dialog.SelectedPath;
        }

        private void BrowseConfigFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select the Xenko configuration file",
                Filter = "Xml Files (*.xml)|*.xml|All Files (*.*)|*.*",
                Multiselect = false,
                CheckFileExists = true,
            };

            if (dialog.ShowDialog() == true)
                XenkoConfigFilename = dialog.FileName;
        }

        private string xenkoPath;
        public string XenkoPath
        {
            get { return xenkoPath; }
            set
            {
                if (SetValue(ref xenkoPath, value, "XenkoPath"))
                    CheckXenkoPath();
            }
        }

        private bool isXenkoPathValid;
        public bool IsXenkoPathValid
        {
            get { return isXenkoPathValid; }
            set { SetValue(ref isXenkoPathValid, value, "IsXenkoPathValid"); }
        }

        private void CheckXenkoPath()
        {
            IsXenkoPathValid = Directory.Exists(XenkoPath);
        }

        private string xenkoConfigFilename;
        public string XenkoConfigFilename
        {
            get { return xenkoConfigFilename; }
            set
            {
                if (SetValue(ref xenkoConfigFilename, value, "XenkoConfigFilename"))
                    CheckXenkoConfigFilename();
            }
        }

        private bool isXenkoConfigFilenameValid;
        public bool IsXenkoConfigFilenameValid
        {
            get { return isXenkoConfigFilenameValid; }
            set { SetValue(ref isXenkoConfigFilenameValid, value, "IsXenkoConfigFilenameValid"); }
        }

        private void CheckXenkoConfigFilename()
        {
            if (string.IsNullOrWhiteSpace(XenkoConfigFilename))
            {
                IsXenkoConfigFilenameValid = true;
                return;
            }

            var tempFilename = XenkoConfigFilename;

            if (Path.IsPathRooted(tempFilename) == false)
                tempFilename = Path.Combine(XenkoPath, XenkoConfigFilename);

            IsXenkoConfigFilenameValid = File.Exists(tempFilename);
        }

        private ICommand acceptCommand;
        public ICommand AcceptCommand
        {
            get
            {
                if (acceptCommand == null)
                    acceptCommand = new AnonymousCommand(Accept);
                return acceptCommand;
            }
        }

        private void Accept()
        {
            if (string.IsNullOrWhiteSpace(XenkoPath))
            {
                MessageBox.Show("Invalid Xenko Path, this field must not be empty.", "Xenko Path Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (Directory.Exists(XenkoPath) == false)
            {
                string message = string.Format("Invalid Xenko Path, the directory '{0}' does not exit.", XenkoPath);
                MessageBox.Show(message, "Xenko Path Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Options.XenkoPath = XenkoPath;
            Options.XenkoConfigFilename = XenkoConfigFilename;

            Options.Save();

            var handler = OptionsChanged;
            if (handler != null)
                handler();

            CloseCommand.Execute(null); // this just closes the Options window
        }

        public event Action OptionsChanged;
    }
}
