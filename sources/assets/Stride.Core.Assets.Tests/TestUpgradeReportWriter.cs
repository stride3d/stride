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
    private static readonly string ProjectFile = Path.Combine(ProjectDirectory, "Game.csproj");
    private static readonly string SolutionFile = Path.Combine(Path.GetTempPath(), "Game.slnx");
    private static readonly string BackupFolder = Path.Combine(Path.GetTempPath(), ".stride-backup-20261121-143200");

    private const string LeftoversSection = """
        ## 2026-11-21 14:32 · Stride 4.4.3 → 4.5.0 · 4 items left

        ### Game · 4 items left

        6 steps: 2 done, 1 failed, 2 partial, 1 not run. Also upgraded: MyPlugin 1.0.0 → 2.0.0.

        #### May change behaviour silently

        - [ ] `Stride-4.5.0-C3` Add `[EffectKeys]` ([guide](https://doc/4.5.html#stride-4.5.0-c3)) (partial)
          - `Keys.cs(42,13)`: `add [EffectKeys]`, not applied

        #### Failed

        - [ ] `Stride-4.5.0-C2` Fix the callbacks ([guide](https://doc/4.5.html#stride-4.5.0-c2))
          - The rule threw.

        #### Partial

        - [ ] `Stride-4.5.0-P1` Update the effects ([guide](https://doc/4.5.html#stride-4.5.0-p1))
          - `Effects/Main.sdfx` is left unchanged: The file can't be read
          - `Player.cs(7,5)`: `fmt.IsSRgb() → fmt.IsSRgb`, not applied

        #### Not run

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

        ### Game · 1 step done

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
        var text = UpgradeReportWriter.AddUpgrade(null, ProjectFile, [LeftoversReport()], Time);

        Assert.Equal(Lf($"""
            # Upgrade report: Game.csproj

            Upgrades of this project left the items below to do by hand. The newest upgrade is first. Once every item is handled, remove this file and the backup of the upgrades with **Remove the report and backup** in Game Studio: deleting only this file keeps the backup, which a revert still puts back.

            {LeftoversSection}
            """), text);
    }

    [Fact]
    public void TheNewestUpgradeGoesOnTopOfTheOnesTheFileLists()
    {
        var existing = UpgradeReportWriter.AddUpgrade(null, ProjectFile, [LeftoversReport()], Time).ReplaceLineEndings("\r\n");

        var text = UpgradeReportWriter.AddUpgrade(existing, ProjectFile, [CleanReport()], Time.AddDays(3));

        Assert.Equal(Lf($"""
            # Upgrade report: Game.csproj

            Upgrades of this project left the items below to do by hand. The newest upgrade is first. Once every item is handled, remove this file and the backup of the upgrades with **Remove the report and backup** in Game Studio: deleting only this file keeps the backup, which a revert still puts back.

            {CleanSection}
            {LeftoversSection}
            """), text);
    }

    [Fact]
    public void AFileWithoutUpgradeGetsTheUpgradeAtItsEnd()
    {
        var text = UpgradeReportWriter.AddUpgrade("# Notes\n\nHandled by hand.\n\n", ProjectFile, [CleanReport()], Time);

        Assert.StartsWith("# Notes\n\nHandled by hand.\n\n## 2026-11-21 14:32 · Stride 4.5.0 → 4.5.7 · 1 step done\n", text);
    }

    [Fact]
    public void TheUpgradeReportListsEachProject()
    {
        var report = new UpgradeReport();
        report.Projects.Add(CleanReport());
        var input = Path.Combine(ProjectDirectory, "Game.sln");

        Assert.Equal(Lf($"""
            # Upgrade report: Game.sln

            {CleanSection.Replace("## 2026-11-24 14:32 ·", "## 2026-11-21 14:32 ·", StringComparison.Ordinal)}
            """), UpgradeReportWriter.FormatUpgrade(report, input, Time));
        Assert.Equal("# Upgrade report: Game.sln\n\nThe upgrade of 2026-11-21 14:32: no project needed an upgrade.\n", UpgradeReportWriter.FormatUpgrade(new UpgradeReport(), input, Time));
    }

    [Fact]
    public void TheReportOfASolutionHasASubsectionPerProject()
    {
        var windows = CleanReport(Path.Combine(Path.GetTempPath(), "Game.Windows", "Game.Windows.csproj"));
        var game = LeftoversReport();
        game.BackupFolder = BackupFolder;

        var text = UpgradeReportWriter.AddUpgrade(null, SolutionFile, [game, windows], Time);

        Assert.StartsWith("# Upgrade report: Game.slnx\n\nUpgrades of this solution left the items below to do by hand.", text, StringComparison.Ordinal);
        // What is of the whole upgrade is in its heading, once
        Assert.Contains("\n## 2026-11-21 14:32 · Stride 4.4.3 → 4.5.0 · 4 items left\n\nThe files it changed are backed up in `.stride-backup-20261121-143200`.\n\n### Game · 4 items left\n", text, StringComparison.Ordinal);
        // A project upgraded from other versions names them
        Assert.Contains("\n### Game.Windows · Stride 4.5.0 → 4.5.7 · 1 step done\n\n1 step: 1 done.\n", text, StringComparison.Ordinal);
        // The paths are relative to the solution
        Assert.Contains("`Game/Keys.cs(42,13)`", text, StringComparison.Ordinal);
        Assert.Contains("  - `Game.Windows/Player.cs`\n", text, StringComparison.Ordinal);
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

        var text = Lf(UpgradeReportWriter.AddUpgrade(null, ProjectFile, [project], Time));

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

        var text = UpgradeReportWriter.AddUpgrade(null, ProjectFile, [project], Time);

        Assert.Contains(title + "\n", text, StringComparison.Ordinal);
        Assert.Contains(summary, text, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUpgradeNamesItsBackup()
    {
        var project = CleanReport();
        project.BackupFolder = BackupFolder;

        Assert.Contains(" · 1 step done\n\nThe files it changed are backed up in `../.stride-backup-20261121-143200`.\n",
            UpgradeReportWriter.AddUpgrade(null, ProjectFile, [project], Time), StringComparison.Ordinal);
        // The report of a reverted upgrade is in its backup folder
        project.Outcome = UpgradeOutcome.Reverted;
        Assert.DoesNotContain("backed up in", UpgradeReportWriter.AddUpgrade(null, ProjectFile, [project], Time), StringComparison.Ordinal);
    }

    [Fact]
    public void ARevertGoesOnTopOfTheUpgradeItReverts()
    {
        var existing = UpgradeReportWriter.AddUpgrade(null, ProjectFile, [LeftoversReport()], Time);
        var revert = new UpgradeRevert([BackupFolder], [], [Path.Combine(BackupFolder, "reverted-20261121-153200")], []);

        var text = UpgradeReportWriter.AddRevert(existing, ProjectFile, ["Game"], revert, [new RevertedVersion("Stride", "4.5.7", "4.5.0")], Time.AddHours(1));

        Assert.Equal(existing.Replace("\n## ", Lf("""

            ## 2026-11-21 15:32 · Stride 4.5.7 → 4.5.0 · reverted

            Every file the last upgrade and its retries changed is put back as it was, from `../.stride-backup-20261121-143200`. Its items below are no longer to do: the next upgrade runs its steps again. The files as they were before the revert are in `../.stride-backup-20261121-143200/reverted-20261121-153200`.


            """) + "## ", StringComparison.Ordinal), text);
    }

    [Fact]
    public void ARevertThatCantPutFilesBackListsThem()
    {
        var unreadable = Path.Combine(Path.GetTempPath(), ".stride-backup-20261120-090000");
        var revert = new UpgradeRevert([BackupFolder, unreadable], [Path.Combine(ProjectDirectory, "Game.csproj"), unreadable], [], []);

        // Without the versions of the records, which the project file not put back can't give
        var text = UpgradeReportWriter.AddRevert(null, ProjectFile, ["Game"], revert, [], Time);

        Assert.Equal(Lf("""
            # Upgrade report: Game.csproj

            Upgrades of this project left the items below to do by hand. The newest upgrade is first. Once every item is handled, remove this file and the backup of the upgrades with **Remove the report and backup** in Game Studio: deleting only this file keeps the backup, which a revert still puts back.

            ## 2026-11-21 14:32 · Last upgrade · partly reverted, 2 items left

            Some files the last upgrade and its retries changed could not be put back as they were: copy them from `../.stride-backup-20261121-143200`, `../.stride-backup-20261120-090000`, then open the project again.

            ### Not put back

            - [ ] `Game.csproj`
            - [ ] The files of `../.stride-backup-20261120-090000`

            """), text);
    }

    [Fact]
    public void ARevertListsTheFilesChangedSinceTheUpgrade()
    {
        var kept = Path.Combine(BackupFolder, "reverted-20261121-153200");
        var revert = new UpgradeRevert([BackupFolder], [], [kept], [Path.Combine(ProjectDirectory, "Game.csproj"), Path.Combine(ProjectDirectory, "Assets", "Scene.sdscene")]);
        // A plugin upgrader that recorded no version before the upgrade
        RevertedVersion[] versions = [new("Stride", "4.5.7", "4.5.0"), new("Stride.BepuPhysics", "4.5.7", null)];

        var text = UpgradeReportWriter.AddRevert(null, ProjectFile, ["Game"], revert, versions, Time);

        Assert.Equal(Lf("""
            # Upgrade report: Game.csproj

            Upgrades of this project left the items below to do by hand. The newest upgrade is first. Once every item is handled, remove this file and the backup of the upgrades with **Remove the report and backup** in Game Studio: deleting only this file keeps the backup, which a revert still puts back.

            ## 2026-11-21 14:32 · Stride 4.5.7 → 4.5.0 · reverted, 2 items left

            Every file the last upgrade and its retries changed is put back as it was, from `../.stride-backup-20261121-143200`. Its items below are no longer to do: the next upgrade runs its steps again. Also reverted: Stride.BepuPhysics 4.5.7. The files as they were before the revert are in `../.stride-backup-20261121-143200/reverted-20261121-153200`.

            ### Changed since the upgrade

            Your changes to these files are only in `../.stride-backup-20261121-143200/reverted-20261121-153200`: copy back what you want to keep.

            - [ ] `Game.csproj`
            - [ ] `Assets/Scene.sdscene`

            """), text);
    }

    [Fact]
    public void ARevertOfASolutionNamesItsProjects()
    {
        var revert = new UpgradeRevert([BackupFolder], [], [], []);

        var text = UpgradeReportWriter.AddRevert(null, SolutionFile, ["Game", "Game.Windows"], revert, [new RevertedVersion("Stride", "4.5.7", "4.5.0")], Time);

        Assert.Contains("\nEvery file the last upgrade and its retries changed in `Game`, `Game.Windows` is put back as it was, from `.stride-backup-20261121-143200`.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUpgradeMarkedAsFinishedGoesOnTopOfTheUpgrade()
    {
        var existing = UpgradeReportWriter.AddUpgrade(null, ProjectFile, [LeftoversReport()], Time);

        var text = UpgradeReportWriter.AddMarkedFinished(existing, ProjectFile, [("Game", ["Stride-4.5.0-P2", "Put the files back as they were"])], Time.AddHours(1));

        Assert.Equal(existing.Replace("\n## ", "\n## 2026-11-21 15:32 · Last upgrade marked as finished\n\nIts items below are done by hand:\n\n- Game: `Stride-4.5.0-P2`, Put the files back as they were\n\n## ", StringComparison.Ordinal), text);
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

    private static ProjectUpgradeReport CleanReport(string? projectFile = null)
    {
        var project = new ProjectUpgradeReport(new UFile(projectFile ?? ProjectFile));
        var stride = new UpgraderReport("Stride", new PackageVersion("4.5.0"), new PackageVersion("4.5.7"), [new UpgradeStepInfo("Stride-4.5.7-C1", "Rename `C` to `D`", null)]);
        stride.Steps[0].ChangedFiles.Add(Path.Combine(Path.GetDirectoryName(projectFile ?? ProjectFile)!, "Player.cs"));
        stride.Steps[0].SetRan();
        project.Upgraders.Add(stride);
        return project;
    }

    // The raw string literals of this file have the line endings of the checkout
    private static string Lf(string text) => text.ReplaceLineEndings("\n");
}
