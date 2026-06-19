// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// GameStudio capture for an existing TopDownRPG project. The runner passes the .sln path on
// the AutoTesting CLI, so GS opens straight to GameStudioWindow.
using System.Threading.Tasks;
using Stride.GameStudio.AutoTesting;

namespace Stride.Editor.Tests;

[UITest(SampleTemplateId = "A363FBC5-89EF-4E7A-B870-6D070813D034")]
public class TopDownLoad : IUITest
{
    public async Task Run(IUITestContext ctx)
    {
        // A passed .sln should open straight to GameStudioWindow. If the project fails to open,
        // GameStudio falls back to the modal ProjectSelectionWindow — treat that as a fast failure
        // rather than waiting out the timeout for a window that will never appear.
        var opened = await ctx.WaitForAnyWindow(new[] { GameStudioWindowNames.GameStudio, GameStudioWindowNames.ProjectSelection }, timeoutSeconds: 180);
        if (opened != GameStudioWindowNames.GameStudio)
        {
            ctx.Exit(1);
            return;
        }
        await ctx.SetWindowSize(GameStudioWindowNames.GameStudio, 2560, 1440);
        await ctx.WaitIdle();

        // A freshly opened, fully-built project must have no unresolved (IUnloadable) objects.
        if (await ctx.CountUnloadable() > 0)
        {
            ctx.Exit(1);
            return;
        }

        await ctx.Screenshot("main");

        ctx.Exit();
    }
}
