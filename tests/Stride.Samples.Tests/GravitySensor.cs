#if STRIDE_AUTOTESTING
using System;
using System.Threading.Tasks;
using Stride.Games.AutoTesting;
using Stride.Input;

namespace GravitySensor.Tests;

[ScreenshotTest(TemplateId = "7174D040-C0FB-4D5C-8170-3411AD8AA4C2")]
public class GravitySensorScreenshots : IScreenshotTest
{
    public async Task Run(IScreenshotTestContext ctx)
    {
        // GravityScript only applies the directional force while the key is held (Input.IsKeyDown),
        // so the key has to stay down across the settle-wait and the screenshot.
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));

        ctx.PressKey(Keys.Down);
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2500));
        await ctx.Screenshot("down");
        ctx.ReleaseKey(Keys.Down);
        await ctx.WaitTime(TimeSpan.FromMilliseconds(500));

        ctx.PressKey(Keys.Up);
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2500));
        await ctx.Screenshot("up");
        ctx.ReleaseKey(Keys.Up);
        await ctx.WaitTime(TimeSpan.FromMilliseconds(500));

        ctx.PressKey(Keys.Left);
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2500));
        await ctx.Screenshot("left");
        ctx.ReleaseKey(Keys.Left);
        await ctx.WaitTime(TimeSpan.FromMilliseconds(500));

        ctx.Exit();
    }
}
#endif
