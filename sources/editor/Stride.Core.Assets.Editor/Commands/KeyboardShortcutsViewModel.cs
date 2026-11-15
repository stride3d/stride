// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Commands;

/// <summary>
/// A line of the Keyboard shortcuts page: a command, its shortcut now, its default, whether the user changed it, and the
/// conflict that keeps it from its shortcut (or that its shortcut causes), if any.
/// </summary>
[Experimental("STREXP003")]
public sealed class KeyboardShortcutViewModel : ViewModelBase
{
    private string? gesture;
    private bool isUserDefined;
    private string? conflict;

    internal KeyboardShortcutViewModel(IViewModelServiceProvider serviceProvider, CommandDescriptor descriptor, string title)
        : base(serviceProvider)
    {
        Descriptor = descriptor;
        Title = title;
        DefaultGesture = CommandGesture.TryParse(descriptor.DefaultGesture, out var defaultGesture) ? defaultGesture.ToString() : descriptor.DefaultGesture;
    }

    public CommandDescriptor Descriptor { get; }

    public string Id => Descriptor.Id;

    public string Title { get; }

    /// <summary>
    /// The category as the editor shows it, empty for none.
    /// </summary>
    public string CategoryName => CommandCategories.GetDisplayName(Descriptor.Category);

    /// <summary>
    /// The shortcut that runs the command now, or null when it has none.
    /// </summary>
    public string? Gesture
    {
        get => gesture;
        private set => SetValue(ref gesture, value);
    }

    /// <summary>
    /// The command's default shortcut, or null.
    /// </summary>
    public string? DefaultGesture { get; }

    /// <summary>
    /// Whether the user gave the command a shortcut or removed its own.
    /// </summary>
    public bool IsUserDefined
    {
        get => isUserDefined;
        private set => SetValue(ref isUserDefined, value);
    }

    /// <summary>
    /// The conflict on this line: why the command does not get its shortcut, or which command its shortcut keeps from its own;
    /// null when there is none.
    /// </summary>
    public string? Conflict
    {
        get => conflict;
        private set => SetValue(ref conflict, value, nameof(Conflict), nameof(HasConflict));
    }

    public bool HasConflict => Conflict is not null;

    internal void Update(string? newGesture, bool userDefined, string? newConflict)
    {
        Gesture = newGesture;
        IsUserDefined = userDefined;
        Conflict = newConflict;
    }
}

/// <summary>
/// The Keyboard shortcuts page of the settings: the commands with their shortcuts, a search, the user's changes (a shortcut, or
/// none), Reset for a command and for all, and the conflicts on their lines. Each change is written at once to the user's
/// shortcuts through the given callback (the settings key of the shell), so that the settings dialog saves or discards it with
/// the other settings.
/// </summary>
[Experimental("STREXP003")]
public sealed class KeyboardShortcutsViewModel : ViewModelBase
{
    private readonly IReadOnlyList<CommandDescriptor> commands;
    private readonly Func<CommandDescriptor, bool> isBuiltIn;
    private readonly Action<IReadOnlyDictionary<string, string>> save;
    private readonly List<KeyboardShortcutViewModel> lines;
    private Dictionary<string, string> overrides;
    private string searchText = string.Empty;
    private KeyboardShortcutViewModel? selectedShortcut;

    /// <param name="serviceProvider">The services of the editor.</param>
    /// <param name="commands">The commands, in registration order.</param>
    /// <param name="isBuiltIn">Whether a command is one of the editor's own (see <see cref="EditorPluginHost.IsBuiltInCommand"/>).</param>
    /// <param name="title">The title of a command.</param>
    /// <param name="overrides">The user's shortcuts now, by command identifier.</param>
    /// <param name="save">Writes the user's shortcuts after a change.</param>
    public KeyboardShortcutsViewModel(IViewModelServiceProvider serviceProvider, IEnumerable<CommandDescriptor> commands, Func<CommandDescriptor, bool> isBuiltIn, Func<CommandDescriptor, string> title, IReadOnlyDictionary<string, string>? overrides, Action<IReadOnlyDictionary<string, string>> save)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(title);
        this.commands = commands.ToList();
        this.isBuiltIn = isBuiltIn ?? throw new ArgumentNullException(nameof(isBuiltIn));
        this.save = save ?? throw new ArgumentNullException(nameof(save));
        this.overrides = overrides is null ? [] : new Dictionary<string, string>(overrides);
        lines = this.commands
            .Select(x => new KeyboardShortcutViewModel(serviceProvider, x, title(x)))
            .OrderBy(x => x.CategoryName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(x => x.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        UpdateLines();
        Refresh();
    }

    /// <summary>
    /// The page of the commands of a plugin host: those it can show, with their titles.
    /// </summary>
    public KeyboardShortcutsViewModel(IViewModelServiceProvider serviceProvider, EditorPluginHost host, IReadOnlyDictionary<string, string>? overrides, Action<IReadOnlyDictionary<string, string>> save)
        : this(serviceProvider, (host ?? throw new ArgumentNullException(nameof(host))).Commands.Items.Where(host.IsCommandAvailable), host.IsBuiltInCommand, x => host.GetCommandTitle(x, null), overrides, save)
    {
    }

    /// <summary>
    /// The text searched: every word must be in the title, the category, the identifier, the shortcut or a keyword of a command.
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
    /// The lines that match the search, by category and title.
    /// </summary>
    public ObservableCollection<KeyboardShortcutViewModel> Shortcuts { get; } = [];

    /// <summary>
    /// Every line, whatever the search.
    /// </summary>
    public IReadOnlyList<KeyboardShortcutViewModel> AllShortcuts => lines;

    /// <summary>
    /// The line the recorder, None and Reset act on.
    /// </summary>
    public KeyboardShortcutViewModel? SelectedShortcut
    {
        get => selectedShortcut;
        set => SetValue(ref selectedShortcut, value);
    }

    /// <summary>
    /// The user's shortcuts now, by command identifier.
    /// </summary>
    public IReadOnlyDictionary<string, string> Overrides => overrides;

    /// <summary>
    /// The shortcuts that are not bound with the user's shortcuts now.
    /// </summary>
    public IReadOnlyList<CommandGestureConflict> Conflicts { get; private set; } = [];

    /// <summary>
    /// Whether the user changed some shortcuts (Reset all then has something to do).
    /// </summary>
    public bool HasOverrides => overrides.Count > 0;

    /// <summary>
    /// The line of a command, or null.
    /// </summary>
    public KeyboardShortcutViewModel? Find(string id) => lines.FirstOrDefault(x => x.Id == id);

    /// <summary>
    /// Why a shortcut can't be given to a command, or null when it can: it must be a shortcut, with Ctrl, Alt or Meta unless its
    /// key is a function key (a command can always get its own default back).
    /// </summary>
    public static string? Validate(CommandDescriptor command, string? gesture)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!CommandGesture.TryParse(gesture, out var parsed))
            return Tr._p("Shortcuts", "This is not a shortcut: press modifiers then one key, such as Ctrl+Shift+K.");
        if (!CommandGestureBindings.IsAllowed(parsed) && !(CommandGesture.TryParse(command.DefaultGesture, out var defaultGesture) && defaultGesture == parsed))
            return Tr._p("Shortcuts", "A shortcut needs Ctrl, Alt or Meta, unless its key is a function key.");
        return null;
    }

    /// <summary>
    /// Gives a command a shortcut, unless <see cref="Validate"/> refuses it. Another command that has the same shortcut keeps
    /// it if it comes first (the user's shortcuts in registration order): the conflict shows on both lines.
    /// </summary>
    /// <returns>Null when the shortcut was given; else why not.</returns>
    public string? SetGesture(KeyboardShortcutViewModel line, string gesture)
    {
        ArgumentNullException.ThrowIfNull(line);
        if (Validate(line.Descriptor, gesture) is { } error)
            return error;
        var parsed = CommandGesture.Parse(gesture);
        // The command's own default: no override needed
        if (CommandGesture.TryParse(line.Descriptor.DefaultGesture, out var defaultGesture) && defaultGesture == parsed)
            overrides.Remove(line.Id);
        else
            overrides[line.Id] = parsed.ToString();
        Apply();
        return null;
    }

    /// <summary>
    /// Takes the shortcut from a command: it has none, even when it has a default.
    /// </summary>
    public void SetNone(KeyboardShortcutViewModel line)
    {
        ArgumentNullException.ThrowIfNull(line);
        if (line.DefaultGesture is null)
            overrides.Remove(line.Id);
        else
            overrides[line.Id] = CommandGestureBindings.NoGesture;
        Apply();
    }

    /// <summary>
    /// Gives a command its default shortcut back.
    /// </summary>
    public void Reset(KeyboardShortcutViewModel line)
    {
        ArgumentNullException.ThrowIfNull(line);
        if (overrides.Remove(line.Id))
            Apply();
    }

    /// <summary>
    /// Gives every command its default shortcut back.
    /// </summary>
    public void ResetAll()
    {
        if (overrides.Count == 0)
            return;
        overrides.Clear();
        Apply();
    }

    private void Apply()
    {
        // A new dictionary each time: the settings can then undo a change
        save(new Dictionary<string, string>(overrides));
        UpdateLines();
        Refresh();
        OnPropertyChanged(nameof(Overrides), nameof(HasOverrides));
    }

    private void UpdateLines()
    {
        var bindings = CommandGestureBindings.Resolve(commands, isBuiltIn, overrides);
        Conflicts = bindings.Conflicts;
        foreach (var line in lines)
        {
            var gesture = bindings.GetGesture(line.Descriptor)?.ToString();
            string? conflict = null;
            if (bindings.Conflicts.FirstOrDefault(x => x.Command == line.Descriptor) is { } lost)
                conflict = Describe(lost);
            else if (bindings.Conflicts.Where(x => x.Winner == line.Descriptor).Select(x => Find(x.Command.Id)?.Title ?? x.Command.Id).ToList() is { Count: > 0 } losers)
                conflict = string.Format(Tr._p("Shortcuts", "{0} is also the shortcut of {1}, which does not get it."), gesture, string.Join(", ", losers));
            line.Update(gesture, overrides.ContainsKey(line.Id), conflict);
        }
    }

    private string Describe(CommandGestureConflict conflict)
    {
        var winner = conflict.Winner is { } command ? Find(command.Id)?.Title ?? command.Id : null;
        return conflict.Reason switch
        {
            CommandGestureConflictReason.Taken when winner is not null => string.Format(Tr._p("Shortcuts", "{0} is not bound: {1} has it."), conflict.Gesture, winner),
            CommandGestureConflictReason.UsedByMainWindow when winner is not null => string.Format(Tr._p("Shortcuts", "{0} is not bound: {1} has it in the main window."), conflict.Gesture, winner),
            CommandGestureConflictReason.NeedsModifier => string.Format(Tr._p("Shortcuts", "{0} is not bound: a shortcut needs Ctrl, Alt or Meta, unless its key is a function key."), conflict.Gesture),
            CommandGestureConflictReason.NotAShortcut => string.Format(Tr._p("Shortcuts", "{0} is not bound: it is not a shortcut."), conflict.Gesture),
            _ => string.Format(Tr._p("Shortcuts", "{0} is not bound: the editor uses it."), conflict.Gesture),
        };
    }

    private void Refresh()
    {
        var words = (searchText ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var selected = SelectedShortcut;
        Shortcuts.Clear();
        foreach (var line in lines.Where(line => words.All(word => Texts(line).Any(text => text.Contains(word, StringComparison.CurrentCultureIgnoreCase)))))
            Shortcuts.Add(line);
        SelectedShortcut = selected is not null && Shortcuts.Contains(selected) ? selected : null;
    }

    private static IEnumerable<string> Texts(KeyboardShortcutViewModel line)
    {
        yield return line.Title;
        yield return line.CategoryName;
        yield return line.Id;
        if (line.Gesture is { } gesture)
            yield return gesture;
        foreach (var keyword in line.Descriptor.Keywords)
            yield return keyword;
    }
}
