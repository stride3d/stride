// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.IO;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The watcher of the game code: which changes count, how they are grouped, and which projects of a session it watches.
/// </summary>
public sealed class GameCodeWatcherTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    // Long enough for the changes a test reports in a row to make one burst on a slow machine
    private static readonly TimeSpan OneBurstDelay = TimeSpan.FromSeconds(1);

    private readonly string directory = Path.Combine(Path.GetTempPath(), "Stride.Core.Assets.Editor.Tests", Guid.NewGuid().ToString("N"));
    private readonly string game;
    private readonly string head;
    private readonly string nestedHead;

    public GameCodeWatcherTests()
    {
        game = WriteProject("MyGame", "MyGame");
        head = WriteProject("MyGame.Windows", "MyGame.Windows");
        // An executable in a folder of the library: its files are its own
        nestedHead = WriteProject(Path.Combine("MyGame", "Tools"), "Tools");
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
    public void AWatcherThatCannotStartLetsTheSessionOpen()
    {
        // As on Linux past the inotify limits
        using var watcher = new GameCodeWatcher([game], [head], TimeSpan.FromMilliseconds(100), watchFileSystem: true,
            (_, _) => throw new IOException("The configured user limit (128) on the number of inotify instances has been reached."));

        Assert.False(watcher.IsWatchingFileSystem);
        Assert.Equal([Path.GetFullPath(game)], watcher.CodeProjects);
    }

    [Fact]
    public void TheCodeProjectsAreWatched()
    {
        using var watcher = new GameCodeWatcher([game], [head]);

        Assert.True(watcher.IsWatchingFileSystem);
    }

    [Fact]
    public async Task TheSourcesAndTheProjectFileOfAWatchedProjectCount()
    {
        using var watcher = CreateWatcher(OneBurstDelay);
        var bursts = Record(watcher);
        var gameDirectory = Path.GetDirectoryName(game)!;
        File.WriteAllText(game, ProjectFile("MyGame") + "<!-- edited -->");

        Change(watcher, FileEventChangeType.Changed, Path.Combine(gameDirectory, "Player.cs"));
        Change(watcher, FileEventChangeType.Created, Path.Combine(gameDirectory, "Scripts", "Enemy.cs"));
        Change(watcher, FileEventChangeType.Deleted, Path.Combine(gameDirectory, "Old.CS"));
        watcher.OnFileChanged(new FileRenameEvent("New.cs", Path.Combine(gameDirectory, "New.cs"), Path.Combine(gameDirectory, "Before.txt")));
        Change(watcher, FileEventChangeType.Changed, game);

        var burst = await bursts.Reader.ReadAsync().AsTask().WaitAsync(Timeout);
        Assert.All(burst, x => Assert.Equal(game, x.ProjectPath));
        Assert.Equal(
            new[] { "Player.cs", Path.Combine("Scripts", "Enemy.cs"), "Old.CS", "New.cs", "MyGame.csproj" },
            burst.Select(x => Path.GetRelativePath(gameDirectory, x.FilePath)));
    }

    [Fact]
    public async Task BuildOutputsHiddenFoldersOtherFilesAndOtherProjectsDoNotCount()
    {
        using var watcher = CreateWatcher();
        var bursts = Record(watcher);
        var gameDirectory = Path.GetDirectoryName(game)!;

        Change(watcher, FileEventChangeType.Changed, Path.Combine(gameDirectory, "obj", "Debug", "net10.0", "MyGame.AssemblyInfo.cs"));
        Change(watcher, FileEventChangeType.Changed, Path.Combine(gameDirectory, "Bin", "Generated.cs"));
        Change(watcher, FileEventChangeType.Changed, Path.Combine(gameDirectory, ".vs", "Backup.cs"));
        Change(watcher, FileEventChangeType.Changed, Path.Combine(gameDirectory, "Assets", "MainScene.sdscene"));
        Change(watcher, FileEventChangeType.Changed, Path.Combine(gameDirectory, "Tools", "Program.cs"));
        Change(watcher, FileEventChangeType.Changed, nestedHead);
        Change(watcher, FileEventChangeType.Changed, Path.Combine(Path.GetDirectoryName(head)!, "MyGameApp.cs"));
        Change(watcher, FileEventChangeType.Changed, head);
        // The project file saved as it was, then while it is being written
        Change(watcher, FileEventChangeType.Changed, game);
        File.WriteAllText(game, string.Empty);
        Change(watcher, FileEventChangeType.Changed, game);
        // Another folder whose name starts like the one of the project
        Change(watcher, FileEventChangeType.Changed, Path.Combine(directory, "MyGame2", "Other.cs"));

        await Task.Delay(watcher.BurstDelay * 4);
        Assert.False(bursts.Reader.TryRead(out _));
    }

    [Fact]
    public async Task ABurstReportsEachFileOnceAndTheNextOneStartsAgain()
    {
        using var watcher = CreateWatcher(OneBurstDelay);
        var bursts = Record(watcher);
        var player = Path.Combine(Path.GetDirectoryName(game)!, "Player.cs");

        // An IDE saves a file in several steps
        Change(watcher, FileEventChangeType.Changed, player);
        Change(watcher, FileEventChangeType.Changed, player);
        Change(watcher, FileEventChangeType.Changed, player);
        var first = await bursts.Reader.ReadAsync().AsTask().WaitAsync(Timeout);
        Assert.Equal(player, Assert.Single(first).FilePath);

        Change(watcher, FileEventChangeType.Changed, player);
        var second = await bursts.Reader.ReadAsync().AsTask().WaitAsync(Timeout);
        Assert.Equal(player, Assert.Single(second).FilePath);
    }

    [Fact]
    public void AChangeAfterDisposeIsDropped()
    {
        var watcher = CreateWatcher();
        var bursts = Record(watcher);
        Change(watcher, FileEventChangeType.Changed, Path.Combine(Path.GetDirectoryName(game)!, "Player.cs"));
        watcher.Dispose();
        Change(watcher, FileEventChangeType.Changed, Path.Combine(Path.GetDirectoryName(game)!, "Enemy.cs"));

        Thread.Sleep(watcher.BurstDelay * 4);
        Assert.False(bursts.Reader.TryRead(out _));
    }

    [Fact]
    public async Task TheFileSystemIsWatched()
    {
        using var watcher = new GameCodeWatcher([game], [head], TimeSpan.FromMilliseconds(200));
        var bursts = Record(watcher);
        var gameDirectory = Path.GetDirectoryName(game)!;

        // The build output does not count
        Directory.CreateDirectory(Path.Combine(gameDirectory, "obj"));
        File.WriteAllText(Path.Combine(gameDirectory, "obj", "MyGame.AssemblyInfo.cs"), "// generated");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(head)!, "MyGameApp.cs"), "// head");
        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.False(bursts.Reader.TryRead(out _));

        var player = Path.Combine(gameDirectory, "Player.cs");
        File.WriteAllText(player, "public class Player { }");
        var burst = await bursts.Reader.ReadAsync().AsTask().WaitAsync(Timeout);
        // The directory watcher reports lowercase paths on Windows
        var pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        Assert.Contains(burst, x => string.Equals(x.FilePath, player, pathComparison) && x.ProjectPath == game);
    }

    [Fact]
    public async Task TheChangesTheFileSystemWatcherMissedAreFoundFromTheFiles()
    {
        var gameDirectory = Path.GetDirectoryName(game)!;
        var player = Path.Combine(gameDirectory, "Player.cs");
        var same = Path.Combine(gameDirectory, "Same.cs");
        var old = Path.Combine(gameDirectory, "Old.cs");
        File.WriteAllText(player, "public class Player { }");
        File.WriteAllText(same, "public class Same { }");
        File.WriteAllText(old, "public class Old { }");
        // Nothing is tracked, as if the watcher missed every event
        using var watcher = new GameCodeWatcher([game], [head, nestedHead], OneBurstDelay, watchFileSystem: true, (_, _) => { });
        var bursts = Record(watcher);

        // Nothing changed yet
        watcher.OnChangesMissed(gameDirectory);

        File.WriteAllText(player, "public class Player { public int Health; }");
        File.Delete(old);
        var enemy = Path.Combine(gameDirectory, "Scripts", "Enemy.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(enemy)!);
        File.WriteAllText(enemy, "public class Enemy { }");
        // Neither the build output nor the files of another project count
        Directory.CreateDirectory(Path.Combine(gameDirectory, "obj"));
        File.WriteAllText(Path.Combine(gameDirectory, "obj", "MyGame.AssemblyInfo.cs"), "// generated");
        File.WriteAllText(Path.Combine(gameDirectory, "Tools", "Program.cs"), "// tools");
        // The watch of a parent folder started again
        watcher.OnChangesMissed(directory);

        var burst = await bursts.Reader.ReadAsync().AsTask().WaitAsync(Timeout);
        Assert.All(burst, x => Assert.Equal(game, x.ProjectPath));
        Assert.Equal(
            new[] { "Old.cs", "Player.cs", Path.Combine("Scripts", "Enemy.cs") }.Order(),
            burst.Select(x => Path.GetRelativePath(gameDirectory, x.FilePath)).Order());

        // Each change is reported once
        watcher.OnChangesMissed(gameDirectory);
        await Task.Delay(watcher.BurstDelay * 3);
        Assert.False(bursts.Reader.TryRead(out _));
    }

    [Fact]
    public async Task TheWatchedProjectsOfASessionAreTheProjectsWhoseAssemblyTheEditorLoads()
    {
        await using var fixture = await EditorSessionFixture.OpenGameAsync(PlatformType.Windows, PlatformType.Linux);
        // The fixture's projects hold no asset types, so the session loaded no assembly
        Assert.Empty(GameCodeWatcher.GetCodeProjects(fixture.Session));

        var library = fixture.Session.LocalPackages.OfType<ProjectViewModel>().Single(x => x.Type == ProjectType.Library);
        var relativeLocation = new UFile(EditorSessionFixture.PackageName + ".csproj");
        library.Package.LoadedAssemblies.Add(new PackageLoadedAssembly(new ProjectReference(Guid.NewGuid(), relativeLocation, ProjectType.Library), Path.Combine(fixture.Directory, "bin", "TestGame.dll")));

        // The location is relative to the package
        var expected = Path.Combine(fixture.Directory, EditorSessionFixture.PackageName, EditorSessionFixture.PackageName + ".csproj");
        Assert.Equal(expected, Assert.Single(GameCodeWatcher.GetCodeProjects(fixture.Session)));
        using var watcher = GameCodeWatcher.Create(fixture.Session);
        Assert.Equal(expected, Assert.Single(watcher.CodeProjects));
    }

    private GameCodeWatcher CreateWatcher(TimeSpan? burstDelay = null) => new([game], [head, nestedHead], burstDelay ?? TimeSpan.FromMilliseconds(100), watchFileSystem: false);

    private static void Change(GameCodeWatcher watcher, FileEventChangeType changeType, string path)
    {
        watcher.OnFileChanged(new FileEvent(changeType, Path.GetFileName(path), path));
    }

    private static System.Threading.Channels.Channel<IReadOnlyList<GameCodeChange>> Record(GameCodeWatcher watcher)
    {
        var bursts = System.Threading.Channels.Channel.CreateUnbounded<IReadOnlyList<GameCodeChange>>();
        watcher.Changed += (_, e) => bursts.Writer.TryWrite(e.Changes);
        return bursts;
    }

    private string WriteProject(string folder, string name)
    {
        var path = Path.Combine(directory, folder, name + ".csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, ProjectFile(name));
        return path;
    }

    private static string ProjectFile(string name) => $"<Project Sdk=\"Microsoft.NET.Sdk\"><!-- {name} --></Project>";
}
