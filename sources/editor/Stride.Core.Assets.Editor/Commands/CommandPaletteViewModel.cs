// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Commands;

/// <summary>
/// A command that the command palette lists: its title in the palette's context, its category, its shortcut, and whether it
/// can run now.
/// </summary>
[Experimental("STREXP003")]
public sealed class CommandPaletteItem
{
    private readonly Func<Task> run;

    /// <param name="descriptor">The command.</param>
    /// <param name="title">Its title in the context of the palette.</param>
    /// <param name="gesture">The text of its shortcut, or null when it has none.</param>
    /// <param name="isEnabled">Whether it can run now.</param>
    /// <param name="isChecked">Whether it is on, for a toggle; null otherwise.</param>
    /// <param name="run">Runs it (through the editor's guard, in the palette's context).</param>
    public CommandPaletteItem(CommandDescriptor descriptor, string title, string? gesture, bool isEnabled, bool? isChecked, Func<Task> run)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        Title = title ?? descriptor.Id;
        Gesture = gesture;
        IsEnabled = isEnabled;
        IsChecked = isChecked;
        this.run = run ?? throw new ArgumentNullException(nameof(run));
    }

    public CommandDescriptor Descriptor { get; }

    public string Id => Descriptor.Id;

    public string Title { get; }

    /// <summary>
    /// The category of the command, or null.
    /// </summary>
    public string? Category => Descriptor.Category;

    /// <summary>
    /// The category as the editor shows it (<see cref="CommandCategories.GetDisplayName"/>), or null.
    /// </summary>
    public string? CategoryName => Category is null ? null : CommandCategories.GetDisplayName(Category);

    /// <summary>
    /// The text of the command's shortcut, or null.
    /// </summary>
    public string? Gesture { get; }

    public bool IsEnabled { get; }

    /// <summary>
    /// Whether a toggle is on; null for a command that is not a toggle.
    /// </summary>
    public bool? IsChecked { get; }

    /// <summary>
    /// Whether the command is one of the recently used ones (listed first).
    /// </summary>
    public bool IsRecent { get; internal set; }

    internal Task RunAsync() => run();

    /// <inheritdoc />
    public override string ToString() => CategoryName is null ? Title : $"{CategoryName}: {Title}";
}

/// <summary>
/// The command palette: a search over the commands of the editor (their titles, categories and keywords), the recently used
/// commands first, and the command to run. The shell shows it and gives it the commands with their state.
/// </summary>
[Experimental("STREXP003")]
public sealed class CommandPaletteViewModel : ViewModelBase
{
    /// <summary>
    /// How many recently used commands the palette remembers.
    /// </summary>
    public const int MaxRecentCommands = 10;

    private readonly IReadOnlyList<CommandPaletteItem> items;
    private readonly List<string> recentIds;
    private string searchText = string.Empty;
    private CommandPaletteItem? selectedItem;

    /// <param name="serviceProvider">The services of the editor.</param>
    /// <param name="items">The commands the palette lists.</param>
    /// <param name="recentIds">The identifiers of the recently used commands, the most recent first.</param>
    public CommandPaletteViewModel(IViewModelServiceProvider serviceProvider, IEnumerable<CommandPaletteItem> items, IEnumerable<string>? recentIds = null)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(items);
        this.items = items.ToList();
        this.recentIds = (recentIds ?? []).Distinct().Take(MaxRecentCommands).ToList();
        foreach (var item in this.items)
            item.IsRecent = this.recentIds.Contains(item.Id);
        Refresh();
    }

    /// <summary>
    /// The text searched: every word must be in the title, the category or a keyword of a command.
    /// </summary>
    public string SearchText
    {
        get => searchText;
        set
        {
            if (SetValue(ref searchText, value ?? string.Empty))
                Refresh();
        }
    }

    /// <summary>
    /// The commands that match the search, the recently used ones first.
    /// </summary>
    public ObservableCollection<CommandPaletteItem> Results { get; } = [];

    /// <summary>
    /// Whether some commands match the search.
    /// </summary>
    public bool HasResults => Results.Count > 0;

    /// <summary>
    /// The command that Enter runs.
    /// </summary>
    public CommandPaletteItem? SelectedItem
    {
        get => selectedItem;
        set => SetValue(ref selectedItem, value);
    }

    /// <summary>
    /// The identifiers of the recently used commands, the most recent first.
    /// </summary>
    public IReadOnlyList<string> RecentIds => recentIds;

    /// <summary>
    /// Raised when a command ran from the palette, with the new list of recently used commands (to keep it for the next time).
    /// </summary>
    public event EventHandler<IReadOnlyList<string>>? RecentCommandsChanged;

    /// <summary>
    /// Moves the selection by <paramref name="delta"/> results, within the list.
    /// </summary>
    public void MoveSelection(int delta)
    {
        if (Results.Count == 0)
            return;
        var index = SelectedItem is null ? (delta > 0 ? -1 : Results.Count) : Results.IndexOf(SelectedItem);
        SelectedItem = Results[Math.Clamp(index + delta, 0, Results.Count - 1)];
    }

    /// <summary>
    /// Runs a command (the selected one by default) if it can run, and puts it first in the recently used commands.
    /// </summary>
    /// <returns>True if the command ran.</returns>
    public async Task<bool> RunAsync(CommandPaletteItem? item = null)
    {
        item ??= SelectedItem;
        if (item is not { IsEnabled: true })
            return false;
        recentIds.Remove(item.Id);
        recentIds.Insert(0, item.Id);
        if (recentIds.Count > MaxRecentCommands)
            recentIds.RemoveRange(MaxRecentCommands, recentIds.Count - MaxRecentCommands);
        RecentCommandsChanged?.Invoke(this, recentIds.ToList());
        await item.RunAsync();
        return true;
    }

    /// <summary>
    /// Whether a command matches a search: each word of it is in its title, its category or one of its keywords, in any case.
    /// </summary>
    public static bool Matches(CommandPaletteItem item, string search)
    {
        ArgumentNullException.ThrowIfNull(item);
        var words = SplitWords(search);
        return words.Length == 0 || words.All(word => Texts(item).Any(text => text.Contains(word, StringComparison.CurrentCultureIgnoreCase)));
    }

    private void Refresh()
    {
        var words = SplitWords(searchText);
        var results = items
            .Where(x => words.Length == 0 || Matches(x, searchText))
            .OrderBy(x => x.IsRecent ? recentIds.IndexOf(x.Id) : int.MaxValue)
            .ThenByDescending(x => x.IsEnabled)
            .ThenBy(x => Rank(x, words))
            .ThenBy(x => x.CategoryName ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(x => x.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        Results.Clear();
        foreach (var item in results)
            Results.Add(item);
        SelectedItem = Results.FirstOrDefault();
        OnPropertyChanged(nameof(HasResults));
    }

    // Lower is better: the title starts with the search, a word of the title starts with it, anything else
    private static int Rank(CommandPaletteItem item, string[] words)
    {
        if (words.Length == 0)
            return 0;
        var first = words[0];
        if (item.Title.StartsWith(first, StringComparison.CurrentCultureIgnoreCase))
            return 0;
        if (SplitWords(item.Title).Any(x => x.StartsWith(first, StringComparison.CurrentCultureIgnoreCase)))
            return 1;
        return item.Title.Contains(first, StringComparison.CurrentCultureIgnoreCase) ? 2 : 3;
    }

    private static IEnumerable<string> Texts(CommandPaletteItem item)
    {
        yield return item.Title;
        if (item.Category is { } category)
        {
            yield return category;
            yield return CommandCategories.GetDisplayName(category);
        }
        foreach (var keyword in item.Descriptor.Keywords)
            yield return keyword;
    }

    private static string[] SplitWords(string? text) => (text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
