#if STRIDE_AUTOTESTING
using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Games.AutoTesting;

namespace AnimatedModel.Tests;

[ScreenshotTest(TemplateId = "99371864-55BD-4C78-B25C-42471F977540")]
public class AnimatedModelScreenshots : IScreenshotTest
{
    public async Task Run(IScreenshotTestContext ctx)
    {
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));

        // ButtonIdle / ButtonRun live in a left-aligned vertical StackPanel; small text-only
        // buttons at the very top-left corner of the portrait viewport.
        await ctx.Tap(new Vector2(0.05f, 0.02f), TimeSpan.FromMilliseconds(500));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        await ctx.Screenshot("idle", claudeFallback: "Knight character standing in idle stance (legs roughly together, arms at sides). Exact pose phase varies between runs.");
        await ctx.WaitTime(TimeSpan.FromMilliseconds(500));

        await ctx.Tap(new Vector2(0.05f, 0.05f), TimeSpan.FromMilliseconds(500));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        await ctx.Screenshot("run", claudeFallback: "Knight character in a running pose (legs apart mid-stride). Exact pose phase varies between runs.");
        await ctx.WaitTime(TimeSpan.FromMilliseconds(500));

        ctx.Exit();
    }
}
#endif
