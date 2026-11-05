// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.CodeEditorSupport;
using Stride.Core.IO;
using Stride.Core.Packages;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Settings;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// What the session did only in the WPF editor before both editors shared it: the recent files on save, the shader editor for the
/// text assets, the Add dependency and Update package commands, Open in IDE, the delayed notifications, and the property grid
/// kept to the main asset view and the asset editors.
/// </summary>
public sealed class TestSessionCore
{
    [Fact]
    public async Task SavingPutsTheSessionAtTheTopOfTheRecentFiles()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var main = (FakeMainViewModel)fixture.Session.Main;
        Assert.Empty(main.RecentFiles);

        Assert.True(await fixture.Session.SaveSessionAsync());

        // No solution: the package file, as in WPF
        Assert.Equal([fixture.PackagePath], main.RecentFiles);
    }

    [Fact]
    public async Task TheTextAssetsOpenWithTheShaderEditor()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var main = (FakeMainViewModel)fixture.Session.Main;
        main.TextAssetTypes.Add(nameof(TestEditorAsset));
        var explorer = new FakeFileExplorerService();
        ((ViewModelServiceProvider)fixture.ServiceProvider).RegisterService(explorer);
        var setting = ExternalToolsSettings.ShaderEditor;
        var editor = Path.Combine(fixture.Directory, "shader-editor");
        File.WriteAllText(editor, string.Empty);
        try
        {
            setting.SetValue(new UFile(editor));
            var wood = fixture.Session.AllAssets.Single();
            fixture.OnUiThread(() => fixture.Assets.SelectAssets([wood]));

            // The test asset has no editor: as a text asset, it can be edited anyway
            Assert.True(fixture.Session.EditSelectedContentCommand.IsEnabled);
            fixture.Session.EditSelectedContentCommand.Execute();

            var file = Path.Combine(fixture.Directory, EditorSessionFixture.PackageName, "Assets", "Textures", "Wood.sdtesteditor");
            Assert.True(SpinWait.SpinUntil(() => explorer.Requests.Count > 0, TimeSpan.FromSeconds(10)));
            Assert.Equal([$"with {editor} {file}"], explorer.Requests);
            Assert.Empty(fixture.Session.EditorCollection.Editors);
        }
        finally
        {
            setting.SetValue(setting.DefaultValue);
        }
    }

    [Fact]
    public async Task SelectingAProjectEnablesUpdatePackageAndAddDependency()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var session = fixture.Session;
        Assert.False(session.IsUpdatePackageEnabled);

        fixture.OnUiThread(() => fixture.Assets.SelectedLocations.Add(fixture.Package));

        Assert.True(SpinWait.SpinUntil(() => session.IsUpdatePackageEnabled, TimeSpan.FromSeconds(10)));
        Assert.True(session.AddDependencyCommand.IsEnabled);
        Assert.NotNull(session.UpdatePackageViewModel);
    }

    [Fact]
    public async Task UpdatePackageNeedsASingleSelectedPackage()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var template = fixture.Session.FindTemplates(Templates.TemplateScope.Package).FirstOrDefault() ?? new Templates.TemplateDescription { Name = "Test" };

        await fixture.Session.UpdatePackageTemplateAsync(template);

        Assert.Equal(["Please select a single package."], fixture.Dialogs.Messages);
    }

    [Fact]
    public async Task AddDependencyTellsWhenNoPackageCanBeAdded()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        fixture.OnUiThread(() => fixture.Assets.SelectedLocations.Add(fixture.Package));

        fixture.Session.AddDependencyCommand.Execute();

        Assert.True(SpinWait.SpinUntil(() => fixture.Dialogs.Messages.Count > 0, TimeSpan.FromSeconds(10)));
        Assert.Equal(["There are no packages that can be added as dependencies to this package."], fixture.Dialogs.Messages);
        Assert.Empty(fixture.Dialogs.PackagePickers);
    }

    [Fact]
    public async Task AddDependencyOffersTheSuggestedPackagesButNotThePackageItself()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var session = fixture.Session;
        session.SuggestedPackages.Add(new PackageName("Some.Suggested.Package", new PackageVersion("1.0.0")));
        fixture.OnUiThread(() => fixture.Assets.SelectedLocations.Add(fixture.Package));

        session.AddDependencyCommand.Execute();

        // The picker opens (the user cancels it): it lists the suggested package, not the selected package
        Assert.True(SpinWait.SpinUntil(() => fixture.Dialogs.PackagePickers.Count > 0, TimeSpan.FromSeconds(10)));
        var filter = Assert.Single(fixture.Dialogs.PackagePickers);
        var candidates = (await session.SuggestPackagesToAddAsync()).ToList();
        Assert.Contains(candidates, x => x.Name == "Some.Suggested.Package" && filter(x));
        Assert.DoesNotContain(candidates, x => x.Name == fixture.Package.Name && filter(x));
        Assert.Empty(fixture.Dialogs.Messages);
    }

    [Fact]
    public async Task OpenInIDETellsWhenTheSessionHasNoSolution()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        Assert.True(string.IsNullOrEmpty(fixture.Session.SolutionPath));

        Assert.False(await CodeEditorOpenerService.StartOrToggle(fixture.Session, null));

        Assert.Equal(["The session currently open is not a VisualStudio session."], fixture.Dialogs.Messages);
    }

    [Fact]
    public async Task OpenInIDEBringsTheVisualStudioThatHasTheSolutionOpenToTheFront()
    {
        using var fixture = await EditorSessionFixture.OpenGameAsync(PlatformType.Windows);
        var visualStudio = new FakeVisualStudioService();
        ((ViewModelServiceProvider)fixture.ServiceProvider).RegisterService(visualStudio);

        Assert.True(await CodeEditorOpenerService.StartOrToggle(fixture.Session, new IDEInfo("Visual Studio", null, IDEType.VisualStudio)));

        Assert.Equal([(fixture.Session.SolutionPath, true)], visualStudio.Requests);
        Assert.Empty(fixture.Dialogs.Messages);
    }

    [Fact]
    public async Task ADelayedNotificationWaitsForTheMainWindowAndIsAskedOnce()
    {
        using var fixture = await EditorSessionFixture.OpenAsync();
        var dialogs = fixture.Dialogs;
        var mainWindowActive = new TaskCompletionSource();
        dialogs.MainWindowActive = mainWindowActive.Task;
        // The first button: yes
        dialogs.PickButton = buttons => buttons.First().Result;
        var ask = new SettingsKey<bool>("Test/AskBeforeSomething", new SettingsContainer(), true);
        var yes = 0;
        IEditorDialogService service = dialogs;

        var first = service.AddDelayedNotification(ask, "Do it?", "Yes", "No", () => ++yes);
        var second = service.AddDelayedNotification(ask, "Do it?", "Yes", "No", () => ++yes);
        await second;
        Assert.Empty(dialogs.Messages);

        mainWindowActive.SetResult();
        await first;

        Assert.Equal(["Do it?"], dialogs.Messages);
        Assert.Equal(1, yes);
    }

    [Fact]
    public async Task ADelayedNotificationThatShouldNotBeAskedRunsTheRememberedAnswer()
    {
        using var fixture = await EditorSessionFixture.OpenAsync();
        var container = new SettingsContainer();
        var ask = new SettingsKey<bool>("Test/AskBeforeSomething", container, false);
        var answer = new SettingsKey<bool>("Test/DoSomething", container, false);
        var (yes, no) = (0, 0);

        await ((IEditorDialogService)fixture.Dialogs).AddDelayedNotification(ask, "Do it?", "Yes", "No", () => ++yes, () => ++no, answer);

        Assert.Empty(fixture.Dialogs.Messages);
        Assert.Equal((0, 1), (yes, no));
    }

    [Fact]
    public async Task OnlyTheMainAssetViewShowsItsSelectionInThePropertyGrid()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var session = fixture.Session;
        var wood = session.AllAssets.Single();
        var picker = new AssetCollectionViewModel(session);
        fixture.OnUiThread(() => picker.UpdateAssetsCollection([wood]));

        fixture.OnUiThread(() => picker.SelectAssets([wood]));

        Assert.Same(session.AssetViewProperties, session.ActiveProperties);
        Assert.Null(picker.AssetViewProperties.ViewModel);
        Assert.Null(session.AssetViewProperties.ViewModel);

        fixture.OnUiThread(() => fixture.Assets.SelectAssets([wood]));

        Assert.True(SpinWait.SpinUntil(() => session.AssetViewProperties.ViewModel is not null, TimeSpan.FromSeconds(10)));
        Assert.Same(session.AssetViewProperties, session.ActiveProperties);
    }

    [Fact]
    public async Task AnAssetEditorShowsItsPropertiesInThePropertiesOfTheMainAssetView()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var wood = fixture.Session.AllAssets.Single();
        var editor = new TestAssetEditorViewModel(wood);

        Assert.Same(fixture.Session.AssetViewProperties, editor.EditorProperties);
        Assert.Same(fixture.ServiceProvider.Get<IUndoRedoService>(), editor.UndoRedoService);

        editor.ShowProperties();

        Assert.Equal(wood.Name, editor.EditorProperties.Name);
        Assert.True(SpinWait.SpinUntil(() => editor.EditorProperties.ViewModel is not null, TimeSpan.FromSeconds(10)));
        editor.Destroy();
    }

    private sealed class TestAssetEditorViewModel(AssetViewModel asset) : AssetEditorViewModel(asset)
    {
        public void ShowProperties() => ShowAssetProperties();
    }

    private sealed class FakeVisualStudioService : IVisualStudioService
    {
        public List<(UFile SolutionPath, bool MakeActive)> Requests { get; } = [];

        public Task<Process?> GetInstanceAsync(UFile solutionPath, bool makeActive)
        {
            Requests.Add((solutionPath, makeActive));
            // A process that runs: the instance of the solution
            return Task.FromResult<Process?>(Process.GetCurrentProcess());
        }
    }
}
