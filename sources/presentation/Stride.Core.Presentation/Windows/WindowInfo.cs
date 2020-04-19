// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Interop;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Interop;

namespace Stride.Core.Presentation.Windows
{
    /// <summary>
    /// A container object for windows and their related information.
    /// </summary>
    public class WindowInfo : IEquatable<WindowInfo>
    {
        private IntPtr hwnd;
        private bool isShown;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowInfo"/> class.
        /// </summary>
        /// <param name="window">The window represented by this object.</param>
        public WindowInfo([NotNull] Window window)
        {
            Window = window ?? throw new ArgumentNullException(nameof(window));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowInfo"/> class.
        /// </summary>
        /// <param name="hwnd">The hwnd of the window represented by this object.</param>
        internal WindowInfo(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) throw new ArgumentException(@"The hwnd cannot be null", nameof(hwnd));
            var window = FromHwnd(hwnd);
            Window = window;
            if (window == null)
                this.hwnd = hwnd;
        }

        /// <summary>
        /// Gets the <see cref="Window"/> represented by this object, if available.
        /// </summary>
        public Window Window { get; }

        /// <summary>
        /// Gets the hwnd of the window represented by this object, if available.
        /// </summary>
        public IntPtr Hwnd => hwnd == IntPtr.Zero && Window != null ? ToHwnd(Window) : hwnd;

        /// <summary>
        /// Gets whether the corresponding window is currently disabled.
        /// </summary>
        public bool IsDisabled { get => HwndHelper.IsDisabled(Hwnd); internal set => HwndHelper.SetDisabled(Hwnd, value); }

        /// <summary>
        /// Gets whether the corresponding window is currently shown.
        /// </summary>
        public bool IsShown
        {
            get => isShown;
            internal set
            {
                isShown = value;
                ForceUpdateHwnd();
            }
        }

        /// <summary>
        /// Gets whether the corresponding window is a blocking window.
        /// </summary>
        public bool IsBlocking { get; internal set; }

        /// <summary>
        /// Gets whether the corresponding window is currently modal.
        /// </summary>
        /// <remarks>
        /// This methods is heuristic, since there is no absolute flag under Windows indicating whether
        /// a window is modal. This method might need to be adjusted depending on the use cases.
        /// </remarks>
        public bool IsModal
        {
            get
            {
                if (IsBlocking)
                    return false;

                if (Hwnd == IntPtr.Zero)
                    return false;

                if (HwndHelper.HasExStyleFlag(Hwnd, NativeHelper.WS_EX_TOOLWINDOW))
                    return false;

                if (HwndHelper.HasStyleFlag(Hwnd, NativeHelper.WS_CHILD))
                    return false;

                var owner = Owner;
                return owner == null || owner.IsModal && owner.IsDisabled;
            }
        }

        /// <summary>
        /// Gets the owner of this window.
        /// </summary>
        [CanBeNull]
        public WindowInfo Owner
        {
            get
            {
                if (!IsShown)
                    return null;

                if (Window?.Owner != null)
                {
                    return WindowManager.Find(ToHwnd(Window.Owner));
                }

                var owner = HwndHelper.GetOwner(Hwnd);
                return owner != IntPtr.Zero ? (WindowManager.Find(owner) ?? new WindowInfo(owner)) : null;
            }
            internal set
            {
                if (value == Owner)
                    return;

                if (Window != null)
                {
                    if (value?.Window == null)
                    {
                        Window.Owner = null;
                        if (value != null)
                        {
                            HwndHelper.SetOwner(Hwnd, value.Hwnd);
                        }
                    }
                    else
                    {
                        Window.Owner = value.Window;
                    }
                }
                else
                {
                    HwndHelper.SetOwner(Hwnd, value?.Hwnd ?? IntPtr.Zero);
                }
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as WindowInfo);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Window?.GetHashCode() ?? 0;
        }

        internal void ForceUpdateHwnd()
        {
            if (Window != null)
                hwnd = ToHwnd(Window);
        }

        /// <inheritdoc/>
        public static bool operator ==(WindowInfo left, WindowInfo right)
        {
            return Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(WindowInfo left, WindowInfo right)
        {
            return !Equals(left, right);
        }

        /// <inheritdoc/>
        public bool Equals(WindowInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Window, other.Window) &&
                   Equals(Hwnd, other.Hwnd);
        }

        internal static IntPtr ToHwnd(Window window)
        {
            return window != null ? new WindowInteropHelper(window).Handle : IntPtr.Zero;
        }

        internal static Window FromHwnd(IntPtr hwnd)
        {
            return hwnd != IntPtr.Zero ? HwndSource.FromHwnd(hwnd)?.RootVisual as Window : null;
        }
    }
}
