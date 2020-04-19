// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.InteropServices;
using Stride.Core.Presentation.Interop;

namespace Stride.Core.Presentation.Windows
{
    /// <summary>
    /// A helper class to access and modify properties of a window through it's <c>hwnd</c>.
    /// </summary>
    public static class HwndHelper
    {
        /// <summary>
        /// Gets the owner of the window corresponding to the given hwnd.
        /// </summary>
        /// <param name="hwnd">The hwnd of the window.</param>
        /// <returns>The hwnd of the owner.</returns>
        /// <remarks>Only top-level windows might owner. Owner of a window is also a top-level window.</remarks>
        public static IntPtr GetOwner(IntPtr hwnd)
        {
            return NativeHelper.GetWindow(hwnd, NativeHelper.GetWindowCmd.GW_OWNER);
        }

        /// <summary>
        /// Sets the owner of the window corresponding to the given hwnd.
        /// </summary>
        /// <param name="hwnd">The hwnd of the window.</param>
        /// <param name="ownerHwnd">The hwnd of the owner window.</param>
        /// <remarks>Only top-level windows might owner. Owner of a window is also a top-level window.</remarks>
        public static void SetOwner(IntPtr hwnd, IntPtr ownerHwnd)
        {
            NativeHelper.SetWindowLong(new HandleRef(null, hwnd), NativeHelper.WindowLongType.HwndParent, ownerHwnd);
        }

        /// <summary>
        /// Gets the parent of the window corresponding to the given hwnd.
        /// </summary>
        /// <param name="hwnd">The hwnd of the window.</param>
        /// <returns>The hwnd of the parent.</returns>
        /// <remarks>Only child windows might have a parent.</remarks>
        public static IntPtr GetParent(IntPtr hwnd)
        {
            return NativeHelper.GetAncestor(hwnd, NativeHelper.GetAncestorFlags.GetParent);
        }

        /// <summary>
        /// Gets whether the window corresponding to the given hwnd is disabled.
        /// </summary>
        /// <param name="hwnd">The hwnd of the window.</param>
        /// <returns><c>True</c> if the window is disabled, <c>False</c> otherwise.</returns>
        public static bool IsDisabled(IntPtr hwnd)
        {
            return HasStyleFlag(hwnd, NativeHelper.WS_DISABLED);
        }

        /// <summary>
        /// Enables or disables the window corresponding to the given hwnd. 
        /// </summary>
        /// <param name="hwnd">The hwnd of the window.</param>
        /// <param name="value"><c>True</c> to disable the window, <c>False</c> to enable it.</param>
        public static void SetDisabled(IntPtr hwnd, bool value)
        {
            var style = NativeHelper.GetWindowLong(hwnd, NativeHelper.GWL_STYLE);
            if (value)
            {
                style |= NativeHelper.WS_DISABLED;
            }
            else
            {
                style &= ~NativeHelper.WS_DISABLED;
            }
            NativeHelper.SetWindowLong(hwnd, NativeHelper.GWL_STYLE, style);
        }

        /// <summary>
        /// Gets whether the window corresponding to the given hwnd has the given style flag.
        /// </summary>
        /// <param name="hwnd">The hwnd of the window.</param>
        /// <param name="flag">The flag to check.</param>
        /// <returns><c>True</c> if the window has the given flag, <c>False</c> otherwise.</returns>
        public static bool HasStyleFlag(IntPtr hwnd, int flag)
        {
            var style = NativeHelper.GetWindowLong(hwnd, NativeHelper.GWL_STYLE);
            return MatchFlag(style, flag);
        }

        /// <summary>
        /// Gets whether the window corresponding to the given hwnd has the given extended style flag.
        /// </summary>
        /// <param name="hwnd">The hwnd of the window.</param>
        /// <param name="flag">The flag to check.</param>
        /// <returns><c>True</c> if the window has the given flag, <c>False</c> otherwise.</returns>
        public static bool HasExStyleFlag(IntPtr hwnd, uint flag)
        {
            var style = NativeHelper.GetWindowLong(hwnd, NativeHelper.GWL_EXSTYLE);
            return MatchFlag(style, flag);
        }

        private static bool MatchFlag(int value, int flag) => (value & flag) == flag;
        private static bool MatchFlag(int value, uint flag) => (value & flag) == flag;
    }
}
