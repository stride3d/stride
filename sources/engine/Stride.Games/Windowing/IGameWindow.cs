using System;
using Stride.Core.Mathematics;

namespace Stride.Games.Windowing;
public interface IGameWindow : IStrideSurface
{
    public IntPtr WindowHandle { get; }
    public Int2 Position { get; set; }

    public string Title { get; set; }

    public WindowState State { get; set; }
}

public enum WindowState
{
    Normal,
    Minimized,
    Maximized,
    FullscreenWindowed,
    FullscreenExclusive,
}
