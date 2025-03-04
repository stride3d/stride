// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace Stride.Core.Assets;

public static partial class NuGetAssemblyResolver
{
    public const string DevSource = @"Stride\NugetDev";
#if STRIDE_NUGET_RESOLVER_UI
    public static string AvaloniaVersion = string.Empty;
#endif

    static bool assembliesResolved;
    static readonly object assembliesLock = new();
    static Dictionary<string, string>? assemblyNameToPath;

    public static void DisableAssemblyResolve()
    {
        assembliesResolved = true;
    }

    /// <summary>
    /// Set up an Assembly resolver which on first missing Assembly runs Nuget resolver over <paramref name="packageName"/>.
    /// </summary>
    /// <param name="packageName">Name of the root package for NuGet resolution.</param>
    /// <param name="packageVersion">Package version.</param>
    public static void SetupNuGet(string targetFramework, string packageName, string packageVersion)
    {
        SetupNuGet([(targetFramework, packageName, packageVersion)]);
    }

    public static void SetupNuGet(List<(string targetFramework, string packageName, string packageVersion)> packagesConfigs)
    {
        if (packagesConfigs.Count == 0) return;

        // Make sure our nuget local store is added to nuget config
        var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var devSourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DevSource);

        while (folder != null)
        {
            if (File.Exists(Path.Combine(folder, @"build\Stride.sln")))
            {
                var settings = Settings.LoadDefaultSettings(null);

                Directory.CreateDirectory(devSourcePath);
                CheckPackageSource(settings, "Stride Dev", devSourcePath);

                settings.SaveToDisk();
                break;
            }
            folder = Path.GetDirectoryName(folder);
        }

        // Note: we perform nuget restore inside the assembly resolver rather than top level module ctor (otherwise it freezes)
        AppDomain.CurrentDomain.AssemblyResolve += (_, eventArgs) =>
        {
            if (!assembliesResolved)
            {
                lock (assembliesLock)
                {
                    // Note: using NuGet will try to recursively resolve NuGet.*.resources.dll, so set assembliesResolved right away so that it bypasses everything
                    assembliesResolved = true;

                    var logger = new Logger();

#if STRIDE_NUGET_RESOLVER_UI
                    var avaloniaLoaded = new TaskCompletionSource();
                    var dialogNotNeeded = new TaskCompletionSource();
                    var dialogClosed = new TaskCompletionSource();

                    // We need to make sure Avalonia has been restored before we can display the UI.
                    const string AvaloniaPackageName = "Avalonia.Desktop";
                    var i = 0;
                    packagesConfigs.Insert(i++, (packagesConfigs[0].targetFramework, "Stride.Core", packagesConfigs[0].packageVersion));
                    packagesConfigs.Insert(i++, (packagesConfigs[0].targetFramework, "Avalonia.Themes.Fluent", AvaloniaVersion));
                    packagesConfigs.Insert(i++, (packagesConfigs[0].targetFramework, AvaloniaPackageName, AvaloniaVersion));

                    var stopwatch = Stopwatch.StartNew();
                    var newWindowThread = new Thread(() =>
                    {
                        avaloniaLoaded.Task.Wait();
                        Thread.Sleep(Math.Max(0, 500 - (int)stopwatch.ElapsedMilliseconds));
                        // Display splash screen after a 500 msec (when NuGet takes some time to restore)
                        if (!dialogNotNeeded.Task.IsCompleted)
                        {
                            ResolverUILauncher.Run(dialogNotNeeded, dialogClosed, logger);
                        }
                    });
                    if (OperatingSystem.IsWindows())
                    {
                        newWindowThread.SetApartmentState(ApartmentState.STA);
                    }
                    newWindowThread.IsBackground = true;
                    newWindowThread.Start();
#endif

                    var previousSynchronizationContext = SynchronizationContext.Current;
                    try
                    {
                        // Since we execute restore synchronously, we don't want any surprise concerning synchronization context (i.e. Avalonia one doesn't work with this)
                        SynchronizationContext.SetSynchronizationContext(null);

                        foreach (var (targetFramework, packageName, packageVersion) in packagesConfigs)
                        {
                            // Parse current TFM
                            var nugetFramework = NuGetFramework.Parse(targetFramework);

                            // Only allow this specific version
                            var versionRange = new VersionRange(new NuGetVersion(packageVersion), true, new NuGetVersion(packageVersion), true);
                            var (request, result) = RestoreHelper.Restore(logger, nugetFramework, RuntimeInformation.RuntimeIdentifier, packageName, versionRange);
                            if (!result.Success)
                            {
                                throw new InvalidOperationException("Could not restore NuGet packages");
                            }

                            // Build list of assemblies
                            var assemblies = RestoreHelper.ListAssemblies(result.LockFile);

                            // Create a dictionary by assembly name
                            // note: we ignore case as filename might not be properly matching assembly name casing
                            assemblyNameToPath ??= new(StringComparer.OrdinalIgnoreCase);
                            foreach (var assembly in assemblies)
                            {
                                var extension = Path.GetExtension(assembly).ToLowerInvariant();
                                if (extension != ".dll")
                                    continue;
                                var assemblyName = Path.GetFileNameWithoutExtension(assembly);
                                // Ignore duplicates (however, make sure it's the same version otherwise display a warning)
                                if (assemblyNameToPath.TryGetValue(assemblyName, out var otherAssembly))
                                {
                                    if (!FileContentIsSame(new FileInfo(otherAssembly), new FileInfo(assembly)))
                                        logger.LogWarning($"Assembly {assemblyName} found in two locations with different content: {assembly} and {otherAssembly}");
                                    continue;
                                }
                                assemblyNameToPath.Add(assemblyName, assembly);
                            }

                            // Register the native libraries
                            var nativeLibs = RestoreHelper.ListNativeLibs(result.LockFile);
                            RegisterNativeDependencies(assemblyNameToPath, nativeLibs);

#if STRIDE_NUGET_RESOLVER_UI
                            if (packageName == AvaloniaPackageName)
                            {
                                LoadNativeDependencies(assemblyNameToPath, nativeLibs);
                                avaloniaLoaded.TrySetResult();
                            }
#endif
                        }
                    }
                    catch (Exception e)
                    {
#if STRIDE_NUGET_RESOLVER_UI
                        logger.LogError($"Error restoring NuGet packages: {e}");
                        dialogClosed.Task.Wait();
#else
                        // Display log in console
                        var logText = $@"Error restoring NuGet packages!

==== Exception details ====

{e}

==== Log ====

{string.Join(Environment.NewLine, logger.Logs.Select(x => $"[{x.Level}] {x.Message}"))}
";
                        Console.WriteLine(logText);
#endif
                        Environment.Exit(1);
                    }
                    finally
                    {
#if STRIDE_NUGET_RESOLVER_UI
                        dialogNotNeeded.TrySetResult();
#endif
                        SynchronizationContext.SetSynchronizationContext(previousSynchronizationContext);
                    }
                }
            }

            if (assemblyNameToPath != null)
            {
                var aname = new AssemblyName(eventArgs.Name);
                if (aname.Name!.StartsWith("Microsoft.Build", StringComparison.Ordinal) && aname.Name != "Microsoft.Build.Locator")
                    return null;
                if (assemblyNameToPath.TryGetValue(aname.Name, out var assemblyPath))
                {
                    return Assembly.LoadFrom(assemblyPath);
                }
            }
            return null;
        };
    }

    static bool FileContentIsSame(FileInfo file1, FileInfo file2)
    {
        if (file1.Length != file2.Length)
            return false;

        // Assume same size and same modified time means it's the same file
        if (file1.LastWriteTimeUtc == file2.LastWriteTimeUtc)
            return true;

        // Otherwise, full file compare
        using var fs1 = file1.OpenRead();
        using var fs2 = file2.OpenRead();
        for (int i = 0; i < file1.Length; i++)
        {
            if (fs1.ReadByte() != fs2.ReadByte())
                return false;
        }
        return true;
    }

    private static void RemoveSources(ISettings settings, string prefixName)
    {
        var packageSources = settings.GetSection("packageSources");
        if (packageSources != null)
        {
            foreach (var packageSource in packageSources.Items.OfType<SourceItem>().ToList())
            {
                if (packageSource.Key.StartsWith(prefixName, StringComparison.Ordinal))
                {
                    // Remove entry from packageSources
                    settings.Remove("packageSources", packageSource);
                }
            }
        }
    }

    private static void CheckPackageSource(ISettings settings, string name, string url)
    {
        settings.AddOrUpdate("packageSources", new SourceItem(name, url));
    }

    /// <summary>
    /// Registers the listed native libs in Stride.Core.NativeLibraryHelper using reflection to avoid a compile time dependency on Stride.Core
    /// </summary>
    private static void RegisterNativeDependencies(Dictionary<string, string> assemblyNameToPath, List<string> nativeLibs)
    {
        var strideCoreAssembly = Assembly.LoadFrom(assemblyNameToPath["Stride.Core"])
            ?? throw new InvalidOperationException("Couldn't find assembly 'Stride.Core' in restored packages");
        var nativeLibraryHelperType = strideCoreAssembly.GetType("Stride.Core.NativeLibraryHelper")
            ?? throw new InvalidOperationException($"Couldn't find type 'Stride.Core.NativeLibraryHelper' in {strideCoreAssembly}");
        var registerDependencyMethod = nativeLibraryHelperType.GetMethod("RegisterDependency")
            ?? throw new InvalidOperationException($"Couldn't find method 'RegisterDependency' in {nativeLibraryHelperType}");
        foreach (var lib in nativeLibs)
            registerDependencyMethod.Invoke(null, [lib]);
    }

    /// <summary>
    /// Loads the listed native libs in Stride.Core.NativeLibraryHelper using reflection to avoid a compile time dependency on Stride.Core
    /// </summary>
    private static void LoadNativeDependencies(Dictionary<string, string> assemblyNameToPath, List<string> nativeLibs)
    {
        var strideCoreAssembly = Assembly.LoadFrom(assemblyNameToPath["Stride.Core"])
            ?? throw new InvalidOperationException("Couldn't find assembly 'Stride.Core' in restored packages");
        var nativeLibraryHelperType = strideCoreAssembly.GetType("Stride.Core.NativeLibraryHelper")
            ?? throw new InvalidOperationException($"Couldn't find type 'Stride.Core.NativeLibraryHelper' in {strideCoreAssembly}");
        var preloadLibraryMethod = nativeLibraryHelperType.GetMethod("PreloadLibrary")
            ?? throw new InvalidOperationException($"Couldn't find method 'PreloadLibrary' in {nativeLibraryHelperType}");
        foreach (var lib in nativeLibs)
        {
            var libName = Path.GetFileNameWithoutExtension(lib);
            preloadLibraryMethod.Invoke(null, [libName, null]);
        }
    }

    public class Logger : ILogger
    {
        private readonly object logLock = new();
        private Action<LogLevel, string>? action;
        public List<(LogLevel Level, string Message)> Logs { get; } = [];

        public void SetupLogAction(Action<LogLevel, string> action)
        {
            lock (logLock)
            {
                this.action = action;
                foreach (var (Level, Message) in Logs)
                    action.Invoke(Level, Message);
            }
        }

        public void LogDebug(string data)
        {
            Log(LogLevel.Debug, data);
        }

        public void LogVerbose(string data)
        {
            Log(LogLevel.Verbose, data);
        }

        public void LogInformation(string data)
        {
            Log(LogLevel.Information, data);
        }

        public void LogMinimal(string data)
        {
            Log(LogLevel.Minimal, data);
        }

        public void LogWarning(string data)
        {
            Log(LogLevel.Warning, data);
        }

        public void LogError(string data)
        {
            Log(LogLevel.Error, data);
        }

        public void LogInformationSummary(string data)
        {
            Log(LogLevel.Information, data);
        }

        public void LogErrorSummary(string data)
        {
            Log(LogLevel.Error, data);
        }

        public void Log(LogLevel level, string data)
        {
            lock (logLock)
            {
                Debug.WriteLine($"[{level}] {data}");
                Logs.Add((level, data));
                action?.Invoke(level, data);
            }
        }

        public Task LogAsync(LogLevel level, string data)
        {
            Log(level, data);
            return Task.CompletedTask;
        }

        public void Log(ILogMessage message)
        {
            Log(message.Level, message.Message);
        }

        public Task LogAsync(ILogMessage message)
        {
            Log(message);
            return Task.CompletedTask;
        }
    }
}
