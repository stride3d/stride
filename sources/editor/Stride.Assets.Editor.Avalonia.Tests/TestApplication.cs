// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using Stride.Assets.Editor.Avalonia.Tests;

[assembly: AvaloniaTestApplication(typeof(TestApplication))]

namespace Stride.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The application the headless tests run in: the Fluent theme, the editor styles of Stride.Core.Presentation.Avalonia and the
/// control themes of Stride.Editor.Avalonia, included the way the GameStudio shell includes them. The plugins merge their own
/// themes when they initialize.
/// </summary>
public sealed class TestApplication : Application
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<TestApplication>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true });

    public override void Initialize()
    {
        var baseUri = new Uri("avares://Stride.Assets.Editor.Avalonia.Tests/");
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(baseUri)
        {
            Source = new Uri("avares://Stride.Core.Presentation.Avalonia/Themes/EditorStyles.axaml"),
        });
        Resources.MergedDictionaries.Add(new ResourceInclude(baseUri)
        {
            Source = new Uri("avares://Stride.Editor.Avalonia/Themes/Generic.axaml"),
        });
    }
}
