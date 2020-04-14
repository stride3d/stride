// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor;
using Xenko.Core.Extensions;
using Xenko.LauncherApp.Views;
using Xenko.Core.Packages;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.View;
using Xenko.Core.Presentation.Windows;

namespace Xenko.LauncherApp
{
    /// <summary>
    /// A class that manages a launcher instance, which must be single per user. It manages to show and hide windows, and keep the services alive
    /// </summary>
    internal class LauncherInstance
    {
        private IDispatcherService dispatcher;
        private LauncherWindow launcherWindow;
        private NugetStore store;
        private App app;

        public LauncherErrorCode Run(bool shouldStartHidden)
        {
            dispatcher = new DispatcherService(Dispatcher.CurrentDispatcher);

            // Note: Initialize is responsible of displaying a message box in case of error
            if (!Initialize())
                return LauncherErrorCode.ErrorWhileInitializingServer;

            app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.InitializeComponent();

            using (new WindowManager(Dispatcher.CurrentDispatcher))
            {
                dispatcher.InvokeTask(() => ApplicationEntryPoint(shouldStartHidden)).Forget();
                app.Run();
            }

            return LauncherErrorCode.Success;
        }

        internal void ShowMainWindow()
        {
            // This method can be invoked only from the dispatcher thread.
            dispatcher.EnsureAccess();

            if (launcherWindow == null)
            {
                // Create the window if we don't have it yet.
                launcherWindow = new LauncherWindow();
                launcherWindow.Initialize(store);
                launcherWindow.Closed += (s, e) => launcherWindow = null;
            }
            if (WindowManager.MainWindow == null)
            {
                // Show it if it's currently not visible
                WindowManager.ShowMainWindow(launcherWindow);
            }
            else
            {
                // Otherwise just activate it.
                if (launcherWindow.WindowState == WindowState.Minimized)
                {
                    launcherWindow.WindowState = WindowState.Normal;
                }
                launcherWindow.Activate();
            }

        }

        internal void CloseMainWindow()
        {
            // This method can be invoked only from the dispatcher thread.
            dispatcher.EnsureAccess();

            launcherWindow.Close();
        }

        internal async void ForceExit()
        {
            await Shutdown();
        }

        /// <summary>
        /// Setup the Launcher's service interface to handle IPC communications.
        /// </summary>
        private bool Initialize()
        {
            // Setup the Nuget store
            store = Launcher.InitializeNugetStore();

            return true;
        }

        private async Task Shutdown()
        {
            // Close view elements first
            launcherWindow?.Close();

            // Yield so that tasks that were awaiting can complete and the server can gracefully terminate
            await Task.Yield();

            // Terminate the server and the app at last
            app.Shutdown();
        }

        private async Task ApplicationEntryPoint(bool shouldStartHidden)
        {
            var authenticated = await CheckAndPromptCredentials();

            if (!authenticated)
                Shutdown();

            if (!shouldStartHidden)
                ShowMainWindow();
        }

        /// <summary>
        /// Ask users for his/her credentials if no session is authenticated or has expired.
        /// </summary>
        /// <returns><c>true</c> if session was validated, <c>false</c> otherwise.</returns>
        private async Task<bool> CheckAndPromptCredentials()
        {
            // This method can be invoked only from the dispatcher thread.
            dispatcher.EnsureAccess();

            // Return whether or not we're now successfully authenticated.
            return true;
        }

        private void RequestShowMainWindow()
        {
            dispatcher.EnsureAccess(false);
            dispatcher.Invoke(ShowMainWindow);
        }

        private void RequestCloseMainWindow()
        {
            dispatcher.EnsureAccess(false);
            dispatcher.Invoke(CloseMainWindow);
        }

        private bool RequestCheckAndPromptCredentials()
        {
            dispatcher.EnsureAccess(false);
            return dispatcher.InvokeTask(CheckAndPromptCredentials).Result;
        }
    }
}
