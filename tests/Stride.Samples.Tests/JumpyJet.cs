// STRIDE_AUTOTESTING gate is explained in samples/Directory.Build.targets.
#if STRIDE_AUTOTESTING
using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Games.AutoTesting;
using Stride.Input;

namespace JumpyJet.Tests;

[ScreenshotTest(TemplateId = "1C9E733A-16BB-48C3-A4DE-722B61EED994")]
public class JumpyJetScreenshots : IScreenshotTest
{
    public async Task Run(IScreenshotTestContext ctx)
    {
        // Jump impulse is 6.5 m/s up, gravity -17 m/s² → apex at ~380ms. Capture mid-ascent
        // (~150ms post-jump) so the bird is well clear of the start position but pipes haven't
        // had time to enter the play area + collide. Whole sequence under ~1s before exit.
        const string JumpHint = "Side-scroller bird game. Bird is alive, mid-flight (HUD shows Score, no Retry/Menu overlay). Pipe positions and exact bird Y vary; ignore those.";
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        await ctx.Screenshot("menu");
        await ctx.Tap(new Vector2(0.5f, 0.7f), TimeSpan.FromMilliseconds(100));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(100));
        await ctx.Screenshot("started", claudeFallback: JumpHint);
        await ctx.PressKey(Keys.Space, TimeSpan.FromMilliseconds(50));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(150));
        await ctx.Screenshot("jumping", claudeFallback: JumpHint);
        ctx.Exit();
    }
}
#endif
