// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// A <see cref="NativeControlHost"/> that embeds the native window of a game (the SDL window of the editor's games): Avalonia
/// reparents the window into the host, and moves, resizes, shows and hides it with the control.
/// </summary>
/// <remarks>
/// The WPF host made the window a child window itself and tracked the layout to move it. Avalonia does that for the handle
/// types its platforms accept: an HWND on Windows and an X11 window (XID) on Linux. On macOS it expects an NSView, while SDL
/// gives an NSWindow; that platform is not supported yet.
/// </remarks>
public sealed class GameEngineHost : NativeControlHost, IDisposable
{
    private bool isDisposed;

    static GameEngineHost()
    {
        FocusableProperty.OverrideMetadata(typeof(GameEngineHost), new StyledPropertyMetadata<bool>(true));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameEngineHost"/> class.
    /// </summary>
    /// <param name="childHandle">The native handle of the hosted window: an HWND on Windows, an X11 window on Linux.</param>
    public GameEngineHost(IntPtr childHandle)
    {
        Handle = childHandle;
        MinWidth = 32;
        MinHeight = 32;
    }

    /// <summary>
    /// Gets the native handle of the hosted window.
    /// </summary>
    public IntPtr Handle { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        // The game owns its window and destroys it when it exits: nothing to release here
        isDisposed = true;
    }

    /// <summary>
    /// Forwards a message of the hosted window to the editor, such as the keyboard and mouse events of a viewport.
    /// </summary>
    /// <remarks>
    /// The WPF host translated the Win32 messages of the window into WPF input events. The SDL window has no message loop
    /// to hook, so the forwarding waits for the viewport work of the port.
    /// </remarks>
    /// <param name="hwnd">The native handle of the hosted window that produced the message.</param>
    /// <param name="msg">The message identifier.</param>
    /// <param name="wParam">The word parameter of the message.</param>
    /// <param name="lParam">The long parameter of the message.</param>
    public void ForwardMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
    {
    }

    /// <inheritdoc />
    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        if (Handle == IntPtr.Zero || isDisposed)
            return base.CreateNativeControlCore(parent);

        return new PlatformHandle(Handle, GetHandleDescriptor());
    }

    /// <inheritdoc />
    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        // The hosted window belongs to the game, which destroys it when it exits: Avalonia must not destroy it. A handle that
        // the base class created (no game window) is the base class's to destroy.
        if (control.Handle != Handle)
            base.DestroyNativeControlCore(control);
    }

    private static string GetHandleDescriptor()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "XID";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "NSView";
        return "HWND";
    }
}
