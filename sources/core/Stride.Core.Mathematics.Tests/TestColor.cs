// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

namespace Stride.Core.Mathematics.Tests
{
    public class TestColor
    {
        [Fact]
        public void TestRGB2HSVConversion()
        {
            Assert.Equal(new ColorHSV(312, 1, 1, 1), ColorHSV.FromColor(new Color4(1, 0, 0.8f, 1)));
            Assert.Equal(new ColorHSV(0, 0, 0, 1), ColorHSV.FromColor(Color.Black));
            Assert.Equal(new ColorHSV(0, 0, 1, 1), ColorHSV.FromColor(Color.White));
            Assert.Equal(new ColorHSV(0, 1, 1, 1), ColorHSV.FromColor(Color.Red));
            Assert.Equal(new ColorHSV(120, 1, 1, 1), ColorHSV.FromColor(Color.Lime));
            Assert.Equal(new ColorHSV(240, 1, 1, 1), ColorHSV.FromColor(Color.Blue));
            Assert.Equal(new ColorHSV(60, 1, 1, 1), ColorHSV.FromColor(Color.Yellow));
            Assert.Equal(new ColorHSV(180, 1, 1, 1), ColorHSV.FromColor(Color.Cyan));
            Assert.Equal(new ColorHSV(300, 1, 1, 1), ColorHSV.FromColor(Color.Magenta));
            Assert.Equal(new ColorHSV(0, 0, 0.7529412f, 1), ColorHSV.FromColor(Color.Silver));
            Assert.Equal(new ColorHSV(0, 0, 0.5019608f, 1), ColorHSV.FromColor(Color.Gray));
            Assert.Equal(new ColorHSV(0, 1, 0.5019608f, 1), ColorHSV.FromColor(Color.Maroon));
        }

        [Fact]
        public void TestHSV2RGBConversion()
        {
            Assert.Equal(Color.Black.ToColor4(), ColorHSV.FromColor(Color.Black).ToColor());
            Assert.Equal(Color.White.ToColor4(), ColorHSV.FromColor(Color.White).ToColor());
            Assert.Equal(Color.Red.ToColor4(), ColorHSV.FromColor(Color.Red).ToColor());
            Assert.Equal(Color.Lime.ToColor4(), ColorHSV.FromColor(Color.Lime).ToColor());
            Assert.Equal(Color.Blue.ToColor4(), ColorHSV.FromColor(Color.Blue).ToColor());
            Assert.Equal(Color.Silver.ToColor4(), ColorHSV.FromColor(Color.Silver).ToColor());
            Assert.Equal(Color.Maroon.ToColor4(), ColorHSV.FromColor(Color.Maroon).ToColor());
            Assert.Equal(new Color(184, 209, 219, 255).ToRgba(), ColorHSV.FromColor(new Color(184, 209, 219, 255)).ToColor().ToRgba());
        }
    }
}
