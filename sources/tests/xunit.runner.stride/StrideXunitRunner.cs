// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace xunit.runner.stride;

public static class StrideXunitRunner
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    public static void Main(string[] _, Action<bool>? setInteractiveMode = null, Action<bool>? setForceSaveImage = null)
    {
        var builder = BuildAvaloniaApp(setInteractiveMode, setForceSaveImage)
            .SetupWithLifetime(new ClassicDesktopStyleApplicationLifetime());
        if (builder.Instance is App app)
        {
            app.Run(app.cts.Token);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(Action<bool>? setInteractiveMode = null, Action<bool>? setForceSaveImage = null)
        => AppBuilder.Configure(() => new App { setInteractiveMode = setInteractiveMode, setForceSaveImage = setForceSaveImage })
            .UsePlatformDetect()
            .With(new Win32PlatformOptions
            {
                // Use Software rendering, otherwise default renderer (OpenGL) interfere with GPU capture tools such as RenderDoc
                RenderingMode = new[] { Win32RenderingMode.Software }
            })
            .WithInterFont()
            .LogToTrace();
}
