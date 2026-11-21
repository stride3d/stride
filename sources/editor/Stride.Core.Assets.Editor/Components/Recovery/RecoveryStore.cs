// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Stride.Core.Assets.Editor.Components.Recovery;

/// <summary>
/// The recovery folders of a session on the disk: one folder per session path under the recovery root, holding one folder per
/// editor process that has the session open. A process folder has a marker (<see cref="MarkerFileName"/>) with the process,
/// and the last recovery copies of the session (<see cref="ManifestFileName"/> and the folder it names).
/// </summary>
internal sealed class RecoveryStore
{
    public const string MarkerFileName = "instance.json";
    public const string ManifestFileName = "copies.json";
    private const string CopiesFolderPrefix = "copies-";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public RecoveryStore(string rootPath, string sessionPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(rootPath);
        ArgumentException.ThrowIfNullOrEmpty(sessionPath);
        SessionPath = Path.GetFullPath(sessionPath);
        SessionFolder = Path.Combine(rootPath, GetSessionFolderName(SessionPath));
    }

    /// <summary>
    /// Gets the full path of the session file (solution or package).
    /// </summary>
    public string SessionPath { get; }

    /// <summary>
    /// Gets the folder of the session, which holds the folders of the processes.
    /// </summary>
    public string SessionFolder { get; }

    /// <summary>
    /// Gets the name of the folder of a session: the name of its file, then a hash of its full path, so that two sessions with the
    /// same name do not share it.
    /// </summary>
    public static string GetSessionFolderName(string sessionPath)
    {
        var fullPath = Path.GetFullPath(sessionPath);
        // The same file on a file system that ignores the case
        var key = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? fullPath.ToUpperInvariant() : fullPath;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)), 0, 8).ToLowerInvariant();
        var name = string.Concat(Path.GetFileNameWithoutExtension(fullPath).Where(x => char.IsLetterOrDigit(x) || x is '-' or '_' or '.'));
        return string.IsNullOrEmpty(name) ? hash : $"{name}-{hash}";
    }

    /// <summary>
    /// Creates a folder for the given process and writes its marker there; each call gets a folder of its own.
    /// </summary>
    /// <returns>The folder.</returns>
    public string WriteMarker(RecoveryMarker marker)
    {
        var folder = Path.Combine(SessionFolder, $"{marker.ProcessId}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        WriteJsonAtomically(Path.Combine(folder, MarkerFileName), marker);
        return folder;
    }

    /// <summary>
    /// Writes the recovery copies of a process: the content of the assets in a new folder, then the manifest that names it, then
    /// removes the copies it replaces. A process that ends while it writes leaves the previous copies readable.
    /// </summary>
    public void WriteCopies(string processFolder, RecoveryManifest manifest, IReadOnlyList<byte[]> contents)
    {
        Directory.CreateDirectory(processFolder);
        var folderName = CopiesFolderPrefix + DateTime.UtcNow.Ticks.ToString("x");
        var copiesFolder = Path.Combine(processFolder, folderName);
        Directory.CreateDirectory(copiesFolder);
        for (var i = 0; i < contents.Count; ++i)
        {
            File.WriteAllBytes(Path.Combine(copiesFolder, i.ToString()), contents[i]);
        }
        manifest.Folder = folderName;
        WriteJsonAtomically(Path.Combine(processFolder, ManifestFileName), manifest);
        DeleteCopiesFolders(processFolder, keep: folderName);
    }

    /// <summary>
    /// Removes the recovery copies of a process, and keeps its marker.
    /// </summary>
    public static void DeleteCopies(string processFolder)
    {
        if (!Directory.Exists(processFolder))
            return;
        TryDelete(() => File.Delete(Path.Combine(processFolder, ManifestFileName)));
        DeleteCopiesFolders(processFolder, keep: null);
    }

    /// <summary>
    /// Removes the folder of a process, with its marker and its copies, and the folder of the session when it is empty.
    /// </summary>
    public void DeleteProcessFolder(string processFolder)
    {
        TryDelete(() =>
        {
            if (Directory.Exists(processFolder))
                Directory.Delete(processFolder, true);
        });
        TryDelete(() =>
        {
            if (Directory.Exists(SessionFolder) && !Directory.EnumerateFileSystemEntries(SessionFolder).Any())
                Directory.Delete(SessionFolder);
        });
    }

    /// <summary>
    /// Gets the copies left by the processes that are gone, oldest first; the copies of a process that runs are never listed.
    /// </summary>
    /// <param name="isAlive">Whether the process of a marker runs.</param>
    /// <param name="ownFolder">The folder of the caller, whose copies are not listed.</param>
    public IReadOnlyList<RecoveredCopies> FindOrphanedCopies(Func<RecoveryMarker, bool> isAlive, string? ownFolder)
    {
        if (!Directory.Exists(SessionFolder))
            return [];

        var result = new List<RecoveredCopies>();
        foreach (var folder in Directory.EnumerateDirectories(SessionFolder))
        {
            if (ownFolder is not null && string.Equals(Path.GetFullPath(folder), Path.GetFullPath(ownFolder), StringComparison.Ordinal))
                continue;
            var marker = ReadJson<RecoveryMarker>(Path.Combine(folder, MarkerFileName));
            // A folder without a marker is being created, or is not ours
            if (marker is null || isAlive(marker))
                continue;

            var manifest = ReadJson<RecoveryManifest>(Path.Combine(folder, ManifestFileName));
            if (manifest?.Folder is null || !Directory.Exists(Path.Combine(folder, manifest.Folder)))
            {
                // Gone without copies: nothing to recover
                DeleteProcessFolder(folder);
                continue;
            }
            result.Add(new RecoveredCopies(folder, marker, manifest));
        }
        return result.OrderBy(x => x.Manifest.Time).ToList();
    }

    /// <summary>
    /// Reads the content of an asset of the given copies.
    /// </summary>
    public static byte[] ReadContent(RecoveredCopies copies, RecoveredAsset asset)
        => File.ReadAllBytes(Path.Combine(copies.Folder, copies.Manifest.Folder!, asset.File.ToString()));

    /// <summary>
    /// Whether the process of a marker runs: a process with the same identifier that started at the same time.
    /// </summary>
    public static bool IsProcessAlive(RecoveryMarker marker)
    {
        try
        {
            using var process = Process.GetProcessById(marker.ProcessId);
            return !process.HasExited && GetStartTicks(process) == marker.ProcessStartTicks;
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            // No such process
            return false;
        }
    }

    /// <summary>
    /// Creates the marker of the current process.
    /// </summary>
    public RecoveryMarker CreateCurrentMarker()
    {
        using var process = Process.GetCurrentProcess();
        return new RecoveryMarker
        {
            ProcessId = process.Id,
            ProcessStartTicks = GetStartTicks(process),
            SessionPath = SessionPath,
        };
    }

    private static long GetStartTicks(Process process) => process.StartTime.ToUniversalTime().Ticks;

    private static void DeleteCopiesFolders(string processFolder, string? keep)
    {
        foreach (var folder in Directory.EnumerateDirectories(processFolder, CopiesFolderPrefix + "*"))
        {
            if (keep is null || !string.Equals(Path.GetFileName(folder), keep, StringComparison.Ordinal))
                TryDelete(() => Directory.Delete(folder, true));
        }
    }

    private static void WriteJsonAtomically<T>(string path, T value)
    {
        var temporaryPath = path + ".tmp";
        File.WriteAllBytes(temporaryPath, JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions));
        File.Move(temporaryPath, path, overwrite: true);
    }

    private static T? ReadJson<T>(string path) where T : class
    {
        try
        {
            return File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllBytes(path), JsonOptions) : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    private static void TryDelete(Action delete)
    {
        try
        {
            delete();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Another process reads it, or it is gone already: the next save, close or start removes it
        }
    }
}

/// <summary>
/// The marker of an editor process that has a session open.
/// </summary>
internal sealed class RecoveryMarker
{
    public int ProcessId { get; set; }

    /// <summary>
    /// Gets or sets when the process started (UTC ticks), so that another process that gets the same identifier is not taken for it.
    /// </summary>
    public long ProcessStartTicks { get; set; }

    public string? SessionPath { get; set; }
}

/// <summary>
/// The list of the recovery copies of a session: what each package had unsaved when they were written.
/// </summary>
internal sealed class RecoveryManifest
{
    public DateTime Time { get; set; }

    /// <summary>
    /// Gets or sets the folder that holds the content of the assets, next to the manifest.
    /// </summary>
    public string? Folder { get; set; }

    public List<RecoveredPackage> Packages { get; set; } = [];
}

/// <summary>
/// What a package of the session had unsaved: its changed and new assets, its deleted assets, and its root assets.
/// </summary>
internal sealed class RecoveredPackage
{
    /// <summary>
    /// Gets or sets the name of the package, which finds it when its file has moved.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the full path of the package file.
    /// </summary>
    public string? Path { get; set; }

    public List<RecoveredAsset> Assets { get; set; } = [];

    public List<RecoveredAsset> DeletedAssets { get; set; } = [];

    /// <summary>
    /// Gets or sets the identifiers of the root assets of the package, when the package itself had unsaved changes.
    /// </summary>
    public List<Guid>? RootAssets { get; set; }
}

/// <summary>
/// An asset of the recovery copies.
/// </summary>
internal sealed class RecoveredAsset
{
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the location of the asset in its package.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the path of its file relative to the folder of the package.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Gets or sets the name of the file of its content in the copies folder; unused for a deleted asset.
    /// </summary>
    public int File { get; set; }
}

/// <summary>
/// The recovery copies that a process left: its folder, its marker and its manifest.
/// </summary>
internal sealed record RecoveredCopies(string Folder, RecoveryMarker Marker, RecoveryManifest Manifest);
