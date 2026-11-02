// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Stride.Cli.Core;

// Where `stride new` puts an asset pack: which package it goes into, then which folders of that package. Failures are
// an InvalidOperationException whose message is for the user.
internal static partial class AssetPackPlacement
{
    private const string AssetsFolder = "Assets";
    private const string ResourcesFolder = "Resources";
    private const string Advice = "Run this command inside a package folder, or pass -o to write to a given folder.";

    private static readonly StringComparison PathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    private static readonly EnumerationOptions Enumeration = new() { IgnoreInaccessible = true };

    // A project that can hold assets. A head is a project that runs (Exe, WinExe), usually for one platform.
    public sealed record PackageProject(string ProjectPath, bool IsHead)
    {
        public string Directory => Path.GetDirectoryName(ProjectPath)!;
        public string Name => Path.GetFileNameWithoutExtension(ProjectPath);
        public string PackagePath => Path.ChangeExtension(ProjectPath, ".sdpkg");
        public bool IsGameLibrary => !IsHead && (Name.EndsWith(".Game", StringComparison.OrdinalIgnoreCase)
            || Path.GetFileName(Directory).EndsWith(".Game", StringComparison.OrdinalIgnoreCase));
    }

    public sealed record PackageChoice(PackageProject Project, string? Note);

    public sealed record PackFolders(string Assets, string Resources, string? ResourcesNote);

    // The package a pack goes into when the command runs in a directory: the one around it, else the one just below it.
    public static PackageChoice ChoosePackage(string directory)
    {
        directory = Path.GetFullPath(directory);
        for (var current = directory; current is not null; current = Path.GetDirectoryName(current))
        {
            var projects = ProjectsIn(current);
            if (projects.Count > 0)
                return ChooseFromInside(current, projects);
        }

        var below = ProjectsBelow(directory);
        if (below.Count == 0)
            throw new InvalidOperationException($"No Stride package in '{directory}', above it or in its subfolders. {Advice}");

        var libraries = below.Where(project => !project.IsHead).ToList();
        return new PackageChoice(libraries.Count > 0 ? PickLibrary(libraries, directory) : PickOne(below, directory), null);
    }

    // The folders of a package that receive the pack. Its assets refer to its resources by relative path, so both move
    // together: the assets into the first folder the package lists, the resources next to that folder. URLs stay the same.
    public static PackFolders GetFolders(PackageProject project)
    {
        var (assetFolders, resourceFolders) = ReadListedFolders(project);
        if (assetFolders.Count == 0)
            throw new InvalidOperationException($"Package '{project.Name}' lists no asset folder, so it would not see the pack. Add '- Path: !dir {AssetsFolder}' under AssetFolders in '{project.PackagePath}'.");

        var assets = assetFolders[0];
        var resources = Path.GetFullPath(Path.Combine(assets, "..", ResourcesFolder));
        var note = resourceFolders.Count == 0 || resourceFolders.Contains(resources, StringComparer.FromComparison(PathComparison))
            ? null
            : $"The resources go in '{resources}' and not in '{resourceFolders[0]}', the folder the package lists: the assets refer to them next to '{assets}'.";
        return new PackFolders(assets, resources, note);
    }

    // The folders of a pack written to a folder the user gave.
    public static PackFolders GetFolders(string output)
        => new(Path.GetFullPath(Path.Combine(output, AssetsFolder)), Path.GetFullPath(Path.Combine(output, ResourcesFolder)), null);

    // The warning to print when a package would not see assets written to this folder, or null when one does.
    public static string? GetOutputWarning(string assets)
    {
        for (var current = Path.GetDirectoryName(assets); current is not null; current = Path.GetDirectoryName(current))
        {
            var projects = ProjectsIn(current);
            if (projects.Count == 0)
                continue;
            if (projects.Any(project => ReadListedFolders(project).Assets.Any(folder => IsSameOrBelow(assets, folder))))
                return null;
            return $"Warning: '{assets}' is not in a folder that package '{projects[0].Name}' lists in its AssetFolders, so the package will not see the pack.";
        }

        return $"Warning: no Stride package around '{assets}' will see the pack.";
    }

    // A pack has the names below at its top level and nothing else. An older CLI so refuses a kind of pack it doesn't know.
    public static void CheckLayout(string packDirectory, string templateName)
    {
        var unknown = Directory.EnumerateFileSystemEntries(packDirectory)
            .Where(entry => !Directory.Exists(entry) || Path.GetFileName(entry) is not (AssetsFolder or ResourcesFolder))
            .Select(entry => Path.GetFileName(entry))
            .Order(StringComparer.Ordinal)
            .ToList();
        if (unknown.Count > 0)
            throw new InvalidOperationException($"Template '{templateName}' writes {string.Join(", ", unknown.Select(entry => $"'{entry}'"))}, and this CLI only knows {AssetsFolder} and {ResourcesFolder} as the top-level folders of an asset pack. Update it with 'stride self update', or use 'dotnet new' to write the template to the current folder as it is.");
        if (!Directory.EnumerateFileSystemEntries(packDirectory).Any())
            throw new InvalidOperationException($"Template '{templateName}' writes nothing.");
    }

    // Moves the files of a staged pack folder into a folder of the package, replacing the ones of the same name.
    public static void MoveFiles(string source, string destination)
    {
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Move(file, target, overwrite: true);
        }
    }

    private static PackageChoice ChooseFromInside(string directory, List<PackageProject> projects)
    {
        var libraries = projects.Where(project => !project.IsHead).ToList();
        if (libraries.Count > 0)
            return new PackageChoice(PickLibrary(libraries, directory), null);

        // A head with a library next to it is one platform of a game: the game's assets are in the library.
        var parent = Path.GetDirectoryName(directory);
        var nextTo = parent is null ? [] : ProjectsBelow(parent).Where(project => !project.IsHead).ToList();
        if (nextTo.Count == 0)
            return new PackageChoice(PickOne(projects, directory), null);

        var library = PickLibrary(nextTo, parent!);
        return new PackageChoice(library, $"'{projects[0].Name}' is a platform project: the pack goes into '{library.Name}', the library next to it. Pass -o to write to a given folder.");
    }

    private static PackageProject PickLibrary(List<PackageProject> libraries, string directory)
    {
        if (libraries.Count == 1)
            return libraries[0];
        var games = libraries.Where(library => library.IsGameLibrary).ToList();
        return PickOne(games.Count > 0 ? games : libraries, directory);
    }

    private static PackageProject PickOne(List<PackageProject> projects, string directory)
        => projects.Count == 1
            ? projects[0]
            : throw new InvalidOperationException($"Several Stride packages in '{directory}': {string.Join(", ", projects.Select(project => $"'{project.Name}'"))}. {Advice}");

    private static List<PackageProject> ProjectsBelow(string directory)
        => Directory.EnumerateDirectories(directory, "*", Enumeration)
            .Where(child => Path.GetFileName(child) is var name && !name.StartsWith('.')
                && !name.Equals("bin", StringComparison.OrdinalIgnoreCase) && !name.Equals("obj", StringComparison.OrdinalIgnoreCase))
            .OrderBy(child => child, StringComparer.OrdinalIgnoreCase)
            .SelectMany(ProjectsIn)
            .ToList();

    private static List<PackageProject> ProjectsIn(string directory)
        => Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*.csproj", Enumeration)
                .Where(project => File.Exists(Path.ChangeExtension(project, ".sdpkg")) || ReferencesStride(project, new HashSet<string>(StringComparer.OrdinalIgnoreCase)))
                .OrderBy(project => project, StringComparer.OrdinalIgnoreCase)
                .Select(project => new PackageProject(project, IsHead(project)))
                .ToList()
            : [];

    private static bool IsHead(string projectPath)
        => Load(projectPath)?.Descendants().FirstOrDefault(element => element.Name.LocalName == "OutputType")?.Value.Trim() is { } type
            && (type.Equals("Exe", StringComparison.OrdinalIgnoreCase) || type.Equals("WinExe", StringComparison.OrdinalIgnoreCase));

    // Directly, or through a project it references, as a game's heads do.
    private static bool ReferencesStride(string projectPath, HashSet<string> visited)
    {
        if (!visited.Add(projectPath) || Load(projectPath) is not { } project)
            return false;

        var directory = Path.GetDirectoryName(projectPath)!;
        return project.Descendants().Any(element => element.Name.LocalName == "PackageReference"
                && element.Attribute("Include")?.Value.StartsWith("Stride.", StringComparison.OrdinalIgnoreCase) == true)
            || project.Descendants()
                .Where(element => element.Name.LocalName == "ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(reference => !string.IsNullOrEmpty(reference) && !reference.Contains('$'))
                .Select(reference => Path.GetFullPath(Path.Combine(directory, reference!.Replace('\\', Path.DirectorySeparatorChar))))
                .Any(reference => ReferencesStride(reference, visited));
    }

    private static XDocument? Load(string projectPath)
    {
        try
        {
            return XDocument.Load(projectPath);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            return null;
        }
    }

    // A project without an .sdpkg is a package with the default folders. Both lists are full paths.
    private static (List<string> Assets, List<string> Resources) ReadListedFolders(PackageProject project)
    {
        if (!File.Exists(project.PackagePath))
            return ([Path.Combine(project.Directory, AssetsFolder)], [Path.Combine(project.Directory, ResourcesFolder)]);

        var lines = File.ReadAllLines(project.PackagePath);
        return (ReadFolderList(lines, "AssetFolders", AssetFolderItem(), project.Directory),
            ReadFolderList(lines, "ResourceFolders", ResourceFolderItem(), project.Directory));
    }

    // Reads a top-level list of the .sdpkg, which is YAML the engine writes. The CLI doesn't load the engine's assets library.
    private static List<string> ReadFolderList(string[] lines, string key, Regex item, string packageDirectory)
    {
        var folders = new List<string>();
        var start = Array.FindIndex(lines, line => line.TrimEnd() == key + ":");
        if (start < 0)
            return folders;

        foreach (var line in lines.Skip(start + 1))
        {
            if (line.Length > 0 && !char.IsWhiteSpace(line[0]) && line[0] != '-')
                break;
            if (item.Match(line) is { Success: true } match)
                folders.Add(Path.GetFullPath(Path.Combine(packageDirectory, match.Groups[1].Value.Trim('"', '\''))));
        }

        return folders;
    }

    private static bool IsSameOrBelow(string path, string folder)
        => path.Equals(folder, PathComparison) || path.StartsWith(folder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, PathComparison);

    [GeneratedRegex(@"^\s*-?\s*Path:\s*(?:!dir\s+)?(\S.*?)\s*$")]
    private static partial Regex AssetFolderItem();

    [GeneratedRegex(@"^\s*-\s*(?:!dir\s+)?(\S.*?)\s*$")]
    private static partial Regex ResourceFolderItem();
}
