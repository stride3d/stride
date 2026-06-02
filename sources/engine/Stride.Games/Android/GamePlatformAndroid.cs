// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_ANDROID
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Stride.Graphics;

namespace Stride.Games
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

        // Lifecycle/memory observers; kept so Destroy() can unregister.
        private LifecycleCallbacks lifecycleCallbacks;
        private MemoryCallbacks memoryCallbacks;

        public GamePlatformAndroid(GameBase game) : base(game)
        {
            PopulateFullName();
            SubscribeAppLifecycle();
        }

        // Application-level callbacks rather than subclassing Activity, so we don't depend on
        // which launcher class the host app uses (Avalonia.Android, custom, etc.).
        private void SubscribeAppLifecycle()
        {
            var app = Application.Context as Application;
            if (app == null) return;
            lifecycleCallbacks = new LifecycleCallbacks(this);
            memoryCallbacks = new MemoryCallbacks(this);
            app.RegisterActivityLifecycleCallbacks(lifecycleCallbacks);
            app.RegisterComponentCallbacks(memoryCallbacks);
        }

        // Map the Android Activity lifecycle onto the shared GamePlatform entry points, which
        // marshal the work onto the game thread. onStart/onStop bracket foreground visibility;
        // onResume/onPause bracket input focus. Callbacks fire for every Activity in the process,
        // so the started/resumed counts gate the app-wide transition to the 0<->1 edge — otherwise
        // intra-app navigation (game -> settings, transient activities) would spuriously drain the
        // GPU or pause audio. All callbacks run on the UI thread, so plain counters are sufficient.
        private class LifecycleCallbacks : Java.Lang.Object, Application.IActivityLifecycleCallbacks
        {
            private readonly GamePlatformAndroid platform;
            private int startedCount;
            private int resumedCount;
            public LifecycleCallbacks(GamePlatformAndroid p) { platform = p; }

            public void OnActivityCreated(Activity activity, Bundle savedInstanceState) { }
            public void OnActivityDestroyed(Activity activity) { }
            public void OnActivitySaveInstanceState(Activity activity, Bundle outState) { }

            public void OnActivityStarted(Activity activity) { if (++startedCount == 1) platform.NotifyAppForeground(); }
            public void OnActivityStopped(Activity activity) { if (--startedCount == 0) platform.NotifyAppBackground(); }
            public void OnActivityResumed(Activity activity) { if (++resumedCount == 1) platform.NotifyAppActivated(); }
            public void OnActivityPaused(Activity activity) { if (--resumedCount == 0) platform.NotifyAppDeactivated(); }
        }

        private class MemoryCallbacks : Java.Lang.Object, IComponentCallbacks2
        {
            private readonly GamePlatformAndroid platform;
            public MemoryCallbacks(GamePlatformAndroid p) { platform = p; }

            public void OnConfigurationChanged(Configuration newConfig) { }
            public void OnLowMemory() => platform.NotifyAppMemoryWarning();
            public void OnTrimMemory([GeneratedEnum] TrimMemory level)
            {
                // Only foreground (Running*) levels; background levels fire after NotifyAppBackground has already GCed.
                if (level >= TrimMemory.RunningModerate)
                    platform.NotifyAppMemoryWarning();
            }
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
                AppContextType.Android => new GameWindowSDL(),
                AppContextType.Headless => new GameWindowHeadless(),
                _ => null,
            };
        }

        public override List<GraphicsDeviceInformation> FindBestDevices(GameGraphicsParameters preferredParameters)
        {
            var gameWindowAndroid = gameWindow as GameWindowSDL;
            if (gameWindowAndroid != null)
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
                        var clientBounds = gameWindowAndroid.ClientBounds;
                        var deviceInfo = new GraphicsDeviceInformation
                        {
                            Adapter = GraphicsAdapterFactory.DefaultAdapter,
                            GraphicsProfile = featureLevel,
                            PresentationParameters = new PresentationParameters(clientBounds.Width, clientBounds.Height,
                                gameWindowAndroid.NativeWindow)
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
            // No-op: the OS owns the surface size on Android. Format changes are handled by the swapchain.
        }

        protected override void Destroy()
        {
            UnsubscribeAppLifecycle();
            base.Destroy();
        }

        private void UnsubscribeAppLifecycle()
        {
            var app = Application.Context as Application;
            if (app == null) return;
            if (lifecycleCallbacks != null) { app.UnregisterActivityLifecycleCallbacks(lifecycleCallbacks); lifecycleCallbacks = null; }
            if (memoryCallbacks != null)    { app.UnregisterComponentCallbacks(memoryCallbacks);          memoryCallbacks = null; }
        }
    }
}
#endif
