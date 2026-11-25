// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
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

    // A shader gate: an effect of an .sdfx file declared with `shader` is declared with `effect`, and a shader declared with `class`
    // is declared with `shader`
    [PackageUpgrader(["Stride.UpgradeRecord.ShaderGate"], "1.0.0", "2.0.0", Name = "ShaderGate")]
    public class ShaderGateUpgrader : global::Stride.Assets.CodePackageUpgrader
    {
        public override void DeclareUpgrades(global::Stride.Assets.UpgradeRegistry registry)
            => registry.Shader("2.0.0", "effect-keywords", "class-to-shader");
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
            => registry.Project("2.0.0", _ => throw new InvalidOperationException("The project can't be changed"));
    }

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
    public void AnUnrestoredSolutionStopsTheUpgrade()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.CodeRules" Version="1.0.0" />""");
        var windows = WriteProject("Game.Windows", """<ProjectReference Include="..\Game\Game.csproj" />""");
        var before = ReadFiles(game, windows);

        // Without its restore, the source migration of the solution can't run
        var result = LoadAndSave(WriteSolution(game, windows));

        Assert.Equal(before, ReadFiles(game, windows));
        AssertError(result, "The upgrade of [Game] is stopped: the solution [Game.slnx] isn't restored");
        AssertError(result, "The upgrade of [Game.Windows] is stopped: the project it references, [Game], could not be upgraded");
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
        AssertError(result, "Restore the files it changed from the upgrade backup");
        // The project that references it is not upgraded at all
        Assert.Equal(windowsBefore, ReadFiles(windows));
        AssertError(result, "The upgrade of [Game.Windows] is stopped: the project it references, [Game], could not be upgraded");
    }

    [Fact]
    public void AProjectWhoseShaderGatesCompletedRecordsTheVersion()
    {
        var game = WriteProject("Game", """<PackageReference Include="Stride.UpgradeRecord.ShaderGate" Version="1.0.0" />""");
        var effect = Path.Combine(Path.GetDirectoryName(game)!, "Main.sdfx");
        File.WriteAllText(effect, "namespace Game { shader Main { mixin Base; }; }");

        LoadAndSave(game);

        Assert.Equal("namespace Game { effect Main { mixin Base; }; }", File.ReadAllText(effect));
        Assert.Equal(new Dictionary<string, string> { ["ShaderGate"] = "2.0.0" }, ReadRecords(game));
    }

    [Fact]
    public void AFailedShaderUpgradeRecordsNothing()
    {
        var game = WriteProject("Game", """<PackageReference Include="Stride.UpgradeRecord.ShaderGate" Version="1.0.0" />""");
        var windows = WriteProject("Game.Windows", """<ProjectReference Include="..\Game\Game.csproj" />""");
        var effect = Path.Combine(Path.GetDirectoryName(game)!, "Main.sdfx");
        File.WriteAllText(effect, "namespace Game { shader Main { mixin Base; }; }");

        // The effect can't be read, on every OS: .NET enforces FileShare.None on Linux and macOS too
        PackageSessionResult result;
        using (new FileStream(effect, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            result = LoadAndSave(WriteSolution(game, windows));

        Assert.Empty(ReadRecords(game));
        Assert.Empty(ReadRecords(windows));
        AssertError(result, "Unable to read the shader [Main.sdfx]");
        AssertError(result, "Error while upgrading package [Game] for [Stride.UpgradeRecord.ShaderGate]", "Restore the files it changed from the upgrade backup");
        // Game comes first in the solution, and its references moved before its shader upgrade failed
        AssertError(result, "The upgrade of [Game.Windows] is stopped: the project it references, [Game], could not be upgraded.", "Restore the files it changed from the upgrade backup");
    }

    [Fact]
    public void AShaderThatCantBeReadFailsTheUpgradeBeforeAnyShaderIsWritten()
    {
        var game = WriteProject("Game", """<PackageReference Include="Stride.UpgradeRecord.ShaderGate" Version="1.0.0" />""");
        var effect = Path.Combine(Path.GetDirectoryName(game)!, "Main.sdfx");
        File.WriteAllText(effect, "namespace Game { shader Main { mixin Base; }; }");
        var shader = Path.Combine(Path.GetDirectoryName(game)!, "Base.sdsl");
        File.WriteAllText(shader, "namespace Game { shader Base { }; }");

        PackageSessionResult result;
        using (new FileStream(shader, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            result = LoadAndSave(game);

        Assert.Equal("namespace Game { shader Main { mixin Base; }; }", File.ReadAllText(effect));
        Assert.Empty(ReadRecords(game));
        AssertError(result, "Unable to read the shader [Base.sdsl]");
        AssertError(result, "Error while upgrading package [Game] for [Stride.UpgradeRecord.ShaderGate]", "Restore the files it changed from the upgrade backup");
    }

    [Theory]
    [InlineData("UpgradeRecord.Fails", "Error while upgrading package [Game]")]
    [InlineData("UpgradeRecord.Throws", "Error while pre-loading package")]
    public void AFailedUpgradeRecordsNothing(string packageName, string error)
    {
        var game = WriteProject("Game", $"""<PackageReference Include="{packageName}" Version="1.0.0" />""");

        var result = LoadAndSave(game);

        Assert.Empty(ReadRecords(game));
        AssertError(result, error);
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
    public void AProjectStoppedAfterItsAssetsLoadedIsToldToRestoreFromTheBackup()
    {
        var game = WriteProject("Game", """<PackageReference Include="UpgradeRecord.FailsAfterAssets" Version="1.0.0" />""");
        var windows = WriteProject("Game.Windows", """<ProjectReference Include="..\Game\Game.csproj" />""");

        var result = LoadAndSave(WriteSolution(game, windows));

        Assert.Empty(ReadRecords(game));
        Assert.Empty(ReadRecords(windows));
        // Game.Windows fails its own last step, or is stopped by Game's, depending on the order the assets load in
        AssertError(result, "Error while upgrading package [Game] for [UpgradeRecord.FailsAfterAssets]", "Restore the files it changed from the upgrade backup");
        AssertError(result, "[Game.Windows]", "Restore the files it changed from the upgrade backup");
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

    private string WriteSolution(params string[] projects)
    {
        var path = Path.Combine(directory, "Game.slnx");
        var solution = new XElement("Solution", projects.Select(x => new XElement("Project", new XAttribute("Path", Path.GetRelativePath(directory, x).Replace('\\', '/')))));
        solution.Save(path);
        return path;
    }

    private static PackageSessionResult LoadAndSave(string path, Action<PackageLoadParameters>? configure = null)
    {
        var parameters = new PackageLoadParameters { AutoCompileProjects = false, LoadAssemblyReferences = false };
        configure?.Invoke(parameters);
        var result = new PackageSessionResult();
        PackageSession.Load(path, result, parameters);
        Assert.NotNull(result.Session);
        using var session = result.Session;
        session.Save(result);
        return result;
    }

    // The records as the file writes them (an Include or an Update), not as the project evaluates
    private static Dictionary<string, string> ReadRecords(string file)
        => XDocument.Load(file).Descendants(PackageUpgrader.UpgradeRecordItem)
            .ToDictionary(x => (string?)x.Attribute("Include") ?? (string)x.Attribute("Update")!, x => (string?)x.Attribute("Version") ?? "");

    private static string? ReadPackageVersion(string project, string package)
        => XDocument.Load(project).Descendants("PackageReference").Single(x => (string?)x.Attribute("Include") == package).Attribute("Version")?.Value;

    private static string[] ReadFiles(params string[] files) => files.Select(File.ReadAllText).ToArray();

    // An error that holds all the texts
    private static void AssertError(LoggerResult result, params string[] texts)
        => Assert.True(result.Messages.Any(x => x.Type == LogMessageType.Error && texts.All(text => x.Text.Contains(text, StringComparison.Ordinal))),
            $"No error with \"{string.Join("\", \"", texts)}\" in:\n{string.Join("\n", result.Messages.Select(x => $"{x.Type}: {x.Text}"))}");
}
