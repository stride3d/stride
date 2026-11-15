// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Xunit;

using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Rendering;

namespace Stride.Graphics.Tests
{
    public class TestGpuProfiling : GraphicTestGameBase
    {
        private static readonly ProfilingKey ScopeKey = new("TestGpuProfiling.Scope");
        // The meter listener compares names: it can see the histogram of the key while the key is being created
        private const string ThrowingKeyName = "TestGpuProfiling.Throwing";
        private static readonly ProfilingKey ThrowingKey = new(ThrowingKeyName);

        // More scopes than one timestamp pool of the query manager (64 queries) holds, so a full pool is read back
        private const int ScopesPerRound = 40;
        private const int RoundCount = 4;

        private GraphicsResourceAllocator allocator;
        private QueryManager queryManager;
        private Texture target;
        private Buffer buffer;

        protected override void RegisterTests()
        {
            base.RegisterTests();

            // One round per frame, as a game profiles; the first one runs before Direct3D 11 reads its first frame's disjoint query
            for (int round = 0; round < RoundCount; round++)
                FrameGameSystem.Draw(DrawRound);
            FrameGameSystem.Draw(ReadBack);
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            allocator = new GraphicsResourceAllocator(GraphicsDevice);
            queryManager = new QueryManager(GraphicsContext.CommandList, allocator);
            target = Texture.New2D(GraphicsDevice, 64, 64, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
            buffer = Buffer.Structured.New<uint>(GraphicsDevice, 1, unorderedAccess: true);
        }

        protected override void Destroy()
        {
            queryManager?.Dispose();
            allocator?.Dispose();
            target?.Dispose();
            buffer?.Dispose();

            base.Destroy();
        }

        private void DrawRound()
        {
            var commandList = GraphicsContext.CommandList;

            for (int scope = 0; scope < ScopesPerRound; scope++)
            {
                using (queryManager.BeginProfile(Color.Green, ScopeKey))
                    commandList.Clear(target, Color.Red);
            }

            ReadBack();
        }

        private void ReadBack()
        {
            // Reading the buffer back submits the command list and waits for it
            buffer.GetData<uint>(GraphicsContext.CommandList);
            queryManager.Flush();
        }

        /// <summary>
        /// GPU profiling scopes give GPU events, round after round, as the query manager reads its timestamp pools back
        /// and reuses them.
        /// </summary>
        [SkippableFact]
        public void ProfilingScopesGiveGpuEvents()
        {
            // A scope around a small clear can last less than the default minimum (1 µs) on a fast GPU
            var minimumProfileDuration = Profiler.MinimumProfileDuration;
            Profiler.MinimumProfileDuration = TimeSpan.Zero;

            var events = Profiler.Subscribe();
            Profiler.Enable(ScopeKey);
            try
            {
                RunGameTest(new TestGpuProfiling());

                Assert.True(ReadGpuEventAsync(events).GetAwaiter().GetResult(), "No GPU profiling event");
            }
            finally
            {
                Profiler.Disable(ScopeKey);
                Profiler.Unsubscribe(events);
                Profiler.MinimumProfileDuration = minimumProfileDuration;
            }

            // The profiler sends the events to subscribers from a background task
            static async Task<bool> ReadGpuEventAsync(ChannelReader<ProfilingEvent> events)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                try
                {
                    await foreach (var profilingEvent in events.ReadAllAsync(timeout.Token))
                    {
                        if (profilingEvent.Key == ScopeKey && profilingEvent.IsGPUEvent())
                            return true;
                    }
                }
                catch (OperationCanceledException)
                {
                }
                return false;
            }
        }

        /// <summary>
        /// When profiling throws while the query manager reads a pool back, disposing the query manager afterwards releases
        /// each pool once, so the first error stays the one reported.
        /// </summary>
        [SkippableFact]
        public void DisposeAfterFlushErrorReleasesPoolsOnce()
        {
            // The duration of a scope goes to a metrics histogram when the scope ends; a listener that throws makes Flush
            // throw after it has read a pool back
            using var listener = new MeterListener
            {
                InstrumentPublished = (instrument, meterListener) =>
                {
                    if (instrument.Meter.Name == "Stride.Profiler" && instrument.Name == ThrowingKeyName)
                        meterListener.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<double>((_, _, _, _) => throw new ProfilingTestException());
            listener.Start();

            Profiler.Enable(ThrowingKey);
            try
            {
                PerformTest(game =>
                {
                    var commandList = game.GraphicsContext.CommandList;

                    using var allocator = new GraphicsResourceAllocator(game.GraphicsDevice);
                    using var target = Texture.New2D(game.GraphicsDevice, 64, 64, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
                    using var buffer = Buffer.Structured.New<uint>(game.GraphicsDevice, 1, unorderedAccess: true);
                    var queryManager = new QueryManager(commandList, allocator);

                    for (int scope = 0; scope < ScopesPerRound; scope++)
                    {
                        using (queryManager.BeginProfile(Color.Green, ThrowingKey))
                            commandList.Clear(target, Color.Red);
                    }

                    // Reading the buffer back submits the command list and waits for it
                    buffer.GetData<uint>(commandList);

                    // Timestamps can take a moment longer to be ready on some drivers
                    Exception flushError = null;
                    var stopwatch = Stopwatch.StartNew();
                    while (flushError is null && stopwatch.ElapsedMilliseconds < 5000)
                        flushError = Record.Exception(queryManager.Flush);
                    Assert.IsType<ProfilingTestException>(flushError);

                    queryManager.Dispose();
                });
            }
            finally
            {
                Profiler.Disable(ThrowingKey);
            }
        }

        private sealed class ProfilingTestException : Exception;
    }
}
