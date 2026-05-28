#if STRIDE_AUTOTESTING
using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Games.AutoTesting;
using Stride.Input;

namespace ThirdPersonPlatformer.Tests;

[ScreenshotTest(TemplateId = "990311E4-152B-458D-8CBD-180903845DA7")]
public class ThirdPersonPlatformerScreenshots : IScreenshotTest
{
    public async Task Run(IScreenshotTestContext ctx)
    {
        const string PoseHint = "Same arena, same character roughly in the middle of the floor. Idle animation phase varies (standing / crouching / squatting are all OK).";
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        await ctx.Tap(new Vector2(0.5f, 0.7f), TimeSpan.FromMilliseconds(500));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(500));
        await ctx.PressKey(Keys.Space, TimeSpan.FromMilliseconds(500));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(500));
        await ctx.Screenshot("after-jump", claudeFallback: PoseHint);
        await ctx.WaitTime(TimeSpan.FromMilliseconds(500));
        ctx.Exit();
    }
}
#endif
