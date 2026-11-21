// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Build;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The files a build of a game reads and produces, and whether a build can be skipped, on files of a temporary folder.
/// </summary>
public sealed class GameBuildFastPathTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "Stride.Core.Assets.Editor.Tests", "FastPath", Guid.NewGuid().ToString("N"));

    public GameBuildFastPathTests()
    {
        Directory.CreateDirectory(directory);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(directory, true);
        }
        catch (IOException)
        {
            // Best effort
        }
    }

    [Fact]
    public void NothingChangedMeansTheBuildIsUpToDate()
    {
        var game = WriteGame();
        var record = Record(game);

        var check = GameBuildFastPath.Check(record, Capture(game));

        Assert.Equal(GameBuildCheckResult.UpToDate, check.Result);
        Assert.True(check.CanSkip);
        Assert.Empty(check.ChangedFiles);
        Assert.True(check.CheckedFileCount > 0);
    }

    [Fact]
    public void WithoutALastBuildTheProjectBuilds()
    {
        var game = WriteGame();

        var check = GameBuildFastPath.Check(null, Capture(game));

        Assert.Equal(GameBuildCheckResult.NoLastBuild, check.Result);
        Assert.False(check.CanSkip);
    }

    [Theory]
    [InlineData("Game/Player.cs")]
    [InlineData("Game/Game.csproj")]
    [InlineData("Game/Assets/MainScene.sdscene")]
    [InlineData("Game/Game.sdpkg")]
    [InlineData("Game.Linux/Game.Linux.csproj")]
    [InlineData("Game/Resources/Texture.png")]
    public void AWrittenFileOfTheGameIsAChange(string file)
    {
        var game = WriteGame();
        var record = Record(game);

        Touch(file);

        var check = GameBuildFastPath.Check(record, Capture(game));
        Assert.Equal(GameBuildCheckResult.InputsChanged, check.Result);
        Assert.Equal(Full(file), Assert.Single(check.ChangedFiles));
    }

    [Fact]
    public void AnAddedOrADeletedFileIsAChange()
    {
        var game = WriteGame();
        var record = Record(game);

        Write("Game/Enemy.cs", "class Enemy {}");
        File.Delete(Full("Game/Player.cs"));

        var check = GameBuildFastPath.Check(record, Capture(game));
        Assert.Equal(GameBuildCheckResult.InputsChanged, check.Result);
        Assert.Equal([Full("Game/Enemy.cs"), Full("Game/Player.cs")], check.ChangedFiles.Order());
    }

    [Theory]
    [InlineData("Game/bin/Debug/Game.dll")]
    [InlineData("Game/obj/Debug/Game.dll")]
    [InlineData("Game/OBJ/stride/assetbuild/index")]
    [InlineData("Game/.vs/settings.json")]
    [InlineData("Game/.hidden")]
    [InlineData("Game/Game.sdpkg.user")]
    [InlineData("Bin/Linux/Debug/data/db/index")]
    public void TheBuildOutputAndTheEditorsOwnFilesAreNotChanges(string file)
    {
        var game = WriteGame();
        var record = Record(game);

        Write(file, "new " + Guid.NewGuid());

        Assert.True(GameBuildFastPath.Check(record, Capture(game)).CanSkip);
    }

    [Theory]
    [InlineData("Directory.Build.props")]
    [InlineData("Directory.Build.targets")]
    [InlineData("Directory.Packages.props")]
    [InlineData("global.json")]
    [InlineData("nuget.config")]
    public void ABuildFileInAFolderAboveTheProjectsIsAChange(string name)
    {
        // The game in a subfolder, so that the build files are above every folder it reads
        var game = WriteGame("Games/MyGame");
        var record = Record(game);

        Write(name, "<Project />");

        var check = GameBuildFastPath.Check(record, Capture(game));
        Assert.Equal(GameBuildCheckResult.InputsChanged, check.Result);
        Assert.Equal(Full(name), Assert.Single(check.ChangedFiles), ignoreCase: !OperatingSystem.IsLinux());
    }

    [Fact]
    public void TheProjectsTheExecutableReferencesAreRead()
    {
        // A library outside the folders of the session, that the restore of the executable lists
        var game = WriteGame("MyGame");
        Write("Shared/Shared.csproj", "<Project />");
        Write("Shared/Code.cs", "class Code {}");
        WriteRestore("MyGame/Game.Linux", "../../Shared/Shared.csproj", "../Game/Game.csproj");
        var record = Record(game);
        Assert.Contains(Full("Shared"), record.Inputs.ProjectFolders);

        Touch("Shared/Code.cs");

        var check = GameBuildFastPath.Check(record, Capture(game));
        Assert.Equal(GameBuildCheckResult.InputsChanged, check.Result);
        Assert.Equal(Full("Shared/Code.cs"), Assert.Single(check.ChangedFiles));
    }

    [Fact]
    public void AnAssetSourceElsewhereIsRead()
    {
        Write("Elsewhere/Model.fbx", "model");
        var game = WriteGame("MyGame") with { Files = [Full("Elsewhere/Model.fbx")] };
        var record = Record(game);

        Touch("Elsewhere/Model.fbx");

        var check = GameBuildFastPath.Check(record, Capture(game));
        Assert.Equal(GameBuildCheckResult.InputsChanged, check.Result);
        Assert.Equal(Full("Elsewhere/Model.fbx"), Assert.Single(check.ChangedFiles));
    }

    [Fact]
    public void ADeletedOrWrittenOutputMeansABuild()
    {
        var game = WriteGame();
        var record = Record(game);

        File.Delete(Full("Bin/Linux/Debug/data/db/bundles/default.bundle"));

        var check = GameBuildFastPath.Check(record, Capture(game));
        Assert.Equal(GameBuildCheckResult.OutputChanged, check.Result);
        Assert.Equal(Full("Bin/Linux/Debug/data/db/bundles/default.bundle"), Assert.Single(check.ChangedFiles));

        // The assembly itself
        record = Record(game);
        Touch("Bin/Linux/Debug/Game.Linux.dll");
        Assert.Equal(GameBuildCheckResult.OutputChanged, GameBuildFastPath.Check(record, Capture(game)).Result);
    }

    [Fact]
    public void NewFilesInTheOutputAreNotChanges()
    {
        // The game writes its own files next to its assembly as it runs (logs, caches)
        var game = WriteGame();
        var record = Record(game);

        Write("Bin/Linux/Debug/local/cache.db", "cache");

        Assert.True(GameBuildFastPath.Check(record, Capture(game)).CanSkip);
    }

    [Theory]
    [InlineData("cache/pipelines.bin")]
    [InlineData("local/db/index")]
    [InlineData("roaming/settings.xml")]
    public void TheFilesTheGameWritesInItsOwnFoldersAreNotChanges(string file)
    {
        // Written by an earlier run, then again by the run after the build
        Write("Bin/Linux/Debug/" + file, "first run");
        var game = WriteGame();
        var record = Record(game);

        Write("Bin/Linux/Debug/" + file, "next run, longer");

        Assert.True(GameBuildFastPath.Check(record, Capture(game)).CanSkip);
    }

    [Fact]
    public void ARestoreMeansABuild()
    {
        var game = WriteGame();
        WriteRestore("Game.Linux", "../Game/Game.csproj");
        WriteRestore("Game");
        var record = Record(game);

        // A restore of the library: a new package version, for example
        Write("Game/obj/project.assets.json", """{ "version": 3, "libraries": { "Stride.Engine/4.5.1": { "type": "package" } } }""");

        var check = GameBuildFastPath.Check(record, Capture(game));
        Assert.Equal(GameBuildCheckResult.Restored, check.Result);
        Assert.Equal(Full("Game/obj/project.assets.json"), Assert.Single(check.ChangedFiles));
    }

    [Fact]
    public void TheKeySeparatesTheProjectsPlatformsConfigurationsAndProperties()
    {
        var project = Full("Game.Linux/Game.Linux.csproj");
        var request = new GameProjectBuildRequest(project, "Linux", new Dictionary<string, string> { ["SolutionPlatform"] = "Linux", ["StrideBuildEngineLogVerbose"] = "true" });
        var key = GameBuildFastPath.GetKey(request);

        // The same build, its properties in another order
        Assert.Equal(key, GameBuildFastPath.GetKey(new GameProjectBuildRequest(project, "Linux", new Dictionary<string, string> { ["StrideBuildEngineLogVerbose"] = "true", ["SolutionPlatform"] = "Linux" })));

        Assert.NotEqual(key, GameBuildFastPath.GetKey(request with { Configuration = "Release" }));
        Assert.NotEqual(key, GameBuildFastPath.GetKey(request with { Target = "Build;SignAndroidPackage" }));
        Assert.NotEqual(key, GameBuildFastPath.GetKey(request with { Platform = "AnyCPU" }));
        Assert.NotEqual(key, GameBuildFastPath.GetKey(request with { ProjectPath = Full("Game.Windows/Game.Windows.csproj") }));
        Assert.NotEqual(key, GameBuildFastPath.GetKey(request with { Properties = new Dictionary<string, string> { ["SolutionPlatform"] = "Linux" } }));
    }

    [Fact]
    public void TheFastPathRemembersOneBuildByKey()
    {
        var game = WriteGame();
        var fastPath = new GameBuildFastPath();
        var record = Record(game);

        fastPath.Record("linux", record);
        Assert.Same(record, fastPath.GetLastBuild("linux"));
        Assert.Null(fastPath.GetLastBuild("windows"));

        fastPath.Forget("linux");
        Assert.Null(fastPath.GetLastBuild("linux"));

        fastPath.Record("linux", record);
        fastPath.Clear();
        Assert.Null(fastPath.GetLastBuild("linux"));
    }

    [Fact]
    public void ASkippedBuildGivesTheLastAssemblyAndTargetOutputs()
    {
        var game = WriteGame();
        var record = Record(game);

        var build = record.ToBuild();

        Assert.Equal(Full("Bin/Linux/Debug/Game.Linux.dll"), build.AssemblyPath);
        Assert.True(build.Completion.IsCompletedSuccessfully);
        Assert.True(build.Completion.Result);
        Assert.False(build.IsCanceled);
        Assert.Equal([Full("Bin/Linux/Debug/Game.Linux.apk")], build.TargetOutputs["GetAndroidPackage"]);
    }

    /// <summary>
    /// Writes a game as the template makes it: a library with its package, code, assets and resources, an executable, and the
    /// output of a build.
    /// </summary>
    /// <param name="folder">The folder of the solution, in the temporary folder.</param>
    private GameBuildInputScope WriteGame(string folder = "")
    {
        string In(string file) => folder.Length == 0 ? file : folder + "/" + file;
        Write(In("Game.slnx"), "<Solution />");
        Write(In("Game/Game.csproj"), "<Project />");
        Write(In("Game/Game.sdpkg"), "!Package");
        Write(In("Game/Player.cs"), "class Player {}");
        Write(In("Game/Assets/MainScene.sdscene"), "!SceneAsset");
        Write(In("Game/Resources/Texture.png"), "png");
        Write(In("Game.Linux/Game.Linux.csproj"), "<Project />");
        Write(In("Game.Linux/GameApp.cs"), "class GameApp {}");
        Write(In("Bin/Linux/Debug/Game.Linux.dll"), "dll");
        Write(In("Bin/Linux/Debug/Game.Linux.apk"), "apk");
        Write(In("Bin/Linux/Debug/data/db/bundles/default.bundle"), "bundle");
        var root = Full(folder);
        return new GameBuildInputScope(Full(In("Game.Linux/Game.Linux.csproj")),
            [root, Full(In("Game")), Full(In("Game/Assets")), Full(In("Game/Resources"))],
            [Full(In("Game.slnx"))]);
    }

    private static GameBuildInputs Capture(GameBuildInputScope game) => GameBuildInputs.Capture(game);

    /// <summary>
    /// A successful build of the game, as it is now: its assembly is in the Bin folder next to its solution.
    /// </summary>
    private static GameBuildRecord Record(GameBuildInputScope game)
    {
        var inputs = Capture(game);
        var assembly = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(game.ProjectPath))!, "Bin", "Linux", "Debug", "Game.Linux.dll");
        var targetOutputs = new Dictionary<string, IReadOnlyList<string>> { ["GetAndroidPackage"] = [Path.ChangeExtension(assembly, ".apk")] };
        var outputs = GameBuildOutputs.Capture(assembly, targetOutputs, inputs.ProjectFolders);
        Assert.True(outputs.HasAssembly);
        return new GameBuildRecord(inputs, outputs, new DateTime(2026, 11, 18, 11, 42, 0), assembly, targetOutputs);
    }

    private void WriteRestore(string projectFolder, params string[] projectReferences)
    {
        var libraries = string.Join(", ", projectReferences.Select((x, i) => $"\"Reference{i}/1.0.0\": {{ \"type\": \"project\", \"path\": \"{x}\", \"msbuildProject\": \"{x}\" }}"));
        Write(projectFolder + "/obj/project.assets.json", $$"""{ "version": 3, "libraries": { {{libraries}} } }""");
    }

    private string Full(string file) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(directory, file)));

    private void Write(string file, string content)
    {
        var path = Full(file);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    /// <summary>
    /// Writes a file again, with the same size: only its time tells the change.
    /// </summary>
    private void Touch(string file)
    {
        var path = Full(file);
        File.WriteAllText(path, File.ReadAllText(path));
        File.SetLastWriteTimeUtc(path, File.GetLastWriteTimeUtc(path).AddSeconds(2));
    }
}
