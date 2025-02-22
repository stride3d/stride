// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_ANDROID
using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Silk.NET.Windowing.Sdl.Android;
using Stride.Core;
using Stride.Games;

namespace Stride.Starter
{
    using AndroidResource = Stride.Games.Resource;

    public abstract class StrideActivity : SilkActivity
    {
        /// <summary>
        /// The game context of the game instance.
        /// </summary>
        protected GameContextAndroid GameContext;

        private StatusBarVisibility lastVisibility;
        private RingerModeIntentReceiver ringerModeIntentReceiver;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameContext.RecreateEditTextPopupWindow = true;
        }

        protected override void OnRun()
        {
            // set up a listener to the android ringer mode (Normal/Silent/Vibrate)
            ringerModeIntentReceiver = new RingerModeIntentReceiver((AudioManager)GetSystemService(AudioService));
            RegisterReceivers();

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
            GameContext = new GameContextAndroid(null, this);
        }

        protected override void OnPause()
        {
            base.OnPause();

            UnregisterReceiver(ringerModeIntentReceiver);
        }

        protected override void OnResume()
        {
            base.OnResume();

            RegisterReceivers();
        }

        /// <summary>
        /// Method to create the popup window that appears when Stride requires user input from the EditText UI control.
        /// </summary>
        /// <param name="androidEditTextControl">The EditText that is used in place of Stride's EditText when user input is required.</param>
        /// <returns>The PopupWindow to display when user input is required from EditText.</returns>
        internal protected virtual PopupWindow CreateEditTextPopup(EditText androidEditTextControl)
        {
            var popupWindow = new PopupWindow(this)
            {
                Width = ViewGroup.LayoutParams.MatchParent,
                Height = ViewGroup.LayoutParams.MatchParent,
                Focusable = true,
                Touchable = true,
                //OutsideTouchable = true,
                SoftInputMode = SoftInput.AdjustPan | SoftInput.StateVisible,
                InputMethodMode = Android.Widget.InputMethod.Needed
            };
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                popupWindow.TouchModal = true;
            }
            popupWindow.SetBackgroundDrawable(new Android.Graphics.Drawables.ColorDrawable(Color.Transparent));
            var editTextOverlay = LayoutInflater.Inflate(AndroidResource.Layout.stride_popup_edittext, root: null) as ViewGroup;
            System.Diagnostics.Debug.Assert(editTextOverlay is not null, "Android Resource Layout for EditText is missing.");
            editTextOverlay.Click += (sender, e) => HideEditTextPopup(popupWindow);

            // We want to place the EditText in a container such that it appears like so:
            // [ [EditText] [OK] ]
            // Refer to stride_popup_edittext.xml
            var editTextContainerParams = new LinearLayout.LayoutParams(width: 0, height: ViewGroup.LayoutParams.WrapContent)
            {
                Weight = 1
            };
            var editTextContainer = editTextOverlay.FindViewById<LinearLayout>(AndroidResource.Id.StrideEditTextContainer);
            System.Diagnostics.Debug.Assert(editTextContainer is not null, "StrideEditTextContainer is missing in the Layout.");
            editTextContainer.AddView(androidEditTextControl, index: 0, editTextContainerParams);

            var editTextOkButton = editTextOverlay.FindViewById<Button>(AndroidResource.Id.StrideEditTextOkButton);
            System.Diagnostics.Debug.Assert(editTextOkButton is not null, "StrideEditTextOkButton is missing in the Layout.");
            editTextOkButton.Click += (sender, e) => HideEditTextPopup(popupWindow);

            popupWindow.ContentView = editTextOverlay;

            return popupWindow;
        }

        internal void ShowEditTextPopup(PopupWindow popupWindow)
        {
            if (popupWindow.IsShowing)
            {
                return;     // Already showing
            }
            RunOnUiThread(() =>
            {
                var rootView = Window.DecorView.RootView;
                System.Diagnostics.Debug.Assert(rootView is not null, "Window does not have a root view.");
                popupWindow.ShowAtLocation(rootView, GravityFlags.Bottom, 0, 0);
            });
        }

        internal void HideEditTextPopup(PopupWindow popupWindow)
        {
            if (!popupWindow.IsShowing)
            {
                return;     // Already hidden
            }
            RunOnUiThread(() =>
            {
                var inputMethodManager = GetSystemService(Context.InputMethodService) as InputMethodManager;
                System.Diagnostics.Debug.Assert(inputMethodManager is not null);
                if (inputMethodManager.IsActive)
                {
                    var rootView = Window.DecorView.RootView;
                    System.Diagnostics.Debug.Assert(rootView is not null, "Window does not have a root view.");
                    inputMethodManager.HideSoftInputFromWindow(rootView.WindowToken, HideSoftInputFlags.None);
                }

                popupWindow.Dismiss();
            });
        }

        private void RegisterReceivers()
        {
            var ringerModeIntentFilter = new IntentFilter(AudioManager.RingerModeChangedAction);
            if (OperatingSystem.IsAndroidVersionAtLeast(34))
            {
                RegisterReceiver(ringerModeIntentReceiver, ringerModeIntentFilter, ReceiverFlags.NotExported);
            }
            else
            {
                RegisterReceiver(ringerModeIntentReceiver, ringerModeIntentFilter);
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
