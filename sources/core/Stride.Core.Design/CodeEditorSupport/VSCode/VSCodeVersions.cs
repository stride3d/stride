// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.CodeEditorSupport.VSCode;

public static class VSCodeVersions
{
    private static readonly Lazy<List<IDEInfo>> IDEInfos = new(BuildIDEInfos());
    public static List<IDEInfo> AvailableInstances => IDEInfos.Value;

    private static List<IDEInfo> BuildIDEInfos()
    {
        List<IDEInfo> instances = [];
        var pathDirectories = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];

        foreach (var directory in pathDirectories)
        {
            var vscodiumExe = Path.Combine(directory, "codium");
            var vscodeExe = Path.Combine(directory, "code");
            if (OperatingSystem.IsWindows())
            {
                vscodiumExe += ".cmd";
                vscodeExe += ".cmd";
            }
            
            if (File.Exists(vscodiumExe))
                instances.Add(new IDEInfo("VS Codium", vscodiumExe, IDEType.VSCode));
            
            if (File.Exists(vscodeExe))
                instances.Add(new IDEInfo("VS Code", vscodeExe, IDEType.VSCode));
            
        }

        return instances;
    }
}
