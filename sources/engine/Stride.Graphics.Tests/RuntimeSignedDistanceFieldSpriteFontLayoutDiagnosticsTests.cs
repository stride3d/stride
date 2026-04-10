using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics.Font;
using Xunit;
using Xunit.Abstractions;
using static Stride.Graphics.SpriteFont;

namespace Stride.Graphics.Tests
{
    public class RuntimeSignedDistanceFieldSpriteFontLayoutDiagnosticsTests
    {
        private readonly ITestOutputHelper output;

        public RuntimeSignedDistanceFieldSpriteFontLayoutDiagnosticsTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void RuntimeSdf_PreGenerateGlyphs_IsIdempotent_ForGlyphOffset()
        {
            using var game = new RuntimeSdfDiagnosticsGame(output, runIdempotenceTest: true, runLayoutStabilityTest: false);
            game.Run();
        }

        [Fact]
        public void RuntimeSdf_ThumbnailLikeWarmup_DoesNotChangeMeasureOrPlacement()
        {
            using var game = new RuntimeSdfDiagnosticsGame(output, runIdempotenceTest: false, runLayoutStabilityTest: true);
            game.Run();
        }

        [Fact]
        public void RuntimeSdf_MultilineMetrics_Should_Track_RuntimeRasterized_Reasonably()
        {
            using var game = new RuntimeSdfDiagnosticsGame(
                output,
                runIdempotenceTest: false,
                runLayoutStabilityTest: false,
                runMultilineComparisonTest: true,
                runThumbnailBoundsConsistencyTest: false);

            game.Run();
        }

        [Fact]
        public void RuntimeSdf_ThumbnailMath_Should_Match_GlyphPlacementBounds()
        {
            using var game = new RuntimeSdfDiagnosticsGame(
                output,
                runIdempotenceTest: false,
                runLayoutStabilityTest: false,
                runMultilineComparisonTest: false,
                runThumbnailBoundsConsistencyTest: true);

            game.Run();
        }

        private sealed class RuntimeSdfDiagnosticsGame : GraphicTestGameBase
        {
            private readonly ITestOutputHelper output;
            private readonly bool runIdempotenceTest;
            private readonly bool runLayoutStabilityTest;

            private readonly bool runMultilineComparisonTest;
            private readonly bool runThumbnailBoundsConsistencyTest;

            private RuntimeRasterizedSpriteFont runtimeRasterFont = null!;

            private RuntimeSignedDistanceFieldSpriteFont runtimeFont = null!;
            private bool completed;

            public RuntimeSdfDiagnosticsGame(
                ITestOutputHelper output,
                bool runIdempotenceTest,
                bool runLayoutStabilityTest,
                bool runMultilineComparisonTest,
                bool runThumbnailBoundsConsistencyTest)
            {
                this.output = output;
                this.runIdempotenceTest = runIdempotenceTest;
                this.runLayoutStabilityTest = runLayoutStabilityTest;
                this.runMultilineComparisonTest = runMultilineComparisonTest;
                this.runThumbnailBoundsConsistencyTest = runThumbnailBoundsConsistencyTest;
            }

            public RuntimeSdfDiagnosticsGame(ITestOutputHelper output, bool runIdempotenceTest, bool runLayoutStabilityTest)
            {
                this.output = output;
                this.runIdempotenceTest = runIdempotenceTest;
                this.runLayoutStabilityTest = runLayoutStabilityTest;
            }

            private void RunMultilineComparisonProbe()
            {
                const string text = "Arial\n64 Regular";
                const float requestedFontSize = 46f;

                var sdfDefaultMeasure = runtimeFont.MeasureString(text);
                var sdfRequestedMeasure = runtimeFont.MeasureString(text, requestedFontSize);

                var rasterDefaultMeasure = runtimeRasterFont.MeasureString(text);
                var rasterRequestedMeasure = runtimeRasterFont.MeasureString(text, requestedFontSize);

                var sdfDefaultLineSpacing = runtimeFont.GetFontDefaultLineSpacing(requestedFontSize);
                var sdfTotalLineSpacing = runtimeFont.GetTotalLineSpacing(requestedFontSize);
                var sdfExtraLineSpacing = runtimeFont.GetExtraLineSpacing(requestedFontSize);
                var sdfBaseOffsetY = GetBaseOffsetY(runtimeFont, requestedFontSize);

                var rasterDefaultLineSpacing = runtimeRasterFont.GetFontDefaultLineSpacing(requestedFontSize);
                var rasterTotalLineSpacing = runtimeRasterFont.GetTotalLineSpacing(requestedFontSize);
                var rasterExtraLineSpacing = runtimeRasterFont.GetExtraLineSpacing(requestedFontSize);
                var rasterBaseOffsetY = GetBaseOffsetY(runtimeRasterFont, requestedFontSize);

                var sdfBounds = ComputeGlyphPlacementBounds(runtimeFont, text, requestedFontSize);
                var rasterBounds = ComputeGlyphPlacementBounds(runtimeRasterFont, text, requestedFontSize);

                output.WriteLine("=== Runtime SDF multiline metrics ===");
                output.WriteLine($"Default measure: {sdfDefaultMeasure}");
                output.WriteLine($"Requested measure: {sdfRequestedMeasure}");
                output.WriteLine($"Default line spacing: {sdfDefaultLineSpacing}");
                output.WriteLine($"Total line spacing: {sdfTotalLineSpacing}");
                output.WriteLine($"Extra line spacing: {sdfExtraLineSpacing}");
                output.WriteLine($"Base offset Y: {sdfBaseOffsetY}");
                output.WriteLine($"Placement bounds: {sdfBounds}");

                output.WriteLine("=== Runtime Raster multiline metrics ===");
                output.WriteLine($"Default measure: {rasterDefaultMeasure}");
                output.WriteLine($"Requested measure: {rasterRequestedMeasure}");
                output.WriteLine($"Default line spacing: {rasterDefaultLineSpacing}");
                output.WriteLine($"Total line spacing: {rasterTotalLineSpacing}");
                output.WriteLine($"Extra line spacing: {rasterExtraLineSpacing}");
                output.WriteLine($"Base offset Y: {rasterBaseOffsetY}");
                output.WriteLine($"Placement bounds: {rasterBounds}");

                // Loose sanity assertions first: we are diagnosing, not enforcing pixel identity.
                Assert.True(Math.Abs(sdfRequestedMeasure.X - rasterRequestedMeasure.X) < 120f,
                    $"Requested multiline measure X diverged too much. SDF={sdfRequestedMeasure.X}, Raster={rasterRequestedMeasure.X}");

                Assert.True(Math.Abs(sdfRequestedMeasure.Y - rasterRequestedMeasure.Y) < 120f,
                    $"Requested multiline measure Y diverged too much. SDF={sdfRequestedMeasure.Y}, Raster={rasterRequestedMeasure.Y}");

                Assert.True(Math.Abs(sdfTotalLineSpacing - rasterTotalLineSpacing) < 80f,
                    $"Total line spacing diverged too much. SDF={sdfTotalLineSpacing}, Raster={rasterTotalLineSpacing}");

                Assert.True(Math.Abs(sdfBaseOffsetY - rasterBaseOffsetY) < 80f,
                    $"Base offset diverged too much. SDF={sdfBaseOffsetY}, Raster={rasterBaseOffsetY}");
            }

            private void RunThumbnailBoundsConsistencyProbe()
            {
                const string text = "Arial\n64 Regular";
                var thumbnailSize = new Vector2(256, 256);

                var sdfResult = ComputeThumbnailConsistency(runtimeFont, text, thumbnailSize, 0.95f);
                var rasterResult = ComputeThumbnailConsistency(runtimeRasterFont, text, thumbnailSize, 0.95f);

                output.WriteLine("=== Runtime SDF thumbnail math ===");
                DumpThumbnailConsistency(sdfResult);

                output.WriteLine("=== Runtime Raster thumbnail math ===");
                DumpThumbnailConsistency(rasterResult);

                // First, raster should be reasonably self-consistent.
                Assert.True(smallEnough(rasterResult.MeasureVsPlacementDelta.X, 80f),
                    $"Raster measure/placement X mismatch too large: {rasterResult.MeasureVsPlacementDelta.X}");
                Assert.True(smallEnough(rasterResult.MeasureVsPlacementDelta.Y, 80f),
                    $"Raster measure/placement Y mismatch too large: {rasterResult.MeasureVsPlacementDelta.Y}");

                // Then compare whether SDF is materially worse than raster.
                Assert.True(
                    Math.Abs(sdfResult.MeasureVsPlacementDelta.X) - Math.Abs(rasterResult.MeasureVsPlacementDelta.X) < 80f,
                    $"SDF X mismatch is materially worse than raster. SDF={sdfResult.MeasureVsPlacementDelta.X}, Raster={rasterResult.MeasureVsPlacementDelta.X}");

                Assert.True(
                    Math.Abs(sdfResult.MeasureVsPlacementDelta.Y) - Math.Abs(rasterResult.MeasureVsPlacementDelta.Y) < 80f,
                    $"SDF Y mismatch is materially worse than raster. SDF={sdfResult.MeasureVsPlacementDelta.Y}, Raster={rasterResult.MeasureVsPlacementDelta.Y}");
            }

            private ThumbnailConsistencyResult ComputeThumbnailConsistency(SpriteFont font, string text, Vector2 thumbnailSize, float fontSizeScale)
            {
                var typeNameSize = font.MeasureString(text);
                var scale = fontSizeScale * Math.Min(thumbnailSize.X / typeNameSize.X, thumbnailSize.Y / typeNameSize.Y);
                var desiredFontSize = scale * font.Size;

                if (font.FontType == SpriteFontType.Dynamic || font is RuntimeSignedDistanceFieldSpriteFont)
                {
                    scale = 1f;
                    typeNameSize = font.MeasureString(text, desiredFontSize);

                    if (font is RuntimeSignedDistanceFieldSpriteFont sdf)
                    {
                        // Use the current thumbnail warmup path you added.
                        sdf.PrepareGlyphsForThumbnail(text, new Vector2(desiredFontSize, desiredFontSize), GraphicsContext.CommandList);
                    }
                    else
                    {
                        font.PreGenerateGlyphs(text, new Vector2(desiredFontSize, desiredFontSize));
                    }
                }

                var placementBounds = ComputeGlyphPlacementBounds(font, text, desiredFontSize);
                var measuredBounds = new RectangleF(0, 0, typeNameSize.X, typeNameSize.Y);

                return new ThumbnailConsistencyResult(
                    desiredFontSize,
                    typeNameSize,
                    measuredBounds,
                    placementBounds,
                    new Vector2(
                        placementBounds.Width - measuredBounds.Width,
                        placementBounds.Height - measuredBounds.Height));
            }

            private RectangleF ComputeGlyphPlacementBounds(SpriteFont font, string text, float fontSize)
            {
                var glyphs = EnumerateGlyphPlacements(font, text, fontSize);

                bool any = false;
                float minX = 0, minY = 0, maxX = 0, maxY = 0;

                foreach (var glyph in glyphs)
                {
                    if (glyph.Width <= 0 || glyph.Height <= 0)
                        continue;

                    if (!any)
                    {
                        minX = glyph.X;
                        minY = glyph.Y;
                        maxX = glyph.X + glyph.Width;
                        maxY = glyph.Y + glyph.Height;
                        any = true;
                    }
                    else
                    {
                        minX = Math.Min(minX, glyph.X);
                        minY = Math.Min(minY, glyph.Y);
                        maxX = Math.Max(maxX, glyph.X + glyph.Width);
                        maxY = Math.Max(maxY, glyph.Y + glyph.Height);
                    }
                }

                return any ? new RectangleF(minX, minY, maxX - minX, maxY - minY) : new RectangleF();
            }

            private List<PlacedGlyph> EnumerateGlyphPlacements(SpriteFont font, string text, float fontSize)
            {
                var results = new List<PlacedGlyph>();

                var glyphEnumeratorType = typeof(SpriteFont).GetNestedType("GlyphEnumerator", BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(glyphEnumeratorType);

                var ctor = glyphEnumeratorType!.GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    binder: null,
                    types: new[]
                    {
            typeof(CommandList),
            typeof(StringProxy),
            typeof(Vector2),
            typeof(bool),
            typeof(int),
            typeof(int),
            typeof(SpriteFont),
            typeof((TextAlignment, Vector2)?)
                    },
                    modifiers: null);

                Assert.NotNull(ctor);

                var stringProxyCtor = typeof(StringProxy).GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    binder: null,
                    types: new[] { typeof(string) },
                    modifiers: null);

                Assert.NotNull(stringProxyCtor);

                var stringProxy = stringProxyCtor!.Invoke(new object[] { text });

                object enumerator = ctor!.Invoke(new object[]
                {
        GraphicsContext.CommandList,
        stringProxy,
        new Vector2(fontSize, fontSize),
        true,
        0,
        text.Length,
        font,
        null
                });

                var moveNext = glyphEnumeratorType.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var currentProp = glyphEnumeratorType.GetProperty("Current", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                Assert.NotNull(moveNext);
                Assert.NotNull(currentProp);

                var baseOffsetY = GetBaseOffsetY(font, fontSize);

                while ((bool)moveNext!.Invoke(enumerator, null)!)
                {
                    var current = currentProp!.GetValue(enumerator);
                    Assert.NotNull(current);

                    var currentType = current!.GetType();

                    var xProp = currentType.GetProperty("X", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var yProp = currentType.GetProperty("Y", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var glyphProp = currentType.GetProperty("Glyph", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    Assert.NotNull(xProp);
                    Assert.NotNull(yProp);
                    Assert.NotNull(glyphProp);

                    var x = Convert.ToSingle(xProp!.GetValue(current));
                    var y = Convert.ToSingle(yProp!.GetValue(current));
                    var glyph = glyphProp!.GetValue(current);
                    Assert.NotNull(glyph);

                    dynamic g = glyph!;
                    float placedX = x + (float)g.Offset.X;
                    float placedY = y + baseOffsetY + (float)g.Offset.Y;
                    float width = (float)g.Subrect.Width;
                    float height = (float)g.Subrect.Height;

                    results.Add(new PlacedGlyph(placedX, placedY, width, height));
                }

                return results;
            }

            private static float GetBaseOffsetY(SpriteFont font, float size)
            {
                var method = typeof(SpriteFont).GetMethod("GetBaseOffsetY", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(method);
                return Convert.ToSingle(method!.Invoke(font, new object[] { size }));
            }

            private void DumpThumbnailConsistency(ThumbnailConsistencyResult result)
            {
                output.WriteLine($"Desired font size: {result.DesiredFontSize}");
                output.WriteLine($"Measured size: {result.MeasuredSize}");
                output.WriteLine($"Measured bounds: {result.MeasuredBounds}");
                output.WriteLine($"Placement bounds: {result.PlacementBounds}");
                output.WriteLine($"Placement - measured delta: {result.MeasureVsPlacementDelta}");
            }

            private static bool smallEnough(float value, float tolerance)
            {
                return Math.Abs(value) < tolerance;
            }

            private readonly record struct PlacedGlyph(float X, float Y, float Width, float Height);

            private readonly record struct ThumbnailConsistencyResult(
                float DesiredFontSize,
                Vector2 MeasuredSize,
                RectangleF MeasuredBounds,
                RectangleF PlacementBounds,
                Vector2 MeasureVsPlacementDelta);
            protected override async Task LoadContent()
            {
                await base.LoadContent();

                var fontSystem = Services.GetService<FontSystem>();
                Assert.NotNull(fontSystem);

                runtimeFont = (RuntimeSignedDistanceFieldSpriteFont)fontSystem.NewRuntimeSignedDistanceField(
                    defaultSize: 64,
                    fontName: "Arial",
                    style: FontStyle.Regular,
                    pixelRange: 8,
                    padding: 2,
                    useKerning: false,
                    extraSpacing: 0,
                    extraLineSpacing: 0,
                    defaultCharacter: ' ');

                Assert.NotNull(runtimeFont);

                runtimeRasterFont = (RuntimeRasterizedSpriteFont)fontSystem.NewDynamic(
                    defaultSize: 64,
                    fontName: "Arial",
                    style: FontStyle.Regular,
                    antiAliasMode: FontAntiAliasMode.Grayscale,
                    useKerning: false,
                    extraSpacing: 0,
                    extraLineSpacing: 0,
                    defaultCharacter: ' ');

                Assert.NotNull(runtimeRasterFont);
            }

            protected override void Update(GameTime gameTime)
            {
                base.Update(gameTime);

                if (completed)
                    return;

                if (runIdempotenceTest)
                    RunIdempotenceProbe();

                if (runLayoutStabilityTest)
                    RunLayoutStabilityProbe();

                if (runMultilineComparisonTest)
                    RunMultilineComparisonProbe();

                if (runThumbnailBoundsConsistencyTest)
                    RunThumbnailBoundsConsistencyProbe();

                completed = true;
                Exit();
            }

            private void RunIdempotenceProbe()
            {
                const string text = "A";
                var requestedSize = new Vector2(64, 64);

                var baseline = WarmupAndCaptureGlyphState(text, requestedSize);
                output.WriteLine($"Baseline: {baseline}");

                for (int i = 0; i < 5; i++)
                {
                    var current = WarmupAndCaptureGlyphState(text, requestedSize);
                    output.WriteLine($"Iteration {i + 1}: {current}");

                    Assert.Equal(baseline.Offset.X, current.Offset.X);
                    Assert.Equal(baseline.Offset.Y, current.Offset.Y);
                    Assert.Equal(baseline.XAdvance, current.XAdvance);
                }
            }

            private void RunLayoutStabilityProbe()
            {
                const string text = "Preview text";
                var requestedSize = new Vector2(48, 48);

                var baselineMeasureDefault = runtimeFont.MeasureString(text);
                var baselineMeasureRequested = runtimeFont.MeasureString(text, requestedSize.X);
                var baselineGlyphStates = WarmupAndCaptureGlyphStates(text, requestedSize);

                output.WriteLine($"Baseline default measure: {baselineMeasureDefault}");
                output.WriteLine($"Baseline requested measure: {baselineMeasureRequested}");
                DumpGlyphStates("Baseline glyph states", baselineGlyphStates);

                for (int i = 0; i < 5; i++)
                {
                    var currentMeasureDefault = runtimeFont.MeasureString(text);
                    var currentMeasureRequested = runtimeFont.MeasureString(text, requestedSize.X);
                    var currentGlyphStates = WarmupAndCaptureGlyphStates(text, requestedSize);

                    output.WriteLine($"Iteration {i + 1} default measure: {currentMeasureDefault}");
                    output.WriteLine($"Iteration {i + 1} requested measure: {currentMeasureRequested}");
                    DumpGlyphStates($"Iteration {i + 1} glyph states", currentGlyphStates);

                    Assert.Equal(baselineMeasureDefault.X, currentMeasureDefault.X);
                    Assert.Equal(baselineMeasureDefault.Y, currentMeasureDefault.Y);

                    Assert.Equal(baselineMeasureRequested.X, currentMeasureRequested.X);
                    Assert.Equal(baselineMeasureRequested.Y, currentMeasureRequested.Y);

                    Assert.Equal(baselineGlyphStates.Count, currentGlyphStates.Count);

                    for (int g = 0; g < baselineGlyphStates.Count; g++)
                    {
                        var expected = baselineGlyphStates[g];
                        var actual = currentGlyphStates[g];

                        Assert.Equal(expected.Character, actual.Character);
                        Assert.Equal(expected.Offset.X, actual.Offset.X);
                        Assert.Equal(expected.Offset.Y, actual.Offset.Y);
                        Assert.Equal(expected.XAdvance, actual.XAdvance);
                    }
                }
            }

            private GlyphState WarmupAndCaptureGlyphState(string text, Vector2 requestedSize)
            {
                runtimeFont.PreGenerateGlyphs(text, requestedSize);

                var states = WarmupAndCaptureGlyphStates(text, requestedSize);
                Assert.Single(states);
                return states[0];
            }

            private List<GlyphState> WarmupAndCaptureGlyphStates(string text, Vector2 requestedSize)
            {
                runtimeFont.PreGenerateGlyphs(text, requestedSize);

                var result = new List<GlyphState>();
                var characters = GetCharactersDictionary(runtimeFont);
                var makeKey = GetMakeKeyMethod();
                var getDfParams = GetDfParamsMethod();

                var dfParams = getDfParams.Invoke(runtimeFont, Array.Empty<object>());
                Assert.NotNull(dfParams);

                foreach (var ch in text)
                {
                    if (char.IsWhiteSpace(ch))
                        continue;

                    var key = makeKey.Invoke(null, new[] { (object)ch, dfParams! });
                    Assert.NotNull(key);

                    if (!characters.TryGetValue(key!, out var specObj))
                    {
                        Assert.True(false, $"Character '{ch}' was not present in runtime SDF character cache after warmup.");
                        continue;
                    }

                    Assert.NotNull(specObj);

                    dynamic spec = specObj!;
                    var glyph = spec.Glyph;

                    result.Add(new GlyphState(
                        ch,
                        new Vector2((float)glyph.Offset.X, (float)glyph.Offset.Y),
                        (float)glyph.XAdvance));
                }

                return result;
            }

            private static Dictionary<object, object> GetCharactersDictionary(RuntimeSignedDistanceFieldSpriteFont font)
            {
                var field = typeof(RuntimeSignedDistanceFieldSpriteFont).GetField(
                    "characters",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(field);

                var raw = field!.GetValue(font);
                Assert.NotNull(raw);

                var result = new Dictionary<object, object>();

                var enumerable = raw as System.Collections.IEnumerable;
                Assert.NotNull(enumerable);

                foreach (var item in enumerable!)
                {
                    var itemType = item.GetType();
                    var keyProp = itemType.GetProperty("Key");
                    var valueProp = itemType.GetProperty("Value");

                    Assert.NotNull(keyProp);
                    Assert.NotNull(valueProp);

                    var key = keyProp!.GetValue(item);
                    var value = valueProp!.GetValue(item);

                    Assert.NotNull(key);
                    Assert.NotNull(value);

                    result.Add(key!, value!);
                }

                return result;
            }

            private static MethodInfo GetMakeKeyMethod()
            {
                var method = typeof(RuntimeSignedDistanceFieldSpriteFont).GetMethod(
                    "MakeKey",
                    BindingFlags.Static | BindingFlags.NonPublic);

                Assert.NotNull(method);
                return method!;
            }

            private static MethodInfo GetDfParamsMethod()
            {
                var method = typeof(RuntimeSignedDistanceFieldSpriteFont).GetMethod(
                    "GetDfParams",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(method);
                return method!;
            }

            private void DumpGlyphStates(string title, List<GlyphState> states)
            {
                output.WriteLine(title);
                foreach (var state in states)
                {
                    output.WriteLine(
                        $"  Char='{state.Character}' Offset=({state.Offset.X}, {state.Offset.Y}) XAdvance={state.XAdvance}");
                }
            }

            private readonly record struct GlyphState(char Character, Vector2 Offset, float XAdvance);
        }
    }
}
