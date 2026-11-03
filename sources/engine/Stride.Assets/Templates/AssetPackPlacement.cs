// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Stride.Core;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Serialization;
using Version = System.Version;

#nullable enable

namespace Stride.Assets.Templates;

// Where an asset pack goes: which package, then which folders of that package. Shared by source between the Game Studio
// generator and the stride CLI. Failures are an InvalidOperationException whose message is for the user.
internal static class AssetPackPlacement
{
    private const string AssetsFolder = "Assets";
    private const string ResourcesFolder = "Resources";
    private const string Advice = "Run this command inside a package folder, or pass -o to write to a given folder.";

    // The first engine whose packs have every path tagged, which a rewrite needs. Only the numbers count, so that a
    // checkout's 4.4.2-devN qualifies.
    private static readonly Version MinVersionToRewritePaths = new(4, 4, 2);

    private static readonly StringComparison PathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    private static readonly EnumerationOptions Enumeration = new() { IgnoreInaccessible = true };
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

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

    // Resources is null when the package lists no resource folder, and NoResourcesError says what to add.
    public sealed record PackFolders(string Assets, string? Resources, string? NoResourcesError);

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

    // The folders of a package that receive the pack: the first asset folder and the first resource folder it lists.
    public static PackFolders GetFolders(PackageProject project)
    {
        var (assetFolders, resourceFolders) = ReadListedFolders(project);
        if (assetFolders.Count == 0)
            throw new InvalidOperationException($"Package '{project.Name}' lists no asset folder, so it would not see the pack. Add '- Path: !dir {AssetsFolder}' under AssetFolders in '{project.PackagePath}'.");

        return resourceFolders.Count > 0
            ? new PackFolders(assetFolders[0], resourceFolders[0], null)
            : new PackFolders(assetFolders[0], null, $"Package '{project.Name}' lists no resource folder, and this pack has resources. Add '- !dir {ResourcesFolder}' under ResourceFolders in '{project.PackagePath}'.");
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
            throw new InvalidOperationException($"Template '{templateName}' writes {string.Join(", ", unknown.Select(entry => $"'{entry}'"))}, and this version only knows {AssetsFolder} and {ResourcesFolder} as the top-level folders of an asset pack. Update it (the CLI with 'stride self update'), or use 'dotnet new' to write the template to the current folder as it is.");
        if (!Directory.EnumerateFileSystemEntries(packDirectory).Any())
            throw new InvalidOperationException($"Template '{templateName}' writes nothing.");
    }

    // Refuses a pack of an engine older than the minimum when its paths would have to change. In the standard layout
    // nothing is rewritten, so any version works.
    public static void CheckVersionForRewrite(PackageVersion engine, PackFolders folders)
    {
        if (folders.Resources is null || !NeedsRewrite(folders.Assets, folders.Resources))
            return;

        var version = engine.Version;
        if (new Version(version.Major, version.Minor, Math.Max(version.Build, 0)) < MinVersionToRewritePaths)
            throw new InvalidOperationException($"The asset packs of Stride {engine} can only go in the standard layout, with {ResourcesFolder} next to the asset folder, and this package lists '{folders.Assets}' and '{folders.Resources}'. Upgrade the project to Stride {MinVersionToRewritePaths} or later with 'stride upgrade', or pass -o to write to a given folder.");
    }

    // Instantiates a pack into a temporary folder, as the template engine can't rename its folders, then moves it into the
    // folders it goes to. Nothing is written to them when the pack is refused. Says which of the two the pack has.
    public static async Task<(bool HasAssets, bool HasResources)> AddAsync(Func<string, Task> instantiate, string templateName, PackFolders folders)
    {
        var staging = Path.Combine(Path.GetTempPath(), $"stride-new-{Guid.NewGuid():N}");
        try
        {
            await instantiate(staging).ConfigureAwait(false);
            CheckLayout(staging, templateName);

            var stagedAssets = Path.Combine(staging, AssetsFolder);
            var stagedResources = Path.Combine(staging, ResourcesFolder);
            var hasAssets = Directory.Exists(stagedAssets);
            var hasResources = Directory.Exists(stagedResources);

            if (hasResources && folders.NoResourcesError is { } noResources)
                throw new InvalidOperationException(noResources);
            if (hasAssets && hasResources)
                RewriteResourcePaths(stagedAssets, stagedResources, folders.Assets, folders.Resources!);

            if (hasAssets)
                MoveFiles(stagedAssets, folders.Assets);
            if (hasResources)
                MoveFiles(stagedResources, folders.Resources!);
            return (hasAssets, hasResources);
        }
        finally
        {
            if (Directory.Exists(staging))
                Directory.Delete(staging, recursive: true);
        }
    }

    // Points the assets of a staged pack at the folders it goes to. Only a path of an asset that resolves to a staged
    // resource changes, in the text of the file, so every other byte stays as it is.
    public static void RewriteResourcePaths(string stagedAssets, string stagedResources, string assets, string resources)
    {
        if (!NeedsRewrite(assets, resources))
            return;

        var relative = Path.GetRelativePath(assets, resources).Replace('\\', '/');
        if (Path.IsPathRooted(relative))
            throw new InvalidOperationException($"No relative path leads from '{assets}' to '{resources}', so the pack's assets could not refer to their resources. Use folders on the same drive, or pass -o to write to a given folder.");

        var stagedResourceFiles = Directory.EnumerateFiles(stagedResources, "*", SearchOption.AllDirectories).ToHashSet(StringComparer.FromComparison(PathComparison));
        foreach (var file in Directory.EnumerateFiles(stagedAssets, "*", SearchOption.AllDirectories))
        {
            if (!TryReadText(file, out var text))
                continue;

            var stagedFolder = Path.GetDirectoryName(file)!;
            var folder = Path.GetDirectoryName(Path.Combine(assets, Path.GetRelativePath(stagedAssets, file)))!;
            string? rewritten;
            try
            {
                rewritten = YamlFilePaths.Rewrite(text, (path, _) => ResolveResource(path, stagedFolder, stagedResourceFiles) is { } resource
                    ? Path.GetRelativePath(folder, Path.Combine(resources, Path.GetRelativePath(stagedResources, resource))).Replace('\\', '/')
                    : null);
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException($"{e.Message} It is in '{Path.GetFileName(file)}'. Pass -o to write to a given folder.");
            }

            if (rewritten is not null)
                File.WriteAllBytes(file, StrictUtf8.GetBytes(rewritten));
        }
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

    // False for a binary file.
    private static bool TryReadText(string file, out string text)
    {
        try
        {
            text = StrictUtf8.GetString(File.ReadAllBytes(file));
            return !text.Contains('\0');
        }
        catch (DecoderFallbackException)
        {
            text = string.Empty;
            return false;
        }
    }

    private static bool NeedsRewrite(string assets, string resources)
        => Path.GetRelativePath(assets, resources).Replace('\\', '/') != $"../{ResourcesFolder}";

    private static string? ResolveResource(string path, string stagedFolder, HashSet<string> stagedResourceFiles)
    {
        // Windows drops these from the end of a path, and they aren't part of a file name.
        if (path.Length == 0 || path[^1] == '.' || char.IsWhiteSpace(path[^1]))
            return null;

        var resolved = Path.GetFullPath(Path.Combine(stagedFolder, path.Replace('\\', '/')));
        return stagedResourceFiles.Contains(resolved) ? resolved : null;
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

        var stream = new YamlStream();
        try
        {
            using var reader = new StreamReader(project.PackagePath);
            stream.Load(reader);
        }
        catch (YamlException e)
        {
            throw new InvalidOperationException($"Could not read '{project.PackagePath}': {e.Message}");
        }

        var package = stream.Documents.FirstOrDefault()?.RootNode as YamlMappingNode;
        return (ReadFolderList(package, "AssetFolders", item => Child(item as YamlMappingNode, "Path"), project.Directory),
            ReadFolderList(package, "ResourceFolders", item => item, project.Directory));
    }

    // Reads a top-level list of the .sdpkg. The CLI doesn't load the engine's assets library.
    private static List<string> ReadFolderList(YamlMappingNode? package, string key, Func<YamlNode, YamlNode?> folder, string packageDirectory)
        => Child(package, key) is YamlSequenceNode list
            ? list.Children.Select(folder).OfType<YamlScalarNode>().Select(path => Path.GetFullPath(Path.Combine(packageDirectory, path.Value))).ToList()
            : [];

    private static YamlNode? Child(YamlMappingNode? mapping, string key)
        => mapping?.Children.FirstOrDefault(child => child.Key is YamlScalarNode name && name.Value == key).Value;

    private static bool IsSameOrBelow(string path, string folder)
        => path.Equals(folder, PathComparison) || path.StartsWith(folder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, PathComparison);
}
