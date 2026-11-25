// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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

    // A project written for <version> that references the projects <references>, and its package
    private string WriteProject(string name, string version, params string[] references)
    {
        var items = string.Concat(references.Select(x => $"""
                <ProjectReference Include="..\{x}\{x}.csproj" />

            """));
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
        var current = AssetRegistry.GetPackageUpgrader("Stride.Engine")!.Attribute.UpdatedVersionRange.MinVersion.ToString();
        WriteProject("Library", "4.4.1");
        var library = Write("Library/Effects/LibOld.sdsl", "shader LibOld { stage float Value; };\n");
        WriteProject("Game", current, "Library");
        const string helpers = "shader GameHelpers { float3 Tint(float3 color) { return color; } };\n";
        const string fx = "shader GameFx : GameHelpers { float3 Apply(float3 color) { return Tint(color); } };\n";
        var gameHelpers = Write("Game/Effects/GameHelpers.sdsl", helpers);
        var gameFx = Write("Game/Effects/GameFx.sdsl", fx);

        var log = LoadSession(WriteSolutionFile("Game", "Library"));

        Assert.True(File.ReadAllText(library).Contains("module float Value;", StringComparison.Ordinal), string.Join(Environment.NewLine, log.Messages));
        Assert.Equal(helpers, File.ReadAllText(gameHelpers));
        Assert.Equal(fx, File.ReadAllText(gameFx));
    }
}
