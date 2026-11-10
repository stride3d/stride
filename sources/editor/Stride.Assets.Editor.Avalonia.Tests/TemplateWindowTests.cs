// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.TemplateEngine.Abstractions;
using Stride.Assets.Editor.Avalonia.Templates;
using Stride.Assets.Editor.Templates;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The dialogs the dotnet new, code library and plugin generators show before they run.
/// </summary>
public sealed class TemplateWindowTests
{
    [AvaloniaFact]
    public async Task TheParametersWindowShowsOneRowPerParameterAndTheAssetPacks()
    {
        var template = new FakeTemplateInfo(
            "Stride.Templates.Game",
            "Test game",
            FakeTemplateParameter.NameParameter(),
            FakeTemplateParameter.Choice(MultiChoiceTemplateParameterViewModel.PlatformsParameterName, "host", true, "windows", "linux", "macos", "host"),
            FakeTemplateParameter.Bool("HDR", true, "Use HDR rendering pipeline"),
            FakeTemplateParameter.Choice("graphicsProfile", "10.0", false, "9.0", "10.0", "11.0"),
            FakeTemplateParameter.Text("orientation", "Default"))
        {
            Description = "Create a game for the platforms you select",
        };
        var packs = Task.FromResult<IReadOnlyList<ITemplateInfo>>([new FakeTemplateInfo("Stride.AssetPack.Particles", "Particles pack") { Description = "Fire and smoke" }]);
        var viewModel = new DotNetNewTemplateParametersViewModel(new ViewModelServiceProvider([DispatcherService.Create()]), template, packs);
        var window = new DotNetNewTemplateParametersWindow { DataContext = viewModel };
        window.Show();
        try
        {
            await PumpAsync();
            Assert.True(window.IsVisible);
            Assert.Same(viewModel, window.Parameters);
            Assert.Equal("New project — Test game", window.Title);
            // The name of the template in the header band, as in WPF
            var band = window.FindControl<Border>("HeaderBand")!;
            Assert.True(band.IsVisible);
            Assert.Equal(viewModel.TemplateName, Assert.IsType<TextBlock>(band.Child).Text);

            // One row per user-facing parameter, with the control of its type
            Assert.Equal(4, window.ParametersList.ItemCount);
            var rows = window.ParametersList.GetVisualDescendants().ToList();
            Assert.Contains(rows.OfType<CheckBox>(), x => x.Content is "Enabled" || x.DataContext is BoolTemplateParameterViewModel);
            Assert.Equal(3, rows.OfType<CheckBox>().Count(x => x.DataContext is TemplateChoiceViewModel));
            Assert.Single(rows.OfType<ComboBox>());
            // The combo box template holds a text box of its own
            Assert.Single(rows.OfType<TextBox>(), x => x.DataContext is TextTemplateParameterViewModel && x.Parent is StackPanel);
            Assert.Contains(rows.OfType<TextBlock>(), x => x.Text == "HDR — Use HDR rendering pipeline");

            // The asset packs of the template
            Assert.True(window.AssetPacksPanel.IsVisible);
            Assert.False(window.AssetPacksDownload.IsVisible);
            Assert.False(window.NoAssetPacks.IsVisible);
            Assert.Equal(1, window.AssetPacksList.ItemCount);
            var pack = Assert.Single(window.AssetPacksList.GetVisualDescendants().OfType<CheckBox>());
            Assert.False(pack.IsChecked);
            window.Click(pack);
            await PumpAsync();
            Assert.True(viewModel.AssetPacks[0].IsChecked);
            Assert.Equal(["Stride.AssetPack.Particles"], viewModel.SelectedAssetPackIdentities);

            // Accepting closes the window; the prompt reads the view model
            window.Click(window.OkButton);
            await WaitUntilAsync(() => !window.IsVisible, "the window to close");
        }
        finally
        {
            viewModel.Destroy();
            await CloseAsync(window);
        }
    }

    [AvaloniaFact]
    public async Task TheParametersWindowShowsTheAssetPackDownload()
    {
        var packs = new TaskCompletionSource<IReadOnlyList<ITemplateInfo>>();
        var viewModel = new DotNetNewTemplateParametersViewModel(new ViewModelServiceProvider([DispatcherService.Create()]), new FakeTemplateInfo("Stride.Templates.Game", "Test game"), packs.Task);
        var window = new DotNetNewTemplateParametersWindow { DataContext = viewModel };
        window.Show();
        try
        {
            await PumpAsync();
            Assert.Equal(0, window.ParametersList.ItemCount);
            Assert.True(window.AssetPacksPanel.IsVisible);
            Assert.True(window.AssetPacksDownload.IsVisible);
            Assert.False(window.NoAssetPacks.IsVisible);

            // No pack could be downloaded
            packs.SetResult([]);
            await WaitUntilAsync(() => viewModel.NoAssetPacksAvailable, "the asset packs to load");
            await PumpAsync();
            Assert.False(window.AssetPacksDownload.IsVisible);
            Assert.True(window.NoAssetPacks.IsVisible);

            window.Click(window.CancelButton);
            await WaitUntilAsync(() => !window.IsVisible, "the window to close");
        }
        finally
        {
            viewModel.Destroy();
            await CloseAsync(window);
        }
    }

    [AvaloniaFact]
    public async Task TheLibraryWindowValidatesTheNames()
    {
        var library = new ProjectLibraryViewModel("MyLibrary", name => name == "Taken", referencingProjects: ["MyGame.Game", "MyGame.Windows"]);
        var window = new ProjectLibraryWindow { DataContext = library };
        window.Show();
        try
        {
            await PumpAsync();
            Assert.True(window.IsVisible);
            Assert.Same(library, window.Library);
            Assert.Equal("New code library", window.Title);
            Assert.Equal("MyLibrary", window.NameBox.Text);
            Assert.Equal("MyLibrary", window.NamespaceBox.Text);
            Assert.True(window.NamespaceBox.IsVisible);
            Assert.True(window.ReferencingProjectBox.IsVisible);
            Assert.Equal("MyGame.Game", window.ReferencingProjectBox.SelectedItem);
            Assert.False(window.WarningLabel.IsVisible);
            Assert.False(window.ErrorLabel.IsVisible);

            // A taken name warns as the user types, and OK is disabled
            library.LibraryName = "Taken";
            await PumpAsync();
            Assert.True(window.WarningLabel.IsVisible);
            Assert.False(window.OkButton.IsEnabled);

            // An invalid namespace is refused when accepted, the window stays
            library.LibraryName = "MyGame.Core";
            library.Namespace = "My Namespace";
            await PumpAsync();
            Assert.False(window.WarningLabel.IsVisible);
            Assert.True(window.OkButton.IsEnabled);
            window.Click(window.OkButton);
            await PumpAsync();
            Assert.True(window.IsVisible);
            Assert.True(window.ErrorLabel.IsVisible);
            Assert.StartsWith("Type a valid namespace name.", window.ErrorLabel.Text);

            // Valid names are accepted
            library.Namespace = "MyGame.Core";
            await PumpAsync();
            window.Click(window.OkButton);
            await WaitUntilAsync(() => !window.IsVisible, "the window to close");
        }
        finally
        {
            await CloseAsync(window);
        }
    }

    [AvaloniaFact]
    public async Task ThePluginWindowAsksNoNamespace()
    {
        var plugin = new ProjectLibraryViewModel("MyPlugin", _ => false, "New plugin", showNamespace: false, nameLabel: "Plugin name:");
        var window = new ProjectLibraryWindow { DataContext = plugin };
        window.Show();
        try
        {
            await PumpAsync();
            Assert.Equal("New plugin", window.Title);
            Assert.False(window.NamespaceBox.IsVisible);
            Assert.False(window.ReferencingProjectBox.IsVisible);
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), x => x.Text == "Plugin name:");

            window.Click(window.CancelButton);
            await WaitUntilAsync(() => !window.IsVisible, "the window to close");
        }
        finally
        {
            await CloseAsync(window);
        }
    }

    private static async Task PumpAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string what)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"Timed out waiting for {what}.");
            await Task.Delay(25);
            await PumpAsync();
        }
    }

    private static async Task CloseAsync(Window window)
    {
        window.Close();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
    }
}

/// <summary>
/// Clicks a control of a headless window as a user would.
/// </summary>
internal static class HeadlessWindowExtensions
{
    public static void Click(this Window window, Control control)
    {
        var center = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)
            ?? throw new InvalidOperationException("The control is not in the window.");
        window.MouseMove(center);
        window.MouseDown(center, MouseButton.Left);
        window.MouseUp(center, MouseButton.Left);
    }
}
