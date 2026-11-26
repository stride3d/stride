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
    public class SucceedingUpgrader : SucceedingUpgraderBase
    {
        // A step below the version of every test project, which an earlier upgrade can leave unfinished
        public override IReadOnlyList<UpgradeStepInfo> GetUpgradeSteps(PackageVersion fromVersion)
            => fromVersion < new PackageVersion("1.0.0") ? [new UpgradeStepInfo("Succeeds-1.0.0-P2", "Move the settings", "https://doc/1.0.html#succeeds-1.0.0-p2")] : [];
    }

    // A step below the version of every test project, which a retry finishes
    [PackageUpgrader(["UpgradeRecord.Retried"], "1.0.0", "2.0.0", Name = "Retried")]
    public class RetriedUpgrader : SucceedingUpgraderBase
    {
        public override IReadOnlyList<UpgradeStepInfo> GetUpgradeSteps(PackageVersion fromVersion)
            => fromVersion < new PackageVersion("1.0.0") ? [new UpgradeStepInfo("Retried-1.0.0-P2", "Move the settings", null)] : [];

        public override bool CanRetryStep(string id) => true;

        public override void RetrySteps(PackageSession session, SolutionProject project, UpgraderReport report, ILogger log)
        {
            foreach (var step in report.Steps)
                step.SetRan();
        }
    }

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

    private const string SecondStepBlocked = "UpgradeRecord.SecondStepBlocked";

    // Each project step sets a property and writes a file next to the project; the second one throws while the project has a
    // Blocked.txt file
    [PackageUpgrader([SecondStepBlocked], "1.0.0", "2.0.0")]
    public class SecondStepBlockedUpgrader : global::Stride.Assets.CodePackageUpgrader
    {
        public override void DeclareUpgrades(global::Stride.Assets.UpgradeRegistry registry)
        {
            registry.Project("2.0.0", "First", context => Change(context, "First"));
            registry.Project("2.0.0", "Second", context =>
            {
                if (File.Exists(Path.Combine(context.ProjectFullPath.GetFullDirectory().ToOSPath(), "Blocked.txt")))
                    throw new InvalidOperationException("The second step is blocked");
                Change(context, "Second");
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

    private const string RetryHook = "UpgradeRecord.RetryHook";

    // Its first project step writes a file next to the project; the second one throws while the project has a Blocked.txt file,
    // and calls DuringSecondStep otherwise
    [PackageUpgrader([RetryHook], "1.0.0", "2.0.0")]
    public class RetryHookUpgrader : global::Stride.Assets.CodePackageUpgrader
    {
        public static Action? DuringSecondStep;

        public override void DeclareUpgrades(global::Stride.Assets.UpgradeRegistry registry)
        {
            registry.Project("2.0.0", "First", context => context.WriteFile(SideFile(context, "First"), "First", System.Text.Encoding.UTF8));
            registry.Project("2.0.0", "Second", context =>
            {
                if (File.Exists(Path.Combine(context.ProjectFullPath.GetFullDirectory().ToOSPath(), "Blocked.txt")))
                    throw new InvalidOperationException("The second step is blocked");
                DuringSecondStep?.Invoke();
            });
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
        var solution = WriteSolution(game, windows);
        var result = LoadAndSave(solution, out var report);

        Assert.Equal(before, ReadFiles(game, windows));
        // The executable is migrated with its game library, so it stops for the same reason
        foreach (var (project, name) in new[] { (game, "Game"), (windows, "Game.Windows") })
        {
            AssertError(result, $"The upgrade of [{name}] is stopped: the solution [Game.slnx] isn't restored");
            Assert.Equal(new Dictionary<string, UpgradeStepStatus> { ["Continue the upgrade"] = UpgradeStepStatus.Failed },
                StepStatuses(report, project, "UpgradeRecord.CodeRules"));
            Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(project)!, UpgradeReportWriter.FileName)));
        }
        // One report next to the solution, with a section of the upgrade and a subsection per project
        var text = File.ReadAllText(Path.Combine(Path.GetDirectoryName(solution)!, UpgradeReportWriter.FileName));
        Assert.StartsWith("# Upgrade report: Game.slnx\n\nUpgrades of this solution left", text, StringComparison.Ordinal);
        Assert.Single(text.Split('\n'), x => x.StartsWith("## ", StringComparison.Ordinal));
        Assert.Contains("\n### Game · 1 item left\n", text, StringComparison.Ordinal);
        Assert.Contains("\n### Game.Windows · 1 item left\n", text, StringComparison.Ordinal);
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

    [Fact]
    public void ARevertedUpgradeOfAnAddedProjectKeepsTheUpgradeOfTheSession()
    {
        var game = WriteProject("Game", $"""<PackageReference Include="{SecondStepBlocked}" Version="1.0.0" />""");
        var extra = WriteProject("Extra", """<PackageReference Include="UpgradeRecord.SecondStepThrows" Version="1.0.0" />""");
        var extraBefore = ReadFiles(extra);
        var parameters = new PackageLoadParameters { AutoCompileProjects = false, LoadAssemblyReferences = false, BackupBeforeUpgrade = true };
        var result = new LoggerResult();
        using var session = new PackageSession { SolutionPath = Path.Combine(directory, "Game.slnx") };
        session.AddExistingProject(game, result, parameters);
        var gameUpgraded = ReadFiles(game);
        var gamePackage = session.Packages.Single(x => x.Meta.Name == "Game");
        Assert.True(gamePackage.IsDirty);

        // Not saved yet: the backup still has the files of the upgrade of Game
        session.AddExistingProject(extra, result, parameters);

        Assert.Equal(extraBefore, ReadFiles(extra));
        Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(extra)!, "First.txt")));
        Assert.Equal(gameUpgraded, ReadFiles(game));
        Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(game)!, "First.txt")));
        Assert.True(gamePackage.IsDirty);
        Assert.Equal(UpgradeOutcome.Completed, session.UpgradeReport.Projects.Single(x => x.ProjectFullPath == new UFile(game)).Outcome);
        Assert.Equal(UpgradeOutcome.Reverted, session.UpgradeReport.Projects.Single(x => x.ProjectFullPath == new UFile(extra)).Outcome);
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
        // Without a backup to put the files back from, the upgrade is kept
        Assert.Equal("UpgradeRecord.SecondStepThrows-2.0.0-P2;UpgradeRecord.SecondStepThrows-2.0.0-P3", ReadUnfinished(game, "UpgradeRecord.SecondStepThrows"));
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

        Assert.Contains(result.Messages, x => x.Type == LogMessageType.Info && x.Text == "Game.csproj: UpgradeRecord.SecondStepThrows 1.0.0 to 2.0.0: kept, 2 items left");
        Assert.Contains(result.Messages, x => x.Type == LogMessageType.Info && x.Text == "  not run  UpgradeRecord.SecondStepThrows-2.0.0-P3 Third");
        Assert.Contains(result.Messages, x => x.Type == LogMessageType.Warning && x.Text.Contains("The upgrade of [Game.csproj] left items to do by hand", StringComparison.Ordinal));
        var text = File.ReadAllText(ReportFile(game));
        Assert.StartsWith("# Upgrade report: Game.csproj\n", text);
        Assert.Contains(" · UpgradeRecord.SecondStepThrows 1.0.0 → 2.0.0 · kept, 2 items left\n", text);
        Assert.Contains("#### Failed\n\n- [ ] `UpgradeRecord.SecondStepThrows-2.0.0-P2` Second\n", text);
        Assert.Contains("#### Not run\n\n- [ ] `UpgradeRecord.SecondStepThrows-2.0.0-P3` Third\n", text);
        Assert.Contains("<summary>Done (1)</summary>\n\n- `UpgradeRecord.SecondStepThrows-2.0.0-P1` First (2 files)\n", text);
    }

    [Fact]
    public void AFileAStepLeftIsInTheReportFile()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.FileFails" Version="1.0.0" />""");

        LoadAndSave(game);

        // Each file with why it was left; one that threw with the first frames of its stack
        var text = File.ReadAllText(ReportFile(game));
        Assert.Contains("#### Partial\n\n- [ ] `UpgradeRecord.FileFails-2.0.0-P1` Write files\n  - `Reported.txt` is left unchanged: The file can't be read\n  - `Missing.txt` is left unchanged: ", text);
        Assert.Matches(@"`Missing\.txt` is left unchanged: [^\n]+\n    ```\n(    at [^\n]+\n){1,3}    ```\n", text);
    }

    [Fact]
    public void ACleanUpgradeAddsItsSectionToAnExistingReportFile()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.Succeeds" Version="1.0.0" />""");
        File.WriteAllText(ReportFile(game), "# Upgrade report: Game.csproj\n\nIntro.\n\n## 2026-11-21 14:32 · Succeeds 0.9.0 → 1.0.0 · 1 item left\n");

        LoadAndSave(game);

        var text = File.ReadAllText(ReportFile(game));
        Assert.Matches(@"^# Upgrade report: Game\.csproj\n\nIntro\.\n\n## [0-9-]+ [0-9:]+ · Succeeds 1\.0\.0 → 2\.0\.0 · done\n\n### Game · done\n\nNo step\.\n\n## 2026-11-21 14:32 · Succeeds 0\.9\.0 → 1\.0\.0 · 1 item left\n$", text);
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
        var failed = false;

        var result = LoadAndSave(WriteSolution(game, windows), out var report, parameters =>
        {
            parameters.PackageUpgradeRequested = (package, _)
                => package.Meta.Name == "Game" ? PackageUpgradeRequestedAnswer.DoNotUpgrade : PackageUpgradeRequestedAnswer.Upgrade;
            parameters.PackageUpgradeFailed = _ =>
            {
                failed = true;
                return PackageUpgradeFailedAnswer.Keep;
            };
        });

        // Nothing failed: Game.Windows stays on its version, as Game does
        Assert.False(failed);
        Assert.Equal(windowsBefore, ReadFiles(windows));
        Assert.Null(report.Find(new UFile(windows), "Succeeds"));
        Assert.Contains(result.Messages, x => x.Type == LogMessageType.Warning && x.Text == "[Game.Windows] is not upgraded: the project it references, [Game], is not upgraded.");
    }

    [Theory]
    [InlineData(UnfinishedUpgradeAnswer.Leave)]
    [InlineData(UnfinishedUpgradeAnswer.Retry)]
    public void AProjectThatReferencesAnUnfinishedProjectIsNotUpgraded(UnfinishedUpgradeAnswer answer)
    {
        var game = WriteProject("Game", """
            <PackageReference Include="UpgradeRecord.Succeeds" Version="1.0.0" />
            <StridePackageUpgradeRecord Include="Succeeds" Version="1.0.0" Unfinished="Succeeds-1.0.0-P2" />
            """);
        var windows = WriteProject("Game.Windows", """<ProjectReference Include="..\Game\Game.csproj" />""");
        var windowsBefore = ReadFiles(windows);
        var failed = false;

        var result = LoadAndSave(WriteSolution(game, windows), parameters =>
        {
            parameters.UnfinishedUpgradeFound = (_, _, _) => answer;
            parameters.PackageUpgradeFailed = _ =>
            {
                failed = true;
                return PackageUpgradeFailedAnswer.Keep;
            };
        });

        // Not now, or a retry that leaves a step: the solution stays on its version, and nothing failed
        Assert.False(failed);
        Assert.Equal("Succeeds-1.0.0-P2", ReadUnfinished(game, "Succeeds"));
        Assert.Equal(new Dictionary<string, string> { ["Succeeds"] = "1.0.0" }, ReadRecords(game));
        Assert.Equal(windowsBefore, ReadFiles(windows));
        Assert.Contains(result.Messages, x => x.Type == LogMessageType.Warning && x.Text == "[Game.Windows] is not upgraded: the project it references, [Game], is not upgraded.");
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

        var result = LoadAndSave(WriteSolution(game, windows), parameters => parameters.BackupBeforeUpgrade = true);

        Assert.Empty(ReadRecords(game));
        Assert.Empty(ReadRecords(windows));
        // Game.Windows fails its own last step, or is stopped by Game's, depending on the order the assets load in
        AssertError(result, "Error while upgrading package [Game] for [UpgradeRecord.FailsAfterAssets]");
        AssertError(result, "[Game.Windows]");
    }

    [Fact]
    public void AFailedUpgradePutsTheFilesBack()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.SecondStepThrows" Version="1.0.0" />""");
        var gameDirectory = Path.GetDirectoryName(game)!;
        var before = ReadFiles(game);

        var result = LoadAndSave(game, out var report, parameters => parameters.BackupBeforeUpgrade = true);

        Assert.Equal(before, ReadFiles(game));
        Assert.False(File.Exists(Path.Combine(gameDirectory, "First.txt")));
        Assert.Equal(UpgradeOutcome.Reverted, report.Projects.Single().Outcome);
        Assert.Contains(result.Messages, x => x.Type == LogMessageType.Warning && x.Text.Contains("every file the upgrade changed, in every project, is put back as it was", StringComparison.Ordinal));
        // Nothing is left to do in the project: the report of the upgrade is in the backup, without items to do by hand
        Assert.False(File.Exists(ReportFile(game)));
        var text = File.ReadAllText(Path.Combine(Directory.GetDirectories(gameDirectory, UpgradeBackup.FolderNamePrefix + "*").Single(), UpgradeReportWriter.FileName));
        Assert.Matches(@"^# Upgrade report: Game\.csproj\n\n## [0-9-]+ [0-9:]+ · UpgradeRecord\.SecondStepThrows 1\.0\.0 → 2\.0\.0 · reverted\n", text);
        Assert.Contains("\n### Game · reverted\n", text, StringComparison.Ordinal);
        Assert.Contains("#### Failed\n\n- `UpgradeRecord.SecondStepThrows-2.0.0-P2` Second\n", text, StringComparison.Ordinal);
        Assert.DoesNotContain("- [ ]", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AFailedUpgradeRevertsEveryProjectOfTheSession()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.SecondStepThrows" Version="1.0.0" />""");
        var tool = WriteProject("Tool", """<PackageReference Include="UpgradeRecord.Succeeds" Version="1.0.0" />""");
        var before = ReadFiles(game, tool);
        IReadOnlyList<ProjectUpgradeReport>? asked = null;

        LoadAndSave(WriteSolution(game, tool), out var report, parameters =>
        {
            parameters.BackupBeforeUpgrade = true;
            parameters.PackageUpgradeFailed = reports =>
            {
                asked = reports;
                return PackageUpgradeFailedAnswer.Revert;
            };
        });

        // Asked once, about the project that failed; the one that succeeded goes back too, so the solution stays on one version
        Assert.Equal([new UFile(game)], asked!.Select(x => x.ProjectFullPath));
        Assert.Equal(before, ReadFiles(game, tool));
        Assert.Empty(ReadRecords(tool));
        Assert.All(report.Projects, x => Assert.Equal(UpgradeOutcome.Reverted, x.Outcome));
    }

    [Fact]
    public void AFailedUpgradeWithoutBackupKeepsTheFiles()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.SecondStepThrows" Version="1.0.0" />""");
        var asked = false;

        var result = LoadAndSave(game, out var report, parameters => parameters.PackageUpgradeFailed = _ =>
        {
            asked = true;
            return PackageUpgradeFailedAnswer.Revert;
        });

        // Revert can't be offered
        Assert.False(asked);
        Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(game)!, "First.txt")));
        Assert.Equal(new Dictionary<string, string> { ["UpgradeRecord.SecondStepThrows"] = "2.0.0" }, ReadRecords(game));
        Assert.Equal(UpgradeOutcome.Kept, report.Projects.Single().Outcome);
        Assert.Contains(result.Messages, x => x.Type == LogMessageType.Warning && x.Text.Contains("The upgrade of [Game] failed, and there is no backup to put the files back from", StringComparison.Ordinal));
    }

    [Fact]
    public void ARevertThatCantPutAFileBackRecordsTheUpgradeAsUnfinished()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.SecondStepThrows" Version="1.0.0" />""");
        var first = Path.Combine(Path.GetDirectoryName(game)!, "First.txt");

        var result = LoadAndSave(game, out var report, parameters =>
        {
            parameters.BackupBeforeUpgrade = true;
            parameters.PackageUpgradeFailed = _ =>
            {
                // A file the upgrade created that can't be deleted
                File.Delete(first);
                Directory.CreateDirectory(first);
                return PackageUpgradeFailedAnswer.Revert;
            };
        });

        Assert.Equal(UpgradeOutcome.PartlyReverted, report.Projects.Single().Outcome);
        // The version before the upgrade, which no upgrade runs on before it is marked as finished
        Assert.Equal(new Dictionary<string, string> { ["UpgradeRecord.SecondStepThrows"] = "1.0.0" }, ReadRecords(game));
        Assert.Equal("Put the files back as they were", ReadUnfinished(game, "UpgradeRecord.SecondStepThrows"));
        AssertError(result, "The upgrade of [Game] failed, and 1 of the files it changed could not be put back as they were: copy them from [");
        Assert.Contains(" · partly reverted, 3 items left\n", File.ReadAllText(ReportFile(game)), StringComparison.Ordinal);
    }

    [Fact]
    public void AKeptUpgradeRecordsTheStepsLeft()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.SecondStepThrows" Version="1.0.0" />""");
        IReadOnlyList<ProjectUpgradeReport>? asked = null;

        LoadAndSave(game, out var report, parameters =>
        {
            parameters.BackupBeforeUpgrade = true;
            parameters.PackageUpgradeFailed = reports =>
            {
                asked = reports;
                return PackageUpgradeFailedAnswer.Keep;
            };
        });

        Assert.Same(report.Projects.Single(), asked!.Single());
        Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(game)!, "First.txt")));
        Assert.Equal(new Dictionary<string, string> { ["UpgradeRecord.SecondStepThrows"] = "2.0.0" }, ReadRecords(game));
        Assert.Equal("UpgradeRecord.SecondStepThrows-2.0.0-P2;UpgradeRecord.SecondStepThrows-2.0.0-P3", ReadUnfinished(game, "UpgradeRecord.SecondStepThrows"));
        var text = File.ReadAllText(ReportFile(game));
        Assert.Contains(" · kept, 2 items left\n", text, StringComparison.Ordinal);
        Assert.Contains($" The files it changed are backed up in `{UpgradeBackup.FolderNamePrefix}", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AKeptUpgradeRecordsTheStepsLeftOfTheProjectsItStopped()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.SecondStepThrows" Version="1.0.0" />""");
        var windows = WriteProject("Game.Windows", """<ProjectReference Include="..\Game\Game.csproj" />""");

        LoadAndSave(WriteSolution(game, windows), parameters =>
        {
            parameters.BackupBeforeUpgrade = true;
            parameters.PackageUpgradeFailed = _ => PackageUpgradeFailedAnswer.Keep;
        });

        // Game.Windows changed nothing, but its steps are left: no upgrade of Game runs them any more
        Assert.Equal("UpgradeRecord.SecondStepThrows-2.0.0-P1;UpgradeRecord.SecondStepThrows-2.0.0-P2;UpgradeRecord.SecondStepThrows-2.0.0-P3", ReadUnfinished(windows, "UpgradeRecord.SecondStepThrows"));
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
        AssertError(result, "The upgrade of [Game] doesn't run: its last upgrade is unfinished");
    }

    [Fact]
    public void AnUnfinishedUpgradeWithoutAnUpgradePendingIsNotAskedAbout()
    {
        var game = WriteProject("Game", $"""
            <PackageReference Include="UpgradeRecord.SecondStepBlocked" Version="2.0.0" />
            <StridePackageUpgradeRecord Include="UpgradeRecord.SecondStepBlocked" Version="2.0.0" Unfinished="{SecondStepBlocked}-2.0.0-P2" />
            """);
        var before = ReadFiles(game);
        var asked = false;

        var result = LoadAndSave(game, parameters => parameters.UnfinishedUpgradeFound = (_, _, _) =>
        {
            asked = true;
            return UnfinishedUpgradeAnswer.Retry;
        });

        // The project loads as it is: the front-end shows the unfinished upgrade
        Assert.False(asked);
        Assert.Equal(before, ReadFiles(game));
        Assert.Contains(result.Messages, x => x.Type == LogMessageType.Info && x.Text == "The last upgrade of [Game] is unfinished, with UpgradeRecord.SecondStepBlocked-2.0.0-P2 to finish by hand.");
    }

    [Fact]
    public void RetryRunsTheProjectStepsLeftAndFinishesTheUpgrade()
    {
        var game = WriteProject("Game", $"""
            <PackageReference Include="UpgradeRecord.SecondStepBlocked" Version="2.0.0" />
            <StridePackageUpgradeRecord Include="UpgradeRecord.SecondStepBlocked" Version="2.0.0" Unfinished="{SecondStepBlocked}-2.0.0-P2;{SecondStepBlocked}-2.0.0-P3" />
            """);

        LoadAndSave(game, out var report, parameters => parameters.RetryUnfinishedUpgrades = true);

        // The steps left run, not the one done before
        var properties = XDocument.Load(game).Descendants("PropertyGroup").Elements().Select(x => x.Name.LocalName).ToList();
        Assert.DoesNotContain("First", properties);
        Assert.Contains("Second", properties);
        Assert.Contains("Third", properties);
        Assert.Equal(new Dictionary<string, string> { [SecondStepBlocked] = "2.0.0" }, ReadRecords(game));
        Assert.Null(ReadUnfinished(game, SecondStepBlocked));
        var retried = Assert.Single(report.Projects.Single().Upgraders);
        Assert.True(retried.IsRetry);
        Assert.All(retried.Steps, x => Assert.Equal(UpgradeStepStatus.Done, x.Status));
    }

    [Fact]
    public void AnUpgradeAfterARetryHasAReportOfItsOwn()
    {
        var game = WriteProject("Game", """
            <PackageReference Include="UpgradeRecord.Retried" Version="1.0.0" />
            <StridePackageUpgradeRecord Include="Retried" Version="1.0.0" Unfinished="Retried-1.0.0-P2" />
            """);

        LoadAndSave(game, out var report, parameters => parameters.UnfinishedUpgradeFound = (_, _, _) => UnfinishedUpgradeAnswer.Retry);

        Assert.Equal(new Dictionary<string, string> { ["Retried"] = "2.0.0" }, ReadRecords(game));
        Assert.Null(ReadUnfinished(game, "Retried"));
        // The upgrade comes first: it names the section of the report
        var upgraders = report.Projects.Single().Upgraders;
        Assert.Equal([(false, "2.0.0"), (true, "1.0.0")], upgraders.Select(x => (x.IsRetry, x.ToVersion?.ToString())));
        Assert.Same(upgraders[0], report.Find(new UFile(game), "Retried"));
        Assert.Equal(UpgradeStepStatus.Done, upgraders[1].Steps.Single().Status);
    }

    [Fact]
    public void RetryRunsOnAnOpenSession()
    {
        var game = WriteProject("Game", $"""
            <PackageReference Include="UpgradeRecord.SecondStepBlocked" Version="2.0.0" />
            <StridePackageUpgradeRecord Include="UpgradeRecord.SecondStepBlocked" Version="2.0.0" Unfinished="{SecondStepBlocked}-2.0.0-P2;Put the files back as they were" />
            """);
        var result = new PackageSessionResult();
        PackageSession.Load(game, result, new PackageLoadParameters { AutoCompileProjects = false, LoadAssemblyReferences = false });
        using var session = result.Session;

        // As the banner of Game Studio does
        var retry = session.RetryUnfinishedUpgrades([session.Projects.OfType<SolutionProject>().Single()], result).Single();

        Assert.True(retry.ChangedProject);
        // A part of the upgrade that is not a step is still left
        Assert.Equal([new UpgradeStepInfo("", "Put the files back as they were", null)], retry.StepsLeft);
        Assert.Equal("Put the files back as they were", ReadUnfinished(game, SecondStepBlocked));
        Assert.Contains("Second", XDocument.Load(game).Descendants("PropertyGroup").Elements().Select(x => x.Name.LocalName));
        Assert.Contains("\n#### Not run\n\n- [ ] Put the files back as they were\n", File.ReadAllText(ReportFile(game)), StringComparison.Ordinal);
    }

    [Fact]
    public void ARetryOfTheProjectsOfTwoUpgradesBacksUpInTheBackupOfEach()
    {
        var game = WriteProject("Game", $"""<PackageReference Include="{SecondStepBlocked}" Version="1.0.0" />""");
        var extra = WriteProject("Extra", $"""<PackageReference Include="{SecondStepBlocked}" Version="1.0.0" />""");
        string[] projects = [game, extra];
        // Each upgraded alone, so that each upgrade has a backup of its own, and kept unfinished
        foreach (var project in projects)
        {
            var blocked = Path.Combine(Path.GetDirectoryName(project)!, "Blocked.txt");
            File.WriteAllText(blocked, "");
            LoadAndSave(project, parameters =>
            {
                parameters.BackupBeforeUpgrade = true;
                parameters.PackageUpgradeFailed = _ => PackageUpgradeFailedAnswer.Keep;
            });
            File.Delete(blocked);
        }
        var backups = projects.Select(x => VSProjectHelper.GetUnfinishedUpgrades(x).Single().Backup!).ToList();
        Assert.NotEqual(backups[0], backups[1], StringComparer.OrdinalIgnoreCase);
        var result = new PackageSessionResult();
        PackageSession.Load(WriteSolution(game, extra), result, new PackageLoadParameters { AutoCompileProjects = false, LoadAssemblyReferences = false });
        using var session = result.Session;

        // As the banner of Game Studio does
        var retries = session.RetryUnfinishedUpgrades([.. session.Projects.OfType<SolutionProject>()], result);

        Assert.All(retries, x => Assert.Empty(x.StepsLeft));
        for (var i = 0; i < projects.Length; i++)
        {
            // The record still names the backup of its upgrade, which has the file the retry created
            Assert.Equal([backups[i]], VSProjectHelper.GetUpgradeRecords(projects[i]).Backups);
            Assert.Contains(Path.Combine(Path.GetDirectoryName(projects[i])!, "Third.txt"), UpgradeBackup.Open(backups[i])!.GetFiles(), StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ASaveDuringARetryWaitsForItAndDoesNotBackUpInTheBackupOfTheUpgrade()
    {
        var game = WriteProject("Game", $"""<PackageReference Include="{RetryHook}" Version="1.0.0" />""");
        var blocked = Path.Combine(Path.GetDirectoryName(game)!, "Blocked.txt");
        File.WriteAllText(blocked, "");
        LoadAndSave(game, parameters =>
        {
            parameters.BackupBeforeUpgrade = true;
            parameters.PackageUpgradeFailed = _ => PackageUpgradeFailedAnswer.Keep;
        });
        File.Delete(blocked);
        var backup = VSProjectHelper.GetUnfinishedUpgrades(game).Single().Backup!;
        var result = new PackageSessionResult();
        PackageSession.Load(game, result, new PackageLoadParameters { AutoCompileProjects = false, LoadAssemblyReferences = false });
        using var session = result.Session;
        var package = session.LocalPackages.Single();

        // As Game Studio does when the user saves while the banner retries
        Task? save = null;
        var saveEndedDuringRetry = false;
        RetryHookUpgrader.DuringSecondStep = () =>
        {
            package.IsDirty = true;
            save = Task.Run(() => session.Save(new LoggerResult()));
            saveEndedDuringRetry = save.Wait(TimeSpan.FromMilliseconds(500));
        };
        try
        {
            var retry = session.RetryUnfinishedUpgrades([session.Projects.OfType<SolutionProject>().Single()], result).Single();
            Assert.Empty(retry.StepsLeft);
        }
        finally
        {
            RetryHookUpgrader.DuringSecondStep = null;
        }

        Assert.NotNull(save);
        await save;
        Assert.False(saveEndedDuringRetry);
        Assert.False(package.IsDirty);
        Assert.DoesNotContain(package.FullPath.ToOSPath(), UpgradeBackup.Open(backup)!.GetFiles(), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void AChangeMadeBeforeARetryStillCountsAsChangedSinceTheUpgrade()
    {
        var game = WriteProject("Game", $"""<PackageReference Include="{SecondStepBlocked}" Version="1.0.0" />""");
        var gameDirectory = Path.GetDirectoryName(game)!;
        var blocked = Path.Combine(gameDirectory, "Blocked.txt");
        File.WriteAllText(blocked, "");
        LoadAndSave(game, parameters =>
        {
            parameters.BackupBeforeUpgrade = true;
            parameters.PackageUpgradeFailed = _ => PackageUpgradeFailedAnswer.Keep;
        });
        var backup = VSProjectHelper.GetUnfinishedUpgrades(game).Single().Backup!;

        // A change to a file of the upgrade that the retry doesn't write
        var first = Path.Combine(gameDirectory, "First.txt");
        File.WriteAllText(first, "Mine");
        File.Delete(blocked);
        LoadAndSave(game, parameters => parameters.RetryUnfinishedUpgrades = true);

        Assert.Null(ReadUnfinished(game, SecondStepBlocked));
        Assert.Equal([first], UpgradeBackup.Open(backup)!.GetChangedFiles());
    }

    [Fact]
    public void RetryLeavesTheCodeStepsAndTheStepsThatFailAgain()
    {
        var game = WriteProject("Game", $"""
            <PackageReference Include="UpgradeRecord.SecondStepBlocked" Version="2.0.0" />
            <StridePackageUpgradeRecord Include="UpgradeRecord.SecondStepBlocked" Version="2.0.0" Unfinished="{SecondStepBlocked}-2.0.0-C1;{SecondStepBlocked}-2.0.0-P2;{SecondStepBlocked}-2.0.0-P3" />
            """);
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(game)!, "Blocked.txt"), "");

        var result = LoadAndSave(game, parameters => parameters.RetryUnfinishedUpgrades = true);

        Assert.Equal($"{SecondStepBlocked}-2.0.0-C1;{SecondStepBlocked}-2.0.0-P2;{SecondStepBlocked}-2.0.0-P3", ReadUnfinished(game, SecondStepBlocked));
        Assert.Contains(result.Messages, x => x.Type == LogMessageType.Warning && x.Text.Contains("The last upgrade of [Game] still leaves", StringComparison.Ordinal));
        Assert.Contains(" · UpgradeRecord.SecondStepBlocked 2.0.0, steps left run again · 3 items left\n", File.ReadAllText(ReportFile(game)), StringComparison.Ordinal);
    }

    [Fact]
    public void TheQuestionAboutAnUnfinishedUpgradeSaysWhichStepsRunAgain()
    {
        var game = WriteProject("Game", $"""
            <PackageReference Include="{SecondStepBlocked}" Version="1.0.0" />
            <StridePackageUpgradeRecord Include="{SecondStepBlocked}" Version="1.0.0" Unfinished="{SecondStepBlocked}-2.0.0-C1;{SecondStepBlocked}-2.0.0-P2;Put the files back as they were" />
            """);
        IReadOnlyList<UpgradeStepInfo>? asked = null;

        LoadAndSave(game, parameters => parameters.UnfinishedUpgradeFound = (_, steps, _) =>
        {
            asked = steps;
            return UnfinishedUpgradeAnswer.Leave;
        });

        // A code step, and a part of the upgrade that is not a step, don't run again
        Assert.Equal([false, true, false], asked!.Select(x => x.CanRetry));
    }

    [Fact]
    public void AKeptUpgradeIsRevertedWhenTheProjectOpens()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.SecondStepThrows" Version="1.0.0" />""");
        var before = ReadFiles(game);
        LoadAndSave(game, parameters =>
        {
            parameters.BackupBeforeUpgrade = true;
            parameters.PackageUpgradeFailed = _ => PackageUpgradeFailedAnswer.Keep;
        });
        // Its upgrade is still pending, as in a project the failure stopped before its references changed
        var document = XDocument.Load(game);
        document.Descendants("PackageReference").Single().SetAttributeValue("Version", "1.0.0");
        document.Save(game);
        string? asked = null;

        var result = new PackageSessionResult();
        PackageSession.Load(game, result, new PackageLoadParameters
        {
            AutoCompileProjects = false,
            LoadAssemblyReferences = false,
            UnfinishedUpgradeFound = (_, _, backup) =>
            {
                asked = backup;
                return UnfinishedUpgradeAnswer.Revert;
            },
        });
        using var session = result.Session;
        session.Save(result);

        Assert.Empty(VSProjectHelper.GetUnfinishedUpgrades(game));
        Assert.StartsWith(UpgradeBackup.FolderNamePrefix, Path.GetFileName(asked), StringComparison.Ordinal);
        Assert.Equal([asked], session.RevertedUnfinishedUpgrade!.BackupFolders);
        Assert.Empty(session.RevertedUnfinishedUpgrade.NotPutBack);
        // The session, which read the files of the upgrade, saves none of them
        Assert.Equal(before, ReadFiles(game));
        Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(game)!, "First.txt")));
    }

    [Fact]
    public void ARevertOfAnUpgradeWithoutBackupLeavesItUnfinished()
    {
        // Kept without a backup: nothing to put the files back from
        var game = WriteProject("Game", $"""
            <PackageReference Include="{SecondStepBlocked}" Version="1.0.0" />
            <StridePackageUpgradeRecord Include="{SecondStepBlocked}" Version="1.0.0" Unfinished="{SecondStepBlocked}-2.0.0-P2" />
            """);

        var result = new PackageSessionResult();
        PackageSession.Load(game, result, new PackageLoadParameters
        {
            AutoCompileProjects = false,
            LoadAssemblyReferences = false,
            UnfinishedUpgradeFound = (_, _, _) => UnfinishedUpgradeAnswer.Revert,
        });
        using var session = result.Session;

        Assert.Null(session!.RevertedUnfinishedUpgrade);
        AssertError(result, "names no backup on this computer");
        Assert.Equal([$"{SecondStepBlocked}-2.0.0-P2"], VSProjectHelper.GetUnfinishedUpgrades(game).Single().Steps);
        Assert.Equal("1.0.0", ReadPackageVersion(game, SecondStepBlocked));
    }

    [Fact]
    public void ARevertWhenAProjectOpensStopsTheCodeMigrationOfTheOtherProjects()
    {
        var kept = WriteProject("Kept", """<PackageReference Include="UpgradeRecord.SecondStepThrows" Version="1.0.0" />""");
        LoadAndSave(kept, parameters =>
        {
            parameters.BackupBeforeUpgrade = true;
            parameters.PackageUpgradeFailed = _ => PackageUpgradeFailedAnswer.Keep;
        });
        var document = XDocument.Load(kept);
        document.Descendants("PackageReference").Single().SetAttributeValue("Version", "1.0.0");
        document.Save(kept);
        // Listed first, so its upgrade is confirmed before the question about the unfinished upgrade
        var code = WriteProject("Code", """<PackageReference Include="UpgradeRecord.CodeRules" Version="1.0.0" />""");

        LoadAndSave(WriteSolution(code, kept), parameters =>
        {
            parameters.AutoCompileProjects = true;
            parameters.UnfinishedUpgradeFound = (_, _, _) => UnfinishedUpgradeAnswer.Revert;
        });

        Assert.DoesNotContain(new UFile(code), CodeRunner.MigratedProjects);
        Assert.Equal("1.0.0", ReadPackageVersion(code, "UpgradeRecord.CodeRules"));
    }

    [Fact]
    public void ARevertPutsBackTheFilesOfTheRetriesToo()
    {
        var game = WriteProject("Game", $"""<PackageReference Include="{SecondStepBlocked}" Version="1.0.0" />""");
        var gameDirectory = Path.GetDirectoryName(game)!;
        var blocked = Path.Combine(gameDirectory, "Blocked.txt");
        File.WriteAllText(blocked, "");
        var before = ReadFiles(game);
        LoadAndSave(game, parameters =>
        {
            parameters.BackupBeforeUpgrade = true;
            parameters.PackageUpgradeFailed = _ => PackageUpgradeFailedAnswer.Keep;
        });
        var kept = VSProjectHelper.GetUnfinishedUpgrades(game).Single();
        Assert.StartsWith(UpgradeBackup.FolderNamePrefix, Path.GetFileName(kept.Backup), StringComparison.Ordinal);

        // A retry, without a backup of its own, once the step is no longer blocked: a code step is still left
        File.Delete(blocked);
        var document = XDocument.Load(game);
        document.Descendants(PackageUpgrader.UpgradeRecordItem).Single().SetAttributeValue(PackageUpgrader.UnfinishedMetadata, $"{SecondStepBlocked}-2.0.0-C1;{SecondStepBlocked}-2.0.0-P2;{SecondStepBlocked}-2.0.0-P3");
        document.Save(game);
        LoadAndSave(game, parameters => parameters.RetryUnfinishedUpgrades = true);
        var retried = VSProjectHelper.GetUnfinishedUpgrades(game).Single();
        Assert.Equal([$"{SecondStepBlocked}-2.0.0-C1"], retried.Steps);
        Assert.Equal(kept.Backup, retried.Backup);
        Assert.True(File.Exists(Path.Combine(gameDirectory, "Third.txt")));
        // The backup records the files as the retry left them: only a change made since counts
        Assert.Empty(UpgradeBackup.Open(retried.Backup!)!.GetChangedFiles());
        File.WriteAllText(Path.Combine(gameDirectory, "First.txt"), "Mine");
        Assert.Equal([Path.Combine(gameDirectory, "First.txt")], UpgradeBackup.Open(retried.Backup!)!.GetChangedFiles());

        // As the banner of Game Studio does
        var revert = PackageSession.RevertUpgrades([retried.Backup!], new LoggerResult());

        Assert.Empty(revert.NotPutBack);
        Assert.Equal(before, ReadFiles(game));
        Assert.Empty(Directory.GetFiles(gameDirectory, "*.txt"));
        // The files as they were before the revert are kept in the backup
        var reverted = Directory.GetDirectories(retried.Backup!, "reverted-*").Single();
        Assert.Equal("Mine", File.ReadAllText(Path.Combine(reverted, "First.txt")));
        Assert.True(File.Exists(Path.Combine(reverted, "Third.txt")));
        Assert.Equal([reverted], revert.KeptFolders);
        // The report, which the revert doesn't put back, says the upgrade below is reverted
        var report = File.ReadAllText(ReportFile(game));
        var top = report[report.IndexOf("\n## ", StringComparison.Ordinal)..];
        // The project had no record before the upgrade
        Assert.EndsWith($" · {SecondStepBlocked} 2.0.0 · reverted, 1 item left", top.Split('\n')[1], StringComparison.Ordinal);
        Assert.Contains($" · reverted, 1 item left\n\nEvery file the last upgrade and its retries changed is put back as it was, from `{Path.GetFileName(retried.Backup)}`.", top, StringComparison.Ordinal);
        Assert.Contains($"The files as they were before the revert are in `{Path.GetFileName(retried.Backup)}/{Path.GetFileName(reverted)}`.\n", top, StringComparison.Ordinal);
        Assert.Contains("### Changed since the upgrade\n", top, StringComparison.Ordinal);
        Assert.Contains("\n- [ ] `First.txt`\n", top, StringComparison.Ordinal);
        Assert.Equal([Path.Combine(gameDirectory, "First.txt")], revert.ChangedFiles);
    }

    [Fact]
    public void ARetryWhenProjectsOpenBacksUpEachProjectInTheBackupOfItsUpgrade()
    {
        // Two upgrades, each kept unfinished with a backup of its own, once their step is no longer blocked: a code step is still left
        var projects = new[] { WriteProject("First", $"""<PackageReference Include="{SecondStepBlocked}" Version="1.0.0" />"""), WriteProject("Second", $"""<PackageReference Include="{SecondStepBlocked}" Version="1.0.0" />""") };
        var backups = new List<string>();
        foreach (var project in projects)
        {
            var blocked = Path.Combine(Path.GetDirectoryName(project)!, "Blocked.txt");
            File.WriteAllText(blocked, "");
            LoadAndSave(project, parameters =>
            {
                parameters.BackupBeforeUpgrade = true;
                parameters.PackageUpgradeFailed = _ => PackageUpgradeFailedAnswer.Keep;
            });
            File.Delete(blocked);
            var document = XDocument.Load(project);
            document.Descendants(PackageUpgrader.UpgradeRecordItem).Single().SetAttributeValue(PackageUpgrader.UnfinishedMetadata, $"{SecondStepBlocked}-2.0.0-C1;{SecondStepBlocked}-2.0.0-P2;{SecondStepBlocked}-2.0.0-P3");
            document.Save(project);
            backups.Add(VSProjectHelper.GetUnfinishedUpgrades(project).Single().Backup!);
        }
        Assert.NotEqual(backups[0], backups[1], StringComparer.OrdinalIgnoreCase);

        LoadAndSave(WriteSolution(projects), parameters => parameters.RetryUnfinishedUpgrades = true);

        for (var i = 0; i < projects.Length; i++)
        {
            var retried = VSProjectHelper.GetUnfinishedUpgrades(projects[i]).Single();
            Assert.Equal([$"{SecondStepBlocked}-2.0.0-C1"], retried.Steps);
            Assert.Equal(backups[i], retried.Backup);
            // The files of the retry are in that backup, as the retry left them
            var backup = UpgradeBackup.Open(backups[i])!;
            Assert.Contains(Path.Combine(Path.GetDirectoryName(projects[i])!, "Third.txt"), backup.GetFiles(), StringComparer.OrdinalIgnoreCase);
            Assert.Empty(backup.GetChangedFiles());
        }
    }

    [Fact]
    public void AnUpgradeThatLeftItemsNamesItsBackupUntilItIsRemoved()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.FileFails" Version="1.0.0" />""");

        LoadAndSave(game, parameters => parameters.BackupBeforeUpgrade = true);

        // The upgrade completed, with files left to change by hand
        var (unfinished, backups) = VSProjectHelper.GetUpgradeRecords(game);
        Assert.Empty(unfinished);
        var backup = Assert.Single(backups);
        Assert.StartsWith(UpgradeBackup.FolderNamePrefix, Path.GetFileName(backup), StringComparison.Ordinal);
        Assert.Empty(UpgradeBackup.Open(backup)!.GetChangedFiles());

        // As removing the report and backup in Game Studio does
        Assert.Equal([backup], VSProjectHelper.RemoveUpgradeBackups(game));
        Assert.Empty(VSProjectHelper.GetUpgradeRecords(game).Backups);
        Assert.Equal(new Dictionary<string, string> { ["UpgradeRecord.FileFails"] = "2.0.0" }, ReadRecords(game));
    }

    [Fact]
    public void AnUnfinishedUpgradeMarkedAsFinishedLetsTheNextOneRun()
    {
        var game = WriteProject("Game", """
            <PackageReference Include="UpgradeRecord.Succeeds" Version="1.0.0" />
            <StridePackageUpgradeRecord Include="Succeeds" Version="1.0.0" Unfinished="Succeeds-1.0.0-P2;Succeeds-0.9.0-P1" />
            """);
        File.WriteAllText(ReportFile(game), "# Upgrade report: Game.csproj\n\nIntro.\n\n## 2026-11-21 14:32 · Succeeds 0.9.0 → 1.0.0 · kept, 2 items left\n");
        IReadOnlyList<UpgradeStepInfo>? asked = null;

        LoadAndSave(game, parameters => parameters.UnfinishedUpgradeFound = (_, steps, _) =>
        {
            asked = steps;
            return UnfinishedUpgradeAnswer.MarkFinished;
        });

        // A step the upgrader no longer declares has only its ID
        Assert.Equal([new UpgradeStepInfo("Succeeds-1.0.0-P2", "Move the settings", "https://doc/1.0.html#succeeds-1.0.0-p2"), new UpgradeStepInfo("Succeeds-0.9.0-P1", "", null)], asked);
        Assert.Equal(new Dictionary<string, string> { ["Succeeds"] = "2.0.0" }, ReadRecords(game));
        Assert.Null(ReadUnfinished(game, "Succeeds"));
        // The report gets the mark, then the upgrade that ran after it on top
        var text = File.ReadAllText(ReportFile(game));
        var upgrade = text.IndexOf(" · Succeeds 1.0.0 → 2.0.0 · ", StringComparison.Ordinal);
        var mark = text.IndexOf(" · Last upgrade marked as finished\n\nIts items below are done by hand:\n\n- Game: `Succeeds-1.0.0-P2`, `Succeeds-0.9.0-P1`\n", StringComparison.Ordinal);
        var kept = text.IndexOf(" · Succeeds 0.9.0 → 1.0.0 · ", StringComparison.Ordinal);
        Assert.True(upgrade > 0 && upgrade < mark && mark < kept, text);
    }

    [Fact]
    public void TheBackupPutsBackTheFilesItHas()
    {
        var root = Directory.CreateDirectory(Path.Combine(directory, "Solution")).FullName;
        var inside = Path.Combine(root, "Game", "Player.cs");
        var outside = Path.Combine(directory, "Shared", "Lib.cs");
        var created = Path.Combine(root, "Game", "New.cs");
        var moved = Path.Combine(root, "Game", "Water.sdsl.cs");
        foreach (var file in new[] { inside, outside, moved })
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
        backup.SnapshotMove(moved, moved + ".bak");
        File.Move(moved, moved + ".bak");
        Assert.True(backup.HasChanges(x => x == outside));

        // A later session opens it from its folder
        var opened = UpgradeBackup.Open(backup.FolderPath!)!;
        Assert.Empty(opened.Restore(_ => true));
        Assert.Equal("Before", File.ReadAllText(inside));
        Assert.Equal("Before", File.ReadAllText(outside));
        Assert.Equal("Before", File.ReadAllText(moved));
        Assert.False(File.Exists(created));
        Assert.False(File.Exists(moved + ".bak"));
        Assert.False(opened.HasChanges(_ => true));
        // The folder lists only the files still to put back
        Assert.False(UpgradeBackup.Open(backup.FolderPath!)!.HasChanges(_ => true));
    }

    [Fact]
    public void AFileAMoveCreatedStaysWhileTheFileItWasMovedFromIsNotBack()
    {
        var root = Directory.CreateDirectory(Path.Combine(directory, "Game")).FullName;
        var source = Path.Combine(root, "Water.sdsl.cs");
        File.WriteAllText(source, "Before");
        var backup = new UpgradeBackup(root, DateTime.Now);

        backup.SnapshotMove(source, source + ".bak");
        File.Move(source, source + ".bak");
        // Its copy is lost
        Directory.Delete(backup.FolderPath!, recursive: true);

        Assert.Equal([source], backup.Restore(_ => true));
        Assert.Equal("Before", File.ReadAllText(source + ".bak"));
    }

    [Fact]
    public void TheBackupSkipsTheFilesOfBackupFolders()
    {
        var root = Path.Combine(directory, "Game");
        var backup = new UpgradeBackup(root, DateTime.Now);

        Assert.True(UpgradeBackup.IsInBackupFolder(".stride-backup-20261125-151200/Effects/Water.sdsl.cs"));
        Assert.True(UpgradeBackup.IsInBackupFolder("Game/.stride-backup-20261125-151200/Water.sdsl.cs"));
        Assert.False(UpgradeBackup.IsInBackupFolder("Effects/Water.sdsl.cs"));
        Assert.Null(backup.GetBackupPath(Path.Combine(root, ".stride-backup-20261125-151200", "Water.sdsl.cs")));
    }

    [Fact]
    public void AFileOnANetworkShareIsBackedUpUnderItsShare()
    {
        // Network paths are Windows paths
        if (!OperatingSystem.IsWindows())
            return;

        var backup = new UpgradeBackup(@"C:\Solution", DateTime.Now);

        Assert.Equal(@"outside\server\share\dir\Lib.cs", backup.GetBackupPath(@"\\server\share\dir\Lib.cs"));
        Assert.Equal(@"outside\D\Shared\Lib.cs", backup.GetBackupPath(@"D:\Shared\Lib.cs"));
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

    private string WriteProject(string name, string items, bool sdk = false, string? folder = null)
    {
        var path = Path.Combine(directory, folder ?? name, name + ".csproj");
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

    private static string ReportFile(string project) => UpgradeReportWriter.GetPath(UpgradeReportWriter.FindOwner(project));

    // An error that holds all the texts
    private static void AssertError(LoggerResult result, params string[] texts)
        => Assert.True(result.Messages.Any(x => x.Type == LogMessageType.Error && texts.All(text => x.Text.Contains(text, StringComparison.Ordinal))),
            $"No error with \"{string.Join("\", \"", texts)}\" in:\n{string.Join("\n", result.Messages.Select(x => $"{x.Type}: {x.Text}"))}");
}
