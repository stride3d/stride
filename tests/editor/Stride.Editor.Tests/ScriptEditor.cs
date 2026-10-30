// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// GameStudio capture for the RoslynPad-backed script editor: open a freshly generated NewGame
// project (one script, almost no assets — the lightest project with a script), open its camera
// script in the script editor, and capture it once Roslyn has classified the document. Guards the
// RoslynPad/Roslyn-5 MEF composition — a regression throws CompositionFailedException and the
// script editor renders blank, drifting this capture.
using System.Threading.Tasks;
using Stride.GameStudio.AutoTesting;

namespace Stride.Editor.Tests;

[UITest(SampleTemplateId = "81d2adea-37b1-4711-834c-0d73a05c206c")]
public class ScriptEditor : IUITest
{
    public async Task Run(IUITestContext ctx)
    {
        // The runner passes the .sln on the CLI, so GS opens straight to GameStudioWindow; a failed
        // open falls back to the modal project picker — treat that as a fast failure.
        var opened = await ctx.WaitForAnyWindow(new[] { GameStudioWindowNames.GameStudio, GameStudioWindowNames.ProjectSelection }, timeoutSeconds: 180);
        if (opened != GameStudioWindowNames.GameStudio) { ctx.Exit(1); return; }
        await ctx.SetWindowSize(GameStudioWindowNames.GameStudio, 2560, 1440);
        await ctx.WaitIdle();

        // Open the camera script in the RoslynPad script editor — this boots RoslynHost/RoslynWorkspace.
        var url = await ctx.OpenAssetEditor("BasicCameraController");
        if (url is null) { ctx.Exit(1); return; }
        await ctx.WaitIdle();

        // Classification runs asynchronously after the document opens (MEF composition + first
        // solution load on a cold machine), so wait for multi-color text rather than a fixed delay.
        // Capture even on timeout — the monochrome/blank frame is the failure evidence.
        var highlighted = await ctx.WaitForSyntaxHighlighting(url, timeoutSeconds: 90);
        await ctx.WaitFrames(2);

        await ctx.CapturePanel(url, "script-editor", 1400, 900);

        ctx.Exit(highlighted ? 0 : 1);
    }
}
