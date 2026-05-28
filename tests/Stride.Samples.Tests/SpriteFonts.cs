// 8 categories: Introduction / Static / Dynamic / Style / Alias / Language / Alignment / Animation.
// Pause the auto-advance with Space, then step through each with Right.
#if STRIDE_AUTOTESTING
using System;
using System.Threading.Tasks;
using Stride.Games.AutoTesting;
using Stride.Input;

namespace SpriteFonts.Tests;

[ScreenshotTest(TemplateId = "1EEB50EC-1AA7-4D1F-9DDD-E5E12404B001")]
public class SpriteFontsScreenshots : IScreenshotTest
{
    public async Task Run(IScreenshotTestContext ctx)
    {
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        await ctx.PressKey(Keys.Space, TimeSpan.FromMilliseconds(100));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(500));
        await ctx.Screenshot("font-1");

        for (int i = 2; i <= 8; i++)
        {
            await ctx.PressKey(Keys.Right, TimeSpan.FromMilliseconds(100));
            await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
            await ctx.Screenshot($"font-{i}");
        }

        ctx.Exit();
    }
}
#endif
