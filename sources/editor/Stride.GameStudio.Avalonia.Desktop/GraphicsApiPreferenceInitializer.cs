// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Assets;

namespace Stride.GameStudio.Avalonia.Desktop;

/// <summary>
/// Feeds the editor graphics API to the resolver before its init (-100000): the persisted preference, raw-read from the
/// settings file (the settings stack isn't up yet; same file and key as the WPF Game Studio), as <see cref="ViewportGraphicsApi"/>
/// resolves it.
/// </summary>
internal static class GraphicsApiPreferenceInitializer
{
    private const string GraphicsApiDefault = "Default";

    /// <summary>
    /// The persisted preference that <see cref="ViewportGraphicsApi"/> overrode, to tell the user once the logger is up.
    /// </summary>
    public static string? OverriddenPreference { get; private set; }

    [ModuleInitializer(-200000)]
    internal static void Initialize()
    {
        string? preference = null;
        const string key = "Environment/GraphicsApi:";
        try
        {
            var confPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "stride", "GameStudioSettings.conf");
            if (File.Exists(confPath))
            {
                foreach (var line in File.ReadLines(confPath))
                {
                    var idx = line.IndexOf(key, StringComparison.Ordinal);
                    if (idx >= 0)
                    {
                        var value = line.Substring(idx + key.Length).Trim();
                        // "Default" (or empty) means follow the platform default
                        if (value.Length > 0 && !value.Equals(GraphicsApiDefault, StringComparison.OrdinalIgnoreCase))
                            preference = value;
                        break;
                    }
                }
            }
        }
        catch { /* best-effort: resolver falls back to the default API */ }

        if (ViewportGraphicsApi.Overrides(preference))
            OverriddenPreference = preference;
        // null leaves HostPreference unset: the resolver follows the platform default
        GraphicsApiSelector.HostPreference = ViewportGraphicsApi.Resolve(preference);
    }
}
