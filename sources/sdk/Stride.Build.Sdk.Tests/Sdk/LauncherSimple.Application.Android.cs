// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if ANDROID
using System;
using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace xunit.runner.stride;

// Avalonia 12 moved app bootstrap from `AvaloniaMainActivity<TApp>` to a custom Android Application
// class. Compiled into each test assembly by Stride.Build.Sdk.Tests so GetType().Assembly resolves
// to the test assembly — `App.TestAssembly` must be set before base.OnCreate fires Avalonia init,
// which in turn creates MainView and TestsViewModel (the consumer of TestAssembly for discovery).
[Application]
public class AvaloniaApplication : AvaloniaAndroidApplication<App>
{
    protected AvaloniaApplication(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer) { }

    public override void OnCreate()
    {
        App.TestAssembly = GetType().Assembly;
        base.OnCreate();
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder).WithInterFont();
}
#endif
