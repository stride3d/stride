// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Stride.Assets.FFmpeg;

/// <summary>
/// Finds a command line tool: flat next to this assembly (deployed native dependencies), under
/// runtimes/&lt;rid&gt;/native next to it (source tree), or at the package root (NuGet layout).
/// </summary>
static class ToolLocator
{
    public static string? LocateTool(string toolName, bool ensureExecutable = false)
    {
        var exeExt = OperatingSystem.IsWindows() ? ".exe" : string.Empty;

        // On non-Windows, prefer the system PATH (apt/brew)
        if (!OperatingSystem.IsWindows())
        {
            var pathDirectories = Environment.GetEnvironmentVariable("PATH")?.Split(':') ?? [];
            foreach (var directory in pathDirectories)
            {
                var toolLocation = Path.Combine(directory, toolName);
                if (File.Exists(toolLocation))
                    return EnsureExecutable(toolLocation, ensureExecutable);
            }
        }

        var asmDir = Path.GetDirectoryName(typeof(ToolLocator).Assembly.Location)!;

        // Deployed flat next to the assemblies (the native dependencies of an application or the editor)
        var flatTool = Path.Combine(asmDir, toolName + exeExt);
        if (File.Exists(flatTool))
            return EnsureExecutable(flatTool, ensureExecutable);

        var rid = GetCurrentRid();
        if (rid != null)
        {
            var nativeRel = Path.Combine("runtimes", rid, "native", toolName + exeExt);
            var ridTool = Path.Combine(asmDir, nativeRel);
            if (File.Exists(ridTool))
                return EnsureExecutable(ridTool, ensureExecutable);

            // <pkgRoot>/lib/<tfm>/<dll> -> <pkgRoot>/runtimes/<rid>/native/
            var pkgRoot = Path.GetDirectoryName(Path.GetDirectoryName(asmDir));
            if (pkgRoot != null)
            {
                var pkgRidTool = Path.Combine(pkgRoot, nativeRel);
                if (File.Exists(pkgRidTool))
                    return EnsureExecutable(pkgRidTool, ensureExecutable);
            }
        }

        return null;
    }

    private static string EnsureExecutable(string path, bool ensureExecutable)
    {
        if (!ensureExecutable || (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()))
            return path;
        try
        {
            var current = File.GetUnixFileMode(path);
            const UnixFileMode anyExecute = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
            if ((current & anyExecute) == 0)
                File.SetUnixFileMode(path, current | UnixFileMode.UserExecute);
        }
        catch
        {
            // Best effort: a failed chmod surfaces as a clearer error when the process starts
        }
        return path;
    }

    private static string? GetCurrentRid()
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
