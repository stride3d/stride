// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Stride.Core;

public static class NativeLibraryHelper
{
    private const string UNIX_LIB_PREFIX = "lib";
    private static readonly Dictionary<string, IntPtr> LoadedLibraries = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> NativeDependenciesWithoutExtensions = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> NativeDependenciesWithExtensions = new(StringComparer.OrdinalIgnoreCase);

    public static string LocateExecutable(string executableName, Type owner)
    {
        // Nuget native libraries
        if (NativeDependenciesWithExtensions.TryGetValue(executableName, out var path))
            return path;

        // Try in current path
        if (File.Exists(executableName))
            return executableName;

        // Try runtimes specific path
        if (LocateLibraryInFolders(owner, executableName, out path))
            return path;

        throw new InvalidOperationException($"Could not locate native executable {executableName}");
    }

    /// <summary>
    /// Try to preload the library.
    /// This is useful when we want to have AnyCPU .NET and CPU-specific native code.
    /// </summary>
    /// <param name="libraryName">Name of the library, without the extension.</param>
    /// <param name="owner">Type whose assembly location is related to the native library (we can't use GetCallingAssembly as it might be wrong due to optimizations).</param>
    /// <exception cref="System.InvalidOperationException">Library could not be loaded.</exception>
    public static void PreloadLibrary(string libraryName, Type owner)
    {
#if STRIDE_PLATFORM_DESKTOP
        lock (LoadedLibraries)
        {
            // If already loaded, just exit as we want to load it just once
            if (LoadedLibraries.ContainsKey(libraryName))
            {
                return;
            }

            // Was the dependency registered beforehand?
            {
                if (NativeDependenciesWithoutExtensions.TryGetValue(libraryName, out var path) && NativeLibrary.TryLoad(path, out var result))
                {
                    LoadedLibraries.Add(libraryName, result);
                    return;
                }
            }

            // Try default loading mechanism (https://docs.microsoft.com/en-us/dotnet/core/dependency-loading/loading-unmanaged)
            {
                if (NativeLibrary.TryLoad(libraryName, owner.Assembly, searchPath: null, out var result))
                {
                    LoadedLibraries.Add(libraryName, result);
                    return;
                }
            }

            var extension = Platform.Type switch
            {
                PlatformType.Windows => ".dll",
                PlatformType.Linux => ".so",
                PlatformType.macOS => ".dylib",
                _ => throw new PlatformNotSupportedException(),
            };

            var libraryNameWithExtension = libraryName + extension;

            if (Platform.Type != PlatformType.Windows)
            {
                // on linux/macos opening a library without a path will look it up in the global library locations
                // e.g. /lib/x86_64-linux-gnu, /lib, /usr/lib, etc. 
                if (NativeLibrary.TryLoad(libraryNameWithExtension, out var result))
                {
                    LoadedLibraries.Add(libraryName, result);
                    return;
                }
                else if (!libraryName.StartsWith(UNIX_LIB_PREFIX, StringComparison.Ordinal)
                    && NativeLibrary.TryLoad(UNIX_LIB_PREFIX + libraryNameWithExtension, out result))
                {
                    LoadedLibraries.Add(libraryName, result);
                    return;
                }
            }

            // We are trying to load the dll from a shadow path if it is already registered, otherwise we use it directly from the folder
            {
                if (LocateLibraryInFolders(owner, libraryNameWithExtension, out var libraryFilename))
                {
                    if (NativeLibrary.TryLoad(libraryFilename, out var result))
                    {
                        LoadedLibraries.Add(libraryName, result);
                        return;
                    }
                }
            }

            // Attempt to load it from PATH
            foreach (var p in Environment.GetEnvironmentVariable("PATH")!.Split(Path.PathSeparator))
            {
                var libraryFilename = Path.Combine(p, libraryNameWithExtension);
                if (NativeLibrary.TryLoad(libraryFilename, out var result))
                {
                    LoadedLibraries.Add(libraryName, result);
                    return;
                }
            }

            throw new InvalidOperationException($"Could not locate or load native library {libraryName}");
        }
#endif
    }

    private static bool LocateLibraryInFolders(Type owner, string libraryNameWithExtension, [MaybeNullWhen(false)] out string result)
    {
        var platform = Platform.Type switch
        {
            PlatformType.Windows => "win",
            PlatformType.Linux => "linux",
            PlatformType.macOS => "osx",
            _ => throw new PlatformNotSupportedException(),
        };

        var cpu = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "x86",
            Architecture.X64 => "x64",
            Architecture.Arm => "ARM",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException(),
        };

        var platformNativeLibsFolder = Path.Combine("runtimes", $"{platform}-{cpu}", "native");
        foreach (var libraryPath in new[]
        {
            Path.Combine(Path.GetDirectoryName(owner.GetTypeInfo().Assembly.Location) ?? string.Empty, platformNativeLibsFolder),
            Path.Combine(Environment.CurrentDirectory ?? string.Empty, platformNativeLibsFolder),
            Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? string.Empty, platformNativeLibsFolder),
            // Also try without platform for Windows-only packages (backward compat for editor packages)
            Path.Combine(Path.GetDirectoryName(owner.GetTypeInfo().Assembly.Location) ?? string.Empty, cpu),
            Path.Combine(Environment.CurrentDirectory ?? string.Empty, cpu),
        })
        {
            var libraryFilename = Path.Combine(libraryPath, libraryNameWithExtension);
            if (File.Exists(libraryFilename))
            {
                result = libraryFilename;
                return true;
            }
        }

        result = null;
        return false;
    }

    /// <summary>
    /// UnLoad a specific native dynamic library loaded previously by <see cref="PreloadLibrary" />.
    /// </summary>
    /// <param name="libraryName">Name of the library to unload.</param>
    public static void UnLoad(string libraryName)
    {
#if STRIDE_PLATFORM_DESKTOP
        lock (LoadedLibraries)
        {
            if (LoadedLibraries.TryGetValue(libraryName, out var libHandle))
            {
                NativeLibrary.Free(libHandle);
                LoadedLibraries.Remove(libraryName);
            }
        }
#endif
    }

    /// <summary>
    /// UnLoad all native dynamic library loaded previously by <see cref="PreloadLibrary"/>.
    /// </summary>
    public static void UnLoadAll()
    {
#if STRIDE_PLATFORM_DESKTOP
        lock (LoadedLibraries)
        {
            foreach (var libraryItem in LoadedLibraries)
            {
                NativeLibrary.Free(libraryItem.Value);
            }
            LoadedLibraries.Clear();
        }
#endif
    }

    /// <summary>
    /// Registers a native dependency.
    /// </summary>
    /// <param name="libraryPath">The full path to the native library.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void RegisterDependency(string libraryPath)
    {
        ArgumentNullException.ThrowIfNull(libraryPath);

        lock (LoadedLibraries)
        {
            var libraryNameWithoutExtension = Path.GetFileNameWithoutExtension(libraryPath);
            var libraryNameWithExtension = Path.GetFileName(libraryPath);
            NativeDependenciesWithoutExtensions[libraryNameWithoutExtension] = libraryPath;
            NativeDependenciesWithExtensions[libraryNameWithExtension] = libraryPath;
        }
    }
}
