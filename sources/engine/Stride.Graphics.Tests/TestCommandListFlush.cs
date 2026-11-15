// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11
using System;
using System.Diagnostics;
using System.Threading;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Xunit;

using Stride.Core.Mathematics;

namespace Stride.Graphics.Tests;

/// <summary>
/// <see cref="CommandList.Flush"/> on Direct3D 11 sends what the immediate context recorded to the GPU, as a frame
/// with no swap-chain present (headless) has nothing else to send it.
/// </summary>
public class TestCommandListFlush : GraphicTestGameBase
{
    [SkippableFact]
    public unsafe void FlushedCommandsReachTheGpuWithoutAnotherFlush()
    {
        PerformTest(game =>
        {
            const int S_FALSE = 1;

            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;
            var context = device.NativeDeviceContext;
            using var target = Texture.New2D(device, 64, 64, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);

            var description = new QueryDesc(Query.Event);
            ComPtr<ID3D11Query> query = default;
            HResult result = device.NativeDevice.Handle->CreateQuery(in description, ref query);
            result.Throw();
            try
            {
                commandList.Clear(target, Color.Red);
                context.Handle->End(query);
                commandList.Flush();

                // Polled without a flush, so that only Flush can have sent the commands
                var completed = 0;
                var stopwatch = Stopwatch.StartNew();
                while (context.Handle->GetData(query, ref completed, sizeof(int), (int) AsyncGetdataFlag.Donotflush) == S_FALSE)
                {
                    Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), "The GPU did not reach the commands: Flush did not send them.");
                    Thread.Sleep(1);
                }
            }
            finally
            {
                query.Dispose();
            }
        });
    }
}
#endif
