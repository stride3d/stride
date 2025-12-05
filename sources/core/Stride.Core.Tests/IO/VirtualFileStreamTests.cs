// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization;
using Xunit;

namespace Stride.Core.Tests.IO;

public class VirtualFileStreamTests
{
    [Fact]
    public void Constructor_WithMemoryStream_CreatesStream()
    {
        using var memStream = new MemoryStream([1, 2, 3, 4, 5]);
        using var vfs = new VirtualFileStream(memStream);

        Assert.NotNull(vfs);
        Assert.Equal(5, vfs.Length);
        Assert.Equal(0, vfs.Position);
    }

    [Fact]
    public void Constructor_WithStartPosition_SetsPosition()
    {
        using var memStream = new MemoryStream([1, 2, 3, 4, 5]);
        using var vfs = new VirtualFileStream(memStream, startPosition: 2);

        Assert.Equal(3, vfs.Length); // 5 - 2
        Assert.Equal(0, vfs.Position); // Position relative to start
    }

    [Fact]
    public void Constructor_WithStartAndEndPosition_LimitsLength()
    {
        using var memStream = new MemoryStream([1, 2, 3, 4, 5]);
        using var vfs = new VirtualFileStream(memStream, startPosition: 1, endPosition: 4);

        Assert.Equal(3, vfs.Length); // 4 - 1
    }

    [Fact]
    public void Read_ReadsFromStream()
    {
        using var memStream = new MemoryStream([1, 2, 3, 4, 5]);
        using var vfs = new VirtualFileStream(memStream);
        var buffer = new byte[3];

        var bytesRead = vfs.Read(buffer, 0, 3);

        Assert.Equal(3, bytesRead);
        Assert.Equal(new byte[] { 1, 2, 3 }, buffer);
    }

    [Fact]
    public void Read_WithEndPosition_LimitsRead()
    {
        using var memStream = new MemoryStream([1, 2, 3, 4, 5]);
        using var vfs = new VirtualFileStream(memStream, startPosition: 0, endPosition: 3);
        var buffer = new byte[5];

        var bytesRead = vfs.Read(buffer, 0, 5);

        Assert.Equal(3, bytesRead);
        Assert.Equal(new byte[] { 1, 2, 3, 0, 0 }, buffer);
    }

    [Fact]
    public void Write_WritesToStream()
    {
        using var memStream = new MemoryStream();
        using var vfs = new VirtualFileStream(memStream);
        var data = new byte[] { 1, 2, 3 };

        vfs.Write(data, 0, 3);
        vfs.Flush();

        Assert.Equal(3, vfs.Position);
        Assert.Equal(3, vfs.Length);
    }

    [Fact]
    public void Write_WithEndPosition_ThrowsWhenExceeded()
    {
        using var memStream = new MemoryStream();
        memStream.Write([1, 2, 3, 4, 5]);
        memStream.Position = 0;
        using var vfs = new VirtualFileStream(memStream, startPosition: 0, endPosition: 3, seekToBeginning: true);
        var data = new byte[] { 1, 2, 3, 4, 5 };

        Assert.Throws<IOException>(() => vfs.Write(data, 0, 5));
    }

    [Fact]
    public void Seek_Begin_SeeksFromStart()
    {
        using var memStream = new MemoryStream([1, 2, 3, 4, 5]);
        using var vfs = new VirtualFileStream(memStream);

        vfs.Seek(2, SeekOrigin.Begin);

        Assert.Equal(2, vfs.Position);
    }

    [Fact]
    public void Seek_Current_SeeksFromCurrent()
    {
        using var memStream = new MemoryStream([1, 2, 3, 4, 5]);
        using var vfs = new VirtualFileStream(memStream);
        vfs.Position = 2;

        vfs.Seek(1, SeekOrigin.Current);

        Assert.Equal(3, vfs.Position);
    }

    [Fact]
    public void Seek_End_SeeksFromEnd()
    {
        using var memStream = new MemoryStream([1, 2, 3, 4, 5]);
        using var vfs = new VirtualFileStream(memStream);

        vfs.Seek(-2, SeekOrigin.End);

        Assert.Equal(3, vfs.Position);
    }

    [Fact]
    public void Position_GetSet_WorksCorrectly()
    {
        using var memStream = new MemoryStream([1, 2, 3, 4, 5]);
        using var vfs = new VirtualFileStream(memStream);

        vfs.Position = 3;

        Assert.Equal(3, vfs.Position);
    }

    [Fact]
    public void CanRead_ReturnsTrue_ForReadableStream()
    {
        using var memStream = new MemoryStream([1, 2, 3]);
        using var vfs = new VirtualFileStream(memStream);

        Assert.True(vfs.CanRead);
    }

    [Fact]
    public void CanWrite_ReturnsTrue_ForWritableStream()
    {
        using var memStream = new MemoryStream();
        using var vfs = new VirtualFileStream(memStream);

        Assert.True(vfs.CanWrite);
    }

    [Fact]
    public void CanSeek_ReturnsTrue()
    {
        using var memStream = new MemoryStream([1, 2, 3]);
        using var vfs = new VirtualFileStream(memStream);

        Assert.True(vfs.CanSeek);
    }

    [Fact]
    public void SetLength_SetsStreamLength()
    {
        using var memStream = new MemoryStream();
        using var vfs = new VirtualFileStream(memStream);

        vfs.SetLength(10);

        Assert.Equal(10, vfs.Length);
    }

    [Fact]
    public void SetLength_WithEndPosition_ThrowsNotSupported()
    {
        using var memStream = new MemoryStream([1, 2, 3, 4, 5]);
        using var vfs = new VirtualFileStream(memStream, endPosition: 3);

        Assert.Throws<NotSupportedException>(() => vfs.SetLength(10));
    }

    [Fact]
    public void StartPosition_ReturnsCorrectValue()
    {
        using var memStream = new MemoryStream([1, 2, 3, 4, 5]);
        using var vfs = new VirtualFileStream(memStream, startPosition: 2);

        Assert.Equal(2, vfs.StartPosition);
    }

    [Fact]
    public void EndPosition_ReturnsCorrectValue()
    {
        using var memStream = new MemoryStream([1, 2, 3, 4, 5]);
        using var vfs = new VirtualFileStream(memStream, startPosition: 1, endPosition: 4);

        Assert.Equal(4, vfs.EndPosition);
    }

    [Fact]
    public void Dispose_DisposesInternalStream_WhenOwned()
    {
        var memStream = new MemoryStream([1, 2, 3]);
        var vfs = new VirtualFileStream(memStream, disposeInternalStream: true);

        vfs.Dispose();

        Assert.Throws<ObjectDisposedException>(() => memStream.Position);
    }

    [Fact]
    public void Dispose_DoesNotDisposeInternalStream_WhenNotOwned()
    {
        var memStream = new MemoryStream([1, 2, 3]);
        var vfs = new VirtualFileStream(memStream, disposeInternalStream: false);

        vfs.Dispose();

        // Should not throw
        var pos = memStream.Position;
        Assert.Equal(0, pos);
        memStream.Dispose();
    }
}
