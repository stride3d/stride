// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

using Stride.Graphics.Regression;

namespace Stride.Graphics.Tests
{
    public class TestEffectProfileFeatures : GameTestBase
    {
        public TestEffectProfileFeatures()
        {
            GraphicsDeviceManager.PreferredGraphicsProfile = [ GraphicsProfile.Level_11_0 ];
        }

        [SkippableFact]
        public void FeaturesFollowTheEffectProfile()
        {
            PerformTest(
                game =>
                {
                    var device = game.GraphicsDevice;
                    var gameShaderProfile = device.ShaderProfile;
                    try
                    {
                        device.ShaderProfile = GraphicsProfile.Level_11_0;
                        var full = device.Features;
                        Assert.True(full.HasComputeShaders);

                        device.ShaderProfile = GraphicsProfile.Level_9_3;
                        var limited = device.Features;

                        // Only Direct3D picks the shader model from the effect profile
                        var isDirect3D = GraphicsDevice.Platform is GraphicsPlatform.Direct3D11 or GraphicsPlatform.Direct3D12;
                        Assert.Equal(!isDirect3D && full.HasComputeShaders, limited.HasComputeShaders);
                        Assert.Equal(!isDirect3D && full.HasGeometryShaders, limited.HasGeometryShaders);
                        Assert.Equal(!isDirect3D && full.HasPixelShaderUnorderedAccess, limited.HasPixelShaderUnorderedAccess);
                        Assert.Equal(!isDirect3D && full.HasMultiSampleDepthAsSRV, limited.HasMultiSampleDepthAsSRV);
                        Assert.Equal(full.HasDepthAsSRV, limited.HasDepthAsSRV);
                        Assert.Equal(full.CurrentProfile, limited.CurrentProfile);
                    }
                    finally
                    {
                        device.ShaderProfile = gameShaderProfile;
                    }
                });
        }
    }
}
