// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stride.Graphics;

/// <summary>
/// Pins the macOS Vulkan ICD to the MoltenVK we ship under <c>runtimes/osx-arm64/native/</c>,
/// so the LunarG loader (when installed locally / via brew) loads our bundled version instead
/// of whatever <c>molten-vk</c> the system happens to have. Caller-set VK_DRIVER_FILES wins.
/// </summary>
internal static class BundledMoltenVK
{
    private const string ManifestName = "MoltenVK_icd.json";

    private const string DylibName = "libvulkan.1.dylib";

    [ModuleInitializer]
    internal static void AutoInit()
    {
        if (!OperatingSystem.IsMacOS()) return;
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VK_DRIVER_FILES"))) return;

        var asmDir = Path.GetDirectoryName(typeof(BundledMoltenVK).Assembly.Location);
        if (string.IsNullOrEmpty(asmDir)) asmDir = AppContext.BaseDirectory;
        // Stride's build flattens StrideNativeLib next to the assembly rather than under
        // runtimes/<rid>/native/ (so dlopen-by-name works); fall back to the nested layout in
        // case a future build changes that.
        var rid = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
        string? manifest = null;
        foreach (var candidate in new[] { Path.Combine(asmDir, ManifestName), Path.Combine(asmDir, "runtimes", rid, "native", ManifestName) })
            if (File.Exists(candidate)) { manifest = candidate; break; }

        // The shipped ICD JSON only lands next to projects that directly reference Stride.Graphics's
        // StrideNativeLib (e.g. Stride.Graphics.Tests). Transitive consumers (Physics/Engine/UI tests)
        // get the .dylib via .ssdeps propagation but not the .json. Synthesize one in a temp dir
        // pointing at the locally-found dylib so the loader still has an ICD to drive.
        if (manifest is null)
        {
            string? dylib = null;
            foreach (var candidate in new[] { Path.Combine(asmDir, DylibName), Path.Combine(asmDir, "runtimes", rid, "native", DylibName) })
                if (File.Exists(candidate)) { dylib = candidate; break; }
            if (dylib is null) return;
            var tmpDir = Path.Combine(Path.GetTempPath(), "stride-moltenvk");
            Directory.CreateDirectory(tmpDir);
            manifest = Path.Combine(tmpDir, ManifestName);
            var dylibJson = dylib.Replace("\\", "/").Replace("\"", "\\\"");
            File.WriteAllText(manifest,
                "{ \"file_format_version\": \"1.0.0\", \"ICD\": { \"library_path\": \"" + dylibJson + "\", \"api_version\": \"1.4.0\", \"is_portability_driver\": true } }");
        }

        Environment.SetEnvironmentVariable("VK_DRIVER_FILES", manifest);
        // .NET on POSIX only updates the managed env table; libc getenv (used by the Vulkan
        // loader) still sees the old value. Mirror via setenv so the native loader picks it up.
        PosixSetEnv("VK_DRIVER_FILES", manifest, 1);
    }

    [DllImport("libc", EntryPoint = "setenv")]
    private static extern int PosixSetEnv(string name, string value, int overwrite);
}
#endif
