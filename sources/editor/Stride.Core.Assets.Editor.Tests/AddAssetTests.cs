// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.AddAssets;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Adding assets to a session: the Add asset list (the templates of the session), running a template in the selected folder,
/// the Add asset dialog and the files dropped on the asset view, over the test templates and their generator.
/// </summary>
public sealed class AddAssetTests
{
    private static TestAssetTemplates Generator => TestAssetTemplates.Generator;

    [Fact]
    public async Task TheAddAssetListGroupsTheAssetTemplatesOfTheSession()
    {
        using var fixture = await OpenAsync();
        var templates = fixture.Assets.AddAssetTemplateCollection;

        Assert.Equal("All templates", templates.RootGroup.Name);
        Assert.Equal([templates.RootGroup], templates.RootGroups);
        Assert.Same(templates.RootGroup, templates.SelectedGroup);
        Assert.False(templates.IsSearching);
        Assert.Null(templates.SelectedTemplate);

        // The group of the templates is created under the root
        var group = Assert.Single(templates.RootGroup.SubGroups, x => x.Name == TestTemplatePackage.AssetGroup);
        Assert.Equal([TestTemplatePackage.AssetFactoryId, TestTemplatePackage.ImportId, TestTemplatePackage.OtherImportId], group.Templates.Select(x => x.Id));
        // A template whose asset type is not loaded is left out, as one without a group (as in WPF), and the project templates
        // are not assets
        Assert.DoesNotContain(templates.RootGroup.GetTemplatesRecursively(), x => x.Id == TestTemplatePackage.UnknownAssetTypeId);
        Assert.DoesNotContain(templates.RootGroup.GetTemplatesRecursively(), x => x.Id == TestTemplatePackage.AssetId);
        Assert.DoesNotContain(templates.RootGroup.GetTemplatesRecursively(), x => x.Id == TestTemplatePackage.GameId);

        // Every template is listed at first, the selected group narrows the list
        Assert.Contains(templates.Templates, x => x.Id == TestTemplatePackage.AssetFactoryId);
        Assert.Contains(templates.Templates, x => x.Id == TestTemplatePackage.ImportId);
        templates.SelectedGroup = group;
        Assert.Equal([TestTemplatePackage.AssetFactoryId, TestTemplatePackage.ImportId, TestTemplatePackage.OtherImportId], templates.Templates.Select(x => x.Id));
        var factory = templates.Templates.First();
        Assert.Equal(TestTemplatePackage.AssetFactoryName, factory.Name);
        Assert.Equal("An asset created by the test generator", factory.Description);
        Assert.Same(TestTemplatePackage.AssetFactory, factory.GetTemplate());
    }

    [Fact]
    public async Task SearchingListsTheMatchingTemplatesOfEveryGroupByName()
    {
        using var fixture = await OpenAsync();
        var templates = fixture.Assets.AddAssetTemplateCollection;
        templates.SelectedGroup = templates.RootGroup.SubGroups.Single(x => x.Name == TestTemplatePackage.AssetGroup);

        templates.SearchToken = "IMPORTED";

        // The search is not case sensitive, it spans every group and sorts the matches by name
        Assert.True(templates.IsSearching);
        Assert.Same(templates.RootGroup, templates.SelectedGroup);
        Assert.Equal([TestTemplatePackage.ImportName, TestTemplatePackage.OtherImportName], templates.Templates.Select(x => x.Name));

        templates.SearchToken = "test ";
        var names = templates.Templates.Select(x => x.Name).ToList();
        Assert.Contains(TestTemplatePackage.AssetFactoryName, names);
        Assert.Contains(TestTemplatePackage.OtherImportName, names);
        Assert.Equal(names.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase), names);

        // A search without a match lists nothing; clearing the search lists every template again
        templates.SearchToken = "no such template";
        Assert.Empty(templates.Templates);
        templates.SearchToken = "";
        Assert.False(templates.IsSearching);
        Assert.Contains(templates.Templates, x => x.Id == TestTemplatePackage.ImportId);
        Assert.Contains(templates.Templates, x => x.Id == TestTemplatePackage.AssetFactoryId);
    }

    [Fact]
    public async Task SelectingATemplateProposesANameUniqueInTheTargetFolder()
    {
        using var fixture = await OpenAsync((TestTemplatePackage.AssetFactoryDefaultOutputName, "existing"));
        var templates = fixture.Assets.AddAssetTemplateCollection;
        var factory = templates.RootGroup.GetTemplatesRecursively().Single(x => x.Id == TestTemplatePackage.AssetFactoryId);

        // Without a folder there is nothing to propose
        templates.SelectedTemplate = factory;
        Assert.Equal(string.Empty, templates.Name);
        Assert.Null(templates.TargetDirectory);

        templates.CurrentDirectory = fixture.Package.AssetMountPoint;
        templates.SelectedTemplate = null;
        templates.SelectedTemplate = factory;

        Assert.Same(fixture.Package.AssetMountPoint, templates.TargetDirectory);
        Assert.StartsWith(TestTemplatePackage.AssetFactoryDefaultOutputName, templates.Name);
        Assert.NotEqual(TestTemplatePackage.AssetFactoryDefaultOutputName, templates.Name);
    }

    [Fact]
    public async Task TheListFollowsTheAssetTypesOfTheSession()
    {
        using var fixture = await OpenAsync();
        var templates = fixture.Assets.AddAssetTemplateCollection;
        var group = templates.RootGroup.SubGroups.Single(x => x.Name == TestTemplatePackage.AssetGroup);
        templates.SelectedGroup = group;
        templates.SelectedTemplate = templates.Templates.First();

        // A refresh (a package joining the session) rebuilds the groups and lists every template again
        templates.Refresh();

        Assert.Same(templates.RootGroup, templates.SelectedGroup);
        Assert.NotSame(group, templates.RootGroup.SubGroups.Single(x => x.Name == TestTemplatePackage.AssetGroup));
        Assert.Contains(templates.Templates, x => x.Id == TestTemplatePackage.AssetFactoryId);
        Assert.Contains(templates.Templates, x => x.Id == TestTemplatePackage.OtherImportId);
        Assert.Null(templates.SelectedTemplate);
    }

    [Fact]
    public async Task RunningATemplateCreatesTheAssetInTheSelectedFolderAndSelectsIt()
    {
        using var fixture = await OpenAsync();
        var folder = fixture.Package.GetOrCreateAssetDirectory("Generated", false);
        fixture.Assets.SelectedLocations.Add(folder);
        var template = FindTemplate(fixture, TestTemplatePackage.AssetFactoryId);

        var created = await RunAsync(fixture, template, null);

        var asset = Assert.Single(created);
        Assert.Equal(EditorSessionFixture.Url("Generated/" + TestTemplatePackage.AssetFactoryDefaultOutputName), asset.Url);
        Assert.Equal("generated", Assert.IsType<TestEditorAsset>(asset.Asset).Value);
        Assert.Same(folder, asset.Directory);
        Assert.Contains(asset, folder.Assets);
        Assert.Equal([asset], fixture.Session.AllAssets);
        Assert.Equal([asset], fixture.Assets.SelectedAssets);
        Assert.Empty(fixture.Dialogs.Messages);
        Assert.True(fixture.Session.IsDirty);

        // The generator ran in the folder, with the name of the template
        var parameters = Generator.LastParameters!;
        Assert.Same(TestTemplatePackage.AssetFactory, parameters.Description);
        Assert.Same(fixture.Package.Package, parameters.Package);
        Assert.Equal("Generated", parameters.TargetLocation.FullPath);
        Assert.Equal(TestTemplatePackage.AssetFactoryDefaultOutputName, parameters.Name);
        Assert.Empty(parameters.SourceFiles);

        // One undo entry, named after the asset
        var transaction = Assert.Single(fixture.Session.ActionHistory.Transactions);
        Assert.Equal($"Create asset '{asset.Url}'", transaction.Name);
        fixture.Session.UndoRedoService!.Undo();
        Assert.True(asset.IsDeleted);
        Assert.Empty(fixture.Session.AllAssets);
        Assert.DoesNotContain(asset, folder.Assets);
        fixture.Session.UndoRedoService.Redo();
        Assert.False(asset.IsDeleted);
        Assert.Equal([asset], fixture.Session.AllAssets);
    }

    [Fact]
    public async Task RunningATemplateNeedsASingleFolder()
    {
        using var fixture = await OpenAsync(("Textures/Wood", "wood"));
        var template = FindTemplate(fixture, TestTemplatePackage.AssetFactoryId);

        // No folder selected
        Assert.Empty(await RunAsync(fixture, template, null));
        Assert.Contains("select a directory or package", Assert.Single(fixture.Dialogs.Messages));

        // Two folders selected
        fixture.Assets.SelectedLocations.Add(fixture.Package);
        fixture.Assets.SelectedLocations.Add(fixture.Session.AllAssets.Single().Directory);
        Assert.Empty(await RunAsync(fixture, template, null));
        Assert.Equal(2, fixture.Dialogs.Messages.Count);
        Assert.Contains("multiple locations", fixture.Dialogs.Messages[1]);

        Assert.Single(fixture.Session.AllAssets);
    }

    [Fact]
    public async Task ATemplateThatIsNotAnAssetTemplateIsRefused()
    {
        using var fixture = await OpenAsync();
        fixture.Assets.SelectedLocations.Add(fixture.Package);

        // A template of the asset scope that does not describe an asset (listed by nothing, but a plugin could run it)
        var template = new TemplateDescriptionViewModel(fixture.ServiceProvider, TestTemplatePackage.Package.Templates.Single(x => x.Id == TestTemplatePackage.AssetId));
        Assert.Empty(await RunAsync(fixture, template, null));

        Assert.Contains("not an asset template", Assert.Single(fixture.Dialogs.Messages));
        Assert.Empty(fixture.Session.AllAssets);
        Assert.Empty(await RunAsync(fixture, null, null));
    }

    [Fact]
    public async Task AFailedOrCancelledGenerationCreatesNothing()
    {
        using var fixture = await OpenAsync();
        fixture.Assets.SelectedLocations.Add(fixture.Package);
        var template = FindTemplate(fixture, TestTemplatePackage.AssetFactoryId);

        // The session opening showed its own progress window
        var progressWindows = fixture.Dialogs.ProgressWindows.Count;
        Generator.FailNextRun = true;
        Assert.Empty(await RunAsync(fixture, template, null));
        // The progress window shows the error
        Assert.Equal(progressWindows + 1, fixture.Dialogs.ProgressWindows.Count);
        var progress = fixture.Dialogs.ProgressWindows[^1];
        Assert.True(progress.WorkDone);
        Assert.True(progress.Log.HasErrors);

        Generator.CancelNextRun = true;
        Assert.Empty(await RunAsync(fixture, template, null));

        Assert.Empty(fixture.Session.AllAssets);
        Assert.Empty(fixture.Dialogs.Messages);
        Assert.False(fixture.Session.IsDirty);
        Assert.False(fixture.Session.ActionHistory.CanUndo);
    }

    [Fact]
    public async Task TheAddAssetDialogRunsThePickedTemplateInTheSelectedFolder()
    {
        using var fixture = await OpenAsync();
        var folder = fixture.Package.GetOrCreateAssetDirectory("Picked", false);
        fixture.Assets.SelectedLocations.Add(folder);
        fixture.Dialogs.PickAddAssetTemplate = templates => templates.RootGroup.GetTemplatesRecursively().Single(x => x.Id == TestTemplatePackage.AssetFactoryId);

        fixture.Assets.ShowAddAssetDialogCommand.Execute();
        Assert.True(SpinWait.SpinUntil(() => fixture.Session.AllAssets.Any(), TimeSpan.FromSeconds(10)), "the asset to be created");
        WaitForHistory(fixture, 0);

        // The dialog proposes the templates for the selected folder
        var dialog = Assert.Single(fixture.Dialogs.AddAssetDialogs);
        Assert.Same(folder, dialog.CurrentDirectory);
        Assert.Contains(dialog.RootGroup.GetTemplatesRecursively(), x => x.Id == TestTemplatePackage.AssetFactoryId);
        var asset = Assert.Single(fixture.Session.AllAssets);
        Assert.Equal(EditorSessionFixture.Url("Picked/" + TestTemplatePackage.AssetFactoryDefaultOutputName), asset.Url);
        Assert.Equal([asset], fixture.Assets.SelectedAssets);

        // Cancelling the dialog creates nothing
        fixture.Dialogs.PickAddAssetTemplate = _ => null;
        fixture.Assets.ShowAddAssetDialogCommand.Execute();
        Assert.True(SpinWait.SpinUntil(() => fixture.Dialogs.AddAssetDialogs.Count == 2, TimeSpan.FromSeconds(10)), "the dialog to open");
        Assert.Single(fixture.Session.AllAssets);

        // The dialog needs a folder
        fixture.Assets.SelectedLocations.Clear();
        fixture.Assets.ShowAddAssetDialogCommand.Execute();
        Assert.True(SpinWait.SpinUntil(() => fixture.Dialogs.Messages.Count == 1, TimeSpan.FromSeconds(10)), "the message");
        Assert.Equal(2, fixture.Dialogs.AddAssetDialogs.Count);
    }

    [Fact]
    public async Task AddingFilesImportsThemWithTheTemplateOfTheirExtension()
    {
        using var fixture = await OpenAsync();
        using var files = new TemporaryFiles();
        var wood = files.Write("Wood" + TestTemplatePackage.OtherImportExtension);
        var stone = files.Write("Stone" + TestTemplatePackage.OtherImportExtension);
        fixture.Assets.SelectedLocations.Add(fixture.Package);
        // The files stay where they are (the second button of the copy prompt: No)
        fixture.Dialogs.PickButton = _ => 2;

        await AddFilesAsync(fixture, [wood, stone]);

        // One template imports the extension: no dialog, one asset per file, named after it
        Assert.Empty(fixture.Dialogs.AssetTemplatesDialogs);
        Assert.Equal(2, fixture.Dialogs.Messages.Count);
        Assert.All(fixture.Dialogs.Messages, x => Assert.Contains("do you want to copy it", x));
        Assert.Equal(["Stone", "Wood"], fixture.Session.AllAssets.Select(x => x.Name).Order());
        var woodAsset = fixture.Session.AllAssets.Single(x => x.Name == "Wood");
        Assert.Equal(wood.FullPath, Assert.IsType<TestOtherEditorAsset>(woodAsset.Asset).Value);
        Assert.Equal(2, fixture.Assets.SelectedAssets.Count);
        var parameters = Generator.LastParameters!;
        Assert.Same(TestTemplatePackage.OtherImport, parameters.Description);
        Assert.Equal([wood, stone], parameters.SourceFiles);

        // One undo entry for both
        Assert.Equal("Create 2 assets", Assert.Single(fixture.Session.ActionHistory.Transactions).Name);

        // A folder brings its files
        var subFolder = files.Write(Path.Combine("More", "Brick" + TestTemplatePackage.OtherImportExtension));
        await AddFilesAsync(fixture, [new UFile(Path.GetDirectoryName(subFolder.FullPath)!)]);
        Assert.Contains(fixture.Session.AllAssets, x => x.Name == "Brick");

        // Files no template imports are refused
        fixture.Dialogs.Messages.Clear();
        await AddFilesAsync(fixture, [files.Write("notes.txt")]);
        Assert.Contains("aren't supported", Assert.Single(fixture.Dialogs.Messages));
        Assert.Equal(3, fixture.Session.AllAssets.Count());
    }

    [Fact]
    public async Task AddingFilesCopiesThemIntoTheResourcesFolderWhenAsked()
    {
        using var fixture = await OpenAsync();
        using var files = new TemporaryFiles();
        var wood = files.Write("Wood" + TestTemplatePackage.OtherImportExtension);
        fixture.Assets.SelectedLocations.Add(fixture.Package);
        // The first button of the copy prompt (Yes), then the default location of the resource
        fixture.Dialogs.PickButton = _ => 1;
        fixture.Dialogs.Answer = MessageBoxResult.Yes;

        await AddFilesAsync(fixture, [wood]);

        Assert.Equal(2, fixture.Dialogs.Messages.Count);
        Assert.Contains("do you want to copy it", fixture.Dialogs.Messages[0]);
        Assert.Contains("default location", fixture.Dialogs.Messages[1]);
        var copy = Path.GetFullPath(Path.Combine(fixture.Directory, EditorSessionFixture.PackageName, "Resources", "Wood" + TestTemplatePackage.OtherImportExtension));
        Assert.True(File.Exists(copy), copy);
        var asset = Assert.Single(fixture.Session.AllAssets);
        Assert.Equal(new UFile(copy), Assert.IsType<TestOtherEditorAsset>(asset.Asset).Value);
        Assert.Equal([new UFile(copy)], Generator.LastParameters!.SourceFiles);
    }

    [Fact]
    public async Task AddingFilesSeveralTemplatesImportAsksWhichOne()
    {
        using var fixture = await OpenAsync();
        using var files = new TemporaryFiles();
        var wood = files.Write("Wood" + TestTemplatePackage.ImportExtension);
        fixture.Assets.SelectedLocations.Add(fixture.Package);
        fixture.Dialogs.PickButton = _ => 2;

        // Cancelled: nothing is created
        await AddFilesAsync(fixture, [wood]);
        var dialog = Assert.Single(fixture.Dialogs.AssetTemplatesDialogs);
        Assert.Equal(0, dialog.FileCount);
        Assert.Equal([TestTemplatePackage.ImportId, TestTemplatePackage.OtherImportId], dialog.Templates.Select(x => x.Template.Id));
        Assert.All(dialog.Templates, x => Assert.Equal(string.Empty, x.CountText));
        Assert.Empty(fixture.Session.AllAssets);

        // The picked template imports the file
        fixture.Dialogs.PickAssetTemplate = templates => templates.Templates.Single(x => x.Template.Id == TestTemplatePackage.OtherImportId).Template;
        await AddFilesAsync(fixture, [wood]);
        var asset = Assert.Single(fixture.Session.AllAssets);
        Assert.Equal("Wood", asset.Name);
        Assert.IsType<TestOtherEditorAsset>(asset.Asset);
        Assert.Contains("do you want to copy it", Assert.Single(fixture.Dialogs.Messages));
    }

    [Fact]
    public async Task AddingFilesOfSeveralKindsImportsTheOnesThePickedTemplateAccepts()
    {
        using var fixture = await OpenAsync();
        using var files = new TemporaryFiles();
        var wood = files.Write("Wood" + TestTemplatePackage.ImportExtension);
        var stone = files.Write("Stone" + TestTemplatePackage.OtherImportExtension);
        fixture.Assets.SelectedLocations.Add(fixture.Package);
        fixture.Dialogs.PickButton = _ => 2;
        fixture.Dialogs.PickAssetTemplate = templates => templates.Templates.Single(x => x.Template.Id == TestTemplatePackage.ImportId).Template;

        await AddFilesAsync(fixture, [wood, stone]);

        // The dialog says how many files each template accepts
        var dialog = Assert.Single(fixture.Dialogs.AssetTemplatesDialogs);
        Assert.Equal(2, dialog.FileCount);
        Assert.Equal([TestTemplatePackage.ImportId, TestTemplatePackage.OtherImportId], dialog.Templates.Select(x => x.Template.Id));
        Assert.Equal([1, 2], dialog.Templates.Select(x => x.FileCount));
        Assert.Equal("1 out of 2 file(s)", dialog.Templates[0].CountText);
        // Only the file the picked template accepts is imported
        var asset = Assert.Single(fixture.Session.AllAssets);
        Assert.Equal("Wood", asset.Name);
        Assert.IsType<TestEditorAsset>(asset.Asset);
        Assert.Equal([wood], Generator.LastParameters!.SourceFiles);
    }

    [Fact]
    public void TheTemplatesOfASetOfFilesAreListedByOrder()
    {
        _ = Generator;
        var services = new ViewModelServiceProvider([new NullDispatcherService()]);
        var templates = new AssetTemplatesViewModel(services, 3,
        [
            new KeyValuePair<TemplateAssetDescription, int>(TestTemplatePackage.OtherImport, 3),
            new KeyValuePair<TemplateAssetDescription, int>(TestTemplatePackage.Import, 1),
        ]);
        try
        {
            Assert.Equal(3, templates.FileCount);
            Assert.Equal([TestTemplatePackage.ImportId, TestTemplatePackage.OtherImportId], templates.Templates.Select(x => x.Template.Id));
            Assert.Equal(["1 out of 3 file(s)", "3 out of 3 file(s)"], templates.Templates.Select(x => x.CountText));
            Assert.Equal(TestTemplatePackage.ImportName, templates.Templates[0].ToString());
            Assert.Null(templates.SelectedTemplate);
        }
        finally
        {
            templates.Destroy();
        }
    }

    private static Task<EditorSessionFixture> OpenAsync(params (string Location, string Value)[] assets)
    {
        // The generator and the importers are registered once the first test asks for them
        _ = Generator;
        return EditorSessionFixture.OpenAsync(assets);
    }

    /// <summary>
    /// Runs a template and waits for the action history to record the creation: it does so once the asset notifications are
    /// done, on another thread, which the test must not race with a change of the selection (or the end of the session).
    /// </summary>
    private static async Task<List<AssetViewModel>> RunAsync(EditorSessionFixture fixture, ITemplateDescriptionViewModel? template, IList<UFile>? files = null)
    {
        var transactions = fixture.Session.ActionHistory.Transactions.Count;
        var assets = await fixture.Assets.RunAssetTemplateAsync(template, files);
        if (assets.Count > 0)
            WaitForHistory(fixture, transactions);
        return assets;
    }

    private static async Task AddFilesAsync(EditorSessionFixture fixture, IReadOnlyCollection<UFile> files)
    {
        var assets = fixture.Session.AllAssets.Count();
        var transactions = fixture.Session.ActionHistory.Transactions.Count;
        await fixture.Assets.AddFilesAsync(files);
        if (fixture.Session.AllAssets.Count() > assets)
            WaitForHistory(fixture, transactions);
    }

    private static void WaitForHistory(EditorSessionFixture fixture, int transactions)
    {
        Assert.True(SpinWait.SpinUntil(() => fixture.Session.ActionHistory.Transactions.Count > transactions, TimeSpan.FromSeconds(10)), "the creation to be recorded");
    }

    private static ITemplateDescriptionViewModel FindTemplate(EditorSessionFixture fixture, Guid id)
    {
        return fixture.Assets.AddAssetTemplateCollection.RootGroup.GetTemplatesRecursively().Single(x => x.Id == id);
    }

    /// <summary>
    /// A temporary folder of source files.
    /// </summary>
    private sealed class TemporaryFiles : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "Stride.Core.Assets.Editor.Tests", "Files", Guid.NewGuid().ToString("N"));

        public UFile Write(string relativePath)
        {
            var path = Path.Combine(directory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, relativePath);
            return new UFile(path);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch (IOException)
            {
                // Best effort
            }
        }
    }
}
