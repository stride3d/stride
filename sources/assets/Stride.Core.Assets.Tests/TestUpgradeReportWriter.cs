// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Core.IO;
using Xunit;

namespace Stride.Core.Assets.Tests;

public class TestUpgradeReportWriter
{
    private static readonly DateTime Time = new(2026, 11, 21, 14, 32, 0);
    private static readonly string ProjectDirectory = Path.Combine(Path.GetTempPath(), "Game");

    private const string LeftoversSection = """
        ## 2026-11-21 14:32 · Stride 4.4.3 → 4.5.0 · 4 items left

        6 steps: 2 done, 1 failed, 2 partial, 1 not run. Also upgraded: MyPlugin 1.0.0 → 2.0.0.

        ### May change behaviour silently

        - [ ] `Stride-4.5.0-C3` Add `[EffectKeys]` ([guide](https://doc/4.5.html#stride-4.5.0-c3)) (partial)
          - `Keys.cs(42,13)`: `add [EffectKeys]`, not applied

        ### Failed

        - [ ] `Stride-4.5.0-C2` Fix the callbacks ([guide](https://doc/4.5.html#stride-4.5.0-c2))
          - The rule threw.

        ### Partial

        - [ ] `Stride-4.5.0-P1` Update the effects ([guide](https://doc/4.5.html#stride-4.5.0-p1))
          - `Effects/Main.sdfx` is left unchanged: The file can't be read
          - `Player.cs(7,5)`: `fmt.IsSRgb() → fmt.IsSRgb`, not applied

        ### Not run

        - [ ] `Stride-4.5.0-P2` Add the plugin packages ([guide](https://doc/4.5.html#stride-4.5.0-p2))

        <details>
        <summary>Done (2)</summary>

        - `Stride-4.5.0-C1` Rename `A` to `B` ([guide](https://doc/4.5.html#stride-4.5.0-c1)) (5 edits in 2 files)
          <details>
          <summary>Files</summary>

          - `Player.cs`
          - `Scripts/Enemy.cs`

          </details>
        - `MyPlugin-2.0.0-P1` Move the settings

        </details>

        """;

    private const string CleanSection = """
        ## 2026-11-24 14:32 · Stride 4.5.0 → 4.5.7 · 1 step done

        1 step: 1 done.

        <details>
        <summary>Done (1)</summary>

        - `Stride-4.5.7-C1` Rename `C` to `D` (1 file)
          <details>
          <summary>Files</summary>

          - `Player.cs`

          </details>

        </details>

        """;

    [Fact]
    public void ANewFileListsTheLeftoversThenTheStepsDone()
    {
        var text = UpgradeReportWriter.AddToProjectFile(null, LeftoversReport(), Time);

        Assert.Equal(Lf($"""
            # Upgrade report: Game.csproj

            Upgrades of this project left the items below to do by hand. Delete this file once every item is handled. The newest upgrade is first.

            {LeftoversSection}
            """), text);
    }

    [Fact]
    public void TheNewestUpgradeGoesOnTopOfTheOnesTheFileLists()
    {
        var existing = UpgradeReportWriter.AddToProjectFile(null, LeftoversReport(), Time).ReplaceLineEndings("\r\n");

        var text = UpgradeReportWriter.AddToProjectFile(existing, CleanReport(), Time.AddDays(3));

        Assert.Equal(Lf($"""
            # Upgrade report: Game.csproj

            Upgrades of this project left the items below to do by hand. Delete this file once every item is handled. The newest upgrade is first.

            {CleanSection}
            {LeftoversSection}
            """), text);
    }

    [Fact]
    public void AFileWithoutUpgradeGetsTheUpgradeAtItsEnd()
    {
        var text = UpgradeReportWriter.AddToProjectFile("# Notes\n\nHandled by hand.\n\n", CleanReport(), Time);

        Assert.StartsWith("# Notes\n\nHandled by hand.\n\n## 2026-11-21 14:32 · Stride 4.5.0 → 4.5.7 · 1 step done\n", text);
    }

    [Fact]
    public void TheUpgradeReportListsEachProject()
    {
        var report = new UpgradeReport();
        report.Projects.Add(CleanReport());

        Assert.Equal(Lf($"""
            # Upgrade report: Game.sln

            The upgrade of 2026-11-21 14:32.

            {CleanSection.Replace("## 2026-11-24 14:32 ·", "## Game.csproj ·", StringComparison.Ordinal)}
            """), UpgradeReportWriter.FormatUpgrade(report, "Game.sln", Time));
        Assert.Equal("# Upgrade report: Game.sln\n\nThe upgrade of 2026-11-21 14:32. No project needed an upgrade.\n", UpgradeReportWriter.FormatUpgrade(new UpgradeReport(), "Game.sln", Time));
    }

    [Fact]
    public void TheLogHasOneLineThenTheSteps()
    {
        var project = LeftoversReport();

        Assert.Equal("Stride 4.4.3 to 4.5.0: 4 items left", UpgradeReportWriter.FormatSummary(project));
        Assert.Equal(
            [
                "  done     Stride-4.5.0-C1 Rename `A` to `B`",
                "  failed   Stride-4.5.0-C2 Fix the callbacks",
                "  partial  Stride-4.5.0-C3 Add `[EffectKeys]` (1 change not made)",
                "  partial  Stride-4.5.0-P1 Update the effects (1 file left unchanged, 1 change not made)",
                "  not run  Stride-4.5.0-P2 Add the plugin packages",
                "  done     MyPlugin-2.0.0-P1 Move the settings",
            ], UpgradeReportWriter.FormatStepLines(project));
        Assert.Equal("Stride 4.5.0 to 4.5.7: 1 step done", UpgradeReportWriter.FormatSummary(CleanReport()));
    }

    [Fact]
    public void TheDialogsListTheStepsWithTheirIdLinkedToTheGuide()
    {
        Assert.Equal("""
            - failed [Stride-4.5.0-C2](https://doc/4.5.html#stride-4.5.0-c2) Fix the callbacks
            - partial [Stride-4.5.0-C3](https://doc/4.5.html#stride-4.5.0-c3) Add `[EffectKeys]` (1 change not made)
            - partial [Stride-4.5.0-P1](https://doc/4.5.html#stride-4.5.0-p1) Update the effects (1 file left unchanged, 1 change not made)
            - not run [Stride-4.5.0-P2](https://doc/4.5.html#stride-4.5.0-p2) Add the plugin packages
            """.ReplaceLineEndings("\n"), UpgradeReportWriter.FormatStepsLeftList(LeftoversReport()));
        Assert.Equal(
            "- [Stride-4.5.0-P1](https://doc/4.5.html#stride-4.5.0-p1) Update the effects\n- `Stride-4.4.0-P9`\n- Put the files back as they were",
            UpgradeReportWriter.FormatStepList([new UpgradeStepInfo("Stride-4.5.0-P1", "Update the effects", "https://doc/4.5.html#stride-4.5.0-p1"), new UpgradeStepInfo("Stride-4.4.0-P9", "", null), new UpgradeStepInfo("", "Put the files back as they were", null)]));
    }

    [Fact]
    public void AnExceptionShowsItsFirstFrames()
    {
        var project = CleanReport();
        project.Upgraders[0].Steps[0].SetFailed(Throw());

        var text = Lf(UpgradeReportWriter.AddToProjectFile(null, project, Time));

        const string Start = "  - Deep\n    ```\n";
        var block = text[(text.IndexOf(Start, StringComparison.Ordinal) + Start.Length)..];
        var frames = block[..block.IndexOf("    ```\n", StringComparison.Ordinal)].Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, frames.Length);
        Assert.All(frames, x => Assert.StartsWith("    at ", x, StringComparison.Ordinal));

        static Exception Throw()
        {
            try
            {
                One();
                return null!;
            }
            catch (Exception e)
            {
                return e;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void One() => Two();
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void Two() => Three();
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void Three() => Four();
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void Four() => throw new InvalidOperationException("Deep");
        }
    }

    [Theory]
    [InlineData(UpgradeOutcome.Reverted, "## 2026-11-21 14:32 · Stride 4.5.0 → 4.5.7 · reverted", "A step of a project of the solution failed, so every file the upgrade changed, in every project, was put back as it was: the project stays on Stride 4.5.0. Nothing below is to do by hand: fix why the upgrade failed, and its next upgrade runs these steps again.")]
    [InlineData(UpgradeOutcome.Kept, "## 2026-11-21 14:32 · Stride 4.5.0 → 4.5.7 · kept, 1 item left", "A step failed, and the files were kept as the upgrade left them: finish the items below by hand, then mark the upgrade as finished.")]
    [InlineData(UpgradeOutcome.PartlyReverted, "## 2026-11-21 14:32 · Stride 4.5.0 → 4.5.7 · partly reverted, 1 item left", "some files the upgrade changed could not be put back as they were: copy them from the backup")]
    public void AFailedUpgradeSaysWhatHappenedToItsFiles(UpgradeOutcome outcome, string title, string summary)
    {
        var project = CleanReport();
        project.Upgraders[0].Steps[0].Status = UpgradeStepStatus.Failed;
        project.Outcome = outcome;

        var text = UpgradeReportWriter.AddToProjectFile(null, project, Time);

        Assert.Contains(title + "\n", text, StringComparison.Ordinal);
        Assert.Contains(summary, text, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUpgradeThatContinuesAnUnfinishedOneNamesItsSteps()
    {
        var project = CleanReport();
        project.ContinuedFromUnfinished.AddRange(["Stride-4.5.0-P2", "Stride-4.5.0-C3"]);

        Assert.Contains("1 step: 1 done. It continued an upgrade marked as finished, which had left `Stride-4.5.0-P2`, `Stride-4.5.0-C3` to finish by hand.\n",
            UpgradeReportWriter.AddToProjectFile(null, project, Time), StringComparison.Ordinal);
    }

    private static ProjectUpgradeReport LeftoversReport()
    {
        var project = new ProjectUpgradeReport(new UFile(Path.Combine(ProjectDirectory, "Game.csproj")));
        var stride = new UpgraderReport("Stride", new PackageVersion("4.4.3"), new PackageVersion("4.5.0"),
        [
            Step("Stride-4.5.0-C1", "Rename `A` to `B`"),
            Step("Stride-4.5.0-C2", "Fix the callbacks"),
            Step("Stride-4.5.0-C3", "Add `[EffectKeys]`") with { MayChangeBehaviour = true },
            Step("Stride-4.5.0-P1", "Update the effects"),
            Step("Stride-4.5.0-P2", "Add the plugin packages"),
        ]);
        stride.Steps[0].ChangedFiles.AddRange([Path.Combine(ProjectDirectory, "Player.cs"), Path.Combine(ProjectDirectory, "Scripts", "Enemy.cs")]);
        stride.Steps[0].Edits = 5;
        stride.Steps[0].SetRan();
        stride.Steps[1].Status = UpgradeStepStatus.Failed;
        stride.Steps[1].Error = "The rule\nthrew.";
        stride.Steps[2].MissedChanges.Add(new UpgradeMissedChange(Path.Combine(ProjectDirectory, "Keys.cs"), 42, 13, "add [EffectKeys]"));
        stride.Steps[2].SetRan();
        stride.Steps[3].FailedUnits.Add(new UpgradeUnitFailure(Path.Combine(ProjectDirectory, "Effects", "Main.sdfx"), "The file can't be read"));
        stride.Steps[3].MissedChanges.Add(new UpgradeMissedChange(Path.Combine(ProjectDirectory, "Player.cs"), 7, 5, "fmt.IsSRgb() → fmt.IsSRgb"));
        stride.Steps[3].SetRan();
        project.Upgraders.Add(stride);

        var plugin = new UpgraderReport("MyPlugin", new PackageVersion("1.0.0"), new PackageVersion("2.0.0"), [new UpgradeStepInfo("MyPlugin-2.0.0-P1", "Move the settings", null)]);
        plugin.Steps[0].SetRan();
        project.Upgraders.Add(plugin);
        return project;

        static UpgradeStepInfo Step(string id, string title) => new(id, title, $"https://doc/4.5.html#{id.ToLowerInvariant()}");
    }

    private static ProjectUpgradeReport CleanReport()
    {
        var project = new ProjectUpgradeReport(new UFile(Path.Combine(ProjectDirectory, "Game.csproj")));
        var stride = new UpgraderReport("Stride", new PackageVersion("4.5.0"), new PackageVersion("4.5.7"), [new UpgradeStepInfo("Stride-4.5.7-C1", "Rename `C` to `D`", null)]);
        stride.Steps[0].ChangedFiles.Add(Path.Combine(ProjectDirectory, "Player.cs"));
        stride.Steps[0].SetRan();
        project.Upgraders.Add(stride);
        return project;
    }

    // The raw string literals of this file have the line endings of the checkout
    private static string Lf(string text) => text.ReplaceLineEndings("\n");
}
