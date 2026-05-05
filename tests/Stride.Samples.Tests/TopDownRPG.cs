#if STRIDE_AUTOTESTING
using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Games.AutoTesting;
using Stride.Input;

namespace TopDownRPG.Tests;

[ScreenshotTest(TemplateId = "A363FBC5-89EF-4E7A-B870-6D070813D034")]
public class TopDownRPGScreenshots : IScreenshotTest
{
    public async Task Run(IScreenshotTestContext ctx)
    {
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        await ctx.Tap(new Vector2(0.5f, 0.7f), TimeSpan.FromMilliseconds(500));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(500));
        await ctx.PressKey(Keys.Space, TimeSpan.FromMilliseconds(500));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(500));
        await ctx.Screenshot("after-jump");
        await ctx.WaitTime(TimeSpan.FromMilliseconds(500));
        ctx.Exit();
    }
}
#endif
