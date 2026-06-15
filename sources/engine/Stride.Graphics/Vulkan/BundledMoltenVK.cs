// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stride.Graphics;

/// <summary>
/// macOS Vulkan environment bootstrap: pins our bundled MoltenVK ICD, and rewrites
/// brew's validation layer manifest with an absolute library_path so the loader can
/// dlopen it without /opt/homebrew/lib being on dyld's search path.
/// </summary>
internal static class BundledMoltenVK
{
    private const string IcdManifestName = "MoltenVK_icd.json";

    private const string DylibName = "libvulkan.1.dylib";

    private const string ValidationLayerDylib = "libVkLayer_khronos_validation.dylib";

    [ModuleInitializer]
    internal static void AutoInit()
    {
        if (!OperatingSystem.IsMacOS()) return;
        PinBundledIcd();
        PinBrewValidationLayer();
    }

    private static void PinBundledIcd()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VK_DRIVER_FILES"))) return;

        var asmDir = Path.GetDirectoryName(typeof(BundledMoltenVK).Assembly.Location);
        if (string.IsNullOrEmpty(asmDir)) asmDir = AppContext.BaseDirectory;
        // Host tool .exes (CompilerApp etc.) get runtimes/ next to the entry-point dll via
        // transitive copy, not next to Stride.Graphics.dll — search both.
        var rid = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
        var searchDirs = (asmDir == AppContext.BaseDirectory)
            ? new[] { asmDir }
            : new[] { asmDir, AppContext.BaseDirectory };
        // Locate the dylib; we always synthesize the ICD JSON ourselves with an absolute
        // library_path. The bundled JSON's bare "libvulkan.1.dylib" name dlopens via DYLD
        // and finds brew's loader instead of our MoltenVK → infinite-recursion / wrong driver.
        string? dylib = null;
        foreach (var dir in searchDirs)
        {
            foreach (var candidate in new[] { Path.Combine(dir, DylibName), Path.Combine(dir, "runtimes", rid, "native", DylibName) })
                if (File.Exists(candidate)) { dylib = candidate; break; }
            if (dylib is not null) break;
        }
        if (dylib is null) return;

        var tmpDir = Path.Combine(Path.GetTempPath(), "stride-moltenvk");
        Directory.CreateDirectory(tmpDir);
        var manifest = Path.Combine(tmpDir, IcdManifestName);
        var dylibJson = JsonEscape(dylib);
        File.WriteAllText(manifest,
            "{ \"file_format_version\": \"1.0.0\", \"ICD\": { \"library_path\": \"" + dylibJson + "\", \"api_version\": \"1.4.0\", \"is_portability_driver\": true } }");

        SetVkEnv("VK_DRIVER_FILES", manifest);
    }

    private static void PinBrewValidationLayer()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VK_LAYER_PATH"))) return;

        // brew ships the manifest with a bare library_path; the loader's dlopen can't find
        // /opt/homebrew/lib because it isn't on dyld's default search path. Synthesize a
        // replacement with the absolute dylib path and pin via VK_LAYER_PATH.
        var candidates = new[]
        {
            "/opt/homebrew/lib/" + ValidationLayerDylib,
            "/usr/local/lib/" + ValidationLayerDylib,
        };
        string? layerDylib = null;
        foreach (var c in candidates) if (File.Exists(c)) { layerDylib = c; break; }
        if (layerDylib is null) return;

        var tmpDir = Path.Combine(Path.GetTempPath(), "stride-vulkan-layers");
        Directory.CreateDirectory(tmpDir);
        var manifest = Path.Combine(tmpDir, "VkLayer_khronos_validation.json");
        var libJson = JsonEscape(layerDylib);
        File.WriteAllText(manifest,
            "{ \"file_format_version\": \"1.2.0\", \"layer\": { \"name\": \"VK_LAYER_KHRONOS_validation\", \"type\": \"GLOBAL\", \"library_path\": \"" + libJson + "\", \"api_version\": \"1.4.0\", \"implementation_version\": \"1\", \"description\": \"Khronos Validation Layer\" } }");

        SetVkEnv("VK_LAYER_PATH", tmpDir);
    }

    private static string JsonEscape(string s) => s.Replace("\\", "/").Replace("\"", "\\\"");

    // .NET on POSIX only updates the managed env table; libc getenv (used by the Vulkan
    // loader) still sees the old value. Mirror via setenv so the native loader picks it up.
    private static void SetVkEnv(string name, string value)
    {
        Environment.SetEnvironmentVariable(name, value);
        PosixSetEnv(name, value, 1);
    }

    [DllImport("libc", EntryPoint = "setenv")]
    private static extern int PosixSetEnv(string name, string value, int overwrite);
}
#endif
