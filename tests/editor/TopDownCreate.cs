// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// GameStudio capture for the TopDownRPG template "create + open" flow: pick TopDownRPG in
// ProjectSelectionWindow, accept the platform dialog, wait for GameStudioWindow.
using System;
using System.Threading.Tasks;
using Stride.Assets;
using Stride.GameStudio.AutoTesting;

namespace Stride.Editor.Tests;

[UITest(SampleTemplateId = "A363FBC5-89EF-4E7A-B870-6D070813D034")]
public class TopDownCreate : IUITest
{
    public async Task Run(IUITestContext ctx)
    {
        await ctx.WaitDispatcherIdle();

        if (!await ctx.WaitForWindow("ProjectSelectionWindow", timeoutSeconds: 30))
        {
            ctx.Exit(1);
            return;
        }
        await Task.Delay(TimeSpan.FromSeconds(1)); // templates panel populate

        if (!await ctx.SelectTemplate(new Guid("A363FBC5-89EF-4E7A-B870-6D070813D034")))
        {
            ctx.Exit(1);
            return;
        }
        await ctx.WaitFrames(2);

        // Sample flow: ProjectSelectionWindow → DotNetNewTemplateParametersWindow → project gen → GameStudioWindow.
        if (!await ctx.CloseModalWithOk("ProjectSelectionWindow")) { ctx.Exit(1); return; }
        if (!await ctx.WaitForWindow("DotNetNewTemplateParametersWindow", timeoutSeconds: 30)) { ctx.Exit(1); return; }
        if (!await ctx.CloseModalWithOk("DotNetNewTemplateParametersWindow")) { ctx.Exit(1); return; }

        // TopDownRPG pulls in more assets than NewGame; cap at 5 min for cold NuGet restore on CI.
        if (!await ctx.WaitForWindow("GameStudioWindow", timeoutSeconds: 300)) { ctx.Exit(1); return; }
        await ctx.SetWindowSize("GameStudioWindow", 2560, 1440);
        await ctx.WaitIdle();

        await ctx.Screenshot("main");
        await ctx.CapturePanel("AssetView", "panel-assets", 1200, 900);
        await ctx.CapturePanel("PropertyGrid", "panel-properties", 700, 900);
        await ctx.CapturePanel("SolutionExplorer", "panel-solution", 700, 900);
        await ctx.CapturePanel("References", "panel-references", 700, 900);
        await ctx.CapturePanel("BuildLog", "panel-buildlog", 1200, 900);
        // Scene editor document — Title is the asset URL.
        await ctx.CapturePanel(GameSettingsAsset.DefaultSceneLocation, "scene-main", 1400, 900);

        ctx.Exit();
    }
}
