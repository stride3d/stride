// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
using Stride.Core;
using Stride.Games;
using Silk.NET.Windowing.Sdl.Android;

namespace Stride.Starter
{
    using Resource = Stride.Games.Resource;
    
    public abstract class StrideActivity : SilkActivity
    {
        /// <summary>
        /// The game context of the game instance.
        /// </summary>
        protected GameContextAndroid GameContext;

        private StatusBarVisibility lastVisibility;
        private RelativeLayout mainLayout;
        private RingerModeIntentReceiver ringerModeIntentReceiver;

        protected override void OnRun()
        {
            // set up a listener to the android ringer mode (Normal/Silent/Vibrate)
            ringerModeIntentReceiver = new RingerModeIntentReceiver((AudioManager)GetSystemService(AudioService));
            RegisterReceiver(ringerModeIntentReceiver, new IntentFilter(AudioManager.RingerModeChangedAction));

            // Set the android global context
            if (PlatformAndroid.Context == null)
                PlatformAndroid.Context = this;

            // Unpack the files contained in the apk
            //await VirtualFileSystem.UnpackAPK();

            // setup the application view and stride game context
            SetupGameContext();
        }

        protected virtual void SetupGameContext()
        {
            // Create the Game context
            GameContext = new GameContextAndroid(null, FindViewById<RelativeLayout>(Resource.Id.EditTextLayout));
        }

        protected override void OnPause()
        {
            base.OnPause();

            UnregisterReceiver(ringerModeIntentReceiver);
        }

        protected override void OnResume()
        {
            base.OnResume();

            RegisterReceiver(ringerModeIntentReceiver, new IntentFilter(AudioManager.RingerModeChangedAction));
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
