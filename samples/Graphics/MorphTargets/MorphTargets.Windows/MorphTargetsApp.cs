using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new SampleGame();
game.Run();

sealed class SampleGame : Game
{
    // After Initialize, which applies the Game Settings back buffer size over anything set earlier.
    protected override void BeginRun()
    {
        base.BeginRun();
        Window.AllowUserResizing = true;
        // Half the desktop's width and height, centered on it.
        var desktop = GraphicsDevice.Adapter.Outputs[0].DesktopBounds;
        GraphicsDeviceManager.PreferredBackBufferWidth = desktop.Width / 2;
        GraphicsDeviceManager.PreferredBackBufferHeight = desktop.Height / 2;
        GraphicsDeviceManager.ApplyChanges();
        Window.Position = new Int2(desktop.X + desktop.Width / 4, desktop.Y + desktop.Height / 4);
    }
}
