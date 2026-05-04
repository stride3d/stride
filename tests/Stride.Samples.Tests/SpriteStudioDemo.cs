#if STRIDE_AUTOTESTING
using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Games.AutoTesting;
using Stride.Input;

namespace SpriteStudioDemo.Tests;

[ScreenshotTest(TemplateId = "6BE30E8D-9346-4130-87BE-12BF9CC362DE")]
public class SpriteStudioDemoScreenshots : IScreenshotTest
{
    public async Task Run(IScreenshotTestContext ctx)
    {
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));

        const string SpriteHint = "Same forest scene, same girl character with sword. Chick (yellow blob enemy) spawn timing is non-deterministic — ignore differences in chick count or position between baseline and capture. Exact animation pose / sword swing phase can also vary.";
        await ctx.Tap(new Vector2(0.83f, 0.05f), TimeSpan.FromMilliseconds(500));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        await ctx.Screenshot("after-tap", claudeFallback: SpriteHint);
        await ctx.WaitTime(TimeSpan.FromMilliseconds(500));

        await ctx.PressKey(Keys.Space, TimeSpan.FromMilliseconds(200));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(100));
        await ctx.Screenshot("after-space", claudeFallback: SpriteHint);
        await ctx.WaitTime(TimeSpan.FromMilliseconds(500));

        ctx.Exit();
    }
}
#endif
