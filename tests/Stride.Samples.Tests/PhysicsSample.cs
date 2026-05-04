// Cycle through 3 demo scenes.
#if STRIDE_AUTOTESTING
using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Games.AutoTesting;

namespace PhysicsSample.Tests;

[ScreenshotTest(TemplateId = "d20d150b-d3cb-454e-8c11-620b4c9d393f")]
public class PhysicsSampleScreenshots : IScreenshotTest
{
    public async Task Run(IScreenshotTestContext ctx)
    {
        const string PhysicsHint = "Same demo scene composition (same objects in roughly the same arrangement and same labelled mode). Physics-driven object positions / animation phases vary every run; ignore those.";
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        await ctx.Screenshot("constraints", claudeFallback: PhysicsHint);

        for (int i = 2; i <= 3; i++)
        {
            await ctx.Tap(new Vector2(0.95f, 0.5f), TimeSpan.FromMilliseconds(500));
            await ctx.WaitTime(TimeSpan.FromMilliseconds(1500));
            await ctx.Screenshot($"scene-{i}", claudeFallback: PhysicsHint);
        }

        ctx.Exit();
    }
}
#endif
