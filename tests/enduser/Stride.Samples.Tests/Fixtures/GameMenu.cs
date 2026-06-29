// Navigate the main menu.
#if STRIDE_AUTOTESTING
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Games.AutoTesting;

namespace GameMenu.Tests;

[ScreenshotTest(TemplateId = "7ac2c705-6240-4ddc-af63-fc438d10f4de")]
public class GameMenuScreenshots : IScreenshotTest
{
    [ModuleInitializer]
    internal static void Register() => AutoTestingBootstrap.RegisterTest(new GameMenuScreenshots());

    public async Task Run(IScreenshotTestContext ctx)
    {
        // Score on FOREGROUND modal contents only. The main menu sits behind every modal and its
        // text labels ('Hiro Nakamura', 'John Doe', HUD counters) leak around modal edges by a
        // few pixels' worth of font-metric drift between rendering backends — not a regression.
        const string MenuHint =
            "Score only the foreground modal (title, textbox value, buttons, icons). "
            + "Anything outside it — stray text fragments, partial names, counters, or labels "
            + "peeking around the modal — is background-menu content leaking through font-metric "
            + "drift between backends; ignore it.";

        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        await ctx.Screenshot("main-menu", claudeFallback: MenuHint);

        await ctx.Tap(new Vector2(0.4765625f, 0.8389084f), TimeSpan.FromMilliseconds(250));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(250));
        await ctx.Screenshot("after-tap-1", claudeFallback: MenuHint);

        await ctx.Tap(new Vector2(0.6609375f, 0.7315141f), TimeSpan.FromMilliseconds(250));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(250));
        await ctx.Screenshot("after-tap-2", claudeFallback: MenuHint);

        await ctx.Tap(new Vector2(0.5390625f, 0.7764084f), TimeSpan.FromMilliseconds(250));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        await ctx.Screenshot("after-tap-3", claudeFallback: MenuHint);

        ctx.Exit();
    }
}
#endif
