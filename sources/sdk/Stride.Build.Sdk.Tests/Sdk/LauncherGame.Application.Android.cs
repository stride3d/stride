// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if ANDROID
using System;
using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Stride.Graphics.Regression;
using xunit.runner.stride.ViewModels;

namespace xunit.runner.stride;

// Graphics-regression variant of AvaloniaApplication (selected by StrideGraphicsRegression=true).
// All App.* delegates the runner reads on OnFrameworkInitializationCompleted must be assigned
// here, BEFORE base.OnCreate fires Avalonia init — MainActivity.OnCreate runs after init, which
// would leave a SubscribeImageComparison?.Invoke against a null delegate and no image events
// reaching the runner UI.
[Application]
public class AvaloniaApplication : AvaloniaAndroidApplication<App>
{
    protected AvaloniaApplication(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer) { }

    public override void OnCreate()
    {
        App.TestAssembly = GetType().Assembly;
        App.SetInteractiveMode = interactiveMode => GameTestBase.ForceInteractiveMode = interactiveMode;
        App.SetForceSaveImage = forceSaveImage => GameTestBase.ForceSaveImageOnSuccess = forceSaveImage;
        App.SubscribeImageComparison = subscribe => ImageTester.ImageComparisonCompleted += (s, e) =>
            subscribe(new ImageCompareResult(e.CurrentPath, e.ReferencePath, e.Passed, e.Stats.ToString()));
        base.OnCreate();
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder).WithInterFont();
}
#endif
