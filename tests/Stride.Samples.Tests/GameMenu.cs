// Navigate the main menu.
#if STRIDE_AUTOTESTING
using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Games.AutoTesting;

namespace GameMenu.Tests;

[ScreenshotTest(TemplateId = "7ac2c705-6240-4ddc-af63-fc438d10f4de")]
public class GameMenuScreenshots : IScreenshotTest
{
    public async Task Run(IScreenshotTestContext ctx)
    {
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        await ctx.Screenshot("main-menu");

        await ctx.Tap(new Vector2(0.4765625f, 0.8389084f), TimeSpan.FromMilliseconds(250));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(250));
        await ctx.Screenshot("after-tap-1");

        await ctx.Tap(new Vector2(0.6609375f, 0.7315141f), TimeSpan.FromMilliseconds(250));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(250));
        await ctx.Screenshot("after-tap-2");

        await ctx.Tap(new Vector2(0.5390625f, 0.7764084f), TimeSpan.FromMilliseconds(250));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        await ctx.Screenshot("after-tap-3");

        ctx.Exit();
    }
}
#endif
