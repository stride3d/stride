// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_ANDROID
using System;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Content;
using Android.Media;
using Android.Runtime;
using OpenTK.Graphics;
using OpenTK.Platform.Android;
using Stride.Core;
using Stride.Games;
using Stride.Games.Android;
using Stride.Graphics.OpenGL;

namespace Stride.Starter
{
    using Resource = Stride.Games.Resource;
    
    // NOTE: the class should implement View.IOnSystemUiVisibilityChangeListener but doing so will prevent the engine to work on Android below 3.0 (API Level 11 is mandatory).
    // So the methods are implemented but the class does not implement View.IOnSystemUiVisibilityChangeListener.
    // Maybe this will change when support for API Level 10 is dropped
    // TODO: make this class implement View.IOnSystemUiVisibilityChangeListener when support of Android < 3.0 is dropped.
    public class AndroidStrideActivity : Activity
    {
        /// <summary>
        /// The game view, internally a SurfaceView
        /// </summary>
        protected AndroidStrideGameView GameView;

        /// <summary>
        /// The game context of the game instance.
        /// </summary>
        protected GameContextAndroid GameContext;

        /// <summary>
        /// The instance of the game to run.
        /// </summary>
        protected GameBase Game;

        private Action setFullscreenViewCallback;
        private StatusBarVisibility lastVisibility;
        private RelativeLayout mainLayout;
        private RingerModeIntentReceiver ringerModeIntentReceiver;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set the android global context
            if (PlatformAndroid.Context == null)
                PlatformAndroid.Context = this;

            // Remove the title bar
            RequestWindowFeature(WindowFeatures.NoTitle);

            // Unpack the files contained in the apk
            //await VirtualFileSystem.UnpackAPK();
            
            // Create the Android OpenGl view
            GameView = new AndroidStrideGameView(this);

            // setup the application view and stride game context
            SetupGameViewAndGameContext();

            // set up a listener to the android ringer mode (Normal/Silent/Vibrate)
            ringerModeIntentReceiver = new RingerModeIntentReceiver((AudioManager)GetSystemService(AudioService));
            RegisterReceiver(ringerModeIntentReceiver, new IntentFilter(AudioManager.RingerModeChangedAction));

            SetFullscreenView();
            InitializeFullscreenViewCallback();
        }

        public void OnSystemUiVisibilityChange(StatusBarVisibility visibility)
        {
            //Log.Debug("Stride", "OnSystemUiVisibilityChange: visibility=0x{0:X8}", (int)visibility);
            var diffVisibility = lastVisibility ^ visibility;
            lastVisibility = visibility;
            if ((((int)diffVisibility & (int)SystemUiFlags.LowProfile) != 0) && (((int)visibility & (int)SystemUiFlags.LowProfile) == 0))
            {
                // visibility has changed out of low profile mode; change it back, which requires a delay to work properly:
                // http://stackoverflow.com/questions/11027193/maintaining-lights-out-mode-view-setsystemuivisibility-across-restarts
                RemoveFullscreenViewCallback();
                PostFullscreenViewCallback();
            }
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            //Log.Debug("Stride", "OnWindowFocusChanged: hasFocus={0}", hasFocus);
            base.OnWindowFocusChanged(hasFocus);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            {
                // use fullscreen immersive mode
                if (hasFocus)
                {
                    SetFullscreenView();
                }
            }
            // TODO: uncomment this once the class implements View.IOnSystemUiVisibilityChangeListener.
            /*else if (Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwich)
            {
                // use fullscreen low profile mode, with a delay
                if (hasFocus)
                {
                    RemoveFullscreenViewCallback();
                    PostFullscreenViewCallback();
                }
                else
                {
                    RemoveFullscreenViewCallback();
                }
            }*/
        }

        private void SetupGameViewAndGameContext()
        {
            // Force the format of the window color buffer (avoid conversions)
            // TODO: PDX-364: depth format is currently hard coded (need to investigate how it can be transmitted)
            var windowColorBufferFormat = Format.Rgba8888;
            
            // Set the main view of the Game
            var context = PlatformAndroid.Context;
            if (context != this)
            {
                try
                {
                    var windowManager = context.GetSystemService(WindowService).JavaCast<IWindowManager>();
                    var mainView = LayoutInflater.From(context).Inflate(Resource.Layout.Game, null);
                    windowManager.AddView(mainView, new WindowManagerLayoutParams(WindowManagerTypes.SystemAlert, WindowManagerFlags.Fullscreen, windowColorBufferFormat));
                    mainLayout = mainView.FindViewById<RelativeLayout>(Resource.Id.GameViewLayout);
                }
                catch (Exception) {} // don't have the Alert permissions
            }
            if (mainLayout == null)
            {
                Window.SetFormat(windowColorBufferFormat);
                SetContentView(Resource.Layout.Game);
                mainLayout = FindViewById<RelativeLayout>(Resource.Id.GameViewLayout);
            }

            // Set the content of the view
            mainLayout.AddView(GameView);

            // Create the Game context
            GameContext = new GameContextAndroid(GameView, FindViewById<RelativeLayout>(Resource.Id.EditTextLayout));
        }

        protected override void OnPause()
        {
            base.OnPause();

            UnregisterReceiver(ringerModeIntentReceiver);

            GameView?.Pause();
        }

        protected override void OnResume()
        {
            base.OnResume();

            RegisterReceiver(ringerModeIntentReceiver, new IntentFilter(AudioManager.RingerModeChangedAction));

            GameView?.Resume();
        }

        private void InitializeFullscreenViewCallback()
        {
            //Log.Debug("Stride", "InitializeFullscreenViewCallback");
            if ((Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwich) && (Build.VERSION.SdkInt < BuildVersionCodes.Kitkat))
            {
                setFullscreenViewCallback = SetFullscreenView;
                // TODO: uncomment this once the class implements View.IOnSystemUiVisibilityChangeListener. Right now only Kitkat supports full screen    
                //Window.DecorView.SetOnSystemUiVisibilityChangeListener(this);
            }
        }

        private void PostFullscreenViewCallback()
        {
            //Log.Debug("Stride", "PostFullscreenViewCallback");
            var handler = Window.DecorView.Handler;
            if (handler != null)
            {
                // post callback with delay, which needs to be longer than transient status bar timeout, otherwise it will have no effect!
                handler.PostDelayed(setFullscreenViewCallback, 4000);
            }
        }

        private void RemoveFullscreenViewCallback()
        {
            //Log.Debug("Stride", "RemoveFullscreenViewCallback");
            var handler = Window.DecorView.Handler;
            if (handler != null)
            {
                // remove any pending callbacks
                handler.RemoveCallbacks(setFullscreenViewCallback);
            }
        }

        private void SetFullscreenView()
        {
            //Log.Debug("Stride", "SetFullscreenView");
            if (Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwich) // http://redth.codes/such-android-api-levels-much-confuse-wow/
            {
                var view = Window.DecorView;
                int flags = (int)view.SystemUiVisibility;
                if (Build.VERSION.SdkInt >= BuildVersionCodes.JellyBean)
                {
                    // http://developer.android.com/training/system-ui/status.html
                    flags |= (int)(SystemUiFlags.Fullscreen | SystemUiFlags.LayoutFullscreen | SystemUiFlags.LayoutStable);
                }
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
                {
                    // http://developer.android.com/training/system-ui/immersive.html; the only mode that can really hide the nav bar
                    flags |= (int)(SystemUiFlags.HideNavigation | SystemUiFlags.ImmersiveSticky | SystemUiFlags.LayoutHideNavigation);
                }
                else
                {
                    // http://developer.android.com/training/system-ui/dim.html; low profile or 'lights out' mode to minimize the nav bar
                    flags |= (int)SystemUiFlags.LowProfile;
                }
                view.SystemUiVisibility = (StatusBarVisibility)flags;
            }
        }

        private class RingerModeIntentReceiver : BroadcastReceiver
        {
            private readonly AudioManager audioManager;

            private int muteCounter;

            public RingerModeIntentReceiver(AudioManager audioManager)
            {
                this.audioManager = audioManager;
            }

            public override void OnReceive(Context context, Intent intent)
            {
                UpdateMusicMuteStatus();
            }

            private void UpdateMusicMuteStatus()
            {
                switch (audioManager.RingerMode)
                {
                    case RingerMode.Normal:
                        for (int i = 0; i < muteCounter; i++)
                            audioManager.SetStreamMute(Stream.Music, false);
                        muteCounter = 0;
                        break;
                    case RingerMode.Silent:
                    case RingerMode.Vibrate:
                        audioManager.SetStreamMute(Stream.Music, true);
                        ++muteCounter;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
#endif
