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
        public void RuntimeSdf_PreGenerateGlyphs_IsIdempotent_ForGlyphOffsetAndAdvance()
        {
            using var game = new RuntimeSdfDiagnosticsGame(
                output,
                runIdempotenceRegressionTest: true,
                runThumbnailWarmupLayoutRegressionTest: false,
                runBoundedWarmupUploadConvergenceRegressionTest: false);
            game.Run();
        }

        [Fact]
        public void RuntimeSdf_ThumbnailLikeWarmup_IsLayoutStable_AcrossRepeatedCalls()
        {
            using var game = new RuntimeSdfDiagnosticsGame(
                output,
                runIdempotenceRegressionTest: false,
                runThumbnailWarmupLayoutRegressionTest: true,
                runBoundedWarmupUploadConvergenceRegressionTest: false);
            game.Run();
        }

        [Fact]
        public void RuntimeSdf_BoundedWarmup_TransitionsRequestedGlyphs_ToUploaded()
        {
            using var game = new RuntimeSdfDiagnosticsGame(
                output,
                runIdempotenceRegressionTest: false,
                runThumbnailWarmupLayoutRegressionTest: false,
                runBoundedWarmupUploadConvergenceRegressionTest: true);
            game.Run();
        }

        private sealed class RuntimeSdfDiagnosticsGame : GraphicTestGameBase
        {
            private readonly ITestOutputHelper output;
            private readonly bool runIdempotenceRegressionTest;
            private readonly bool runThumbnailWarmupLayoutRegressionTest;
            private readonly bool runBoundedWarmupUploadConvergenceRegressionTest;

            private RuntimeSignedDistanceFieldSpriteFont runtimeFont = null!;
            private bool completed;

            public RuntimeSdfDiagnosticsGame(
                ITestOutputHelper output,
                bool runIdempotenceRegressionTest,
                bool runThumbnailWarmupLayoutRegressionTest,
                bool runBoundedWarmupUploadConvergenceRegressionTest)
            {
                this.output = output;
                this.runIdempotenceRegressionTest = runIdempotenceRegressionTest;
                this.runThumbnailWarmupLayoutRegressionTest = runThumbnailWarmupLayoutRegressionTest;
                this.runBoundedWarmupUploadConvergenceRegressionTest = runBoundedWarmupUploadConvergenceRegressionTest;
            }

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
            }

            protected override void Update(GameTime gameTime)
            {
                base.Update(gameTime);

                if (completed)
                    return;

                if (runIdempotenceRegressionTest)
                    RunPreGenerateGlyphsIdempotenceRegression();

                if (runThumbnailWarmupLayoutRegressionTest)
                    RunThumbnailWarmupLayoutRegression();

                if (runBoundedWarmupUploadConvergenceRegressionTest)
                    RunBoundedWarmupUploadConvergenceRegression();

                completed = true;
                Exit();
            }

            private void RunPreGenerateGlyphsIdempotenceRegression()
            {
                const string text = "A";
                var requestedSize = new Vector2(64, 64);

                var baseline = WarmupAndCaptureGlyphState(text, requestedSize);

                for (int i = 0; i < 5; i++)
                {
                    var current = WarmupAndCaptureGlyphState(text, requestedSize);
                    AssertGlyphStateEqual(baseline, current, $"Iteration {i + 1}");
                }
            }

            private void RunThumbnailWarmupLayoutRegression()
            {
                const string text = "Preview text";
                var requestedSize = new Vector2(48, 48);

                var baselineMeasureDefault = runtimeFont.MeasureString(text);
                var baselineMeasureRequested = runtimeFont.MeasureString(text, requestedSize.X);
                var baselineGlyphStates = WarmupAndCaptureGlyphStates(text, requestedSize);

                for (int i = 0; i < 5; i++)
                {
                    var currentMeasureDefault = runtimeFont.MeasureString(text);
                    var currentMeasureRequested = runtimeFont.MeasureString(text, requestedSize.X);
                    var currentGlyphStates = WarmupAndCaptureGlyphStates(text, requestedSize);

                    AssertVector2AlmostEqual(baselineMeasureDefault, currentMeasureDefault, $"Default measure iteration {i + 1}");
                    AssertVector2AlmostEqual(baselineMeasureRequested, currentMeasureRequested, $"Requested measure iteration {i + 1}");

                    Assert.Equal(baselineGlyphStates.Count, currentGlyphStates.Count);
                    for (int g = 0; g < baselineGlyphStates.Count; g++)
                    {
                        AssertGlyphStateEqual(baselineGlyphStates[g], currentGlyphStates[g], $"Glyph {g} iteration {i + 1}");
                    }
                }
            }

            private void RunBoundedWarmupUploadConvergenceRegression()
            {
                const string text = "Arial\n64 Regular";
                var requestedSize = new Vector2(64, 64);
                const int maxIterations = 12;

                var makeKey = GetMakeKeyMethod();
                var getDfParams = GetDfParamsMethod();
                var dfParams = getDfParams.Invoke(runtimeFont, Array.Empty<object>());
                Assert.NotNull(dfParams);

                var requestedGlyphs = text
                    .Where(ch => !char.IsWhiteSpace(ch))
                    .Distinct()
                    .Select(ch => (Character: ch, Key: makeKey.Invoke(null, new[] { (object)ch, dfParams! })))
                    .ToList();

                foreach (var glyph in requestedGlyphs)
                    Assert.NotNull(glyph.Key);

                for (int iteration = 1; iteration <= maxIterations; iteration++)
                {
                    runtimeFont.PrepareGlyphsForThumbnail(text, requestedSize, GraphicsContext.CommandList);

                    var characters = GetCharactersDictionary(runtimeFont);
                    int uploadedCount = 0;
                    var pending = new List<char>();

                    foreach (var (character, key) in requestedGlyphs)
                    {
                        Assert.NotNull(key);

                        if (!characters.TryGetValue(key!, out var specObj))
                        {
                            pending.Add(character);
                            continue;
                        }

                        dynamic spec = specObj!;
                        if (spec.IsBitmapUploaded)
                            uploadedCount++;
                        else
                            pending.Add(character);
                    }

                    output.WriteLine($"Iteration {iteration}/{maxIterations}: uploaded={uploadedCount}/{requestedGlyphs.Count}, pending=\"{new string(pending.ToArray())}\"");

                    if (uploadedCount == requestedGlyphs.Count)
                        return;
                }

                Assert.Fail($"Not all requested glyphs transitioned to uploaded within {maxIterations} iterations.");
            }

            private GlyphState WarmupAndCaptureGlyphState(string text, Vector2 requestedSize)
            {
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
                        Assert.Fail($"Character '{ch}' was not present in runtime SDF character cache after warmup.");

                    dynamic spec = specObj!;
                    var glyph = spec.Glyph;

                    result.Add(new GlyphState(
                        ch,
                        new Vector2((float)glyph.Offset.X, (float)glyph.Offset.Y),
                        (float)glyph.XAdvance));
                }

                return result;
            }

            private static void AssertGlyphStateEqual(GlyphState expected, GlyphState actual, string context)
            {
                Assert.Equal(expected.Character, actual.Character);
                AssertVector2AlmostEqual(expected.Offset, actual.Offset, $"{context} offset");
                Assert.InRange(Math.Abs(expected.XAdvance - actual.XAdvance), 0f, 0.0001f);
            }

            private static void AssertVector2AlmostEqual(Vector2 expected, Vector2 actual, string context)
            {
                Assert.InRange(Math.Abs(expected.X - actual.X), 0f, 0.0001f);
                Assert.InRange(Math.Abs(expected.Y - actual.Y), 0f, 0.0001f);
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
                    "GetDistanceFieldParams",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(method);
                return method!;
            }

            private readonly record struct GlyphState(char Character, Vector2 Offset, float XAdvance);
        }
    }
}
