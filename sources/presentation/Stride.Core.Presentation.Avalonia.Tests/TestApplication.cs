// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using Stride.Core.Presentation.Avalonia.Tests;

[assembly: AvaloniaTestApplication(typeof(TestApplication))]
// One application for the whole assembly: rebuilding it for each test resets the UI thread, and the headless platform
// setup then sometimes runs against the render loop of another thread (InvalidOperationException in
// DefaultRenderLoop.Add, https://github.com/AvaloniaUI/Avalonia/issues/22021)
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerAssembly)]

namespace Stride.Core.Presentation.Avalonia.Tests;

/// <summary>
/// The application the headless tests run in: the Fluent theme, the theme of AvaloniaEdit (the code editor) and the editor
/// styles of Stride.Core.Presentation.Avalonia.
/// </summary>
public sealed class TestApplication : Application
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<TestApplication>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true });

    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://Stride.Core.Presentation.Avalonia.Tests/"))
        {
            Source = new Uri("avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml"),
        });
        Styles.Add(new StyleInclude(new Uri("avares://Stride.Core.Presentation.Avalonia.Tests/"))
        {
            Source = new Uri("avares://Stride.Core.Presentation.Avalonia/Themes/EditorStyles.axaml"),
        });
    }
}
