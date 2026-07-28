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
    [Fact]
    public void Level_11_2_MapsTo_Level_11_1()
    {
        // 0xB200 has no native D3D feature level; it must resolve to the real 11_1 (0xB100),
        // matching Vulkan, instead of the raw-cast 0xB200 that fails CreateDevice on D3D11/D3D12.
        Assert.Equal(GraphicsProfile.Level_11_1.ToFeatureLevel(), GraphicsProfile.Level_11_2.ToFeatureLevel());
        Assert.NotEqual((D3DFeatureLevel)0xB200, GraphicsProfile.Level_11_2.ToFeatureLevel());
    }

    [Fact]
    public void EveryProfile_MapsToADefinedFeatureLevel()
    {
        // Guards against any profile (now or future) mapping to a value with no native feature level.
        foreach (GraphicsProfile profile in Enum.GetValues<GraphicsProfile>())
        {
            var featureLevel = profile.ToFeatureLevel();
            Assert.True(Enum.IsDefined(featureLevel),
                $"{profile} (0x{(int)profile:X4}) maps to undefined D3DFeatureLevel 0x{(int)featureLevel:X4}");
        }
    }
}

#endif
