// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO.Enumeration;
using System.Text.Json;

namespace Stride.Core.Assets.Editor.Components.Build;

/// <summary>
/// Remembers the last successful build of each project, platform and configuration, and tells whether a new build can be skipped
/// because none of the files it reads changed since then and its output is still there.
/// </summary>
/// <remarks>
/// <para>
/// The files a build reads (<see cref="GameBuildInputs"/>) are the files of the folders of the session (its solution, its packages,
/// their asset and resource folders) and of the projects the built project references, the MSBuild and NuGet files of the folders
/// above them (<c>Directory.Build.props</c>...), and the source files of the assets that live elsewhere. Their size and time are read
/// before each build: a file added, deleted or written since the last build means a build.
/// </para>
/// <para>
/// The output (<see cref="GameBuildOutputs"/>) is the folder of the built assembly, the items returned by the targets of the build, and
/// the restore result of each project (<c>obj/project.assets.json</c>): a file of it deleted or written since the build, by a clean or a
/// package restore, means a build. New files there, and the cache, local and roaming folders where the game writes as it runs, do not
/// count.
/// </para>
/// </remarks>
internal sealed class GameBuildFastPath
{
    private readonly Lock gate = new();
    private readonly Dictionary<string, GameBuildRecord> lastBuilds = [];

    /// <summary>
    /// The key of the builds that produce the same output: the project, its target, configuration, platform and global properties.
    /// </summary>
    public static string GetKey(GameProjectBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var properties = request.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => $"{x.Key}={x.Value}");
        return string.Join('|', [GameBuildInputs.NormalizePath(request.ProjectPath), request.Target, request.Configuration, request.Platform, .. properties]);
    }

    /// <summary>
    /// The last successful build with the given key, if it is remembered.
    /// </summary>
    public GameBuildRecord? GetLastBuild(string key)
    {
        lock (gate)
            return lastBuilds.GetValueOrDefault(key);
    }

    /// <summary>
    /// Remembers a successful build.
    /// </summary>
    public void Record(string key, GameBuildRecord build)
    {
        ArgumentNullException.ThrowIfNull(build);
        lock (gate)
            lastBuilds[key] = build;
    }

    /// <summary>
    /// Forgets the last build with the given key: a new build of it started, and its output is not known until it ends.
    /// </summary>
    public void Forget(string key)
    {
        lock (gate)
            lastBuilds.Remove(key);
    }

    /// <summary>
    /// Forgets every build.
    /// </summary>
    public void Clear()
    {
        lock (gate)
            lastBuilds.Clear();
    }

    /// <summary>
    /// Tells whether the last build with the same key is still up to date.
    /// </summary>
    /// <param name="lastBuild">The last successful build of the same key, or <c>null</c> when none is remembered.</param>
    /// <param name="inputs">The files the build reads, as they are now.</param>
    /// <returns>Whether the build can be skipped, or why not.</returns>
    public static GameBuildCheck Check(GameBuildRecord? lastBuild, GameBuildInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        if (lastBuild is null)
            return new GameBuildCheck(GameBuildCheckResult.NoLastBuild, null, [], 0);

        var changes = inputs.FindChanges(lastBuild.Inputs);
        if (changes.Count > 0)
            return new GameBuildCheck(GameBuildCheckResult.InputsChanged, lastBuild, changes, inputs.Count);

        var outputChanges = lastBuild.Outputs.FindChanges();
        if (outputChanges.Count > 0)
        {
            var restored = outputChanges.Any(GameBuildOutputs.IsRestoreFile);
            return new GameBuildCheck(restored ? GameBuildCheckResult.Restored : GameBuildCheckResult.OutputChanged, lastBuild, outputChanges, inputs.Count);
        }

        return new GameBuildCheck(GameBuildCheckResult.UpToDate, lastBuild, [], inputs.Count);
    }
}

/// <summary>
/// A successful build, as <see cref="GameBuildFastPath"/> remembers it.
/// </summary>
/// <param name="Inputs">The files the build read, as they were when it started.</param>
/// <param name="Outputs">Its output, as it was when it ended.</param>
/// <param name="Time">When it ended, in local time.</param>
/// <param name="AssemblyPath">The assembly it produced.</param>
/// <param name="TargetOutputs">The items returned by its targets.</param>
internal sealed record GameBuildRecord(GameBuildInputs Inputs, GameBuildOutputs Outputs, DateTime Time, string AssemblyPath, IReadOnlyDictionary<string, IReadOnlyList<string>> TargetOutputs)
{
    /// <summary>
    /// The build as the deploy step and the start of the game receive it, when it is skipped.
    /// </summary>
    public IGameProjectBuild ToBuild() => new SkippedGameProjectBuild(AssemblyPath, TargetOutputs);

    private sealed class SkippedGameProjectBuild(string assemblyPath, IReadOnlyDictionary<string, IReadOnlyList<string>> targetOutputs) : IGameProjectBuild
    {
        public string AssemblyPath { get; } = assemblyPath;

        public Task<bool> Completion { get; } = Task.FromResult(true);

        public bool IsCanceled => false;

        public IReadOnlyDictionary<string, IReadOnlyList<string>> TargetOutputs { get; } = targetOutputs;

        public void Cancel()
        {
        }
    }
}

/// <summary>
/// Whether a build can be skipped (<see cref="GameBuildFastPath.Check"/>).
/// </summary>
internal enum GameBuildCheckResult
{
    /// <summary>
    /// No successful build of the same project, platform and configuration is remembered.
    /// </summary>
    NoLastBuild,

    /// <summary>
    /// Files the build reads were added, deleted or written since the last build.
    /// </summary>
    InputsChanged,

    /// <summary>
    /// The output of the last build was deleted or written since.
    /// </summary>
    OutputChanged,

    /// <summary>
    /// The packages of a project were restored since the last build.
    /// </summary>
    Restored,

    /// <summary>
    /// Nothing changed: the build can be skipped.
    /// </summary>
    UpToDate,
}

/// <summary>
/// The answer of <see cref="GameBuildFastPath.Check"/>.
/// </summary>
/// <param name="Result">Whether the build can be skipped, or why not.</param>
/// <param name="LastBuild">The last successful build of the same key, if any.</param>
/// <param name="ChangedFiles">The files that changed since the last build, inputs or outputs.</param>
/// <param name="CheckedFileCount">The number of files the build reads.</param>
internal sealed record GameBuildCheck(GameBuildCheckResult Result, GameBuildRecord? LastBuild, IReadOnlyList<string> ChangedFiles, int CheckedFileCount)
{
    public bool CanSkip => Result == GameBuildCheckResult.UpToDate;
}

/// <summary>
/// The size and the last write time of a file; <see cref="Missing"/> for a file that does not exist.
/// </summary>
internal readonly record struct FileStamp(long Length, DateTime LastWriteTimeUtc)
{
    public static FileStamp Missing => new(-1, default);

    public static FileStamp Of(string path)
    {
        try
        {
            var info = new FileInfo(path);
            return info.Exists ? new FileStamp(info.Length, info.LastWriteTimeUtc) : Missing;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Missing;
        }
    }
}

/// <summary>
/// The folders and files a build of the session reads, as the session knows them (<see cref="GameBuildInputs.Capture"/> adds the
/// projects the built project references).
/// </summary>
/// <param name="ProjectPath">The project file of the built project.</param>
/// <param name="Folders">The folders whose files the build reads: the folders of the solution, of the projects and packages of the
/// session, and their asset and resource folders.</param>
/// <param name="Files">Other files the build reads: the solution file, the source files of the assets.</param>
internal sealed record GameBuildInputScope(string ProjectPath, IReadOnlyList<string> Folders, IReadOnlyList<string> Files);

/// <summary>
/// The files a build reads, with their size and time.
/// </summary>
internal sealed class GameBuildInputs
{
    /// <summary>
    /// The files of the folders above the projects that MSBuild and NuGet read: a new one changes the build too.
    /// </summary>
    internal static readonly IReadOnlyList<string> AncestorBuildFiles =
    [
        "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "Directory.Solution.props", "Directory.Solution.targets",
        "global.json", "NuGet.Config", "nuget.config", "NuGet.config",
    ];

    private static readonly bool IsCaseSensitive = OperatingSystem.IsLinux();
    private static readonly StringComparer PathComparer = IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
    private static readonly StringComparison PathComparison = IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    private readonly Dictionary<string, FileStamp> files;

    private GameBuildInputs(Dictionary<string, FileStamp> files, IReadOnlyList<string> folders, IReadOnlyList<string> projectFolders)
    {
        this.files = files;
        Folders = folders;
        ProjectFolders = projectFolders;
    }

    /// <summary>
    /// The folders that were read, the projects the built project references included.
    /// </summary>
    public IReadOnlyList<string> Folders { get; }

    /// <summary>
    /// The folders of the built project and of the projects it references.
    /// </summary>
    public IReadOnlyList<string> ProjectFolders { get; }

    /// <summary>
    /// The number of files.
    /// </summary>
    public int Count => files.Count;

    /// <summary>
    /// Reads the size and the time of the files a build reads.
    /// </summary>
    public static GameBuildInputs Capture(GameBuildInputScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var projectPath = NormalizePath(scope.ProjectPath);
        var projectFolders = new List<string> { Path.GetDirectoryName(projectPath)! };
        foreach (var reference in ReadProjectReferences(projectPath))
            projectFolders.Add(Path.GetDirectoryName(reference)!);
        projectFolders = projectFolders.Distinct(PathComparer).ToList();
        var folders = RemoveNestedFolders([.. projectFolders, .. scope.Folders.Where(x => !string.IsNullOrEmpty(x)).Select(NormalizePath)]);

        var stamps = new Dictionary<string, FileStamp>(PathComparer);
        foreach (var folder in folders)
            AddFolder(stamps, folder);

        // MSBuild and NuGet look for these files in every folder above a project
        var ancestors = new HashSet<string>(PathComparer);
        foreach (var folder in folders)
        {
            for (var directory = Path.GetDirectoryName(folder); !string.IsNullOrEmpty(directory); directory = Path.GetDirectoryName(directory))
            {
                if (!ancestors.Add(directory))
                    break;
            }
        }
        foreach (var directory in ancestors)
        {
            foreach (var name in AncestorBuildFiles)
                stamps.TryAdd(Path.Combine(directory, name), FileStamp.Of(Path.Combine(directory, name)));
        }

        foreach (var file in scope.Files.Where(x => !string.IsNullOrEmpty(x)).Select(NormalizePath))
            stamps.TryAdd(file, FileStamp.Of(file));

        // A file outside the folders that is missing counts as a stamp of its own, so that its creation is a change
        return new GameBuildInputs(stamps, folders, projectFolders);
    }

    /// <summary>
    /// The files that are new, deleted or changed compared to an earlier capture.
    /// </summary>
    public IReadOnlyList<string> FindChanges(GameBuildInputs earlier)
    {
        ArgumentNullException.ThrowIfNull(earlier);
        var changes = new List<string>();
        foreach (var (path, stamp) in files)
        {
            if (!earlier.files.TryGetValue(path, out var earlierStamp) || earlierStamp != stamp)
                changes.Add(path);
        }
        foreach (var path in earlier.files.Keys)
        {
            if (!files.ContainsKey(path))
                changes.Add(path);
        }
        return changes;
    }

    /// <summary>
    /// The full path of a file or a folder, without a trailing separator.
    /// </summary>
    internal static string NormalizePath(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    /// <summary>
    /// Whether a file or a folder is a build output (<c>bin</c>, <c>obj</c>), a hidden one, or a file the editor writes for itself
    /// next to the packages (their user settings): its changes don't change the build.
    /// </summary>
    internal static bool IsIgnored(ReadOnlySpan<char> name, bool isDirectory)
    {
        if (name.StartsWith('.'))
            return true;
        if (isDirectory)
            return name.Equals("bin", StringComparison.OrdinalIgnoreCase) || name.Equals("obj", StringComparison.OrdinalIgnoreCase);
        return name.EndsWith(".sdpkg.user", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The project files of the projects a project references, directly or not, as its last restore lists them
    /// (<c>obj/project.assets.json</c>); none when it was not restored.
    /// </summary>
    internal static IReadOnlyList<string> ReadProjectReferences(string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath)!;
        var assetsFile = Path.Combine(projectDirectory, "obj", "project.assets.json");
        if (!File.Exists(assetsFile))
            return [];

        try
        {
            using var stream = File.OpenRead(assetsFile);
            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("libraries", out var libraries) || libraries.ValueKind != JsonValueKind.Object)
                return [];

            var references = new List<string>();
            foreach (var library in libraries.EnumerateObject())
            {
                if (library.Value.ValueKind != JsonValueKind.Object
                    || !library.Value.TryGetProperty("type", out var type) || type.GetString() != "project"
                    || !library.Value.TryGetProperty("path", out var path) || path.GetString() is not { Length: > 0 } relativePath)
                {
                    continue;
                }
                references.Add(NormalizePath(Path.Combine(projectDirectory, relativePath.Replace('\\', Path.DirectorySeparatorChar))));
            }
            return references;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            // Being written by a restore: the build that follows reads the new one
            return [];
        }
    }

    private static void AddFolder(Dictionary<string, FileStamp> stamps, string folder)
    {
        if (!Directory.Exists(folder))
            return;

        var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = 0 };
        try
        {
            var enumerable = new FileSystemEnumerable<(string Path, FileStamp Stamp)>(folder, (ref FileSystemEntry entry) => (entry.ToFullPath(), new FileStamp(entry.Length, entry.LastWriteTimeUtc.UtcDateTime)), options)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory && !IsIgnored(entry.FileName, false),
                ShouldRecursePredicate = (ref FileSystemEntry entry) => !IsIgnored(entry.FileName, true),
            };
            foreach (var (path, stamp) in enumerable)
                stamps[path] = stamp;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // The folder is being deleted, or cannot be read: what was read so far counts, and the next capture differs
        }
    }

    private static List<string> RemoveNestedFolders(List<string> folders)
    {
        var result = new List<string>();
        foreach (var folder in folders.Distinct(PathComparer).OrderBy(x => x.Length))
        {
            if (!result.Any(x => IsUnder(folder, x)))
                result.Add(folder);
        }
        return result;
    }

    private static bool IsUnder(string path, string folder)
    {
        return path.Length > folder.Length && path.StartsWith(folder, PathComparison) && (path[folder.Length] == Path.DirectorySeparatorChar || folder.EndsWith(Path.DirectorySeparatorChar));
    }
}

/// <summary>
/// The output of a build: the files of the folder of its assembly, the items returned by its targets, and the restore result of the
/// projects it built, with their size and time when it ended.
/// </summary>
internal sealed class GameBuildOutputs
{
    private const string RestoreFileName = "project.assets.json";

    private readonly Dictionary<string, FileStamp> files;

    private GameBuildOutputs(Dictionary<string, FileStamp> files)
    {
        this.files = files;
    }

    /// <summary>
    /// The number of files.
    /// </summary>
    public int Count => files.Count;

    /// <summary>
    /// Whether the output of a build is there to be checked: its assembly exists.
    /// </summary>
    public bool HasAssembly { get; private init; }

    /// <summary>
    /// Reads the output of a build that just ended.
    /// </summary>
    /// <param name="assemblyPath">The assembly the build produced.</param>
    /// <param name="targetOutputs">The items returned by the targets of the build.</param>
    /// <param name="projectFolders">The folders of the built project and of the projects it references.</param>
    public static GameBuildOutputs Capture(string assemblyPath, IReadOnlyDictionary<string, IReadOnlyList<string>> targetOutputs, IEnumerable<string> projectFolders)
    {
        var stamps = new Dictionary<string, FileStamp>(OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
        var hasAssembly = !string.IsNullOrEmpty(assemblyPath) && File.Exists(assemblyPath);
        if (hasAssembly)
        {
            var folder = Path.GetDirectoryName(Path.GetFullPath(assemblyPath))!;
            var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = 0 };
            try
            {
                var enumerable = new FileSystemEnumerable<(string Path, FileStamp Stamp)>(folder, (ref FileSystemEntry entry) => (entry.ToFullPath(), new FileStamp(entry.Length, entry.LastWriteTimeUtc.UtcDateTime)), options)
                {
                    ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory,
                    // The game writes its own files there as it runs
                    ShouldRecursePredicate = (ref FileSystemEntry entry) => !IsGameFolder(entry.Directory, entry.FileName, folder),
                };
                foreach (var (path, stamp) in enumerable)
                    stamps[path] = stamp;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                hasAssembly = false;
            }
        }

        foreach (var item in targetOutputs.Values.SelectMany(x => x))
        {
            if (Path.IsPathFullyQualified(item) && File.Exists(item))
                stamps.TryAdd(item, FileStamp.Of(item));
        }

        foreach (var folder in projectFolders)
        {
            var restoreFile = Path.Combine(folder, "obj", RestoreFileName);
            stamps.TryAdd(restoreFile, FileStamp.Of(restoreFile));
        }

        return new GameBuildOutputs(stamps) { HasAssembly = hasAssembly };
    }

    /// <summary>
    /// The files of the output that were deleted or written since it was read.
    /// </summary>
    public IReadOnlyList<string> FindChanges()
    {
        var changes = new List<string>();
        foreach (var (path, stamp) in files)
        {
            if (FileStamp.Of(path) != stamp)
                changes.Add(path);
        }
        return changes;
    }

    /// <summary>
    /// The folders next to its assembly where a game writes as it runs (<c>PlatformFolders</c>): its cache, local and roaming
    /// files, which are no output of the build.
    /// </summary>
    internal static readonly IReadOnlyList<string> GameFolders = ["cache", "local", "roaming"];

    private static bool IsGameFolder(ReadOnlySpan<char> parent, ReadOnlySpan<char> name, string outputFolder)
    {
        if (!Path.TrimEndingDirectorySeparator(parent).Equals(Path.TrimEndingDirectorySeparator(outputFolder.AsSpan()), StringComparison.Ordinal))
            return false;
        foreach (var gameFolder in GameFolders)
        {
            if (name.Equals(gameFolder, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Whether a file is the restore result of a project.
    /// </summary>
    public static bool IsRestoreFile(string path) => string.Equals(Path.GetFileName(path), RestoreFileName, StringComparison.OrdinalIgnoreCase);
}
