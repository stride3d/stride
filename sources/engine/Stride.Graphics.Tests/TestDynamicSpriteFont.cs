// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;


namespace Stride.Graphics.Tests
{
    public class TestDynamicSpriteFont : TestSpriteFont
    {
        public TestDynamicSpriteFont()
            : base("DynamicFonts/", "dyn")
        {
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Fact]
        public void RunTestDynamicSpriteFont()
        {
            RunGameTest(new TestDynamicSpriteFont());
        }
    }
}
