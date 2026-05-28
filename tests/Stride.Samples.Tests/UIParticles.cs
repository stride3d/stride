#if STRIDE_AUTOTESTING
using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Games.AutoTesting;

namespace UIParticles.Tests;

[ScreenshotTest(TemplateId = "DA4B1982-2A93-48FB-8EDA-7B13AD79E6A2")]
public class UIParticlesScreenshots : IScreenshotTest
{
    public async Task Run(IScreenshotTestContext ctx)
    {
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        await ctx.Screenshot("initial");

        await ctx.Tap(new Vector2(179f / 600f, 235f / 600f), TimeSpan.FromMilliseconds(150));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(250));
        await ctx.Screenshot("after-tap-1");

        await ctx.Tap(new Vector2(360f / 600f, 328f / 600f), TimeSpan.FromMilliseconds(150));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(1250));
        await ctx.Screenshot("after-tap-2");

        await ctx.Tap(new Vector2(179f / 600f, 235f / 600f), TimeSpan.FromMilliseconds(150));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(250));
        await ctx.Screenshot("after-tap-3");

        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        ctx.Exit();
    }
}
#endif
