using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Games;
public abstract class StrideWindow : ComponentBase
{
    #region Public Events

    /// <summary>
    /// Indicate if the window is currently activated.
    /// </summary>
    public bool IsActivated;

    /// <summary>
    /// Occurs when this window is activated.
    /// </summary>
    public event EventHandler<EventArgs> Activated;

    /// <summary>
    /// Occurs when device client size is changed.
    /// </summary>
    public event EventHandler<EventArgs> ClientSizeChanged;

    /// <summary>
    /// Occurs when this window is deactivated.
    /// </summary>
    public event EventHandler<EventArgs> Deactivated;

    /// <summary>
    /// Occurs when device orientation is changed.
    /// </summary>
    public event EventHandler<EventArgs> OrientationChanged;

    /// <summary>
    /// Occurs when device fullscreen mode is changed.
    /// </summary>
    public event EventHandler<EventArgs> FullscreenChanged;

    /// <summary>
    /// Occurs before the window gets destroyed.
    /// </summary>
    public event EventHandler<EventArgs> Closing;

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets or sets, user possibility to resize this window.
    /// </summary>
    public abstract bool AllowUserResizing { get; set; }

    /// <summary>
    /// Gets the client bounds.
    /// </summary>
    /// <value>The client bounds.</value>
    public abstract Rectangle ClientBounds { get; }

    /// <summary>
    /// Gets the current orientation.
    /// </summary>
    /// <value>The current orientation.</value>
    public abstract DisplayOrientation CurrentOrientation { get; }

    public WindowState CurrentWindowState;

    /// <summary>
    /// Gets the native window.
    /// </summary>
    /// <value>The native window.</value>
    public abstract WindowHandle NativeWindow { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="GameWindow" /> is visible.
    /// </summary>
    /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
    public abstract bool Visible { get; set; }

    /// <summary>
    /// Gets or sets the position of the window on the screen.
    /// </summary>
    public virtual Int2 Position { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this window has a border
    /// </summary>
    /// <value><c>true</c> if this window has a border; otherwise, <c>false</c>.</value>
    public abstract bool IsBorderLess { get; set; }

    /// <summary>
    /// Gets or sets the title of the window.
    /// </summary>
    public string Title { get; protected set; }

    /// <summary>
    /// The size the window should have when switching from fullscreen to windowed mode.
    /// To get the current actual size use <see cref="ClientBounds"/>.
    /// This gets overwritten when the user resizes the window. 
    /// </summary>
    public Int2 WindowedSize { get; set; } = new Int2(768, 432);

    /// <summary>
    /// The size the window should have when switching from windowed to fullscreen mode.
    /// To get the current actual size use <see cref="ClientBounds"/>.
    /// </summary>
    public Int2 FullscreenSize { get; set; } = new Int2(1920, 1080);

    #endregion

    #region Public Methods and Operators

    public abstract void BeginScreenDeviceChange(bool willBeFullScreen);

    public void EndScreenDeviceChange()
    {
        EndScreenDeviceChange(ClientBounds.Width, ClientBounds.Height);
    }

    public abstract void EndScreenDeviceChange(int clientWidth, int clientHeight);

    #endregion

    #region Methods

    public bool Exiting;

    public Action InitCallback;

    public Action RunCallback;

    public Action ExitCallback;

    public abstract void Run();

    /// <summary>
    /// Sets the size of the client area and triggers the <see cref="ClientSizeChanged"/> event.
    /// This will trigger a backbuffer resize too.
    /// </summary>
    public void SetSize(Int2 size)
    {
        Resize(size.X, size.Y);
        OnClientSizeChanged(this, EventArgs.Empty);
    }

    /// <summary>
    /// Only used internally by the device managers when they adapt the window size to the backbuffer size.
    /// Resizes the window, without sending the resized event.
    /// </summary>
    internal abstract void Resize(int width, int height);

    public virtual IMessageLoop CreateUserManagedMessageLoop()
    {
        // Default: not implemented
        throw new PlatformNotSupportedException();
    }

    internal IServiceRegistry Services { get; set; }

    protected internal abstract void SetSupportedOrientations(DisplayOrientation orientations);

    protected void OnActivated(object source, EventArgs e)
    {
        IsActivated = true;

        var handler = Activated;
        handler?.Invoke(source, e);
    }

    protected void OnClientSizeChanged(object source, EventArgs e)
    {
        if (CurrentWindowState == WindowState.Windowed)
        {
            // Update preferred windowed size in windowed mode 
            var resizeSize = ClientBounds.Size;
            WindowedSize = new Int2(resizeSize.Width, resizeSize.Height);
        }
        var handler = ClientSizeChanged;
        handler?.Invoke(this, e);
    }

    protected void OnDeactivated(object source, EventArgs e)
    {
        IsActivated = false;

        var handler = Deactivated;
        handler?.Invoke(source, e);
    }

    protected void OnOrientationChanged(object source, EventArgs e)
    {
        var handler = OrientationChanged;
        handler?.Invoke(this, e);
    }

    protected void OnClosing(object source, EventArgs e)
    {
        var handler = Closing;
        handler?.Invoke(this, e);
    }

    public abstract void SetTitle(string title);

    #endregion

    public void OnPause()
    {
        OnDeactivated(this, EventArgs.Empty);
    }

    public void OnResume()
    {
        OnActivated(this, EventArgs.Empty);
    }
}
