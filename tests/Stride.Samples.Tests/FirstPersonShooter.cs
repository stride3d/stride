#if STRIDE_AUTOTESTING
using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Games.AutoTesting;
using Stride.Input;

namespace FirstPersonShooter.Tests;

[ScreenshotTest(TemplateId = "B12AF970-1F11-4BC8-9571-3B4DA9E20F05")]
public class FirstPersonShooterScreenshots : IScreenshotTest
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
