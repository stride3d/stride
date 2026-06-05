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
        if (!await ctx.WaitForWindow("GameStudioWindow", timeoutSeconds: 180))
        {
            ctx.Exit(1);
            return;
        }
        await ctx.SetWindowSize("GameStudioWindow", 2560, 1440);
        await ctx.WaitIdle();

        await ctx.Screenshot("main");

        ctx.Exit();
    }
}
