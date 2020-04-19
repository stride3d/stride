// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Interop
{
    /// <summary>
    /// Enables to register listener to the native clipboard changed event (also called clipboard viewers)
    /// </summary>
    public static class ClipboardMonitor
    {
        private static IntPtr hwndNextViewer;
        private static readonly ConditionalWeakTable<Window, HwndSource> Listeners = new ConditionalWeakTable<Window, HwndSource>();

        /// <summary>
        /// Raised when the clipboard has changed and contains text.
        /// </summary>
        /// <remarks>The sender of this event a window that was previously registered as a clipboard viewer with <see cref="RegisterListener"/>.</remarks>
        public static event EventHandler<EventArgs> ClipboardTextChanged;

        /// <summary>
        /// Registers the given <paramref name="window"/> as a clipboard viewer.
        /// </summary>
        /// <param name="window"></param>
        /// <exception cref="ArgumentNullException">window is <c>null</c></exception>
        /// <exception cref="InvalidOperationException">window is already registered.</exception>
        public static void RegisterListener([NotNull] Window window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            HwndSource hwndSource;
            if (Listeners.TryGetValue(window, out hwndSource))
                throw new InvalidOperationException($"The given {window} is already registered as a clipboard listener.");

            hwndSource = GetHwndSource(window);
            if (hwndSource == null)
                return;

            Listeners.Add(window, hwndSource);

            window.Dispatcher.Invoke(() =>
            {
                // start processing window messages
                hwndSource.AddHook(WinProc);
                // set the window as a viewer
                hwndNextViewer = NativeHelper.SetClipboardViewer(hwndSource.Handle);
            });
        }

        /// <summary>
        /// Unregisters the given <paramref name="window"/> as a clipboard viewer.
        /// </summary>
        /// <param name="window"></param>
        /// <exception cref="ArgumentNullException">window is <c>null</c></exception>
        /// <exception cref="InvalidOperationException">window was not previously registered.</exception>
        public static void UnregisterListener([NotNull] Window window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            HwndSource hwndSource;
            if (!Listeners.TryGetValue(window, out hwndSource))
                throw new InvalidOperationException($"The given {window} is not registered as a clipboard listener.");

            window.Dispatcher.Invoke(() =>
            {
                // stop processing window messages
                hwndSource.RemoveHook(WinProc);
                // restore the chain
                NativeHelper.ChangeClipboardChain(hwndSource.Handle, hwndNextViewer);
            });
        }

        [CanBeNull]
        private static HwndSource GetHwndSource([NotNull] Window window)
        {
            var handle = new WindowInteropHelper(window).Handle;
            return handle != IntPtr.Zero ? HwndSource.FromHwnd(handle) : null;
        }

        private static void OnClipboardContentChanged(IntPtr hwnd)
        {
            var hwndSource = HwndSource.FromHwnd(hwnd);
            hwndSource?.Dispatcher.InvokeAsync(() =>
            {
                if (SafeClipboard.ContainsText())
                {
                    ClipboardTextChanged?.Invoke(hwndSource.RootVisual, EventArgs.Empty);
                }
            });
        }

        private static IntPtr WinProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case NativeHelper.WM_CHANGECBCHAIN:
                    if (wParam == hwndNextViewer)
                    {
                        // clipboard viewer chain changed, need to fix it. 
                        hwndNextViewer = lParam;
                    }
                    else if (hwndNextViewer != IntPtr.Zero)
                    {
                        // pass the message to the next viewer. 
                        NativeHelper.SendMessage(hwndNextViewer, msg, wParam, lParam);
                    }
                    break;

                case NativeHelper.WM_DRAWCLIPBOARD:
                    // clipboard content changed 
                    OnClipboardContentChanged(hwnd);
                    // pass the message to the next viewer. 
                    NativeHelper.SendMessage(hwndNextViewer, msg, wParam, lParam);
                    break;
            }

            return IntPtr.Zero;
        }
    }
}
