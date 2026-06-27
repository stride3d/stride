// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

            AllWindowsList.Add(windowInfo);
            BlockingWindowsList.Add(windowInfo);

            // Disable the main window now so the blocking window is recognized as modal when shown.
            RefreshDisabledStates();

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

        /// <summary>
        /// Recomputes the disabled state of the main window and of every blocking window from the
        /// current contents of the tracking lists, rather than toggling it incrementally as windows
        /// come and go. This is idempotent and self-healing: a missed or reordered window event can no
        /// longer leave the main window permanently disabled, because the next event recomputes the
        /// correct state from ground truth.
        /// </summary>
        private static void RefreshDisabledStates()
        {
            // Drop tracked windows that have gone away without us seeing a proper hide event.
            PruneClosed(ModalWindowsList);
            PruneClosed(BlockingWindowsList);

            var modalPresent = ModalWindowsList.Count > 0;

            // A blocking window is disabled only while a modal sits in front of it.
            foreach (var blockingWindow in BlockingWindowsList)
                SetDisabled(blockingWindow, modalPresent);

            // The main window is disabled while any blocking or modal window is up.
            if (MainWindow != null && MainWindow.IsShown)
                SetDisabled(MainWindow, modalPresent || BlockingWindowsList.Count > 0);
        }

        private static void SetDisabled(WindowInfo windowInfo, bool disabled)
        {
            if (windowInfo.Hwnd == IntPtr.Zero)
                return;
            if (windowInfo.IsDisabled != disabled)
            {
                Logger.Verbose($"Window ({windowInfo.Hwnd}) {(disabled ? "disabled" : "enabled")}.");
                windowInfo.IsDisabled = disabled;
            }
        }

        private static void PruneClosed(List<WindowInfo> list)
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var windowInfo = list[i];
                // Only prune windows we actually saw shown; a not-yet-shown window has no hwnd yet.
                if (windowInfo.IsShown && (windowInfo.Hwnd == IntPtr.Zero || !NativeHelper.IsWindow(windowInfo.Hwnd)))
                {
                    Logger.Verbose($"Pruning destroyed window ({windowInfo.Hwnd}) from tracking lists.");
                    list.RemoveAt(i);
                    AllWindowsList.Remove(windowInfo);
                    windowInfo.IsShown = false;
                }
            }
        }

        /// <summary>
        /// Determines whether the given window was injected into our process by external tooling
        /// (Visual Studio XAML adorners, Snoop, inspectors) rather than created by the application.
        /// Such windows run on our UI thread but their type comes from an assembly that lives outside
        /// the application directory; this is identified by assembly location rather than a brittle
        /// list of known tooling type names. Native windows (e.g. a Win32 message box, with no managed
        /// <see cref="Window"/>) are genuine OS modals and are never considered foreign.
        /// </summary>
        private static bool IsForeignWindow(WindowInfo windowInfo)
        {
            var window = windowInfo.Window;
            if (window == null)
                return false;

            var location = window.GetType().Assembly.Location;
            if (string.IsNullOrEmpty(location))
                return false; // dynamic/single-file assembly: can't tell, assume ours to avoid hiding real dialogs

            return !location.StartsWith(AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase);
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

                // Discard windows injected into our process by external tooling (e.g. Visual Studio
                // XAML adorners, Snoop, inspectors). They run on our UI thread but are not ours, and
                // must not be treated as application modals (which would keep the main window disabled).
                if (IsForeignWindow(windowInfo))
                {
                    Logger.Debug($"Discarding foreign window '{windowInfo.Window?.GetType().FullName}' ({hwnd})");
                    return;
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
                    Logger.Debug($"Setting owner of existing blocking window {blockingWindow.Hwnd} to be the main window ({hwnd}).");
                    blockingWindow.Owner = MainWindow;
                }
            }
            else if (windowInfo.IsBlocking)
            {
                Logger.Info($"Blocking window ({hwnd}) shown.");
            }
            else if (windowInfo.IsModal)
            {
                Logger.Info($"Modal window ({hwnd}) shown.");
                ModalWindowsList.Add(windowInfo);
            }

            RefreshDisabledStates();
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
                BlockingWindowsList.Remove(windowInfo);
                windowInfo.IsBlocking = false;
                RefreshDisabledStates();
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
                    RefreshDisabledStates();

                    if (ModalWindowsList.Count == 0)
                    {
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
