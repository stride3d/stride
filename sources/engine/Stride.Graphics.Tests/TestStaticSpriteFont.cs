// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Graphics.Tests
{
    public class TestStaticSpriteFont : TestSpriteFont
    {
        public TestStaticSpriteFont()
            : base("StaticFonts/", "sta")
        {
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Fact]
        public void RunTestStaticSpriteFont()
        {
            RunGameTest(new TestStaticSpriteFont());
        }
    }
}
