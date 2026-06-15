#if STRIDE_AUTOTESTING
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Stride.Games.AutoTesting;

namespace CustomEffect.Tests;

[ScreenshotTest(TemplateId = "16476A4C-C131-4F48-865A-288EC7D5445F")]
public class CustomEffectScreenshots : IScreenshotTest
{
    [ModuleInitializer]
    internal static void Register() => AutoTestingBootstrap.RegisterTest(new CustomEffectScreenshots());

    public async Task Run(IScreenshotTestContext ctx)
    {
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        await ctx.Screenshot("steady");
        await ctx.WaitTime(TimeSpan.FromMilliseconds(500));
        ctx.Exit();
    }
}
#endif
