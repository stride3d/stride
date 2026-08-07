// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect.GGXPrefiltering;

namespace Stride.Graphics.Tests;

public class TestRadiancePrefilteringGgxFaceContinuity : GraphicTestGameBase
{
    // The 12 edges cube faces share, in Direct3D face order, with the orientation each pair meets at.
    private static readonly (int FaceA, Edge EdgeA, int FaceB, Edge EdgeB, bool Reversed)[] SharedEdges =
    {
        (0, Edge.Bottom, 3, Edge.Right,  false), (0, Edge.Left,  4, Edge.Right,  false),
        (0, Edge.Right,  5, Edge.Left,   false), (0, Edge.Top,   2, Edge.Right,  true),
        (1, Edge.Bottom, 3, Edge.Left,   true),  (1, Edge.Left,  5, Edge.Right,  false),
        (1, Edge.Right,  4, Edge.Left,   false), (1, Edge.Top,   2, Edge.Left,   false),
        (2, Edge.Bottom, 4, Edge.Top,    false), (2, Edge.Top,   5, Edge.Top,    true),
        (3, Edge.Bottom, 5, Edge.Bottom, true),  (3, Edge.Top,   4, Edge.Bottom, false),
    };

    private enum Edge { Top, Bottom, Left, Right }

    private const int OutputSize = 64;

    private double seamRatio;
    private double withinFaceVariation;

    protected override void RegisterTests()
    {
        base.RegisterTests();

        FrameGameSystem.Draw(MeasureFaceContinuity);
    }

    /// <summary>
    /// A cubemap discretizes a function over a sphere, so neighboring texels on either side of a shared
    /// face edge must stay as close as neighboring texels within a face. The output format differs from
    /// the source here, which is what a low dynamic range skybox produces in <c>SkyboxGenerator</c>.
    /// </summary>
    private void MeasureFaceContinuity()
    {
        var commandList = GraphicsContext.CommandList;
        var input = Content.Load<Texture>("CubeMap");

        using var output = Texture.New2D(GraphicsDevice, OutputSize, OutputSize, MathUtil.Log2(OutputSize),
            PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget, 6);

        var filter = new RadiancePrefilteringGGXNoCompute(RenderContext.GetShared(Services))
        {
            RadianceMap = input,
            PrefilteredRadiance = output,
            MipmapGenerationCount = MathUtil.Log2(OutputSize),
        };
        filter.Draw(new RenderDrawContext(Services, RenderContext.GetShared(Services), GraphicsContext));

        var faces = new double[6][,];
        for (var face = 0; face < faces.Length; face++)
            faces[face] = ReadLuminance(output, commandList, face);

        var seam = 0.0;
        foreach (var (faceA, edgeA, faceB, edgeB, reversed) in SharedEdges)
        {
            var a = ReadEdge(faces[faceA], edgeA);
            var b = ReadEdge(faces[faceB], edgeB);
            if (reversed)
                Array.Reverse(b);

            var total = 0.0;
            for (var i = 0; i < OutputSize; i++)
                total += Math.Abs(a[i] - b[i]);
            seam += total / OutputSize;
        }
        seam /= SharedEdges.Length;

        var within = 0.0;
        foreach (var face in faces)
        {
            var total = 0.0;
            for (var y = 1; y < OutputSize; y++)
                for (var x = 0; x < OutputSize; x++)
                    total += Math.Abs(face[y, x] - face[y - 1, x]);
            within += total / ((OutputSize - 1) * OutputSize);
        }
        within /= faces.Length;

        withinFaceVariation = within;
        seamRatio = seam / Math.Max(double.Epsilon, within);
    }

    private static double[,] ReadLuminance(Texture texture, CommandList commandList, int face)
    {
        var data = texture.GetData<byte>(commandList, face, 0);
        var result = new double[OutputSize, OutputSize];
        for (var y = 0; y < OutputSize; y++)
        {
            for (var x = 0; x < OutputSize; x++)
            {
                var i = (y * OutputSize + x) * 4;
                result[y, x] = 0.2126 * data[i] + 0.7152 * data[i + 1] + 0.0722 * data[i + 2];
            }
        }
        return result;
    }

    private static double[] ReadEdge(double[,] face, Edge edge)
    {
        var result = new double[OutputSize];
        for (var i = 0; i < OutputSize; i++)
        {
            result[i] = edge switch
            {
                Edge.Top => face[0, i],
                Edge.Bottom => face[OutputSize - 1, i],
                Edge.Left => face[i, 0],
                _ => face[i, OutputSize - 1],
            };
        }
        return result;
    }

    [SkippableFact]
    public void HighestLevelIsContinuousAcrossFaces()
    {
        SkipTestForGraphicPlatform(GraphicsPlatform.Vulkan);

        var game = new TestRadiancePrefilteringGgxFaceContinuity();
        RunGameTest(game);

        // A flat level has no seam to measure, so it would satisfy the ratio below for the wrong reason.
        Assert.True(game.withinFaceVariation > 1.0,
            $"Mip 0 varies by {game.withinFaceVariation:F2} between neighboring texels, so it carries no image. " +
            "The filter returns a single averaged color when it runs at roughness 0.");

        Assert.True(game.seamRatio < 0.5,
            $"Mip 0 steps across cube face edges by {game.seamRatio:F2} times the variation inside a face. " +
            "The levels the filter produces stay near 0.3, so this level does not match its neighbors.");
    }
}
