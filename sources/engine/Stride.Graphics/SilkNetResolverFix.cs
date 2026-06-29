// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Silk.NET.Core.Loader;

namespace Stride.Graphics;

/// <summary>
/// Works around Silk.NET's DefaultPathResolver having a hardcoded list of Linux distro RIDs
/// for native library fallback resolution. Distros not in the list (e.g. Ubuntu) fail to find
/// native libs in runtimes/linux-x64/native/.
/// See docs/build/silk-net-runtimesfolder-issue.md
/// </summary>
internal static class SilkNetResolverFix
{
    private static readonly string LinuxRid = "linux" + RuntimeInformation.OSArchitecture switch
    {
        Architecture.X64 => "-x64",
        Architecture.X86 => "-x86",
        Architecture.Arm64 => "-arm64",
        Architecture.Arm => "-arm",
        _ => ""
    };

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Initialize()
    {
        if (!OperatingSystem.IsLinux())
            return;

        if (PathResolver.Default is DefaultPathResolver defaultResolver)
        {
            // Insert right after BaseDirectoryResolver, before the broken RuntimesFolderResolver
            var index = defaultResolver.Resolvers.IndexOf(DefaultPathResolver.BaseDirectoryResolver);
            defaultResolver.Resolvers.Insert(index >= 0 ? index + 1 : 0, RuntimesFolderResolver);
        }
    }

    /// <summary>
    /// Searches runtimes/linux-{arch}/native/ for the requested library.
    /// </summary>
    private static readonly Func<string, IEnumerable<string>> RuntimesFolderResolver = name =>
    {
        var fileName = Path.GetFileName(name);
        if (string.IsNullOrWhiteSpace(fileName))
            return Enumerable.Empty<string>();

        var runtimesDir = Path.Combine(AppContext.BaseDirectory, "runtimes");
        if (!Directory.Exists(runtimesDir))
            return Enumerable.Empty<string>();

        // Try linux-{arch}: e.g. runtimes/linux-x64/native/
        var candidate = Path.Combine(runtimesDir, LinuxRid, "native", fileName);
        if (File.Exists(candidate))
            return new[] { candidate };

        // Try generic: runtimes/linux/native/
        candidate = Path.Combine(runtimesDir, "linux", "native", fileName);
        if (File.Exists(candidate))
            return new[] { candidate };

        return Enumerable.Empty<string>();
    };
}
