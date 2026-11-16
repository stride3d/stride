// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;

namespace Stride.Core.Presentation.Avalonia.Services;

/// <summary>
/// Binds the native library of HarfBuzzSharp to its own functions on Linux, before anything loads the system HarfBuzz.
/// </summary>
/// <remarks>
/// libHarfBuzzSharp.so calls its own hb_* functions through the PLT (it isn't linked with -Bsymbolic) and binds them lazily.
/// FreeType 2.14 loads the system libharfbuzz.so.0 into the global scope when it opens a font file, as fontconfig does to build
/// its cache: the calls not bound yet then land in the system copy, which frees memory of the other one and aborts the process
/// ("free(): invalid pointer"). Loaded with RTLD_NOW first, every call binds to libHarfBuzzSharp itself, and the later load by
/// the runtime gets the same handle. Remove once HarfBuzzSharp links with -Bsymbolic (https://github.com/mono/SkiaSharp/issues/3038).
/// </remarks>
public static class HarfBuzzBinding
{
    private const string LibraryName = "libHarfBuzzSharp";
    private const string LibraryFileName = LibraryName + ".so";
    private const int RTLD_NOW = 2;

    /// <summary>
    /// Loads libHarfBuzzSharp.so with every symbol bound now; nothing off Linux.
    /// </summary>
    /// <remarks>
    /// Call it first in <c>Main</c>, before anything loads Skia or fontconfig: once the system HarfBuzz is in the global scope,
    /// binding at load (even with LD_BIND_NOW) binds to it.
    /// </remarks>
    public static void BindNow()
    {
        if (!OperatingSystem.IsLinux())
            return;
        var path = FindRuntimePath();
        try
        {
            // Resolved before dlopen: the runtime resolves a P/Invoke at its first call, which clears the error dlopen leaves
            dlerror();
            // Kept loaded for the whole process, as the runtime keeps the libraries of P/Invoke
            if (dlopen(path, RTLD_NOW) == 0)
                Console.Error.WriteLine($"{path} could not be loaded with its symbols bound: {Marshal.PtrToStringUTF8(dlerror())}");
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            Console.Error.WriteLine($"{path} could not be loaded with its symbols bound: {e.Message}");
        }
    }

    // The file loaded later for [DllImport("libHarfBuzzSharp")]: the first native search directory of the app that holds it (an
    // app run from its own folder), else the one Stride's NuGet resolver registered, which Game Studio's import resolver loads
    // (Game Studio run from its packages), else the bare name, which the dynamic loader looks up as the runtime then does
    private static string FindRuntimePath()
    {
        var directories = AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES") as string;
        foreach (var directory in directories?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries) ?? [])
        {
            var path = Path.Combine(directory, LibraryFileName);
            if (File.Exists(path))
                return path;
        }
        try
        {
            // Full path: dlopen doesn't look in the current directory, where LocateLibrary can find it
            return Path.GetFullPath(NativeLibraryHelper.LocateLibrary(LibraryName, typeof(HarfBuzzBinding)));
        }
        catch (FileNotFoundException)
        {
            return LibraryFileName;
        }
    }

    [DllImport("libc.so.6")]
    private static extern nint dlopen(string path, int flags);

    [DllImport("libc.so.6")]
    private static extern nint dlerror();
}
