// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Editors;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The signature of a file on disk (<see cref="FileContentSignature"/>), which editors compare to notice external changes.
/// </summary>
public sealed class TestFileContentSignature : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("stride-signature-").FullName;

    public void Dispose() => Directory.Delete(directory, true);

    [Fact]
    public void ANullPathHasTheSignatureOfAMissingFile()
    {
        var signature = FileContentSignature.Capture(null);

        Assert.False(signature.Exists);
        Assert.Equal(default, signature);
    }

    [Fact]
    public void AMissingFileHasNoSignature()
    {
        Assert.Equal(default, FileContentSignature.Capture(Path.Combine(directory, "Missing.cs")));
    }

    [Fact]
    public void AnUnchangedFileKeepsItsSignature()
    {
        var path = Path.Combine(directory, "Script.cs");
        File.WriteAllText(path, "hello");

        var first = FileContentSignature.Capture(path);
        var second = FileContentSignature.Capture(path);

        Assert.True(first.Exists);
        Assert.Equal(5, first.Length);
        Assert.Equal(first, second);
    }

    [Fact]
    public void ChangingTheContentChangesTheSignature()
    {
        var path = Path.Combine(directory, "Script.cs");
        File.WriteAllText(path, "hello");
        var before = FileContentSignature.Capture(path);

        File.WriteAllText(path, "hello world");

        Assert.NotEqual(before, FileContentSignature.Capture(path));
    }

    [Fact]
    public void TouchingTheFileChangesTheSignature()
    {
        var path = Path.Combine(directory, "Script.cs");
        File.WriteAllText(path, "hello");
        var before = FileContentSignature.Capture(path);

        // Same length, another write time: another program saved the file
        File.SetLastWriteTimeUtc(path, before.LastWriteTimeUtc.AddSeconds(-10));

        Assert.NotEqual(before, FileContentSignature.Capture(path));
    }

    [Fact]
    public void DeletingTheFileChangesTheSignature()
    {
        var path = Path.Combine(directory, "Script.cs");
        File.WriteAllText(path, "hello");
        var before = FileContentSignature.Capture(path);

        File.Delete(path);

        Assert.NotEqual(before, FileContentSignature.Capture(path));
        Assert.False(FileContentSignature.Capture(path).Exists);
    }
}
