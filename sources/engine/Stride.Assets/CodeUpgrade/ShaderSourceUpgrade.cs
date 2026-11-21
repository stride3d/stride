// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Evaluation;
using NuGet.ProjectModel;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Shaders.Parsing.Upgrade;

namespace Stride.Assets;

/// <summary>
/// Upgrades the <c>.sdsl</c> and <c>.sdfx</c> files of a project to the current shader language (<see cref="ShaderUpgrader"/>).
/// </summary>
internal static class ShaderSourceUpgrade
{
    /// <summary>The step that writes <c>effect</c> in place of <c>shader</c> on an effect of an <c>.sdfx</c> file.</summary>
    public const string EffectKeywordsStep = "effect-keywords";

    // Two paths name the same file whatever their case where the file system ignores it, as Path.GetRelativePath reads them
    private static readonly StringComparer PathComparer = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    /// <summary>
    /// The folders that hold the files of a package: the folder of its project, and its asset folders outside of it.
    /// </summary>
    public static IReadOnlyList<string> Folders(UFile projectFullPath, Package package)
    {
        var projectFolder = Path.GetFullPath(projectFullPath.GetFullDirectory().ToOSPath());
        var folders = new List<string> { projectFolder };
        if (package.RootDirectory is { } root)
        {
            folders.AddRange(package.AssetFolders.Select(x => Path.GetFullPath(UPath.Combine(root, x.Path).ToOSPath()))
                .Where(x => Directory.Exists(x) && !IsInside(projectFolder, x)));
        }
        return folders.Distinct(PathComparer).ToList();
    }

    private static bool IsInside(string folder, string path)
    {
        var relative = Path.GetRelativePath(folder, path);
        return !Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    /// <param name="folders">The folders of the project (<see cref="Folders"/>); their shader files are upgraded (<c>bin</c>, <c>obj</c> and the backups of earlier upgrades left out).</param>
    /// <param name="dependencies">The shader files the project uses (path → text) as of this gate (<see cref="DependencyReader"/>): read to know their classes, never changed.</param>
    /// <param name="steps">The steps of the gate (<c>ShaderUpgrader.Steps</c>, <see cref="EffectKeywordsStep"/>): only those run.</param>
    /// <param name="backup">The copy-on-write backup to snapshot each file into before writing it, or <c>null</c>.</param>
    /// <returns><c>false</c> when a file couldn't be read, and nothing was written, or couldn't be written.</returns>
    public static bool Run(IReadOnlyList<string> folders, IReadOnlyDictionary<string, string> dependencies, IReadOnlyCollection<string> steps, UpgradeBackup backup, ILogger log)
    {
        // Every file is read before any is written: the steps decide across files
        var effects = steps.Contains(EffectKeywordsStep) ? Read(folders, "*.sdfx", log) : [];
        var sources = steps.Any(ShaderUpgrader.Steps.Contains) ? Read(folders, "*.sdsl", log) : [];
        if (effects == null || sources == null)
            return false;

        var writes = new List<(ShaderFile File, string Text, string Message)>();
        foreach (var effect in effects)
        {
            try
            {
                if (StridePackageUpgrader.UpgradeEffectShaderKeywords(effect.Text, out var count) is { } upgraded)
                    writes.Add((effect, upgraded, $"[{effect.Name}]: {count} effect(s) declared with 'shader' are now declared with 'effect'."));
            }
            catch (Exception e)
            {
                log.Warning($"Could not upgrade the effect file [{effect.Name}]", e);
            }
        }

        IReadOnlyList<string> notes = [];
        if (sources.Count > 0)
        {
            // A project nested in the folder of this one is upgraded with it
            var own = sources.Select(x => x.FullPath).ToHashSet(PathComparer);
            var dependencyTexts = dependencies.Where(x => !own.Contains(Path.GetFullPath(x.Key))).ToDictionary(x => x.Key, x => x.Value);
            // By name, so that the notes of the upgrade name the files as the messages do
            var result = ShaderUpgrader.Upgrade(sources.ToDictionary(x => x.Name, x => x.Text, StringComparer.Ordinal), EngineShaderClasses.Current,
                dependencies: dependencyTexts, steps: steps);
            var byName = sources.ToDictionary(x => x.Name, StringComparer.Ordinal);
            foreach (var (name, text) in result.Files)
            {
                if (text != byName[name].Text)
                    writes.Add((byName[name], text, $"Upgraded shader [{name}] to the current shader language"));
            }
            notes = result.Notes;
        }

        foreach (var (file, text, message) in writes)
        {
            try
            {
                backup?.Snapshot(file.FullPath);
                File.WriteAllText(file.FullPath, text, new UTF8Encoding(file.HasBom));
                log.Info(message);
            }
            catch (Exception e)
            {
                log.Error($"Unable to write the upgraded shader [{file.Name}]", e);
                return false;
            }
        }
        foreach (var note in notes)
            log.Warning($"Shader upgrade: {note}");
        return true;
    }

    /// <summary>
    /// A library that a project uses, a project it references or a NuGet package: its shader files are read, never changed.
    /// </summary>
    /// <param name="Name">The name of the package, or the path of the project.</param>
    /// <param name="StrideVersion">The Stride version its shaders were written for: the one its own engine dependency names, or, without one, the highest one of what it uses; null when nothing says it.</param>
    /// <param name="Uses">The libraries with shader files that it uses, also through others.</param>
    public sealed record Library(string Name, bool IsProject, IReadOnlyList<string> Files, System.Version StrideVersion, IReadOnlyList<Library> Uses);

    /// <summary>
    /// The libraries that a project uses, those they use first: the projects it references, which the session upgrades before it, and its NuGet
    /// packages. Read from its restore output, and from its project for the projects it references.
    /// </summary>
    public static IReadOnlyList<Library> Dependencies(UFile projectFullPath, PackageSession session, ILogger log)
    {
        var projectDirectory = projectFullPath.GetFullDirectory().ToOSPath();
        var (projectReferences, _) = ReadReferences(projectFullPath, log);

        // A project whose packages can't be read isn't upgraded (UnreadablePackages); one with no package but the engine's may have no restore output
        var libraries = new List<Library>();
        var assetsFile = Path.Combine(projectDirectory, "obj", LockFileFormat.AssetsFileName);
        if (File.Exists(assetsFile))
            libraries.AddRange(FromRestoreOutput(new LockFileFormat().Read(assetsFile), projectDirectory, session));

        // A referenced project that the restore output doesn't list: its version is in its own restore output
        var listed = libraries.Where(x => x.IsProject).Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var reference in projectReferences.Where(x => !listed.Contains(x)))
        {
            var files = ProjectFiles(ProjectFolders(reference, session), "*.sdsl").ToList();
            if (files.Count > 0)
                libraries.Insert(0, new Library(reference, true, files, OwnStrideVersion(reference), []));
        }
        return libraries;
    }

    /// <summary>
    /// Why the shader upgrade can't read the packages a project uses, or null when it can, or when it uses no package but the engine's: its
    /// restore output is missing or can't be read, reports an error for one of them, or lists one that isn't on disk.
    /// </summary>
    public static string UnreadablePackages(UFile projectFullPath, ILogger log)
    {
        if (!ReadReferences(projectFullPath, log).HasPackages)
            return null;
        var assetsFile = Path.Combine(projectFullPath.GetFullDirectory().ToOSPath(), "obj", LockFileFormat.AssetsFileName);
        var shown = $"obj/{LockFileFormat.AssetsFileName}";
        if (!File.Exists(assetsFile))
            return $"it has no restore output ({shown}): its restore failed, or didn't run";
        LockFile lockFile;
        try
        {
            lockFile = new LockFileFormat().Read(assetsFile);
        }
        catch (Exception e)
        {
            return $"its restore output ({shown}) can't be read: {e.Message}";
        }
        if (lockFile.LogMessages.FirstOrDefault(x => x.Level == NuGet.Common.LogLevel.Error && (x.LibraryId == null || !IsEnginePackage(x.LibraryId))) is { } error)
            return $"its restore failed: {error.Message}";
        var missing = Target(lockFile)?.Libraries.Where(x => x.Type == "package" && !IsEnginePackage(x.Name))
            .FirstOrDefault(x => lockFile.GetLibrary(x.Name, x.Version)?.Path is not { } path || !lockFile.PackageFolders.Any(folder => Directory.Exists(Path.Combine(folder.Path, path))));
        return missing != null ? $"its restore output lists the package {missing.Name} {missing.Version}, which isn't in its package folders" : null;
    }

    // The target the libraries are read from: the one with no runtime
    private static LockFileTarget Target(LockFile lockFile)
        => lockFile.Targets.FirstOrDefault(x => string.IsNullOrEmpty(x.RuntimeIdentifier)) ?? lockFile.Targets.LastOrDefault();

    // The projects it references, and whether it references packages, as MSBuild evaluates the project
    private static (List<string> ProjectReferences, bool HasPackages) ReadReferences(UFile projectFullPath, ILogger log)
    {
        Project project = null;
        try
        {
            project = VSProjectHelper.LoadProject(projectFullPath.ToOSPath());
            var references = project.GetItems("ProjectReference").Select(x => Path.GetFullPath(Path.Combine(project.DirectoryPath, x.EvaluatedInclude))).ToList();
            return (references, project.GetItems("PackageReference").Any(x => !IsEnginePackage(x.EvaluatedInclude)));
        }
        catch (Exception e)
        {
            log.Warning($"Unable to load project [{projectFullPath.GetFileName()}]: the shader upgrade reads only what its restore output lists", e);
            return ([], false);
        }
        finally
        {
            project?.ProjectCollection.UnloadAllProjects();
            project?.ProjectCollection.Dispose();
        }
    }

    private static IReadOnlyList<string> ProjectFolders(string projectPath, PackageSession session)
    {
        var path = new UFile(projectPath);
        return session.Projects.OfType<SolutionProject>().FirstOrDefault(x => x.FullPath == path) is { } referenced
            ? Folders(path, referenced.Package)
            : [path.GetFullDirectory().ToOSPath()];
    }

    // The highest engine version that the project depends on directly, in its own restore output
    private static System.Version OwnStrideVersion(string projectPath)
    {
        var assetsFile = Path.Combine(Path.GetDirectoryName(projectPath)!, "obj", LockFileFormat.AssetsFileName);
        if (!File.Exists(assetsFile))
            return null;
        try
        {
            return new LockFileFormat().Read(assetsFile).PackageSpec.TargetFrameworks.SelectMany(x => x.Dependencies)
                .Where(x => IsEnginePackage(x.Name)).Select(x => x.LibraryRange.VersionRange?.MinVersion?.Version).Max();
        }
        catch (Exception)
        {
            return null;
        }
    }

    // The libraries of the restore output, those they use first, with their Stride version: the engine packages they depend on, as their nuspec
    // or project declares them, or for a library with no engine dependency of its own, those of the libraries it uses
    private static List<Library> FromRestoreOutput(LockFile lockFile, string projectDirectory, PackageSession session)
    {
        var target = Target(lockFile);
        if (target == null)
            return [];
        var targetLibraries = target.Libraries.Where(x => !IsEnginePackage(x.Name)).ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        var versions = new Dictionary<string, System.Version>(StringComparer.OrdinalIgnoreCase);
        var closures = new Dictionary<string, List<Library>>(StringComparer.OrdinalIgnoreCase);
        var result = new List<Library>();

        System.Version StrideVersion(string name)
        {
            if (versions.TryGetValue(name, out var version))
                return version;
            versions[name] = null;
            // An old plugin that uses a newer one is still written for its own version
            var dependencies = targetLibraries[name].Dependencies;
            version = dependencies.Where(x => IsEnginePackage(x.Id)).Select(x => x.VersionRange?.MinVersion?.Version).Max()
                ?? dependencies.Where(x => targetLibraries.ContainsKey(x.Id)).Select(x => StrideVersion(x.Id)).Max();
            return versions[name] = version;
        }

        // Post-order, so that a library comes after what it uses
        List<Library> Visit(string name)
        {
            if (closures.TryGetValue(name, out var closure))
                return closure;
            closures[name] = closure = [];
            foreach (var dependency in targetLibraries[name].Dependencies.Where(x => targetLibraries.ContainsKey(x.Id)))
                closure.AddRange(Visit(dependency.Id).Where(x => !closure.Contains(x)));

            var targetLibrary = targetLibraries[name];
            var library = lockFile.GetLibrary(targetLibrary.Name, targetLibrary.Version);
            var isProject = targetLibrary.Type == "project";
            string projectPath = null;
            IReadOnlyList<string> folders = [];
            if (isProject && (library?.MSBuildProject ?? library?.Path) is { } project)
            {
                projectPath = Path.GetFullPath(Path.Combine(projectDirectory, project));
                folders = ProjectFolders(projectPath, session);
            }
            else if (!isProject && library?.Path is { } path)
            {
                folders = lockFile.PackageFolders.Select(x => Path.Combine(x.Path, path)).Where(Directory.Exists).Take(1).ToList();
            }
            var files = ProjectFiles(folders, "*.sdsl").ToList();
            if (files.Count > 0)
            {
                var own = new Library(projectPath ?? name, isProject, files, StrideVersion(name), closure.ToList());
                result.Add(own);
                closures[name] = closure = [.. closure, own];
            }
            return closure;
        }

        foreach (var name in targetLibraries.Keys)
            Visit(name);
        return result;
    }

    /// <summary>
    /// Reads the libraries that a project uses as of one shader gate, in memory: a library written for an older Stride version goes through the
    /// gates above its version up to that one, with what it uses read the same way. One note per library replaces the notes of its upgrade.
    /// </summary>
    public sealed class DependencyReader(IReadOnlyList<Library> libraries, IReadOnlyList<ShaderUpgradeRegistration> gates)
    {
        private readonly Dictionary<string, string> texts = [];
        private readonly Dictionary<(Library, ShaderUpgradeRegistration), IReadOnlyDictionary<string, string>> read = [];
        private readonly Dictionary<Library, PackageVersion> readAs = [];

        /// <summary>
        /// The shader files of every library (path → text) as of <paramref name="gate"/>.
        /// </summary>
        public IReadOnlyDictionary<string, string> Read(ShaderUpgradeRegistration gate)
        {
            var result = new Dictionary<string, string>();
            foreach (var library in libraries)
            {
                foreach (var (path, text) in Read(library, gate))
                    result[path] = text;
            }
            return result;
        }

        private IReadOnlyDictionary<string, string> Read(Library library, ShaderUpgradeRegistration gate)
        {
            if (read.TryGetValue((library, gate), out var result))
                return result;
            result = library.Files.ToDictionary(x => x, x => texts.TryGetValue(x, out var text) ? text : texts[x] = File.ReadAllText(x));
            if (library.StrideVersion is { } version)
            {
                foreach (var crossed in gates.Where(x => version < x.GateVersion.Version && x.GateVersion.Version <= gate.GateVersion.Version && x.Steps.Any(ShaderUpgrader.Steps.Contains)))
                {
                    var uses = new Dictionary<string, string>();
                    foreach (var used in library.Uses)
                    {
                        foreach (var (path, text) in Read(used, crossed))
                            uses[path] = text;
                    }
                    result = ShaderUpgrader.Upgrade(result, EngineShaderClasses.Current, dependencies: uses, steps: crossed.Steps).Files;
                    if (!readAs.TryGetValue(library, out var readVersion) || readVersion < crossed.GateVersion)
                        readAs[library] = crossed.GateVersion;
                }
            }
            return read[(library, gate)] = result;
        }

        /// <summary>
        /// One note per library written for an older Stride version.
        /// </summary>
        public void WriteNotes(ILogger log)
        {
            foreach (var (library, gate) in readAs)
            {
                var (written, read) = (Short(library.StrideVersion), Short(gate.Version));
                log.Warning(library.IsProject
                    ? $"Shader upgrade: {Path.GetFileNameWithoutExtension(library.Name)} is written for Stride {written}: the upgrade read its shaders as upgraded to {read}, and didn't change them. Upgrade it too."
                    : $"Shader upgrade: {library.Name} is written for Stride {written}: the upgrade read its shaders as upgraded to {read}, and didn't change them. Its author should publish a version for this Stride.");
            }

            static string Short(System.Version version) => $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    // A shader file of the project as read: its name is the one the messages give it
    private sealed record ShaderFile(string FullPath, string Name, string Text, bool HasBom);

    // The shader files of the project, or null when one can't be read
    private static List<ShaderFile> Read(IReadOnlyList<string> folders, string pattern, ILogger log)
    {
        var files = new List<ShaderFile>();
        string current = null;
        try
        {
            foreach (var path in ProjectFiles(folders, pattern))
            {
                current = path;
                var bytes = File.ReadAllBytes(path);
                var hasBom = bytes is [0xEF, 0xBB, 0xBF, ..];
                var text = new UTF8Encoding(hasBom).GetString(bytes, hasBom ? 3 : 0, bytes.Length - (hasBom ? 3 : 0));
                files.Add(new ShaderFile(path, Name(folders, path), text, hasBom));
                current = null;
            }
        }
        catch (Exception e)
        {
            log.Error(current != null ? $"Unable to read the shader [{Name(folders, current)}]" : $"Unable to list the {pattern} files of the project", e);
            return null;
        }
        return files;
    }

    private static IEnumerable<string> ProjectFiles(IReadOnlyList<string> folders, string pattern)
    {
        return folders.SelectMany(folder => Directory.EnumerateFiles(folder, pattern, SearchOption.AllDirectories).Where(file =>
        {
            var parts = Path.GetRelativePath(folder, file).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return !parts.Any(x => x.Equals("bin", StringComparison.OrdinalIgnoreCase) || x.Equals("obj", StringComparison.OrdinalIgnoreCase)
                || x.StartsWith(".stride-backup", StringComparison.OrdinalIgnoreCase));
        })).Select(Path.GetFullPath).Distinct(PathComparer);
    }

    // A file as the messages name it: from the folder of the project
    private static string Name(IReadOnlyList<string> folders, string file) => Path.GetRelativePath(folders[0], file).Replace('\\', '/');

    // The engine's classes are those of this version (EngineShaderClasses), not those of the packages the project restored
    private static bool IsEnginePackage(string name) => name.StartsWith("Stride.", StringComparison.OrdinalIgnoreCase)
        && !StridePackagesToSkipUpgrade.PackageNames.Any(x => name.StartsWith(x, StringComparison.OrdinalIgnoreCase));
}
