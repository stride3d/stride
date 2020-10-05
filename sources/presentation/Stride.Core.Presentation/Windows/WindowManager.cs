// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Extensions;
using Stride.Core.Presentation.Interop;

namespace Stride.Core.Presentation.Windows
{
    /// <summary>
    /// A singleton class to manage the windows of an application and their relation to each other. It introduces the concept of blocking window,
    /// which can block the main window of the application but does not interact with modal windows.
    /// </summary>
    public class WindowManager : IDisposable
    {
        // TODO: this list should be completely external
        private static readonly string[] DebugWindowTypeNames =
        {
            // WPF adorners introduced in Visual Studio 2015 Update 2
            "Microsoft.XamlDiagnostics.WpfTap",
            // WPF Inspector
            "ChristianMoser.WpfInspector",
            // Snoop
            "Snoop.SnoopUI",
        };

        private static readonly List<WindowInfo> ModalWindowsList = new List<WindowInfo>();
        private static readonly List<WindowInfo> BlockingWindowsList = new List<WindowInfo>();
        private static readonly HashSet<WindowInfo> AllWindowsList = new HashSet<WindowInfo>();

        // This must remains a field to prevent garbage collection!
        private static NativeHelper.WinEventDelegate winEventProc;
        private static IntPtr hook;
        private static Dispatcher dispatcher;
        private static bool initialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowManager"/> class.
        /// </summary>
        public WindowManager([NotNull] Dispatcher dispatcher)
        {
            if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
            if (initialized) throw new InvalidOperationException("An instance of WindowManager is already existing.");

            initialized = true;
            winEventProc = WinEventProc;
            WindowManager.dispatcher = dispatcher;
            uint processId = (uint)Process.GetCurrentProcess().Id;
            hook = NativeHelper.SetWinEventHook(NativeHelper.EVENT_OBJECT_SHOW, NativeHelper.EVENT_OBJECT_HIDE, IntPtr.Zero, winEventProc, processId, 0, NativeHelper.WINEVENT_OUTOFCONTEXT);
            if (hook == IntPtr.Zero)
                throw new InvalidOperationException("Unable to initialize the window manager.");

            Logger.Info($"{nameof(WindowManager)} initialized");
        }

#if DEBUG // Use a logger result for debugging
        public static Logger Logger { get; } = new LoggerResult();
#else
        public static Logger Logger { get; } = GlobalLogger.GetLogger(nameof(WindowManager));
#endif

        /// <summary>
        /// Gets the current main window.
        /// </summary>
        public static WindowInfo MainWindow { get; private set; }

        /// <summary>
        /// Gets the collection of currently visible modal windows.
        /// </summary>
        public static IReadOnlyList<WindowInfo> ModalWindows => ModalWindowsList;

        /// <summary>
        /// Gets the collection of currently visible blocking windows.
        /// </summary>
        public static IReadOnlyList<WindowInfo> BlockingWindows => BlockingWindowsList;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!NativeHelper.UnhookWinEvent(hook))
                throw new InvalidOperationException("An error occurred while disposing the window manager.");

            hook = IntPtr.Zero;
            winEventProc = null;
            dispatcher = null;
            MainWindow = null;
            AllWindowsList.Clear();
            ModalWindowsList.Clear();
            BlockingWindowsList.Clear();

            Logger.Info($"{nameof(WindowManager)} disposed");
            initialized = false;
        }

        /// <summary>
        /// Shows the given window as the main window of the application. It is mandatory to use this method for the main window to use features of the <see cref="WindowManager"/>.
        /// </summary>
        /// <param name="window">The main window to show.</param>
        public static void ShowMainWindow([NotNull] Window window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            CheckDispatcher();

            if (MainWindow != null)
            {
                var message = "This application already has a main window.";
                Logger.Error(message);
                throw new InvalidOperationException(message);
            }
            Logger.Info($"Main window showing. ({window})");

            MainWindow = new WindowInfo(window);
            AllWindowsList.Add(MainWindow);

            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();
        }

        /// <summary>
        /// Shows the given window as blocking window. A blocking window will always block the main window of the application, even if shown before it, but does not
        /// affect modal windows. However it can still be blocked by them..
        /// </summary>
        /// <param name="window">The blocking window to show.</param>
        public static void ShowBlockingWindow([NotNull] Window window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            CheckDispatcher();

            var windowInfo = new WindowInfo(window) { IsBlocking = true };
            if (BlockingWindowsList.Contains(windowInfo))
                throw new InvalidOperationException("This window has already been shown as blocking.");

            window.Owner = MainWindow?.Window;
            window.WindowStartupLocation = MainWindow != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen;

            // Set the owner now so the window can be recognized as modal when shown
            if (MainWindow != null)
            {
                MainWindow.IsDisabled = true;
            }

            AllWindowsList.Add(windowInfo);
            BlockingWindowsList.Add(windowInfo);

            // Update the hwnd on load in case the window is closed before being shown
            // We will receive EVENT_OBJECT_HIDE but not EVENT_OBJECT_SHOW in this case.
            window.Loaded += (sender, e) => windowInfo.ForceUpdateHwnd();
            window.Closed += (sender, e) => ActivateMainWindow();

            Logger.Info($"Modal window showing. ({window})");
            window.Show();
        }

        /// <summary>
        /// Displays the given window at the mouse cursor position when <see cref="Window.Show"/> will be called.
        /// </summary>
        /// <param name="window">The window to place at cursor position.</param>
        /// <remarks>This method must be called before <see cref="Window.Show"/>.</remarks>
        public static void ShowAtCursorPosition([NotNull] Window window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Loaded += PositionWindowToMouseCursor;
        }

        private static void PositionWindowToMouseCursor(object sender, RoutedEventArgs e)
        {
            var window = (Window)sender;
            // dispatch with one frame delay to make sure WPF layout passes are completed (if not, actual width and height might be incorrect)
            window.Dispatcher.InvokeAsync(() =>
            {
                var area = window.GetWorkArea();
                if (area != Rect.Empty)
                {
                    var mousePosition = window.GetCursorScreenPosition();
                    var expandRight = area.Right > mousePosition.X + window.ActualWidth;
                    var expandBottom = area.Bottom > mousePosition.Y + window.ActualHeight;
                    window.Left = expandRight ? mousePosition.X : mousePosition.X - window.ActualWidth;
                    window.Top = expandBottom ? mousePosition.Y : mousePosition.Y - window.ActualHeight;
                }
            });

            window.Loaded -= PositionWindowToMouseCursor;
        }

        private static void ActivateMainWindow()
        {
            if (MainWindow != null && MainWindow.Hwnd != IntPtr.Zero)
                NativeHelper.SetActiveWindow(MainWindow.Hwnd);
        }

        private static void CheckDispatcher()
        {
            if (dispatcher.Thread != Thread.CurrentThread)
            {
                const string message = "This method must be invoked from the dispatcher thread";
                Logger.Error(message);
                throw new InvalidOperationException(message);
            }
        }

        private static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hwnd == IntPtr.Zero)
                return;

            var rootHwnd = NativeHelper.GetAncestor(hwnd, NativeHelper.GetAncestorFlags.GetRoot);
            if (rootHwnd != IntPtr.Zero && rootHwnd != hwnd)
            {
                Logger.Debug($"Discarding non-root window ({hwnd}) - root: ({NativeHelper.GetAncestor(hwnd, NativeHelper.GetAncestorFlags.GetRoot)})");
                return;
            }

            // idObject == 0 means it is the window itself, not a child object
            if (eventType == NativeHelper.EVENT_OBJECT_SHOW && idObject == 0)
            {
                if (dispatcher.CheckAccess())
                    WindowShown(hwnd);
                else
                    dispatcher.InvokeAsync(() => WindowShown(hwnd));
            }
            if (eventType == NativeHelper.EVENT_OBJECT_HIDE && idObject == 0)
            {
                if (dispatcher.CheckAccess())
                    WindowHidden(hwnd);
                else
                    dispatcher.InvokeAsync(() => WindowHidden(hwnd));
            }
        }

        private static void WindowShown(IntPtr hwnd)
        {
            if (!HwndHelper.HasStyleFlag(hwnd, NativeHelper.WS_VISIBLE))
            {
                Logger.Debug($"Discarding non-visible window ({hwnd})");
                return;
            }

            Logger.Verbose($"Processing newly shown window ({hwnd})...");
            var windowInfo = Find(hwnd);
            if (windowInfo == null)
            {
                windowInfo = new WindowInfo(hwnd);

                // Ignore window created on separate UI threads
                if (windowInfo.Window?.Dispatcher != dispatcher)
                    return;

                if (Debugger.IsAttached)
                {
                    // Some external processes might attach a window to ours, we want to discard them.
                    foreach (var debugWindowTypeName in DebugWindowTypeNames)
                    {
                        if (windowInfo.Window?.GetType().FullName.StartsWith(debugWindowTypeName) ?? false)
                        {
                            Logger.Debug($"Discarding debug/diagnostics window '{windowInfo.Window.GetType().FullName}' ({hwnd})");
                            return;
                        }
                    }
                }

                // Make sure first window is activated (modal windows is not auto activated after splash screen)
                if (AllWindowsList.Count == 0)
                {
                    windowInfo.Window?.Activate();
                    windowInfo.Window?.Focus();
                }

                AllWindowsList.Add(windowInfo);
            }
            windowInfo.IsShown = true;

            if (windowInfo == MainWindow)
            {
                Logger.Info($"Main window ({hwnd}) shown.");
                foreach (var blockingWindow in BlockingWindowsList)
                {
                    Logger.Debug($"Setting owner of exiting blocking window {blockingWindow.Hwnd} to be the main window ({hwnd}).");
                    blockingWindow.Owner = MainWindow;
                }
                if (ModalWindowsList.Count > 0 || BlockingWindowsList.Count > 0)
                {
                    Logger.Verbose($"Main window ({MainWindow.Hwnd}) disabled because a modal or blocking window is already visible.");
                    MainWindow.IsDisabled = true;
                }
            }
            else if (windowInfo.IsBlocking)
            {
                Logger.Info($"Blocking window ({hwnd}) shown.");
                if (MainWindow != null && MainWindow.IsShown)
                {
                    Logger.Verbose($"Main window ({MainWindow.Hwnd}) disabled by new blocking window.");
                    MainWindow.IsDisabled = true;
                }
                if (ModalWindowsList.Count > 0)
                {
                    Logger.Verbose($"Blocking window ({hwnd}) disabled because a modal is already visible.");
                    windowInfo.IsDisabled = true;
                }
            }
            else if (windowInfo.IsModal)
            {
                Logger.Info($"Modal window ({hwnd}) shown.");
                ModalWindowsList.Add(windowInfo);
            }
        }

        private static void WindowHidden(IntPtr hwnd)
        {
            Logger.Verbose($"Processing newly hidden window ({hwnd})...");

            var windowInfo = Find(hwnd);
            if (windowInfo == null)
            {
                var message = $"This window was not handled by the {nameof(WindowManager)} ({hwnd})";
                Logger.Verbose(message);
                return;
            }

            windowInfo.IsShown = false;
            AllWindowsList.Remove(windowInfo);

            if (MainWindow != null && MainWindow.Equals(windowInfo))
            {
                Logger.Info($"Main window ({hwnd}) closed.");
                MainWindow = null;
            }
            else if (windowInfo.IsBlocking)
            {
                Logger.Info($"Blocking window ({hwnd}) closed.");
                var index = BlockingWindowsList.IndexOf(windowInfo);
                if (index < 0)
                    throw new InvalidOperationException("An unregistered blocking window has been closed.");
                BlockingWindowsList.RemoveAt(index);
                windowInfo.IsBlocking = false;
                if (MainWindow != null && MainWindow.IsShown && BlockingWindowsList.Count == 0 && ModalWindows.Count == 0)
                {
                    Logger.Verbose($"Main window ({MainWindow.Hwnd}) enabled because no more modal nor blocking windows are visible.");
                    MainWindow.IsDisabled = false;
                }
                ActivateMainWindow();
            }
            else
            {
                // Note: We cannot check windowInfo.IsModal anymore at that point because the window is closed.
                var index = ModalWindowsList.IndexOf(windowInfo);
                if (index >= 0)
                {
                    Logger.Info($"Modal window ({hwnd}) closed.");
                    ModalWindowsList.RemoveAt(index);

                    if (ModalWindowsList.Count == 0)
                    {
                        foreach (var blockingWindow in BlockingWindowsList)
                        {
                            Logger.Verbose($"Blocking window ({blockingWindow.Hwnd}) enabled because no more modal windows are visible.");
                            blockingWindow.IsDisabled = false;
                        }
                        if (MainWindow != null && MainWindow.IsShown && BlockingWindowsList.Count == 0 && ModalWindows.Count == 0)
                        {
                            Logger.Verbose($"Main window ({MainWindow.Hwnd}) enabled because no more modal nor blocking windows are visible.");
                            MainWindow.IsDisabled = false;
                        }
                        // re-activate only after all popups have closed, since some popups are spawned from popups themselves,
                        // when their original parent closes, reactivating the main window causes the still living children to close.
                        ActivateMainWindow();
                    }
                }
            }
        }

        [CanBeNull]
        internal static WindowInfo Find(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return null;

            var result = AllWindowsList.FirstOrDefault(x => Equals(x.Hwnd, hwnd));
            if (result != null)
                return result;

            var window = WindowInfo.FromHwnd(hwnd);

            // Ignore window created on separate UI threads
            if (window == null || window.Dispatcher != dispatcher)
                return null;

            return AllWindowsList.FirstOrDefault(x => Equals(x.Window, window));
        }
    }
}
