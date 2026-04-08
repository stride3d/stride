// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Graphics.Tests
{
    /// <summary>
    /// DEPRECATED. Precompiled fonts are not supported anymore and will be merged as a feature of the other fonts (Offline/SDF) soon
    /// </summary>
    public class TestPrecompiledSpriteFont : TestSpriteFont
    {
        public TestPrecompiledSpriteFont() : base(assetPrefix: "PrecompiledFonts/", saveImageSuffix: "pre")
        {
        }


        [Fact]
        public void RunTestPrecompiledSpriteFont()
        {
            RunGameTest(new TestPrecompiledSpriteFont());
        }
    }
}
