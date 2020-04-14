// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace Xenko.PrivacyPolicy
{
    /// <summary>
    /// A helper class to manage Privacy Policy acceptance.
    /// </summary>
    internal static class PrivacyPolicyHelper
    {
        internal const string PrivacyPolicyNotLoaded = "Unable to load the End User License Agreement file.";
        private const string Xenko30Name = "Xenko-3.0";

        static PrivacyPolicyHelper()
        {
            var localMachine32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            using (var subkey = localMachine32.OpenSubKey(@"SOFTWARE\Xenko\Agreements\"))
            {
                if (subkey != null)
                {
                    var value = (string)subkey.GetValue(Xenko30Name);
                    Xenko30Accepted = value != null && value.ToLowerInvariant() == "true";
                }
            }
        }

        /// <summary>
        /// Gets whether the Privacy Policy for Xenko 3.0 has been accepted.
        /// </summary>
        internal static bool Xenko30Accepted { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="Action"/> that will restart the application.
        /// </summary>
        internal static Action RestartApplication { get; set; }

        /// <summary>
        /// Checks whether the Privacy Policy for Xenko 2.0 has been accepted or not. If not, displays a window asking for the agreement.
        /// If the user declines, the application is terminated. Otherwise, it is restarted with the same arguments.
        /// </summary>
        internal static void EnsurePrivacyPolicyXenko30()
        {
            if (RestartApplication == null)
                throw new InvalidOperationException("The RestartApplication property must be set before calling this method.");

            if (!Xenko30Accepted)
            {
                var app = new Application();
                app.Run(new PrivacyPolicyWindow(true));
                if (!Xenko30Accepted)
                {
                    MessageBox.Show("The Privacy Policy has been declined. The application will now exit.", "Xenko", MessageBoxButton.OK, MessageBoxImage.Information);
                    Environment.Exit(1);
                }
                // We restart the application after Privacy Policy acceptance.
                RestartApplication();
            }
        }

        /// <summary>
        /// Notifies that the Privacy Policy for Xenko 3.0 has been accepted.
        /// </summary>
        /// <returns><c>True</c> if the acceptance could be properly saved, <c>false</c> otherwise.</returns>
        internal static bool AcceptXenko30()
        {
            try
            {
                var localMachine32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
                using (var subkey = localMachine32.CreateSubKey(@"SOFTWARE\Xenko\Agreements\"))
                {
                    if (subkey == null)
                        return false;

                    subkey.SetValue(Xenko30Name, "True");
                    Xenko30Accepted = true;
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static bool RevokeAllPrivacyPolicy()
        {
            try
            {
                var localMachine32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
                using (var subkey = localMachine32.CreateSubKey(@"SOFTWARE\Xenko\Agreements\"))
                {
                    if (subkey == null)
                        return false;

                    foreach (var valueName in subkey.GetValueNames())
                    {
                        subkey.DeleteValue(valueName);
                    }
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
