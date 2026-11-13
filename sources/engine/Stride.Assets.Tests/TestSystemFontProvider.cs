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

        // A font without a name uses the default of the system without saving it, so the asset is the same on every system
        [Fact]
        public void DefaultFontIsNotSaved()
        {
            var font = new SpriteFontAsset { FontSource = new SystemFontProvider() };
            var yaml = Save(font);
            Assert.DoesNotContain("FontName", yaml);

            var loaded = Load(yaml);
            var provider = Assert.IsType<SystemFontProvider>(loaded.FontSource);
            Assert.Null(provider.FontName);
            Assert.False(string.IsNullOrEmpty(provider.GetFontName()));
            Assert.NotNull(provider.GetFontPath());
            Assert.Null(provider.FontName);
            Assert.DoesNotContain("FontName", Save(loaded));
        }

        // A name the user chose is saved as it is, even where the font found is a fallback
        [Fact]
        public void ChosenFontIsSavedAsItIs()
        {
            var provider = new SystemFontProvider("This Font Does Not Exist 12345");
            Assert.NotNull(provider.GetFontPath());

            var yaml = Save(new SpriteFontAsset { FontSource = provider });
            Assert.Contains("FontName: This Font Does Not Exist 12345", yaml);
            Assert.Equal("This Font Does Not Exist 12345", ((SystemFontProvider)Load(yaml).FontSource).FontName);
        }

        // A new font names Arial, so the editor shows a name and the asset is the same on every system
        [Fact]
        public void NewFontsNameArial()
        {
            Stride.Core.Assets.AssetFactory<SpriteFontAsset>[] factories =
            [
                new OfflineRasterizedSpriteFontFactory(),
                new RuntimeRasterizedSpriteFontFactory(),
                new SignedDistanceFieldSpriteFontFactory(),
            ];
            foreach (var factory in factories)
            {
                var font = factory.New();
                Assert.Equal("Arial", Assert.IsType<SystemFontProvider>(font.FontSource).FontName);
                Assert.Contains("FontName: Arial", Save(font));
            }
        }

        private static string Save(SpriteFontAsset font)
        {
            using var stream = new MemoryStream();
            Stride.Core.Assets.AssetFileSerializer.Save(stream, font, null);
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }

        private static SpriteFontAsset Load(string yaml)
        {
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(yaml));
            return Stride.Core.Assets.AssetFileSerializer.Load<SpriteFontAsset>(stream, "Font.sdfnt").Asset;
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
