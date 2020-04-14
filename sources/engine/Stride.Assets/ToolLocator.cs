using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core.IO;

namespace Xenko.Assets
{
    static class ToolLocator
    {
        public static UFile LocateTool(string toolName)
        {
            var tool = UPath.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), new UFile(toolName));
            if (File.Exists(tool))
                return tool;

            tool = UPath.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), new UFile($"../../content/{toolName}"));
            if (File.Exists(tool))
                return tool;

            return null;
        }
    }
}
