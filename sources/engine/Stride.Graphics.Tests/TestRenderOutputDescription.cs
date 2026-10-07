// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Graphics.Tests
{
    public class TestRenderOutputDescription
    {
        [Fact]
        public void SpanShorterThanEightSetsItsTargets()
        {
            var output = new RenderOutputDescription([PixelFormat.R32G32B32A32_Float, PixelFormat.R16G16B16A16_Float]);

            Assert.Equal(2, output.RenderTargetCount);
            Assert.Equal(PixelFormat.R32G32B32A32_Float, output.RenderTargetFormat0);
            Assert.Equal(PixelFormat.R16G16B16A16_Float, output.RenderTargetFormat1);
            Assert.Equal(PixelFormat.None, output.RenderTargetFormat2);
        }

        [Fact]
        public void EmptySpanHasNoTargets()
        {
            var output = new RenderOutputDescription([], PixelFormat.D32_Float);

            Assert.Equal(0, output.RenderTargetCount);
            Assert.Equal(PixelFormat.D32_Float, output.DepthStencilFormat);
        }

        [Fact]
        public void TrailingNoneIsNotCounted()
        {
            var output = new RenderOutputDescription([PixelFormat.R8G8B8A8_UNorm, PixelFormat.None, PixelFormat.R16_Float, PixelFormat.None]);

            Assert.Equal(3, output.RenderTargetCount);
        }
    }
}
