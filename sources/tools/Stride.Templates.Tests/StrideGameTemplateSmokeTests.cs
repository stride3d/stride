// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Stride.Templates.Tests;

/// <summary>
/// End-to-end smoke for the Stride template packages: dotnet pack → dotnet new install →
/// dotnet new <c>&lt;template&gt;</c> for each of stride-game, stride-fps, stride-csharp-beginner.
/// Validates that the orchestrator output and the packed nupkg shape produce instantiable
/// projects through the dotnet new template engine. A subsequent <c>dotnet restore</c> isn't
/// run — it'd need <see cref="Stride"/>.Engine in bin/packages or on nuget.org, neither of
/// which is reliably available on a fresh CI runner that only builds the templates slnf.
/// </summary>
public class StrideGameTemplateSmokeTests
{
    private readonly ITestOutputHelper output;

    public StrideGameTemplateSmokeTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void PackInstallInstantiate()
    {
        var repoRoot = FindRepoRoot();
        var enginePackagesDir = Path.Combine(repoRoot, "bin", "packages");
        Assert.True(Directory.Exists(enginePackagesDir),
            $"Expected bin/packages/ to exist at {enginePackagesDir}. Build the engine first " +
            "(any normal test run does this transitively).");

        // Covers all three preprocessor variants:
        //   - blank game (Stride.Templates.Games → stride-game): NewGame body, no sample-derived steps
        //   - starter (Stride.Templates.Games.Starters → stride-fps): full preprocessor incl. dep
        //     collapse / asset prune / source-name rename
        //   - sample (Stride.Templates.Samples → stride-csharp-beginner): same preprocessor flow as
        //     starters but smaller asset set
        var packagesToPack = new[]
        {
            ("Stride.Templates.Games",          "stride-game",            "SmokeBlankGame"),
            ("Stride.Templates.Games.Starters", "stride-fps",             "SmokeFps"),
            ("Stride.Templates.Samples",        "stride-csharp-beginner", "SmokeTutorial"),
        };
        var nupkgs = new List<string>();
        foreach (var (packageId, _, _) in packagesToPack)
        {
            var csproj = Path.Combine(repoRoot, "sources", "templates", packageId, $"{packageId}.csproj");
            Assert.True(File.Exists(csproj), $"Templates csproj not found at {csproj}");

            // Stride.Templates.Common.targets sets PackageOutputPath=$(StrideRoot)bin\packages\,
            // so all Stride.Templates.* pack outputs land in the engine's shared package feed
            // dir (same one consumers will restore from). Stride.Templates.Tests.csproj has a
            // BeforeBuild target that explicitly Packs all three template projects — the
            // in-test pack below is a fallback for non-build invocations.
            string? nupkg = FindNupkg(enginePackagesDir, packageId);
            if (nupkg == null)
            {
                output.WriteLine($"No pre-built nupkg for {packageId}; running dotnet pack.");
                var packResult = RunDotnet(repoRoot, "pack", csproj, "-c", "Debug", "--nologo", "-v", "minimal");
                Assert.True(packResult.exitCode == 0, $"dotnet pack {packageId} failed:\n{packResult.output}");
                nupkg = FindNupkg(enginePackagesDir, packageId);
            }
            Assert.NotNull(nupkg);
            output.WriteLine($"Using nupkg: {nupkg}");
            nupkgs.Add(nupkg);
        }

        // Step 2: instantiate in an isolated workspace. Each test run gets its own dir so parallel
        // runs and previous failures don't interfere; tracked install is scoped to the nupkg path
        // so we can uninstall cleanly at the end.
        var workspace = Path.Combine(Path.GetTempPath(), "stride-template-smoke-" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(workspace);
        try
        {
            // Pre-clean any leftover install from a prior aborted run; ignore failure if not
            // installed. dotnet new install errors with exit 106 if the package file is already
            // in ~/.templateengine/packages/.
            foreach (var (packageId, _, _) in packagesToPack)
                RunDotnet(workspace, "new", "uninstall", packageId);

            foreach (var nupkg in nupkgs)
            {
                var installResult = RunDotnet(workspace, "new", "install", nupkg);
                Assert.True(installResult.exitCode == 0, $"dotnet new install {nupkg} failed:\n{installResult.output}");
            }

            try
            {
                foreach (var (_, shortName, projectName) in packagesToPack)
                    InstantiateAndValidate(workspace, templateShortName: shortName, projectName: projectName);
            }
            finally
            {
                // Uninstall by package id, not by path: dotnet new install copies the nupkg
                // into its tracking dir and registers it by id, so the original path is no
                // longer a valid uninstall target.
                foreach (var (packageId, _, _) in packagesToPack)
                    RunDotnet(workspace, "new", "uninstall", packageId);
            }
        }
        finally
        {
            try { Directory.Delete(workspace, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }

    /// <summary>
    /// Instantiates the given template with <c>-n &lt;projectName&gt;</c> and statically
    /// validates the generated library csproj (Stride.Engine PackageReference is present).
    /// The library is the package-bearing project in every Stride template variant;
    /// per-platform exec projects may not exist (e.g. CSharpBeginner has Windows-only).
    /// </summary>
    private void InstantiateAndValidate(string workspace, string templateShortName, string projectName)
    {
        var newResult = RunDotnet(workspace, "new", templateShortName, "-n", projectName);
        Assert.True(newResult.exitCode == 0, $"dotnet new {templateShortName} failed:\n{newResult.output}");

        var instantiated = Path.Combine(workspace, projectName);
        Assert.True(Directory.Exists(instantiated), $"Instantiated project dir missing at {instantiated}");

        // Two layouts in the wild: stride-game uses the post-restructure flat layout
        // (<Name>/<Name>.csproj — no .Game suffix); sample-derived templates (starters, samples)
        // keep the older nested layout (<Name>.Game/<Name>.Game.csproj). Try the flat one first.
        var libraryCsproj = Path.Combine(instantiated, projectName, $"{projectName}.csproj");
        if (!File.Exists(libraryCsproj))
            libraryCsproj = Path.Combine(instantiated, $"{projectName}.Game", $"{projectName}.Game.csproj");
        Assert.True(File.Exists(libraryCsproj), $"Expected library csproj at <{instantiated}>/{{{projectName},{projectName}.Game}}/...");

        // Static check on the generated csproj instead of a NuGet restore. A real restore would
        // need Stride.Engine in bin/packages or on nuget.org; on a fresh CI runner that built
        // only the templates slnf, neither has it (Stride.Engine isn't a transitive build dep
        // of the test). The PackageReference being present + non-empty version is what restore
        // would ultimately validate.
        var csprojText = File.ReadAllText(libraryCsproj);
        Assert.True(csprojText.Contains("<PackageReference Include=\"Stride.Engine\""),
            $"Generated {Path.GetFileName(libraryCsproj)} missing Stride.Engine PackageReference:\n{csprojText}");
    }

    private static string? FindNupkg(string dir, string packageId)
    {
        // Glob `{packageId}.*.nupkg` would also match longer-prefix packages (e.g. searching
        // for "Stride.Templates.Games" matches "Stride.Templates.Games.Starters.<ver>.nupkg"
        // because the `*` between packageId and `.nupkg` is unconstrained). Require a digit
        // right after `{packageId}.` to anchor to the version segment.
        var prefix = packageId + ".";
        return Directory.EnumerateFiles(dir, "*.nupkg")
            .Where(f =>
            {
                var name = Path.GetFileName(f);
                return name.StartsWith(prefix, StringComparison.Ordinal)
                    && name.Length > prefix.Length
                    && char.IsDigit(name[prefix.Length]);
            })
            .OrderByDescending(File.GetLastWriteTime)
            .FirstOrDefault();
    }

    private (int exitCode, string output) RunDotnet(string workingDir, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var a in args)
            psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi)!;
        // Read stdout and stderr concurrently. Sequentially reading ReadToEnd on one stream
        // then the other deadlocks once the un-read stream's pipe buffer fills (`dotnet pack`
        // produces enough output to hit this in seconds — symptom: pack appears to take
        // forever, because the child is stalled mid-write, not actually compiling).
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        var combined = (stdout + stderr).Trim();
        output.WriteLine($"$ dotnet {string.Join(' ', args)}");
        output.WriteLine($"  → exit {proc.ExitCode}");
        if (combined.Length > 0)
            output.WriteLine(combined);
        return (proc.ExitCode, combined);
    }

    /// <summary>
    /// Walk up from the test's bin dir looking for build/Stride.sln as the repo root marker.
    /// The test is launched from sources/tools/Stride.Templates.Tests/bin/.../ so a few hops up.
    /// </summary>
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "build", "Stride.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate repo root (no build/Stride.sln found walking up from test bin dir)");
    }
}
