// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.LightProbes;

namespace Stride.Graphics.Tests;

/// <summary>
/// Captures light probes with <see cref="LightProbeGenerator.GenerateCoefficients"/>, which renders a
/// cubemap per probe and projects it onto the spherical harmonics basis.
/// </summary>
/// <remarks>
/// <para>
///   The probes sit in a box of emissive walls, so every direction that ends on a given wall has exactly
///   that wall's color, whatever the box size and wherever in the box the probe is.
/// </para>
/// <para>
///   Each wall having its own color is what lets this test see an axis or a face being mixed up, which a
///   single-color enclosure cannot: reconstructing a channel from the captured harmonics has to peak
///   along the axis that channel's wall is on.
/// </para>
/// </remarks>
public class LightProbeCaptureTests : EmissiveBoxCaptureTestBase
{
    private const float BoxExtent = 5.0f;

    private const int CoefficientCount =
        LightProbeGenerator.LambertHamonicOrder * LightProbeGenerator.LambertHamonicOrder;

    /// <summary>
    /// One color per wall, keyed by the direction the wall is on. Authored colors are taken as sRGB and
    /// reach the cubemap converted to linear, which this test does not have to account for: it only
    /// compares a channel against itself across directions, and the conversion preserves that order.
    /// </summary>
    /// <remarks>
    /// One channel per axis, lit on the positive side and black on the negative one, so that all of a
    /// channel's light arrives from a single known direction. Order 3 harmonics are very smooth, so
    /// reconstructing along an axis picks up a lot of the four walls around it: colors that share a
    /// channel end up too close together to tell apart, while one channel per axis stays unambiguous.
    /// </remarks>
    private static readonly (Vector3 Direction, Color3 Color)[] Walls =
    [
        (Vector3.UnitX, new Color3(1.0f, 0.0f, 0.0f)),
        (Vector3.UnitY, new Color3(0.0f, 1.0f, 0.0f)),
        (Vector3.UnitZ, new Color3(0.0f, 0.0f, 1.0f)),
        (-Vector3.UnitX, new Color3(0.0f, 0.0f, 0.0f)),
        (-Vector3.UnitY, new Color3(0.0f, 0.0f, 0.0f)),
        (-Vector3.UnitZ, new Color3(0.0f, 0.0f, 0.0f)),
    ];

    /// <summary>
    /// The channel each axis lights, and so the direction that channel has to come back from.
    /// </summary>
    private static readonly (int Channel, string Name, Vector3 Direction)[] LitAxes =
    [
        (0, "red", Vector3.UnitX),
        (1, "green", Vector3.UnitY),
        (2, "blue", Vector3.UnitZ),
    ];

    /// <summary>
    /// Off the center on all three axes, and not on any symmetry plane of the box, so the peaks are checked
    /// away from the symmetric case as well, and the capture is checked to give each probe its own result.
    /// Which way round a face is stays invisible to this test: order 3 harmonics are too smooth to show it,
    /// which is what <see cref="CubemapSceneRendererTests"/> is for.
    /// </summary>
    private static readonly Vector3 OffCenterProbePosition = new(1.3f, -0.7f, 0.9f);

    private Dictionary<LightProbeComponent, List<Color3>> captured;
    private LightProbeComponent centerProbe;
    private LightProbeComponent offCenterProbe;

    protected override void PopulateScene()
    {
        foreach (var (direction, color) in Walls)
            AddEmissiveQuad(direction, direction * BoxExtent, 2.0f * BoxExtent, color);

        centerProbe = AddLightProbe(Vector3.Zero);
        offCenterProbe = AddLightProbe(OffCenterProbePosition);
    }

    protected override void Capture()
    {
        captured = LightProbeGenerator.GenerateCoefficients(this);
    }

    private LightProbeComponent AddLightProbe(Vector3 position)
    {
        var probe = new LightProbeComponent();
        var entity = new Entity("LightProbe") { probe };
        entity.Transform.Position = position;
        Scene.Entities.Add(entity);

        return probe;
    }

    [Fact]
    public void CaptureEncodesWallDirections()
    {
        var game = new LightProbeCaptureTests();
        RunGameTest(game);

        Assert.NotNull(game.captured);
        Assert.Equal(2, game.captured.Count);

        foreach (var probe in new[] { game.centerProbe, game.offCenterProbe })
        {
            var coefficients = game.captured[probe];
            Assert.Equal(CoefficientCount, coefficients.Count);

            var harmonics = ToHarmonics(coefficients);

            // Each channel has one lit wall, so reconstructing it has to peak along that wall's axis and
            // nowhere else. This is what catches a swapped axis, or a positive and negative face the
            // wrong way round: the peak moves to another direction entirely
            foreach (var (channel, name, litDirection) in LitAxes)
            {
                var directions = Walls.Select(w => w.Direction).ToArray();
                var values = directions
                    .Select(d => Channel(harmonics.Evaluate(ToHarmonicsSpace(d)), channel))
                    .ToArray();

                var peak = Array.IndexOf(values, values.Max());

                Assert.True(directions[peak] == litDirection,
                    $"The {name} channel peaks along {directions[peak]}, expected {litDirection} where its " +
                    $"wall is. Values per direction: " +
                    string.Join(", ", directions.Zip(values, (d, v) => $"{d}={v:F3}")));
            }
        }
    }

    private static float Channel(Color3 color, int channel)
        => channel == 0 ? color.R : channel == 1 ? color.G : color.B;

    /// <summary>
    /// Turns a world direction into the space the captured harmonics are in, which is the world with Z
    /// negated. That is the right handed world to cubemap convention: the face a cubemap calls positive Z
    /// holds the view along world negative Z, for a rendered cubemap the same as for a texture asset.
    /// Every shader reading these harmonics negates Z the same way, so this test has to as well:
    /// LightProbeShader, ComputeSphericalHarmonics, LightSkyboxShader and SkyboxShaderCubemap.
    /// </summary>
    private static Vector3 ToHarmonicsSpace(Vector3 worldDirection)
        => new(worldDirection.X, worldDirection.Y, -worldDirection.Z);

    /// <summary>
    /// Rebuilds a <see cref="SphericalHarmonics"/> that <see cref="SphericalHarmonics.Evaluate"/> can use.
    /// <see cref="LightProbeGenerator.GenerateCoefficients"/> returns its coefficients already multiplied
    /// by <see cref="SphericalHarmonics.BaseCoefficients"/>, which Evaluate applies itself.
    /// </summary>
    private static SphericalHarmonics ToHarmonics(List<Color3> coefficients)
    {
        var harmonics = new SphericalHarmonics(LightProbeGenerator.LambertHamonicOrder);
        for (var i = 0; i < coefficients.Count; i++)
            harmonics.Coefficients[i] = coefficients[i] * (1.0f / SphericalHarmonics.BaseCoefficients[i]);

        return harmonics;
    }
}
