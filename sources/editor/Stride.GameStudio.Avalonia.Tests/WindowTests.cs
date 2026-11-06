// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The application and its windows load their markup.
/// </summary>
public sealed class WindowTests
{
    [AvaloniaFact]
    public void TheApplicationIsTheShellsApp()
    {
        var app = Assert.IsType<App>(Application.Current);
        Assert.Contains(app.Styles, x => x is FluentTheme);
        // The control themes of the editor host are merged (the preview view)
        Assert.True(app.TryGetResource("StrideLogo", null, out _));
    }

    [AvaloniaFact]
    public async Task TheSettingsWindowShowsTheEditorSettings()
    {
        var window = new SettingsWindow
        {
            DataContext = new SettingsViewModel(App.InitializeServiceProvider(), EditorSettings.SettingsContainer.CurrentProfile),
        };
        window.Show();
        try
        {
            Assert.True(window.IsVisible);
            var viewModel = Assert.IsType<SettingsViewModel>(window.DataContext);
            Assert.NotEmpty(viewModel.Categories);
            Assert.NotNull(viewModel.SelectedCategory);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheAboutWindowShowsTheEditorVersion()
    {
        var window = new AboutWindow();
        window.Show();
        try
        {
            Assert.True(window.IsVisible);
            // The version shown is the NuGet version of the engine the editor was built with
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), x => x.Inlines?.Text?.Contains(Stride.StrideVersion.NuGetVersion) == true);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheDebugWindowShowsTheSessionsLog()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new Stride.Assets.Sprite.SpriteSheetAsset()));
        var window = new DebugWindow { ViewModel = new DebugWindowViewModel(fixture.ServiceProvider) };
        window.Show();
        try
        {
            Assert.True(window.IsVisible);
            Assert.NotEmpty(window.GetVisualDescendants().OfType<TabControl>());
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheMainWindowOpensEmptyAndWithASession()
    {
        using var fixture = ShellSessionFixture.Create(("Sheet", new Stride.Assets.Sprite.SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            Assert.True(window.IsVisible);
            Assert.Equal(Stride.GameStudio.Avalonia.Helpers.StrideGameStudio.EditorTitle, fixture.Main.Title);

            Assert.True(await fixture.Main.OpenSession(fixture.PackagePath));
            await WindowTestHelper.PumpAsync();

            Assert.NotNull(fixture.Main.Session);
            Assert.Single(window.GetVisualDescendants().OfType<SolutionExplorerView>());
        }
        finally
        {
            await window.CloseAsync();
        }
    }
}
