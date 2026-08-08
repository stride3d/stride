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
}

#endif
