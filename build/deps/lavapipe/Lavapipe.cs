using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stride.Dependencies.Lavapipe;

/// <summary>
/// Auto-configures VK_DRIVER_FILES to point at the packaged lavapipe ICD manifest.
/// </summary>
/// <remarks>
/// The packaged <c>lvp_icd.json</c> uses a relative <c>library_path</c>, so the Vulkan
/// loader resolves <c>vulkan_lvp.dll</c>/<c>libvulkan_lvp.so</c>/<c>libvulkan_lvp.dylib</c>
/// against the manifest's own directory — no runtime rewriting needed.
///
/// Resolution order:
///   1. <c>runtimes/&lt;rid&gt;/native/</c> relative to AppContext.BaseDirectory (normal deployment)
///   2. Next to this managed DLL (single-file / flat deployment)
///   3. NuGet package cache (when consumer uses PackageReference ExcludeAssets="runtime")
///
/// Caller can override by pre-setting VK_DRIVER_FILES before the module loads.
/// </remarks>
public static class Lavapipe
{
    private const string ManifestName = "lvp_icd.json";

    [ModuleInitializer]
    internal static void AutoInit()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VK_DRIVER_FILES")))
            return;
        TryConfigure();
    }

    /// <summary>Force configuration. Returns true if VK_DRIVER_FILES was set.</summary>
    public static bool TryConfigure()
    {
        var manifestPath = LocateManifest();
        if (manifestPath is null)
            return false;
        Environment.SetEnvironmentVariable("VK_DRIVER_FILES", manifestPath);
        return true;
    }

    static string? LocateManifest()
    {
        var rid = GetRid();
        var asmLoc = typeof(Lavapipe).Assembly.Location;
        var asmDir = string.IsNullOrEmpty(asmLoc) ? null : Path.GetDirectoryName(asmLoc);

        // In-app candidates (no NuGet cache needed)
        string?[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native", ManifestName),
            Path.Combine(AppContext.BaseDirectory, ManifestName),
            asmDir is null ? null : Path.Combine(asmDir, ManifestName),
            // NuGet-cache layout when this dll is loaded directly from the cache:
            // <cache>/<pkg>/<ver>/lib/net10.0/Stride.Dependencies.Lavapipe.dll
            // → manifest at <cache>/<pkg>/<ver>/runtimes/<rid>/native/<ManifestName>
            asmDir is null ? null : Path.GetFullPath(Path.Combine(asmDir, "..", "..", "runtimes", rid, "native", ManifestName)),
        ];

        foreach (var p in candidates)
        {
            if (!string.IsNullOrEmpty(p) && File.Exists(p))
                return Path.GetFullPath(p);
        }

        // Last-resort: scan the NuGet package cache (handles ExcludeAssets="runtime"
        // where natives are not copied to output and this dll also may not be).
        var nugetRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        var pkgRoot = Path.Combine(nugetRoot, "stride.dependencies.lavapipe");
        if (Directory.Exists(pkgRoot))
        {
            foreach (var verDir in Directory.EnumerateDirectories(pkgRoot).OrderByDescending(d => d))
            {
                var p = Path.Combine(verDir, "runtimes", rid, "native", ManifestName);
                if (File.Exists(p))
                    return p;
            }
        }

        return null;
    }

    static string GetRid()
    {
        if (OperatingSystem.IsWindows())
            return "win-x64";
        if (OperatingSystem.IsLinux())
            return "linux-x64";
        if (OperatingSystem.IsMacOS())
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
        throw new PlatformNotSupportedException();
    }
}
