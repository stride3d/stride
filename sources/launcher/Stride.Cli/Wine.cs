// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.ComponentModel;
using System.Diagnostics;

// Wine, which runs the WPF Game Studio off Windows. Setting up its prefix (Windows .NET, d3dcompiler_47, fonts) is up
// to the user: the CLI only starts `wine <exe>`, as a shell would.
internal static class Wine
{
    private const string Command = "wine";

    // How long winepath may take. The first wine command of a new prefix creates it, which takes a while; a prefix
    // that isn't set up can't run Game Studio anyway, so past this the Z: drive will do.
    private static readonly TimeSpan WinePathTimeout = TimeSpan.FromSeconds(30);

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

    /// <summary>Starts <paramref name="arguments"/> under <paramref name="wine"/>, without Wine's own debug output unless WINEDEBUG says.</summary>
    public static ProcessStartInfo StartInfo(string wine, IEnumerable<string> arguments)
    {
        var startInfo = new ProcessStartInfo(wine) { UseShellExecute = false };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);
        if (Environment.GetEnvironmentVariable("WINEDEBUG") is null)
            startInfo.Environment["WINEDEBUG"] = "-all";
        return startInfo;
    }

    /// <summary>
    ///   The path a program under Wine sees for <paramref name="path"/> (Game Studio takes a Unix path for a missing
    ///   solution): what winepath makes of it with the drives of the prefix, else the path through the Z: drive,
    ///   which a new prefix maps to the root of the file system.
    /// </summary>
    public static string ToWindowsPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (Find() is { } wine && RunWinePath(wine, fullPath) is { } windowsPath)
            return windowsPath;
        return "Z:" + fullPath.Replace('/', '\\');
    }

    // `wine winepath -w <path>`, or null when it fails or takes too long (left to finish: killing wine while it
    // creates a prefix would break it).
    private static string? RunWinePath(string wine, string fullPath)
    {
        try
        {
            var startInfo = StartInfo(wine, ["winepath", "-w", fullPath]);
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            using var process = Process.Start(startInfo);
            if (process is null)
                return null;

            // Drain both streams (async) so the pipe can't deadlock; only the path on stdout is kept.
            var output = process.StandardOutput.ReadToEndAsync();
            _ = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(WinePathTimeout) || process.ExitCode != 0)
                return null;
            var windowsPath = output.Result.Trim();
            return windowsPath.Length > 0 ? windowsPath : null;
        }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException or IOException)
        {
            return null;
        }
    }
}
