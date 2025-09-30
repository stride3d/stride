// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using JetBrains.Rider.PathLocator;

namespace Stride.Core.CodeEditorSupport.Rider;

public static class RiderVersions
{
    private static readonly Lazy<List<IDEInfo>> IDEInfos = new(BuildIDEInfos());
    public static List<IDEInfo> AvailableInstances => IDEInfos.Value;

    private static List<IDEInfo> BuildIDEInfos()
    {
        RiderPathLocator pathLocator = new RiderPathLocator(new RiderLocatorEnvironment());
        
        List<IDEInfo> instances = [];
        foreach (var info in pathLocator.GetAllRiderPaths())
        {
            if(info.Path != null)
                instances.Add(new IDEInfo($"Rider {pathLocator.GetBuildNumber(info.Path)}", info.Path, IDEType.Rider, pathLocator.GetBuildNumber(info.Path)));
        }

        return instances;
    }
}
