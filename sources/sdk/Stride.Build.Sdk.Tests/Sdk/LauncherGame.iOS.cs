// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if IOS
using Avalonia;
using Avalonia.iOS;
using Foundation;
using Stride.Graphics.Regression;
using UIKit;
using xunit.runner.stride.ViewModels;

namespace xunit.runner.stride;

// Graphics-regression iOS variant — mirrors LauncherSimple.iOS.cs but also wires
// GameTestBase callbacks for the test runner's Interactive Mode / Force Save Image toggles.
// Selected by Stride.Build.Sdk.Tests when StrideGraphicsRegression=true.
public class Program
{
    public static void Main(string[] args) => UIApplication.Main(args, null, typeof(AppDelegate));
}

[Register(nameof(AppDelegate))]
public partial class AppDelegate : AvaloniaAppDelegate<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        App.TestAssembly = GetType().Assembly;

        // Match the desktop LauncherGame wiring: route the runner-UI checkboxes to the per-process
        // GameTestBase flags. Assigned before App.Initialize so the App reads them at startup.
        App.SetInteractiveMode = interactiveMode => GameTestBase.ForceInteractiveMode = interactiveMode;
        App.SetForceSaveImage = forceSaveImage => GameTestBase.ForceSaveImageOnSuccess = forceSaveImage;
        App.SubscribeImageComparison = subscribe => ImageTester.ImageComparisonCompleted += (s, e) =>
            subscribe(new ImageCompareResult(e.CurrentPath, e.ReferencePath, e.Passed, e.Stats.ToString()));

        // Non-interactive entry point for the host orchestration script:
        //   xcrun simctl launch <udid> <bundleid> --xunit-command run
        // Avalonia still hosts the Stride game surfaces, but RunAll fires immediately and the
        // process exits with the failed-test count when done. Mirrors the Android Intent extra.
        var args = NSProcessInfo.ProcessInfo.Arguments;
        for (int i = 0; i + 1 < args.Length; i++)
        {
            if (args[i] == "--xunit-command" && args[i + 1] == "run")
            {
                App.HeadlessMode = true;
                break;
            }
        }

        return base.CustomizeAppBuilder(builder).WithInterFont();
    }
}
#endif
