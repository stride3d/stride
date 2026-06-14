// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace Stride.Packaging.Tests;

/// <summary>An extra local package feed plus the Stride.* glob patterns it should serve.</summary>
internal sealed record ExtraFeed(string Key, string Path, params string[] Patterns);

internal static class NuGetConsumerFeed
{
    /// <summary>
    /// nuget.config resolving Stride.* only from the workspace bin/packages feed — no public
    /// fallback for first-party packages, so a missing-from-pack package fails as NU1101
    /// instead of silently resolving a public version. Mirrors the workspace's own
    /// packageSourceMapping (Stride.Dependencies.* etc. still flow from nuget.org). Extra
    /// feeds (e.g. a freshly packed plugin) are mapped to their own patterns.
    /// </summary>
    public static void WriteStrictNuGetConfig(string consumerDir, IReadOnlyList<ExtraFeed>? extraFeeds = null)
    {
        var binPackages = TestEnvironment.BinPackages();

        var sources = new StringBuilder();
        var mappings = new StringBuilder();
        if (extraFeeds is not null)
        {
            foreach (var feed in extraFeeds)
            {
                sources.AppendLine($"""    <add key="{feed.Key}" value="{feed.Path}" />""");
                mappings.AppendLine($"""    <packageSource key="{feed.Key}">""");
                foreach (var pattern in feed.Patterns)
                    mappings.AppendLine($"""      <package pattern="{pattern}" />""");
                mappings.AppendLine("""    </packageSource>""");
            }
        }

        File.WriteAllText(Path.Combine(consumerDir, "nuget.config"), $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                <add key="stride-local" value="{binPackages}" />
            {sources.ToString().TrimEnd()}
              </packageSources>
              <packageSourceMapping>
                <packageSource key="nuget.org">
                  <package pattern="*" />
                  <package pattern="Stride.GNU.*" />
                  <package pattern="Stride.Mono.*" />
                  <package pattern="Stride.Dependencies.*" />
                  <package pattern="Stride.GraphX.*" />
                  <package pattern="Stride.Metrics" />
                  <package pattern="Stride.QuickGraph" />
                </packageSource>
                <packageSource key="stride-local">
                  <package pattern="Stride" />
                  <package pattern="Stride.*" />
                </packageSource>
            {mappings.ToString().TrimEnd()}
              </packageSourceMapping>
            </configuration>
            """);
    }
}

/// <summary>Exit code plus the captured combined stdout/stderr of a dotnet invocation.</summary>
internal sealed record ExecResult(int ExitCode, string Output);

internal static class Dotnet
{
    /// <summary>Run a <c>dotnet</c> command, streaming output to the test log; returns exit code + captured output.</summary>
    public static ExecResult Exec(IEnumerable<string> args, string workingDir, ITestOutputHelper output, int timeoutMin)
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = workingDir,
        };
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        var captured = new List<string>();
        void Sink(string tag, string? data)
        {
            if (data is null) return;
            lock (captured) captured.Add(data);
            output.WriteLine($"[{tag}] {data}");
        }

        using var proc = Process.Start(psi)!;
        proc.OutputDataReceived += (_, e) => Sink("stdout", e.Data);
        proc.ErrorDataReceived += (_, e) => Sink("stderr", e.Data);
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        if (!proc.WaitForExit(timeoutMin * 60_000))
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException($"`dotnet {string.Join(' ', args)}` timed out after {timeoutMin}min");
        }
        proc.WaitForExit(); // flush async readers
        string text;
        lock (captured) text = string.Join('\n', captured);
        return new ExecResult(proc.ExitCode, text);
    }
}

internal static class ConsumerBuild
{
    /// <summary>Run <c>dotnet build &lt;project&gt;</c> as an external consumer; streams output, returns exit code + output.</summary>
    public static ExecResult Run(string project, string workingDir, ITestOutputHelper output, int timeoutMin, params string[] extraArgs)
    {
        var args = new List<string> { "build", project, "-c", "Debug", "-nr:false", "-v:m" };
        args.AddRange(extraArgs);
        return Dotnet.Exec(args, workingDir, output, timeoutMin);
    }
}
