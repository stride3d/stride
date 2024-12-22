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
        if(!OperatingSystem.IsWindows())
        {
            var pathDirectories = Environment.GetEnvironmentVariable("PATH")?.Split(':') ?? Array.Empty<string>();

            foreach (var directory in pathDirectories)
            {
                var toolLocation = Path.Combine(directory, toolName);
                if (File.Exists(toolLocation))
                    return new UFile(toolLocation);
            }
        }

        var tool = UPath.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), new UFile($"{toolName}.exe"));
        if (File.Exists(tool))
            return tool;

        tool = UPath.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), new UFile($"../../content/{toolName}.exe"));
        if (File.Exists(tool))
            return tool;

        return null;
    }
}
