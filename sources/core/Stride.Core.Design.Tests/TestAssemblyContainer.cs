// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using Stride.Core.Reflection;
using Xunit;

namespace Stride.Core.Design.Tests;

public class TestAssemblyContainer
{
    [Fact]
    public void HostCopyWins()
    {
        // A package shipping its own copy of an assembly the host has
        using var directory = new TemporaryDirectory();
        var hostAssembly = typeof(AssemblyContainer).Assembly;
        var copy = Path.Combine(directory.Path, Path.GetFileName(hostAssembly.Location));
        File.Copy(hostAssembly.Location, copy);

        var container = new AssemblyContainer();
        Assert.Same(hostAssembly, container.LoadAssemblyFromPath(copy));
        Assert.Empty(container.LoadedAssemblies);
    }

    [Fact]
    public void PackageAssemblyAndItsDependencyLoadWithoutException()
    {
        using var directory = new TemporaryDirectory();
        var (packagePath, dependencyName) = EmitPackageWithDependency(directory.Path);

        var notFound = 0;
        void OnFirstChance(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is FileNotFoundException or FileLoadException)
                Interlocked.Increment(ref notFound);
        }

        var container = new AssemblyContainer();
        AppDomain.CurrentDomain.FirstChanceException += OnFirstChance;
        Assembly? package;
        Type? baseType;
        try
        {
            package = container.LoadAssemblyFromPath(packagePath);
            baseType = package?.GetType("PackageType")?.BaseType;
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= OnFirstChance;
        }

        Assert.NotNull(package);
        Assert.NotNull(baseType);
        Assert.Equal(dependencyName, baseType.Assembly.GetName().Name);

        // The dependency the host does not provide loads in the context of the assembly that brought it
        var context = AssemblyLoadContext.GetLoadContext(package);
        Assert.NotSame(AssemblyLoadContext.Default, context);
        Assert.Same(context, AssemblyLoadContext.GetLoadContext(baseType.Assembly));
        Assert.Equal(0, notFound);
    }

    [Fact]
    public void ReloadLoadsANewCopyThatNamesResolveTo()
    {
        using var directory = new TemporaryDirectory();
        var (packagePath, _) = EmitPackageWithDependency(directory.Path);
        var packageName = Path.GetFileNameWithoutExtension(packagePath);

        var container = new AssemblyContainer();
        var original = container.LoadAssemblyFromPath(packagePath);
        Assert.NotNull(original);
        var originalDependency = original.GetType("PackageType")!.BaseType!.Assembly;

        // Rebuilt: same name, new file (loaded from bytes, so it can be overwritten)
        var removed = container.RemoveAssembly(original);
        Assert.NotNull(removed);
        EmitPackageWithDependency(directory.Path, packageName, originalDependency);
        var reloaded = container.LoadAssemblyFromPath(packagePath);

        Assert.NotNull(reloaded);
        Assert.NotSame(original, reloaded);
        Assert.NotSame(AssemblyLoadContext.GetLoadContext(original), AssemblyLoadContext.GetLoadContext(reloaded));
        // The dependency was not reloaded: the new copy binds to the current one
        Assert.Same(originalDependency, reloaded.GetType("PackageType")!.BaseType!.Assembly);
        // A name read by host code resolves to the current copy
        Assert.Same(reloaded, Type.GetType($"PackageType, {packageName}", throwOnError: false)?.Assembly);

        // Undo: the original copy is current again
        container.RemoveAssembly(reloaded);
        container.RestoreAssembly(removed);
        Assert.Same(original, Type.GetType($"PackageType, {packageName}", throwOnError: false)?.Assembly);

        container.UnloadAssembly(original);
        container.UnloadAssembly(originalDependency);
    }

    [Fact]
    public void DependentsClosureFollowsReferencesDependenciesFirst()
    {
        using var directory = new TemporaryDirectory();
        var (packagePath, _) = EmitPackageWithDependency(directory.Path);

        var container = new AssemblyContainer();
        var package = container.LoadAssemblyFromPath(packagePath)!;
        var dependency = package.GetType("PackageType")!.BaseType!.Assembly;

        // Reloading the dependency reloads the package that references it, after it
        Assert.Equal([dependency, package], container.GetDependentsClosure([dependency]));
        // Nothing references the package
        Assert.Equal([package], container.GetDependentsClosure([package]));

        container.UnloadAssembly(package);
        container.UnloadAssembly(dependency);
    }

    [Fact]
    public void ReferenceFromHostCodeBindsTheContainerCopy()
    {
        using var directory = new TemporaryDirectory();
        var (packagePath, dependencyName) = EmitPackageWithDependency(directory.Path);

        var container = new AssemblyContainer();
        var package = container.LoadAssemblyFromPath(packagePath)!;
        var dependency = package.GetType("PackageType")!.BaseType!.Assembly;

        // A lookup by name from code that doesn't reference it (as WPF or the YAML serializer) binds nothing
        Assert.Same(package, Type.GetType($"PackageType, {package.GetName().Name}", throwOnError: false)?.Assembly);
        Assert.False(AssemblyContainer.IsBoundByHost(package));
        // References between assemblies of the container bind nothing either
        Assert.False(AssemblyContainer.IsBoundByHost(dependency));

        // Host code (default context) deriving from a type of the container's copy
        var hostName = "Stride.HostCodeProbe" + Guid.NewGuid().ToString("N");
        var dependencyImage = File.ReadAllBytes(Path.Combine(directory.Path, dependencyName + ".dll"));
        var dependencyType = new AssemblyLoadContext(null, isCollectible: true).LoadFromStream(new MemoryStream(dependencyImage)).GetType("DependencyType")!;
        var builder = new PersistedAssemblyBuilder(new AssemblyName(hostName), typeof(object).Assembly);
        builder.DefineDynamicModule(hostName).DefineType("HostType", TypeAttributes.Public, dependencyType).CreateType();
        var hostImage = new MemoryStream();
        builder.Save(hostImage);
        hostImage.Position = 0;
        var host = AssemblyLoadContext.Default.LoadFromStream(hostImage);

        Assert.Same(dependency, host.GetType("HostType")!.BaseType!.Assembly);
        Assert.True(AssemblyContainer.IsBoundByHost(dependency));
        Assert.False(AssemblyContainer.IsBoundByHost(package));

        container.UnloadAssembly(package);
        container.UnloadAssembly(dependency);
    }

    /// <summary>
    /// Writes a package assembly whose type derives from a type of a dependency next to it.
    /// </summary>
    /// <param name="dependency">An already loaded dependency to reference instead of writing a new one.</param>
    private static (string PackagePath, string DependencyName) EmitPackageWithDependency(string directory, string? packageName = null, Assembly? dependency = null)
    {
        packageName ??= "Stride.PackageProbe" + Guid.NewGuid().ToString("N");
        Type dependencyType;
        if (dependency is null)
        {
            var dependencyName = "Stride.DependencyProbe" + Guid.NewGuid().ToString("N");
            var dependencyBuilder = new PersistedAssemblyBuilder(new AssemblyName(dependencyName), typeof(object).Assembly);
            dependencyBuilder.DefineDynamicModule(dependencyName).DefineType("DependencyType", TypeAttributes.Public).CreateType();
            var dependencyPath = Path.Combine(directory, dependencyName + ".dll");
            dependencyBuilder.Save(dependencyPath);

            // Only to name its type in the package: a throwaway context, never the one the container loads it in
            var image = File.ReadAllBytes(dependencyPath);
            dependencyType = new AssemblyLoadContext(null, isCollectible: true).LoadFromStream(new MemoryStream(image)).GetType("DependencyType")!;
        }
        else
        {
            dependencyType = dependency.GetType("DependencyType")!;
        }

        var builder = new PersistedAssemblyBuilder(new AssemblyName(packageName), typeof(object).Assembly);
        var type = builder.DefineDynamicModule(packageName).DefineType("PackageType", TypeAttributes.Public, dependencyType);
        type.DefineDefaultConstructor(MethodAttributes.Public);
        type.CreateType();
        var packagePath = Path.Combine(directory, packageName + ".dll");
        builder.Save(packagePath);
        return (packagePath, dependencyType.Assembly.GetName().Name!);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Stride.AssemblyContainer." + Guid.NewGuid().ToString("N"));

        public TemporaryDirectory() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); }
            catch (IOException) { /* a file loaded by path stays locked */ }
        }
    }

    [Fact]
    public void TypeNameOfAContainerAssemblyResolvesFromOutside()
    {
        // An assembly only a container loads (from bytes, in a load context of its own)
        var assemblyName = "Stride.ContainerProbe" + Guid.NewGuid().ToString("N");
        var directory = Path.Combine(Path.GetTempPath(), assemblyName);
        Directory.CreateDirectory(directory);
        try
        {
            var builder = new System.Reflection.Emit.PersistedAssemblyBuilder(new System.Reflection.AssemblyName(assemblyName), typeof(object).Assembly);
            builder.DefineDynamicModule(assemblyName).DefineType("Probe", System.Reflection.TypeAttributes.Public).CreateType();
            var path = Path.Combine(directory, assemblyName + ".dll");
            builder.Save(path);

            var container = new AssemblyContainer();
            var assembly = container.LoadAssemblyFromPath(path);
            Assert.NotNull(assembly);

            // A generic argument naming it binds through the default context (as WPF reads a BAML type), which only
            // the container knows how to answer
            var type = Type.GetType($"System.Collections.Generic.List`1[[Probe, {assemblyName}]]", throwOnError: false);
            Assert.NotNull(type);
            Assert.Same(assembly, type.GetGenericArguments()[0].Assembly);

            container.UnloadAssembly(assembly);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
