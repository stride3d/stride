using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Stride.Core.IO;

namespace Stride.Assets;

static class ToolLocator
{
    /// <summary>
    /// Finds an executable tool by name. Pass <paramref name="ensureExecutable"/>=true for native
    /// CLI binaries shipped via nupkg — flips the Unix execute bit on Linux/macOS, where pack/restore
    /// drops it.
    /// </summary>
    public static UFile LocateTool(string toolName, bool ensureExecutable = false)
    {
        // Bin-dir lookups use the platform-correct extension so we don't try to exec a
        // Windows .exe on Unix.
        var exeExt = OperatingSystem.IsWindows() ? ".exe" : string.Empty;

        // On non-Windows, prefer system PATH (apt/brew). Falls through to the bin-dir
        // lookup below, which a future Stride.Dependencies.FFmpeg-style NuGet can satisfy.
        if (!OperatingSystem.IsWindows())
        {
            var pathDirectories = Environment.GetEnvironmentVariable("PATH")?.Split(':') ?? Array.Empty<string>();

            foreach (var directory in pathDirectories)
            {
                var toolLocation = Path.Combine(directory, toolName);
                if (File.Exists(toolLocation))
                    return EnsureExecutable(new UFile(toolLocation), ensureExecutable);
            }
        }

        var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        // RID-keyed bundled binary: runtimes/<rid>/native/<tool>{.exe}. Two layouts to cover:
        //  * Source-tree build / single-flat publish — runtimes/ sits alongside the assembly.
        //  * NuGet package cache — assembly is at <pkgRoot>/lib/<tfm>/<dll>, natives at
        //    <pkgRoot>/runtimes/<rid>/native/ (standard NuGet layout, two levels up).
        var rid = GetCurrentRid();
        if (rid != null)
        {
            var nativeRel = new UFile($"runtimes/{rid}/native/{toolName}{exeExt}");
            var ridTool = UPath.Combine(asmDir, nativeRel);
            if (File.Exists(ridTool))
                return EnsureExecutable(ridTool, ensureExecutable);

            var pkgRoot = Path.GetDirectoryName(Path.GetDirectoryName(asmDir));
            if (pkgRoot != null)
            {
                var pkgRidTool = UPath.Combine(pkgRoot, nativeRel);
                if (File.Exists(pkgRidTool))
                    return EnsureExecutable(pkgRidTool, ensureExecutable);
            }
        }

        // Legacy locations kept for backward compatibility with older deps/ layouts.
        var tool = UPath.Combine(asmDir, new UFile($"{toolName}{exeExt}"));
        if (File.Exists(tool))
            return EnsureExecutable(tool, ensureExecutable);

        tool = UPath.Combine(asmDir, new UFile($"../../content/{toolName}{exeExt}"));
        if (File.Exists(tool))
            return EnsureExecutable(tool, ensureExecutable);

        return null;
    }

    private static UFile EnsureExecutable(UFile path, bool ensureExecutable)
    {
        if (!ensureExecutable)
            return path;
        // +x normalization only applies to ELF / Mach-O executables. Skip on any other host
        // (Windows, future targets) — the file mode concept doesn't apply or is irrelevant.
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            return path;
        try
        {
            var osPath = path.ToOSPath();
            var current = File.GetUnixFileMode(osPath);
            const UnixFileMode anyExecute = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
            if ((current & anyExecute) == 0)
                File.SetUnixFileMode(osPath, current | UnixFileMode.UserExecute);
        }
        catch
        {
            // Best-effort; if chmod fails (read-only fs, foreign filesystem with no POSIX attrs,
            // etc.) the subsequent Process.Start will surface a clearer error to the user.
        }
        return path;
    }

    private static string GetCurrentRid()
    {
        string os;
        if (OperatingSystem.IsWindows()) os = "win";
        else if (OperatingSystem.IsLinux()) os = "linux";
        else if (OperatingSystem.IsMacOS()) os = "osx";
        else return null;

        var arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "x86",
            _ => null,
        };
        return arch == null ? null : $"{os}-{arch}";
    }
}
