// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Graphics.Tests
{
    public class TestStaticSpriteFont : TestSpriteFont
    {
        public TestStaticSpriteFont() : base(assetPrefix: "StaticFonts/", saveImageSuffix: "sta")
        {
        }


        [Fact]
        public void RunTestStaticSpriteFont()
        {
            RunGameTest(new TestStaticSpriteFont());
        }
    }
}
