using System;
using Stride.Core.Serialization;
using Stride.Core.Streaming;
using Stride.Games;
using Stride.Graphics;

namespace Stride.Engine.Builder;
public class MinimalGame : GameBase
{

    /// <summary>
    /// Gets the graphics device manager.
    /// </summary>
    /// <value>The graphics device manager.</value>
    public GraphicsDeviceManager GraphicsDeviceManager { get; internal set; }

    public MinimalGame()
    {
        // Creates the graphics device manager
        GraphicsDeviceManager = new GraphicsDeviceManager(this);
        Services.AddService<IGraphicsDeviceManager>(GraphicsDeviceManager);
        Services.AddService<IGraphicsDeviceService>(GraphicsDeviceManager);
    }

    public override void ConfirmRenderingSettings(bool gameCreation)
    {
        var deviceManager = (GraphicsDeviceManager)graphicsDeviceManager;

        if (gameCreation)
        {
            //if our device width or height is actually smaller then requested we use the device one
            deviceManager.PreferredBackBufferWidth = Context.RequestedWidth = Math.Min(deviceManager.PreferredBackBufferWidth, Window.ClientBounds.Width);
            deviceManager.PreferredBackBufferHeight = Context.RequestedHeight = Math.Min(deviceManager.PreferredBackBufferHeight, Window.ClientBounds.Height);
        }
    }

    protected override void Initialize()
    {
        base.Initialize();

        Content.Serializer.LowLevelSerializerSelector = new SerializerSelector("Default", "Content");
    }
}
