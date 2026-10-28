// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.IO;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.ViewModels;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The project selection window (startup, File > New) and the new project window (solution explorer) over a package of test
/// templates.
/// </summary>
public sealed class ProjectTemplateWindowTests
{
    [AvaloniaFact]
    public async Task TheProjectSelectionWindowListsTheRecentProjectsAndTheTemplates()
    {
        var dialogs = new RecordingDialogService();
        var recent = new UFile(Path.Combine(Path.GetTempPath(), "RecentGames", "Adventure", "Adventure.slnx"));
        var templates = new NewOrOpenSessionTemplateCollectionViewModel(CreateServices(dialogs), [recent], _ => { });
        var window = new ProjectSelectionWindow { DataContext = templates };
        window.Show();
        try
        {
            await WindowTestHelper.PumpAsync();
            Assert.True(window.IsVisible);
            Assert.Same(templates, window.Templates);
            Assert.Equal("Project selection", window.Title);

            // The recent projects come first, the new project properties are disabled for them
            Assert.Same(templates.RecentGroup, templates.SelectedGroup);
            Assert.Equal(2, window.FindControl<TemplateBrowserView>("Browser")!.FindControl<TreeView>("Groups")!.ItemCount);
            Assert.Equal(1, window.FindControl<TemplateBrowserView>("Browser")!.FindControl<ListBox>("Templates")!.ItemCount);
            Assert.False(window.FindControl<Button>("SelectButton")!.IsEnabled);
            Assert.False(window.FindControl<Grid>("Properties")!.IsEnabled);

            // A template: its name is proposed and can be edited
            templates.SelectedGroup = templates.NewProjectGroup;
            await WindowTestHelper.PumpAsync();
            Assert.Contains(templates.Templates, x => x.Id == TestTemplatePackage.GameId);
            Assert.Equal(templates.Templates.Count, window.FindControl<TemplateBrowserView>("Browser")!.FindControl<ListBox>("Templates")!.ItemCount);
            templates.SelectedTemplate = templates.Templates.Single(x => x.Id == TestTemplatePackage.GameId);
            await WindowTestHelper.PumpAsync();
            Assert.True(window.FindControl<Button>("SelectButton")!.IsEnabled);
            Assert.True(window.FindControl<Grid>("Properties")!.IsEnabled);
            Assert.Equal(templates.Name, window.FindControl<TextBox>("NameBox")!.Text);
            Assert.StartsWith(TestTemplatePackage.GameDefaultOutputName, window.FindControl<TextBox>("NameBox")!.Text);

            // An invalid name is refused with a message, the window stays
            templates.Name = "Bad/Name";
            await window.ClickAsync(window.FindControl<Button>("SelectButton")!);
            await WindowTestHelper.PumpAsync();
            Assert.Equal(["Invalid name."], dialogs.Messages);
            Assert.True(window.IsVisible);
            Assert.Null(templates.GetExistingSessionPath());
            Assert.NotNull(templates.GetNewSessionParameters());
        }
        finally
        {
            templates.Destroy();
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task SelectingARecentProjectClosesTheProjectSelectionWindow()
    {
        var recent = new UFile(Path.Combine(Path.GetTempPath(), "RecentGames", "Adventure", "Adventure.slnx"));
        var templates = new NewOrOpenSessionTemplateCollectionViewModel(CreateServices(new RecordingDialogService()), [recent], _ => { });
        var window = new ProjectSelectionWindow { DataContext = templates };
        window.Show();
        try
        {
            await WindowTestHelper.PumpAsync();
            var project = Assert.IsType<ExistingProjectViewModel>(Assert.Single(templates.Templates));
            templates.SelectedTemplate = project;
            await WindowTestHelper.PumpAsync();
            Assert.True(window.FindControl<Button>("SelectButton")!.IsEnabled);

            await window.ClickAsync(window.FindControl<Button>("SelectButton")!);
            await WindowTestHelper.WaitUntilAsync(() => !window.IsVisible, "the window to close");

            // The host opens the project the window selected
            Assert.Equal(recent, templates.GetExistingSessionPath());
            Assert.Null(templates.GetNewSessionParameters());
        }
        finally
        {
            templates.Destroy();
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task CancellingTheProjectSelectionWindowClosesIt()
    {
        var templates = new NewOrOpenSessionTemplateCollectionViewModel(CreateServices(new RecordingDialogService()), [], _ => { });
        var window = new ProjectSelectionWindow { DataContext = templates };
        window.Show();
        try
        {
            await WindowTestHelper.PumpAsync();
            await window.ClickAsync(window.FindControl<Button>("CancelButton")!);
            await WindowTestHelper.WaitUntilAsync(() => !window.IsVisible, "the window to close");
        }
        finally
        {
            templates.Destroy();
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheNewProjectWindowListsTheProjectTemplatesOfTheSession()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new Stride.Assets.Sprite.SpriteSheetAsset()));
        var dialogs = fixture.ReplaceDialogService();
        _ = TestTemplatePackage.Package;
        var templates = new NewProjectTemplateCollectionViewModel(fixture.ServiceProvider, fixture.Session);
        var window = new NewProjectWindow { DataContext = templates };
        window.Show();
        try
        {
            await WindowTestHelper.PumpAsync();
            Assert.True(window.IsVisible);
            Assert.Same(templates, window.Templates);
            Assert.Equal("New project", window.Title);

            // The templates that add a project to the session, listed at once
            Assert.Equal(1, window.FindControl<TemplateBrowserView>("Browser")!.FindControl<TreeView>("Groups")!.ItemCount);
            Assert.Contains(templates.Templates, x => x.Id == TestTemplatePackage.LibraryId);
            Assert.Contains(templates.Templates, x => x.Id == TestTemplatePackage.PluginId);
            Assert.DoesNotContain(templates.Templates, x => x.Id == TestTemplatePackage.GameId);
            Assert.Equal(templates.Templates.Count, window.FindControl<TemplateBrowserView>("Browser")!.FindControl<ListBox>("Templates")!.ItemCount);
            Assert.False(window.FindControl<Button>("SelectButton")!.IsEnabled);

            templates.SelectedTemplate = templates.Templates.Single(x => x.Id == TestTemplatePackage.LibraryId);
            await WindowTestHelper.PumpAsync();
            Assert.True(window.FindControl<Button>("SelectButton")!.IsEnabled);
            Assert.Equal("MyTestLibrary", window.FindControl<TextBox>("NameBox")!.Text);

            // The template is accepted: the host runs it with the parameters of the window
            await window.ClickAsync(window.FindControl<Button>("SelectButton")!);
            await WindowTestHelper.WaitUntilAsync(() => !window.IsVisible, "the window to close");
            Assert.Empty(dialogs.Messages);
            var parameters = templates.GetNewPackageParameters();
            Assert.NotNull(parameters);
            Assert.Same(TestTemplatePackage.Library, parameters.TemplateDescription);
            Assert.Equal("MyTestLibrary", parameters.OutputName);
        }
        finally
        {
            templates.Destroy();
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public void TheMainViewModelOffersANewSessionAndTheProjectSelection()
    {
        var main = new MainViewModel(App.InitializeServiceProvider());

        Assert.NotNull(main.NewSessionCommand);
        Assert.NotNull(main.SelectProjectCommand);
        Assert.NotNull(main.SelectInitialProjectCommand);
        // All need the main window the project selection is a dialog of
        Assert.Equal(main.NewSessionCommand.IsEnabled, main.SelectProjectCommand.IsEnabled);
        Assert.Equal(main.NewSessionCommand.IsEnabled, main.SelectInitialProjectCommand.IsEnabled);
        Assert.Null(main.Session);
    }

    private static IViewModelServiceProvider CreateServices(RecordingDialogService dialogs)
    {
        // The package is registered once the first test asks for it
        _ = TestTemplatePackage.Package;
        return new ViewModelServiceProvider([DispatcherService.Create(), dialogs]);
    }
}
