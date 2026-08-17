// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D

using System;

using Xunit;

using Silk.NET.Core.Native;

namespace Stride.Graphics.Tests;

/// <summary>
/// Regression tests for the GraphicsProfile -> D3DFeatureLevel mapping in GraphicsProfileHelper.
/// Direct3D-only: the helper and D3DFeatureLevel exist only under STRIDE_GRAPHICS_API_DIRECT3D.
/// </summary>
public class TestGraphicsProfileHelper
{
    [Theory]
    [InlineData(GraphicsProfile.Level_9_1, D3DFeatureLevel.Level91)]
    [InlineData(GraphicsProfile.Level_9_2, D3DFeatureLevel.Level92)]
    [InlineData(GraphicsProfile.Level_9_3, D3DFeatureLevel.Level93)]
    [InlineData(GraphicsProfile.Level_10_0, D3DFeatureLevel.Level100)]
    [InlineData(GraphicsProfile.Level_10_1, D3DFeatureLevel.Level101)]
    [InlineData(GraphicsProfile.Level_11_0, D3DFeatureLevel.Level110)]
    [InlineData(GraphicsProfile.Level_11_1, D3DFeatureLevel.Level111)]
    [InlineData(GraphicsProfile.Level_11_2, D3DFeatureLevel.Level111)]
    public void Profile_MapsToExpectedFeatureLevel(GraphicsProfile profile, D3DFeatureLevel expected)
    {
        Assert.Equal(expected, profile.ToFeatureLevel());
    }

    [Fact]
    public void EveryProfile_MapsToADefinedFeatureLevel()
    {
        // Guards against a profile added later without a matching entry in the mapping.
        foreach (GraphicsProfile profile in Enum.GetValues<GraphicsProfile>())
        {
            var featureLevel = profile.ToFeatureLevel();
            Assert.True(Enum.IsDefined(featureLevel),
                $"{profile} (0x{(int)profile:X4}) maps to undefined D3DFeatureLevel 0x{(int)featureLevel:X4}");
        }
    }

    [Fact]
    public void UnknownProfile_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((GraphicsProfile)0x1234).ToFeatureLevel());
    }

    [Fact]
    public void Level_11_1_And_11_2_ShareAFeatureLevel()
    {
        // 11.2 is an API revision on 11_1 hardware, so the two profiles are the same capability tier.
        // Anything that probes a device with them (GraphicsAdapter.IsProfileSupported) answers alike.
        Assert.Equal(GraphicsProfile.Level_11_1.ToFeatureLevel(), GraphicsProfile.Level_11_2.ToFeatureLevel());
    }

    [Fact]
    public void Profiles_MapElementWise_NotByReinterpretingTheSpan()
    {
        GraphicsProfile[] profiles = [GraphicsProfile.Level_11_2, GraphicsProfile.Level_11_1, GraphicsProfile.Level_10_0];

        var featureLevels = GraphicsProfileHelper.ToFeatureLevels(profiles);

        D3DFeatureLevel[] expected = [D3DFeatureLevel.Level111, D3DFeatureLevel.Level111, D3DFeatureLevel.Level100];
        Assert.Equal(expected, featureLevels);
        // A bulk reinterpretation of the span would leak 0xB200 through as a feature level.
        Assert.All(featureLevels, featureLevel => Assert.True(Enum.IsDefined(featureLevel)));
    }

    [Fact]
    public void NoProfiles_MapToNoFeatureLevels()
    {
        Assert.Empty(GraphicsProfileHelper.ToFeatureLevels([]));
    }

    [Fact]
    public void UnknownProfileInASequence_Throws()
    {
        GraphicsProfile[] profiles = [GraphicsProfile.Level_11_0, (GraphicsProfile)0x1234];

        Assert.Throws<ArgumentOutOfRangeException>(() => GraphicsProfileHelper.ToFeatureLevels(profiles));
    }
}

#endif
