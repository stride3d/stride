// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Text.Json;

namespace Stride.Cli.Core;

/// <summary>
///   Persists, per major.minor line, the version that <see cref="StrideVersionManager.Update"/> currently
///   manages. Versions installed manually with <c>install</c> are never listed here, so update leaves them
///   untouched. Stored under the local application data folder so the CLI and the launcher share it.
/// </summary>
internal sealed class ManagedVersionStore
{
    private readonly string filePath;

    public ManagedVersionStore()
    {
        filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "stride", "installs.json");
    }

    public Dictionary<string, string> Load()
    {
        if (!File.Exists(filePath))
            return new(StringComparer.OrdinalIgnoreCase);

        return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(filePath))
            ?? new(StringComparer.OrdinalIgnoreCase);
    }

    public void Save(Dictionary<string, string> managedVersions)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        // Write to a temporary file then move it into place so a concurrent reader never sees a partial file.
        var temporaryPath = filePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(managedVersions, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporaryPath, filePath, overwrite: true);
    }
}
