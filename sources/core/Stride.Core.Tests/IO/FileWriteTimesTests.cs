// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Xunit;

namespace Stride.Core.Tests.IO;

public class FileWriteTimesTests : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("FileWriteTimesTest_").FullName;

    public void Dispose()
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort
        }
    }

    [Fact]
    public void ReconcileFindsTheFilesCreatedChangedAndDeletedSinceTheyWereRecorded()
    {
        var times = new FileWriteTimes();
        var changed = Write("Changed.cs", "a");
        var touched = Write("Touched.cs", "a");
        var same = Write("Same.cs", "a");
        var deleted = Write("Deleted.cs", "a");
        var back = Path.Combine(directory, "Back.cs");
        foreach (var file in new[] { changed, touched, same, deleted, back })
            times.Add(file);

        File.WriteAllText(changed, "longer");
        // Same size, later write
        File.SetLastWriteTimeUtc(touched, File.GetLastWriteTimeUtc(touched).AddSeconds(5));
        File.Delete(deleted);
        Write("Back.cs", "a");
        var created = Write(Path.Combine("Sub", "Created.cs"), "a");

        var changes = times.Reconcile(directory, [created, same]);

        Assert.Equal(
            new[] { (FileEventChangeType.Changed, changed), (FileEventChangeType.Changed, touched), (FileEventChangeType.Deleted, deleted), (FileEventChangeType.Created, back), (FileEventChangeType.Created, created) }.OrderBy(x => x.Item2),
            changes.Select(x => (x.ChangeType, x.FullPath)).OrderBy(x => x.FullPath));
        // Each change is found once
        Assert.Empty(times.Reconcile(directory, [created, same]));
    }

    [Fact]
    public void AnUpdateAfterAnEventLeavesNothingToReconcile()
    {
        var times = new FileWriteTimes();
        var file = Write("Player.cs", "a");
        Assert.True(times.Add(file));

        File.WriteAllText(file, "reported by the watcher");
        times.Update(file);
        Assert.False(times.Add(file));

        Assert.Empty(times.Reconcile(directory));
    }

    [Fact]
    public void ReconcileLooksOnlyInTheDirectory()
    {
        var times = new FileWriteTimes();
        var inside = Write(Path.Combine("Inside", "Player.cs"), "a");
        var sibling = Write(Path.Combine("Inside2", "Player.cs"), "a");
        times.Add(inside);
        times.Add(sibling);

        File.WriteAllText(inside, "changed");
        File.WriteAllText(sibling, "changed");

        var change = Assert.Single(times.Reconcile(Path.Combine(directory, "Inside")));
        Assert.Equal(inside, change.FullPath);
        Assert.True(times.Contains(sibling));
    }

    [Theory]
    [InlineData("Game", "Game", true)]
    [InlineData("Game/Player.cs", "Game", true)]
    [InlineData("Game/Player.cs", "Game/", true)]
    [InlineData("Game2/Player.cs", "Game", false)]
    [InlineData("Other/Player.cs", "Game", false)]
    public void IsUnderComparesWholeFolders(string path, string folder, bool expected)
    {
        Assert.Equal(expected, FileWriteTimes.IsUnder(Path.Combine(directory, path), Path.Combine(directory, folder)));
    }

    private string Write(string name, string content)
    {
        var path = Path.Combine(directory, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }
}
