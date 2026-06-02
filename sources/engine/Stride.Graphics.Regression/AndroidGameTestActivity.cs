// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_ANDROID
using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Window;
using Stride.Engine;
using Stride.Starter;

namespace Stride.Graphics.Regression
{
    // Handle these config changes in-place — the default behavior is destroy+recreate, which fires
    // OnDestroy → Destroyed event → the GameTester finally-block clears GameToStart, leaving the
    // recreated activity's OnRun dereferencing a null game.
    [Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode | ConfigChanges.KeyboardHidden)]
    public class AndroidGameTestActivity : StrideActivity
    {
        public static Game GameToStart;
        public Game Game;

        public static event EventHandler<EventArgs> Destroyed;

        private IOnBackInvokedCallback backInvokedCallback;

        protected override void OnRun()
        {
            base.OnRun();

            if (Game == null) // application can be restarted
            {
                Game = GameToStart;
                Game.Exiting += Game_Exiting;
            }
            Game.Run(GameContext);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Android 13+ delivers back via OnBackInvokedDispatcher (predictive-back gesture);
            // OnBackPressed is bypassed entirely when the dispatcher has a callback registered.
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                backInvokedCallback = new BackInvokedCallback(this);
                OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(
                    IOnBackInvokedDispatcher.PriorityDefault, backInvokedCallback);
            }
        }

        // SDL on Android consumes KEYCODE_BACK as a regular input event before it reaches
        // OnBackPressed. Intercept at dispatch time so the hardware/legacy back button works.
        public override bool DispatchKeyEvent(KeyEvent e)
        {
            if (e.KeyCode == Keycode.Back && e.Action == KeyEventActions.Up)
            {
                RequestExit();
                return true;
            }
            return base.DispatchKeyEvent(e);
        }

        private void RequestExit()
        {
            // Game_Exiting → Finish() finishes the activity once the game loop honors the exit.
            Game?.Exit();
        }

        private sealed class BackInvokedCallback : Java.Lang.Object, IOnBackInvokedCallback
        {
            private readonly AndroidGameTestActivity activity;
            public BackInvokedCallback(AndroidGameTestActivity activity) => this.activity = activity;
            public void OnBackInvoked() => activity.RequestExit();
        }

        void Game_Exiting(object sender, EventArgs e)
        {
            Finish();
        }

        protected override void OnDestroy()
        {
            if (backInvokedCallback != null && OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                OnBackInvokedDispatcher.UnregisterOnBackInvokedCallback(backInvokedCallback);
                backInvokedCallback = null;
            }
            Game?.Dispose();

            base.OnDestroy();

            var handler = Destroyed;
            handler?.Invoke(this, EventArgs.Empty);
        }
    }
}
#endif
