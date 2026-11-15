// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Games;
using Xunit;

namespace Stride.Graphics.Tests;

/// <summary>
///   The driver's pipeline cache kept between runs: <see cref="GraphicsDevice.GetPipelineCacheData"/> and
///   <see cref="GraphicsDevice.MergePipelineCache"/> on the device, and the file <see cref="GraphicsDeviceManager"/>
///   keeps for them. On Direct3D no pipeline cache is exposed and the data is empty.
/// </summary>
public class TestPipelineCache : GraphicTestGameBase
{
    private const int ReadBeforeDrawFrame = 1;
    private const int DrawFrame = 2;
    private const int ReadAfterDrawFrame = 4;

    /// <summary>
    ///   The bundled Lavapipe (Mesa 26.2) stores no pipeline in its cache and returns a blob shorter than the header it
    ///   declares, which the engine drops, so what a real driver stores can only be seen on a GPU (<c>STRIDE_TESTS_GPU=1</c>).
    /// </summary>
    private static bool IsSoftwareRendering => Environment.GetEnvironmentVariable("STRIDE_GRAPHICS_SOFTWARE_RENDERING") == "1";

    private byte[] dataBeforeDraw;
    private byte[] dataAfterDraw;
    private bool ownDataMerged;
    private bool fileWrittenOnRequest;
    private GraphicsAdapter adapter;

    protected override void RegisterTests()
    {
        base.RegisterTests();

        FrameGameSystem.Update(ReadBeforeDrawFrame, ReadBeforeDraw);
        FrameGameSystem.Draw(DrawFrame, DrawTexture);
        FrameGameSystem.Update(ReadAfterDrawFrame, ReadAfterDraw);
    }

    private void ReadBeforeDraw()
    {
        adapter = GraphicsDevice.Adapter;
        dataBeforeDraw = GraphicsDevice.GetPipelineCacheData();
    }

    /// <summary>Builds the pipeline of the texture draw.</summary>
    private void DrawTexture()
    {
        GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Black);
        GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);
        GraphicsContext.DrawTexture(UVTexture);
    }

    private void ReadAfterDraw()
    {
        dataAfterDraw = GraphicsDevice.GetPipelineCacheData();
        ownDataMerged = GraphicsDevice.MergePipelineCache(dataAfterDraw);

        // The game's early save, before the device is disposed; nothing while the file is not kept
        GraphicsDeviceManager.SavePipelineCache();
        fileWrittenOnRequest = VirtualFileSystem.ApplicationCache.FileExists(GraphicsDeviceManager.GetPipelineCacheFile(adapter));
    }

    [SkippableFact]
    public void TheDataOfADrawnPipelineMergesBack()
    {
        Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Vulkan, "No pipeline cache is exposed");
        Skip.If(IsSoftwareRendering, "Lavapipe stores no pipeline in its cache");

        var game = new TestPipelineCache();
        RunGameTest(game);

        Assert.True(game.ownDataMerged);
        Assert.True(game.dataAfterDraw.Length > game.dataBeforeDraw.Length, "The drawn pipeline is in the data");
    }

    [SkippableFact]
    public void BadDataIsDropped()
    {
        using var device = GraphicsDevice.New();

        Assert.False(device.MergePipelineCache(null));
        Assert.False(device.MergePipelineCache([]));
        var garbage = new byte[1000];
        new Random(1).NextBytes(garbage);
        Assert.False(device.MergePipelineCache(garbage));

        var data = device.GetPipelineCacheData();
        Skip.If(data.Length == 0, "No pipeline cache is exposed");

        Assert.False(device.MergePipelineCache(data[..^1]), "Truncated data is dropped");
        Assert.False(device.MergePipelineCache([.. data, 0]), "Padded data is dropped");
        var overwritten = (byte[])data.Clone();
        overwritten[^1] ^= 1;
        Assert.False(device.MergePipelineCache(overwritten), "Overwritten data is dropped");

        // A whole envelope around the driver's blob of another device or driver (VkPipelineCacheHeaderVersionOne: deviceID at 12,
        // pipelineCacheUUID at 16). On Lavapipe the blob is dropped before these fields are read, so this part tells on a GPU only
        var driverVersion = device.Adapter.DriverInfo.DriverVersion;
        Assert.True(PipelineCacheData.TryUnwrap(data, driverVersion, out var blob));
        var otherDevice = blob.ToArray();
        otherDevice[12] ^= 0xFF;
        Assert.False(device.MergePipelineCache(PipelineCacheData.Wrap(otherDevice, driverVersion)), "Another device's data is dropped");
        var otherDriver = blob.ToArray();
        otherDriver[16] ^= 0xFF;
        Assert.False(device.MergePipelineCache(PipelineCacheData.Wrap(otherDriver, driverVersion)), "Another driver's data is dropped");

        // The same driver in another version, which its header does not tell when the driver kept its pipelineCacheUUID
        var otherVersion = PipelineCacheData.Wrap(blob, driverVersion + " (another)");
        Assert.False(PipelineCacheData.TryUnwrap(otherVersion, driverVersion, out _), "Another driver version's envelope does not open");
        Assert.False(device.MergePipelineCache(otherVersion), "Another driver version's data is dropped");
    }

    [SkippableFact]
    public void TheManagerKeepsTheFileBetweenRuns()
    {
        Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Vulkan, "No pipeline cache is exposed");

        var file = GraphicsDeviceManager.GetPipelineCacheFile(GraphicsAdapterFactory.DefaultAdapter);
        Assert.NotNull(file);
        var cache = VirtualFileSystem.ApplicationCache;
        if (cache.FileExists(file))
            cache.FileDelete(file);

        var notKeeping = new TestPipelineCache();
        RunGameTest(notKeeping);
        Assert.Equal(file, GraphicsDeviceManager.GetPipelineCacheFile(notKeeping.adapter));
        Assert.False(notKeeping.fileWrittenOnRequest, "Nothing is written on request while PersistPipelineCache is off");
        Assert.False(cache.FileExists(file), "Nothing is written while PersistPipelineCache is off");

        var keeping = new TestPipelineCache();
        keeping.GraphicsDeviceManager.PersistPipelineCache = true;
        RunGameTest(keeping);
        Assert.True(keeping.fileWrittenOnRequest, "The device's cache is written on request");
        Assert.True(cache.FileExists(file), "The device's cache is written when the device is disposed");

        byte[] written;
        using (var stream = cache.OpenStream(file, VirtualFileMode.Open, VirtualFileAccess.Read))
        {
            written = new byte[stream.Length];
            stream.ReadExactly(written);
        }
        Assert.True(PipelineCacheData.TryUnwrap(written, keeping.adapter.DriverInfo.DriverVersion, out _), "The file is whole");

        using var device = GraphicsDevice.New();
        if (!IsSoftwareRendering)
            Assert.True(device.MergePipelineCache(written), "The file merges into a new device");
    }

    [SkippableFact]
    public void FilesNothingUsesAreRemovedWhenTheFileIsWritten()
    {
        Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Vulkan, "No pipeline cache is exposed");

        var cache = VirtualFileSystem.ApplicationCache;
        var oldDriver = $"pipelines-{Guid.NewGuid():N}.bin";
        var otherGpuInUse = $"pipelines-{Guid.NewGuid():N}.bin";
        var leftByACrash = $"{oldDriver}.{Guid.NewGuid():N}.tmp";
        var beingWritten = $"{otherGpuInUse}.{Guid.NewGuid():N}.tmp";
        try
        {
            WriteFile(oldDriver, TimeSpan.FromDays(60));
            WriteFile(otherGpuInUse, TimeSpan.FromDays(2));
            WriteFile(leftByACrash, TimeSpan.FromDays(2));
            WriteFile(beingWritten, TimeSpan.Zero);

            var keeping = new TestPipelineCache();
            keeping.GraphicsDeviceManager.PersistPipelineCache = true;
            RunGameTest(keeping);

            Assert.True(cache.FileExists(GraphicsDeviceManager.GetPipelineCacheFile(keeping.adapter)), "The device's own file stays");
            Assert.False(cache.FileExists(oldDriver), "The file of a driver nothing wrote for long is removed");
            Assert.True(cache.FileExists(otherGpuInUse), "The file of another device written lately stays");
            Assert.False(cache.FileExists(leftByACrash), "A temporary file left behind is removed");
            Assert.True(cache.FileExists(beingWritten), "A temporary file being written stays");
        }
        finally
        {
            foreach (var file in (string[])[oldDriver, otherGpuInUse, leftByACrash, beingWritten])
            {
                if (cache.FileExists(file))
                    cache.FileDelete(file);
            }
        }

        void WriteFile(string name, TimeSpan age)
        {
            using (var stream = cache.OpenStream(name, VirtualFileMode.Create, VirtualFileAccess.Write))
                stream.WriteByte(0);
            File.SetLastWriteTime(cache.GetAbsolutePath(name), DateTime.Now - age);
        }
    }

    [SkippableFact]
    public void AFileOverTheLimitIsNotMerged()
    {
        Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Vulkan, "No pipeline cache is exposed");
        Skip.If(IsSoftwareRendering, "Lavapipe stores no pipeline in its cache");

        var file = GraphicsDeviceManager.GetPipelineCacheFile(GraphicsAdapterFactory.DefaultAdapter);
        var cache = VirtualFileSystem.ApplicationCache;
        if (cache.FileExists(file))
            cache.FileDelete(file);

        // The first run leaves its drawn pipeline in the file, and the second starts with it
        var first = new TestPipelineCache();
        first.GraphicsDeviceManager.PersistPipelineCache = true;
        RunGameTest(first);

        var merging = new TestPipelineCache();
        merging.GraphicsDeviceManager.PersistPipelineCache = true;
        RunGameTest(merging);
        Assert.True(merging.dataBeforeDraw.Length > first.dataBeforeDraw.Length, "The file of the earlier run is merged");

        var startingOver = new TestPipelineCache();
        startingOver.GraphicsDeviceManager.PersistPipelineCache = true;
        startingOver.GraphicsDeviceManager.MaximumPipelineCacheSize = 1;
        RunGameTest(startingOver);
        Assert.True(startingOver.dataBeforeDraw.Length < merging.dataBeforeDraw.Length, "A file over the limit is not merged");
    }
}
