// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Xenko.Core.Assets.Editor;
using Xenko.Core.Annotations;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.Core.Windows;
using Xenko.PrivacyPolicy;
using Xenko.LauncherApp.CrashReport;
using Xenko.LauncherApp.Services;
using Xenko.Metrics;
using Dispatcher = System.Windows.Threading.Dispatcher;
using Xenko.Core.Packages;
using MessageBox = System.Windows.MessageBox;

namespace Xenko.LauncherApp
{
    /// <summary>
    /// Entry point class of the Launcher.
    /// </summary>
    public static class Launcher
    {
        internal static FileLock Mutex;
        internal static MetricsClient Metrics;

        public const string ApplicationName = "Xenko Launcher";

        /// <summary>
        /// The entry point function of the launcher.
        /// </summary>
        /// <returns>The process error code to return.</returns>
        [STAThread]
        public static int Main()
        {
            // For now, we force culture to invariant one because GNU.Gettext.GettextResourceManager.GetSatelliteAssembly crashes when Assembly.Location is null
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            var arguments = ProcessArguments();
            var result = ProcessAction(arguments);
            return (int)result;
        }

        /// <summary>
        /// Initializes a <see cref="NugetStore"/> instance assuming the entry point assembly is located at the root of the store.
        /// </summary>
        /// <returns>A new instance of <see cref="NugetStore"/>.</returns>
        [NotNull]
        internal static NugetStore InitializeNugetStore()
        {
            var thisExeDirectory = new UFile(Assembly.GetEntryAssembly().Location).GetFullDirectory().ToWindowsPath();
            var store = new NugetStore(thisExeDirectory);
            return store;
        }

        /// <summary>
        /// Displays a message to the user with OK and Cancel buttons, and returns whether the user cancelled.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <returns>True if the user answered OK, False otherwise.</returns>
        internal static bool DisplayMessage(string message)
        {
            var result = MessageBox.Show(message, "Xenko", MessageBoxButton.YesNo, MessageBoxImage.Information);
            return result == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Displays an error message to the user with just an OK button.
        /// </summary>
        /// <param name="message">The message to display.</param>
        internal static void DisplayError(string message)
        {
            MessageBox.Show(message, "Xenko", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static LauncherArguments ProcessArguments()
        {
            var result = new LauncherArguments
            {
                // Default action is to run the server
                Actions = new List<LauncherArguments.ActionType> { LauncherArguments.ActionType.Run }
            };

            // Environment.GetCommandLineArgs correctly process arguments regarding the presence of '\' and '"'
            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();

            foreach (var arg in args)
            {
                if (string.Equals(arg, "/Uninstall", StringComparison.InvariantCultureIgnoreCase))
                {
                    // No other action possible when uninstalling.
                    result.Actions.Clear();
                    result.Actions.Add(LauncherArguments.ActionType.Uninstall);
                }
            }

            return result;
        }

        private static LauncherErrorCode ProcessAction(LauncherArguments args)
        {
            var result = LauncherErrorCode.UnknownError;
            foreach (var action in args.Actions)
            {
                switch (action)
                {
                    case LauncherArguments.ActionType.Run:
                        result = TryRun();
                        break;
                    case LauncherArguments.ActionType.Uninstall:
                        result = Uninstall();
                        break;
                    default:
                        // Unknown action
                        return LauncherErrorCode.UnknownError;
                }
                if (IsError(result))
                    return result;
            }
            return result;
        }

        private static LauncherErrorCode TryRun()
        {
            try
            {
                // Ensure to create parent of lock directory.
                Directory.CreateDirectory(EditorPath.DefaultTempPath);
                using (Mutex = FileLock.TryLock(Path.Combine(EditorPath.DefaultTempPath, "launcher.lock")))
                {
                    if (Mutex != null)
                    {
                        return RunSingleInstance(false);
                    }

                    MessageBox.Show("An instance of Xenko Launcher is already running.", "Xenko", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return LauncherErrorCode.ServerAlreadyRunning;
                }
            }
            catch (Exception e)
            {
                DisplayError($"Cannot start the instance of the Xenko Launcher due to the following exception:\n{e.Message}");
                return LauncherErrorCode.UnknownError;
            }
        }

        private static LauncherErrorCode RunSingleInstance(bool shouldStartHidden)
        {
            try
            {
                // Only needed for Xenko up to 2.x (and possibly 3.0): setup the XenkoDir to make sure that it is passed to the underlying process (msbuild...etc.)
                Environment.SetEnvironmentVariable("SiliconStudioXenkoDir", AppDomain.CurrentDomain.BaseDirectory);
                Environment.SetEnvironmentVariable("XenkoDir", AppDomain.CurrentDomain.BaseDirectory);

                // We need to do that before starting recording metrics
                // TODO: we do not display Privacy Policy anymore from launcher, because it's either accepted from installer or shown again when a new version of GS with new Privacy Policy starts. Might want to reconsider that after the 2.0 free period
                PrivacyPolicyHelper.RestartApplication = SelfUpdater.RestartApplication;
                PrivacyPolicyHelper.EnsurePrivacyPolicyXenko30();

                // Install Metrics for the launcher
                using (Metrics = new MetricsClient(CommonApps.XenkoLauncherAppId))
                {
                    // HACK: force resolve the presentation assembly prior to initializing the app. This is to fix an issue with XAML themes.
                    // see issue PDX-2899
                    var txt = new Core.Presentation.Controls.TextBox();
                    GC.KeepAlive(txt); // prevent aggressive optimization from removing the line where we create the dummy TextBox.

                    var instance = new LauncherInstance();
                    return instance.Run(shouldStartHidden);
                }
            }
            catch (Exception exception)
            {
                CrashReportHelper.HandleException(Dispatcher.CurrentDispatcher, exception);
                return LauncherErrorCode.ErrorWhileRunningServer;
            }
        }

        private static LauncherErrorCode Uninstall()
        {
            try
            {
                // Kill all running processes
                var path = new UFile(Assembly.GetEntryAssembly().Location).GetFullDirectory().ToWindowsPath();
                if (!UninstallHelper.CloseProcessesInPath(DisplayMessage, "Xenko", path))
                    return LauncherErrorCode.UninstallCancelled; // User cancelled

                // Uninstall packages (they might have uninstall actions)
                var store = new NugetStore(path);
                foreach (var package in store.MainPackageIds.SelectMany(store.GetLocalPackages).FilterXenkoMainPackages().ToList())
                {
                    store.UninstallPackage(package, null).Wait();
                }

                foreach (var remainingFiles in Directory.GetFiles(path, "*.lock").Concat(Directory.GetFiles(path, "*.old")))
                {
                    try
                    {
                        File.Delete(remainingFiles);
                    }
                    catch (Exception e)
                    {
                        e.Ignore();
                    }
                }

                PrivacyPolicyHelper.RevokeAllPrivacyPolicy();

                return LauncherErrorCode.Success;
            }
            catch (Exception)
            {
                return LauncherErrorCode.ErrorWhileUninstalling;
            }
        }

        private static bool IsError(LauncherErrorCode errorCode)
        {
            return (int)errorCode < 0;
        }
    }
}
    

