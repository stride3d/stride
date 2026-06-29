// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_IOS
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Foundation;
using UIKit;
using Stride.Graphics;

namespace Stride.Games
{
    internal class GamePlatformiOS : GamePlatform, IGraphicsDeviceFactory
    {
        [DllImport("/usr/lib/libSystem.dylib")]
        private static unsafe extern int sysctlbyname([MarshalAs(UnmanagedType.LPStr)] string name, IntPtr oldp, int* oldlenp, IntPtr newp, uint newlen);

        private unsafe void PopulateFullName()
        {
            int len;
            sysctlbyname("hw.machine", IntPtr.Zero, &len, IntPtr.Zero, 0);
            if (len == 0) return;

            var output = Marshal.AllocHGlobal(len);
            sysctlbyname("hw.machine", output, &len, IntPtr.Zero, 0);
            FullName = Marshal.PtrToStringAnsi(output);
            Marshal.FreeHGlobal(output);
        }

        // Notification observers; kept so Destroy() can remove them. All fire on main thread.
        private NSObject didBecomeActiveObserver;
        private NSObject willResignActiveObserver;
        private NSObject didEnterBackgroundObserver;
        private NSObject willEnterForegroundObserver;
        private NSObject didReceiveMemoryWarningObserver;

        public GamePlatformiOS(GameBase game) : base(game)
        {
            PopulateFullName();
            SubscribeAppLifecycle();
        }

        // NSNotificationCenter rather than UIApplicationDelegate overrides so we don't depend
        // on which AppDelegate the host app uses (Avalonia.iOS, custom, etc.). Notifications fire
        // on the main thread; the shared GamePlatform.NotifyApp* helpers marshal the work onto
        // the game thread (and NotifyAppBackground blocks here, bounded, until the GPU is drained).
        private void SubscribeAppLifecycle()
        {
            var center = NSNotificationCenter.DefaultCenter;
            didBecomeActiveObserver         = center.AddObserver(UIApplication.DidBecomeActiveNotification,         _ => NotifyAppActivated());
            willResignActiveObserver        = center.AddObserver(UIApplication.WillResignActiveNotification,        _ => NotifyAppDeactivated());
            didEnterBackgroundObserver      = center.AddObserver(UIApplication.DidEnterBackgroundNotification,      _ => NotifyAppBackground());
            willEnterForegroundObserver     = center.AddObserver(UIApplication.WillEnterForegroundNotification,     _ => NotifyAppForeground());
            didReceiveMemoryWarningObserver = center.AddObserver(UIApplication.DidReceiveMemoryWarningNotification, _ => NotifyAppMemoryWarning());
        }

        public override string DefaultAppDirectory
        {
            get
            {
                var assemblyUri = new Uri(Assembly.GetEntryAssembly().Location);
                return Path.GetDirectoryName(assemblyUri.LocalPath);
            }
        }

        internal override GameWindow GetSupportedGameWindow(AppContextType type)
        {
            return type switch
            {
                AppContextType.iOS => new GameWindowSDL(),
                AppContextType.Headless => new GameWindowHeadless(),
                _ => null,
            };
        }

        public override List<GraphicsDeviceInformation> FindBestDevices(GameGraphicsParameters preferredParameters)
        {
            var gameWindowiOS = gameWindow as GameWindowSDL;
            if (gameWindowiOS != null)
            {
                var graphicsAdapter = GraphicsAdapterFactory.DefaultAdapter;
                var graphicsDeviceInfos = new List<GraphicsDeviceInformation>();
                var preferredGraphicsProfiles = preferredParameters.PreferredGraphicsProfile;
                foreach (var featureLevel in preferredGraphicsProfiles)
                {
                    // Check if this profile is supported.
                    if (graphicsAdapter.IsProfileSupported(featureLevel))
                    {
                        // Report the SDL window's actual size; formats stay caller-preferred (negotiated at swapchain creation).
                        var clientBounds = gameWindowiOS.ClientBounds;
                        var deviceInfo = new GraphicsDeviceInformation
                        {
                            Adapter = GraphicsAdapterFactory.DefaultAdapter,
                            GraphicsProfile = featureLevel,
                            PresentationParameters = new PresentationParameters(clientBounds.Width,
                                                                                clientBounds.Height,
                                                                                gameWindowiOS.NativeWindow)
                            {
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
            // No-op: the OS owns the surface size on iOS. Format changes are handled by the swapchain.
        }

        protected override void Destroy()
        {
            UnsubscribeAppLifecycle();
            base.Destroy();
        }

        private void UnsubscribeAppLifecycle()
        {
            var center = NSNotificationCenter.DefaultCenter;
            if (didBecomeActiveObserver != null)        { center.RemoveObserver(didBecomeActiveObserver);        didBecomeActiveObserver = null; }
            if (willResignActiveObserver != null)       { center.RemoveObserver(willResignActiveObserver);       willResignActiveObserver = null; }
            if (didEnterBackgroundObserver != null)     { center.RemoveObserver(didEnterBackgroundObserver);     didEnterBackgroundObserver = null; }
            if (willEnterForegroundObserver != null)    { center.RemoveObserver(willEnterForegroundObserver);    willEnterForegroundObserver = null; }
            if (didReceiveMemoryWarningObserver != null) { center.RemoveObserver(didReceiveMemoryWarningObserver); didReceiveMemoryWarningObserver = null; }
        }
    }
}
#endif
