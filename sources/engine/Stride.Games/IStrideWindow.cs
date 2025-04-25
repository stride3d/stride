using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Games;

/// <summary>
/// Probably temporary until Silk 3.0 is released. Will need to validate use cases when available.
/// </summary>
public interface IStrideWindow
{
    public IServiceRegistry Services { get; set; }

    /// <summary>
    /// Gets or sets the title of the window.
    /// </summary>
    public string Title { get; protected set; }
    
    /// <summary>
    /// Gets or sets the position of the window on the screen.
    /// </summary>
    public Int2 Position { get; set; }

    /// <summary>
    /// The size the window should have when switching from fullscreen to windowed mode.
    /// To get the current actual size use <see cref="ClientBounds"/>.
    /// This gets overwritten when the user resizes the window. 
    /// </summary>
    public Int2 WindowSize { get; set; }

    /// <summary>
    /// Gets the client bounds.
    /// </summary>
    /// <value>The client bounds.</value>
    public Rectangle ClientBounds { get; protected set; }

    /// <summary>
    /// Gets the current orientation.
    /// </summary>
    /// <value>The current orientation.</value>
    public DisplayOrientation CurrentOrientation { get; protected set; }

    /// <summary>
    /// Gets the window state.
    /// </summary>
    public WindowState CurrentWindowState { get; protected set; }

    /// <summary>
    /// Gets the window border.
    /// </summary>
    public WindowBorder CurrentWindowBorder { get; protected set; }

    /// <summary>
    /// Gets the native window.
    /// </summary>
    /// <value>The native window.</value>
    public IntPtr Handle { get; }

    public void SetTitle(string title);

    public void SetSize(Int2 size);

    public void Run();
}

public enum WindowState
{
    /// <summary>
    /// The window is in its regular configuration.
    /// </summary>
    Normal,

    /// <summary>
    /// The window has been minimized to the task bar.
    /// </summary>
    Minimized,

    /// <summary>
    /// The window has been maximized, covering the entire desktop, but not the taskbar.
    /// </summary>
    Maximized,

    /// <summary>
    /// The window has been fullscreened, covering the entire surface of the monitor.
    /// </summary>
    Fullscreen
}

/// <summary>
/// Represents the window border.
/// </summary>
public enum WindowBorder
{
    /// <summary>
    /// The window can be resized by clicking and dragging its border.
    /// </summary>
    Resizable,

    /// <summary>
    /// The window border is visible, but cannot be resized. All window-resizings must happen solely in the code.
    /// </summary>
    Fixed,

    /// <summary>
    /// The window border is hidden.
    /// </summary>
    Hidden
}
