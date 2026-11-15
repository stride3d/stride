// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Core.Assets;

/// <summary>
/// What to do with a shader file of a project, after a change on the disk.
/// </summary>
public enum ProjectShaderAction
{
    /// <summary>The file has no asset yet.</summary>
    Add,
    /// <summary>The file has an asset, and its content may have changed.</summary>
    ContentChanged,
    /// <summary>The asset has no file anymore.</summary>
    Delete,
}

/// <summary>
/// An action on a shader file, with its path as the session or the project writes it.
/// </summary>
public readonly record struct ProjectShaderChange(string FilePath, ProjectShaderAction Action);

/// <summary>
/// Decides from the state of the disk, the project and the session what a batch of file events means for shader assets.
/// </summary>
/// <remarks>The folder part of a path from a file event can be lower case, so paths are compared without case.</remarks>
public static class ProjectShaderChanges
{
    private const string BuiltExtension = ".sdsl";
    private const string EffectExtension = ".sdfx";

    /// <summary>Whether this is the path of a file that has a shader asset.</summary>
    public static bool IsShaderFile(string path)
    {
        return HasExtension(path, BuiltExtension) || HasExtension(path, EffectExtension);
    }

    /// <summary>Whether this is the path of a file that the build of the user shaders takes.</summary>
    public static bool IsBuiltFile(string path)
    {
        return HasExtension(path, BuiltExtension);
    }

    /// <summary>Whether the path is the folder or inside the folder.</summary>
    public static bool IsUnder(string path, string directory)
    {
        return (Normalize(path) + "/").StartsWith(Normalize(directory).TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Whether a file event on this path can change the shader assets of the project in this folder.
    /// </summary>
    /// <param name="path">The path of the event; a rename has two of them.</param>
    /// <param name="projectDirectory">The folder of the project file.</param>
    /// <param name="changeType">The type of the event.</param>
    /// <param name="isDirectory">Whether the event is about a folder. A deleted path can't tell.</param>
    public static bool IsTouched(string path, string projectDirectory, FileEventChangeType changeType, bool isDirectory)
    {
        if (!IsUnder(path, projectDirectory)
            || IsUnder(path, Path.Combine(projectDirectory, "bin"))
            || IsUnder(path, Path.Combine(projectDirectory, "obj")))
        {
            return false;
        }

        return IsShaderFile(path)
            || changeType == FileEventChangeType.Deleted
            || (isDirectory && changeType is FileEventChangeType.Created or FileEventChangeType.Renamed);
    }

    /// <summary>
    /// Whether <see cref="Plan"/> needs the items of the project, which take an evaluation of the project to get.
    /// </summary>
    public static bool NeedsProjectItems(IEnumerable<string> touchedPaths, IEnumerable<string> sessionFiles, Func<string, bool> fileExists, Func<string, bool> directoryExists)
    {
        var session = new HashSet<string>(sessionFiles.Select(Normalize), StringComparer.OrdinalIgnoreCase);
        return touchedPaths.Any(x => directoryExists(x) || (IsShaderFile(x) && !session.Contains(Normalize(x)) && fileExists(x)));
    }

    /// <summary>
    /// Computes the actions of a batch of file events. A path can be a shader file or a folder of them.
    /// </summary>
    /// <param name="touchedPaths">The paths of the file events.</param>
    /// <param name="sessionFiles">The files of the assets in the session.</param>
    /// <param name="projectItems">The files the project includes.</param>
    /// <param name="deletedInSession">The files of the assets deleted in the session and not saved yet.</param>
    /// <param name="fileExists">Whether a file is on the disk now.</param>
    /// <returns>The actions, with each path taken from the session or from the project.</returns>
    public static List<ProjectShaderChange> Plan(IEnumerable<string> touchedPaths, IEnumerable<string> sessionFiles, IEnumerable<string> projectItems, IEnumerable<string> deletedInSession, Func<string, bool> fileExists)
    {
        var session = ToLookup(sessionFiles);
        var items = ToLookup(projectItems);
        var deleted = ToLookup(deletedInSession);

        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var touchedPath in touchedPaths.Select(Normalize))
        {
            if (IsShaderFile(touchedPath))
                files.Add(touchedPath);

            var folder = touchedPath + "/";
            files.UnionWith(session.Keys.Concat(items.Keys).Where(x => x.StartsWith(folder, StringComparison.OrdinalIgnoreCase)));
        }

        var changes = new List<ProjectShaderChange>();
        foreach (var file in files)
        {
            if (session.TryGetValue(file, out var sessionFile))
                changes.Add(new ProjectShaderChange(sessionFile, fileExists(sessionFile) ? ProjectShaderAction.ContentChanged : ProjectShaderAction.Delete));
            else if (items.TryGetValue(file, out var item) && !deleted.ContainsKey(file) && fileExists(item))
                changes.Add(new ProjectShaderChange(item, ProjectShaderAction.Add));
        }
        return changes;
    }

    private static Dictionary<string, string> ToLookup(IEnumerable<string> files)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files.Where(IsShaderFile))
            lookup[Normalize(file)] = file;
        return lookup;
    }

    private static bool HasExtension(string path, string extension) => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string path) => new UFile(path).FullPath;
}
