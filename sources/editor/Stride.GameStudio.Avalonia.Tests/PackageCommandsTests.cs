// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Stride.Assets.Templates;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core;
using Stride.Core.CodeEditorSupport;
using Stride.Core.Packages;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The package commands of the WPF editor: the package picker of Add dependency, and the Update package, Add dependency and
/// Open in IDE items of the Project menu, the toolbar and the solution explorer.
/// </summary>
public sealed class PackageCommandsTests
{
    [AvaloniaFact]
    public async Task ThePackagePickerFiltersAndReturnsThePick()
    {
        using var fixture = await ShellSessionFixture.OpenAsync();
        var owner = new MainWindow { DataContext = fixture.Main };
        owner.Show();
        try
        {
            var session = fixture.Session;
            session.SuggestedPackages.Add(new PackageName("Some.Physics", new PackageVersion("1.0.0")));
            session.SuggestedPackages.Add(new PackageName("Some.Particles", new PackageVersion("1.0.0")));
            session.SuggestedPackages.Add(new PackageName("Hidden.Package", new PackageVersion("1.0.0")));
            var candidates = await session.SuggestPackagesToAddAsync();
            // The caller's filter hides a package and the session's own one
            var picker = new PackagePickerViewModel(fixture.ServiceProvider, candidates, x => x.Name.StartsWith("Some.", StringComparison.Ordinal));
            var window = new PackagePickerWindow { DataContext = picker };
            var result = window.ShowDialog<bool>(owner);
            await WindowTestHelper.PumpAsync();

            var list = window.GetVisualDescendants().OfType<ListBox>().Single(x => x.Name == "PackageList");
            var ok = window.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "OkButton");
            Assert.Equal(["Some.Physics", "Some.Particles"], picker.Packages.Select(x => x.Name));
            Assert.False(ok.IsEnabled);

            // The search narrows the list, and drops a selection it hides
            list.SelectedItem = picker.Packages[0];
            await WindowTestHelper.PumpAsync();
            Assert.True(ok.IsEnabled);
            picker.SearchText = "parti";
            await WindowTestHelper.PumpAsync();
            Assert.Equal(["Some.Particles"], picker.Packages.Select(x => x.Name));
            Assert.Null(picker.SelectedPackage);
            Assert.False(ok.IsEnabled);

            list.SelectedItem = picker.Packages[0];
            await WindowTestHelper.PumpAsync();
            ok.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.True(await result);
            Assert.Equal(["Some.Particles"], picker.SelectedPackages.Select(x => x.Name));
        }
        finally
        {
            await owner.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheProjectMenuHasThePackageCommands()
    {
        using var fixture = ShellSessionFixture.Create();
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            Assert.True(await fixture.Main.OpenSession(fixture.PackagePath));
            await WindowTestHelper.PumpAsync();
            var session = fixture.Session;
            var locations = session.AssetCollection.SelectedLocations;
            var projectMenu = MenuItem(window, "ProjectMenu");
            var updatePackage = MenuItem(window, "UpdatePackageMenuItem");
            var addDependency = MenuItem(window, "AddDependencyMenuItem");
            var openInIDE = MenuItem(window, "OpenInIDEMenuItem");

            // The project: Update package lists the Update platforms template, and Add dependency adds to it
            locations.Clear();
            locations.Add(fixture.Package);
            await WindowTestHelper.PumpAsync();
            projectMenu.Open();
            await WindowTestHelper.PumpAsync();
            Assert.True(updatePackage.IsVisible);
            var template = updatePackage.Items.OfType<PackageTemplateViewModel>().Single(x => x.GetTemplate().Id == UpdatePlatformsGenerator.TemplateId);
            Assert.True(template.UpdatePackageCommand.IsEnabled);
            Assert.True(addDependency.IsEffectivelyEnabled);
            Assert.True(openInIDE.IsEffectivelyEnabled);
            projectMenu.Close();
            await WindowTestHelper.PumpAsync();

            // The dependencies of the project: Add dependency, and no Update package
            locations.Clear();
            locations.Add(fixture.Package.Dependencies);
            await WindowTestHelper.PumpAsync();
            projectMenu.Open();
            await WindowTestHelper.PumpAsync();
            Assert.False(updatePackage.IsVisible);
            Assert.True(addDependency.IsEffectivelyEnabled);
            projectMenu.Close();
            await WindowTestHelper.PumpAsync();

            // The toolbar: Open in IDE, and the IDEs to open the solution with, the default one first
            var button = window.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "OpenInIDEButton");
            Assert.True(button.IsEffectivelyEnabled);
            var dropDown = window.GetVisualDescendants().OfType<DropDownButton>().Single(x => x.Name == "OpenWithIDEButton");
            var items = Assert.IsType<MenuFlyout>(dropDown.Flyout).Items;
            var first = Assert.IsType<MenuItem>(items[0]);
            Assert.Equal(IDEInfo.DefaultIDE.DisplayName, first.Header);
            Assert.Same(session.OpenInIDECommand, first.Command);
            Assert.Same(IDEInfo.DefaultIDE, first.CommandParameter);
            Assert.Equal(fixture.Main.AvailableIDEs.Count, items.Count);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    private static MenuItem MenuItem(Window window, string name) => window.GetLogicalDescendants().OfType<MenuItem>().Single(x => x.Name == name);
}
