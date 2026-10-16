// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;

using Xunit;

using Stride.Graphics.Regression;

namespace Stride.Graphics.Tests
{
    public class TestTimestampQueries : GraphicTestGameBase
    {
        /// <summary>
        /// A timestamp pool is written between draws, read back, then written and read back again, as the query manager
        /// does when it recycles a pool. The second read must give the new timestamps.
        /// </summary>
        [SkippableFact]
        public void ReusedPoolGivesNewTimestamps()
        {
            PerformTest(game =>
            {
                using var pool = QueryPool.New(game.GraphicsDevice, QueryType.Timestamp, 2);

                var first = WriteTwoAndRead(game, pool);
                var second = WriteTwoAndRead(game, pool);

                Assert.True(first[1] >= first[0]);
                Assert.True(second[1] >= second[0]);
                Assert.True(second[0] > first[1], $"The second read gave old timestamps: {first[0]}, {first[1]} then {second[0]}, {second[1]}");
            });
        }

        private static long[] WriteTwoAndRead(GameTestBase game, QueryPool pool)
        {
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            using var source = Texture.New2D(device, 4, 4, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource);
            using var target = Texture.New2D(device, 64, 64, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
            using var buffer = Buffer.Structured.New<uint>(device, 1, unorderedAccess: true);

            // Timestamps between two draws, so inside a render pass on Vulkan
            commandList.SetRenderTargetAndViewport(null, target);
            device.PrimitiveQuad.Draw(game.GraphicsContext, source);
            commandList.WriteTimestamp(pool, 0);
            device.PrimitiveQuad.Draw(game.GraphicsContext, source);
            commandList.WriteTimestamp(pool, 1);

            // Reading the buffer back submits the command list and waits for it
            buffer.GetData<uint>(commandList);

            // The scene draw after the test needs the back buffer
            commandList.SetRenderTargetAndViewport(device.Presenter.DepthStencilBuffer, device.Presenter.BackBuffer);

            var data = new long[pool.QueryCount];
            var stopwatch = Stopwatch.StartNew();
            while (!pool.TryGetData(data))
                Assert.True(stopwatch.ElapsedMilliseconds < 5000, "Timestamps not ready after 5 s");
            return data;
        }
    }
}
