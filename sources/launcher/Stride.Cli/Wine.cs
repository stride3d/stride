// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Wine, which runs the WPF Game Studio off Windows. Setting up its prefix (Windows .NET, d3dcompiler_47, fonts) is up
// to the user: the CLI only starts `wine <exe>`, as a shell would.
internal static class Wine
{
    private const string Command = "wine";

    /// <summary>The wine command on the PATH, or null when there is none (or on Windows, which needs none).</summary>
    public static string? Find()
    {
        if (OperatingSystem.IsWindows())
            return null;

        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory, Command);
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }

    /// <summary>
    ///   The path a program under Wine sees for <paramref name="path"/>: through the Z: drive, which every prefix maps
    ///   to the root of the file system. Game Studio takes a Unix path for a missing solution.
    /// </summary>
    public static string ToWindowsPath(string path)
        => "Z:" + Path.GetFullPath(path).Replace('/', '\\');
}
