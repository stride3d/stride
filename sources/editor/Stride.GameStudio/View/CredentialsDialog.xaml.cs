// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Net.Sockets;
using System.Windows;
using Renci.SshNet;
using Renci.SshNet.Common;
using Xenko.Core.Assets.Editor.Services;
using Xenko.GameStudio.Services;
using Xenko.Core.Translation;
using MessageBoxButton = Xenko.Core.Presentation.Services.MessageBoxButton;
using MessageBoxImage = Xenko.Core.Presentation.Services.MessageBoxImage;

namespace Xenko.GameStudio.View
{
    /// <summary>
    /// </summary>
    public partial class CredentialsDialog : ICredentialsDialog

    {
        /// <summary>
        /// Define parameterless constructor to make XAML designer happy.
        /// </summary>
        public CredentialsDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize instance of <see cref="CredentialsDialog"/> using the editor dialog service <paramref name="service"/>.
        /// </summary>
        /// <param name="service">Editor dialog service to use to display various dialogs.</param>
        public CredentialsDialog(IEditorDialogService service) : this()
        {
            // Setup our dialogs using saved settings.
            Service = service;
            Host.Text = XenkoEditorSettings.Host.GetValue();
            Port.Value = XenkoEditorSettings.Port.GetValue();
            Username.Text = XenkoEditorSettings.Username.GetValue();
            Password.Password = RemoteFacilities.Decrypt(XenkoEditorSettings.Password.GetValue());
            Location.Text = XenkoEditorSettings.Location.GetValue();
        }

        /// <summary>
        /// Are credentials valid? Meaning that we can access the remote host.
        /// </summary>
        public bool AreCredentialsValid { get; private set; }

        private CredentialError lastError;
        private IEditorDialogService Service { get; }

        /// <summary>
        /// Check if current credentials are valid.
        /// </summary>
        /// <param name="sender">Not used.</param>
        /// <param name="e">Not used.</param>
        private void OnTestSettings(object sender, RoutedEventArgs e)
        {
            CheckCredentials();
            DisplayError();
        }

        /// <summary>
        /// Display error message associated to <seealso cref="CredentialError"/>.
        /// </summary>
        private void DisplayError()
        {
            string message;
            var messageBoxImage = MessageBoxImage.Error;
            switch (lastError)
            {
                case CredentialError.None:
                    messageBoxImage = MessageBoxImage.Information;
                    message = Tr._p("Credentials", "Your credentials are correct.");
                    break;
                case CredentialError.InvalidHost:
                    message = Tr._p("Credentials", "Couldn't reach the specified host.");
                    break;
                case CredentialError.InvalidUserOrPassword:
                    message = Tr._p("Credentials", "Invalid credentials.");
                    break;
                case CredentialError.InvalidPath:
                    message = Tr._p("Credentials", "The location you specified doesn't exist.");
                    break;
                default:
                    message = Tr._p("Credentials", "An unknown error occurred.");
                    break;
            }
            Service.MessageBox(message, MessageBoxButton.OK, messageBoxImage);
        }

        /// <summary>
        /// Action executed when pressing the OK button.
        /// </summary>
        /// <param name="sender">Not used.</param>
        /// <param name="e">Not used.</param>
        private void OnOk(object sender, RoutedEventArgs e)
        {
            CheckCredentials();
            if (lastError == CredentialError.None)
            {
                XenkoEditorSettings.Host.SetValue(Host.Text);
                XenkoEditorSettings.Port.SetValue((int) Port.Value);
                XenkoEditorSettings.Username.SetValue(Username.Text);
                XenkoEditorSettings.Password.SetValue(RemoteFacilities.Encrypt(Password.Password));
                XenkoEditorSettings.Location.SetValue(Location.Text);
                XenkoEditorSettings.AskForCredentials.SetValue(CheckBox.IsChecked == null || !CheckBox.IsChecked.Value);
                XenkoEditorSettings.Save();
                AreCredentialsValid = true;
                Result = Core.Presentation.Services.DialogResult.Ok;
                Close();
            }
            else
            {
                AreCredentialsValid = false;
                DisplayError();
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Result = Core.Presentation.Services.DialogResult.Cancel;
            Close();
        }

        private void CheckCredentials()
        {
            lastError = CredentialError.None;

            var sshClient = new SshClient(RemoteFacilities.NewConnectionInfo(Host.Text, (int) Port.Value, Username.Text, Password.Password));
            try
            {
                sshClient.Connect();
                if (sshClient.IsConnected)
                {
                    var command = sshClient.CreateCommand("cd " + Location.Text);
                    // Ignore output
                    command.Execute();
                    if (!string.IsNullOrEmpty(command.Error))
                    {
                        lastError = CredentialError.InvalidPath;
                    }
                }
            }
            catch (Exception e) when (e is SshException || e is SocketException)
            {
                lastError = CredentialError.InvalidHost;
            }
            catch (SshAuthenticationException)
            {
                lastError = CredentialError.InvalidUserOrPassword;
            }
        }

        /// <summary>
        /// Sets of expected errors.
        /// </summary>
        private enum CredentialError
        {
            None,
            InvalidHost,
            InvalidUserOrPassword,
            InvalidPath
        }
    }
}
