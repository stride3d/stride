// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;


namespace Xenko.Graphics.Tests
{
    public class TestDynamicSpriteFont : TestSpriteFont
    {
        public TestDynamicSpriteFont()
            : base("DynamicFonts/", "dyn")
        {
        }

        internal static void Main()
        {
            using (var game = new TestDynamicSpriteFont())
                game.Run();
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
