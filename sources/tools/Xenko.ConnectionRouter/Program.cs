// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mono.Options;
using Xenko.Core.Diagnostics;
using Xenko.Core.Windows;
using Xenko.Engine.Network;

namespace Xenko.ConnectionRouter
{
    partial class Program
    {
        private static bool ConsoleVisible = false;

        static int Main(string[] args)
        {
            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            var showHelp = false;
            var windowsPhonePortMapping = false;
            int exitCode = 0;
            string logFileName = "routerlog.txt";

            var p = new OptionSet
                {
                    "Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp) All Rights Reserved",
                    "Xenko Router Server - Version: "
                    +
                    String.Format(
                        "{0}.{1}.{2}",
                        typeof(Program).Assembly.GetName().Version.Major,
                        typeof(Program).Assembly.GetName().Version.Minor,
                        typeof(Program).Assembly.GetName().Version.Build) + string.Empty,
                    string.Format("Usage: {0} command [options]*", exeName),
                    string.Empty,
                    "=== Options ===",
                    string.Empty,
                    { "h|help", "Show this message and exit", v => showHelp = v != null },
                    { "log-file=", "Log build in a custom file (default: routerlog.txt).", v => logFileName = v },
                    { "register-windowsphone-portmapping", "Register Windows Phone IpOverUsb port mapping", v => windowsPhonePortMapping = true },
                };

            try
            {
                var commandArgs = p.Parse(args);
                if (showHelp)
                {
                    p.WriteOptionDescriptions(Console.Out);
                    return 0;
                }

                // Make sure path exists
                if (commandArgs.Count > 0)
                    throw new OptionException("This command expect no additional arguments", "");

                if (windowsPhonePortMapping)
                {
                    WindowsPhoneTracker.RegisterWindowsPhonePortMapping();
                    return 0;
                }

                SetupTrayIcon(logFileName);

                // Enable file logging
                if (!string.IsNullOrEmpty(logFileName))
                {
                    var fileLogListener = new TextWriterLogListener(File.Open(logFileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                    GlobalLogger.GlobalMessageLogged += fileLogListener;
                }

                // TODO: Lock will be only for this folder but it should be shared across OS
                using (var mutex = FileLock.TryLock("connectionrouter.lock"))
                {
                    if (mutex == null)
                    {
                        Console.WriteLine("Another instance of Xenko Router is already running");
                        return -1;
                    }

                    var router = new Router();

                    // Start router (in listen server mode)
                    router.Listen(RouterClient.DefaultPort).Wait();

                    // Start Android management thread
                    new Thread(() => AndroidTracker.TrackDevices(router)) { IsBackground = true }.Start();

                    // Start Windows Phone management thread
                    new Thread(() => WindowsPhoneTracker.TrackDevices(router)) { IsBackground = true }.Start();

                    //Start iOS device discovery and proxy launcher
                    //Currently this is used only internally for QA testing... as we cannot attach the debugger from windows for normal usages..
                    if (IosTracker.CanProxy())
                    {
                        new Thread(async () =>
                        {
                            var iosTracker = new IosTracker(router);
                            await iosTracker.TrackDevices();
                        }) { IsBackground = true }.Start();
                    }

                    // Start WinForms loop
                    System.Windows.Forms.Application.Run();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}: {1}", exeName, e);
                if (e is OptionException)
                    p.WriteOptionDescriptions(Console.Out);
                exitCode = 1;
            }

            return exitCode;
        }

        private static string FormatLog(ILogMessage message)
        {
            var builder = new StringBuilder();
            builder.Append("[");
            builder.Append(message.Module);
            builder.Append("] ");
            builder.Append(message.Type.ToString().ToLowerInvariant()).Append(": ");
            builder.Append(message.Text);
            return builder.ToString();
        }

        private static void SetupTrayIcon(string logFileName)
        {
            // Create tray icon
            var components = new System.ComponentModel.Container();

            var notifyIcon = new System.Windows.Forms.NotifyIcon(components);
            notifyIcon.Text = "Xenko Connection Router";
            notifyIcon.Icon = Properties.Resources.Logo;
            notifyIcon.Visible = true;
            notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu();

            if (!string.IsNullOrEmpty(logFileName))
            {
                var showLogMenuItem = new System.Windows.Forms.MenuItem("Show &Log");
                showLogMenuItem.Click += (sender, args) => OnShowLogClick(logFileName);
                notifyIcon.ContextMenu.MenuItems.Add(showLogMenuItem);

                notifyIcon.BalloonTipClicked += (sender, args) => OnShowLogClick(logFileName);
            }

            var openConsoleMenuItem = new System.Windows.Forms.MenuItem("Open Console");
            openConsoleMenuItem.Click += (sender, args) => OnOpenConsoleClick((System.Windows.Forms.MenuItem)sender);
            notifyIcon.ContextMenu.MenuItems.Add(openConsoleMenuItem);

            var exitMenuItem = new System.Windows.Forms.MenuItem("E&xit");
            exitMenuItem.Click += (sender, args) => OnExitClick();
            notifyIcon.ContextMenu.MenuItems.Add(exitMenuItem);

            GlobalLogger.GlobalMessageLogged += (logMessage) =>
            {
                // Log only warning, errors and more
                if (logMessage.Type < LogMessageType.Warning)
                    return;

                var toolTipIcon = logMessage.Type < LogMessageType.Error ? System.Windows.Forms.ToolTipIcon.Warning : System.Windows.Forms.ToolTipIcon.Error;

                // Display notification (for two second)
                notifyIcon.ShowBalloonTip(2000, "Xenko Connection Router", logMessage.ToString(), toolTipIcon);
            };

            System.Windows.Forms.Application.ApplicationExit += (sender, e) =>
            {
                notifyIcon.Visible = false;
                notifyIcon.Icon = null;
                notifyIcon.Dispose();
            };
        }

        private static void OnOpenConsoleClick(System.Windows.Forms.MenuItem menuItem)
        {
            menuItem.Enabled = false;

            // Check if not already done
            if (ConsoleVisible)
                return;
            ConsoleVisible = true;

            // Show console
            ConsoleLogListener.ShowConsole();

            // Enable console logging
            var consoleLogListener = new ConsoleLogListener { LogMode = ConsoleLogMode.Always };
            GlobalLogger.GlobalMessageLogged += consoleLogListener;
        }

        private static void OnShowLogClick(string logFileName)
        {
            System.Diagnostics.Process.Start(logFileName);
        }

        private static void OnExitClick()
        {
            System.Windows.Forms.Application.Exit();
        }
    }
}
