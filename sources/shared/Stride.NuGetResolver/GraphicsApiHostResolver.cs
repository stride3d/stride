// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Stride.Core.Assets;

/// <summary>
/// Local-build counterpart of the NuGet-cache resolver: with <c>StrideMultiGraphicsApiHost=true</c>,
/// graphics-API-dependent assemblies sit in per-API subfolders (e.g. <c>Vulkan/</c>) next to the exe.
/// Loads them from the selected API's subfolder via <see cref="AssemblyLoadContext.Default"/> Resolving.
/// Without an explicit selection, the platform default falls back to a staged API if it isn't there.
/// No-op on a flat build (no such subfolders).
/// </summary>
public static class GraphicsApiHostResolver
{
    static bool initialized;

    /// <param name="explicitApi">API explicitly chosen at launch (see <see cref="GraphicsApiSelector"/>),
    /// or null to use the platform default, falling back to an API the build actually staged.</param>
    /// <returns>The API this process loads.</returns>
    public static string Setup(string? explicitApi)
    {
        var api = explicitApi ?? GraphicsApiSelector.Default;
        if (initialized)
            return api;
        initialized = true;

        // A --graphics-api error was recorded: don't hook, so graphics can't silently load the default.
        if (GraphicsApiSelector.StartupError != null)
            return api;

        var baseDir = AppContext.BaseDirectory;
        var apiDir = Path.Combine(baseDir, api);
        if (!Directory.Exists(apiDir))
        {
            // Selected API's folder is absent: a flat build (no per-API layout — flat DLLs load
            // normally) or the API wasn't built. Tell them apart by whether any subfolder holds the engine.
            var available = Directory.GetDirectories(baseDir)
                .Where(d => File.Exists(Path.Combine(d, "Stride.Graphics.dll")))
                .Select(Path.GetFileName)
                .ToList();
            if (available.Count == 0)
                return api; // flat build

            if (explicitApi != null)
            {
                GraphicsApiSelector.FailStartup($"Graphics API '{explicitApi}' is not available in this build. Available: {string.Join(", ", available)}.");
                return api;
            }

            // Nothing was asked for and the platform default isn't staged (e.g. a Vulkan-only
            // build on Windows): use what the build produced instead.
            api = GraphicsApiSelector.KnownApis.FirstOrDefault(k => available.Contains(k, StringComparer.OrdinalIgnoreCase)) ?? available[0]!;
            apiDir = Path.Combine(baseDir, api);
        }

        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            var candidate = Path.Combine(apiDir, name.Name + ".dll");
            return File.Exists(candidate) ? context.LoadFromAssemblyPath(candidate) : null;
        };

        AssemblyLoadContext.Default.ResolvingUnmanagedDll += (assembly, libraryName) =>
        {
            foreach (var candidate in NativeCandidates(apiDir, libraryName))
            {
                if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out var handle))
                    return handle;
            }
            return IntPtr.Zero;
        };

        return api;
    }

    static string[] NativeCandidates(string apiDir, string libraryName)
    {
        // Probe the API subfolder for the native module under the common OS name variants.
        if (OperatingSystem.IsWindows())
            return [Path.Combine(apiDir, libraryName + ".dll"), Path.Combine(apiDir, libraryName)];
        if (OperatingSystem.IsMacOS())
            return [Path.Combine(apiDir, "lib" + libraryName + ".dylib"), Path.Combine(apiDir, libraryName)];
        return [Path.Combine(apiDir, "lib" + libraryName + ".so"), Path.Combine(apiDir, libraryName)];
    }
}
