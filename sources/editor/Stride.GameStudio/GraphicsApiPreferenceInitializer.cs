// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Settings;

namespace Stride.GameStudio;

internal static class GraphicsApiPreferenceInitializer
{
    /// <summary>Feeds the persisted editor graphics API to the resolver before its init (-100000); raw-reads the settings file (settings stack isn't up yet).</summary>
    [ModuleInitializer(-200000)]
    internal static void Initialize()
    {
        const string key = "Environment/GraphicsApi:";
        try
        {
            var confPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "stride", "GameStudioSettings.conf");
            if (!File.Exists(confPath))
                return;
            foreach (var line in File.ReadLines(confPath))
            {
                var idx = line.IndexOf(key, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    var value = line.Substring(idx + key.Length).Trim();
                    // "Default" (or empty) means follow the platform default: leave HostPreference unset.
                    if (value.Length > 0 && !value.Equals(EditorSettings.GraphicsApiDefault, StringComparison.OrdinalIgnoreCase))
                        GraphicsApiSelector.HostPreference = value;
                    break;
                }
            }
        }
        catch { /* best-effort: resolver falls back to the default API */ }
    }
}
