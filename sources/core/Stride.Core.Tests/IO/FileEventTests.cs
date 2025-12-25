// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Xunit;

namespace Stride.Core.Tests.IO;

public class FileEventTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var changeType = FileEventChangeType.Created;
        var name = "test.txt";
        var fullPath = @"C:\temp\test.txt";

        var fileEvent = new FileEvent(changeType, name, fullPath);

        Assert.Equal(changeType, fileEvent.ChangeType);
        Assert.Equal(name, fileEvent.Name);
        Assert.Equal(fullPath, fileEvent.FullPath);
    }

    [Theory]
    [InlineData(FileEventChangeType.Created)]
    [InlineData(FileEventChangeType.Deleted)]
    [InlineData(FileEventChangeType.Changed)]
    [InlineData(FileEventChangeType.Renamed)]
    public void Constructor_SupportsAllChangeTypes(FileEventChangeType changeType)
    {
        var fileEvent = new FileEvent(changeType, "file.txt", @"C:\file.txt");

        Assert.Equal(changeType, fileEvent.ChangeType);
    }

    [Fact]
    public void FileRenameEvent_SetsPropertiesCorrectly()
    {
        var name = "newfile.txt";
        var fullPath = @"C:\temp\newfile.txt";
        var oldFullPath = @"C:\temp\oldfile.txt";

        var renameEvent = new FileRenameEvent(name, fullPath, oldFullPath);

        Assert.Equal(FileEventChangeType.Renamed, renameEvent.ChangeType);
        Assert.Equal(name, renameEvent.Name);
        Assert.Equal(fullPath, renameEvent.FullPath);
        Assert.Equal(oldFullPath, renameEvent.OldFullPath);
    }

    [Fact]
    public void FileRenameEvent_ToString_ReturnsFormattedString()
    {
        var renameEvent = new FileRenameEvent(
            "newfile.txt",
            @"C:\temp\newfile.txt",
            @"C:\temp\oldfile.txt"
        );

        var result = renameEvent.ToString();

        Assert.Contains("Renamed", result);
        Assert.Contains(@"C:\temp\newfile.txt", result);
        Assert.Contains(@"C:\temp\oldfile.txt", result);
    }

    [Fact]
    public void FileEvent_InheritsFromEventArgs()
    {
        var fileEvent = new FileEvent(FileEventChangeType.Created, "test.txt", @"C:\test.txt");

        Assert.IsAssignableFrom<EventArgs>(fileEvent);
    }

    [Fact]
    public void FileRenameEvent_InheritsFromFileEvent()
    {
        var renameEvent = new FileRenameEvent("new.txt", @"C:\new.txt", @"C:\old.txt");

        Assert.IsAssignableFrom<FileEvent>(renameEvent);
    }
}
