// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Uncomment the following line to enable debug logging for native library loading
#define DEBUG_NATIVE_LOADING

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Stride.Core;

/// <summary>
///   Provides helper methods for locating, preloading, registering, and unloading
///   native libraries and executables across different platforms.
/// </summary>
/// <remarks>
///   <see cref="NativeLibraryHelper"/> is designed to support scenarios where managed assemblies
///   depend on platform-specific native binaries, such as when distributing .NET assemblies
///   with associated CPU-architecture-specific libraries.
///   It abstracts the complexity of locating and loading these native libraries from
///   various runtime and environment-specific locations, and ensures that libraries are
///   loaded and unloaded in a thread-safe manner.
///   <para/>
///   Methods in this class are particularly useful for applications that need to dynamically resolve
///   and manage native dependencies at runtime, such as plugins or cross-platform frameworks.
/// </remarks>
public static partial class NativeLibraryHelper
{
    // Map of loaded libraries to their handles
    private static readonly Dictionary<string, nint> loadedLibraries = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Lock loadedLibrariesLock = new();

    private static readonly Dictionary<string, string> nativeDependenciesWithoutExtensions = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> nativeDependenciesWithExtensions = new(StringComparer.OrdinalIgnoreCase);

    // The expected library extension for the current platform
    private static readonly string libExtension = Platform.Type switch
    {
        PlatformType.Windows => ".dll",
        PlatformType.Linux => ".so",
        PlatformType.macOS => ".dylib",

        _ => throw new PlatformNotSupportedException()
    };

    // The expected RID platform name for the current platform (used in NuGet runtimes folder)
    private static readonly string platform = Platform.Type switch
    {
        PlatformType.Windows => "win",
        PlatformType.Linux => "linux",
        PlatformType.macOS => "osx",

        _ => throw new PlatformNotSupportedException()
    };

    // The expected RID CPU name for the current platform (used in NuGet runtimes folder)
    private static readonly string cpu = RuntimeInformation.ProcessArchitecture switch
    {
        Architecture.X86 => "x86",
        Architecture.X64 => "x64",
        Architecture.Arm => "ARM",
        Architecture.Arm64 => "arm64",

        _ => throw new PlatformNotSupportedException()
    };

    // The expected folder for native libraries for the current platform and CPU
    private static readonly string platformNativeLibsDir =
        Path.Combine("runtimes", $"{platform}-{cpu}", "native");


    /// <summary>
    ///   Locates the full path to a native executable file by searching known locations and
    ///   runtime-specific directories.
    /// </summary>
    /// <param name="executableName">The name of the executable file to locate, including the extension.</param>
    /// <param name="ownerType">
    ///   The type whose assembly is used to determine runtime-specific search paths for the executable.
    /// </param>
    /// <returns>The full path to the located executable file.</returns>
    /// <exception cref="FileNotFoundException">
    ///   Thrown if the executable file cannot be found in any of the searched locations.
    /// </exception>
    /// <remarks>
    ///   This method is typically used to resolve platform-specific native binaries required by managed code.
    /// </remarks>
    public static string LocateExecutable(string executableName, Type ownerType)
    {
        // NuGet native libraries
        if (nativeDependenciesWithExtensions.TryGetValue(executableName, out string? knownExePath))
            return knownExePath;

        // Try in current path
        if (File.Exists(executableName))
            return executableName;

        // Try runtimes specific path
        if (TryFindLibraryPath(ownerType, executableName, out knownExePath))
            return knownExePath;

        throw new FileNotFoundException($"Could not locate native executable {executableName}");
    }

    /// <summary>
    ///   Try to preload a native library.
    /// </summary>
    /// <param name="libraryName">The name of the library, without the extension.</param>
    /// <param name="ownerType">
    ///   The <see cref="Type"/> whose Assembly location is related to the native library.
    ///   This is needed because <see cref="Assembly.GetCallingAssembly"/> cannot be used,
    ///   as it might be wrong due to optimizations.
    /// </param>
    /// <exception cref="DllNotFoundException">
    ///   The library with name <paramref name="libraryName"/> could not be loaded.
    /// </exception>
    /// <remarks>
    ///   <para>
    ///     Preloading native libraries can be useful when we want to have a .NET assembly
    ///     for the <c>AnyCPU</c> platform, and an associated CPU-specific native library.
    ///   </para>
    ///   <para>
    ///     By preloading the native library, we ensure that it is loaded before any P/Invoke calls,
    ///     so those calls use the already loaded library instead of trying to load it again.
    ///   </para>
    /// </remarks>
    public static void PreloadLibrary(string libraryName, Type ownerType)
    {
#if STRIDE_PLATFORM_DESKTOP
        lock (loadedLibrariesLock)
        {
            // If already loaded, just exit as we want to load it just once
            if (loadedLibraries.ContainsKey(libraryName))
            {
                return;
            }

            // Was the dependency registered beforehand?
            if (nativeDependenciesWithoutExtensions.TryGetValue(libraryName, out string? path) &&
                NativeLibrary.TryLoad(path, out nint knownLibHandle))
            {
                AddLoadedLibrary(libraryName, knownLibHandle);
                return;
            }

            var libraryNameWithExtension = libraryName + libExtension;

            if (Platform.Type != PlatformType.Windows)
            {
                // On Linux / MacOS opening a library without a path will look it up in the global library locations:
                // e.g., /lib/x86_64-linux-gnu, /lib, /usr/lib, etc.
                if (NativeLibrary.TryLoad(libraryNameWithExtension, out nint result))
                {
                    AddLoadedLibrary(libraryName, result);
                    return;
                }
                // Also try with 'lib' prefix common on Linux / MacOS
                else if (!libraryName.StartsWith("lib", StringComparison.Ordinal) &&
                          NativeLibrary.TryLoad("lib" + libraryNameWithExtension, out result))
                {
                    AddLoadedLibrary(libraryName, result);
                    return;
                }
            }

            // Try to load the DLL from the directory where its containing type's assembly is located
            if (TryFindLibraryPath(ownerType, libraryNameWithExtension, out string? libraryFilename))
            {
                if (NativeLibrary.TryLoad(libraryFilename!, out nint result))
                {
                    AddLoadedLibrary(libraryName, result);
                    return;
                }
            }

            // Finally, try the default loading mechanism (https://docs.microsoft.com/en-us/dotnet/core/dependency-loading/loading-unmanaged)
            if (NativeLibrary.TryLoad(libraryName, ownerType.Assembly, searchPath: null, out nint handle))
            {
                AddLoadedLibrary(libraryName, handle);
                return;
            }

            // Attempt to load it from PATH
            bool loaded = TryLoadFromEnvironment(libraryNameWithExtension);

            throw new DllNotFoundException($"Could not locate or load native library {libraryName}");
        }

        //
        // Attempts to load the library from the paths defined in the environment's PATH variable.
        //
        bool TryLoadFromEnvironment(string libraryNameWithExtension)
        {
            var envPaths = Environment.GetEnvironmentVariable("PATH")!.Split(Path.PathSeparator);
            foreach (var pathDir in envPaths)
            {
                var libraryFilePath = Path.Combine(pathDir, libraryNameWithExtension);
                if (NativeLibrary.TryLoad(libraryFilePath, out var result))
                {
                    AddLoadedLibrary(libraryName, result);
                    return true;
                }
            }

            // Not found
            return false;
        }

        //
        // Adds the loaded library to the dictionary and logs the loading event.
        //
        void AddLoadedLibrary(string name, nint handle)
        {
            loadedLibraries.Add(name, handle);
            LogLibraryLoaded(name, handle);
        }
#endif
    }

    /// <summary>
    ///   Attempts to locate the full file path of a native library associated with the specified owner type.
    ///   The search includes several directories relative to the owner's assembly, the current process,
    ///   and the current working directory.
    /// </summary>
    /// <param name="ownerType">
    ///   The type whose assembly location is used as a reference point for searching the library path.
    ///   Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="libraryNameWithExtension">
    ///   The file name of the native library to locate, including its extension
    ///   (for example, <c>"mylib.dll"</c> or <c>"libmylib.so"</c>).
    ///   Cannot be <see langword="null"/> or empty.
    /// </param>
    /// <param name="result">
    ///   When this method returns, contains the full file path to the located library if found;
    ///   otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if the library file is found and its path is assigned to <paramref name="result"/>;
    ///   otherwise, <see langword="false"/>.
    /// </returns>
    private static bool TryFindLibraryPath(Type ownerType, string libraryNameWithExtension,
                                           [NotNullWhen(true)] out string? result)
    {
        string ownerAssemblyDir = Path.GetDirectoryName(ownerType.Assembly.Location) ?? string.Empty;
        string currentExeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? string.Empty;

        result = ProbePath(Path.Combine(ownerAssemblyDir, platformNativeLibsDir)) ??
                 ProbePath(Path.Combine(Environment.CurrentDirectory, platformNativeLibsDir)) ??
                 ProbePath(Path.Combine(currentExeDir, platformNativeLibsDir)) ??
                 // Also try without platform for Windows-only packages (backwards compatible for editor packages)
                 ProbePath(Path.Combine(ownerAssemblyDir, cpu)) ??
                 ProbePath(Path.Combine(Environment.CurrentDirectory, cpu));

        return result is not null;

        //
        // Probes the specified directory for the presence of the native library file.
        //
        string? ProbePath(string path)
        {
            var libraryFilePath = Path.Combine(path, libraryNameWithExtension);
            return File.Exists(libraryFilePath) ? libraryFilePath : null;
        }
    }

    /// <summary>
    ///   Unloads a native library that was loaded previously by <see cref="PreloadLibrary"/>.
    /// </summary>
    /// <param name="libraryName">The name of the library to unload.</param>
    public static void Unload(string libraryName)
    {
#if STRIDE_PLATFORM_DESKTOP
        lock (loadedLibrariesLock)
        {
            if (loadedLibraries.TryGetValue(libraryName, out nint libHandle))
            {
                NativeLibrary.Free(libHandle);
                loadedLibraries.Remove(libraryName);
            }
        }
#endif
    }

    /// <summary>
    ///   Unloads all native libraries that were loaded previously by <see cref="PreloadLibrary"/>.
    /// </summary>
    public static void UnloadAll()
    {
#if STRIDE_PLATFORM_DESKTOP
        lock (loadedLibrariesLock)
        {
            foreach (var libraryItem in loadedLibraries)
            {
                NativeLibrary.Free(libraryItem.Value);
            }
            loadedLibraries.Clear();
        }
#endif
    }

    /// <summary>
    ///   Registers a native library as a dependency.
    /// </summary>
    /// <param name="libraryPath">The full path to the native library.</param>
    /// <exception cref="ArgumentNullException"><paramref name="libraryPath"/> is <see langword="null"/>.</exception>
    public static void RegisterDependency(string libraryPath)
    {
        ArgumentNullException.ThrowIfNull(libraryPath);

        lock (loadedLibrariesLock)
        {
            var libraryNameWithoutExtension = Path.GetFileNameWithoutExtension(libraryPath);
            var libraryNameWithExtension = Path.GetFileName(libraryPath);
            nativeDependenciesWithoutExtensions[libraryNameWithoutExtension] = libraryPath;
            nativeDependenciesWithExtensions[libraryNameWithExtension] = libraryPath;
        }
    }

    #region Debug helpers

    //
    // Logs the loading of a native library in debug mode.
    //
    [Conditional("DEBUG_NATIVE_LOADING")]
    private static void LogLibraryLoaded(string libraryName, nint handle)
    {
#if DEBUG_NATIVE_LOADING
        var libraryPath = GetLibraryPath(handle);
        Debug.WriteLine($"Native library \"{libraryName}\" loaded from \"{libraryPath}\"");
#endif
    }

#if DEBUG && DEBUG_NATIVE_LOADING

    /// <summary>
    ///   Retrieves the file system path of a native library associated with the specified handle,
    ///   if available for the current operating system.
    /// </summary>
    /// <param name="handle">
    ///   A handle to the native library whose file system path is to be retrieved. Must not be zero.
    /// </param>
    /// <returns>
    ///   The file system path of the native library if it can be determined; otherwise, <see langword="null"/>.
    /// </returns>
    /// <remarks>
    ///   This method supports Windows, Linux, and macOS platforms.
    ///   On unsupported platforms or if the handle is zero, the method returns <see langword="null"/>.
    /// </remarks>
    private static string? GetLibraryPath(nint handle)
    {
        if (handle == 0)
            return null;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return GetWindowsPath(handle);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return GetPosixPath(handle);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return GetPosixPath(handle);

        return null;
    }

    /// <summary>
    ///   Attempts to retrieve the file system path of a native library on Windows.
    /// </summary>
    /// <param name="handle">The handle (hMODULE) of the loaded native library.</param>
    /// <returns>
    ///   The file system path of the native library if it can be determined; otherwise, <see langword="null"/>.
    /// </returns>
    private static unsafe string? GetWindowsPath(nint handle)
    {
        // We use max length of UNICODE_STRING, assuming long-path-aware application
        const int LENGTH_FOR_BUFFER = 32_767;

        char[] buffer = ArrayPool<char>.Shared.Rent(LENGTH_FOR_BUFFER);
        uint length;
        fixed (char* pBuffer = buffer)
        {
            length = GetModuleFileNameW(handle, pBuffer, (uint)buffer.Length);
        }

        string? result = length > 0
            ? new string(buffer.AsSpan(0, (int)length))
            : null;

        ArrayPool<char>.Shared.Return(buffer);
        return result;
    }

    // From Windows SDK (libloaderapi.h):
    //   DWORD GetModuleFileNameW(HMODULE hModule, LPWSTR lpFilename, DWORD nSize);
    [LibraryImport("kernel32")]
    private static unsafe partial uint GetModuleFileNameW(nint hModule, char* lpFilename, uint nSize);


    /// <summary>
    ///   Attempts to retrieve the file system path of a native library on POSIX platforms
    ///   (Linux and macOS).
    /// </summary>
    /// <param name="address">The address of the loaded native library.</param>
    /// <returns>
    ///   The file system path of the native library if it can be determined; otherwise, <see langword="null"/>.
    /// </returns>
    private static string? GetPosixPath(IntPtr address)
    {
        if (dladdr(address, out Dl_info info) != 0 &&
            info.dli_fname != IntPtr.Zero)
        {
            return Marshal.PtrToStringAnsi(info.dli_fname);
        }

        return null;
    }

    // From POSIX <dlfcn.h>, valid for Linux and macOS:
    //   int dladdr(void *addr, Dl_info *info);
    [LibraryImport("libdl", EntryPoint = "dladdr")]
    private static partial int dladdr(IntPtr address, out Dl_info info);

    // Struct equivalent to Dl_info in <dlfcn.h>
    [StructLayout(LayoutKind.Sequential)]
    public struct Dl_info
    {
        public IntPtr dli_fname;  // Pathname of shared object
        public IntPtr dli_fbase;  // Base address
        public IntPtr dli_sname;  // Name of nearest symbol
        public IntPtr dli_saddr;  // Address of nearest symbol
    }

#endif

    #endregion
}
