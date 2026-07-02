// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Stride.VisualStudio.Commands;

/// <summary>
/// Runs the bundled <c>stride</c> CLI (the version-matched Stride tools front-end) as a child process.
/// The extension talks to it only through its command line and exit code, so the CLI owns all
/// version-specific launch/upgrade logic.
/// </summary>
internal static class StrideCli
{
    /// <summary>Outcome of a <c>stride</c> invocation.</summary>
    internal readonly record struct Result(bool Launched, bool RuntimeMissing, int ExitCode, string StandardOutput, string StandardError)
    {
        public bool Succeeded => Launched && !RuntimeMissing && ExitCode == 0;
    }

    /// <summary>Full path to the bundled <c>stride.exe</c>, or null if it isn't next to the extension.</summary>
    internal static string? Locate()
    {
        var extensionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(extensionDirectory))
            return null;

        var path = Path.Combine(extensionDirectory, "tools", "stride", "stride.exe");
        return File.Exists(path) ? path : null;
    }

    /// <summary>
    /// Runs the bundled <c>stride</c> with <paramref name="arguments"/>, capturing its output and, when
    /// <paramref name="onOutputLine"/> is given, forwarding each stdout/stderr line as it arrives (e.g. to an
    /// output window). Sets <see cref="Result.RuntimeMissing"/> when the CLI can't find its .NET runtime.
    /// </summary>
    internal static async Task<Result> RunAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken,
        Func<string, Task>? onOutputLine = null)
    {
        var stride = Locate();
        if (stride is null)
            return new Result(Launched: false, RuntimeMissing: false, ExitCode: -1, string.Empty, string.Empty);

        var startInfo = new ProcessStartInfo(stride)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // The CLI is a console app; without this it flashes a console window when launched from the host.
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory,
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);
        // The net8 host exports DOTNET_ROOT*/DOTNET_HOST_PATH; a net10 apphost that inherits them resolves the
        // wrong runtime and fails with "install .NET". Strip them so the CLI uses the machine install.
        foreach (var variable in new[] { "DOTNET_ROOT", "DOTNET_ROOT(x86)", "DOTNET_ROOT_X64", "DOTNET_ROOT_ARM64", "DOTNET_HOST_PATH" })
            startInfo.Environment.Remove(variable);

        Process process;
        try
        {
            process = Process.Start(startInfo)!;
        }
        catch (Win32Exception)
        {
            // No .NET runtime for the apphost to launch at all.
            return new Result(Launched: false, RuntimeMissing: true, ExitCode: -1, string.Empty, string.Empty);
        }

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        // stdout and stderr are pumped concurrently; serialize the forwarding so lines don't interleave mid-write.
        var forwardLock = new SemaphoreSlim(1, 1);

        async Task PumpAsync(TextReader reader, StringBuilder sink)
        {
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                sink.AppendLine(line);
                if (onOutputLine is not null)
                {
                    await forwardLock.WaitAsync(cancellationToken);
                    try { await onOutputLine(line); }
                    finally { forwardLock.Release(); }
                }
            }
        }

        await Task.WhenAll(
            PumpAsync(process.StandardOutput, stdout),
            PumpAsync(process.StandardError, stderr));
        await process.WaitForExitAsync(cancellationToken);

        var stderrText = stderr.ToString();
        // A framework-dependent apphost that launches but can't find its target framework prints the
        // "install/update .NET" banner and exits non-zero; treat that as a missing runtime too.
        var runtimeMissing = process.ExitCode != 0
            && (stderrText.Contains("Microsoft.NETCore.App", StringComparison.Ordinal)
                || stderrText.Contains("You must install or update .NET", StringComparison.OrdinalIgnoreCase));

        return new Result(Launched: true, runtimeMissing, process.ExitCode, stdout.ToString(), stderrText);
    }
}
