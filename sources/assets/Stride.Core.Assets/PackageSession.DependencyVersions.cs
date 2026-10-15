// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Reflection;

namespace Stride.Core.Assets;

/// <summary>
/// A NuGet dependency of a project whose restored version differs from the one the session loaded: a Stride package
/// (one shipping a .sdpkg) of another version, or a library the container loaded from another version's file.
/// </summary>
/// <remarks>
/// <see cref="Apply"/> switches what the session knows of the dependency; reloading <see cref="PackageAssemblies"/>
/// from their new file and the assemblies that reference the dependency completes the change.
/// </remarks>
public sealed class DependencyVersionChange
{
    private PackageVersion? oldVersion;
    private List<string>? oldContainerAssemblies;
    private readonly List<LoadedAssembly> removedLibraries = [];
    // The new version's copies the reload loaded, taken out by Revert and put back by Apply (redo)
    private readonly List<LoadedAssembly> revertedNewCopies = [];

    internal DependencyVersionChange(PackageSession session, Dependency dependency, Package? package)
    {
        Session = session;
        Dependency = dependency;
        Package = package;
    }

    public PackageSession Session { get; }

    /// <summary>The dependency as the project now restores it.</summary>
    public Dependency Dependency { get; }

    /// <summary>The session's package of that name for a Stride package; null for a library.</summary>
    public Package? Package { get; }

    /// <summary>The loaded assemblies of <see cref="Package"/>, and the file of the new version each one loads from.</summary>
    public List<(PackageLoadedAssembly LoadedAssembly, string NewPath)> PackageAssemblies { get; } = [];

    /// <summary>The library's assemblies the container loaded from another version, and the file of the new one.</summary>
    public List<(LoadedAssembly LoadedAssembly, string NewPath)> Libraries { get; } = [];

    /// <summary>The files of the version the session knew, registered with the container.</summary>
    public List<string> OldAssemblyPaths { get; } = [];

    /// <summary>Why the new version can't replace the loaded one in this session, or null when it can.</summary>
    public string? Blocker { get; internal set; }

    public override string ToString()
        => Package is not null ? $"{Dependency.Name} {Package.Meta.Version} -> {Dependency.Version}" : $"{Dependency.Name} -> {Dependency.Version}";

    /// <summary>
    /// Makes the session use the new version: the container knows the new files instead of the old ones, the package
    /// takes the new version and assembly list, and a library's loaded copy leaves the container, so that a reference
    /// to it loads the new file.
    /// </summary>
    public void Apply()
    {
        var container = Session.AssemblyContainer;
        foreach (var path in OldAssemblyPaths)
            container.UnregisterDependency(path);
        foreach (var path in Dependency.Assemblies)
            container.RegisterDependency(path);

        if (Package is not null)
        {
            oldVersion = Package.Meta.Version;
            Package.Meta.Version = Dependency.Version;
            if (Package.Container is StandalonePackage standalonePackage)
            {
                oldContainerAssemblies = [.. standalonePackage.Assemblies];
                standalonePackage.Assemblies.Clear();
                standalonePackage.Assemblies.AddRange(Dependency.Assemblies);
            }
            Dependency.Package = Package;
        }

        removedLibraries.Clear();
        foreach (var (loadedAssembly, _) in Libraries)
        {
            if (container.RemoveAssembly(loadedAssembly.Assembly) is { } removed)
                removedLibraries.Add(removed);
        }
        foreach (var newCopy in revertedNewCopies)
            container.RestoreAssembly(newCopy);
        revertedNewCopies.Clear();
    }

    /// <summary>Undoes <see cref="Apply"/>.</summary>
    public void Revert()
    {
        var container = Session.AssemblyContainer;
        // One assembly of a name in the container: the new version's library copies make way for the old ones
        revertedNewCopies.Clear();
        foreach (var (_, newPath) in Libraries)
        {
            var newCopy = container.LoadedAssemblies.FirstOrDefault(x => string.Equals(x.Path, Path.GetFullPath(newPath), StringComparison.OrdinalIgnoreCase));
            if (newCopy is not null && container.RemoveAssembly(newCopy.Assembly) is { } removed)
                revertedNewCopies.Add(removed);
        }
        foreach (var removed in removedLibraries)
            container.RestoreAssembly(removed);
        removedLibraries.Clear();

        if (Package is not null)
        {
            Package.Meta.Version = oldVersion!;
            if (oldContainerAssemblies is not null && Package.Container is StandalonePackage standalonePackage)
            {
                standalonePackage.Assemblies.Clear();
                standalonePackage.Assemblies.AddRange(oldContainerAssemblies);
            }
        }

        foreach (var path in Dependency.Assemblies)
            container.UnregisterDependency(path);
        foreach (var path in OldAssemblyPaths)
            container.RegisterDependency(path);
    }
}

partial class PackageSession
{
    // The time of each project's lock file when FindDependencyVersionChanges last read it
    private readonly Dictionary<SolutionProject, DateTime> readLockFiles = [];

    /// <summary>
    /// Reads the restored dependencies of <paramref name="project"/> again and returns the ones whose version differs
    /// from what the session loaded. A dependency the session has not loaded anything of is not a change: it loads
    /// from its new files when needed.
    /// </summary>
    public List<DependencyVersionChange> FindDependencyVersionChanges(SolutionProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        // The dependency lists are read again only when the restore wrote a new lock file: views observe them
        var previous = project.FlattenedDependencies.ToList();
        var lockFile = Path.Combine(project.FullPath.GetFullDirectory().ToOSPath(), "obj", NuGet.ProjectModel.LockFileFormat.AssetsFileName);
        var lockFileTime = File.Exists(lockFile) ? File.GetLastWriteTimeUtc(lockFile) : DateTime.MinValue;
        if (!readLockFiles.TryGetValue(project, out var readTime) || readTime != lockFileTime)
        {
            UpdateDependencies(project, directDependencies: false, flattenedDependencies: true);
            readLockFiles[project] = lockFileTime;
        }

        var loadedAssemblies = AssemblyContainer.LoadedAssemblies;
        var changes = new List<DependencyVersionChange>();
        foreach (var dependency in project.FlattenedDependencies)
        {
            if (dependency.Type != DependencyType.Package)
                continue;

            var oldPaths = previous.FirstOrDefault(x => x.Type == DependencyType.Package && string.Equals(x.Name, dependency.Name, StringComparison.OrdinalIgnoreCase) && x.Version != dependency.Version)?.Assemblies ?? [];

            var package = Packages.FirstOrDefault(x => x.Container is StandalonePackage && string.Equals(x.Meta.Name, dependency.Name, StringComparison.OrdinalIgnoreCase));
            if (package is not null)
            {
                if (package.Meta.Version == dependency.Version)
                    continue;

                var change = new DependencyVersionChange(this, dependency, package);
                change.OldAssemblyPaths.AddRange(oldPaths.Union(((StandalonePackage)package.Container).Assemblies, StringComparer.OrdinalIgnoreCase));

                var newPaths = dependency.Assemblies.GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key!, x => x.First(), StringComparer.OrdinalIgnoreCase);
                foreach (var loadedAssembly in package.LoadedAssemblies)
                {
                    if (newPaths.TryGetValue(Path.GetFileName(loadedAssembly.Path), out var newPath))
                        change.PackageAssemblies.Add((loadedAssembly, newPath));
                    else
                        change.Blocker ??= $"the new version has no {Path.GetFileName(loadedAssembly.Path)}";
                }
                // Each assembly of a package loads with it: one the new version adds would need a load of its own
                var added = newPaths.Keys.Except(package.LoadedAssemblies.Select(x => Path.GetFileName(x.Path)), StringComparer.OrdinalIgnoreCase).FirstOrDefault();
                if (added is not null)
                    change.Blocker ??= $"the new version adds {added}";
                // The editor shows the package's assets as the old version loaded them
                if (package.Assets.Count > 0)
                    change.Blocker ??= "it ships assets";
                // Its companions follow its version, but their entry comes from the loaded package: they would stay at
                // the old version, bound to the new one
                if (package.CompanionPackages.Count > 0 || companionLinks.Any(x => x.Declaring == package))
                    change.Blocker ??= "its companion packages follow its version";

                changes.Add(change);
                continue;
            }

            // A library: the container's copy came from another version's file
            var libraryChange = new DependencyVersionChange(this, dependency, null);
            foreach (var newPath in dependency.Assemblies)
            {
                var name = Path.GetFileNameWithoutExtension(newPath);
                var loadedAssembly = loadedAssemblies.FirstOrDefault(x => string.Equals(x.Assembly.GetName().Name, name, StringComparison.OrdinalIgnoreCase));
                if (loadedAssembly is not null && !IsSameFile(loadedAssembly.Path, newPath))
                    libraryChange.Libraries.Add((loadedAssembly, newPath));
            }
            if (libraryChange.Libraries.Count == 0 && oldPaths.Count == 0)
                continue;

            libraryChange.OldAssemblyPaths.AddRange(oldPaths.Concat(libraryChange.Libraries.Select(x => x.LoadedAssembly.Path)).Distinct(StringComparer.OrdinalIgnoreCase));
            changes.Add(libraryChange);
        }
        return changes;
    }

    // A copy next to the game (copied to its output) is the same version as the package cache's file
    private static bool IsSameFile(string path, string otherPath)
    {
        if (string.Equals(Path.GetFullPath(path), Path.GetFullPath(otherPath), StringComparison.OrdinalIgnoreCase))
            return true;

        try
        {
            var file = new FileInfo(path);
            var otherFile = new FileInfo(otherPath);
            return file.Exists && otherFile.Exists && file.Length == otherFile.Length
                && File.ReadAllBytes(path).AsSpan().SequenceEqual(File.ReadAllBytes(otherPath));
        }
        catch (IOException)
        {
            return false;
        }
    }
}
