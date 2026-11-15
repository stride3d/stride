// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using Xunit;
using Stride.Core.IO;
using Stride.Core.Storage;

namespace Stride.Core.Tests.Serialization;

// Mounts its own file system: the provider registry of VirtualFileSystem isn't thread-safe
[Collection("Non-Parallel Collection")]
public sealed class FileOdbBackendTests : IDisposable
{
    private const int WriterCount = 16;
    private const int ReaderCount = 2;
    private const int Rounds = 32;
    private const int SharedCount = 8;
    private const int ForcedRounds = 500;

    private readonly string directory = Directory.CreateTempSubdirectory("stride-odb-").FullName;
    private readonly IVirtualFileProvider provider;
    private readonly FileOdbBackend backend;

    public FileOdbBackendTests()
    {
        var mountPoint = $"/odb_test_{Guid.NewGuid():N}";
        provider = VirtualFileSystem.MountFileSystem(mountPoint, directory);
        backend = new FileOdbBackend(mountPoint + "/db", string.Empty, isReadOnly: false);
    }

    public void Dispose()
    {
        backend.Dispose();
        VirtualFileSystem.UnregisterProvider(provider);
        Directory.Delete(directory, recursive: true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConcurrentWritesStoreEveryObject(bool forceWrite)
    {
        // Each round, every writer writes the shared objects and one of its own. Forced, each writer gives its own bytes
        // under a shared id (as the effect cache does under an input hash): the stored file must be one whole write.
        var sharedIds = Enumerable.Range(0, SharedCount)
            .Select(key => forceWrite ? ObjectId.FromBytes(BitConverter.GetBytes(key)) : ObjectId.FromBytes(Payload(key, -1, 0)))
            .ToArray();

        var contentAddressed = new ConcurrentDictionary<ObjectId, byte[]>();
        var errors = new ConcurrentQueue<Exception>();
        var start = new Barrier(WriterCount);

        var writers = Enumerable.Range(0, WriterCount).Select(writer => new Thread(() =>
        {
            start.SignalAndWait();
            try
            {
                for (var round = 0; round < Rounds; round++)
                {
                    // CreateStream has no forceWrite: use it for half the content-addressed writes
                    var useCreateStream = !forceWrite && round % 2 == 1;
                    for (var key = 0; key < SharedCount; key++)
                    {
                        var data = Payload(key, forceWrite ? writer : -1, forceWrite ? round : 0);
                        Write(sharedIds[key], data, forceWrite, useCreateStream);
                    }

                    var own = Payload(SharedCount, writer, round);
                    var ownId = ObjectId.FromBytes(own);
                    contentAddressed[ownId] = own;
                    Write(ownId, own, forceWrite, useCreateStream);
                }
            }
            catch (Exception e)
            {
                errors.Enqueue(e);
            }
        })).ToList();

        foreach (var thread in writers)
            thread.Start();
        foreach (var thread in writers)
            thread.Join();

        Assert.True(errors.IsEmpty, string.Join(Environment.NewLine, errors));

        foreach (var (id, data) in contentAddressed)
            Assert.Equal(data, Read(id));

        for (var key = 0; key < SharedCount; key++)
        {
            var stored = Read(sharedIds[key]);
            if (forceWrite)
            {
                Assert.Equal(key, BitConverter.ToInt32(stored, 0));
                Assert.Equal(Payload(key, BitConverter.ToInt32(stored, 4), BitConverter.ToInt32(stored, 8)), stored);
            }
            else
            {
                Assert.Equal(Payload(key, -1, 0), stored);
            }
        }

        Assert.Empty(Directory.GetFiles(Path.Combine(directory, "db", "tmp")));
    }

    [Fact]
    public void ForcedWriteNeverHidesTheObject()
    {
        // A forced write replaces the stored file: a reader must find the old file or the new one, never none
        var id = ObjectId.FromBytes(BitConverter.GetBytes(-1));
        Write(id, Payload(0, 0, 0), forceWrite: false, useCreateStream: false);

        var start = new Barrier(1 + ReaderCount);
        var writing = true;
        var missing = 0;
        var readers = Enumerable.Range(0, ReaderCount).Select(_ => new Thread(() =>
        {
            start.SignalAndWait();
            while (Volatile.Read(ref writing))
            {
                if (!backend.Exists(id))
                    Interlocked.Increment(ref missing);
            }
        })).ToList();
        foreach (var thread in readers)
            thread.Start();

        start.SignalAndWait();
        try
        {
            for (var round = 1; round <= ForcedRounds; round++)
                Write(id, Payload(0, 0, round), forceWrite: true, useCreateStream: false);
        }
        finally
        {
            Volatile.Write(ref writing, false);
            foreach (var thread in readers)
                thread.Join();
        }

        Assert.Equal(0, missing);
        var stored = Read(id);
        Assert.Equal(Payload(0, 0, BitConverter.ToInt32(stored, 8)), stored);
        Assert.Empty(Directory.GetFiles(Path.Combine(directory, "db", "tmp")));
    }

    private void Write(ObjectId id, byte[] data, bool forceWrite, bool useCreateStream)
    {
        if (useCreateStream)
        {
            using var stream = backend.CreateStream();
            stream.Write(data);
            Assert.Equal(id, stream.CurrentHash);
        }
        else
        {
            using var stream = new MemoryStream(data);
            backend.Write(id, stream, data.Length, forceWrite);
        }
    }

    private byte[] Read(ObjectId id)
    {
        using var stream = backend.OpenStream(id);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    /// <summary>
    /// Bytes that name their key, writer and round in a 12-byte header, followed by 4-12 KB derived from them.
    /// </summary>
    private static byte[] Payload(int key, int writer, int round)
    {
        var random = new Random((key * 1009 + writer) * 1009 + round);
        var data = new byte[12 + 4096 + random.Next(8192)];
        BitConverter.TryWriteBytes(data.AsSpan(0), key);
        BitConverter.TryWriteBytes(data.AsSpan(4), writer);
        BitConverter.TryWriteBytes(data.AsSpan(8), round);
        random.NextBytes(data.AsSpan(12));
        return data;
    }
}
