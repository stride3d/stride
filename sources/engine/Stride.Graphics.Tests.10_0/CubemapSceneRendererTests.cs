// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Skyboxes;

namespace Stride.Graphics.Tests;

/// <summary>
/// Renders a scene into a cubemap with <see cref="CubemapSceneRenderer"/> and reads the faces back, to
/// check every face holds the direction it is supposed to, the right way up.
/// </summary>
/// <remarks>
/// <para>
///   The probe sits at the center of a box, and each of the six walls is split into four quadrants with a
///   color of its own, so which quadrant a texel holds is all that decides its value.
/// </para>
/// <para>
///   Twenty four known patches is what makes the faces checkable one texel at a time. Which quadrant a
///   texel has to show follows from its direction alone, so this sees a face being in the wrong slot and
///   a face being flipped or rotated within itself, which a whole-wall color cannot.
/// </para>
/// </remarks>
public class CubemapSceneRendererTests : EmissiveBoxCaptureTestBase
{
    private const float BoxExtent = 5.0f;
    private const int CubemapSize = 128;

    /// <summary>
    /// The six wall axes, in the order <see cref="CubeMapFace"/> uses.
    /// </summary>
    private static readonly Vector3[] WallAxes =
    [
        Vector3.UnitX, -Vector3.UnitX,
        Vector3.UnitY, -Vector3.UnitY,
        Vector3.UnitZ, -Vector3.UnitZ,
    ];

    private Texture cubemap;
    private Half4[][] faces;

    protected override void PopulateScene()
    {
        foreach (var axis in WallAxes)
            foreach (var (first, second) in Quadrants)
            {
                // A quarter-wall quad at the center of its quarter, positioned by its world center so which
                // local axis the plane turns into does not matter
                var (firstAxis, secondAxis) = InPlaneAxes(axis);
                AddEmissiveQuad(axis,
                    axis * BoxExtent + firstAxis * (first * BoxExtent * 0.5f) + secondAxis * (second * BoxExtent * 0.5f),
                    BoxExtent,
                    QuadrantColor(axis, first, second));
            }
    }

    protected override void Capture()
    {
        cubemap = CubemapSceneRenderer.GenerateCubemap(this, Vector3.Zero, CubemapSize);

        faces = Enumerable.Range(0, 6)
            .Select(face => cubemap.GetData<Half4>(GraphicsContext.CommandList, arrayIndex: face))
            .ToArray();
    }

    private static readonly (float First, float Second)[] Quadrants =
        [(-1.0f, -1.0f), (-1.0f, 1.0f), (1.0f, -1.0f), (1.0f, 1.0f)];

    /// <summary>
    /// The two axes spanning the wall on the given axis, in a fixed order so that a quadrant is named the
    /// same when it is built and when it is looked up.
    /// </summary>
    private static (Vector3 First, Vector3 Second) InPlaneAxes(Vector3 axis)
    {
        if (axis.X != 0.0f)
            return (Vector3.UnitY, Vector3.UnitZ);
        if (axis.Y != 0.0f)
            return (Vector3.UnitX, Vector3.UnitZ);

        return (Vector3.UnitX, Vector3.UnitY);
    }

    /// <summary>
    /// A color identifying one quadrant of one wall. Red separates the six walls, green and blue the four
    /// quadrants, so all twenty four are far apart from each other.
    /// </summary>
    private static Color3 QuadrantColor(Vector3 axis, float first, float second)
    {
        var wall = Array.FindIndex(WallAxes, a => a == axis);

        return new Color3(
            (wall + 1) / 6.0f,
            first > 0.0f ? 0.8f : 0.3f,
            second > 0.0f ? 0.8f : 0.3f);
    }

    [Fact]
    public void FacesHoldTheirOwnDirection()
    {
        var game = new CubemapSceneRendererTests();
        RunGameTest(game);

        Assert.NotNull(game.faces);

        var failures = new List<string>();

        for (var face = 0; face < 6; face++)
        {
            // The center of each quarter of the face, which is the center of a wall quadrant and so as
            // far as a sample can get from the boundaries between them
            foreach (var u in new[] { -0.5f, 0.5f })
            foreach (var v in new[] { -0.5f, 0.5f })
            {
                // Authored colors are taken as sRGB and reach the float cubemap converted to linear
                var expected = ExpectedColor(face, u, v).ToLinear();
                var actual = Sample(game.faces[face], u, v);

                if (Distance(expected, actual) > 0.05f * 0.05f)
                {
                    failures.Add($"face {(CubeMapFace)face} at (u {u}, v {v}): expected {expected}, " +
                                 $"got {actual}");
                }
            }
        }

        Assert.True(failures.Count == 0,
            $"{failures.Count} of 24 cubemap samples showed the wrong wall quadrant:\n  " +
            string.Join("\n  ", failures));
    }

    /// <summary>
    /// The quadrant a texel has to show, worked out from the direction that texel stands for.
    /// </summary>
    private static Color3 ExpectedColor(int face, float u, float v)
    {
        var direction = ToWorld(UvToDirection(u, v, face));

        // The wall a ray from the center reaches first is the one on its largest component
        var axis = LargestComponentAxis(direction);
        var hit = direction * (BoxExtent / Math.Abs(Vector3.Dot(direction, axis)));

        var (firstAxis, secondAxis) = InPlaneAxes(axis);

        return QuadrantColor(axis, Vector3.Dot(hit, firstAxis), Vector3.Dot(hit, secondAxis));
    }

    /// <summary>
    /// The direction a face texel stands for, matching <c>uvToDirectionVS</c> in
    /// LambertianPrefilteringSHNoComputePass1, which is the standard cubemap layout.
    /// </summary>
    private static Vector3 UvToDirection(float u, float v, int face) => face switch
    {
        0 => new Vector3(1.0f, -v, -u),
        1 => new Vector3(-1.0f, -v, u),
        2 => new Vector3(u, 1.0f, v),
        3 => new Vector3(u, -1.0f, -v),
        4 => new Vector3(u, -v, 1.0f),
        5 => new Vector3(-u, -v, -1.0f),
        _ => throw new ArgumentOutOfRangeException(nameof(face)),
    };

    /// <summary>
    /// Cubemap directions are the world with Z negated, the right handed world to cubemap convention. The
    /// face a cubemap calls positive Z holds the view along world negative Z, and every shader reading a
    /// cubemap or harmonics from one negates Z to match: LightProbeShader, ComputeSphericalHarmonics,
    /// LightSkyboxShader and SkyboxShaderCubemap.
    /// </summary>
    private static Vector3 ToWorld(Vector3 cubemapDirection)
        => new(cubemapDirection.X, cubemapDirection.Y, -cubemapDirection.Z);

    private static Vector3 LargestComponentAxis(Vector3 v)
    {
        var abs = new Vector3(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));

        if (abs.X >= abs.Y && abs.X >= abs.Z)
            return v.X > 0.0f ? Vector3.UnitX : -Vector3.UnitX;
        if (abs.Y >= abs.Z)
            return v.Y > 0.0f ? Vector3.UnitY : -Vector3.UnitY;

        return v.Z > 0.0f ? Vector3.UnitZ : -Vector3.UnitZ;
    }

    /// <summary>
    /// Reads the texel at a face coordinate, both axes running from -1 to 1.
    /// </summary>
    private static Color3 Sample(Half4[] face, float u, float v)
    {
        var x = (int)((u + 1.0f) * 0.5f * CubemapSize);
        var y = (int)((v + 1.0f) * 0.5f * CubemapSize);
        var texel = face[Math.Clamp(y, 0, CubemapSize - 1) * CubemapSize + Math.Clamp(x, 0, CubemapSize - 1)];

        return new Color3((float)texel.X, (float)texel.Y, (float)texel.Z);
    }

    private static float Distance(Color3 a, Color3 b)
    {
        var d = a - b;

        return d.R * d.R + d.G * d.G + d.B * d.B;
    }
}
