// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using Stride.Core.Assets.Editor.Avalonia.Tests;

[assembly: AvaloniaTestApplication(typeof(TestApplication))]

namespace Stride.Core.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The application the headless tests run in: the Fluent theme plus the editor styles of Stride.Core.Presentation.Avalonia.
/// </summary>
public sealed class TestApplication : Application
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<TestApplication>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true });

    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://Stride.Core.Assets.Editor.Avalonia.Tests/"))
        {
            Source = new Uri("avares://Stride.Core.Presentation.Avalonia/Themes/EditorStyles.axaml"),
        });
    }
}
