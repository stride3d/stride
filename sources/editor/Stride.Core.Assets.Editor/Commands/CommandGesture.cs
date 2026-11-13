// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Stride.Core.Assets.Editor.Commands;

/// <summary>
/// The modifier keys of a <see cref="CommandGesture"/>.
/// </summary>
[Flags]
[Experimental("STREXP003")]
public enum CommandModifiers
{
    None = 0,
    Ctrl = 1,
    Shift = 2,
    Alt = 4,
    /// <summary>
    /// The Windows key, or Command on macOS.
    /// </summary>
    Meta = 8,
}

/// <summary>
/// A shortcut, toolkit-neutral: modifier keys and one key, written "Ctrl+Shift+K". Two gestures are equal whatever the order
/// and the case of their parts, so that the editor can tell when two commands claim the same shortcut. Each shell turns it
/// into a gesture of its toolkit from <see cref="ToString"/>.
/// </summary>
[Experimental("STREXP003")]
public readonly record struct CommandGesture
{
    private CommandGesture(CommandModifiers modifiers, string key)
    {
        Modifiers = modifiers;
        Key = key;
    }

    /// <summary>
    /// The modifier keys.
    /// </summary>
    public CommandModifiers Modifiers { get; }

    /// <summary>
    /// The key, as the toolkits name it ("K", "F5", "Delete", "OemPlus"), with its first letter upper case.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Parses a gesture such as "Ctrl+Shift+K": modifiers ("Ctrl" or "Control", "Shift", "Alt", "Meta", "Win" or "Cmd") then
    /// one key, separated by '+', in any case.
    /// </summary>
    /// <exception cref="FormatException">The text is not a gesture.</exception>
    public static CommandGesture Parse(string text)
    {
        return TryParse(text, out var gesture) ? gesture : throw new FormatException($"[{text}] is not a shortcut: write modifiers then one key, such as \"Ctrl+Shift+K\".");
    }

    /// <summary>
    /// Parses a gesture such as "Ctrl+Shift+K" (see <see cref="Parse"/>).
    /// </summary>
    public static bool TryParse([NotNullWhen(true)] string? text, out CommandGesture gesture)
    {
        gesture = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // "Ctrl++" is Ctrl and the plus key
        var parts = text.Trim().EndsWith("++", StringComparison.Ordinal)
            ? [.. text.Trim()[..^2].Split('+'), "+"]
            : text.Split('+');
        var modifiers = CommandModifiers.None;
        string? key = null;
        for (var i = 0; i < parts.Length; ++i)
        {
            var part = parts[i].Trim();
            if (part.Length == 0)
                return false;
            if (i < parts.Length - 1)
            {
                var modifier = ParseModifier(part);
                if (modifier == CommandModifiers.None || (modifiers & modifier) != 0)
                    return false;
                modifiers |= modifier;
            }
            else
            {
                if (ParseModifier(part) != CommandModifiers.None)
                    return false;
                key = part.Length == 1 ? part.ToUpperInvariant() : char.ToUpperInvariant(part[0]) + part[1..];
            }
        }
        if (key is null)
            return false;

        gesture = new CommandGesture(modifiers, key);
        return true;
    }

    /// <summary>
    /// Whether two gestures are the same shortcut, ignoring the case of the key.
    /// </summary>
    public bool Equals(CommandGesture other) => Modifiers == other.Modifiers && string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Modifiers, Key is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Key));

    /// <summary>
    /// The gesture as "Ctrl+Shift+Alt+Meta+Key", which the toolkits parse.
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string>(5);
        if (Modifiers.HasFlag(CommandModifiers.Ctrl))
            parts.Add("Ctrl");
        if (Modifiers.HasFlag(CommandModifiers.Shift))
            parts.Add("Shift");
        if (Modifiers.HasFlag(CommandModifiers.Alt))
            parts.Add("Alt");
        if (Modifiers.HasFlag(CommandModifiers.Meta))
            parts.Add("Meta");
        parts.Add(Key);
        return string.Join("+", parts);
    }

    private static CommandModifiers ParseModifier(string part)
    {
        return part.ToUpperInvariant() switch
        {
            "CTRL" or "CONTROL" => CommandModifiers.Ctrl,
            "SHIFT" => CommandModifiers.Shift,
            "ALT" => CommandModifiers.Alt,
            "META" or "WIN" or "CMD" => CommandModifiers.Meta,
            _ => CommandModifiers.None,
        };
    }
}
