// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Text;
using Stride.Core.IO;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Settings;

/// <summary>
/// Writes the settings of a profile to a file that other editors write too: only the keys whose value changed since this
/// writer last read or wrote them go to the file, over a fresh copy of it, so the values the other editors wrote since stay.
/// </summary>
/// <remarks>
/// The WPF and Avalonia Game Studios share the internal settings file, and either can run while the other does. Writing the
/// whole profile from memory would put back the values this editor read, such as a list of recent sessions the other editor
/// has changed since.
/// </remarks>
internal sealed class ChangedSettingsWriter
{
    private static readonly MethodInfo SerializeValueMethod = typeof(ChangedSettingsWriter).GetMethod(nameof(SerializeValue), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo CopyValueMethod = typeof(ChangedSettingsWriter).GetMethod(nameof(CopyValue), BindingFlags.NonPublic | BindingFlags.Static)!;

    private readonly Lock gate = new();
    private readonly SettingsContainer container;
    private readonly SettingsProfile profile;
    private readonly UFile filePath;
    // The file as this editor first read it, and the value of each key, serialized, as this editor last wrote it
    private readonly SettingsProfile fileAtStart;
    private readonly Dictionary<UFile, string> writtenValues = [];

    /// <param name="container">The container of the settings.</param>
    /// <param name="profile">The profile this editor reads and changes, loaded from the file.</param>
    /// <param name="filePath">The file.</param>
    public ChangedSettingsWriter(SettingsContainer container, SettingsProfile profile, UFile filePath)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(filePath);
        this.container = container;
        this.profile = profile;
        this.filePath = filePath;
        // Registered in the container: the keys registered after this writer, such as the settings of each Game Studio, convert
        // their values in it as in the profile. In a copy that is not registered they stay unread, and read as the key's default.
        fileAtStart = container.LoadSettingsProfile(filePath, false, null, true) ?? container.CreateSettingsProfile(false, null, true);
    }

    /// <summary>
    /// Writes the keys of the profile that this editor changed to the file, and keeps the other values of the file.
    /// </summary>
    /// <returns><c>false</c> when the file could not be written.</returns>
    public bool Write()
    {
        lock (gate)
        {
            var latest = LoadFile();
            var changes = new List<(UFile Name, string Value)>();
            foreach (var key in container.GetAllSettingsKeys())
            {
                var value = Serialize(key, profile);
                var known = writtenValues.TryGetValue(key.Name, out var written) ? written : Serialize(key, fileAtStart);
                if (value == known)
                    continue;

                Copy(key, profile, latest);
                changes.Add((key.Name, value));
            }
            if (!container.SaveSettingsProfile(latest, filePath))
                return false;

            foreach (var (name, value) in changes)
                writtenValues[name] = value;
            return true;
        }
    }

    // Loaded at each write: the keys registered by then convert their values as it loads, so it needs no registration
    private SettingsProfile LoadFile()
    {
        return container.LoadSettingsProfile(filePath, false, null, false) ?? container.CreateSettingsProfile(false, null, false);
    }

    private static string Serialize(SettingsKey key, SettingsProfile from)
    {
        return (string)SerializeValueMethod.MakeGenericMethod(ValueType(key)).Invoke(null, [key, from])!;
    }

    private static void Copy(SettingsKey key, SettingsProfile from, SettingsProfile to)
    {
        CopyValueMethod.MakeGenericMethod(ValueType(key)).Invoke(null, [key, from, to]);
    }

    private static Type ValueType(SettingsKey key)
    {
        for (var type = key.GetType(); type is not null; type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SettingsKey<>))
                return type.GetGenericArguments()[0];
        }
        throw new NotSupportedException($"The settings key {key.Name} has no value type.");
    }

    private static string SerializeValue<T>(SettingsKey<T> key, SettingsProfile from)
    {
        var value = key.GetValue(from, false);
        if (value is null)
            return string.Empty;

        using var stream = new MemoryStream();
        SettingsYamlSerializer.Default.Serialize(stream, value);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void CopyValue<T>(SettingsKey<T> key, SettingsProfile from, SettingsProfile to)
    {
        key.SetValue(key.GetValue(from, false), to);
    }
}
