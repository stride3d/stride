// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mono.Options;
using Stride.Core.Diagnostics;
using Stride.Core.Windows;
using Stride.Engine.Network;

namespace Stride.ConnectionRouter
{
    partial class Program
    {
        private static bool ConsoleVisible = false;

        static int Main(string[] args)
        {
            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            var showHelp = false;
            int exitCode = 0;
            string logFileName = "routerlog.txt";

            var p = new OptionSet
                {
                    "Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp) All Rights Reserved",
                    "Stride Router Server - Version: "
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
                        Console.WriteLine("Another instance of Stride Router is already running");
                        return -1;
                    }

                    var router = new Router();

                    // Start router (in listen server mode)
                    router.Listen(RouterClient.DefaultPort).Wait();

                    // Start Android management thread
                    new Thread(() => AndroidTracker.TrackDevices(router)) { IsBackground = true }.Start();

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

                    SetupTrayIcon(logFileName);

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
            notifyIcon.Text = "Stride Connection Router";
            notifyIcon.Icon = Properties.Resources.Logo;
            notifyIcon.Visible = true;
            var contextMenu = new System.Windows.Forms.ContextMenuStrip(components);

            if (!string.IsNullOrEmpty(logFileName))
            {
                var showLogMenuItem = new System.Windows.Forms.ToolStripMenuItem("Show &Log");
                showLogMenuItem.Click += (sender, args) => OnShowLogClick(logFileName);
                contextMenu.Items.Add(showLogMenuItem);

                notifyIcon.BalloonTipClicked += (sender, args) => OnShowLogClick(logFileName);
            }

            var openConsoleMenuItem = new System.Windows.Forms.ToolStripMenuItem("Open Console");
            openConsoleMenuItem.Click += (sender, args) => OnOpenConsoleClick((System.Windows.Forms.ToolStripMenuItem)sender);
            contextMenu.Items.Add(openConsoleMenuItem);

            var exitMenuItem = new System.Windows.Forms.ToolStripMenuItem("E&xit");
            exitMenuItem.Click += (sender, args) => OnExitClick();
            contextMenu.Items.Add(exitMenuItem);

            notifyIcon.ContextMenuStrip = contextMenu;

            GlobalLogger.GlobalMessageLogged += (logMessage) =>
            {
                // Log only warning, errors and more
                if (logMessage.Type < LogMessageType.Warning)
                    return;

                var toolTipIcon = logMessage.Type < LogMessageType.Error ? System.Windows.Forms.ToolTipIcon.Warning : System.Windows.Forms.ToolTipIcon.Error;

                // Display notification (for two second)
                notifyIcon.ShowBalloonTip(2000, "Stride Connection Router", logMessage.ToString(), toolTipIcon);
            };

            System.Windows.Forms.Application.ApplicationExit += (sender, e) =>
            {
                notifyIcon.Visible = false;
                notifyIcon.Icon = null;
                notifyIcon.Dispose();
            };
        }

        private static void OnOpenConsoleClick(System.Windows.Forms.ToolStripMenuItem menuItem)
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
