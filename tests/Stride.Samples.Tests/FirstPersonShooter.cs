#if STRIDE_AUTOTESTING
using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Games.AutoTesting;

namespace FirstPersonShooter.Tests;

[ScreenshotTest(TemplateId = "B12AF970-1F11-4BC8-9571-3B4DA9E20F05")]
public class FirstPersonShooterScreenshots : IScreenshotTest
{
    public async Task Run(IScreenshotTestContext ctx)
    {
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        await ctx.Screenshot("initial");

        await ctx.Tap(new Vector2(0.5f, 0.7f), TimeSpan.Zero);
        await ctx.WaitTime(TimeSpan.FromMilliseconds(500));
        await ctx.Screenshot("reloading");

        await ctx.WaitTime(TimeSpan.FromMilliseconds(3000));
        await ctx.Screenshot("reloaded");

        await ctx.Tap(new Vector2(0.5f, 0.7f), TimeSpan.Zero);
        await ctx.WaitTime(TimeSpan.FromMilliseconds(1000));
        await ctx.Screenshot("after-fire");

        ctx.Exit();
    }
}
#endif
