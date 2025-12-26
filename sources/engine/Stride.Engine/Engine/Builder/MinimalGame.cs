// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;

namespace Stride.Engine.Builder;

/// <summary>
/// A game class with no registered systems by default.
/// </summary>
public class MinimalGame : GameBase, IHostedService
{

    /// <summary>
    /// Gets the graphics device manager.
    /// </summary>
    /// <value>The graphics device manager.</value>
    public GraphicsDeviceManager GraphicsDeviceManager { get; internal set; }

    public MinimalGame(GameContext gameContext) : base()
    {
        Context = gameContext ?? GetDefaultContext();
        Context.CurrentGame = this;
        Context.Services = Services;

        // Create Platform
        Context.GamePlatform = GamePlatform.Create(Context);
        Context.GamePlatform.Activated += GamePlatform_Activated;
        Context.GamePlatform.Deactivated += GamePlatform_Deactivated;
        Context.GamePlatform.Exiting += GamePlatform_Exiting;
        Context.GamePlatform.WindowCreated += GamePlatformOnWindowCreated;

        // Setup registry
        Services.AddService<IGame>(this);
        Services.AddService<IGraphicsDeviceFactory>(Context.GamePlatform);
        Services.AddService<IGamePlatform>(Context.GamePlatform);

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
            //if our device width or height is actually smaller than requested we use the device one
            deviceManager.PreferredBackBufferWidth = Context.RequestedWidth = Math.Min(deviceManager.PreferredBackBufferWidth, Window.ClientBounds.Width);
            deviceManager.PreferredBackBufferHeight = Context.RequestedHeight = Math.Min(deviceManager.PreferredBackBufferHeight, Window.ClientBounds.Height);
        }
    }

    protected override void Initialize()
    {
        base.Initialize();

        Content.Serializer.LowLevelSerializerSelector = new SerializerSelector("Default", "Content");

        // Add window specific input source
        var inputManager = Services.GetService<InputManager>();
        if (inputManager is not null)
        {
            var windowInputSource = InputSourceFactory.NewWindowInputSource(Context);
            inputManager.Sources.Add(windowInputSource);
        }
    }

    protected override void PrepareContext()
    {
        //Allow the user to add their own ContentManager
        var contentManager = Services.GetService<ContentManager>();

        if (contentManager is null)
        {
            Log.Info("No ContentManager found, creating default ContentManager");
            contentManager = new ContentManager(Services);
            Services.AddService<IContentManager>(contentManager);
            Services.AddService(contentManager);
        }

        Content = contentManager;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        Run();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        Exit();
        return Task.CompletedTask;
    }

}
