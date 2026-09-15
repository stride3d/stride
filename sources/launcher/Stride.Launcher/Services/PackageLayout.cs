// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.IO;

namespace Stride.Launcher.Services;

/// <summary>The layout of an installed Stride package: where its executables live.</summary>
internal static class PackageLayout
{
    /// <summary>The tools/&lt;tfm&gt; and lib/&lt;tfm&gt; folders of <paramref name="installPath"/>.</summary>
    public static IEnumerable<string> FrameworkDirectories(string installPath)
    {
        foreach (var topLevel in new[] { "tools", "lib" })
        {
            var directory = Path.Combine(installPath, topLevel);
            if (!Directory.Exists(directory))
                continue;
            foreach (var frameworkDirectory in Directory.EnumerateDirectories(directory))
                yield return frameworkDirectory;
        }
    }
}
