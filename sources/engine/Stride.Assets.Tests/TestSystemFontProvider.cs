// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.IO;
using Stride.Assets.SpriteFont;
using Xunit;

namespace Stride.Assets.Tests
{
    public class TestSystemFontProvider
    {
        [Fact]
        public void DefaultFontResolves()
        {
            // Smoke test: the game-side sprite-font tests bundle their own .ttf/.otf so they're
            // deterministic across platforms. This keeps the SystemFontProvider lookup path itself
            // covered (FreeType probe + per-OS search dirs) without depending on a specific font.
            var path = new SystemFontProvider().GetFontPath();
            Assert.NotNull(path);
            Assert.True(File.Exists(path));
        }
    }
}
