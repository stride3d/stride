// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Diagnostics;
using Stride.Core.Assets;

internal static class Tools
{
    // Runs a located tool, forwarding arguments. When wait is true (Asset Compiler) the console is inherited
    // and the tool's exit code returned; otherwise (Game Studio) it is launched detached. The tool loads the
    // session's assemblies, so given a session path it runs on the .NET major that session needs. An editor
    // reads the host options itself (toolReadsHostArgs): only one that understands them gets them, and it is told
    // it was re-executed; the compiler is simply started on the right major.
    public static int Run(string? executable, string description, IReadOnlyList<string> forwardedArgs, bool wait, string? sessionPath = null, bool toolReadsHostArgs = true)
    {
        if (executable is null)
        {
            Console.Error.WriteLine($"Could not find {description}.");
            return 1;
        }

        var appDll = Path.ChangeExtension(executable, ".dll");
        var args = forwardedArgs.ToList();
        var hostAware = !toolReadsHostArgs || DotNetHostSelector.SupportsHostSelection(appDll);
        ProcessStartInfo startInfo;
        if (hostAware)
        {
            var decision = DotNetHostSelector.ResolveFor(appDll, DotNetHostSelector.ReadNativeMajor(appDll), sessionPath, args);
            if (decision.Kind == DotNetHostSelector.DecisionKind.Missing)
            {
                Console.Error.WriteLine(decision.Reason);
                return 1;
            }
            startInfo = decision.Kind == DotNetHostSelector.DecisionKind.Relaunch
                ? DotNetHostSelector.RelaunchStartInfo(appDll, decision.Major, args, decision.Install, marker: toolReadsHostArgs)
                : DotNetHostSelector.NativeStartInfo(appDll, args);
        }
        else
        {
            // An older editor would take the option for a session path.
            var index = args.IndexOf(DotNetHostSelector.FrameworkArg);
            if (index >= 0)
                args.RemoveRange(index, Math.Min(2, args.Count - index));
            startInfo = DotNetHostSelector.NativeStartInfo(appDll, args);
        }
        // A detached editor must not share this console: dotnet.exe is a console program and would be tied to it.
        startInfo.CreateNoWindow = !wait;

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
