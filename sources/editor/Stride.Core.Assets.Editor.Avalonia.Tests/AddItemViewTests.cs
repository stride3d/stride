// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Components.AddAssets;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Templates;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The Add asset list (the flyout of the button and the dialog) and the dialog picking the template of a set of files, over the
/// descriptions of the test templates.
/// </summary>
public sealed class AddItemViewTests
{
    [AvaloniaFact]
    public async Task TheAddItemWindowListsTheGroupsAndTheTemplatesOfTheSelectedGroup()
    {
        var (services, collection) = CreateCollection();
        var window = new AddItemWindow { DataContext = collection };
        window.Show();
        try
        {
            await PumpAsync();
            Assert.True(window.IsVisible);
            Assert.Equal("Add asset...", window.Title);
            Assert.Same(collection, window.Templates);
            var view = window.FindControl<AddItemView>("Items")!;
            var groups = view.FindControl<ListBox>("Groups")!;
            var templates = view.FindControl<ListBox>("Templates")!;
            var search = view.FindControl<TextBox>("SearchBox")!;
            Assert.Same(collection, view.Collection);

            // Every template at first, with the groups on the side; the search box has the focus
            Assert.Equal(2, groups.ItemCount);
            Assert.Equal(3, templates.ItemCount);
            Assert.Null(groups.SelectedItem);
            Assert.True(search.IsFocused);
            // The import button has no command here
            Assert.False(view.FindControl<Button>("ImportButton")!.IsVisible);

            // A group narrows the list
            var scripts = collection.RootGroup.SubGroups.Single(x => x.Name == "Scripts");
            groups.SelectedItem = scripts;
            await PumpAsync();
            Assert.Same(scripts, collection.SelectedGroup);
            Assert.Equal(1, templates.ItemCount);
            Assert.Equal(TestTemplatePackage.OtherImportName, collection.Templates.Single().Name);

            // A search lists the matches of every group, the groups cannot be picked meanwhile
            search.Text = "imported";
            await PumpAsync();
            Assert.True(collection.IsSearching);
            Assert.Same(collection.RootGroup, collection.SelectedGroup);
            Assert.Null(groups.SelectedItem);
            Assert.False(groups.IsEnabled);
            Assert.Equal([TestTemplatePackage.ImportName, TestTemplatePackage.OtherImportName], collection.Templates.Select(x => x.Name));
            Assert.Equal(2, templates.ItemCount);

            search.Text = "";
            await PumpAsync();
            Assert.True(groups.IsEnabled);
            Assert.Equal(3, templates.ItemCount);
        }
        finally
        {
            collection.Destroy();
            await CloseAsync(window);
        }
    }

    [AvaloniaFact]
    public async Task ClickingATemplateClosesTheWindowWithIt()
    {
        var (_, collection) = CreateCollection();
        var window = new AddItemWindow { DataContext = collection };
        window.Show();
        try
        {
            await PumpAsync();
            var templates = window.FindControl<AddItemView>("Items")!.FindControl<ListBox>("Templates")!;
            var item = Assert.IsType<ListBoxItem>(templates.ContainerFromIndex(1));

            Click(window, item);
            await WaitUntilAsync(() => !window.IsVisible, "the window to close");

            Assert.Same(collection.Templates.ElementAt(1), window.SelectedTemplate);
            Assert.Equal(TestTemplatePackage.ImportId, window.SelectedTemplate!.Id);
        }
        finally
        {
            collection.Destroy();
            await CloseAsync(window);
        }
    }

    [AvaloniaFact]
    public async Task EnterInTheSearchBoxValidatesTheFirstMatchAndEscapeCancels()
    {
        var (_, collection) = CreateCollection();
        var window = new AddItemWindow { DataContext = collection };
        window.Show();
        try
        {
            await PumpAsync();
            var view = window.FindControl<AddItemView>("Items")!;
            var search = view.FindControl<TextBox>("SearchBox")!;
            search.Focus();
            window.KeyTextInput("factory");
            await PumpAsync();
            Assert.Equal("factory", collection.SearchToken);
            Assert.Single(collection.Templates);

            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            await WaitUntilAsync(() => !window.IsVisible, "the window to close");
            Assert.Equal(TestTemplatePackage.AssetFactoryId, window.SelectedTemplate!.Id);
        }
        finally
        {
            collection.Destroy();
            await CloseAsync(window);
        }

        var (_, other) = CreateCollection();
        window = new AddItemWindow { DataContext = other };
        window.Show();
        try
        {
            await PumpAsync();
            window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
            await WaitUntilAsync(() => !window.IsVisible, "the window to close");
            Assert.Null(window.SelectedTemplate);
        }
        finally
        {
            other.Destroy();
            await CloseAsync(window);
        }
    }

    [AvaloniaFact]
    public async Task TheViewRunsItsCommandsForTheButtonAndTheValidatedTemplate()
    {
        var (services, collection) = CreateCollection();
        ITemplateDescriptionViewModel? added = null;
        var imported = false;
        var view = new AddItemView
        {
            DataContext = collection,
            AddItemCommand = new AnonymousCommand<ITemplateDescriptionViewModel>(services, x => added = x),
        };
        var validated = new List<ITemplateDescriptionViewModel>();
        view.ItemValidated += (_, e) => validated.Add(e.Template);
        var window = new Window { Content = view };
        window.Show();
        try
        {
            await PumpAsync();
            var import = view.FindControl<Button>("ImportButton")!;
            Assert.False(import.IsVisible);

            // The import button shows with its command
            view.SelectFilesToCreateItemCommand = new AnonymousCommand(services, () => imported = true);
            await PumpAsync();
            Assert.True(import.IsVisible);
            Click(window, import);
            await PumpAsync();
            Assert.True(imported);

            // Enter on the selected template validates it: the event, then the command
            var templates = view.FindControl<ListBox>("Templates")!;
            collection.SelectedTemplate = collection.Templates.ElementAt(2);
            await PumpAsync();
            Assert.True(templates.ContainerFromIndex(2)!.Focus());
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            await PumpAsync();
            Assert.Equal([collection.Templates.ElementAt(2)], validated);
            Assert.Same(collection.Templates.ElementAt(2), added);

            // Reset lists everything again and clears the selection
            collection.SearchToken = "factory";
            await PumpAsync();
            view.Reset();
            await PumpAsync();
            Assert.Equal(string.Empty, collection.SearchToken);
            Assert.Null(collection.SelectedTemplate);
            Assert.Same(collection.RootGroup, collection.SelectedGroup);
            Assert.Equal(3, templates.ItemCount);
        }
        finally
        {
            collection.Destroy();
            await CloseAsync(window);
        }
    }

    [AvaloniaFact]
    public async Task TheItemTemplatesWindowListsTheTemplatesWithTheirCountsAndClosesWithThePickedOne()
    {
        var services = CreateServices();
        var templates = new AssetTemplatesViewModel(services, 3,
        [
            new KeyValuePair<TemplateAssetDescription, int>(TestTemplatePackage.OtherImport, 3),
            new KeyValuePair<TemplateAssetDescription, int>(TestTemplatePackage.Import, 1),
        ]);
        var window = new ItemTemplatesWindow { DataContext = templates };
        window.Show();
        try
        {
            await PumpAsync();
            Assert.Equal("Add asset...", window.Title);
            Assert.Same(templates, window.Templates);
            var list = window.FindControl<ListBox>("TemplateList")!;
            // By order, the first one selected
            Assert.Equal(2, list.ItemCount);
            Assert.Same(templates.Templates[0], templates.SelectedTemplate);
            Assert.Equal(TestTemplatePackage.ImportId, templates.Templates[0].Template.Id);
            Assert.Equal("1 out of 3 file(s)", templates.Templates[0].CountText);

            // A click on the second one picks it
            Click(window, Assert.IsType<ListBoxItem>(list.ContainerFromIndex(1)));
            await WaitUntilAsync(() => !window.IsVisible, "the window to close");
            Assert.Equal(TestTemplatePackage.OtherImportId, window.SelectedTemplate!.Id);
        }
        finally
        {
            templates.Destroy();
            await CloseAsync(window);
        }

        // Enter picks the selected one, Escape picks nothing
        var other = new AssetTemplatesViewModel(services, [TestTemplatePackage.Import, TestTemplatePackage.OtherImport]);
        window = new ItemTemplatesWindow { DataContext = other };
        window.Show();
        try
        {
            await PumpAsync();
            Assert.All(other.Templates, x => Assert.Equal(string.Empty, x.CountText));
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            await WaitUntilAsync(() => !window.IsVisible, "the window to close");
            Assert.Equal(TestTemplatePackage.ImportId, window.SelectedTemplate!.Id);
        }
        finally
        {
            other.Destroy();
            await CloseAsync(window);
        }

        var cancelled = new AssetTemplatesViewModel(services, [TestTemplatePackage.Import]);
        window = new ItemTemplatesWindow { DataContext = cancelled };
        window.Show();
        try
        {
            await PumpAsync();
            window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
            await WaitUntilAsync(() => !window.IsVisible, "the window to close");
            Assert.Null(window.SelectedTemplate);
        }
        finally
        {
            cancelled.Destroy();
            await CloseAsync(window);
        }
    }

    private static IViewModelServiceProvider CreateServices()
    {
        // The package is registered once the first test asks for it
        _ = TestTemplatePackage.Package;
        return new ViewModelServiceProvider([DispatcherService.Create()]);
    }

    /// <summary>
    /// An add item list with two groups: "Assets" (the factory and the import templates) and "Scripts" (the other import).
    /// </summary>
    private static (IViewModelServiceProvider Services, AddItemTemplateCollectionViewModel Collection) CreateCollection()
    {
        var services = CreateServices();
        var collection = new AddItemTemplateCollectionViewModel(services);
        var assets = new TemplateDescriptionGroupViewModel(collection.RootGroup, "Assets");
        assets.Templates.Add(new TemplateDescriptionViewModel(services, TestTemplatePackage.AssetFactory));
        assets.Templates.Add(new TemplateDescriptionViewModel(services, TestTemplatePackage.Import));
        var scripts = new TemplateDescriptionGroupViewModel(collection.RootGroup, "Scripts");
        scripts.Templates.Add(new TemplateDescriptionViewModel(services, TestTemplatePackage.OtherImport));
        // The list follows the selected group
        collection.SelectedGroup = null;
        collection.SelectedGroup = collection.RootGroup;
        return (services, collection);
    }

    private static void Click(Window window, Control control)
    {
        var center = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)
            ?? throw new InvalidOperationException("The control is not in the window.");
        window.MouseMove(center);
        window.MouseDown(center, MouseButton.Left);
        window.MouseUp(center, MouseButton.Left);
    }

    private static async Task PumpAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string what, int timeoutMilliseconds = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
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
