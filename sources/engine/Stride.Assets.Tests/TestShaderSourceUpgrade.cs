// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Xunit;

namespace Stride.Assets.Tests;

/// <summary>
/// Which files the upgrade of a project reaches.
/// </summary>
public class TestShaderSourceUpgrade : IDisposable
{
    // The shader gate of the engine's upgrader, without its other upgrades
    private sealed class ShaderUpgrader : CodePackageUpgrader
    {
        public override void DeclareUpgrades(UpgradeRegistry registry)
        {
            var engine = new UpgradeRegistry();
            new StridePackageUpgrader().DeclareUpgrades(engine);
            registry.ShaderUpgrades.AddRange(engine.ShaderUpgrades);
        }
    }

    // The session's runner of source-code migrations, for the projects that have C# files: those of these tests have none, so there is nothing
    // to migrate, and their sessions upgrade without the restore that a migration needs
    private sealed class CSharpProjectsRunner(ICodeUpgradeRunner inner) : ICodeUpgradeRunner
    {
        public bool HasSourceMigrations(IReadOnlyList<PendingCodeUpgrade> pending)
            => inner?.HasSourceMigrations(WithCSharp(pending)) ?? false;

        public bool Run(UFile solutionPath, IReadOnlyList<PendingCodeUpgrade> pending, UpgradeBackup backup, ILogger log)
            => inner?.Run(solutionPath, WithCSharp(pending), backup, log) ?? true;

        private static List<PendingCodeUpgrade> WithCSharp(IReadOnlyList<PendingCodeUpgrade> pending)
            => pending.Where(x => Directory.EnumerateFiles(x.ProjectFullPath.GetFullDirectory().ToOSPath(), "*.cs", SearchOption.AllDirectories).Any()).ToList();
    }

    private static readonly Lock RunnerLock = new();
    private readonly string directory = Directory.CreateTempSubdirectory("stride-shader-upgrade-").FullName;

    public TestShaderSourceUpgrade()
    {
        // The runner is global: the one Stride.Assets registers keeps serving every project with C# files
        lock (RunnerLock)
        {
            RuntimeHelpers.RunModuleConstructor(typeof(StridePackageUpgrader).Module.ModuleHandle);
            if (CodeUpgradeRunner.Instance is not CSharpProjectsRunner)
                CodeUpgradeRunner.Instance = new CSharpProjectsRunner(CodeUpgradeRunner.Instance);
        }
    }

    // The version that the engine's upgrader moves a project to
    private static string CurrentVersion => AssetRegistry.GetPackageUpgrader("Stride.Engine")!.Attribute.UpdatedVersionRange.MinVersion.ToString();

    public void Dispose()
    {
        Directory.Delete(directory, recursive: true);
        GC.SuppressFinalize(this);
    }

    private string Write(string path, string text)
    {
        var fullPath = Path.Combine(directory, path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, text);
        return fullPath;
    }

    // The two steps of the upgrade of a project, as the session runs them around the restore
    private LoggerResult UpgradeFrom(string version, params string[] assetFolders)
    {
        var package = new Package { Meta = { Name = "Game" }, FullPath = Path.Combine(directory, "Game", "Game.sdpkg") };
        foreach (var folder in assetFolders)
            package.AssetFolders.Add(new AssetFolder(folder));
        _ = new SolutionProject(package, Guid.NewGuid(), Write("Game/Game.csproj", "<Project />"));

        var dependency = new PackageDependency("Stride.Engine", new PackageVersionRange(new PackageVersion(version)));
        var log = new LoggerResult();
        var session = new PackageSession();
        Assert.True(new ShaderUpgrader().UpgradeBeforeAssembliesLoaded(new PackageLoadParameters(), session, log, package, dependency, null));
        Assert.True(new ShaderUpgrader().UpgradeAfterRestore(new PackageLoadParameters(), session, log, package, dependency, null));
        return log;
    }

    // The gate is the number of the release that changed the language: a project stamped below it is upgraded, one stamped on it is not
    [Theory]
    [InlineData("4.5.4", true)]
    [InlineData("4.5.4-dev3", true)]
    [InlineData("4.5.5", false)]
    [InlineData("4.5.5-beta", false)]
    public void TheShaderUpgradeRunsForAProjectBelowTheGate(string version, bool upgraded)
    {
        const string text = "shader Own { stage float Value; };\n";
        var effect = Write("Game/Effects/Own.sdsl", text);

        UpgradeFrom(version);

        Assert.Equal(upgraded, File.ReadAllText(effect) != text);
        Assert.Equal(upgraded, File.ReadAllText(effect).Contains("module float Value;", StringComparison.Ordinal));
    }

    // The gate 4.4.2 writes `effect` for the `shader` that declared an effect of Stride 4.3
    [Theory]
    [InlineData("4.4.1", true)]
    [InlineData("4.4.2", false)]
    public void TheEffectsOfStride43AreDeclaredWithEffect(string version, bool upgraded)
    {
        const string text = "namespace Game\n{\n    shader Old\n    {\n        mixin OldBase;\n    };\n}\n";
        var effect = Write("Game/Effects/Old.sdfx", text);

        UpgradeFrom(version);

        Assert.Equal(upgraded ? text.Replace("shader Old", "effect Old") : text, File.ReadAllText(effect));
    }

    // An upgraded shader keeps its byte order mark, or its lack of one
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnUpgradedShaderKeepsItsByteOrderMark(bool byteOrderMark)
    {
        var encoding = new UTF8Encoding(byteOrderMark);
        var effect = Write("Game/Effects/Own.sdsl", "");
        File.WriteAllText(effect, "shader Own { stage float Value; };\n", encoding);

        UpgradeFrom("4.4.1.0");

        var bytes = File.ReadAllBytes(effect);
        Assert.Equal(byteOrderMark, bytes.AsSpan().StartsWith(new UTF8Encoding(true).GetPreamble()));
        Assert.Contains("module float Value;", File.ReadAllText(effect));
    }

    // Build output and the backups of earlier upgrades keep their shaders: the class of a backup must not stand for the project's
    [Theory]
    [InlineData("Game/bin/Own.sdsl")]
    [InlineData("Game/obj/Own.sdsl")]
    [InlineData("Game/Bin/Own.sdsl")]
    [InlineData("Game/OBJ/Own.sdsl")]
    [InlineData("Game/.stride-backup-20261102-073632/Effects/Own.sdsl")]
    public void ShadersOfBuildOutputAndBackupsAreLeftAlone(string path)
    {
        const string text = "shader Own { stage float Value; };\n";
        var file = Write(path, text);
        var effect = Write("Game/Effects/Own.sdsl", text);

        UpgradeFrom("4.4.1.0");

        Assert.Equal(text, File.ReadAllText(file));
        Assert.Contains("module float Value;", File.ReadAllText(effect));
    }

    // The asset folders of the package hold its files too, also outside the project folder
    [Fact]
    public void TheShadersOfAnAssetFolderOutsideTheProjectAreUpgraded()
    {
        const string text = "shader Shared { stage float Value; };\n";
        var shared = Write("Shared/Effects/Shared.sdsl", text);

        UpgradeFrom("4.4.1.0", "../Shared");

        Assert.Contains("module float Value;", File.ReadAllText(shared));
    }

    // An asset folder inside the project folder is read with it, also when the package names it with another case
    [Fact]
    public void AShaderOfAnAssetFolderInsideTheProjectIsUpgradedOnce()
    {
        var own = Write("Game/Assets/Own.sdsl", "shader Own { stage float Value; };\n");

        var log = UpgradeFrom("4.4.1.0", "assets");

        Assert.Equal("Upgraded shader [Assets/Own.sdsl] to the current shader language", Assert.Single(log.Messages, x => x.Text.StartsWith("Upgraded shader", StringComparison.Ordinal)).Text);
        Assert.DoesNotContain(log.Messages, x => x.Type == LogMessageType.Warning);
        Assert.Equal("shader Own { module float Value; };\n", File.ReadAllText(own));
    }

    // The messages name a file from the folder of the project, as the other messages of the project upgrade do
    [Fact]
    public void TheMessagesNameFilesFromTheProjectFolder()
    {
        Write("Game/Effects/Old.sdfx", "namespace Game\n{\n    shader Old\n    {\n        mixin OldBase;\n    };\n}\n");
        var own = Write("Game/Effects/Own.sdsl", "class Own { stage float Value; };\n");
        Write("Shared/Effects/Shared.sdsl", "shader Shared { stage float Value; };\n");

        var log = UpgradeFrom("4.3.0", "../Shared");

        var messages = log.Messages.Select(x => x.Text).ToList();
        Assert.Contains("[Effects/Old.sdfx]: 1 effect(s) declared with 'shader' are now declared with 'effect'.", messages);
        Assert.Contains("Upgraded shader [Effects/Own.sdsl] to the current shader language", messages);
        Assert.Contains("Upgraded shader [../Shared/Effects/Shared.sdsl] to the current shader language", messages);
        Assert.DoesNotContain(messages, x => x.Contains(directory, StringComparison.OrdinalIgnoreCase));
        Assert.Equal("shader Own { module float Value; };\n", File.ReadAllText(own));
    }

    // A project written for <version> that references the projects <references> and the packages <packages>, and its package
    private string WriteProject(string name, string version, string[] references = null, string[] packages = null)
    {
        var items = string.Concat((references ?? []).Select(x => $"""
                <ProjectReference Include="..\{x}\{x}.csproj" />

            """).Concat((packages ?? []).Select(x => $"""
                <PackageReference Include="{x}" Version="1.0.0" />

            """)));
        var project = Write($"{name}/{name}.csproj", $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Stride.Engine" Version="{version}" />
            {items}  </ItemGroup>
            </Project>
            """);
        var packagePath = Path.Combine(directory, name, $"{name}.sdpkg");
        AssetFileSerializer.Save(packagePath, new Package { FullPath = packagePath, Meta = { Name = name, Version = new PackageVersion("1.0.0") } }, null);
        return project;
    }

    private string WriteSolutionFile(params string[] projects)
    {
        var entries = string.Concat(projects.Select(x => $$"""
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "{{x}}", "{{x}}\{{x}}.csproj", "{{{Guid.NewGuid().ToString().ToUpperInvariant()}}}"
            EndProject

            """));
        return Write("Game.sln", $"""
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            {entries}Global
            EndGlobal

            """);
    }

    // The session upgrades every project of the solution, with no restore: these projects use no package but Stride
    private LoggerResult LoadSession(string path)
    {
        var result = new PackageSessionResult();
        PackageSession.Load(path, result, new PackageLoadParameters
        {
            AutoCompileProjects = false,
            LoadAssemblyReferences = false,
            PackageUpgradeRequested = (_, _) => PackageUpgradeRequestedAnswer.Upgrade,
        });
        result.Session?.Dispose();
        return result;
    }

    // A project upgrades from its own Stride version. A game already on this version keeps its shaders, which may have been written after its
    // upgrade (the gate would make GameHelpers a module), while the library it references crosses the gate
    [Fact]
    public void ASessionUpgradesEachProjectFromItsOwnVersion()
    {
        WriteProject("Library", "4.4.1");
        var library = Write("Library/Effects/LibOld.sdsl", "shader LibOld { stage float Value; };\n");
        WriteProject("Game", CurrentVersion, ["Library"]);
        const string helpers = "shader GameHelpers { float3 Tint(float3 color) { return color; } };\n";
        const string fx = "shader GameFx : GameHelpers { float3 Apply(float3 color) { return Tint(color); } };\n";
        var gameHelpers = Write("Game/Effects/GameHelpers.sdsl", helpers);
        var gameFx = Write("Game/Effects/GameFx.sdsl", fx);

        var log = LoadSession(WriteSolutionFile("Game", "Library"));

        Assert.True(File.ReadAllText(library).Contains("module float Value;", StringComparison.Ordinal), string.Join(Environment.NewLine, log.Messages));
        Assert.Equal(helpers, File.ReadAllText(gameHelpers));
        Assert.Equal(fx, File.ReadAllText(gameFx));
    }

    // The projects that a project references are read from its project too, upgraded before it whatever the order of the solution: with no
    // restore output here, the game still sees that LibHelpers became a module
    [Theory]
    [InlineData("Library", "Game")]
    [InlineData("Game", "Library")]
    public void ASessionReadsTheReferencedProjectsWithoutARestore(string first, string second)
    {
        WriteProject("Library", "4.4.1");
        Write("Library/Effects/LibHelpers.sdsl", "shader LibHelpers { stage float3 Tint(float3 color) { return color; } };\n");
        Write("Library/Effects/LibUser.sdsl", "shader LibUser : ComputeColor, LibHelpers { override float4 Compute() { return float4(Tint(1), 1); } };\n");
        WriteProject("Game", "4.4.1", ["Library"]);
        var game = Write("Game/Effects/GameFx.sdsl", "shader GameFx : ComputeColor, LibHelpers { override float4 Compute() { return float4(Tint(0.5), 1); } };\n");

        var log = LoadSession(WriteSolutionFile(first, second));

        Assert.True(File.ReadAllText(game).Contains("import LibHelpers;", StringComparison.Ordinal), string.Join(Environment.NewLine, log.Messages));
    }

    // A project whose packages the shader upgrade can't read (no restore output, or a failed restore) isn't upgraded: without them it would
    // write other files, which no later upgrade would fix once its references name this version. Nothing in it changes
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AProjectWhosePackagesCantBeReadIsNotUpgraded(bool failedRestore)
    {
        var project = WriteProject("Game", "4.4.1", packages: ["Some.Plugin"]);
        const string text = "shader GameFx { stage float Value; };\n";
        var shader = Write("Game/Effects/GameFx.sdsl", text);
        if (failedRestore)
        {
            Write("Game/obj/project.assets.json", new JsonObject
            {
                ["version"] = 3,
                ["targets"] = new JsonObject { ["net10.0"] = new JsonObject() },
                ["libraries"] = new JsonObject(),
                ["projectFileDependencyGroups"] = new JsonObject(),
                ["packageFolders"] = new JsonObject(),
                ["logs"] = new JsonArray(new JsonObject { ["code"] = "NU1101", ["level"] = "Error", ["message"] = "Unable to find package Some.Plugin.", ["libraryId"] = "Some.Plugin" }),
            }.ToJsonString());
        }
        var projectText = File.ReadAllText(project);

        var log = LoadSession(WriteSolutionFile("Game"));

        Assert.Equal(text, File.ReadAllText(shader));
        Assert.Equal(projectText, File.ReadAllText(project));
        var reason = failedRestore ? "its restore failed: Unable to find package Some.Plugin." : "it has no restore output (obj/project.assets.json)";
        Assert.Contains(log.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains($"Unable to upgrade project [Game]: {reason}", StringComparison.Ordinal));
    }

    // tests/shader-upgrade/dependencies/<case>/solution.txt: the projects of a solution, each referencing the projects and the plugins before
    // it. "<Project>": its shaders before the upgrade in <case>/<Project>/before/, and after it in after/; "<Project> refused": the user
    // didn't upgrade it; "plugin <Name>": a NuGet package with the shaders of <case>/<Name>/, which nothing writes
    public static TheoryData<string> SolutionFixtures()
    {
        var data = new TheoryData<string>();
        foreach (var fixture in Directory.EnumerateDirectories(Path.Combine(FixturesDirectory, "dependencies")).Where(x => File.Exists(Path.Combine(x, "solution.txt"))).Order(StringComparer.Ordinal))
            data.Add(Path.GetFileName(fixture));
        return data;
    }

    private static string FixturesDirectory { get; } = FindFixtures();

    private static string FindFixtures()
    {
        for (var parent = new DirectoryInfo(AppContext.BaseDirectory); parent != null; parent = parent.Parent)
        {
            var fixtures = Path.Combine(parent.FullName, "tests", "shader-upgrade");
            if (Directory.Exists(fixtures))
                return fixtures;
        }
        throw new DirectoryNotFoundException("tests/shader-upgrade is not above the test assembly");
    }

    // A plugin is written for <StrideVersion> and depends on the plugins <Uses>; a project for this version, or for 4.4.1 when the user refused
    // its upgrade
    private sealed record SolutionMember(string Name, bool Refused, bool IsPlugin, string StrideVersion, IReadOnlyList<string> Uses);

    private sealed record SolutionFixture(string Directory, List<SolutionMember> Members);

    private static SolutionFixture ReadSolution(string fixture)
    {
        var fixtureDirectory = Path.Combine(FixturesDirectory, "dependencies", fixture);
        var members = File.ReadAllLines(Path.Combine(fixtureDirectory, "solution.txt")).Where(x => x.Length > 0 && x[0] != '#').Select(x => x.Split(' '))
            .Select(x => x[0] == "plugin" ? new SolutionMember(x[1], false, true, x[2], x[3..])
                : new SolutionMember(x[0], x.Length > 1 && x[1] == "refused", false, x.Length > 1 && x[1] == "refused" ? "4.4.1" : CurrentVersion, []))
            .ToList();
        return new SolutionFixture(fixtureDirectory, members);
    }

    // Each project as a game, <root>/<project>/<project>.csproj with its shaders in Effects/ and a restore output that lists the projects and
    // the plugins before it; the plugins in the NuGet folder <root>/packages
    // <pluginFiles>: the files of a plugin (by name), in place of those of the fixture
    private List<(Package Package, bool Refused)> WriteSolution(SolutionFixture solution, string root, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> pluginFiles = null)
    {
        var packageFolder = Path.Combine(directory, root, "packages") + Path.DirectorySeparatorChar;
        var libraries = new JsonObject();
        var targetLibraries = new JsonObject();
        var projects = new List<(Package, bool)>();
        foreach (var member in solution.Members)
        {
            if (member.IsPlugin)
            {
                var path = $"{member.Name.ToLowerInvariant()}/1.0.0";
                var files = pluginFiles?.GetValueOrDefault(member.Name)
                    ?? Directory.EnumerateFiles(Path.Combine(solution.Directory, member.Name)).ToDictionary(Path.GetFileName, File.ReadAllText);
                foreach (var (file, text) in files)
                    Write($"{root}/packages/{path}/stride/Assets/Effects/{file}", text);
                libraries[$"{member.Name}/1.0.0"] = new JsonObject { ["type"] = "package", ["path"] = path, ["files"] = new JsonArray() };
                var dependencies = new JsonObject { ["Stride.Engine"] = member.StrideVersion };
                foreach (var used in member.Uses)
                    dependencies[used] = "1.0.0";
                targetLibraries[$"{member.Name}/1.0.0"] = new JsonObject { ["type"] = "package", ["dependencies"] = dependencies };
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(Path.Combine(solution.Directory, member.Name, "before")))
                Write($"{root}/{member.Name}/Effects/{Path.GetFileName(file)}", File.ReadAllText(file));
            var restore = new JsonObject
            {
                ["version"] = 3,
                ["targets"] = new JsonObject { ["net10.0"] = targetLibraries.DeepClone() },
                ["libraries"] = libraries.DeepClone(),
                ["projectFileDependencyGroups"] = new JsonObject(),
                ["packageFolders"] = new JsonObject { [packageFolder] = new JsonObject() },
            };
            Write($"{root}/{member.Name}/obj/project.assets.json", restore.ToJsonString());
            var package = new Package { Meta = { Name = member.Name }, FullPath = Path.Combine(directory, root, member.Name, $"{member.Name}.sdpkg") };
            _ = new SolutionProject(package, Guid.NewGuid(), Write($"{root}/{member.Name}/{member.Name}.csproj", "<Project />"));
            projects.Add((package, member.Refused));
            libraries[$"{member.Name}/1.0.0"] = new JsonObject { ["type"] = "project", ["path"] = $"../{member.Name}/{member.Name}.csproj", ["msbuildProject"] = $"../{member.Name}/{member.Name}.csproj" };
            targetLibraries[$"{member.Name}/1.0.0"] = new JsonObject { ["type"] = "project", ["dependencies"] = new JsonObject { ["Stride.Engine"] = member.StrideVersion } };
        }
        return projects;
    }

    // Each project the user upgrades, in reference order, as the session does it (PackageSession.PreLoadPackageDependencies, then
    // PackageSession.TryLoadAssemblies)
    private static void UpgradeSolution(List<(Package Package, bool Refused)> projects, CodePackageUpgrader upgrader, string version, LoggerResult log)
    {
        var dependency = new PackageDependency("Stride.Engine", new PackageVersionRange(new PackageVersion(version)));
        foreach (var (package, _) in projects.Where(x => !x.Refused))
        {
            var session = new PackageSession();
            Assert.True(upgrader.UpgradeBeforeAssembliesLoaded(new PackageLoadParameters(), session, log, package, dependency, null));
            Assert.True(upgrader.UpgradeAfterRestore(new PackageLoadParameters(), session, log, package, dependency, null));
        }
    }

    // The shaders of the projects of a copy of a solution, by <project>/<file>
    private Dictionary<string, string> ReadWritten(SolutionFixture solution, string root)
        => ReadShaders(solution.Members.Where(x => !x.IsPlugin).Select(x => (x.Name, Path.Combine(directory, root, x.Name, "Effects"))));

    // The shaders of the projects of a fixture as they should end: after/, or before/ for a project the user didn't upgrade
    private static Dictionary<string, string> ReadExpected(SolutionFixture solution)
        => ReadShaders(solution.Members.Where(x => !x.IsPlugin).Select(x => (x.Name, Path.Combine(solution.Directory, x.Name, x.Refused ? "before" : "after"))));

    private static Dictionary<string, string> ReadShaders(IEnumerable<(string Project, string Folder)> folders)
        => folders.SelectMany(x => Directory.EnumerateFiles(x.Folder).Select(file => (Key: $"{x.Project}/{Path.GetFileName(file)}", Text: File.ReadAllText(file).ReplaceLineEndings("\n"))))
            .ToDictionary(x => x.Key, x => x.Text);

    // A project reads what it references, already upgraded, and writes only its own files: a library never changes for its users, and nothing
    // writes a project the user didn't upgrade, or a plugin. Running it again changes nothing. With STRIDE_SHADER_UPGRADE_UPDATE=1, it writes
    // the after/ folders instead (review the result before committing it)
    [Theory]
    [MemberData(nameof(SolutionFixtures))]
    public void TheProjectsOfASolutionAreUpgradedInReferenceOrder(string fixture)
    {
        var solution = ReadSolution(fixture);
        var projects = WriteSolution(solution, "solution");
        var plugins = Directory.Exists(Path.Combine(directory, "solution", "packages"))
            ? Directory.EnumerateFiles(Path.Combine(directory, "solution", "packages"), "*", SearchOption.AllDirectories).ToDictionary(x => x, File.ReadAllText)
            : [];

        var log = new LoggerResult();
        UpgradeSolution(projects, new ShaderUpgrader(), "4.4.1.0", log);

        var written = ReadWritten(solution, "solution");
        if (Environment.GetEnvironmentVariable("STRIDE_SHADER_UPGRADE_UPDATE") == "1")
        {
            foreach (var (name, text) in written.Where(x => !solution.Members.Single(y => !y.IsPlugin && x.Key.StartsWith($"{y.Name}/", StringComparison.Ordinal)).Refused))
            {
                var after = Path.Combine(solution.Directory, Path.GetDirectoryName(name)!, "after", Path.GetFileName(name));
                Directory.CreateDirectory(Path.GetDirectoryName(after)!);
                File.WriteAllText(after, text);
            }
            foreach (var message in log.Messages)
                Console.WriteLine($"{fixture}: {message.Text}");
            return;
        }

        var expected = ReadExpected(solution);
        Assert.Equal(expected.Keys.Order(), written.Keys.Order());
        foreach (var (name, text) in written)
            Assert.Equal(expected[name], text);
        Assert.All(plugins, x => Assert.Equal(x.Value, File.ReadAllText(x.Key)));

        UpgradeSolution(projects, new ShaderUpgrader(), "4.4.1.0", new LoggerResult());
        Assert.Equal(written, ReadWritten(solution, "solution"));
    }

    // The files of a plugin as the project upgrade of its author writes them, gate by gate from <version>
    private static IReadOnlyDictionary<string, string> UpgradedAsAProject(string folder, string version)
    {
        var registry = new UpgradeRegistry();
        new ShaderUpgrader().DeclareUpgrades(registry);
        IReadOnlyDictionary<string, string> files = Directory.EnumerateFiles(folder).ToDictionary(Path.GetFileName, File.ReadAllText);
        foreach (var gate in registry.ShaderUpgrades.Where(x => new PackageVersion(version).Version < x.GateVersion.Version).OrderBy(x => x.GateVersion.Version))
            files = Shaders.Parsing.Upgrade.ShaderUpgrader.Upgrade(files, Shaders.Parsing.Upgrade.EngineShaderClasses.Current, steps: gate.Steps).Files;
        return files;
    }

    // The upgrade of a game reads an old plugin as the upgrade of its author writes it. So upgrading the game, then moving to the version of the
    // plugin for this Stride, ends as moving the plugin first, then upgrading the game
    [Fact]
    public void AGameOnAnOldPluginEndsTheSameInEitherOrder()
    {
        var solution = ReadSolution("old-plugin");
        var plugin = solution.Members.Single(x => x.IsPlugin);
        var log = new LoggerResult();
        UpgradeSolution(WriteSolution(solution, "game-first"), new ShaderUpgrader(), "4.4.1.0", log);

        var pluginFirst = solution with { Members = solution.Members.Select(x => x.IsPlugin ? x with { StrideVersion = CurrentVersion } : x).ToList() };
        var upgradedPlugin = new Dictionary<string, IReadOnlyDictionary<string, string>> { [plugin.Name] = UpgradedAsAProject(Path.Combine(solution.Directory, plugin.Name), plugin.StrideVersion) };
        UpgradeSolution(WriteSolution(pluginFirst, "plugin-first", upgradedPlugin), new ShaderUpgrader(), "4.4.1.0", new LoggerResult());

        Assert.Equal(ReadWritten(solution, "plugin-first"), ReadWritten(solution, "game-first"));
        Assert.Single(log.Messages, x => x.Text.Contains($"{plugin.Name} is written for Stride 4.4.1", StringComparison.Ordinal));
    }
}
