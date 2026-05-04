#if STRIDE_AUTOTESTING
using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Games.AutoTesting;
using Stride.Input;

namespace SpaceEscape.Tests;

[ScreenshotTest(TemplateId = "F9C4B79D-E313-47BC-9287-75A0395B8AC4")]
public class SpaceEscapeScreenshots : IScreenshotTest
{
    public async Task Run(IScreenshotTestContext ctx)
    {
        // Capture quickly: a procedural obstacle in front of the ship can kill the run if we
        // wait too long after starting. One lane change is enough to verify input is wired up.
        const string LevelHint = "Endless runner with 3 lanes. Only check that the red ship is in the expected lane (center or left) — same as the baseline. Ignore everything else (procedurally-generated track segments, obstacle/pipe placement, distance counter, animation phase).";
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        await ctx.Screenshot("intro");
        await ctx.Tap(new Vector2(0.496875f, 0.8010563f), TimeSpan.FromMilliseconds(500));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(500));
        await ctx.Screenshot("center", claudeFallback: LevelHint);
        await ctx.PressKey(Keys.Left, TimeSpan.FromMilliseconds(100));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(500));
        await ctx.Screenshot("left", claudeFallback: LevelHint);
        ctx.Exit();
    }
}
#endif
