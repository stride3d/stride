// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)
#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Stride.Input
{
    /// <summary>
    /// Tells whether the game window has focus, for the game controller sources that read their devices in the background.
    /// </summary>
    internal static class WindowsInputFocus
    {
        private const uint GaRoot = 2;

        /// <summary>
        /// Gets whether the top-level window that contains <paramref name="control"/> is the foreground window.
        /// </summary>
        /// <param name="control">The game's control. When the game is embedded in another window, its top-level window is that window.</param>
        /// <returns><c>true</c> if that window is the foreground window, or if there is no game window.</returns>
        public static bool HasFocus(Control? control)
        {
            if (control is null || control.IsDisposed || !control.IsHandleCreated)
                return true;

            return GetForegroundWindow() == GetAncestor(control.Handle, GaRoot);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr window, uint flags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
    }
}
#endif
