// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The Keyboard shortcuts page: each command with its shortcut, the shortcuts the user gives, removes and resets, what it
/// refuses, and the conflicts it shows on the lines of both commands.
/// </summary>
public sealed class TestKeyboardShortcuts
{
    private static readonly IViewModelServiceProvider Services = new ViewModelServiceProvider([new NullDispatcherService()]);

    private static CommandDescriptor Command(string id, string? gesture = null, string? category = null, string[]? keywords = null) => new()
    {
        Id = id,
        Title = () => id[(id.LastIndexOf('.') + 1)..],
        DefaultGesture = gesture,
        Category = category,
        Keywords = keywords ?? [],
        ExecuteAsync = _ => Task.CompletedTask,
    };

    // The commands of the editor ("Stride.") come before those of the plugins
    private static (KeyboardShortcutsViewModel Page, List<IReadOnlyDictionary<string, string>> Saved) Open(IReadOnlyDictionary<string, string>? overrides, params CommandDescriptor[] commands)
    {
        var saved = new List<IReadOnlyDictionary<string, string>>();
        var page = new KeyboardShortcutsViewModel(Services, commands, x => x.Id.StartsWith("Stride.", StringComparison.Ordinal), x => x.Title(), overrides, saved.Add);
        return (page, saved);
    }

    private static KeyboardShortcutViewModel Line(KeyboardShortcutsViewModel page, string id) => page.Find(id) ?? throw new KeyNotFoundException(id);

    [Fact]
    public void EachCommandShowsItsShortcutAndWhetherTheUserChangedIt()
    {
        var (page, _) = Open(new Dictionary<string, string> { ["Tests.Moved"] = "Ctrl+Alt+M" },
            Command("Stride.Save", "Ctrl+S", CommandCategories.File),
            Command("Tests.Moved", "Ctrl+Alt+K"),
            Command("Tests.Plain"));

        Assert.Equal(3, page.Shortcuts.Count);
        Assert.Equal("Ctrl+S", Line(page, "Stride.Save").Gesture);
        Assert.False(Line(page, "Stride.Save").IsUserDefined);
        Assert.Equal("File", Line(page, "Stride.Save").CategoryName);
        Assert.Equal("Ctrl+Alt+M", Line(page, "Tests.Moved").Gesture);
        Assert.Equal("Ctrl+Alt+K", Line(page, "Tests.Moved").DefaultGesture);
        Assert.True(Line(page, "Tests.Moved").IsUserDefined);
        Assert.Null(Line(page, "Tests.Plain").Gesture);
        Assert.True(page.HasOverrides);
        Assert.Empty(page.Conflicts);
    }

    [Fact]
    public void TheSearchLooksInTheTitlesCategoriesIdentifiersShortcutsAndKeywords()
    {
        var (page, _) = Open(null,
            Command("Stride.Save", "Ctrl+S", CommandCategories.File),
            Command("Tests.Build", "F6", CommandCategories.Project, ["compile"]));

        page.SearchText = "file";
        Assert.Equal(["Stride.Save"], page.Shortcuts.Select(x => x.Id));
        page.SearchText = "ctrl+s";
        Assert.Equal(["Stride.Save"], page.Shortcuts.Select(x => x.Id));
        page.SearchText = "compile";
        Assert.Equal(["Tests.Build"], page.Shortcuts.Select(x => x.Id));
        page.SearchText = "tests.build";
        Assert.Equal(["Tests.Build"], page.Shortcuts.Select(x => x.Id));
        page.SearchText = string.Empty;
        Assert.Equal(2, page.Shortcuts.Count);
    }

    [Fact]
    public void AShortcutNeedsAModifierUnlessItIsAFunctionKeyOrTheCommandsDefault()
    {
        var plain = Command("Tests.Plain");
        var withDefault = Command("Tests.Delete", "Delete");

        Assert.NotNull(KeyboardShortcutsViewModel.Validate(plain, "K"));
        Assert.NotNull(KeyboardShortcutsViewModel.Validate(plain, "Shift+K"));
        Assert.NotNull(KeyboardShortcutsViewModel.Validate(plain, "not a shortcut"));
        Assert.NotNull(KeyboardShortcutsViewModel.Validate(plain, null));
        Assert.Null(KeyboardShortcutsViewModel.Validate(plain, "Ctrl+K"));
        Assert.Null(KeyboardShortcutsViewModel.Validate(plain, "F8"));
        // A command can always get its own default back
        Assert.Null(KeyboardShortcutsViewModel.Validate(withDefault, "Delete"));

        var (page, saved) = Open(null, plain, withDefault);
        Assert.NotNull(page.SetGesture(Line(page, "Tests.Plain"), "K"));
        Assert.Empty(saved);
        Assert.Null(Line(page, "Tests.Plain").Gesture);
    }

    [Fact]
    public void GivingRemovingAndResettingShortcutsSavesTheOverrides()
    {
        var (page, saved) = Open(null,
            Command("Stride.Save", "Ctrl+S"),
            Command("Tests.Plain"));
        var save = Line(page, "Stride.Save");
        var plain = Line(page, "Tests.Plain");

        Assert.Null(page.SetGesture(plain, "Ctrl+Alt+P"));
        Assert.Equal("Ctrl+Alt+P", plain.Gesture);
        Assert.True(plain.IsUserDefined);
        Assert.Equal("Ctrl+Alt+P", saved[^1]["Tests.Plain"]);

        // None: no shortcut, even with a default
        page.SetNone(save);
        Assert.Null(save.Gesture);
        Assert.True(save.IsUserDefined);
        Assert.Equal("None", saved[^1]["Stride.Save"]);

        // Its own default again: no override
        Assert.Null(page.SetGesture(save, "Ctrl+S"));
        Assert.False(saved[^1].ContainsKey("Stride.Save"));
        Assert.False(save.IsUserDefined);

        // None for a command with no default: no override either
        page.SetNone(plain);
        Assert.False(saved[^1].ContainsKey("Tests.Plain"));
        Assert.Null(plain.Gesture);

        Assert.Null(page.SetGesture(plain, "Ctrl+Alt+P"));
        page.Reset(plain);
        Assert.Null(plain.Gesture);
        Assert.Empty(saved[^1]);

        Assert.Null(page.SetGesture(plain, "Ctrl+Alt+P"));
        page.SetNone(save);
        Assert.True(page.HasOverrides);
        page.ResetAll();
        Assert.False(page.HasOverrides);
        Assert.Empty(saved[^1]);
        Assert.Equal("Ctrl+S", save.Gesture);
        Assert.Null(plain.Gesture);
        // Each save is a new dictionary
        Assert.Equal(saved.Count, saved.Distinct().Count());
    }

    [Fact]
    public void AConflictShowsOnTheLinesOfBothCommands()
    {
        var (page, _) = Open(null,
            Command("Stride.Save", "Ctrl+S"),
            Command("Tests.Export", "Ctrl+S"),
            Command("Tests.Plain"));
        var save = Line(page, "Stride.Save");
        var export = Line(page, "Tests.Export");

        // The editor's default comes before a plugin's
        Assert.Equal("Ctrl+S", save.Gesture);
        Assert.Null(export.Gesture);
        Assert.True(export.HasConflict);
        Assert.Contains("Save", export.Conflict);
        Assert.True(save.HasConflict);
        Assert.Contains("Export", save.Conflict);
        Assert.Single(page.Conflicts);

        // The user's shortcut comes first
        Assert.Null(page.SetGesture(Line(page, "Tests.Plain"), "Ctrl+S"));
        Assert.Equal("Ctrl+S", Line(page, "Tests.Plain").Gesture);
        Assert.Null(save.Gesture);
        Assert.Contains("Plain", save.Conflict);
        Assert.Contains("Save", Line(page, "Tests.Plain").Conflict);

        page.ResetAll();
        Assert.Equal("Ctrl+S", save.Gesture);
        Assert.Null(Line(page, "Tests.Plain").Conflict);
    }
}
