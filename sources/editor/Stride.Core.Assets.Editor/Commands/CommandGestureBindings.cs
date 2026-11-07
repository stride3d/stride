// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Stride.Core.Assets.Editor.Commands;

/// <summary>
/// Which command each shortcut runs: the first to claim a shortcut gets it. The shortcuts of the editor's own commands
/// come first, then the commands in registration order (the editor's, then the plugins' in the order they joined). A
/// command whose shortcut is taken, or is not a shortcut, is not bound, with a warning that names both.
/// </summary>
/// <remarks>
/// A shortcut needs Ctrl, Alt or Meta, unless its key is a function key (F1 to F24): a plain or shifted key would be taken
/// from the text boxes.
/// </remarks>
[Experimental("STREXP003")]
public sealed class CommandGestureBindings
{
    private CommandGestureBindings(IReadOnlyList<(CommandGesture Gesture, CommandDescriptor Command)> bindings, IReadOnlyList<string> warnings)
    {
        Bindings = bindings;
        Warnings = warnings;
    }

    /// <summary>
    /// The shortcuts bound to commands.
    /// </summary>
    public IReadOnlyList<(CommandGesture Gesture, CommandDescriptor Command)> Bindings { get; }

    /// <summary>
    /// A message for each shortcut that is not bound.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>
    /// Binds the default shortcuts of <paramref name="commands"/>.
    /// </summary>
    /// <param name="reserved">The shortcuts that the editor already uses, each with what uses it ("File > Save", for instance).</param>
    /// <param name="commands">The commands, in registration order.</param>
    /// <param name="describe">How a warning names a command, with its plugin; by default its identifier.</param>
    public static CommandGestureBindings Resolve(IEnumerable<KeyValuePair<string, string>> reserved, IEnumerable<CommandDescriptor> commands, Func<CommandDescriptor, string>? describe = null)
    {
        ArgumentNullException.ThrowIfNull(reserved);
        ArgumentNullException.ThrowIfNull(commands);
        describe ??= x => $"the command [{x.Id}]";

        var owners = new Dictionary<CommandGesture, string>();
        foreach (var (text, owner) in reserved)
        {
            if (CommandGesture.TryParse(text, out var gesture))
                owners.TryAdd(gesture, owner);
        }

        var bindings = new List<(CommandGesture, CommandDescriptor)>();
        var warnings = new List<string>();
        foreach (var command in commands)
        {
            if (command.DefaultGesture is not { } text)
                continue;
            if (!CommandGesture.TryParse(text, out var gesture))
            {
                warnings.Add($"The shortcut [{text}] of {describe(command)} is not bound: it is not a shortcut (write modifiers then one key, such as \"Ctrl+Shift+K\").");
                continue;
            }
            if (!IsAllowed(gesture))
            {
                warnings.Add($"The shortcut [{gesture}] of {describe(command)} is not bound: a shortcut needs Ctrl, Alt or Meta, unless its key is a function key.");
                continue;
            }
            if (owners.TryGetValue(gesture, out var owner))
            {
                warnings.Add($"The shortcut [{gesture}] of {describe(command)} is not bound: {owner} already uses it.");
                continue;
            }
            owners.Add(gesture, describe(command));
            bindings.Add((gesture, command));
        }
        return new CommandGestureBindings(bindings, warnings);
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
}
