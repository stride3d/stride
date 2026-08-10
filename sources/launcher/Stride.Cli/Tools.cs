// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Diagnostics;

internal static class Tools
{
    // Runs a located tool, forwarding arguments. When wait is true (Asset Compiler) the console is inherited
    // and the tool's exit code returned; otherwise (Game Studio) it is launched detached.
    public static int Run(string? executable, string description, IReadOnlyList<string> forwardedArgs, bool wait)
    {
        if (executable is null)
        {
            Console.Error.WriteLine($"Could not find {description}.");
            return 1;
        }

        // Run a managed .dll via the shared `dotnet` host (cross-platform); run a native apphost (.exe) directly.
        ProcessStartInfo startInfo;
        if (executable.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            startInfo = new ProcessStartInfo("dotnet") { UseShellExecute = false };
            startInfo.ArgumentList.Add(executable);
        }
        else
        {
            startInfo = new ProcessStartInfo(executable) { UseShellExecute = false };
        }

        foreach (var argument in forwardedArgs)
            startInfo.ArgumentList.Add(argument);

        var process = Process.Start(startInfo);
        if (process is null)
        {
            Console.Error.WriteLine($"Failed to start {executable}.");
            return 1;
        }

        if (!wait)
            return 0;

        process.WaitForExit();
        return process.ExitCode;
    }
}
