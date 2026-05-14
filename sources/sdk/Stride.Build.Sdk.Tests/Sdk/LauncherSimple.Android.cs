// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if ANDROID
using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Window;
using Avalonia;
using Avalonia.Android;

namespace xunit.runner.stride;

// Compiled into each test assembly by Stride.Build.Sdk.Tests so GetType().Assembly resolves to
// the test assembly (Android has no entry assembly for the runner to discover tests from).
// Derives directly from AvaloniaMainActivity<App> — the standard one-level shape — because
// .NET Android's activation-constructor IL injection silently bails through an extra
// non-generic intermediate over a generic Java base, breaking JNI handle activation.
[Activity(
    Label = "Stride Tests",
    Theme = "@style/Theme.AppCompat.NoActionBar",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode | ConfigChanges.KeyboardHidden)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Stride.Core APIs (PlatformFolders, VirtualFileSystem) need the Android Context;
        // set it here since the test runner has no StrideActivity to do it.
        Stride.Core.PlatformAndroid.Context ??= this;

        // Per-test-assembly: GetType().Assembly is the test assembly, which is what the App
        // uses to discover tests (the runner itself lives in xunit.runner.stride).
        App.TestAssembly = GetType().Assembly;

        base.OnCreate(savedInstanceState);

        // Android 15+ forces edge-to-edge for SDK 35+ targets; pad the content view by the
        // system-bar insets so the runner UI doesn't render under the status bar.
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var content = FindViewById<View>(Android.Resource.Id.Content);
            content?.SetOnApplyWindowInsetsListener(new SystemBarInsetPadding());
            content?.RequestApplyInsets();
        }

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

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder).WithInterFont();

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

    [System.Runtime.Versioning.SupportedOSPlatform("android30.0")]
    private sealed class SystemBarInsetPadding : Java.Lang.Object, View.IOnApplyWindowInsetsListener
    {
        public WindowInsets? OnApplyWindowInsets(View v, WindowInsets insets)
        {
            var bars = insets.GetInsets(WindowInsets.Type.SystemBars());
            v.SetPadding(bars.Left, bars.Top, bars.Right, bars.Bottom);
            return insets;
        }
    }
}
#endif
