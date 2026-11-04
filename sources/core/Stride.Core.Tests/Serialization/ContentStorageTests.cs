// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Core.Streaming;

namespace Stride.Core.Tests.Serialization;

[Collection("Non-Parallel Collection")]
public class ContentStorageTests
{
    private static ContentManager CreateContentManager()
    {
        VirtualFileSystem.CreateDirectory(VirtualFileSystem.ApplicationDatabasePath);
        return new ContentManager(new DatabaseFileProviderService(new DatabaseFileProvider(ContentIndexMap.NewTool(VirtualFileSystem.ApplicationDatabaseIndexName), new ObjectDatabase(VirtualFileSystem.ApplicationDatabasePath, VirtualFileSystem.ApplicationDatabaseIndexName))));
    }

    private static byte[] Write(in ContentStorageHeader header)
    {
        using var memory = new MemoryStream();
        header.Write(new BinarySerializationWriter(memory));
        return memory.ToArray();
    }

    [Fact]
    public void SameChunksGiveSameHeader()
    {
        var contentManager = CreateContentManager();
        List<byte[]> chunks = [new byte[16], new byte[4]];

        ContentStorage.Create(contentManager, "storage_test", chunks, out var first);
        Thread.Sleep(20);
        ContentStorage.Create(contentManager, "storage_test", chunks, out var second);

        Assert.Equal(Write(first), Write(second));
    }

    [Fact]
    public void ContentWithACreationTimeStillLoads()
    {
        ContentStorage.Create(CreateContentManager(), "storage_test_old", [new byte[16], new byte[4]], out var header);

        // What older versions wrote: the time of the build, and a hash code made from it
        var time = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var hashCode = (int)time.Ticks;
        hashCode = (hashCode * 397) ^ 2;
        hashCode = (hashCode * 397) ^ 16;
        hashCode = (hashCode * 397) ^ 4;
        header.PackageTime = time;
        header.HashCode = hashCode;

        using var service = new ContentStreamingService();
        var storage = service.GetStorage(ref header);

        Assert.Equal(time.Ticks, storage.PackageTime.Ticks);
    }
}
