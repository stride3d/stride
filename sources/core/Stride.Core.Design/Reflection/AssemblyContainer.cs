// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyModel;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

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
    private static AssemblyContainer currentContainer;

    [ThreadStatic]
    private static LoggerResult log;

    [ThreadStatic]
    private static string? currentSearchDirectory;

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

        log = new LoggerResult();

        assemblyFullPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, assemblyFullPath));
        var assemblyDirectory = Path.GetDirectoryName(assemblyFullPath);

        if (assemblyDirectory == null || !Directory.Exists(assemblyDirectory))
        {
            throw new ArgumentException("Invalid assembly path. Doesn't contain directory information");
        }

        try
        {
            return LoadAssemblyFromPathInternal(assemblyFullPath);
        }
        finally
        {
            if (outputLog != null)
            {
                log.CopyTo(outputLog);
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

    [ThreadStatic]
    private static bool resolvingFromHost;

    // Simple names the host does not provide
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> missingHostAssemblies = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The assembly of that simple name when the host application provides it, loaded in the default load context; otherwise null.
    /// </summary>
    /// <remarks>The host's set is fixed after startup, so each missing name is asked only once.</remarks>
    internal static Assembly? TryLoadHostAssembly(string simpleName)
    {
        var loaded = System.Runtime.Loader.AssemblyLoadContext.Default.Assemblies.FirstOrDefault(x => !x.IsDynamic && string.Equals(x.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase));
        if (loaded is not null || missingHostAssemblies.ContainsKey(simpleName))
            return loaded;

        // The host's own resolution only: the containers' resolve handler stays out of it
        resolvingFromHost = true;
        try
        {
            return System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(simpleName));
        }
        catch (FileNotFoundException)
        {
            missingHostAssemblies.TryAdd(simpleName, true);
            return null;
        }
        catch (FileLoadException)
        {
            return null;
        }
        finally
        {
            resolvingFromHost = false;
        }
    }

    private Assembly? LoadAssemblyByName(AssemblyName assemblyName, string searchDirectory)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(assemblyName);
#else
        if (assemblyName is null) throw new ArgumentNullException(nameof(assemblyName));
#endif

        // Note: Do not compare by full name as it is too restricive in regards to versioning.
        // For example consider App -> Foo -> Bar (1.0) with the App referencing a newer version of Bar (2.0)
        // A full name comparison would now compare Bar (1.0) to Bar (2.0) and fail to load.

        // First, check the list of already loaded assemblies
        {
            var matchingAssembly = loadedAssemblies.FirstOrDefault(x => AssemblyName.ReferenceMatchesDefinition(assemblyName, x.Assembly.GetName()));
            if (matchingAssembly != null)
                return matchingAssembly.Assembly;
        }

        // Share an assembly the host has loaded or ships, so its serializers and registries exist once
        {
            var hostAssembly = System.Runtime.Loader.AssemblyLoadContext.Default.Assemblies.FirstOrDefault(x => !x.IsDynamic && AssemblyName.ReferenceMatchesDefinition(assemblyName, x.GetName()));
            if (hostAssembly != null)
                return hostAssembly;

            if (assemblyName.Name is { } simpleName && TryLoadHostAssembly(simpleName) is { } shippedAssembly)
                return shippedAssembly;
        }

        // Second, use .deps.json files (generated by MSBuild when PreserveCompilationContext == true)
        // It ensures that we load the same assemblies as were used while compiling
        foreach (var loadedAssembly in loadedAssemblies)
        {
            var dependencies = loadedAssembly.Dependencies;
            if (dependencies == null)
                continue;

            if (dependencies.TryGetValue(assemblyName.Name!, out var fullPath))
            {
                return LoadAssemblyFromPathInternal(fullPath);
            }
        }

        // Look in search paths
        var assemblyPartialPathList = new List<string>();
        assemblyPartialPathList.AddRange(KnownAssemblyExtensions.Select(knownExtension => assemblyName.Name + knownExtension));
        foreach (var assemblyPartialPath in assemblyPartialPathList)
        {
            var assemblyFullPath = Path.Combine(searchDirectory, assemblyPartialPath);
            if (File.Exists(assemblyFullPath))
            {
                return LoadAssemblyFromPathInternal(assemblyFullPath);
            }
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
                            return LoadAssemblyFromPathInternal(dependency);
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

    private Assembly? LoadAssemblyFromPathInternal(string assemblyFullPath)
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
                    log.Warning($"Assembly [{simpleName}] is already loaded from [{sameName.Path}]; not loading [{assemblyFullPath}]");
                    return sameName.Assembly;
                }

                if (!File.Exists(assemblyFullPath))
                    return null;

                // The assembly loads from bytes, without a location for its module initializer to probe natives from
                RegisterNativeLibraries(assemblyFullPath);

                // Find pdb (if it exists)
                var pdbFullPath = Path.ChangeExtension(assemblyFullPath, ".pdb");
                if (!File.Exists(pdbFullPath))
                    pdbFullPath = null;

                // PreLoad the assembly into memory without locking it
                var assemblyBytes = File.ReadAllBytes(assemblyFullPath);
                var pdbBytes = pdbFullPath != null ? File.ReadAllBytes(pdbFullPath) : null;

                // Load the assembly into the current AppDomain
                Assembly assembly;
                if (new UDirectory(AppDomain.CurrentDomain.BaseDirectory) == new UFile(assemblyFullPath).GetFullDirectory())
                {
                    // If loading from base directory, don't even try to load through byte array, as Assembly.Load will notice there is a "safer" version to load
                    // This happens usually when opening Stride assemblies themselves
                    assembly = Assembly.LoadFrom(assemblyFullPath);
                }
                else
                {
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

                    // TODO: Is using AppDomain would provide more opportunities for unloading?
                    const uint unverifiableExecutableWithFixups = 0x80131019;
                    try
                    {
                        assembly = pdbBytes != null ? Assembly.Load(assemblyBytes, pdbBytes) : Assembly.Load(assemblyBytes);
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

                    // Force assembly resolve with proper name (with proper context)
                    var previousSearchDirectory = currentSearchDirectory;
                    var previousContainer = currentContainer;
                    try
                    {
                        currentContainer = this;
                        currentSearchDirectory = Path.GetDirectoryName(assemblyFullPath);

                        Assembly.Load(assembly.FullName);
                    }
                    finally
                    {
                        currentContainer = previousContainer;
                        currentSearchDirectory = previousSearchDirectory;
                    }
                }

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
            log.Error($"Error while loading assembly reference [{assemblyFullPath}]", exception);
            if (exception is ReflectionTypeLoadException loaderException)
            {
                foreach (var exceptionForType in loaderException.LoaderExceptions)
                {
                    log.Error("Unable to load type. See exception.", exceptionForType);
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

    private static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
    {
        if (resolvingFromHost)
            return null;

        // If it is handled by current thread, then we can handle it here.
        var container = currentContainer;
        string? searchDirectory = currentSearchDirectory;

        // If it's a dependent assembly loaded later, find container and path
        if (container == null && args.RequestingAssembly != null && assemblyToContainers.TryGetValue(args.RequestingAssembly, out var loadedAssembly))
        {
            // Assembly reference requested after initial resolve, we need to setup context temporarily
            container = loadedAssembly.Container;
            searchDirectory = Path.GetDirectoryName(loadedAssembly.Path);
        }

        // Load assembly
        if (container != null)
        {
            var assemblyName = new AssemblyName(args.Name);
            return container.LoadAssemblyByName(assemblyName, searchDirectory ?? string.Empty);
        }

        // Asked from outside any container (e.g. WPF binding a BAML type's generic arguments): use a container's copy
        return FindContainerAssembly(new AssemblyName(args.Name));
    }

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
}
