// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Stride.Core.Assets.Editor;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Windows;
using Stride.PrivacyPolicy;
using Stride.LauncherApp.CrashReport;
using Stride.LauncherApp.Services;
using Stride.Metrics;
using Dispatcher = System.Windows.Threading.Dispatcher;
using Stride.Core.Packages;
using MessageBox = System.Windows.MessageBox;
using System.Diagnostics;

namespace Stride.LauncherApp
{
    /// <summary>
    /// Entry point class of the Launcher.
    /// </summary>
    public static class Launcher
    {
        internal static FileLock Mutex;
        internal static MetricsClient Metrics;

        public const string ApplicationName = "Stride Launcher";

        /// <summary>
        /// The entry point function of the launcher.
        /// </summary>
        /// <returns>The process error code to return.</returns>
        [STAThread]
        public static int Main(string[] args)
        {
            // For now, we force culture to invariant one because GNU.Gettext.GettextResourceManager.GetSatelliteAssembly crashes when Assembly.Location is null
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            var arguments = ProcessArguments(args);
            var result = ProcessAction(arguments);
            return (int)result;
        }

        /// <summary>
        /// Returns path of Launcher (we can't use Assembly.GetEntryAssembly().Location in .NET Core, especially with self-publish).
        /// </summary>
        /// <returns></returns>
        internal static string GetExecutablePath()
        {
            return Process.GetCurrentProcess().MainModule.FileName;
        }

        /// <summary>
        /// Initializes a <see cref="NugetStore"/> instance assuming the entry point assembly is located at the root of the store.
        /// </summary>
        /// <returns>A new instance of <see cref="NugetStore"/>.</returns>
        [NotNull]
        internal static NugetStore InitializeNugetStore()
        {
            NugetStore.RemoveHttpTimeout();

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
            var result = MessageBox.Show(message, "Stride", MessageBoxButton.YesNo, MessageBoxImage.Information);
            return result == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Displays an error message to the user with just an OK button.
        /// </summary>
        /// <param name="message">The message to display.</param>
        internal static void DisplayError(string message)
        {
            MessageBox.Show(message, "Stride", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static LauncherArguments ProcessArguments(string[] args)
        {
            var result = new LauncherArguments
            {
                // Default action is to run the server
                Actions = new List<LauncherArguments.ActionType> { LauncherArguments.ActionType.Run }
            };

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

                    MessageBox.Show("An instance of Stride Launcher is already running.", "Stride", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return LauncherErrorCode.ServerAlreadyRunning;
                }
            }
            catch (Exception e)
            {
                DisplayError($"Cannot start the instance of the Stride Launcher due to the following exception:\n{e.Message}");
                return LauncherErrorCode.UnknownError;
            }
        }

        private static LauncherErrorCode RunSingleInstance(bool shouldStartHidden)
        {
            try
            {
                // Only needed for Stride up to 2.x (and possibly 3.0): setup the StrideDir to make sure that it is passed to the underlying process (msbuild...etc.)
                Environment.SetEnvironmentVariable("SiliconStudioStrideDir", AppDomain.CurrentDomain.BaseDirectory);
                Environment.SetEnvironmentVariable("StrideDir", AppDomain.CurrentDomain.BaseDirectory);

                // We need to do that before starting recording metrics
                // TODO: we do not display Privacy Policy anymore from launcher, because it's either accepted from installer or shown again when a new version of GS with new Privacy Policy starts. Might want to reconsider that after the 2.0 free period
                PrivacyPolicyHelper.RestartApplication = SelfUpdater.RestartApplication;
                PrivacyPolicyHelper.EnsurePrivacyPolicyStride40();

                // Install Metrics for the launcher
                using (Metrics = new MetricsClient(CommonApps.StrideLauncherAppId))
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
                if (!UninstallHelper.CloseProcessesInPath(DisplayMessage, "Stride", path))
                    return LauncherErrorCode.UninstallCancelled; // User cancelled

                // Uninstall packages (they might have uninstall actions)
                var store = new NugetStore(path);
                foreach (var package in store.MainPackageIds.SelectMany(store.GetLocalPackages).FilterStrideMainPackages().ToList())
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
    

