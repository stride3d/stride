// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.IO;
using System.Windows;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModel;

namespace Stride.ConfigEditor.ViewModels
{
    public class OptionsViewModel : ViewModelBase
    {
        public Options Options { get; private set; }

        public OptionsViewModel()
        {
            Options = Options.Load() ?? new Options();

            StridePath = Options.StridePath;
            StrideConfigFilename = Options.StrideConfigFilename;

            CheckStridePath();
            CheckStrideConfigFilename();

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
                Description = "Select Stride base directory",
                ShowNewFolderButton = true,
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                StridePath = dialog.SelectedPath;
        }

        private void BrowseConfigFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select the Stride configuration file",
                Filter = "Xml Files (*.xml)|*.xml|All Files (*.*)|*.*",
                Multiselect = false,
                CheckFileExists = true,
            };

            if (dialog.ShowDialog() == true)
                StrideConfigFilename = dialog.FileName;
        }

        private string stridePath;
        public string StridePath
        {
            get { return stridePath; }
            set
            {
                if (SetValue(ref stridePath, value, "StridePath"))
                    CheckStridePath();
            }
        }

        private bool isStridePathValid;
        public bool IsStridePathValid
        {
            get { return isStridePathValid; }
            set { SetValue(ref isStridePathValid, value, "IsStridePathValid"); }
        }

        private void CheckStridePath()
        {
            IsStridePathValid = Directory.Exists(StridePath);
        }

        private string strideConfigFilename;
        public string StrideConfigFilename
        {
            get { return strideConfigFilename; }
            set
            {
                if (SetValue(ref strideConfigFilename, value, "StrideConfigFilename"))
                    CheckStrideConfigFilename();
            }
        }

        private bool isStrideConfigFilenameValid;
        public bool IsStrideConfigFilenameValid
        {
            get { return isStrideConfigFilenameValid; }
            set { SetValue(ref isStrideConfigFilenameValid, value, "IsStrideConfigFilenameValid"); }
        }

        private void CheckStrideConfigFilename()
        {
            if (string.IsNullOrWhiteSpace(StrideConfigFilename))
            {
                IsStrideConfigFilenameValid = true;
                return;
            }

            var tempFilename = StrideConfigFilename;

            if (Path.IsPathRooted(tempFilename) == false)
                tempFilename = Path.Combine(StridePath, StrideConfigFilename);

            IsStrideConfigFilenameValid = File.Exists(tempFilename);
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
            if (string.IsNullOrWhiteSpace(StridePath))
            {
                MessageBox.Show("Invalid Stride Path, this field must not be empty.", "Stride Path Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (Directory.Exists(StridePath) == false)
            {
                string message = string.Format("Invalid Stride Path, the directory '{0}' does not exit.", StridePath);
                MessageBox.Show(message, "Stride Path Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Options.StridePath = StridePath;
            Options.StrideConfigFilename = StrideConfigFilename;

            Options.Save();

            var handler = OptionsChanged;
            if (handler != null)
                handler();

            CloseCommand.Execute(null); // this just closes the Options window
        }

        public event Action OptionsChanged;
    }
}
