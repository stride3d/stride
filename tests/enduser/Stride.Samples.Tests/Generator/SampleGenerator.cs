// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
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
    private const int UpgradeTimeoutSeconds = 900;
    private const int UpgradeAllTimeoutSeconds = 1800;

    /// <param name="upgrade">Whether to bring a sample written for an older engine to this one; else see <see cref="UpgradeAll"/>.</param>
    public static PackageSession Generate(UDirectory outputPath, Guid templateGuid, string sampleName, LoggerResult logger, bool upgrade = true)
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
        if (upgrade && NeedsUpgrade(outputPath.ToOSPath()))
            Upgrade(outputPath.ToOSPath(), sampleName, logger);

        return session;
    }

    /// <summary>Whether the sample generated in <paramref name="directory"/> is written for an older engine.</summary>
    public static bool NeedsUpgrade(string directory) => DotNetNewTemplateGenerator.IsOlderContent(directory);

    /// <summary>
    /// Brings the samples generated in <paramref name="sampleDirectories"/>, written for an older engine, to this one in
    /// one upgrade of a solution with the projects of all of them. Returns false when it failed, leaving the samples
    /// partly upgraded.
    /// </summary>
    /// <remarks>
    /// One upgrade starts the asset compiler, its MSBuild and Roslyn once, where an upgrade of each sample starts them
    /// again. The samples share no asset id, type or namespace, so each comes out as its own upgrade would leave it.
    /// </remarks>
    public static bool UpgradeAll(string root, IReadOnlyList<string> sampleDirectories)
    {
        // The projects of each sample's own solution
        var solution = Path.Combine(root, "Upgrade.slnx");
        new XDocument(new XElement("Solution", sampleDirectories.SelectMany(directory =>
        {
            var sampleSolution = Directory.EnumerateFiles(directory, "*.slnx", SearchOption.TopDirectoryOnly).Single();
            return XDocument.Load(sampleSolution).Descendants("Project")
                .Select(x => new XElement("Project", new XAttribute("Path", Path.GetFullPath((string)x.Attribute("Path")!, directory))));
        }))).Save(solution);

        var log = Path.Combine(root, "upgrade.log");
        File.Delete(log);
        Console.WriteLine($"upgrading {sampleDirectories.Count} samples to Stride {StridePackageUpgrader.CurrentVersion} together...");
        var watch = Stopwatch.StartNew();
        // The files the upgrade overwrites only just came from the template: nothing to back up
        var upgrade = RunDotnet([FindAssetCompiler(), "upgrade", solution, "--no-backup"], root, log, UpgradeAllTimeoutSeconds, nodeReuse: true);
        if (upgrade.ExitCode != 0)
        {
            Console.WriteLine($"The upgrade of the {sampleDirectories.Count} samples together failed (exit code {upgrade.ExitCode}); see {log}:\n{Tail(upgrade.Output)}");
            return false;
        }

        Console.WriteLine($"Upgraded {sampleDirectories.Count} samples to Stride {StridePackageUpgrader.CurrentVersion} together in {watch.Elapsed.TotalSeconds:F0} s");
        return true;
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

        // The files the upgrade overwrites only just came from the template: nothing to back up
        var upgrade = RunDotnet([FindAssetCompiler(), "upgrade", solution, "--no-backup"], directory, log, UpgradeTimeoutSeconds, nodeReuse: true);
        if (upgrade.ExitCode != 0)
        {
            // The upgrade restores the sample first, and stops there, with nothing changed, when the restore fails
            if (!upgrade.Output.Any(IsStridePackageNotFound))
                throw new InvalidOperationException($"The upgrade of {sampleName} to Stride {StridePackageUpgrader.CurrentVersion} failed (exit code {upgrade.ExitCode}); see {log}:\n{Tail(upgrade.Output)}");

            Console.WriteLine($"[{sampleName}] NOT upgraded: no feed has the Stride version it is written for. Its references move to {StridePackageUpgrader.CurrentVersion} as they are.");
            foreach (var project in Directory.EnumerateFiles(directory, "*.csproj", SearchOption.AllDirectories))
                StridePackageUpgrader.UpgradeProjectVersions(project, logger);
            return;
        }

        Console.WriteLine($"[{sampleName}] upgraded to Stride {StridePackageUpgrader.CurrentVersion} in {watch.Elapsed.TotalSeconds:F0} s");
    }

    // NU1101: no feed has the package; NU1102: no feed has that version of it; NU1103: the feeds have only prereleases
    // above that version, such as the dev packages of this engine
    private static bool IsStridePackageNotFound(string line)
        => (line.Contains("NU1101", StringComparison.Ordinal) || line.Contains("NU1102", StringComparison.Ordinal))
            && line.Contains("Unable to find package Stride", StringComparison.Ordinal)
            || line.Contains("NU1103", StringComparison.Ordinal) && line.Contains("Unable to find a stable package Stride", StringComparison.Ordinal);

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
    /// <param name="nodeReuse">Whether MSBuild may keep its nodes for reuse: only for a program that shuts them down when it ends.</param>
    internal static (int ExitCode, IReadOnlyList<string> Output) RunDotnet(IReadOnlyList<string> arguments, string workingDirectory, string logPath, int timeoutSeconds, bool nodeReuse = false)
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
        // as long as the node idles. The variable also reaches the builds that a program runs inside, which an argument does not.
        if (!nodeReuse)
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
