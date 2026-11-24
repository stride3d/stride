// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Stride.Packaging.Tests;

/// <summary>An extra local package feed plus the Stride.* glob patterns it should serve.</summary>
internal sealed record ExtraFeed(string Key, string Path, params string[] Patterns);

internal static class NuGetConsumerFeed
{
    /// <summary>
    /// nuget.config resolving Stride.* only from the workspace bin/packages feed, with no public
    /// fallback for first-party packages, so a missing-from-pack package fails as NU1101
    /// instead of silently resolving a public version. It takes the packageSourceMapping of the repository's
    /// nuget.config: Stride.Dependencies.* and the like come from nuget.org, except the unpublished packages that CI maps to bin/packages.
    /// Extra feeds (e.g. a freshly packed plugin) are mapped to their own patterns. The packages go to the case's own
    /// folder, the engine's come from the shared one (<see cref="TestEnvironment.SharedPackagesFolder"/>).
    /// </summary>
    public static void WriteStrictNuGetConfig(string consumerDir, IReadOnlyList<ExtraFeed>? extraFeeds = null)
        => WriteStrictNuGetConfig(consumerDir, TestEnvironment.CasePackagesFolder(consumerDir), TestEnvironment.SharedPackagesFolder, extraFeeds);

    /// <summary>The same, with the packages in <paramref name="packagesFolder"/> and no other fallback folder than <paramref name="fallbackFolder"/>.</summary>
    /// <remarks>The companion packages a session restores go to that folder too: the session reads this nuget.config.</remarks>
    public static void WriteStrictNuGetConfig(string consumerDir, string packagesFolder, string? fallbackFolder, IReadOnlyList<ExtraFeed>? extraFeeds = null)
    {
        static XElement Add(string key, string value) => new("add", new XAttribute("key", key), new XAttribute("value", value));

        // The mapping of the repository names the sources nuget.org and stride-local, the keys of the sources below
        var mapping = new XElement(XDocument.Load(Path.Combine(TestEnvironment.WorktreeRoot(), "nuget.config")).Root!.Element("packageSourceMapping")!);
        var sources = new XElement("packageSources",
            new XElement("clear"),
            Add("nuget.org", "https://api.nuget.org/v3/index.json"),
            Add("stride-local", TestEnvironment.BinPackages()));
        foreach (var feed in extraFeeds ?? [])
        {
            sources.Add(Add(feed.Key, feed.Path));
            mapping.Add(new XElement("packageSource", new XAttribute("key", feed.Key),
                feed.Patterns.Select(pattern => new XElement("package", new XAttribute("pattern", pattern)))));
        }

        new XDocument(new XElement("configuration",
            new XElement("config", Add("globalPackagesFolder", packagesFolder)),
            new XElement("fallbackPackageFolders",
                new XElement("clear"),
                fallbackFolder is not null ? Add("shared", fallbackFolder) : null),
            sources,
            mapping)).Save(Path.Combine(consumerDir, "nuget.config"));
    }
}

/// <summary>Exit code plus the captured combined stdout/stderr of a dotnet invocation.</summary>
internal sealed record ExecResult(int ExitCode, string Output);

internal static class Dotnet
{
    /// <summary>
    /// Run a <c>dotnet</c> command in a case's temp tree, streaming output to the test log; returns exit code + captured output.
    /// It runs on the SDK the repo's global.json selects, and the asset compilers it starts hash files into the case's own
    /// file version cache, not the machine's.
    /// </summary>
    public static ExecResult Exec(IEnumerable<string> args, string workingDir, ITestOutputHelper output, int timeoutMin)
    {
        var caseDir = TestEnvironment.CaseDirectory(workingDir);
        TestEnvironment.WriteGlobalJson(caseDir);
        var psi = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = workingDir,
        };
        // The test process holds the paths of the SDK that runs it (from `dotnet test`, and from FindAndSetMSBuildVersion):
        // a child build would load parts of that SDK into the one it runs on
        psi.Environment.Remove("MSBUILD_EXE_PATH");
        psi.Environment.Remove("MSBuildExtensionsPath");
        psi.Environment.Remove("MSBuildSDKsPath");
        psi.Environment["STRIDE_FILE_VERSION_CACHE"] = Path.Combine(caseDir, "FileVersionTracker.cache");
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
        // A cancelled run stops the process with its children, and the test
        using var stop = TestContext.Current.CancellationToken.Register(() => { try { proc.Kill(entireProcessTree: true); } catch { } });

        if (!proc.WaitForExit(timeoutMin * 60_000))
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException($"`dotnet {string.Join(' ', args)}` timed out after {timeoutMin}min");
        }
        proc.WaitForExit(); // flush async readers
        TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
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
