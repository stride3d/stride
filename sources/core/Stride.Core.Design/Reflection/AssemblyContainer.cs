// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using Stride.Core.Diagnostics;

namespace Stride.Core.Reflection;

public class LoadedAssembly
{
    public AssemblyContainer Container { get; }
    public string Path { get; }
    public Assembly Assembly { get; }

    public Dictionary<string, string>? Dependencies { get; }

    public LoadedAssembly(AssemblyContainer container, string path, Assembly assembly, Dictionary<string, string>? dependencies)
    {
        Container = container;
        Path = path;
        Assembly = assembly;
        Dependencies = dependencies;
    }
}
public class AssemblyContainer
{
    private readonly List<LoadedAssembly> loadedAssemblies = [];
    private readonly Dictionary<string, LoadedAssembly> loadedAssembliesByName = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly HashSet<string> dependencies = new(StringComparer.InvariantCultureIgnoreCase);
    private static readonly string[] KnownAssemblyExtensions = [".dll", ".exe"];

    [ThreadStatic]
    private static LoggerResult? log;

    private static readonly ConditionalWeakTable<Assembly, LoadedAssembly> assemblyToContainers = [];

    /// <summary>
    /// The default assembly container loader.
    /// </summary>
    public static readonly AssemblyContainer Default = new();

    static AssemblyContainer()
    {
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
    }

    /// <summary>
    /// Gets a copy of the list of loaded assemblies.
    /// </summary>
    /// <value>
    /// The loaded assemblies.
    /// </value>
    public IList<LoadedAssembly> LoadedAssemblies
    {
        get
        {
            lock (loadedAssemblies)
            {
                return [.. loadedAssemblies];
            }
        }
    }

    public Assembly? LoadAssemblyFromPath(string assemblyFullPath, ILogger? outputLog = null)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(assemblyFullPath);
#else
        if (assemblyFullPath is null) throw new ArgumentNullException(nameof(assemblyFullPath));
#endif

        assemblyFullPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, assemblyFullPath));
        var assemblyDirectory = Path.GetDirectoryName(assemblyFullPath);

        if (assemblyDirectory == null || !Directory.Exists(assemblyDirectory))
        {
            throw new ArgumentException("Invalid assembly path. Doesn't contain directory information");
        }

        // Dependencies resolved while loading (module initializers included) log here too; resolved later, they don't log
        var previousLog = log;
        var currentLog = log = new LoggerResult();
        try
        {
            return LoadAssemblyFromPathInternal(assemblyFullPath);
        }
        finally
        {
            log = previousLog;
            if (outputLog != null)
            {
                currentLog.CopyTo(outputLog);
            }
        }
    }

    public bool UnloadAssembly(Assembly assembly)
    {
        return RemoveAssembly(assembly) is not null;
    }

    /// <summary>
    /// Removes an assembly from this container and returns what the container knew about it, or null when it holds
    /// no such assembly. <see cref="RestoreAssembly"/> takes that back.
    /// </summary>
    public LoadedAssembly? RemoveAssembly(Assembly assembly)
    {
        lock (loadedAssemblies)
        {
            var loadedAssembly = loadedAssemblies.FirstOrDefault(x => x.Assembly == assembly);
            if (loadedAssembly == null)
                return null;

            loadedAssemblies.Remove(loadedAssembly);
            loadedAssembliesByName.Remove(loadedAssembly.Path);
            assemblyToContainers.Remove(assembly);
            return loadedAssembly;
        }
    }

    /// <summary>
    /// Takes back an assembly this container removed, with the path and dependencies it was loaded with. The types of
    /// an assembly put back are live again, and resolving what they reference goes through this container.
    /// </summary>
    public void RestoreAssembly(LoadedAssembly loadedAssembly)
    {
        ArgumentNullException.ThrowIfNull(loadedAssembly);

        lock (loadedAssemblies)
        {
            if (loadedAssemblies.Any(x => x.Assembly == loadedAssembly.Assembly))
                return;

            loadedAssemblies.Add(loadedAssembly);
            loadedAssembliesByName[loadedAssembly.Path] = loadedAssembly;
            assemblyToContainers.AddOrUpdate(loadedAssembly.Assembly, loadedAssembly);
        }
    }

    public void RegisterDependency(string assemblyFullPath)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(assemblyFullPath);
#else
        if (assemblyFullPath is null) throw new ArgumentNullException(nameof(assemblyFullPath));
#endif

        lock (dependencies)
        {
            dependencies.Add(assemblyFullPath);
        }
    }

    /// <summary>
    /// Forgets a file <see cref="RegisterDependency"/> made known, as when a dependency moves to another version: a
    /// reference by name then finds the other version's file.
    /// </summary>
    public void UnregisterDependency(string assemblyFullPath)
    {
        ArgumentNullException.ThrowIfNull(assemblyFullPath);

        lock (dependencies)
        {
            dependencies.Remove(assemblyFullPath);
        }
    }

    /// <summary>
    /// The current assembly of this container that a reference to <paramref name="assemblyName"/> binds to, whatever load context holds it; otherwise null.
    /// </summary>
    private Assembly? FindLoadedAssembly(AssemblyName assemblyName)
    {
        // Note: Do not compare by full name as it is too restricive in regards to versioning.
        // For example consider App -> Foo -> Bar (1.0) with the App referencing a newer version of Bar (2.0)
        // A full name comparison would now compare Bar (1.0) to Bar (2.0) and fail to load.
        lock (loadedAssemblies)
        {
            return loadedAssemblies.FirstOrDefault(x => AssemblyName.ReferenceMatchesDefinition(assemblyName, x.Assembly.GetName()))?.Assembly;
        }
    }

    /// <summary>
    /// The file a reference to <paramref name="assemblyName"/> loads from, for an assembly the host does not provide; otherwise null.
    /// </summary>
    /// <param name="requestingPath">The file of the assembly whose load context asks.</param>
    private string? FindDependencyPath(AssemblyName assemblyName, string requestingPath)
    {
        // First, use .deps.json files (generated by MSBuild when PreserveCompilationContext == true)
        // It ensures that we load the same assemblies as were used while compiling. The requesting assembly's own
        // comes first: another one may have been built against another version.
        lock (loadedAssemblies)
        {
            if (loadedAssembliesByName.TryGetValue(requestingPath, out var requesting) && requesting.Dependencies?.TryGetValue(assemblyName.Name!, out var requestedPath) == true)
                return requestedPath;

            foreach (var loadedAssembly in loadedAssemblies)
            {
                if (loadedAssembly.Dependencies?.TryGetValue(assemblyName.Name!, out var fullPath) == true)
                    return fullPath;
            }
        }

        var searchDirectory = Path.GetDirectoryName(requestingPath)!;

        // Look in search paths
        foreach (var knownExtension in KnownAssemblyExtensions)
        {
            var assemblyFullPath = Path.Combine(searchDirectory, assemblyName.Name + knownExtension);
            if (File.Exists(assemblyFullPath))
                return assemblyFullPath;
        }

        // See if it was registered
        lock (dependencies)
        {
            foreach (var dependency in dependencies)
            {
                // Check by simple name first
                var otherName = Path.GetFileNameWithoutExtension(dependency);
                if (string.Equals(assemblyName.Name, otherName, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var otherAssemblyName = AssemblyName.GetAssemblyName(dependency);
                        if (AssemblyName.ReferenceMatchesDefinition(assemblyName, otherAssemblyName))
                            return dependency;
                    }
                    catch (Exception)
                    {
                        // Ignore
                    }
                }
            }
        }

        return null;
    }

    /// <param name="context">The load context of the assembly whose reference asks for this one, or null for an assembly the container is asked to load.</param>
    private Assembly? LoadAssemblyFromPathInternal(string assemblyFullPath, ContainerLoadContext? context = null)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(assemblyFullPath);
#else
        if (assemblyFullPath is null) throw new ArgumentNullException(nameof(assemblyFullPath));
#endif

        assemblyFullPath = Path.GetFullPath(assemblyFullPath);

        try
        {
            lock (loadedAssemblies)
            {
                if (loadedAssembliesByName.TryGetValue(assemblyFullPath, out var loadedAssembly))
                {
                    return loadedAssembly.Assembly;
                }

                // One assembly of a name per container: a second copy (the same assembly in another output folder)
                // would register its types and serializers twice
                var simpleName = Path.GetFileNameWithoutExtension(assemblyFullPath);
                var sameName = loadedAssemblies.FirstOrDefault(x => string.Equals(x.Assembly.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase));
                if (sameName != null)
                {
                    log?.Warning($"Assembly [{simpleName}] is already loaded from [{sameName.Path}]; not loading [{assemblyFullPath}]");
                    return sameName.Assembly;
                }

                if (!File.Exists(assemblyFullPath))
                    return null;

                if (context is null)
                {
                    // The host's copy wins. One it has loaded already needs no context: a context can't be freed
                    var hostAssembly = AssemblyLoadContext.Default.Assemblies.FirstOrDefault(x => !x.IsDynamic && string.Equals(x.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase));
                    if (hostAssembly is not null)
                        return hostAssembly;

                    // A load context of its own, asked by name: the host's copy still wins (the default context, with the
                    // trusted platform assemblies and the host's resolvers), else the context loads this file. Reloading
                    // the assembly loads the new copy in a new context.
                    context = new ContainerLoadContext(this, assemblyFullPath);
                    if (context.PrimaryName is { } primaryName)
                        return context.LoadFromAssemblyName(primaryName);
                }

                // The assembly loads from bytes, without a location for its module initializer to probe natives from
                RegisterNativeLibraries(assemblyFullPath);

                // Find pdb (if it exists)
                var pdbFullPath = Path.ChangeExtension(assemblyFullPath, ".pdb");
                if (!File.Exists(pdbFullPath))
                    pdbFullPath = null;

                // PreLoad the assembly into memory without locking it
                var assemblyBytes = File.ReadAllBytes(assemblyFullPath);
                var pdbBytes = pdbFullPath != null ? File.ReadAllBytes(pdbFullPath) : null;

                // Load .deps.json file (if any)
                var depsFile = Path.ChangeExtension(assemblyFullPath, ".deps.json");
                Dictionary<string, string>? dependenciesMapping = null;
                if (File.Exists(depsFile))
                {
                    // Read file
                    var dependenciesReader = new DependencyContextJsonReader();
                    DependencyContext? dependencies = null;
                    using (var dependenciesStream = File.OpenRead(depsFile))
                    {
                        dependencies = dependenciesReader.Read(dependenciesStream);
                    }

                    // Locate NuGet package folder
                    var settings = NuGet.Configuration.Settings.LoadDefaultSettings(Path.GetDirectoryName(assemblyFullPath));
                    var globalPackagesFolder = NuGet.Configuration.SettingsUtility.GetGlobalPackagesFolder(settings);

                    // Build list of assemblies listed in .deps.json file
                    dependenciesMapping = [];
                    foreach (var library in dependencies.RuntimeLibraries)
                    {
                        if (library.Path == null)
                            continue;

                        foreach (var runtimeAssemblyGroup in library.RuntimeAssemblyGroups)
                        {
                            foreach (var runtimeFile in runtimeAssemblyGroup.RuntimeFiles)
                            {
                                var fullPath = Path.Combine(globalPackagesFolder, library.Path, runtimeFile.Path);
                                if (File.Exists(fullPath))
                                {
                                    var assemblyName = Path.GetFileNameWithoutExtension(runtimeFile.Path);

                                    // TODO: Properly deal with file duplicates (same file in multiple package, or RID conflicts)
                                    dependenciesMapping.TryAdd(assemblyName, fullPath);
                                }
                                else
                                {
                                    // DLL missing from NuGet cache — check for dev-redirect stub
                                    var packageFolder = Path.Combine(globalPackagesFolder, library.Path);
                                    var assemblyName = Path.GetFileNameWithoutExtension(runtimeFile.Path);
                                    var redirectPath = TryResolveDevRedirect(packageFolder, library.Name, assemblyName);
                                    if (redirectPath != null)
                                        dependenciesMapping.TryAdd(assemblyName, redirectPath);
                                }
                            }
                        }

                        // It seems .deps.json files don't contain library info if referencer is non-RID specific and dependency is RID specific
                        // (i.e. compiling Game project without runtime identifier and a dependency needs runtime identifier)
                        // TODO: Look for a proper way to query that properly, maybe using NuGet API? however some constraints are:
                        // - limited info (only .deps.json available from path, no project info; we might need to reconstruct a proper NuGet restore request from .deps.json)
                        // - need to be fast (make sure to use NuGet caching mechanism)
                        if (library.RuntimeAssemblyGroups.Count == 0)
                        {
                            var runtimeFolder = new[] { "win", "any" }
                                .Select(runtime => Path.Combine(globalPackagesFolder, library.Path, "runtimes", runtime))
                                .Where(Directory.Exists)
                                .SelectMany(folder => Directory.EnumerateDirectories(Path.Combine(folder, "lib")))
                                .FirstOrDefault(file => Path.GetFileName(file).StartsWith("net", StringComparison.Ordinal)); // Only consider framework netXX and netstandardX.X
                            if (runtimeFolder != null)
                            {
                                foreach (var runtimeFile in Directory.EnumerateFiles(runtimeFolder, "*.dll"))
                                {
                                    var assemblyName = Path.GetFileNameWithoutExtension(runtimeFile);

                                    // TODO: Properly deal with file duplicates (same file in multiple package, or RID conflicts)
                                    dependenciesMapping.TryAdd(assemblyName, runtimeFile);
                                }
                            }
                        }
                    }
                }

                Assembly assembly;
                const uint unverifiableExecutableWithFixups = 0x80131019;
                try
                {
                    using var assemblyStream = new MemoryStream(assemblyBytes);
                    using var pdbStream = pdbBytes != null ? new MemoryStream(pdbBytes) : null;
                    assembly = context.LoadFromStream(assemblyStream, pdbStream);
                }
                catch (FileLoadException fileLoadException)
                when ((uint)fileLoadException.HResult == unverifiableExecutableWithFixups)
                {
                    // No way to load from byte array (see https://stackoverflow.com/questions/5005409/exception-with-resolving-assemblies-attempt-to-load-an-unverifiable-executable)
                    assembly = Assembly.LoadFrom(assemblyFullPath);
                }
                catch (BadImageFormatException)
                {
                    // It could be a mixed mode assembly (see https://stackoverflow.com/questions/2945080/how-do-i-dynamically-load-raw-assemblies-that-contains-unmanaged-codebypassing)
                    assembly = Assembly.LoadFrom(assemblyFullPath);
                }
                loadedAssembly = new LoadedAssembly(this, assemblyFullPath, assembly, dependenciesMapping);
                loadedAssemblies.Add(loadedAssembly);
                loadedAssembliesByName.Add(assemblyFullPath, loadedAssembly);

                // Its [DllImport] calls find the native libraries registered above only through a resolver on its load context
                NativeLibraryHelper.RegisterResolverFor(assembly);

                // Make sure there is no duplicate
                Debug.Assert(!assemblyToContainers.TryGetValue(assembly, out var _));
                // Add to mapping
                assemblyToContainers.GetValue(assembly, _ => loadedAssembly);

                // Make sure that Module initializer are called
                foreach (var module in assembly.Modules)
                {
                    RuntimeHelpers.RunModuleConstructor(module.ModuleHandle);
                }
                return assembly;
            }
        }
        catch (Exception exception)
        {
            log?.Error($"Error while loading assembly reference [{assemblyFullPath}]", exception);
            if (exception is ReflectionTypeLoadException loaderException)
            {
                foreach (var exceptionForType in loaderException.LoaderExceptions)
                {
                    log?.Error("Unable to load type. See exception.", exceptionForType);
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Registers the native libraries shipped with an assembly: next to it, or at the package root for the NuGet
    /// layout lib/&lt;framework&gt;/&lt;assembly&gt; (the game's dependencies resolve from the package cache).
    /// </summary>
    private static void RegisterNativeLibraries(string assemblyFullPath)
    {
        var assemblyDirectory = Path.GetDirectoryName(assemblyFullPath)!;
        NativeLibraryHelper.RegisterDependenciesFrom(assemblyDirectory);

        var libDirectory = Path.GetDirectoryName(assemblyDirectory);
        if (libDirectory is not null && string.Equals(Path.GetFileName(libDirectory), "lib", StringComparison.OrdinalIgnoreCase))
            NativeLibraryHelper.RegisterDependenciesFrom(Path.GetDirectoryName(libDirectory)!);
    }

    /// <summary>
    /// Whether code outside this assembly's container (the host's) references it and resolved that reference: that
    /// binding stays, so the assembly can't be swapped for a new copy without mixing the types of both.
    /// </summary>
    public static bool IsBoundByHost(Assembly assembly) => boundByHost.TryGetValue(assembly, out _);

    private static readonly ConditionalWeakTable<Assembly, object> boundByHost = [];

    /// <summary>
    /// These assemblies and the ones of this container that reference them, directly or through another, dependencies
    /// first. A reference stays bound to the copy it resolved first, so reloading an assembly reloads those too.
    /// </summary>
    public List<Assembly> GetDependentsClosure(IEnumerable<Assembly> assemblies)
    {
        List<Assembly> loaded;
        lock (loadedAssemblies)
        {
            loaded = loadedAssemblies.Select(x => x.Assembly).ToList();
        }

        // One assembly of a name per container
        var byName = loaded.GroupBy(x => x.GetName().Name!, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var references = loaded.ToDictionary(x => x, x => x.GetReferencedAssemblies()
            .Select(reference => reference.Name is { } name ? byName.GetValueOrDefault(name) : null)
            .OfType<Assembly>()
            .ToList());

        var closure = new HashSet<Assembly>(assemblies);
        bool added;
        do
        {
            added = false;
            foreach (var (assembly, referenced) in references)
            {
                if (!closure.Contains(assembly) && referenced.Any(closure.Contains))
                    added = closure.Add(assembly);
            }
        } while (added);

        var ordered = new List<Assembly>();
        var visited = new HashSet<Assembly>();
        void Visit(Assembly assembly)
        {
            if (!visited.Add(assembly))
                return;
            foreach (var referenced in references.GetValueOrDefault(assembly) ?? [])
            {
                if (closure.Contains(referenced))
                    Visit(referenced);
            }
            ordered.Add(assembly);
        }
        foreach (var assembly in assemblies.Concat(loaded).Where(closure.Contains))
            Visit(assembly);
        return ordered;
    }

    // Asked from outside the containers' load contexts (e.g. WPF binding a BAML type's generic arguments, a type name
    // read by host code): a container's current copy. The default context does not keep the answer, so a reload is seen.
    private static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name);
        var assembly = FindContainerAssembly(assemblyName);

        // The requesting assembly is the caller of a lookup by name too: only one that references the assembly binds to it
        if (assembly is not null && args.RequestingAssembly is { } requesting
            && !(AssemblyLoadContext.GetLoadContext(requesting) is ContainerLoadContext context && assemblyToContainers.TryGetValue(assembly, out var loadedAssembly) && context.Container == loadedAssembly.Container)
            && requesting.GetReferencedAssemblies().Any(x => string.Equals(x.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase)))
        {
            boundByHost.AddOrUpdate(assembly, requesting);
        }
        return assembly;
    }

    // Only the assemblies currently in a container are mapped: removing one unmaps it
    private static Assembly? FindContainerAssembly(AssemblyName assemblyName)
    {
        foreach (var (assembly, _) in assemblyToContainers)
        {
            var name = assembly.GetName();
            if (string.Equals(name.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase)
                && (assemblyName.Version is null || name.Version >= assemblyName.Version))
                return assembly;
        }
        return null;
    }

    // Platform-aware probe order for dev-redirect HintPaths that contain $(StrideGraphicsApi).
    // Picks the first API whose dev-built DLL exists on disk; we can't know the eventual runtime
    // API at resolver time (Game/GraphicsAdapter haven't initialized yet), so we prefer the
    // native API for the host platform.
    private static readonly string[] GraphicsApiProbeOrder = OperatingSystem.IsWindows()
        ? new[] { "Direct3D11", "Direct3D12", "Vulkan", "OpenGL", "OpenGLES" }
        : new[] { "Vulkan", "OpenGL", "OpenGLES" };

    /// <summary>
    /// Checks if a NuGet package folder contains a dev-redirect stub props and resolves the assembly path from its HintPath.
    /// </summary>
    private static string? TryResolveDevRedirect(string packageFolder, string packageName, string assemblyName)
    {
        var propsPath = Path.Combine(packageFolder, "build", $"{packageName}.props");
        if (!File.Exists(propsPath))
            return null;

        try
        {
            var doc = System.Xml.Linq.XDocument.Load(propsPath);
            System.Xml.Linq.XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";

            if (doc.Descendants(ns + "StrideDevRedirect").FirstOrDefault()?.Value != "true")
                return null;

            var devRoot = doc.Descendants(ns + "StrideDevRoot").FirstOrDefault()?.Value ?? "";
            var devConfig = doc.Descendants(ns + "StrideDevConfiguration").FirstOrDefault()?.Value ?? "Debug";

            foreach (var reference in doc.Descendants(ns + "Reference"))
            {
                var include = reference.Attribute("Include")?.Value;
                if (!string.Equals(include, assemblyName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var hintPath = reference.Element(ns + "HintPath")?.Value;
                if (hintPath == null)
                    continue;

                hintPath = hintPath
                    .Replace("$(StrideDevRoot)", devRoot)
                    .Replace("$(StrideDevConfiguration)", devConfig);

                if (hintPath.Contains("$(StrideGraphicsApi)"))
                {
                    foreach (var api in GraphicsApiProbeOrder)
                    {
                        var candidate = hintPath.Replace("$(StrideGraphicsApi)", api);
                        if (File.Exists(candidate))
                            return candidate;
                    }
                    return null;
                }

                if (File.Exists(hintPath))
                    return hintPath;
            }
        }
        catch
        {
            // Ignore malformed props files
        }

        return null;
    }

    /// <summary>
    /// The load context of an assembly a container loads, and of the dependencies it brings that the host does not
    /// provide. Not collectible: an assembly loaded again (a reload) gets a new context next to the old one.
    /// </summary>
    /// <remarks>
    /// A reference resolves to the container's current assembly of that name, whatever context holds it; then to the
    /// default context (the host's copy wins); then <see cref="AssemblyLoadContext.Resolving"/> loads it in this context
    /// from the package's files. Nothing throws until every step failed.
    /// </remarks>
    private sealed class ContainerLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyContainer container;
        private readonly string primaryPath;

        public ContainerLoadContext(AssemblyContainer container, string primaryPath)
            : base(Path.GetFileName(primaryPath))
        {
            this.container = container;
            this.primaryPath = primaryPath;
            Resolving += OnResolving;

            try
            {
                // Asked by simple name, so that the host's copy wins whatever its version
                PrimaryName = new AssemblyName(AssemblyName.GetAssemblyName(primaryPath).Name!);
            }
            catch (Exception)
            {
                // Not a managed assembly: loaded from its file, and fails there with the actual error
            }
        }

        /// <summary>The assembly this context was created for, or null when its file has no readable name.</summary>
        public AssemblyName? PrimaryName { get; }

        public AssemblyContainer Container => container;

        protected override Assembly? Load(AssemblyName assemblyName)
            => IsPrimary(assemblyName) ? null : container.FindLoadedAssembly(assemblyName);

        private Assembly? OnResolving(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            var path = IsPrimary(assemblyName) ? primaryPath : container.FindDependencyPath(assemblyName, primaryPath);
            return path != null ? container.LoadAssemblyFromPathInternal(path, this) : null;
        }

        private bool IsPrimary(AssemblyName assemblyName)
            => string.Equals(assemblyName.Name, PrimaryName?.Name, StringComparison.OrdinalIgnoreCase);
    }
}
