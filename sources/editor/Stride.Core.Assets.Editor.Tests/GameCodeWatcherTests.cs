// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The watcher of the game code: which changes count, how they are grouped, and which projects of a session it watches.
/// </summary>
public sealed class GameCodeWatcherTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

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
    public async Task TheSourcesAndTheProjectFileOfAWatchedProjectCount()
    {
        using var watcher = CreateWatcher();
        var bursts = Record(watcher);
        var gameDirectory = Path.GetDirectoryName(game)!;

        Change(watcher, FileEventChangeType.Changed, Path.Combine(gameDirectory, "Player.cs"));
        Change(watcher, FileEventChangeType.Created, Path.Combine(gameDirectory, "Scripts", "Enemy.cs"));
        Change(watcher, FileEventChangeType.Deleted, Path.Combine(gameDirectory, "Old.CS"));
        watcher.OnFileChanged(new FileRenameEvent("New.cs", Path.Combine(gameDirectory, "New.cs"), Path.Combine(gameDirectory, "Before.txt")));
        File.WriteAllText(game, ProjectFile("MyGame") + "<!-- edited -->");
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
        using var watcher = CreateWatcher();
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
        Assert.Contains(burst, x => x.FilePath == player && x.ProjectPath == game);
    }

    [Fact]
    public async Task TheWatchedProjectsOfASessionAreTheProjectsWhoseAssemblyTheEditorLoads()
    {
        using var fixture = await EditorSessionFixture.OpenGameAsync(PlatformType.Windows, PlatformType.Linux);
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

    private GameCodeWatcher CreateWatcher() => new([game], [head, nestedHead], TimeSpan.FromMilliseconds(100), watchFileSystem: false);

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
