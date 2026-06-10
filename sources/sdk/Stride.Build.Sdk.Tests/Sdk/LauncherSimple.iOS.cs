// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if IOS
using Avalonia;
using Avalonia.iOS;
using Foundation;
using UIKit;

namespace xunit.runner.stride;

// Compiled into each test assembly by Stride.Build.Sdk.Tests so GetType().Assembly resolves to
// the test assembly (iOS has no entry assembly for the runner to discover tests from).
// Mirrors LauncherSimple.Android.cs but built around the UIApplicationDelegate / Avalonia.iOS
// entry shape instead of an [Activity].
public class Program
{
    // iOS requires an explicit Main; UIApplication.Main hands control to the AppDelegate below,
    // whose CustomizeAppBuilder (called during init, before App.Initialize) wires the per-process
    // statics the Avalonia app reads at startup.
    public static void Main(string[] args) => UIApplication.Main(args, null, typeof(AppDelegate));
}

[Register(nameof(AppDelegate))]
public partial class AppDelegate : AvaloniaAppDelegate<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        // Per-test-assembly: GetType().Assembly is the test assembly, which is what the App
        // uses to discover tests (the runner itself lives in xunit.runner.stride).
        App.TestAssembly = GetType().Assembly;

        // Non-interactive entry point for the host orchestration script:
        //   xcrun simctl launch <udid> <bundleid> --xunit-command run [--xunit-filter <expr>]
        // Avalonia still hosts the Stride game surfaces, but RunAll fires immediately and the
        // process exits with the failed-test count when done. Mirrors the Android Intent extras.
        var args = NSProcessInfo.ProcessInfo.Arguments;
        for (int i = 0; i + 1 < args.Length; i++)
        {
            if (args[i] == "--xunit-command" && args[i + 1] == "run")
                App.HeadlessMode = true;
            else if (args[i] == "--xunit-filter")
                App.HeadlessFilter = args[i + 1];
            else if (args[i] == "--xunit-repeat")
                App.HeadlessRepeat = args[i + 1];
        }

        return base.CustomizeAppBuilder(builder).WithInterFont();
    }
}
#endif
