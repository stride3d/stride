// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if ANDROID
using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Window;
using Avalonia.Android;

namespace xunit.runner.stride;

// Avalonia 12 moved bootstrap into AvaloniaApplication (see Launcher.Application.Android.cs);
// this activity just handles Android-side lifecycle, back gestures, and reading the launch Intent.
// Derives directly from non-generic AvaloniaMainActivity — keeping the JNI activation-constructor
// shape one level over the generic base, since .NET Android's IL injection misbehaves with deeper
// generic hierarchies.
[Activity(
    Label = "Stride Tests",
    Theme = "@style/Theme.AppCompat.NoActionBar",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode | ConfigChanges.KeyboardHidden)]
public class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Stride.Core APIs (PlatformFolders, VirtualFileSystem) need the Android Context;
        // set it here since the test runner has no StrideActivity to do it.
        Stride.Core.PlatformAndroid.Context ??= this;

        // Non-interactive entry point for the host orchestration script:
        //   adb shell am start -n <pkg>/.MainActivity --es xunit_command run
        // Avalonia is already initialized by AvaloniaApplication.OnCreate (it ran first), so
        // we set the flag then trigger the headless run explicitly.
        if (Intent?.GetStringExtra("xunit_command") == "run")
        {
            App.HeadlessMode = true;
            // Optional --es xunit_filter "<vstest --filter expr>" narrows the run to matching tests.
            App.HeadlessFilter = Intent.GetStringExtra("xunit_filter");
            // Optional --es xunit_repeat "N" reruns the filtered set up to N times (stop on fail).
            App.HeadlessRepeat = Intent.GetStringExtra("xunit_repeat");
            App.TryStartHeadlessRun();
        }

        base.OnCreate(savedInstanceState);
    }

    private BackInvokedCallback? backCallback;

    // SDK 33+ predictive back: OnBackPressed isn't called when the app has opted in (default for
    // SDK 33+ targets), so route through the dispatcher. Register at OnResume/unregister at OnPause
    // with PriorityOverlay so we win over anything Avalonia or another layer registers.
    protected override void OnResume()
    {
        base.OnResume();
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            backCallback ??= new BackInvokedCallback(this);
            OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(
                IOnBackInvokedDispatcher.PriorityOverlay, backCallback);
        }
    }

    protected override void OnPause()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33) && backCallback != null)
            OnBackInvokedDispatcher.UnregisterOnBackInvokedCallback(backCallback);
        base.OnPause();
    }

    // Pre-API-33 path: predictive back isn't engaged, OnBackPressed still fires.
    public override void OnBackPressed()
    {
        if (App.HandleBackRequest?.Invoke() == true) return;
        base.OnBackPressed();
    }

    private sealed class BackInvokedCallback : Java.Lang.Object, IOnBackInvokedCallback
    {
        private readonly Activity activity;
        public BackInvokedCallback(Activity activity) => this.activity = activity;
        public void OnBackInvoked()
        {
            if (App.HandleBackRequest?.Invoke() != true)
                activity.Finish();
        }
    }
}
#endif
