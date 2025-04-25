using System;
using System.Collections.Generic;
using System.IO;
using Stride.Core;
using Stride.Graphics;

namespace Stride.Games.SDL;
public class SDLGamePlatform : GamePlatform, IWindowedPlatform
{

    public override string DefaultAppDirectory
    {
        get
        {
            var appPath = PlatformFolders.ApplicationExecutablePath ?? PlatformFolders.ApplicationBinaryDirectory;
            var assemblyUri = new Uri(appPath);
            return Path.GetDirectoryName(assemblyUri.LocalPath);
        }
    }

    public event EventHandler<EventArgs> WindowCreated;

    public GameWindow MainWindow { get; protected set; } = new GameWindowSDL();

    public override List<GraphicsDeviceInformation> FindBestDevices(GameGraphicsParameters preferredParameters)
    {
        var graphicsDeviceInfos = new List<GraphicsDeviceInformation>();

        // Iterate on each adapter
        foreach (var graphicsAdapter in GraphicsAdapterFactory.Adapters)
        {
            if (!string.IsNullOrEmpty(preferredParameters.RequiredAdapterUid) && graphicsAdapter.AdapterUid != preferredParameters.RequiredAdapterUid) continue;

            // Skip adapeters that don't have graphics output 
            // but only if no RequiredAdapterUid is provided (OculusVR at init time might be in a device with no outputs)
            if (graphicsAdapter.Outputs.Length == 0 && string.IsNullOrEmpty(preferredParameters.RequiredAdapterUid))
            {
                continue;
            }

            var preferredGraphicsProfiles = preferredParameters.PreferredGraphicsProfile;

            // Iterate on each preferred graphics profile
            foreach (var featureLevel in preferredGraphicsProfiles)
            {
                // Check if this profile is supported.
                if (graphicsAdapter.IsProfileSupported(featureLevel))
                {
                    var deviceInfo = new GraphicsDeviceInformation
                    {
                        Adapter = graphicsAdapter,
                        GraphicsProfile = featureLevel,
                        PresentationParameters =
                            {
                                MultisampleCount = preferredParameters.PreferredMultisampleCount,
                                IsFullScreen = preferredParameters.IsFullScreen,
                                PreferredFullScreenOutputIndex = preferredParameters.PreferredFullScreenOutputIndex,
                                PresentationInterval = preferredParameters.SynchronizeWithVerticalRetrace ? PresentInterval.One : PresentInterval.Immediate,
                                DeviceWindowHandle = MainWindow.NativeWindow,
                                ColorSpace = preferredParameters.ColorSpace,
                            },
                    };

                    var preferredMode = new DisplayMode(preferredParameters.PreferredBackBufferFormat,
                        preferredParameters.PreferredBackBufferWidth,
                        preferredParameters.PreferredBackBufferHeight,
                        preferredParameters.PreferredRefreshRate);

                    // if we want to switch to fullscreen, try to find only needed output, otherwise check them all
                    if (preferredParameters.IsFullScreen)
                    {
                        if (preferredParameters.PreferredFullScreenOutputIndex < graphicsAdapter.Outputs.Length)
                        {
                            var output = graphicsAdapter.Outputs[preferredParameters.PreferredFullScreenOutputIndex];
                            var displayMode = output.FindClosestMatchingDisplayMode(preferredGraphicsProfiles, preferredMode);
                            AddDevice(displayMode, deviceInfo, preferredParameters, graphicsDeviceInfos);
                        }
                    }
                    else
                    {
                        AddDevice(preferredMode, deviceInfo, preferredParameters, graphicsDeviceInfos);
                    }

                    // If the profile is supported, we are just using the first best one
                    break;
                }
            }
        }

        return graphicsDeviceInfos;
    }

    public override GraphicsDevice CreateDevice(GraphicsDeviceInformation deviceInformation)
    {
        var graphicsDevice = GraphicsDevice.New(deviceInformation.Adapter, deviceInformation.DeviceCreationFlags, MainWindow.NativeWindow, deviceInformation.GraphicsProfile);
        graphicsDevice.ColorSpace = deviceInformation.PresentationParameters.ColorSpace;

        graphicsDevice.Presenter = new SwapChainGraphicsPresenter(graphicsDevice, deviceInformation.PresentationParameters);

        return graphicsDevice;
    }

    public override void RecreateDevice(GraphicsDevice currentDevice, GraphicsDeviceInformation deviceInformation)
    {
        currentDevice.ColorSpace = deviceInformation.PresentationParameters.ColorSpace;
        currentDevice.Recreate(deviceInformation.Adapter ?? GraphicsAdapterFactory.Default, [deviceInformation.GraphicsProfile], deviceInformation.DeviceCreationFlags, MainWindow.NativeWindow);
    }

    public override void DeviceChanged(GraphicsDevice currentDevice, GraphicsDeviceInformation deviceInformation)
    {
        // Force to resize the gameWindow
        MainWindow.Resize(deviceInformation.PresentationParameters.BackBufferWidth, deviceInformation.PresentationParameters.BackBufferHeight);
    }

    protected override void Destroy()
    {
        if (MainWindow != null)
        {
            MainWindow.Dispose();
            MainWindow = null;
        }

        base.Destroy();
    }

}
