// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.IO;
using System.Linq;
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

        // Core families resolve on every platform: present natively on Windows/macOS, and via the
        // metric-compatible Liberation (or DejaVu) fallback on Linux. Includes a Windows-only font
        // (Segoe UI), a Linux font (Liberation Sans) and a non-Windows font (Helvetica) to exercise
        // the fallback in both directions.
        [Theory]
        [InlineData("Arial")]
        [InlineData("Helvetica")]
        [InlineData("Times New Roman")]
        [InlineData("Courier New")]
        [InlineData("Segoe UI")]
        [InlineData("Liberation Sans")]
        public void KnownFontResolves(string fontName)
        {
            var path = new SystemFontProvider(fontName).GetFontPath();
            Assert.NotNull(path);
            Assert.True(File.Exists(path), $"'{fontName}' did not resolve to an existing font file.");
        }

        // An unknown font name still resolves (via the generic sans fallback) rather than failing.
        [Fact]
        public void UnknownFontFallsBack()
        {
            var path = new SystemFontProvider("This Font Does Not Exist 12345").GetFontPath();
            Assert.NotNull(path);
            Assert.True(File.Exists(path));
        }

        // The candidate chain is what keeps layout (and screenshot goldens) consistent across
        // platforms: the requested font is tried first, then its metric-compatible equivalent in the
        // same typeface category. Tested on the chain itself, so it doesn't depend on which fonts
        // happen to be installed (e.g. msttcorefonts) on the test machine.
        [Theory]
        [InlineData("Arial", "Liberation Sans")]
        [InlineData("Helvetica", "Liberation Sans")]
        [InlineData("Segoe UI", "Liberation Sans")]
        [InlineData("Times New Roman", "Liberation Serif")]
        [InlineData("Courier New", "Liberation Mono")]
        [InlineData("Liberation Sans", "Arial")]            // reverse direction
        [InlineData("Liberation Serif", "Times New Roman")] // reverse direction
        public void FallbackCandidatesAreMetricCompatible(string requested, string expectedEquivalent)
        {
            var candidates = SystemFontProvider.GetFontNameCandidates(requested).ToList();
            Assert.Equal(requested, candidates[0]); // the requested font is always tried first
            Assert.Contains(expectedEquivalent, candidates);
        }

        // Serif/mono requests must not collapse to the sans default (the bug this fixes).
        [Theory]
        [InlineData("Times New Roman")]
        [InlineData("Courier New")]
        public void SerifAndMonoDoNotFallBackToSans(string requested)
        {
            Assert.DoesNotContain("Liberation Sans", SystemFontProvider.GetFontNameCandidates(requested));
        }
    }
}
