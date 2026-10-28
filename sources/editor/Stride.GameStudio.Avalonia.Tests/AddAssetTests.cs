// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Stride.Assets.Editor.Avalonia.Templates;
using Stride.Assets.Editor.Templates;
using Stride.Assets.Materials;
using Stride.Assets.Models;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Components.AddAssets;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// Adding an asset from the asset view of the shell: the Add asset button and its flyout, the Add asset dialog (the context menus
/// and Ctrl+I) and the model import options window, with the real generators of the editor.
/// </summary>
public sealed class AddAssetTests
{
    private const string AssetLocation = "Text";

    private static readonly Guid MaterialId = new("DFAAC7DE-EF67-49C7-AC95-F5EE6BFFE633");

    private static readonly Lazy<Package> LazyTemplates = new(RegisterTemplates);

    [AvaloniaFact]
    public async Task TheAddAssetButtonDropsDownTheTemplatesAndCreatesThePickedOne()
    {
        var (fixture, window) = await OpenAsync();
        using (fixture)
        {
            try
            {
                var button = window.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "AddAssetButton");
                Assert.True(button.IsVisible);
                fixture.Session.AssetCollection.SelectedLocations.Add(fixture.Package);

                // The flyout shows the templates of the session for the Add asset command
                await window.ClickAsync(button);
                var flyout = Assert.IsType<Flyout>(button.Flyout);
                Assert.True(flyout.IsOpen);
                var view = Assert.IsType<AddItemView>(flyout.Content);
                var templates = Assert.IsType<AddAssetTemplateCollectionViewModel>(view.DataContext);
                Assert.Same(fixture.Session.AssetCollection.AddAssetTemplateCollection, templates);
                Assert.Same(fixture.Session.AssetCollection.RunAssetTemplateCommand, view.AddItemCommand);
                Assert.Same(fixture.Session.AssetCollection.SelectFilesToCreateAssetCommand, view.SelectFilesToCreateItemCommand);
                Assert.True(view.FindControl<Button>("ImportButton")!.IsVisible);
                var material = templates.RootGroup.GetTemplatesRecursively().Single(x => x.Id == MaterialId);
                Assert.Contains(templates.RootGroup.SubGroups, x => x.Name == "Material");

                // Validating a template closes the flyout and creates the asset in the selected folder, selected
                view.Validate(material);
                await WindowTestHelper.WaitUntilAsync(() => fixture.Session.AllAssets.Any(x => x.Asset is MaterialAsset), "the material");
                Assert.False(flyout.IsOpen);
                var created = fixture.Session.AllAssets.Single(x => x.Asset is MaterialAsset);
                Assert.Equal(ShellSessionFixture.Url("Material"), created.Url);
                await WindowTestHelper.WaitUntilAsync(() => fixture.Session.AssetCollection.SelectedAssets.Contains(created), "the selection");
                await WindowTestHelper.WaitUntilAsync(() => fixture.Session.ActionHistory.CanUndo, "the history");
                Assert.Equal($"Create asset '{created.Url}'", fixture.Session.ActionHistory.Transactions.Single().Name);
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task CtrlIOpensTheAddAssetDialogForTheSelectedFolder()
    {
        var (fixture, window) = await OpenAsync();
        using (fixture)
        {
            try
            {
                var dialogs = fixture.ReplaceDialogService();
                dialogs.PickAddAssetTemplate = templates => templates.RootGroup.GetTemplatesRecursively().Single(x => x.Id == MaterialId);
                var folder = fixture.Asset(AssetLocation).Directory;
                fixture.Session.AssetCollection.SelectedLocations.Add(folder);
                // The context menu of the asset list carries the shortcut; its items only join the tree once the menu opens
                var list = window.GetVisualDescendants().OfType<ListBox>().Single(x => x.Name == "AssetList");
                var menuItem = list.ContextMenu!.Items.OfType<MenuItem>().First(x => x.Header is "Add asset...");
                Assert.Equal(new KeyGesture(Key.I, KeyModifiers.Control), menuItem.InputGesture);

                window.KeyPressQwerty(PhysicalKey.I, RawInputModifiers.Control);
                window.KeyReleaseQwerty(PhysicalKey.I, RawInputModifiers.Control);
                await WindowTestHelper.WaitUntilAsync(() => fixture.Session.AllAssets.Any(x => x.Asset is MaterialAsset), "the material");

                // The dialog proposed the templates for the selected folder, and the material went there
                var dialog = Assert.Single(dialogs.AddAssetDialogs);
                Assert.Same(folder, dialog.CurrentDirectory);
                var created = fixture.Session.AllAssets.Single(x => x.Asset is MaterialAsset);
                Assert.Same(folder, created.Directory);
                Assert.Empty(dialogs.Messages);
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task TheModelImportWindowShowsTheOptionsAndPicksASkeleton()
    {
        using var fixture = await AssetPickerTests.OpenModelWithSkeletonsAsync();
        var dialogs = fixture.ReplaceDialogService();
        var skeleton = fixture.Asset("Skeleton");
        dialogs.PickAsset = picker => picker.FilteredAssets.Single(x => x.Id == skeleton.Id);
        var options = new ImportModelFromFileViewModel(fixture.Session) { ShowDeduplicateMaterialsCheckBox = true };
        var window = new ModelAssetTemplateWindow { DataContext = options };
        window.Show();
        try
        {
            await WindowTestHelper.PumpAsync();
            Assert.Equal("Import from model", window.Title);
            Assert.Same(options, window.Options);
            Assert.True(window.FindControl<CheckBox>("ImportMaterialsBox")!.IsChecked);
            Assert.True(window.FindControl<CheckBox>("DeduplicateMaterialsBox")!.IsVisible);
            Assert.False(window.FindControl<TextBlock>("FbxWarning")!.IsVisible);
            Assert.True(window.FindControl<RadioButton>("ImportSkeletonButton")!.IsChecked);
            Assert.Equal("(none)", window.FindControl<TextBlock>("SkeletonNameText")!.Text);

            // Unchecking the materials disables the deduplication
            var materials = window.FindControl<CheckBox>("ImportMaterialsBox")!;
            await window.ClickAsync(materials);
            Assert.False(options.ImportMaterials);
            Assert.False(window.FindControl<CheckBox>("DeduplicateMaterialsBox")!.IsEnabled);

            // Browsing picks a skeleton of the session and selects its option
            await window.ClickAsync(window.FindControl<Button>("BrowseSkeletonButton")!);
            await WindowTestHelper.WaitUntilAsync(() => options.SkeletonToReuse is not null, "the skeleton");
            Assert.Same(skeleton, options.SkeletonToReuse);
            Assert.True(options.ReuseSkeleton);
            Assert.False(options.ImportSkeleton);
            await WindowTestHelper.PumpAsync();
            Assert.True(window.FindControl<RadioButton>("ReuseSkeletonButton")!.IsChecked);
            Assert.Equal(skeleton.Url, window.FindControl<TextBlock>("SkeletonNameText")!.Text);
            Assert.Contains(typeof(SkeletonAsset), Assert.Single(dialogs.AssetPickers).AcceptedTypes);

            // Clearing it keeps the option, with no skeleton
            await window.ClickAsync(window.FindControl<Button>("ClearSkeletonButton")!);
            Assert.Null(options.SkeletonToReuse);
            Assert.Equal("(none)", options.SkeletonName);

            // OK closes the window
            await window.ClickAsync(window.FindControl<Button>("OkButton")!);
            await WindowTestHelper.WaitUntilAsync(() => !window.IsVisible, "the window to close");
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    private static async Task<(ShellSessionFixture Fixture, MainWindow Window)> OpenAsync()
    {
        _ = LazyTemplates.Value;
        var fixture = ShellSessionFixture.Create((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        Assert.True(await fixture.Main.OpenSession(fixture.PackagePath));
        await WindowTestHelper.PumpAsync();
        return (fixture, window);
    }

    /// <summary>
    /// A package with the material template of the Stride package, which the factory generator of the editor runs.
    /// </summary>
    private static Package RegisterTemplates()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Stride.GameStudio.Avalonia.Tests", "AssetTemplates", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var package = new Package
        {
            FullPath = new UFile(Path.Combine(directory, "TestAssetTemplates" + Package.PackageFileExtension)),
            Meta = { Name = "TestAssetTemplates", Version = new PackageVersion("1.0.0") },
        };
        package.Templates.Add(new TemplateAssetFactoryDescription
        {
            Id = MaterialId,
            Name = "Material",
            Scope = TemplateScope.Asset,
            Group = "Material",
            Order = -10,
            AssetTypeName = nameof(MaterialAsset),
            DefaultOutputName = "Material",
            Description = "An empty material",
            FullPath = new UFile(Path.Combine(directory, "DefaultMaterial" + TemplateDescription.FileExtension)),
        });
        TemplateManager.RegisterPackage(package);
        return package;
    }
}
