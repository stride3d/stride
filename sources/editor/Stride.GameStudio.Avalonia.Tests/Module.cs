// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Reflection;

namespace Stride.GameStudio.Avalonia.Tests;

internal static class Module
{
    /// <summary>
    /// The settings and the most recently used sessions the tests write, away from the user's own.
    /// </summary>
    internal static readonly string ConfigDirectory = Path.Combine(Path.GetTempPath(), "Stride.GameStudio.Avalonia.Tests", "config-" + Environment.ProcessId);

    // Stride's attribute (not System.Runtime.CompilerServices'): the generated initializer runs the scan index registration first
    [Stride.Core.ModuleInitializer]
    internal static void Initialize()
    {
        // EditorPath builds the settings paths from the application data folder, which is XDG_CONFIG_HOME on Linux and
        // %APPDATA% on Windows; both are read when the path is asked for
        Directory.CreateDirectory(ConfigDirectory);
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", ConfigDirectory);
        Environment.SetEnvironmentVariable("APPDATA", ConfigDirectory);

        // The test asset types must be known to load the saved packages
        AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);

        // What the desktop host would have parsed from the command line: no thumbnails and no previews, there is no graphics device
        App.Options = new StartupOptions { EnableThumbnails = false, DisablePreview = true };

        // The session loads the projects of the solution with MSBuild, like in the editor
        PackageSessionPublicHelper.FindAndSetMSBuildVersion();
    }
}
