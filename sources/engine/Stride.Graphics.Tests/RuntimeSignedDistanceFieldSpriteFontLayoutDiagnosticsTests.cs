using System;
using System.Collections.Generic;
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

        private sealed class RuntimeSdfDiagnosticsGame : GraphicTestGameBase
        {
            private readonly ITestOutputHelper output;
            private readonly bool runIdempotenceTest;
            private readonly bool runLayoutStabilityTest;

            private RuntimeSignedDistanceFieldSpriteFont runtimeFont = null!;
            private bool completed;

            public RuntimeSdfDiagnosticsGame(
                ITestOutputHelper output,
                bool runIdempotenceTest,
                bool runLayoutStabilityTest)
            {
                this.output = output;
                this.runIdempotenceTest = runIdempotenceTest;
                this.runLayoutStabilityTest = runLayoutStabilityTest;
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

                if (runIdempotenceTest)
                    RunIdempotenceProbe();

                if (runLayoutStabilityTest)
                    RunLayoutStabilityProbe();

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
