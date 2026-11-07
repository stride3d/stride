// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.IO;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The Explore section of the asset context menu, and Show in Explorer in the solution explorer: what the session asks the file
/// manager to show or open.
/// </summary>
public sealed class TestExploreCommands
{
    [Fact]
    public async Task ShowInExplorerSelectsTheFileOfTheLastSelectedAsset()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"), ("Textures/Stone", "stone"));
        var explorer = Register(fixture);
        var session = fixture.Session;
        var wood = session.AllAssets.Single(x => x.Name == "Wood");
        var stone = session.AllAssets.Single(x => x.Name == "Stone");

        fixture.Assets.SelectAssets([stone, wood]);

        Assert.True(session.ExploreCommand.IsEnabled);
        session.ExploreCommand.Execute(fixture.Assets.SelectedContent);

        Assert.Equal([$"show {AssetFile(fixture, "Textures", "Wood")}"], explorer.Requests);
        Assert.Empty(fixture.Dialogs.Messages);
    }

    [Fact]
    public async Task ShowInExplorerOpensTheFolderOfADirectory()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var explorer = Register(fixture);
        var textures = fixture.Session.AllAssets.Single().Directory;

        fixture.Session.ExploreCommand.Execute(new object[] { textures });

        Assert.Equal([$"folder {Path.Combine(fixture.Directory, EditorSessionFixture.PackageName, "Assets", "Textures")}"], explorer.Requests.Select(TrimEnd));
    }

    [Fact]
    public async Task ShowInExplorerSelectsTheProjectOfAPackage()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var explorer = Register(fixture);

        fixture.Assets.SelectedLocations.Clear();
        fixture.Assets.SelectedLocations.Add(fixture.Package);

        // Without a parameter, the selected location
        Assert.True(fixture.Session.ExploreCommand.IsEnabled);
        fixture.Session.ExploreCommand.Execute(null);

        // The package is the one of a project: its .csproj, as in WPF
        Assert.IsType<ProjectViewModel>(fixture.Package);
        Assert.Equal([$"show {Path.Combine(fixture.Directory, EditorSessionFixture.PackageName, EditorSessionFixture.PackageName + ".csproj")}"], explorer.Requests);
    }

    [Fact]
    public async Task ShowInExplorerAsksToSaveAnAssetThatHasNoFile()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var explorer = Register(fixture);
        var wood = fixture.Session.AllAssets.Single();
        File.Delete(AssetFile(fixture, "Textures", "Wood"));

        fixture.Session.ExploreCommand.Execute(new object[] { wood });

        Assert.Empty(explorer.Requests);
        Assert.Equal(["You need to save the asset before you can explore it."], fixture.Dialogs.Messages);
    }

    [Fact]
    public async Task AFileManagerThatCantStartIsReported()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var explorer = Register(fixture);
        explorer.Fail = true;

        fixture.Session.ExploreCommand.Execute(new object[] { fixture.Session.AllAssets.Single() });

        Assert.Equal(["There was a problem starting the file explorer."], fixture.Dialogs.Messages);
    }

    [Fact]
    public async Task TheAssetCommandsNeedASingleAsset()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"), ("Textures/Stone", "stone"));
        var session = fixture.Session;
        var assets = session.AllAssets.ToList();

        fixture.Assets.SelectAssets(assets);

        Assert.False(session.OpenAssetFileCommand.IsEnabled);
        Assert.False(session.OpenSourceFileCommand.IsEnabled);
        Assert.False(session.OpenWithTextEditorCommand.IsEnabled);
        Assert.True(session.ExploreCommand.IsEnabled);

        fixture.Assets.SelectAssets([assets[0]]);

        Assert.True(session.OpenAssetFileCommand.IsEnabled);
        Assert.True(session.OpenSourceFileCommand.IsEnabled);
        Assert.True(session.OpenWithTextEditorCommand.IsEnabled);
        // The test assets have no source file
        Assert.False(session.ExploreSourceFileCommand.IsEnabled);
    }

    [Fact]
    public async Task OpenAssetFileEditsTheFile()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var explorer = Register(fixture);

        fixture.Session.OpenAssetFileCommand.Execute(fixture.Session.AllAssets.Single());

        Assert.Equal([$"edit {AssetFile(fixture, "Textures", "Wood")}"], explorer.Requests);
    }

    [Fact]
    public async Task AnAssetWithoutSourceFileHasNoSourceToOpenOrShow()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var explorer = Register(fixture);
        var wood = fixture.Session.AllAssets.Single();

        fixture.Session.OpenSourceFileCommand.Execute(wood);
        fixture.Session.ExploreSourceFileCommand.Execute(wood);

        Assert.Empty(explorer.Requests);
        Assert.Equal(["This asset doesn't have a source file to open.", "This asset doesn't have a source file."], fixture.Dialogs.Messages);
    }

    [Fact]
    public async Task OpenWithTextEditorUsesTheEditorOfTheSettings()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var explorer = Register(fixture);
        var wood = fixture.Session.AllAssets.Single();
        var setting = ExternalToolsSettings.DefaultTextEditor;
        var editor = Path.Combine(fixture.Directory, "editor");
        File.WriteAllText(editor, string.Empty);
        try
        {
            setting.SetValue(new UFile(editor));

            fixture.Session.OpenWithTextEditorCommand.Execute(wood);

            Assert.Equal([$"with {editor} {AssetFile(fixture, "Textures", "Wood")}"], explorer.Requests);
        }
        finally
        {
            setting.SetValue(setting.DefaultValue);
        }
    }

    [SkippableFact]
    public async Task OpenWithTextEditorFallsBackToTheEditorOfTheSystemOutsideWindows()
    {
        Skip.If(OperatingSystem.IsWindows(), "Windows has the Notepad of the default setting.");
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var explorer = Register(fixture);

        // The default setting is Windows' Notepad
        fixture.Session.OpenWithTextEditorCommand.Execute(fixture.Session.AllAssets.Single());

        Assert.Equal([$"edit {AssetFile(fixture, "Textures", "Wood")}"], explorer.Requests);
    }

    private static FakeFileExplorerService Register(EditorSessionFixture fixture)
    {
        var explorer = new FakeFileExplorerService();
        ((ViewModelServiceProvider)fixture.ServiceProvider).RegisterService(explorer);
        Assert.Same(explorer, fixture.ServiceProvider.TryGet<IFileExplorerService>());
        return explorer;
    }

    private static string AssetFile(EditorSessionFixture fixture, string folder, string name)
        => Path.Combine(fixture.Directory, EditorSessionFixture.PackageName, "Assets", folder, name + ".sdtesteditor");

    private static string TrimEnd(string request) => request.TrimEnd('/', '\\');
}
