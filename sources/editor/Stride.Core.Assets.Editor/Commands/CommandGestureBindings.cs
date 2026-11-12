// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Stride.Core.Assets.Editor.Commands;

/// <summary>
/// Which command each shortcut runs. The shortcuts are claimed in three layers: first the user's (the shortcuts the user gave
/// to commands, or took from them), then the defaults of the editor's own commands, then the defaults of the plugins'
/// commands; in a layer, the commands in registration order. The first to claim a shortcut gets it; a command whose shortcut
/// is taken, or is not a shortcut, is not bound, and the conflict is listed with who keeps the shortcut
/// (<see cref="Conflicts"/>).
/// </summary>
/// <remarks>
/// <para>
/// A shortcut is claimed in its scope (<see cref="CommandDescriptor.GestureScope"/>): the main window, or an editor type.
/// The same shortcut can be bound in the main window and in an editor; the editor's runs while the focus is in that editor.
/// A plugin's default shortcut in an editor cannot take a shortcut that the main window binds, though; the user's can.
/// </para>
/// <para>
/// A shortcut needs Ctrl, Alt or Meta, unless its key is a function key (F1 to F24): a plain or shifted key would be taken
/// from the text boxes. The editor's own defaults are exempt (Edit > Delete is the Delete key): they run after the focused
/// control, which keeps the keys it uses.
/// </para>
/// </remarks>
[Experimental("STREXP003")]
public sealed class CommandGestureBindings
{
    /// <summary>
    /// The value of a user override that removes the shortcut of a command; an empty value does too.
    /// </summary>
    public const string NoGesture = "None";

    private readonly Dictionary<CommandDescriptor, CommandGesture> gestures;

    private CommandGestureBindings(IReadOnlyList<(CommandGesture Gesture, CommandDescriptor Command)> bindings, IReadOnlyList<CommandGestureConflict> conflicts, IReadOnlyCollection<CommandDescriptor> removedByUser)
    {
        Bindings = bindings;
        Conflicts = conflicts;
        Warnings = conflicts.Select(x => x.Message).ToList();
        RemovedByUser = removedByUser;
        gestures = bindings.ToDictionary(x => x.Command, x => x.Gesture);
    }

    /// <summary>
    /// The shortcuts bound to commands, in the registration order of the commands.
    /// </summary>
    public IReadOnlyList<(CommandGesture Gesture, CommandDescriptor Command)> Bindings { get; }

    /// <summary>
    /// The shortcuts that are not bound: which command loses, why, and who keeps the shortcut.
    /// </summary>
    public IReadOnlyList<CommandGestureConflict> Conflicts { get; }

    /// <summary>
    /// A message for each shortcut that is not bound (the messages of <see cref="Conflicts"/>).
    /// </summary>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>
    /// The commands whose shortcut the user removed.
    /// </summary>
    public IReadOnlyCollection<CommandDescriptor> RemovedByUser { get; }

    /// <summary>
    /// The shortcut bound to a command, or null when it has none.
    /// </summary>
    public CommandGesture? GetGesture(CommandDescriptor command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return gestures.TryGetValue(command, out var gesture) ? gesture : null;
    }

    /// <summary>
    /// Binds the default shortcuts of <paramref name="commands"/>, in registration order, as plugins' commands.
    /// </summary>
    /// <param name="reserved">The shortcuts that the editor already uses, each with what uses it ("File > Save", for instance).</param>
    /// <param name="commands">The commands, in registration order.</param>
    /// <param name="describe">How a warning names a command, with its plugin; by default its identifier.</param>
    public static CommandGestureBindings Resolve(IEnumerable<KeyValuePair<string, string>> reserved, IEnumerable<CommandDescriptor> commands, Func<CommandDescriptor, string>? describe = null)
    {
        ArgumentNullException.ThrowIfNull(reserved);
        return Resolve(commands, _ => false, null, reserved, describe);
    }

    /// <summary>
    /// Binds the shortcuts of <paramref name="commands"/>: the user's overrides, then the default shortcuts of the editor's own
    /// commands, then those of the plugins' commands.
    /// </summary>
    /// <param name="commands">The commands, in registration order.</param>
    /// <param name="isBuiltIn">Whether a command is one of the editor's own.</param>
    /// <param name="overrides">The user's shortcuts by command identifier: a shortcut such as "Ctrl+Shift+K", or
    /// <see cref="NoGesture"/> (or empty) for none. A command that is not registered is skipped. Null for none.</param>
    /// <param name="reserved">The shortcuts of the main window that are not commands, each with what uses it; null for none.
    /// They come before every layer.</param>
    /// <param name="describe">How a warning names a command, with its plugin; by default its identifier.</param>
    public static CommandGestureBindings Resolve(IEnumerable<CommandDescriptor> commands, Func<CommandDescriptor, bool> isBuiltIn, IReadOnlyDictionary<string, string>? overrides = null, IEnumerable<KeyValuePair<string, string>>? reserved = null, Func<CommandDescriptor, string>? describe = null)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(isBuiltIn);
        describe ??= x => $"the command [{x.Id}]";

        var list = commands.ToList();
        var owners = new Dictionary<(Type? Scope, CommandGesture Gesture), (string Name, CommandDescriptor? Command)>();
        foreach (var (text, owner) in reserved ?? [])
        {
            if (CommandGesture.TryParse(text, out var gesture))
                owners.TryAdd((null, gesture), (owner, null));
        }

        var claims = new List<(int Index, CommandGesture Gesture, CommandDescriptor Command)>();
        var conflicts = new List<CommandGestureConflict>();
        var removed = new List<CommandDescriptor>();
        var overridden = new HashSet<CommandDescriptor>();

        // The user's layer: a shortcut given to a command, or taken from it
        if (overrides is not null)
        {
            for (var i = 0; i < list.Count; ++i)
            {
                var command = list[i];
                if (!overrides.TryGetValue(command.Id, out var text))
                    continue;
                overridden.Add(command);
                if (IsNoGesture(text))
                {
                    removed.Add(command);
                    continue;
                }
                var name = $"{describe(command)} (a shortcut of the user)";
                if (!CommandGesture.TryParse(text, out var gesture))
                {
                    conflicts.Add(NotAShortcut(command, text, name));
                    continue;
                }
                // The user can give a command back its default shortcut, even one the rule refuses (Edit > Delete)
                if (!IsAllowed(gesture) && !(CommandGesture.TryParse(command.DefaultGesture, out var defaultGesture) && defaultGesture == gesture))
                {
                    conflicts.Add(NeedsModifier(command, gesture, name));
                    continue;
                }
                Claim(i, command, gesture, name);
            }
        }

        // The defaults of the editor's own commands, then those of the plugins
        foreach (var builtIn in new[] { true, false })
        {
            for (var i = 0; i < list.Count; ++i)
            {
                var command = list[i];
                if (overridden.Contains(command) || isBuiltIn(command) != builtIn || command.DefaultGesture is not { } text)
                    continue;
                var name = describe(command);
                if (!CommandGesture.TryParse(text, out var gesture))
                {
                    conflicts.Add(NotAShortcut(command, text, name));
                    continue;
                }
                if (!builtIn && !IsAllowed(gesture))
                {
                    conflicts.Add(NeedsModifier(command, gesture, name));
                    continue;
                }
                if (!builtIn && command.GestureScope is not null && owners.TryGetValue((null, gesture), out var mainOwner))
                {
                    conflicts.Add(new CommandGestureConflict
                    {
                        Command = command,
                        Gesture = gesture.ToString(),
                        Reason = CommandGestureConflictReason.UsedByMainWindow,
                        Winner = mainOwner.Command,
                        Message = $"The shortcut [{gesture}] of {name} is not bound: {mainOwner.Name} uses it in the main window, which a plugin's shortcut in an editor does not take.",
                    });
                    continue;
                }
                Claim(i, command, gesture, name);
            }
        }

        var bindings = claims.OrderBy(x => x.Index).Select(x => (x.Gesture, x.Command)).ToList();
        return new CommandGestureBindings(bindings, conflicts, removed);

        void Claim(int index, CommandDescriptor command, CommandGesture gesture, string name)
        {
            var key = (command.GestureScope, gesture);
            if (owners.TryGetValue(key, out var owner))
            {
                var where = command.GestureScope is { } scope ? $" in {scope.Name}" : string.Empty;
                conflicts.Add(new CommandGestureConflict
                {
                    Command = command,
                    Gesture = gesture.ToString(),
                    Reason = owner.Command is null ? CommandGestureConflictReason.Reserved : CommandGestureConflictReason.Taken,
                    Winner = owner.Command,
                    Message = $"The shortcut [{gesture}] of {name} is not bound: {owner.Name} already uses it{where}.",
                });
                return;
            }
            owners.Add(key, (name, command));
            claims.Add((index, gesture, command));
        }
    }

    /// <summary>
    /// Whether a shortcut can be bound: it has Ctrl, Alt or Meta, or its key is a function key.
    /// </summary>
    public static bool IsAllowed(CommandGesture gesture)
    {
        if ((gesture.Modifiers & (CommandModifiers.Ctrl | CommandModifiers.Alt | CommandModifiers.Meta)) != 0)
            return true;
        return gesture.Key is { Length: >= 2 } key && (key[0] == 'F' || key[0] == 'f') && int.TryParse(key.AsSpan(1), out var number) && number is >= 1 and <= 24;
    }

    /// <summary>
    /// Whether the value of a user override removes the shortcut: <see cref="NoGesture"/>, in any case, or empty.
    /// </summary>
    public static bool IsNoGesture(string? text) => string.IsNullOrWhiteSpace(text) || string.Equals(text.Trim(), NoGesture, StringComparison.OrdinalIgnoreCase);

    private static CommandGestureConflict NotAShortcut(CommandDescriptor command, string text, string name) => new()
    {
        Command = command,
        Gesture = text,
        Reason = CommandGestureConflictReason.NotAShortcut,
        Message = $"The shortcut [{text}] of {name} is not bound: it is not a shortcut (write modifiers then one key, such as \"Ctrl+Shift+K\").",
    };

    private static CommandGestureConflict NeedsModifier(CommandDescriptor command, CommandGesture gesture, string name) => new()
    {
        Command = command,
        Gesture = gesture.ToString(),
        Reason = CommandGestureConflictReason.NeedsModifier,
        Message = $"The shortcut [{gesture}] of {name} is not bound: a shortcut needs Ctrl, Alt or Meta, unless its key is a function key.",
    };
}

/// <summary>
/// Why a shortcut is not bound (<see cref="CommandGestureConflict.Reason"/>).
/// </summary>
[Experimental("STREXP003")]
public enum CommandGestureConflictReason
{
    /// <summary>
    /// The text is not a shortcut.
    /// </summary>
    NotAShortcut,

    /// <summary>
    /// The shortcut has no Ctrl, Alt or Meta and its key is not a function key.
    /// </summary>
    NeedsModifier,

    /// <summary>
    /// The main window uses the shortcut for something that is not a command.
    /// </summary>
    Reserved,

    /// <summary>
    /// Another command claimed the shortcut first, in the same scope.
    /// </summary>
    Taken,

    /// <summary>
    /// A plugin's default shortcut in an editor is a shortcut of the main window.
    /// </summary>
    UsedByMainWindow,
}

/// <summary>
/// A shortcut that is not bound (<see cref="CommandGestureBindings.Conflicts"/>).
/// </summary>
[Experimental("STREXP003")]
public sealed record CommandGestureConflict
{
    /// <summary>
    /// The command that does not get the shortcut.
    /// </summary>
    public required CommandDescriptor Command { get; init; }

    /// <summary>
    /// The shortcut, as written when it is not one.
    /// </summary>
    public required string Gesture { get; init; }

    /// <summary>
    /// Why the command does not get it.
    /// </summary>
    public required CommandGestureConflictReason Reason { get; init; }

    /// <summary>
    /// The command that keeps the shortcut, or null when it is not a command (the shortcut is reserved, or not valid).
    /// </summary>
    public CommandDescriptor? Winner { get; init; }

    /// <summary>
    /// A message that names both, for the log.
    /// </summary>
    public required string Message { get; init; }
}
