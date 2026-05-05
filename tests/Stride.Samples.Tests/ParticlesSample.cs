// Cycle through 7 demo scenes.
#if STRIDE_AUTOTESTING
using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Games.AutoTesting;

namespace ParticlesSample.Tests;

[ScreenshotTest(TemplateId = "35C3FB4D-2A6E-40EB-825E-D4E5670FEE78")]
public class ParticlesSampleScreenshots : IScreenshotTest
{
    public async Task Run(IScreenshotTestContext ctx)
    {
        // Each scene contains random-emission particle systems whose RNG seed isn't pinned to the
        // fixed timestep — a looser threshold absorbs the per-run particle-position jitter.
        const float ParticleSceneThreshold = 0.10f;

        const string ParticleHint = "Same particle demo scene composition (same stones/platforms/objects in same arrangement, same particle effect type and rough region). Particle positions, shapes, and counts vary every run; ignore that.";
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        await ctx.Screenshot("scene-1", ParticleSceneThreshold, claudeFallback: ParticleHint);

        for (int i = 2; i <= 7; i++)
        {
            await ctx.Tap(new Vector2(0.95f, 0.5f), TimeSpan.FromMilliseconds(500));
            await ctx.WaitTime(TimeSpan.FromMilliseconds(1500));
            await ctx.Screenshot($"scene-{i}", ParticleSceneThreshold, claudeFallback: ParticleHint);
        }

        ctx.Exit();
    }
}
#endif
