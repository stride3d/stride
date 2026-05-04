#if STRIDE_AUTOTESTING
using System;
using System.Threading.Tasks;
using Stride.Games.AutoTesting;

namespace MaterialShader.Tests;

[ScreenshotTest(TemplateId = "f80f8a38-c05a-44bd-ab6d-d2a4f1cf4c58")]
public class MaterialShaderScreenshots : IScreenshotTest
{
    public async Task Run(IScreenshotTestContext ctx)
    {
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        await ctx.Screenshot("steady");
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        ctx.Exit();
    }
}
#endif
