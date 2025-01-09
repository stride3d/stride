using System;

namespace Stride.Games;
public interface IWindowedPlatform
{
    GameWindow MainWindow { get; }

    /// <summary>
    /// Occurs when [window created].
    /// </summary>
    public event EventHandler<EventArgs> WindowCreated;
}
