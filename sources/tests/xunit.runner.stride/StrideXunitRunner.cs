// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace xunit.runner.stride;

public class StrideXunitRunner
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    public static void Main(string[] args, Action<bool> setInteractiveMode = null)
    {
        var builder = BuildAvaloniaApp()
            .SetupWithLifetime(new ClassicDesktopStyleApplicationLifetime());
        if (builder.Instance is App app)
        {
            app.setInteractiveMode = setInteractiveMode;
            app.Run(app.cts.Token);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
