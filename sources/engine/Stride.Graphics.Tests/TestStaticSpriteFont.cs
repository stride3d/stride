// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
        [Fact(Skip = "Only run when working on graphic related changes and tests, otherwise results are results are not deterministic and inconsistent between hardware. " +
            "TODO: Remove this skip when we get teamcity agent functional again.")]
        public void RunTestStaticSpriteFont()
        {
            RunGameTest(new TestStaticSpriteFont());
        }
    }
}
