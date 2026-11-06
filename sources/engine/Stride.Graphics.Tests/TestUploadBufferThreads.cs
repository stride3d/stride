// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace Stride.Graphics.Tests;

/// <summary>
///   Textures and buffers created with initial data on several threads at once share the device's upload buffer.
///   A thread that needs more space than the buffer has left replaces it while other threads are still writing to it.
/// </summary>
public class TestUploadBufferThreads : GraphicTestGameBase
{
    private const int ThreadCount = 8;
    private const int ResourcesPerThread = 8;

    /// <summary>
    ///   Every texture and buffer holds the data it was created with. Sizes of 1 MB and 4 MB (the upload buffer's minimum)
    ///   make the threads replace the upload buffer while others still copy into it.
    /// </summary>
    [Fact]
    public void ResourcesCreatedOnSeveralThreadsHoldTheirData()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            var textures = new List<(Texture Texture, byte Value)>();
            var buffers = new List<(Buffer Buffer, byte Value)>();
            var start = new Barrier(ThreadCount);
            var threads = new Thread[ThreadCount];
            Exception failure = null;

            for (int t = 0; t < ThreadCount; t++)
            {
                int thread = t;
                threads[t] = new Thread(() =>
                {
                    try
                    {
                        start.SignalAndWait();
                        for (int i = 0; i < ResourcesPerThread; i++)
                        {
                            var value = (byte) (1 + thread * ResourcesPerThread + i);
                            var size = (i % 2 == 0) ? 1024 : 512;

                            var texture = NewTexture(device, size, value);
                            lock (textures)
                                textures.Add((texture, value));

                            var data = new byte[size * size * 2];
                            Array.Fill(data, value);
                            var buffer = Buffer.New(device, data, elementSize: 0, BufferFlags.VertexBuffer, usage: GraphicsResourceUsage.Default);
                            lock (buffers)
                                buffers.Add((buffer, value));
                        }
                    }
                    catch (Exception exception)
                    {
                        Interlocked.CompareExchange(ref failure, exception, null);
                    }
                });
                threads[t].Start();
            }
            foreach (var thread in threads)
                thread.Join();

            Assert.Null(failure);
            Assert.Equal(ThreadCount * ResourcesPerThread, textures.Count);

            foreach (var (texture, value) in textures)
            {
                Assert.Equal(-1, texture.GetData<byte>(commandList).AsSpan().IndexOfAnyExcept(value));
                texture.Dispose();
            }
            foreach (var (buffer, value) in buffers)
            {
                Assert.Equal(-1, buffer.GetData<byte>(commandList).AsSpan().IndexOfAnyExcept(value));
                buffer.Dispose();
            }
        });
    }

    private static unsafe Texture NewTexture(GraphicsDevice device, int size, byte value)
    {
        var data = new byte[size * size * 4];
        Array.Fill(data, value);
        fixed (byte* pointer = data)
        {
            var dataBox = new DataBox((nint) pointer, rowPitch: size * 4, slicePitch: data.Length);
            return Texture.New2D(device, size, size, 1, PixelFormat.R8G8B8A8_UNorm, [dataBox]);
        }
    }
}
