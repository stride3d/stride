using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Stride.Core.IO;

namespace Stride.Assets;

static class ToolLocator
{
    public static UFile LocateTool(string toolName)
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
                    return new UFile(toolLocation);
            }
        }

        var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        // RID-keyed bundled binary: runtimes/<rid>/native/<tool>{.exe}. This is where
        // Stride.Assets.csproj copies the per-platform ffmpeg CLIs.
        var rid = GetCurrentRid();
        if (rid != null)
        {
            var ridTool = UPath.Combine(asmDir, new UFile($"runtimes/{rid}/native/{toolName}{exeExt}"));
            if (File.Exists(ridTool))
                return ridTool;
        }

        // Legacy locations kept for backward compatibility with older deps/ layouts.
        var tool = UPath.Combine(asmDir, new UFile($"{toolName}{exeExt}"));
        if (File.Exists(tool))
            return tool;

        tool = UPath.Combine(asmDir, new UFile($"../../content/{toolName}{exeExt}"));
        if (File.Exists(tool))
            return tool;

        return null;
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
