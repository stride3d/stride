// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;

namespace Stride.Core.Assets;

partial class NuGetAssemblyResolver
{
    // Use a inner-class so that UI-stuff are lazy loaded (especially after having their dependencies resolved)
    private static class ResolverUILauncher
    {
        public static void Run(TaskCompletionSource dialogNotNeeded, TaskCompletionSource dialogClosed, Logger logger)
        {
            NuGetResolver.NugetResolverApp.Run((app, ___) =>
            {
                app.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
                var splashScreen = new NuGetResolver.SplashScreenWindow();
                splashScreen.Show();
                // Register log
                logger.SetupLogAction((level, message) => splashScreen.SetupLog(level, message));

                dialogNotNeeded.Task.ContinueWith(__ => splashScreen.CloseApp());
                splashScreen.Closed += (sender2, e2) => NuGetResolver.SplashScreenWindow.InvokeShutDown();

                app.Run(splashScreen);
                splashScreen.Close();
            });
            dialogClosed.SetResult();
        }
    }
}
