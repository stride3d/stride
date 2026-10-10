// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Regression;
using Stride.Rendering.Compositing;
using Stride.Rendering.Images;

namespace Stride.Engine.Tests;

/// <summary>
/// One screenshot per rendering feature no sample uses: MSAA, temporal anti-aliasing, ambient occlusion, depth of field,
/// local reflections and cubemap backgrounds. Each test turns on its feature only.
/// </summary>
public class RenderFeatureTests : RenderFeatureTestBase
{
    public enum BackgroundMode { None, Cubemap3D, Cubemap2D }

    // Temporal anti-aliasing and depth of field autofocus build up over the previous frames
    private const int ScreenshotFrame = 10;

    public MultisampleCount Msaa { get; init; } = MultisampleCount.None;

    /// <summary>
    /// Turns on the post effects of the test, the optional ones start off. Without it, no post effects.
    /// </summary>
    public Action<PostProcessingEffects> PostEffects { get; init; }

    public BackgroundMode Background { get; init; }

    /// <summary>
    /// False for a render that isn't the same on every run (e.g. it depends on GPU readback timing): it then only
    /// checks that the frames render, without exception or graphics validation error.
    /// </summary>
    public bool Screenshot { get; init; } = true;

    // The teapot, about 4 units from the camera, in focus: the floor in front and behind it blurred
    private static readonly Vector4 DepthOfFieldAreas = new(0.5f, 3.5f, 5f, 10f);

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        var forwardRenderer = (ForwardRenderer)SceneSystem.GraphicsCompositor.SingleView;
        forwardRenderer.MSAALevel = Msaa;
        if (PostEffects != null)
        {
            var postEffects = new PostProcessingEffects();
            postEffects.AmbientOcclusion.Enabled = false;
            postEffects.LocalReflections.Enabled = false;
            postEffects.DepthOfField.Enabled = false;
            postEffects.Bloom.Enabled = false;
            postEffects.LightStreak.Enabled = false;
            postEffects.LensFlare.Enabled = false;
            postEffects.Antialiasing.Enabled = false;
            PostEffects(postEffects);
            forwardRenderer.PostEffects = postEffects;
        }

        CreateScene();

        if (Background != BackgroundMode.None)
        {
            // One color per face
            const int size = 16;
            var faces = new[] { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Cyan, Color.Magenta }
                .Select(color => Enumerable.Repeat(color, size * size).ToArray())
                .ToArray();
            Scene.Entities.Add(new Entity
            {
                new BackgroundComponent
                {
                    Texture = Texture.NewCube(GraphicsDevice, size, PixelFormat.R8G8B8A8_UNorm, faces),
                    Is2D = Background == BackgroundMode.Cubemap2D,
                },
            });
        }
    }

    protected override void RegisterTests()
    {
        base.RegisterTests();
        if (Screenshot)
            FrameGameSystem.TakeScreenshot(ScreenshotFrame);
        else
            FrameGameSystem.Draw(ScreenshotFrame, () => { });
    }

    [SkippableFact]
    public void Msaa4x() => RunGameTest(new RenderFeatureTests { Msaa = MultisampleCount.X4, TestName = nameof(Msaa4x) });

    // The color, normal and specular targets are all multisampled, and resolved one after another
    [SkippableFact]
    public void Msaa4xLocalReflections()
    {
        SkipLocalReflectionsOnAndroid();
        RunGameTest(new RenderFeatureTests { Msaa = MultisampleCount.X4, PostEffects = x => x.LocalReflections.Enabled = true, TestName = nameof(Msaa4xLocalReflections) });
    }

    [SkippableFact]
    public void TemporalAntiAliasing() => RunGameTest(new RenderFeatureTests { PostEffects = x => x.Antialiasing = new TemporalAntiAliasEffect(), TestName = nameof(TemporalAntiAliasing) });

    [SkippableFact]
    public void AmbientOcclusion() => RunGameTest(new RenderFeatureTests { PostEffects = x => x.AmbientOcclusion.Enabled = true, TestName = nameof(AmbientOcclusion) });

    [SkippableFact]
    public void DepthOfFieldTripleRhombi() => RunGameTest(new RenderFeatureTests
    {
        PostEffects = x => { x.DepthOfField.Enabled = true; x.DepthOfField.AutoFocus = false; x.DepthOfField.DOFAreas = DepthOfFieldAreas; x.DepthOfField.Technique = BokehTechnique.HexagonalTripleRhombi; },
        TestName = nameof(DepthOfFieldTripleRhombi),
    });

    [SkippableFact]
    public void DepthOfFieldMcIntosh() => RunGameTest(new RenderFeatureTests
    {
        PostEffects = x => { x.DepthOfField.Enabled = true; x.DepthOfField.AutoFocus = false; x.DepthOfField.DOFAreas = DepthOfFieldAreas; x.DepthOfField.Technique = BokehTechnique.HexagonalMcIntosh; },
        TestName = nameof(DepthOfFieldMcIntosh),
    });

    // The focus follows a depth read back from the GPU, so the frame it reaches depends on timing: no screenshot
    [SkippableFact]
    public void DepthOfFieldAutoFocus() => RunGameTest(new RenderFeatureTests
    {
        PostEffects = x => { x.DepthOfField.Enabled = true; x.DepthOfField.AutoFocus = true; },
        Screenshot = false,
        TestName = nameof(DepthOfFieldAutoFocus),
    });

    [SkippableFact]
    public void LocalReflections()
    {
        SkipLocalReflectionsOnAndroid();
        RunGameTest(new RenderFeatureTests { PostEffects = x => x.LocalReflections.Enabled = true, TestName = nameof(LocalReflections) });
    }

    // On the Android emulator, local reflections crash the test process on some CI hosts (EPYC 9V45, Xeon 8573C),
    // and with MSAA their edges change between runs on the others
    private static void SkipLocalReflectionsOnAndroid() =>
        Skip.If(OperatingSystem.IsAndroid(), "Local reflections crash or render unstable edges on the Android emulator (under investigation).");

    [SkippableFact]
    public void BackgroundCubemap3D() => RunGameTest(new RenderFeatureTests { Background = BackgroundMode.Cubemap3D, TestName = nameof(BackgroundCubemap3D) });

    [SkippableFact]
    public void BackgroundCubemap2D() => RunGameTest(new RenderFeatureTests { Background = BackgroundMode.Cubemap2D, TestName = nameof(BackgroundCubemap2D) });
}
