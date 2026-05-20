using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
        var tool = UPath.Combine(asmDir, new UFile($"{toolName}{exeExt}"));
        if (File.Exists(tool))
            return tool;

        tool = UPath.Combine(asmDir, new UFile($"../../content/{toolName}{exeExt}"));
        if (File.Exists(tool))
            return tool;

        return null;
    }
}
