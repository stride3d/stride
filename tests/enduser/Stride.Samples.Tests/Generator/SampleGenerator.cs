// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Stride.Assets;
using Stride.Assets.Templates;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Samples.Generator;

/// <summary>
/// Regenerates a sample from its template GUID into <paramref name="outputPath"/>. Returns the
/// resulting <see cref="PackageSession"/> so callers can inspect or build the generated projects.
/// Caller is responsible for arranging the MSBuild environment via
/// <c>PackageSessionPublicHelper.FindAndSetMSBuildVersion()</c> and registering project
/// templates via <c>DotNetNewTemplateBridge.RegisterProjectTemplates()</c> before invoking this.
/// </summary>
public static class SampleGenerator
{
    private const int RestoreTimeoutSeconds = 600;
    private const int UpgradeTimeoutSeconds = 900;

    public static PackageSession Generate(UDirectory outputPath, Guid templateGuid, string sampleName, LoggerResult logger)
    {
        if (!outputPath.IsAbsolute)
            outputPath = UPath.Combine(Environment.CurrentDirectory, outputPath);

        Console.WriteLine($"Bootstrapping: {sampleName}");

        var session = new PackageSession();
        var generator = new DotNetNewTemplateGenerator();

        logger.MessageLogged += (sender, eventArgs) => Console.WriteLine(eventArgs.Message.Text);

        // Pick the platform variant matching the host OS so the regenerated sample's project
        // layout (and the runner's exe lookup) match what we can actually build/run here.
        var platform = OperatingSystem.IsLinux() ? "linux"
                     : OperatingSystem.IsMacOS() ? "macos"
                     : "windows";
        var parameters = new SessionTemplateGeneratorParameters { Session = session, Unattended = true };
        DotNetNewTemplateGenerator.SetParameters(parameters, new Dictionary<string, string>
        {
            ["platforms"] = platform,
        });

        session.SolutionPath = UPath.Combine<UFile>(outputPath, sampleName + ".slnx");

        if (Directory.Exists(outputPath))
        {
            try
            {
                Directory.Delete(outputPath, recursive: true);
            }
            catch (Exception)
            {
                logger.Warning($"Unable to delete directory [{outputPath}]");
            }
        }

        var strideTemplates = TemplateManager.FindTemplates(session);

        parameters.Description = strideTemplates.First(x => x.Id == templateGuid);
        parameters.Name = sampleName;
        parameters.Namespace = sampleName;
        parameters.OutputDirectory = outputPath;
        parameters.Logger = logger;

        if (!generator.PrepareForRun(parameters).Result)
            logger.Error("PrepareForRun returned false");

        // Headless: only the file output is needed (sample is built + run as a subprocess);
        // skipping IntegrateIntoSession avoids loading per-template Stride.Particles/etc. assemblies
        // into the AssemblyContainer, which would duplicate-register DataContract aliases across
        // sequential test cases.
        if (!generator.GenerateFilesOnly(parameters))
            logger.Error("GenerateFilesOnly returned false");

        if (logger.HasErrors)
            throw new InvalidOperationException($"Error generating sample {sampleName} from template:\r\n{logger.ToText()}");

        // A sample on the engine's version skips this: the generator already moved it to this build
        if (DotNetNewTemplateGenerator.IsOlderContent(outputPath.ToOSPath()))
            Upgrade(outputPath.ToOSPath(), sampleName, logger);

        return session;
    }

    /// <summary>
    /// Brings a sample written for an older engine to this one with the asset compiler's <c>upgrade</c> verb, in its own
    /// process, as <c>stride new</c> does.
    /// </summary>
    /// <remarks>
    /// The code upgrade needs the packages the sample was written for. A sample on a version that no feed has (a number
    /// that was never released) can't go through it: its references move to this engine as they are.
    /// </remarks>
    private static void Upgrade(string directory, string sampleName, ILogger logger)
    {
        var solution = Directory.EnumerateFiles(directory, "*.slnx", SearchOption.TopDirectoryOnly).Single();
        var log = Path.Combine(directory, "upgrade.log");
        var watch = Stopwatch.StartNew();

        var restore = RunDotnet(["restore", solution, "--nologo"], directory, log, RestoreTimeoutSeconds);
        if (restore.ExitCode != 0)
        {
            if (!restore.Output.Any(IsStridePackageNotFound))
                throw new InvalidOperationException($"The restore of {sampleName} before its upgrade failed (exit code {restore.ExitCode}); see {log}:\n{Tail(restore.Output)}");

            Console.WriteLine($"[{sampleName}] NOT upgraded: no feed has the Stride version it is written for. Its references move to {StridePackageUpgrader.CurrentVersion} as they are.");
            foreach (var project in Directory.EnumerateFiles(directory, "*.csproj", SearchOption.AllDirectories))
                StridePackageUpgrader.UpgradeProjectVersions(project, logger);
            return;
        }

        // The files the upgrade overwrites only just came from the template: nothing to back up
        var upgrade = RunDotnet([FindAssetCompiler(), "upgrade", solution, "--no-backup"], directory, log, UpgradeTimeoutSeconds);
        if (upgrade.ExitCode != 0)
            throw new InvalidOperationException($"The upgrade of {sampleName} to Stride {StridePackageUpgrader.CurrentVersion} failed (exit code {upgrade.ExitCode}); see {log}:\n{Tail(upgrade.Output)}");

        Console.WriteLine($"[{sampleName}] upgraded to Stride {StridePackageUpgrader.CurrentVersion} in {watch.Elapsed.TotalSeconds:F0} s");
    }

    // NU1101: no feed has the package; NU1102: no feed has that version of it
    private static bool IsStridePackageNotFound(string line)
        => (line.Contains("NU1101", StringComparison.Ordinal) || line.Contains("NU1102", StringComparison.Ordinal))
            && line.Contains("Unable to find package Stride", StringComparison.Ordinal);

    private static string Tail(IReadOnlyList<string> lines) => string.Join('\n', lines.Skip(Math.Max(0, lines.Count - 40)));

    /// <summary>
    /// The asset compiler this test project builds with its references, in the same configuration: the cross-platform
    /// build, which <c>net*.0</c> selects.
    /// </summary>
    private static string FindAssetCompiler()
    {
        var configuration = typeof(SampleGenerator).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "Debug";
        for (var root = new DirectoryInfo(AppContext.BaseDirectory); root is not null; root = root.Parent)
        {
            var output = Path.Combine(root.FullName, "sources", "assets", "Stride.AssetCompiler", "bin", configuration);
            if (!Directory.Exists(output))
                continue;
            var compiler = Directory.EnumerateDirectories(output, "net*.0")
                .Select(x => Path.Combine(x, "Stride.AssetCompiler.dll"))
                .FirstOrDefault(File.Exists);
            return compiler ?? throw new FileNotFoundException($"No {configuration} build of the asset compiler under {output}.");
        }
        throw new DirectoryNotFoundException($"No Stride checkout above {AppContext.BaseDirectory}.");
    }

    /// <summary>Runs <c>dotnet</c> with <paramref name="arguments"/>, and appends its output to <paramref name="logPath"/>.</summary>
    private static (int ExitCode, IReadOnlyList<string> Output) RunDotnet(IReadOnlyList<string> arguments, string workingDirectory, string logPath, int timeoutSeconds)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);
        // A node that MSBuild keeps for reuse holds the output streams open, so the wait for their end below would last
        // as long as the node idles. The variable also reaches the builds inside the asset compiler, which an argument does not.
        startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";

        var output = new List<string> { $"> dotnet {string.Join(' ', arguments)}" };
        void Collect(string? line)
        {
            if (line is null)
                return;
            lock (output)
                output.Add(line);
        }

        using var process = Process.Start(startInfo)!;
        process.OutputDataReceived += (_, e) => Collect(e.Data);
        process.ErrorDataReceived += (_, e) => Collect(e.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var exited = process.WaitForExit(TimeSpan.FromSeconds(timeoutSeconds));
        if (exited)
        {
            // Lets the readers of the two streams finish
            process.WaitForExit();
        }
        else
        {
            try { process.Kill(entireProcessTree: true); } catch (Exception e) { Collect($"kill failed: {e.Message}"); }
            Collect($"killed after {timeoutSeconds} s");
        }

        lock (output)
        {
            File.AppendAllLines(logPath, output);
            return (exited ? process.ExitCode : -1, output.ToList());
        }
    }
}
