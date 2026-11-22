// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// A project records, for each package upgrader, the version its upgrade moved it to (<see cref="PackageUpgrader.UpgradeRecordItem"/>),
/// when the session saves it after that upgrade completed.
/// </summary>
public class TestUpgradedVersionRecord : IDisposable
{
    public abstract class SucceedingUpgraderBase : PackageUpgrader
    {
        public override bool Upgrade(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage, IList<PackageLoadingAssetFile> assetFiles)
            => true;
    }

    [PackageUpgrader(["UpgradeRecord.Succeeds", "UpgradeRecord.Succeeds.Extra"], "1.0.0", "2.0.0", Name = "Succeeds")]
    public class SucceedingUpgrader : SucceedingUpgraderBase;

    // Named after its package
    [PackageUpgrader(["MyPlugin"], "1.0.0", "3.0.0")]
    public class PluginUpgrader : SucceedingUpgraderBase;

    [PackageUpgrader(["UpgradeRecord.Fails"], "1.0.0", "2.0.0")]
    public class FailingUpgrader : PackageUpgrader
    {
        public override bool Upgrade(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage, IList<PackageLoadingAssetFile> assetFiles)
            => false;
    }

    [PackageUpgrader(["UpgradeRecord.Throws"], "1.0.0", "2.0.0")]
    public class ThrowingUpgrader : PackageUpgrader
    {
        public override bool Upgrade(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage, IList<PackageLoadingAssetFile> assetFiles)
            => throw new InvalidOperationException("The upgrade throws");
    }

    // Its last step, after the assets are loaded, fails
    [PackageUpgrader(["UpgradeRecord.FailsAfterAssets"], "1.0.0", "2.0.0")]
    public class FailingAfterAssetsUpgrader : SucceedingUpgraderBase
    {
        public override bool UpgradeAfterAssetsLoaded(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage, PackageVersionRange dependencyVersionBeforeUpdate)
            => false;
    }

    // It has source migrations (see CodeRunner), which do nothing
    [PackageUpgrader(["UpgradeRecord.CodeRules"], "1.0.0", "2.0.0")]
    public class CodeRulesUpgrader : SucceedingUpgraderBase;

    // The source migration of these upgraders can't run (see CodeRunner)
    [PackageUpgrader(["UpgradeRecord.CodeNotRun"], "1.0.0", "2.0.0")]
    public class CodeNotRunUpgrader : SucceedingUpgraderBase;

    [PackageUpgrader(["UpgradeRecord.CodeThrows"], "1.0.0", "2.0.0")]
    public class CodeThrowingUpgrader : SucceedingUpgraderBase;

    // Its project migration fails as a whole
    [PackageUpgrader(["UpgradeRecord.ProjectFails"], "1.0.0", "2.0.0")]
    public class ProjectFailingUpgrader : global::Stride.Assets.CodePackageUpgrader
    {
        public override void DeclareUpgrades(global::Stride.Assets.UpgradeRegistry registry)
            => registry.Project("2.0.0", "Change the project", _ => throw new InvalidOperationException("The project can't be changed"));
    }

    // Each project step sets a property and writes a file next to the project; the second one then throws
    [PackageUpgrader(["UpgradeRecord.SecondStepThrows"], "1.0.0", "2.0.0")]
    public class SecondStepThrowingUpgrader : global::Stride.Assets.CodePackageUpgrader
    {
        public override void DeclareUpgrades(global::Stride.Assets.UpgradeRegistry registry)
        {
            registry.Project("2.0.0", "First", context => Change(context, "First"));
            registry.Project("2.0.0", "Second", context =>
            {
                Change(context, "Second");
                throw new InvalidOperationException("The second step throws");
            });
            registry.Project("2.0.0", "Third", context => Change(context, "Third"));
        }

        private static void Change(global::Stride.Assets.ProjectUpgradeContext context, string step)
        {
            context.Project.SetProperty(step, "true");
            context.IsDirty = true;
            context.WriteFile(SideFile(context, step), step, System.Text.Encoding.UTF8);
        }
    }

    // Its step can't change two of the files it writes: one it reports itself, one whose move fails
    [PackageUpgrader(["UpgradeRecord.FileFails"], "1.0.0", "2.0.0")]
    public class FileFailingUpgrader : global::Stride.Assets.CodePackageUpgrader
    {
        public override void DeclareUpgrades(global::Stride.Assets.UpgradeRegistry registry)
            => registry.Project("2.0.0", "Write files", context =>
            {
                context.WriteFile(SideFile(context, "First"), "First", System.Text.Encoding.UTF8);
                context.UnitFailed(SideFile(context, "Reported"), new InvalidOperationException("The file can't be read"));
                context.MoveFile(SideFile(context, "Missing"), SideFile(context, "Moved"));
                context.WriteFile(SideFile(context, "Last"), "Last", System.Text.Encoding.UTF8);
            });
    }

    // Its reference bump fails, before its project step
    [PackageUpgrader(["UpgradeRecord.BumpFails"], "1.0.0", "2.0.0")]
    public class BumpFailingUpgrader : global::Stride.Assets.CodePackageUpgrader
    {
        public override void DeclareUpgrades(global::Stride.Assets.UpgradeRegistry registry)
            => registry.Project("2.0.0", "Change the project", _ => { });

        protected override bool UpgradeProjectReferences(UFile projectFullPath, ILogger log) => false;
    }

    private static string SideFile(global::Stride.Assets.ProjectUpgradeContext context, string step)
        => Path.Combine(context.ProjectFullPath.GetFullDirectory().ToOSPath(), step + ".txt");

    /// <summary>
    /// The runner of the session, except for the test upgraders that declare source migrations: it answers that they have
    /// some, and that the migration of some of them could not run, or throws. It keeps the projects it ran on.
    /// </summary>
    private sealed class CodeRunner(ICodeUpgradeRunner? inner) : ICodeUpgradeRunner
    {
        public static readonly ConcurrentBag<UFile> MigratedProjects = [];

        public bool HasSourceMigrations(IReadOnlyList<PendingCodeUpgrade> pending)
            => pending.Any(x => x.Upgrader is CodeRulesUpgrader or CodeNotRunUpgrader or CodeThrowingUpgrader) || (inner?.HasSourceMigrations(pending) ?? false);

        public bool Run(UFile? solutionPath, IReadOnlyList<PendingCodeUpgrade> pending, UpgradeBackup? backup, ILogger log)
        {
            if (pending.Any(x => x.Upgrader is CodeThrowingUpgrader))
                throw new InvalidOperationException("The workspace can't be opened");
            if (pending.Any(x => x.Upgrader is CodeNotRunUpgrader))
                return false;
            foreach (var item in pending)
                MigratedProjects.Add(item.ProjectFullPath);
            return inner?.Run(solutionPath, pending, backup, log) ?? true;
        }
    }

    private static readonly Lock RunnerLock = new();
    private readonly string directory = Directory.CreateTempSubdirectory("stride-upgrade-record-").FullName;

    public TestUpgradedVersionRecord()
    {
        PackageSessionPublicHelper.FindAndSetMSBuildVersion();

        // The runner is global: the one Stride.Assets registers keeps serving every other upgrader
        lock (RunnerLock)
        {
            RuntimeHelpers.RunModuleConstructor(typeof(global::Stride.Assets.StridePackageUpgrader).Module.ModuleHandle);
            if (CodeUpgradeRunner.Instance is not CodeRunner)
                CodeUpgradeRunner.Instance = new CodeRunner(CodeUpgradeRunner.Instance);
        }
    }

    public void Dispose()
    {
        Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public void AnUpgradedProjectRecordsTheVersion()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.Succeeds" Version="1.0.0" />""");

        LoadAndSave(game);

        Assert.Equal(new Dictionary<string, string> { ["Succeeds"] = "2.0.0" }, ReadRecords(game));
        Assert.Equal("2.0.0", ReadPackageVersion(game, "UpgradeRecord.Succeeds"));
        // Nothing goes in the package file: the project has none
        Assert.False(File.Exists(Path.ChangeExtension(game, Package.PackageFileExtension)));
    }

    [Fact]
    public void AnUpgraderOfSeveralPackagesRecordsOnce()
    {
        var game = WriteProject("Game", """
            <PackageReference Include="UpgradeRecord.Succeeds" Version="1.0.0" />
            <PackageReference Include="UpgradeRecord.Succeeds.Extra" Version="1.0.0" />
            """);

        LoadAndSave(game);

        Assert.Equal(new Dictionary<string, string> { ["Succeeds"] = "2.0.0" }, ReadRecords(game));
    }

    [Fact]
    public void TwoUpgradersRecordSideBySide()
    {
        var game = WriteProject("Game", """
            <PackageReference Include="UpgradeRecord.Succeeds" Version="1.0.0" />
            <PackageReference Include="MyPlugin" Version="1.0.0" />
            """);

        LoadAndSave(game);

        Assert.Equal(new Dictionary<string, string> { ["Succeeds"] = "2.0.0", ["MyPlugin"] = "3.0.0" }, ReadRecords(game));
    }

    [Fact]
    public void AnExistingRecordIsUpdated()
    {
        var game = WriteProject("Game", """
            <PackageReference Include="UpgradeRecord.Succeeds" Version="1.0.0" />
            <StridePackageUpgradeRecord Include="Succeeds" Version="1.0.0" />
            """);

        LoadAndSave(game);

        Assert.Equal(new Dictionary<string, string> { ["Succeeds"] = "2.0.0" }, ReadRecords(game));
    }

    [Fact]
    public void AProjectGettingThePackageThroughAProjectReferenceRecordsTheVersion()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.Succeeds" Version="1.0.0" />""");
        var windows = WriteProject("Game.Windows", """<ProjectReference Include="..\Game\Game.csproj" />""");

        // The source migration of a solution runs once it is restored (projects without SDK restore nothing)
        LoadAndSave(WriteSolution(game, windows), parameters => parameters.AutoCompileProjects = true);

        Assert.Equal(new Dictionary<string, string> { ["Succeeds"] = "2.0.0" }, ReadRecords(game));
        Assert.Equal(new Dictionary<string, string> { ["Succeeds"] = "2.0.0" }, ReadRecords(windows));
    }

    [Fact]
    public void AProjectGettingThePackageThroughAProjectReferenceHasItsCodeMigrated()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.CodeRules" Version="1.0.0" />""");
        var windows = WriteProject("Game.Windows", """<ProjectReference Include="..\Game\Game.csproj" />""");

        LoadAndSave(WriteSolution(game, windows), parameters => parameters.AutoCompileProjects = true);

        Assert.Contains(new UFile(game), CodeRunner.MigratedProjects);
        Assert.Contains(new UFile(windows), CodeRunner.MigratedProjects);
    }

    [Fact]
    public void AProjectAddedToASavedSessionHasItsCodeMigrated()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.Succeeds" Version="2.0.0" />""");
        // Restored on its own, as the solution file doesn't list it yet: it needs the SDK, and its package
        var extra = WriteProject("Extra", """<PackageReference Include="UpgradeRecord.CodeRules" Version="1.0.0" />""", sdk: true);
        WriteLocalFeed(("UpgradeRecord.CodeRules", "1.0.0"));
        var parameters = new PackageLoadParameters { AutoCompileProjects = true, LoadAssemblyReferences = false };
        var result = new PackageSessionResult();
        PackageSession.Load(WriteSolution(game), result, parameters);
        using var session = result.Session!;

        // The solution file lists the project once the session is saved
        session.AddExistingProject(new UFile(extra), result, parameters);
        session.Save(result);

        Assert.DoesNotContain(result.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains("is stopped", StringComparison.Ordinal));
        Assert.Contains(new UFile(extra), CodeRunner.MigratedProjects);
        Assert.Equal(new Dictionary<string, string> { ["UpgradeRecord.CodeRules"] = "2.0.0" }, ReadRecords(extra));
        Assert.Contains(result.Messages, x => x.Text.StartsWith("Extra.csproj: UpgradeRecord.CodeRules 1.0.0 to 2.0.0", StringComparison.Ordinal));
    }

    [Fact]
    public void AProjectThatReferencesARefusedProjectHasNoCodeMigrated()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.CodeRules" Version="1.0.0" />""");
        var windows = WriteProject("Game.Windows", """<ProjectReference Include="..\Game\Game.csproj" />""");

        LoadAndSave(WriteSolution(game, windows), parameters =>
        {
            parameters.AutoCompileProjects = true;
            parameters.PackageUpgradeRequested = (package, _)
                => package.Meta.Name == "Game" ? PackageUpgradeRequestedAnswer.DoNotUpgrade : PackageUpgradeRequestedAnswer.Upgrade;
        });

        Assert.DoesNotContain(new UFile(windows), CodeRunner.MigratedProjects);
    }

    [Fact]
    public void AnUnrestoredSolutionStopsTheUpgrade()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.CodeRules" Version="1.0.0" />""");
        var windows = WriteProject("Game.Windows", """<ProjectReference Include="..\Game\Game.csproj" />""");
        var before = ReadFiles(game, windows);

        // Without its restore, the source migration of the solution can't run
        var result = LoadAndSave(WriteSolution(game, windows), out var report);

        Assert.Equal(before, ReadFiles(game, windows));
        // The executable is migrated with its game library, so it stops for the same reason
        foreach (var (project, name) in new[] { (game, "Game"), (windows, "Game.Windows") })
        {
            AssertError(result, $"The upgrade of [{name}] is stopped: the solution [Game.slnx] isn't restored");
            Assert.Equal(new Dictionary<string, UpgradeStepStatus> { ["Continue the upgrade"] = UpgradeStepStatus.Failed },
                StepStatuses(report, project, "UpgradeRecord.CodeRules"));
            Assert.True(File.Exists(ReportFile(project)));
        }
    }

    [Fact]
    public void AnUnrestoredSolutionIsUpgradedWhenNoSourceMigrationApplies()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.Succeeds" Version="1.0.0" />""");
        var windows = WriteProject("Game.Windows", """<ProjectReference Include="..\Game\Game.csproj" />""");

        LoadAndSave(WriteSolution(game, windows));

        Assert.Equal(new Dictionary<string, string> { ["Succeeds"] = "2.0.0" }, ReadRecords(game));
        Assert.Equal(new Dictionary<string, string> { ["Succeeds"] = "2.0.0" }, ReadRecords(windows));
    }

    [Fact]
    public void AProjectAddedBeforeTheSolutionIsSavedIsUpgraded()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.CodeRules" Version="1.0.0" />""");

        // As a new session does: it names its solution, and its first save writes the file
        var solution = Path.Combine(directory, "Game.slnx");
        var result = new LoggerResult();
        using (var session = new PackageSession { SolutionPath = solution })
        {
            session.AddExistingProject(game, result, new PackageLoadParameters { AutoCompileProjects = false, LoadAssemblyReferences = false });
            Assert.False(File.Exists(solution));
            session.Save(result);
        }

        // The source migration ran on the project alone
        Assert.Contains(new UFile(game), CodeRunner.MigratedProjects);
        Assert.Equal(new Dictionary<string, string> { ["UpgradeRecord.CodeRules"] = "2.0.0" }, ReadRecords(game));
        Assert.DoesNotContain(result.Messages, x => x.Type == LogMessageType.Error);
    }

    [Theory]
    [InlineData("UpgradeRecord.CodeNotRun", "The upgrade of [Game] is stopped: its source code could not be migrated")]
    [InlineData("UpgradeRecord.CodeThrows", "The upgrade of [Game] is stopped: the source-code migration failed")]
    public void ASourceMigrationThatCantRunStopsTheUpgrade(string packageName, string error)
    {
        var game = WriteProject("Game", $"""<PackageReference Include="{packageName}" Version="1.0.0" />""");
        var before = ReadFiles(game);

        var result = LoadAndSave(game);

        Assert.Equal(before, ReadFiles(game));
        AssertError(result, error);
    }

    [Fact]
    public void AProjectMigrationThatFailsFailsTheUpgrade()
    {
        var game = WriteProject("Game", """
            <PackageReference Include="UpgradeRecord.ProjectFails" Version="1.0.0" />
            <PackageReference Include="MyPlugin" Version="1.0.0" />
            """);
        var windows = WriteProject("Game.Windows", """<ProjectReference Include="..\Game\Game.csproj" />""");
        var windowsBefore = ReadFiles(windows);

        var result = LoadAndSave(WriteSolution(game, windows), parameters => parameters.AutoCompileProjects = true);

        // One failing upgrader fails the whole package: no upgrader of it is recorded
        Assert.Empty(ReadRecords(game));
        AssertError(result, "Unable to upgrade project [Game.csproj]");
        // The project that references it is not upgraded at all
        Assert.Equal(windowsBefore, ReadFiles(windows));
        AssertError(result, "The upgrade of [Game.Windows] is stopped: the project it references, [Game], could not be upgraded");
    }

    [Fact]
    public void AProjectStepThatThrowsKeepsTheStepsBeforeItAndStopsTheProject()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.SecondStepThrows" Version="1.0.0" />""");
        var gameDirectory = Path.GetDirectoryName(game)!;

        var result = LoadAndSave(game, out var report);

        var properties = XDocument.Load(game).Descendants("PropertyGroup").Elements().Select(x => x.Name.LocalName).ToList();
        Assert.Contains("First", properties);
        Assert.DoesNotContain("Second", properties);
        Assert.DoesNotContain("Third", properties);
        Assert.True(File.Exists(Path.Combine(gameDirectory, "First.txt")));
        Assert.False(File.Exists(Path.Combine(gameDirectory, "Second.txt")));
        Assert.False(File.Exists(Path.Combine(gameDirectory, "Third.txt")));
        Assert.Empty(ReadRecords(game));
        AssertError(result, "UpgradeRecord.SecondStepThrows-2.0.0-P2: Unable to upgrade project [Game.csproj]: the step \"Second\" failed");
        Assert.Equal(new Dictionary<string, UpgradeStepStatus>
        {
            ["UpgradeRecord.SecondStepThrows-2.0.0-P1"] = UpgradeStepStatus.Done,
            ["UpgradeRecord.SecondStepThrows-2.0.0-P2"] = UpgradeStepStatus.Failed,
            ["UpgradeRecord.SecondStepThrows-2.0.0-P3"] = UpgradeStepStatus.NotRun,
        }, StepStatuses(report, game, "UpgradeRecord.SecondStepThrows"));
    }

    [Fact]
    public void AFileAProjectStepCantChangeIsLeftAndTheOthersChange()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.FileFails" Version="1.0.0" />""");
        var gameDirectory = Path.GetDirectoryName(game)!;

        var result = LoadAndSave(game, out var report);

        Assert.Equal("First", File.ReadAllText(Path.Combine(gameDirectory, "First.txt")));
        Assert.Equal("Last", File.ReadAllText(Path.Combine(gameDirectory, "Last.txt")));
        Assert.False(File.Exists(Path.Combine(gameDirectory, "Moved.txt")));
        Assert.Contains(result.Messages, x => x.Type == LogMessageType.Warning && x.Text.Contains("Could not upgrade [Reported.txt]", StringComparison.Ordinal));
        Assert.Contains(result.Messages, x => x.Type == LogMessageType.Warning && x.Text.Contains("Could not upgrade [Missing.txt]", StringComparison.Ordinal));
        // The upgrade completes
        Assert.Equal(new Dictionary<string, string> { ["UpgradeRecord.FileFails"] = "2.0.0" }, ReadRecords(game));
        var step = Assert.Single(report.Find(new UFile(game), "UpgradeRecord.FileFails")!.Steps);
        Assert.Equal(UpgradeStepStatus.Partial, step.Status);
        Assert.Equal([Path.Combine(gameDirectory, "First.txt"), Path.Combine(gameDirectory, "Last.txt")], step.ChangedFiles);
        Assert.Equal([Path.Combine(gameDirectory, "Reported.txt"), Path.Combine(gameDirectory, "Missing.txt")], step.FailedUnits.Select(x => x.Path));
    }

    [Fact]
    public void ACleanUpgradeLogsOneLineAndWritesNoReportFile()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.Succeeds" Version="1.0.0" />""");

        var result = LoadAndSave(game);

        Assert.Contains(result.Messages, x => x.Type == LogMessageType.Info && x.Text == "Game.csproj: Succeeds 1.0.0 to 2.0.0: done");
        Assert.False(File.Exists(ReportFile(game)));
    }

    [Fact]
    public void AnUpgradeThatLeftItemsWritesThemInTheReportFile()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.SecondStepThrows" Version="1.0.0" />""");

        var result = LoadAndSave(game);

        Assert.Contains(result.Messages, x => x.Type == LogMessageType.Info && x.Text == "Game.csproj: UpgradeRecord.SecondStepThrows 1.0.0 to 2.0.0: stopped, 2 items left");
        Assert.Contains(result.Messages, x => x.Type == LogMessageType.Info && x.Text == "  not run  UpgradeRecord.SecondStepThrows-2.0.0-P3 Third");
        Assert.Contains(result.Messages, x => x.Type == LogMessageType.Warning && x.Text.Contains("The upgrade of [Game.csproj] left items to do by hand", StringComparison.Ordinal));
        var text = File.ReadAllText(ReportFile(game));
        Assert.StartsWith("# Upgrade report: Game.csproj\n", text);
        Assert.Contains(" · UpgradeRecord.SecondStepThrows 1.0.0 → 2.0.0 · stopped, 2 items left\n", text);
        Assert.Contains("### Failed\n\n- [ ] `UpgradeRecord.SecondStepThrows-2.0.0-P2` Second\n", text);
        Assert.Contains("### Not run\n\n- [ ] `UpgradeRecord.SecondStepThrows-2.0.0-P3` Third\n", text);
        Assert.Contains("<summary>Done (1)</summary>\n\n- `UpgradeRecord.SecondStepThrows-2.0.0-P1` First (2 files)\n", text);
    }

    [Fact]
    public void AFileAStepLeftIsInTheReportFile()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.FileFails" Version="1.0.0" />""");

        LoadAndSave(game);

        // Each file with why it was left; one that threw with the first frames of its stack
        var text = File.ReadAllText(ReportFile(game));
        Assert.Contains("### Partial\n\n- [ ] `UpgradeRecord.FileFails-2.0.0-P1` Write files\n  - `Reported.txt` is left unchanged: The file can't be read\n  - `Missing.txt` is left unchanged: ", text);
        Assert.Matches(@"`Missing\.txt` is left unchanged: [^\n]+\n    ```\n(    at [^\n]+\n){1,3}    ```\n", text);
    }

    [Fact]
    public void ACleanUpgradeAddsItsSectionToAnExistingReportFile()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.Succeeds" Version="1.0.0" />""");
        File.WriteAllText(ReportFile(game), "# Upgrade report: Game.csproj\n\nIntro.\n\n## 2026-11-21 14:32 · Succeeds 0.9.0 → 1.0.0 · 1 item left\n");

        LoadAndSave(game);

        var text = File.ReadAllText(ReportFile(game));
        Assert.Matches(@"^# Upgrade report: Game\.csproj\n\nIntro\.\n\n## [0-9-]+ [0-9:]+ · Succeeds 1\.0\.0 → 2\.0\.0 · done\n\nNo step\.\n\n## 2026-11-21 14:32 · Succeeds 0\.9\.0 → 1\.0\.0 · 1 item left\n$", text);
    }

    [Fact]
    public void ARefusedUpgradeWritesNoReportFile()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.SecondStepThrows" Version="1.0.0" />""");

        LoadAndSave(game, parameters => parameters.PackageUpgradeRequested = (_, _) => PackageUpgradeRequestedAnswer.DoNotUpgrade);

        Assert.False(File.Exists(ReportFile(game)));
    }

    [Fact]
    public void AFailedReferenceBumpIsReportedWithoutStep()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.BumpFails" Version="1.0.0" />""");

        LoadAndSave(game, out var report);

        Assert.Equal(new Dictionary<string, UpgradeStepStatus>
        {
            ["UpgradeRecord.BumpFails-2.0.0-P1"] = UpgradeStepStatus.NotRun,
            ["Update the package references"] = UpgradeStepStatus.Failed,
        }, StepStatuses(report, game, "UpgradeRecord.BumpFails"));
    }

    [Fact]
    public void AProjectFileThatCantBeWrittenIsReportedWithoutStep()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.SecondStepThrows" Version="1.0.0" />""");
        File.SetAttributes(game, FileAttributes.ReadOnly);
        try
        {
            LoadAndSave(game, out var report);

            Assert.Equal(new Dictionary<string, UpgradeStepStatus>
            {
                ["UpgradeRecord.SecondStepThrows-2.0.0-P1"] = UpgradeStepStatus.NotRun,
                ["UpgradeRecord.SecondStepThrows-2.0.0-P2"] = UpgradeStepStatus.NotRun,
                ["UpgradeRecord.SecondStepThrows-2.0.0-P3"] = UpgradeStepStatus.NotRun,
                ["Update the package references"] = UpgradeStepStatus.Failed,
            }, StepStatuses(report, game, "UpgradeRecord.SecondStepThrows"));
            Assert.NotNull(report.Find(new UFile(game), "UpgradeRecord.SecondStepThrows")!.Steps.Single(x => x.Id is null).Error);
        }
        finally
        {
            File.SetAttributes(game, FileAttributes.Normal);
        }
    }

    [Theory]
    [InlineData("UpgradeRecord.Fails", "Error while upgrading package [Game]")]
    [InlineData("UpgradeRecord.Throws", "Error while pre-loading package")]
    public void AFailedUpgradeRecordsNothing(string packageName, string error)
    {
        var game = WriteProject("Game", $"""<PackageReference Include="{packageName}" Version="1.0.0" />""");

        var result = LoadAndSave(game, out var report);

        Assert.Empty(ReadRecords(game));
        AssertError(result, error);
        // The report says what failed, whether the upgrader returned false or threw
        Assert.Equal(new Dictionary<string, UpgradeStepStatus> { ["Upgrade the package before its assets load"] = UpgradeStepStatus.Failed },
            StepStatuses(report, game, packageName));
    }

    [Fact]
    public void ARefusedUpgradeRecordsNothing()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.Succeeds" Version="1.0.0" />""");

        LoadAndSave(game, parameters => parameters.PackageUpgradeRequested = (_, _) => PackageUpgradeRequestedAnswer.DoNotUpgrade);

        Assert.Empty(ReadRecords(game));
        Assert.Equal("1.0.0", ReadPackageVersion(game, "UpgradeRecord.Succeeds"));
    }

    [Fact]
    public void ARefusedProjectIsNotMigratedByTheLoadOfTheMissingReferences()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.CodeRules" Version="1.0.0" />""");
        var before = ReadFiles(game);

        var parameters = new PackageLoadParameters
        {
            AutoCompileProjects = false,
            LoadAssemblyReferences = false,
            PackageUpgradeRequested = (_, _) => PackageUpgradeRequestedAnswer.DoNotUpgrade,
        };
        var result = new PackageSessionResult();
        PackageSession.Load(game, result, parameters);
        Assert.NotNull(result.Session);
        using var session = result.Session;
        // As Game Studio does once the session is loaded: without the question, the answer given holds
        session.LoadMissingReferences(new LoggerResult());
        session.Save(new LoggerResult());

        Assert.DoesNotContain(new UFile(game), CodeRunner.MigratedProjects);
        Assert.Equal(before, ReadFiles(game));
    }

    [Fact]
    public void AProjectThatReferencesARefusedProjectIsNotUpgraded()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.Succeeds" Version="1.0.0" />""");
        var windows = WriteProject("Game.Windows", """<ProjectReference Include="..\Game\Game.csproj" />""");
        var windowsBefore = ReadFiles(windows);

        var result = LoadAndSave(WriteSolution(game, windows), parameters => parameters.PackageUpgradeRequested = (package, _)
            => package.Meta.Name == "Game" ? PackageUpgradeRequestedAnswer.DoNotUpgrade : PackageUpgradeRequestedAnswer.Upgrade);

        Assert.Equal(windowsBefore, ReadFiles(windows));
        AssertError(result, "The upgrade of [Game.Windows] is stopped: the project it references, [Game], is not upgraded");
    }

    [Fact]
    public void ALoadCancelledDuringTheUpgradeQuestionAsksNoMore()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.Succeeds" Version="1.0.0" />""");
        var tools = WriteProject("Game.Tools", """<PackageReference Include="UpgradeRecord.Succeeds" Version="1.0.0" />""");
        var before = ReadFiles(game, tools);

        // As the progress window's Cancel under the question of the first project: the question closes, the load is cancelled
        using var cancel = new CancellationTokenSource();
        var asked = new List<string>();
        var parameters = new PackageLoadParameters
        {
            AutoCompileProjects = false,
            LoadAssemblyReferences = false,
            CancelToken = cancel.Token,
            PackageUpgradeRequested = (package, _) =>
            {
                asked.Add(package.Meta.Name);
                cancel.Cancel();
                return PackageUpgradeRequestedAnswer.Upgrade;
            },
        };
        var result = new PackageSessionResult();
        PackageSession.Load(WriteSolution(game, tools), result, parameters);
        result.Session?.Dispose();

        Assert.Single(asked);
        Assert.Equal(before, ReadFiles(game, tools));
    }

    [Fact]
    public void AProjectThatFailsAfterItsAssetsLoadedIsNotRecorded()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.FailsAfterAssets" Version="1.0.0" />""");
        var windows = WriteProject("Game.Windows", """<ProjectReference Include="..\Game\Game.csproj" />""");

        var result = LoadAndSave(WriteSolution(game, windows));

        Assert.Empty(ReadRecords(game));
        Assert.Empty(ReadRecords(windows));
        // Game.Windows fails its own last step, or is stopped by Game's, depending on the order the assets load in
        AssertError(result, "Error while upgrading package [Game] for [UpgradeRecord.FailsAfterAssets]");
        AssertError(result, "[Game.Windows]");
    }

    [Fact]
    public void AFailedUpgradePutsEveryFileOfTheProjectBack()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.SecondStepThrows" Version="1.0.0" />""");
        var gameDirectory = Path.GetDirectoryName(game)!;
        var before = ReadFiles(game);

        var result = LoadAndSave(game, out var report, parameters => parameters.BackupBeforeUpgrade = true);

        Assert.Equal(before, ReadFiles(game));
        Assert.False(File.Exists(Path.Combine(gameDirectory, "First.txt")));
        Assert.Equal(UpgradeOutcome.Reverted, report.Projects.Single().Outcome);
        Assert.Contains(result.Messages, x => x.Type == LogMessageType.Warning && x.Text.Contains("every file of the project is put back as it was before the upgrade", StringComparison.Ordinal));
        // The report outlives the files it explains
        Assert.Contains(" · reverted, 2 items left\n", File.ReadAllText(ReportFile(game)), StringComparison.Ordinal);
    }

    [Fact]
    public void AFailedUpgradeWithoutBackupLeavesTheFilesAndSaysSo()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.SecondStepThrows" Version="1.0.0" />""");

        var result = LoadAndSave(game, out var report);

        Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(game)!, "First.txt")));
        Assert.Empty(ReadRecords(game));
        Assert.Equal(UpgradeOutcome.Stopped, report.Projects.Single().Outcome);
        AssertError(result, "The upgrade of [Game] failed, and it has no backup to put its files back from");
    }

    [Fact]
    public void AKeptUpgradeRecordsTheStepsLeft()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.SecondStepThrows" Version="1.0.0" />""");
        ProjectUpgradeReport? asked = null;

        LoadAndSave(game, out var report, parameters =>
        {
            parameters.BackupBeforeUpgrade = true;
            parameters.PackageUpgradeFailed = (_, project) =>
            {
                asked = project;
                return PackageUpgradeFailedAnswer.Keep;
            };
        });

        Assert.Same(report.Projects.Single(), asked);
        Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(game)!, "First.txt")));
        Assert.Equal(new Dictionary<string, string> { ["UpgradeRecord.SecondStepThrows"] = "2.0.0" }, ReadRecords(game));
        Assert.Equal("UpgradeRecord.SecondStepThrows-2.0.0-P2;UpgradeRecord.SecondStepThrows-2.0.0-P3", ReadUnfinished(game, "UpgradeRecord.SecondStepThrows"));
        Assert.Contains(" · kept, 2 items left\n", File.ReadAllText(ReportFile(game)), StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnfinishedUpgradeBlocksTheNextOne()
    {
        var game = WriteProject("Game", """
            <PackageReference Include="UpgradeRecord.Succeeds" Version="1.0.0" />
            <StridePackageUpgradeRecord Include="Succeeds" Version="1.0.0" Unfinished="Succeeds-1.0.0-P2" />
            """);
        var before = ReadFiles(game);

        var result = LoadAndSave(game);

        Assert.Equal(before, ReadFiles(game));
        AssertError(result, "The last upgrade of [Game] was kept unfinished, with Succeeds-1.0.0-P2 to finish by hand");
    }

    [Fact]
    public void AnUnfinishedUpgradeMarkedAsFinishedLetsTheNextOneRun()
    {
        var game = WriteProject("Game", """
            <PackageReference Include="UpgradeRecord.Succeeds" Version="1.0.0" />
            <StridePackageUpgradeRecord Include="Succeeds" Version="1.0.0" Unfinished="Succeeds-1.0.0-P2" />
            """);
        IReadOnlyList<string>? asked = null;

        LoadAndSave(game, out var report, parameters => parameters.UnfinishedUpgradeFound = (_, steps) =>
        {
            asked = steps;
            return true;
        });

        Assert.Equal(["Succeeds-1.0.0-P2"], asked);
        Assert.Equal(new Dictionary<string, string> { ["Succeeds"] = "2.0.0" }, ReadRecords(game));
        Assert.Null(ReadUnfinished(game, "Succeeds"));
        Assert.Equal(["Succeeds-1.0.0-P2"], report.Projects.Single().ContinuedFromUnfinished);
    }

    [Fact]
    public void TheBackupPutsBackTheFilesItHas()
    {
        var root = Directory.CreateDirectory(Path.Combine(directory, "Solution")).FullName;
        var inside = Path.Combine(root, "Game", "Player.cs");
        var outside = Path.Combine(directory, "Shared", "Lib.cs");
        var created = Path.Combine(root, "Game", "New.cs");
        foreach (var file in new[] { inside, outside })
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, "Before");
        }
        var backup = new UpgradeBackup(root, DateTime.Now);

        foreach (var file in new[] { inside, outside, created })
        {
            backup.Snapshot(file);
            File.WriteAllText(file, "After");
        }
        Assert.True(backup.HasChanges(x => x == outside));

        Assert.Empty(backup.Restore(_ => true));
        Assert.Equal("Before", File.ReadAllText(inside));
        Assert.Equal("Before", File.ReadAllText(outside));
        Assert.False(File.Exists(created));
        Assert.False(backup.HasChanges(_ => true));
    }

    [Fact]
    public void AnImportedRecordIsUpdatedInTheProject()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.Succeeds" Version="1.0.0" />""", sdk: true);
        // Shared by every project under it
        var props = Path.Combine(Path.GetDirectoryName(game)!, "Directory.Build.props");
        File.WriteAllText(props, """
            <Project>
              <ItemGroup>
                <StridePackageUpgradeRecord Include="Succeeds" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """);
        var propsBefore = ReadFiles(props);

        LoadAndSave(game);

        Assert.Equal(propsBefore, ReadFiles(props));
        Assert.Equal(new Dictionary<string, string> { ["Succeeds"] = "2.0.0" }, ReadRecords(game));

        // A later upgrade changes the same Update
        VSProjectHelper.SetUpgradeRecord(game, "Succeeds", "3.0.0");
        Assert.Equal(new Dictionary<string, string> { ["Succeeds"] = "3.0.0" }, ReadRecords(game));
        Assert.Equal(propsBefore, ReadFiles(props));
    }

    private string WriteProject(string name, string items, bool sdk = false)
    {
        var path = Path.Combine(directory, name, name + ".csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var project = new XElement("Project",
            new XElement("PropertyGroup", new XElement("TargetFramework", "net10.0")),
            new XElement("ItemGroup", XElement.Parse($"<ItemGroup>{items}</ItemGroup>").Elements()));
        if (sdk)
            project.SetAttributeValue("Sdk", "Microsoft.NET.Sdk");
        project.Save(path);
        return path;
    }

    // A nuget.config for the projects of the test: a local feed with these empty packages, and nothing else
    private void WriteLocalFeed(params (string Id, string Version)[] packages)
    {
        var feed = Directory.CreateDirectory(Path.Combine(directory, "feed")).FullName;
        foreach (var (id, version) in packages)
        {
            using var archive = ZipFile.Open(Path.Combine(feed, $"{id}.{version}.nupkg"), ZipArchiveMode.Create);
            using var nuspec = archive.CreateEntry($"{id}.nuspec").Open();
            XNamespace ns = "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd";
            new XDocument(new XElement(ns + "package", new XElement(ns + "metadata",
                new XElement(ns + "id", id), new XElement(ns + "version", version), new XElement(ns + "authors", "Stride"), new XElement(ns + "description", id))))
                .Save(nuspec);
        }
        new XDocument(new XElement("configuration",
            new XElement("config", new XElement("add", new XAttribute("key", "globalPackagesFolder"), new XAttribute("value", Path.Combine(directory, "packages")))),
            new XElement("packageSources", new XElement("clear"), new XElement("add", new XAttribute("key", "feed"), new XAttribute("value", feed)))))
            .Save(Path.Combine(directory, "nuget.config"));
    }

    private string WriteSolution(params string[] projects)
    {
        var path = Path.Combine(directory, "Game.slnx");
        var solution = new XElement("Solution", projects.Select(x => new XElement("Project", new XAttribute("Path", Path.GetRelativePath(directory, x).Replace('\\', '/')))));
        solution.Save(path);
        return path;
    }

    private static PackageSessionResult LoadAndSave(string path, Action<PackageLoadParameters>? configure = null)
        => LoadAndSave(path, out _, configure);

    private static PackageSessionResult LoadAndSave(string path, out UpgradeReport report, Action<PackageLoadParameters>? configure = null)
    {
        var parameters = new PackageLoadParameters { AutoCompileProjects = false, LoadAssemblyReferences = false };
        configure?.Invoke(parameters);
        var result = new PackageSessionResult();
        PackageSession.Load(path, result, parameters);
        Assert.NotNull(result.Session);
        using var session = result.Session;
        session.Save(result);
        report = session.UpgradeReport;
        return result;
    }

    // The status of each step of the upgrader on the project, by ID
    private static Dictionary<string, UpgradeStepStatus> StepStatuses(UpgradeReport report, string project, string upgrader)
        => report.Find(new UFile(project), upgrader)!.Steps.ToDictionary(x => x.Id ?? x.Title, x => x.Status);

    // The records as the file writes them (an Include or an Update), not as the project evaluates
    private static Dictionary<string, string> ReadRecords(string file)
        => XDocument.Load(file).Descendants(PackageUpgrader.UpgradeRecordItem)
            .ToDictionary(x => (string?)x.Attribute("Include") ?? (string)x.Attribute("Update")!, x => (string?)x.Attribute("Version") ?? "");

    private static string? ReadUnfinished(string file, string upgrader)
        => XDocument.Load(file).Descendants(PackageUpgrader.UpgradeRecordItem).Single(x => (string?)x.Attribute("Include") == upgrader).Attribute(PackageUpgrader.UnfinishedMetadata)?.Value;

    private static string? ReadPackageVersion(string project, string package)
        => XDocument.Load(project).Descendants("PackageReference").Single(x => (string?)x.Attribute("Include") == package).Attribute("Version")?.Value;

    private static string[] ReadFiles(params string[] files) => files.Select(File.ReadAllText).ToArray();

    private static string ReportFile(string project) => Path.Combine(Path.GetDirectoryName(project)!, UpgradeReportWriter.FileName);

    // An error that holds all the texts
    private static void AssertError(LoggerResult result, params string[] texts)
        => Assert.True(result.Messages.Any(x => x.Type == LogMessageType.Error && texts.All(text => x.Text.Contains(text, StringComparison.Ordinal))),
            $"No error with \"{string.Join("\", \"", texts)}\" in:\n{string.Join("\n", result.Messages.Select(x => $"{x.Type}: {x.Text}"))}");
}
