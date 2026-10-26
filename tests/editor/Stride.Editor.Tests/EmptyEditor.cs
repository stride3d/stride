// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Smoke test: launch the editor with no project, capture the startup ProjectSelectionWindow, exit.
using System.Threading.Tasks;
using Stride.GameStudio.AutoTesting;

namespace Stride.Editor.Tests;

[UITest]
public class EmptyEditor : IUITest
{
    public async Task Run(IUITestContext ctx)
    {
        await ctx.WaitDispatcherIdle();
        await ctx.WaitFrames(2);
        await ctx.Screenshot("startup");
        ctx.Exit();
    }
}
