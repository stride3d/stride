// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NuGet.Frameworks;
using NuGet.ProjectModel;
using NuGet.Versioning;
using Xunit;

namespace Stride.Core.Assets.Tests;

public class TestDependencyVersionChanges : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "Stride.DependencyVersions." + Guid.NewGuid().ToString("N"));
    private readonly string packagesFolder;
    private readonly PackageSession session = new();
    private readonly SolutionProject gameProject;

    public TestDependencyVersionChanges()
    {
        packagesFolder = Path.Combine(directory, "packages");
        Directory.CreateDirectory(Path.Combine(directory, "Game", "obj"));
        gameProject = new SolutionProject(new Package { Meta = { Name = "Game" } }, Guid.NewGuid(), Path.Combine(directory, "Game", "Game.csproj"));
        session.Projects.Add(gameProject);
    }

    public void Dispose()
    {
        try { Directory.Delete(directory, recursive: true); }
        catch (IOException) { /* a file loaded by path stays locked */ }
    }

    [Fact]
    public void PackageOfAnotherVersionMovesItsAssembliesToTheNewFiles()
    {
        var oldPath = PackageFile("Plugin", "1.0.4", "Plugin.dll");
        var plugin = AddPackage("Plugin", "1.0.4", oldPath);
        WriteLockFile(("Plugin", "1.0.5", ["Plugin.dll"]));

        var change = Assert.Single(session.FindDependencyVersionChanges(gameProject));
        Assert.Null(change.Blocker);
        Assert.Same(plugin, change.Package);
        var (loadedAssembly, newPath) = Assert.Single(change.PackageAssemblies);
        Assert.Same(plugin.LoadedAssemblies[0], loadedAssembly);
        Assert.Equal(PackageFile("Plugin", "1.0.5", "Plugin.dll"), newPath);

        change.Apply();
        Assert.Equal(new PackageVersion("1.0.5"), plugin.Meta.Version);
        Assert.Equal([newPath], ((StandalonePackage)plugin.Container).Assemblies);
        Assert.Same(plugin, change.Dependency.Package);

        change.Revert();
        Assert.Equal(new PackageVersion("1.0.4"), plugin.Meta.Version);
        Assert.Equal([oldPath], ((StandalonePackage)plugin.Container).Assemblies);
    }

    [Fact]
    public void PackageWhoseAssembliesChangedIsBlocked()
    {
        AddPackage("Dropped", "1.0.4", PackageFile("Dropped", "1.0.4", "Dropped.dll"), PackageFile("Dropped", "1.0.4", "Dropped.Extra.dll"));
        AddPackage("Grown", "1.0.4", PackageFile("Grown", "1.0.4", "Grown.dll"));
        WriteLockFile(("Dropped", "1.0.5", ["Dropped.dll"]), ("Grown", "1.0.5", ["Grown.dll", "Grown.Extra.dll"]));

        var changes = session.FindDependencyVersionChanges(gameProject);
        Assert.Contains("Dropped.Extra.dll", changes.Single(x => x.Dependency.Name == "Dropped").Blocker);
        Assert.Contains("Grown.Extra.dll", changes.Single(x => x.Dependency.Name == "Grown").Blocker);
    }

    [Fact]
    public void PackageWithCompanionsIsBlocked()
    {
        var plugin = AddPackage("Plugin", "1.0.4", PackageFile("Plugin", "1.0.4", "Plugin.dll"));
        var pluginAssets = AddPackage("Plugin.Assets", "1.0.4", PackageFile("Plugin.Assets", "1.0.4", "Plugin.Assets.dll"));
        session.AddCompanionLink(plugin, pluginAssets);
        WriteLockFile(("Plugin", "1.0.5", ["Plugin.dll"]));

        var change = session.FindDependencyVersionChanges(gameProject).Single(x => x.Dependency.Name == "Plugin");
        Assert.Contains("companion", change.Blocker);
    }

    [Fact]
    public void SameVersionIsNoChange()
    {
        AddPackage("Plugin", "1.0.4", PackageFile("Plugin", "1.0.4", "Plugin.dll"));
        WriteLockFile(("Plugin", "1.0.4", ["Plugin.dll"]));

        Assert.Empty(session.FindDependencyVersionChanges(gameProject));
    }

    [Fact]
    public void LibraryLoadedFromAnotherVersionLeavesTheContainer()
    {
        var name = "Stride.LibraryProbe" + Guid.NewGuid().ToString("N");
        var oldPath = EmitLibrary(name, "1.0.4", typeCount: 1);
        var newPath = EmitLibrary(name, "1.0.5", typeCount: 2);
        var library = session.AssemblyContainer.LoadAssemblyFromPath(oldPath);
        Assert.NotNull(library);
        WriteLockFile((name, "1.0.5", [name + ".dll"]));

        var change = Assert.Single(session.FindDependencyVersionChanges(gameProject));
        Assert.Null(change.Package);
        var (loadedAssembly, changedPath) = Assert.Single(change.Libraries);
        Assert.Same(library, loadedAssembly.Assembly);
        Assert.Equal(newPath, changedPath);
        Assert.Contains(oldPath, change.OldAssemblyPaths);

        change.Apply();
        Assert.DoesNotContain(session.AssemblyContainer.LoadedAssemblies, x => x.Assembly == library);
        // The reload of what references it loads the new version
        var newLibrary = session.AssemblyContainer.LoadAssemblyFromPath(newPath);
        Assert.NotNull(newLibrary);
        Assert.NotSame(library, newLibrary);

        // Undo: the old copy is the container's one again
        change.Revert();
        Assert.Contains(session.AssemblyContainer.LoadedAssemblies, x => x.Assembly == library);
        Assert.DoesNotContain(session.AssemblyContainer.LoadedAssemblies, x => x.Assembly == newLibrary);

        // Redo
        change.Apply();
        Assert.DoesNotContain(session.AssemblyContainer.LoadedAssemblies, x => x.Assembly == library);
        Assert.Contains(session.AssemblyContainer.LoadedAssemblies, x => x.Assembly == newLibrary);
        session.AssemblyContainer.UnloadAssembly(newLibrary);
    }

    [Fact]
    public void UnchangedLockFileIsNotReadAgain()
    {
        AddPackage("Plugin", "1.0.4", PackageFile("Plugin", "1.0.4", "Plugin.dll"));
        WriteLockFile(("Plugin", "1.0.5", ["Plugin.dll"]));
        Assert.Single(session.FindDependencyVersionChanges(gameProject));
        var dependencies = gameProject.FlattenedDependencies.ToList();

        // Still a change (nothing applied it), from the same dependency objects
        var change = Assert.Single(session.FindDependencyVersionChanges(gameProject));
        Assert.Equal(dependencies, gameProject.FlattenedDependencies);
        Assert.Same(dependencies[0], change.Dependency);
    }

    [Fact]
    public void LibraryCopiedNextToTheGameIsNoChange()
    {
        var name = "Stride.LibraryProbe" + Guid.NewGuid().ToString("N");
        var packagePath = EmitLibrary(name, "1.0.4", typeCount: 1);
        var copy = Path.Combine(directory, "Game", "bin", name + ".dll");
        Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
        File.Copy(packagePath, copy);
        var library = session.AssemblyContainer.LoadAssemblyFromPath(copy);
        Assert.NotNull(library);
        WriteLockFile((name, "1.0.4", [name + ".dll"]));

        Assert.Empty(session.FindDependencyVersionChanges(gameProject));
        session.AssemblyContainer.UnloadAssembly(library);
    }

    private Package AddPackage(string name, string version, params string[] assemblies)
    {
        var package = new Package { Meta = { Name = name, Version = new PackageVersion(version) } };
        var container = new StandalonePackage(package) { IsDependencyPackage = true };
        container.Assemblies.AddRange(assemblies);
        foreach (var assembly in assemblies)
            package.LoadedAssemblies.Add(new PackageLoadedAssembly(null, assembly));
        session.Projects.Add(container);
        return package;
    }

    private string PackageFile(string name, string version, string file)
        => Path.Combine(packagesFolder, name.ToLowerInvariant(), version, "lib", "net10.0", file);

    private string EmitLibrary(string name, string version, int typeCount)
    {
        var builder = new PersistedAssemblyBuilder(new AssemblyName(name), typeof(object).Assembly);
        var module = builder.DefineDynamicModule(name);
        for (var i = 0; i < typeCount; i++)
            module.DefineType("LibraryType" + i, TypeAttributes.Public).CreateType();
        var path = PackageFile(name, version, name + ".dll");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        builder.Save(path);
        return path;
    }

    // The restored graph as NuGet writes it in obj/project.assets.json
    private void WriteLockFile(params (string Name, string Version, string[] Files)[] packages)
    {
        var lockFile = new LockFile { Version = 3 };
        lockFile.PackageFolders.Add(new LockFileItem(packagesFolder + Path.DirectorySeparatorChar));
        var target = new LockFileTarget { TargetFramework = NuGetFramework.Parse("net10.0") };
        foreach (var (name, version, files) in packages)
        {
            var nugetVersion = NuGetVersion.Parse(version);
            Directory.CreateDirectory(Path.Combine(packagesFolder, name.ToLowerInvariant(), version));
            lockFile.Libraries.Add(new LockFileLibrary { Name = name, Version = nugetVersion, Type = "package", Path = $"{name.ToLowerInvariant()}/{version}" });
            var library = new LockFileTargetLibrary { Name = name, Version = nugetVersion, Type = "package" };
            foreach (var file in files)
                library.RuntimeAssemblies.Add(new LockFileItem($"lib/net10.0/{file}"));
            target.Libraries.Add(library);
        }
        lockFile.Targets.Add(target);
        new LockFileFormat().Write(Path.Combine(directory, "Game", "obj", LockFileFormat.AssetsFileName), lockFile);
    }
}
