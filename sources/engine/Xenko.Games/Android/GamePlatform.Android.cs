// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_ANDROID
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Xenko.Graphics;

namespace Xenko.Games
{
    internal class GamePlatformAndroid : GamePlatform, IGraphicsDeviceFactory
    {
        [DllImport("libc.so")]
        private static extern int __system_property_get(string name, IntPtr value);

        const int MaxPropertyValueLength = 92;

        private void PopulateFullName()
        {
            var str = Marshal.AllocHGlobal(MaxPropertyValueLength);
            __system_property_get("ro.product.manufacturer", str);
            var manufacturer = Marshal.PtrToStringAnsi(str);
            __system_property_get("ro.product.model", str);
            var model = Marshal.PtrToStringAnsi(str);
            Marshal.FreeHGlobal(str);
            FullName = $"{manufacturer} - {model}";
        }

        public GamePlatformAndroid(GameBase game) : base(game)
        {
            PopulateFullName();
        }

        public override string DefaultAppDirectory
        {
            get
            {
                var assemblyUri = new Uri(Assembly.GetEntryAssembly().CodeBase);
                return Path.GetDirectoryName(assemblyUri.LocalPath);
            }
        }

        internal override GameWindow GetSupportedGameWindow(AppContextType type)
        {
            if (type == AppContextType.Android)
            {
                return new GameWindowAndroid();
            }
            else
            {
                return null;
            }
        }

        public override List<GraphicsDeviceInformation> FindBestDevices(GameGraphicsParameters preferredParameters)
        {
            var gameWindowAndroid = gameWindow as GameWindowAndroid;
            if (gameWindowAndroid != null)
            {
                var graphicsAdapter = GraphicsAdapterFactory.Default;
                var graphicsDeviceInfos = new List<GraphicsDeviceInformation>();
                var preferredGraphicsProfiles = preferredParameters.PreferredGraphicsProfile;
                foreach (var featureLevel in preferredGraphicsProfiles)
                {
                    // Check if this profile is supported.
                    if (graphicsAdapter.IsProfileSupported(featureLevel))
                    {
                        // Everything is already created at this point, just transmit what has been done
                        var deviceInfo = new GraphicsDeviceInformation
                        {
                            Adapter = GraphicsAdapterFactory.Default,
                            GraphicsProfile = featureLevel,
                            PresentationParameters = new PresentationParameters(preferredParameters.PreferredBackBufferWidth, preferredParameters.PreferredBackBufferHeight,
                                gameWindowAndroid.NativeWindow)
                            {
                                // TODO: PDX-364: Transmit what was actually created
                                BackBufferFormat = preferredParameters.PreferredBackBufferFormat,
                                DepthStencilFormat = preferredParameters.PreferredDepthStencilFormat,
                            }
                        };

                        graphicsDeviceInfos.Add(deviceInfo);

                        // If the profile is supported, we are just using the first best one
                        break;
                    }
                }

                return graphicsDeviceInfos;
            }
            return base.FindBestDevices(preferredParameters);
        }

        public override void DeviceChanged(GraphicsDevice currentDevice, GraphicsDeviceInformation deviceInformation)
        {
            // TODO: Check when it needs to be disabled on iOS (OpenGL)?
            // Force to resize the gameWindow
            //gameWindow.Resize(deviceInformation.PresentationParameters.BackBufferWidth, deviceInformation.PresentationParameters.BackBufferHeight);
        }
    }
}
#endif
