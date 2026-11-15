// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The command palette: its search over the titles, categories and keywords of the commands, its order (the recently used
/// commands first, then the commands that can run), and the command it runs.
/// </summary>
public sealed class TestCommandPalette
{
    private static readonly IViewModelServiceProvider Services = new ViewModelServiceProvider([new NullDispatcherService()]);

    private static CommandPaletteItem Item(string id, string title, string? category = null, string[]? keywords = null, bool isEnabled = true, Action? run = null)
    {
        var descriptor = new CommandDescriptor
        {
            Id = id,
            Title = () => title,
            Category = category,
            Keywords = keywords ?? [],
            ExecuteAsync = _ => Task.CompletedTask,
        };
        return new CommandPaletteItem(descriptor, title, null, isEnabled, null, () =>
        {
            run?.Invoke();
            return Task.CompletedTask;
        });
    }

    private static IReadOnlyList<string> Ids(CommandPaletteViewModel palette) => [.. palette.Results.Select(x => x.Id)];

    [Fact]
    public void TheSearchLooksInTheTitlesTheCategoriesAndTheKeywordsForEveryWord()
    {
        var save = Item("Tests.Save", "Save all", CommandCategories.File, ["write", "disk"]);
        var build = Item("Tests.Build", "Build", CommandCategories.Project, ["compile"]);
        var log = Item("Tests.Log", "Asset log", CommandCategories.View);
        var palette = new CommandPaletteViewModel(Services, [save, build, log]);

        Assert.Equal(3, palette.Results.Count);

        palette.SearchText = "save";
        Assert.Equal(["Tests.Save"], Ids(palette));
        // A category, in any case
        palette.SearchText = "PROJECT";
        Assert.Equal(["Tests.Build"], Ids(palette));
        // A keyword
        palette.SearchText = "compile";
        Assert.Equal(["Tests.Build"], Ids(palette));
        // Every word, anywhere
        palette.SearchText = "file disk";
        Assert.Equal(["Tests.Save"], Ids(palette));
        palette.SearchText = "file compile";
        Assert.Empty(palette.Results);
        Assert.False(palette.HasResults);
        Assert.Null(palette.SelectedItem);

        palette.SearchText = "  ";
        Assert.Equal(3, palette.Results.Count);
        Assert.True(palette.HasResults);
    }

    [Fact]
    public void TheRecentCommandsComeFirstThenTheCommandsThatCanRunThenTheBestTitles()
    {
        var a = Item("Tests.A", "Open scene");
        var b = Item("Tests.B", "Close scene");
        var c = Item("Tests.C", "Scene settings", isEnabled: false);
        var d = Item("Tests.D", "Reload scene");
        var palette = new CommandPaletteViewModel(Services, [a, b, c, d], ["Tests.D", "Tests.Unknown", "Tests.B"]);

        Assert.True(d.IsRecent);
        Assert.True(b.IsRecent);
        Assert.False(a.IsRecent);
        // The recent ones in their order, then the enabled ones, then by title
        Assert.Equal(["Tests.D", "Tests.B", "Tests.A", "Tests.C"], Ids(palette));
        Assert.Same(d, palette.SelectedItem);

        // Then the titles that start with the search, before those with a word that does, before the others
        var e = Item("Tests.E", "Scenery");
        var f = Item("Tests.F", "Unseen", keywords: ["scene"]);
        palette = new CommandPaletteViewModel(Services, [f, a, e]);
        palette.SearchText = "scene";
        Assert.Equal(["Tests.E", "Tests.A", "Tests.F"], Ids(palette));
    }

    [Fact]
    public void TheSelectionMovesWithinTheResults()
    {
        var palette = new CommandPaletteViewModel(Services, [Item("Tests.A", "A"), Item("Tests.B", "B"), Item("Tests.C", "C")]);

        Assert.Equal("Tests.A", palette.SelectedItem?.Id);
        palette.MoveSelection(1);
        Assert.Equal("Tests.B", palette.SelectedItem?.Id);
        palette.MoveSelection(10);
        Assert.Equal("Tests.C", palette.SelectedItem?.Id);
        palette.MoveSelection(-10);
        Assert.Equal("Tests.A", palette.SelectedItem?.Id);

        palette.SelectedItem = null;
        palette.MoveSelection(-1);
        Assert.Equal("Tests.C", palette.SelectedItem?.Id);
    }

    [Fact]
    public async Task RunningACommandPutsItFirstInTheRecentCommandsAndADisabledOneDoesNotRun()
    {
        var runs = new List<string>();
        var a = Item("Tests.A", "A", run: () => runs.Add("A"));
        var b = Item("Tests.B", "B", run: () => runs.Add("B"));
        var off = Item("Tests.Off", "Off", isEnabled: false, run: () => runs.Add("Off"));
        var recent = Enumerable.Range(0, CommandPaletteViewModel.MaxRecentCommands).Select(x => $"Tests.Old{x}").ToList();
        var palette = new CommandPaletteViewModel(Services, [a, b, off], recent);
        IReadOnlyList<string>? saved = null;
        palette.RecentCommandsChanged += (_, ids) => saved = ids;

        palette.SelectedItem = b;
        Assert.True(await palette.RunAsync());
        Assert.Equal(["B"], runs);
        Assert.NotNull(saved);
        Assert.Equal("Tests.B", saved[0]);
        // The oldest one is forgotten
        Assert.Equal(CommandPaletteViewModel.MaxRecentCommands, saved.Count);
        Assert.DoesNotContain($"Tests.Old{CommandPaletteViewModel.MaxRecentCommands - 1}", saved);

        Assert.True(await palette.RunAsync(a));
        Assert.Equal(["Tests.A", "Tests.B"], palette.RecentIds.Take(2));
        Assert.True(await palette.RunAsync(b));
        Assert.Equal(["Tests.B", "Tests.A"], palette.RecentIds.Take(2));
        Assert.Equal(CommandPaletteViewModel.MaxRecentCommands, palette.RecentIds.Count);

        saved = null;
        Assert.False(await palette.RunAsync(off));
        Assert.Null(saved);
        Assert.Equal(["B", "A", "B"], runs);
    }

    [Fact]
    public void AnItemShowsItsCategoryBeforeItsTitle()
    {
        Assert.Equal("Project: Build", Item("Tests.Build", "Build", CommandCategories.Project).ToString());
        Assert.Equal("Build", Item("Tests.Build", "Build").ToString());
        Assert.Equal("Tests", CommandCategories.GetDisplayName("Tests"));
    }
}
