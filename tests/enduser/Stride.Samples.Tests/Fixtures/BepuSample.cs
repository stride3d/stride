// Opens two demo scenes from the scene menu: Colliders, then Heightfield, where the orb tilts.
#if STRIDE_AUTOTESTING
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Stride.Engine;
using Stride.Games.AutoTesting;
using Stride.Input;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Events;

namespace BepuSample.Tests;

[ScreenshotTest(TemplateId = "b65d1a7b-2503-46a8-a454-6dc79fb9f193")]
public class BepuSampleScreenshots : IScreenshotTest
{
    [ModuleInitializer]
    internal static void Register() => AutoTestingBootstrap.RegisterTest(new BepuSampleScreenshots());

    public async Task Run(IScreenshotTestContext ctx)
    {
        const string PhysicsHint = "Same demo scene (same objects in roughly the same arrangement, same camera framing). Physics-driven object positions vary every run; ignore those.";
        await ctx.WaitTime(TimeSpan.FromMilliseconds(2000));
        await ctx.Screenshot("menu");

        // The camera blends from its start pose for about a second after a scene loads, then follows its target.
        await OpenScene(ctx, "Scenes/Colliders");
        await ctx.WaitTime(TimeSpan.FromMilliseconds(3000));
        await ctx.Screenshot("colliders", claudeFallback: PhysicsHint);

        const string OrbHint = "Same heightfield terrain seen from behind the glass orb, same camera framing. The orb may have rolled a little; ignore its exact position.";
        await OpenScene(ctx, "Scenes/Heightfield");
        await ctx.WaitTime(TimeSpan.FromMilliseconds(3000));
        await ctx.Screenshot("heightfield", claudeFallback: OrbHint);

        // W tilts the orb forward
        await ctx.PressKey(Keys.W, TimeSpan.FromMilliseconds(1000));
        await ctx.WaitTime(TimeSpan.FromMilliseconds(500));
        await ctx.Screenshot("heightfield-tilted", claudeFallback: OrbHint);

        ctx.Exit();
    }

    // The menu lists one button per scene, labelled with the scene's full URL (/<package>/Scenes/<name>). Raising its Click
    // runs the sample's own handler, without depending on where the layout puts the button on screen.
    private static async Task OpenScene(IScreenshotTestContext ctx, string sceneUrl)
    {
        var button = ctx.Game.SceneSystem.SceneInstance.RootScene.Entities
            .SelectMany(entity => entity.GetAll<UIComponent>())
            .Where(ui => ui.Enabled && ui.Page?.RootElement != null)
            .SelectMany(ui => ui.Page.RootElement.FindVisualChildrenOfType<Button>())
            .FirstOrDefault(b => b.FindVisualChildOfType<TextBlock>()?.Text?.EndsWith("/" + sceneUrl, StringComparison.Ordinal) == true)
            ?? throw new InvalidOperationException($"No scene button labelled '.../{sceneUrl}' in the menu.");
        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        await ctx.WaitFrames(2);
    }
}
#endif
