// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The template lists of the project selection window (a new session) and of the new project window (a project added to the
/// session), over a package of test templates.
/// </summary>
public sealed class TemplateDescriptionTests
{
    [Fact]
    public void TheProjectSelectionGroupsTheSessionTemplates()
    {
        var templates = new NewOrOpenSessionTemplateCollectionViewModel(CreateServices(), [], _ => { });
        try
        {
            Assert.Equal([templates.RecentGroup, templates.NewProjectGroup], templates.RootGroups);
            Assert.Equal("Recent projects", templates.RecentGroup.Name);
            Assert.Equal("New project", templates.NewProjectGroup.Name);
            // Without recent projects the new project templates are listed at once
            Assert.Same(templates.NewProjectGroup, templates.SelectedGroup);
            Assert.Null(templates.SelectedTemplate);
            Assert.False(templates.HasSelectedTemplate);
            Assert.False(templates.IsNewProjectSelected);

            // The group of the template is created under the root, "General" holds the templates without a group
            var general = Assert.Single(templates.NewProjectGroup.SubGroups, x => x.Name == "General");
            var games = Assert.Single(templates.NewProjectGroup.SubGroups, x => x.Name == "Test games");
            var starters = Assert.Single(games.SubGroups);
            Assert.Equal("Starters", starters.Name);
            Assert.Equal("Test games/Starters/", starters.Path);
            var game = Assert.Single(starters.Templates);
            Assert.Equal(TestTemplatePackage.GameName, game.Name);
            Assert.Equal(TestTemplatePackage.GameId, game.Id);
            Assert.Equal("A game for the tests", game.Description);
            Assert.Same(TestTemplatePackage.Game, game.GetTemplate());
            // A plugin can start a session, a code library only adds to one; the asset templates are not projects
            Assert.Contains(general.Templates, x => x.Id == TestTemplatePackage.PluginId);
            Assert.DoesNotContain(templates.NewProjectGroup.GetTemplatesRecursively(), x => x.Id == TestTemplatePackage.LibraryId);
            Assert.DoesNotContain(templates.NewProjectGroup.GetTemplatesRecursively(), x => x.Id == TestTemplatePackage.AssetId);

            // The list shows the templates of the selected group and of its sub-groups
            Assert.Contains(templates.Templates, x => x.Id == TestTemplatePackage.GameId);
            Assert.Contains(templates.Templates, x => x.Id == TestTemplatePackage.PluginId);
            templates.SelectedGroup = games;
            Assert.Equal([game], templates.Templates);
            templates.SelectedGroup = general;
            Assert.DoesNotContain(templates.Templates, x => x.Id == TestTemplatePackage.GameId);
        }
        finally
        {
            templates.Destroy();
        }
    }

    [Fact]
    public void SelectingATemplateProposesAUniqueNameUntilTheUserTypesOne()
    {
        using var location = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(location.Path, TestTemplatePackage.GameDefaultOutputName));
        var templates = new NewOrOpenSessionTemplateCollectionViewModel(CreateServices(), [], _ => { });
        try
        {
            templates.Location = new UDirectory(location.Path);
            var game = templates.NewProjectGroup.GetTemplatesRecursively().Single(x => x.Id == TestTemplatePackage.GameId);
            var plugin = templates.NewProjectGroup.GetTemplatesRecursively().Single(x => x.Id == TestTemplatePackage.PluginId);

            templates.SelectedTemplate = game;
            Assert.True(templates.HasSelectedTemplate);
            Assert.True(templates.IsNewProjectSelected);
            // A folder of the default name exists at the location: the proposed name avoids it
            Assert.StartsWith(TestTemplatePackage.GameDefaultOutputName, templates.Name);
            Assert.NotEqual(TestTemplatePackage.GameDefaultOutputName, templates.Name);
            Assert.False(Directory.Exists(Path.Combine(location.Path, templates.Name)));

            templates.SelectedTemplate = plugin;
            Assert.Equal("MyTestPlugin", templates.Name);

            // A name typed by the user is kept when the selection changes
            templates.Name = "Adventure";
            templates.SelectedTemplate = game;
            Assert.Equal("Adventure", templates.Name);

            var parameters = templates.GetNewSessionParameters();
            Assert.NotNull(parameters);
            Assert.Same(TestTemplatePackage.Game, parameters.TemplateDescription);
            Assert.Equal("Adventure", parameters.OutputName);
            Assert.Equal(new UDirectory(location.Path), parameters.OutputDirectory);
            Assert.Equal("", parameters.SolutionName);
            Assert.Null(templates.GetExistingSessionPath());

            templates.SelectedTemplate = null;
            Assert.False(templates.HasSelectedTemplate);
            Assert.Null(templates.GetNewSessionParameters());
        }
        finally
        {
            templates.Destroy();
        }
    }

    [Fact]
    public void ThePropertiesOfANewProjectAreValidated()
    {
        using var location = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(location.Path, "Taken"));
        var templates = new NewOrOpenSessionTemplateCollectionViewModel(CreateServices(), [], _ => { });
        try
        {
            templates.Location = new UDirectory(location.Path);
            templates.SelectedTemplate = templates.NewProjectGroup.GetTemplatesRecursively().Single(x => x.Id == TestTemplatePackage.GameId);

            templates.Name = "Bad/Name";
            Assert.False(templates.ValidateProperties(out var error));
            Assert.Equal("Invalid name.", error);
            Assert.False(templates.ArePropertiesValid);

            templates.Name = "Taken";
            Assert.False(templates.ValidateProperties(out error));
            Assert.StartsWith("Cannot use the selected name", error);

            templates.Name = "Fine";
            templates.SolutionName = "Bad/Solution";
            Assert.False(templates.ValidateProperties(out error));
            Assert.Equal("Invalid solution name.", error);

            templates.SolutionName = "FineSolution";
            templates.SolutionLocation = new UDirectory(Path.Combine(location.Path, "Solutions"));
            Assert.True(templates.ValidateProperties(out error));
            Assert.Equal("", error);
            Assert.True(templates.ArePropertiesValid);
            var parameters = templates.GetNewSessionParameters();
            Assert.NotNull(parameters);
            Assert.Equal("FineSolution", parameters.SolutionName);
            Assert.Equal(new UDirectory(Path.Combine(location.Path, "Solutions")), parameters.SolutionLocation);
        }
        finally
        {
            templates.Destroy();
        }
    }

    [Fact]
    public void RecentProjectsAreListedFirstAndOpenedOrRemoved()
    {
        var first = new UFile(Path.Combine(Path.GetTempPath(), "RecentGames", "Adventure", "Adventure.slnx"));
        var second = new UFile(Path.Combine(Path.GetTempPath(), "RecentGames", "Puzzle", "Puzzle.sdpkg"));
        var removed = new List<UFile>();
        var templates = new NewOrOpenSessionTemplateCollectionViewModel(CreateServices(), [first, second], removed.Add);
        try
        {
            // With recent projects, they are listed first
            Assert.Same(templates.RecentGroup, templates.SelectedGroup);
            Assert.Equal(["Adventure", "Puzzle"], templates.Templates.Select(x => x.Name));
            var adventure = Assert.IsType<ExistingProjectViewModel>(templates.Templates.ElementAt(0));
            Assert.Equal(first, adventure.Path);
            Assert.Equal(first.ToOSPath(), adventure.Description);
            Assert.Null(adventure.GetTemplate());
            Assert.Null(adventure.Icon);

            // Opening a recent project selects it and asks the host to open it
            var openRequested = 0;
            templates.OpenRequested += (_, _) => openRequested++;
            adventure.OpenCommand.Execute();
            Assert.Equal(1, openRequested);
            Assert.Same(adventure, templates.SelectedTemplate);
            Assert.True(templates.HasSelectedTemplate);
            Assert.False(templates.IsNewProjectSelected);
            Assert.Equal(first, templates.GetExistingSessionPath());
            Assert.Null(templates.GetNewSessionParameters());
            // A recent project has no properties to check
            Assert.True(templates.ValidateProperties(out _));

            // Removing a recent project forgets it
            var puzzle = Assert.IsType<ExistingProjectViewModel>(templates.Templates.ElementAt(1));
            puzzle.RemoveCommand.Execute();
            Assert.Equal([second], removed);
            Assert.Equal([adventure], templates.Templates);
            Assert.Equal([adventure], templates.RecentGroup.Templates);
        }
        finally
        {
            templates.Destroy();
        }
    }

    [Fact]
    public async Task TheNewProjectWindowListsTheTemplatesThatAddAProjectToTheSession()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var templates = new NewProjectTemplateCollectionViewModel(fixture.ServiceProvider, fixture.Session);
        try
        {
            Assert.Same(fixture.Session, templates.Session);
            var root = Assert.Single(templates.RootGroups);
            Assert.Equal("All templates", root.Name);
            Assert.Same(root, templates.SelectedGroup);

            // The code library and the plugin templates, not the game (a session of its own) nor the asset template
            Assert.Contains(templates.Templates, x => x.Id == TestTemplatePackage.LibraryId);
            Assert.Contains(templates.Templates, x => x.Id == TestTemplatePackage.PluginId);
            Assert.DoesNotContain(templates.Templates, x => x.Id == TestTemplatePackage.GameId);
            Assert.DoesNotContain(templates.Templates, x => x.Id == TestTemplatePackage.AssetId);
            Assert.Null(templates.GetNewPackageParameters());

            var library = templates.Templates.Single(x => x.Id == TestTemplatePackage.LibraryId);
            templates.SelectedTemplate = library;
            Assert.Equal("MyTestLibrary", templates.Name);
            Assert.True(templates.ValidateProperties(out var error));
            Assert.Equal("", error);
            var parameters = templates.GetNewPackageParameters();
            Assert.NotNull(parameters);
            Assert.Same(TestTemplatePackage.Library, parameters.TemplateDescription);
            Assert.Equal("MyTestLibrary", parameters.OutputName);
            Assert.Equal(templates.Location, parameters.OutputDirectory);
        }
        finally
        {
            templates.Destroy();
        }
    }

    private static IViewModelServiceProvider CreateServices()
    {
        // The package is registered once the first test asks for it
        _ = TestTemplatePackage.Package;
        return new ViewModelServiceProvider([new NullDispatcherService(), new FakeEditorDialogService()]);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Stride.Core.Assets.Editor.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, true);
            }
            catch (IOException)
            {
                // Best effort
            }
        }
    }
}
