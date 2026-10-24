// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

using Stride.Graphics.Regression;

namespace Stride.Graphics.Tests
{
    /// <summary>
    ///   No backend implements bindless access: the fields report <see cref="BindlessTier.None"/>, and the device log shows
    ///   the GPU level next to them.
    /// </summary>
    public class TestBindlessTier : GameTestBase
    {
        [SkippableFact]
        public void BackendReportsNoBindless()
        {
            PerformTest(
                game =>
                {
                    var features = game.GraphicsDevice.Features;
                    Assert.Equal(BindlessTier.None, features.BindlessTier);
                    Assert.Equal(0, features.MaxBindlessSampledTextureCount);

                    var gpu = features.GpuBindless;
                    Assert.Equal(gpu.Tier == BindlessTier.None, gpu.MaxSampledTextureCount == 0);
                    if (GraphicsDevice.Platform == GraphicsPlatform.Direct3D11)
                        Assert.Equal(default, gpu);
                    if (GraphicsDevice.Platform == GraphicsPlatform.Direct3D12)
                        Assert.StartsWith("resource binding tier ", gpu.Detail);

                    Assert.Contains($"BindlessTier: None (GPU: {gpu.Tier}", features.ToString());
                    Assert.Contains($"MaxBindlessSampledTextureCount: 0 (GPU: {gpu.MaxSampledTextureCount})", features.ToString());
                });
        }
    }

    /// <summary>
    ///   The GPU bindless level from Vulkan feature bits and limits, and from the D3D12 resource binding tier.
    /// </summary>
    public class TestBindlessTierMapping
    {
        private static readonly VulkanDescriptorIndexingSupport AllVulkanFeatures = new(
            ShaderSampledImageArrayDynamicIndexing: true,
            RuntimeDescriptorArray: true,
            DescriptorBindingPartiallyBound: true,
            DescriptorBindingVariableDescriptorCount: true,
            DescriptorBindingSampledImageUpdateAfterBind: true,
            DescriptorBindingUpdateUnusedWhilePending: true,
            ShaderSampledImageArrayNonUniformIndexing: true,
            MaxPerStageDescriptorUpdateAfterBindSampledImages: 1_048_576,
            MaxDescriptorSetUpdateAfterBindSampledImages: 2_097_152,
            MaxPerStageUpdateAfterBindResources: 1_500_000);

        [Fact]
        public void VulkanAllFeaturesIsNonUniform()
        {
            var support = GraphicsDeviceFeatures.GetVulkanBindlessSupport(AllVulkanFeatures);

            Assert.Equal(new BindlessSupport(BindlessTier.NonUniform, 1_048_576, null), support);
        }

        [Fact]
        public void VulkanWithoutNonUniformIndexingIsUniform()
        {
            var support = GraphicsDeviceFeatures.GetVulkanBindlessSupport(AllVulkanFeatures with { ShaderSampledImageArrayNonUniformIndexing = false });

            Assert.Equal(new BindlessSupport(BindlessTier.Uniform, 1_048_576, "missing shaderSampledImageArrayNonUniformIndexing"), support);
        }

        [Theory]
        [InlineData("shaderSampledImageArrayDynamicIndexing")]
        [InlineData("runtimeDescriptorArray")]
        [InlineData("descriptorBindingPartiallyBound")]
        [InlineData("descriptorBindingVariableDescriptorCount")]
        [InlineData("descriptorBindingSampledImageUpdateAfterBind")]
        [InlineData("descriptorBindingUpdateUnusedWhilePending")]
        public void VulkanWithoutAUniformFeatureIsNone(string feature)
        {
            var input = feature switch
            {
                "shaderSampledImageArrayDynamicIndexing" => AllVulkanFeatures with { ShaderSampledImageArrayDynamicIndexing = false },
                "runtimeDescriptorArray" => AllVulkanFeatures with { RuntimeDescriptorArray = false },
                "descriptorBindingPartiallyBound" => AllVulkanFeatures with { DescriptorBindingPartiallyBound = false },
                "descriptorBindingVariableDescriptorCount" => AllVulkanFeatures with { DescriptorBindingVariableDescriptorCount = false },
                "descriptorBindingSampledImageUpdateAfterBind" => AllVulkanFeatures with { DescriptorBindingSampledImageUpdateAfterBind = false },
                _ => AllVulkanFeatures with { DescriptorBindingUpdateUnusedWhilePending = false },
            };

            Assert.Equal(new BindlessSupport(BindlessTier.None, 0, $"missing {feature}"), GraphicsDeviceFeatures.GetVulkanBindlessSupport(input));

            // Every missing feature is named
            var withoutNonUniform = GraphicsDeviceFeatures.GetVulkanBindlessSupport(input with { ShaderSampledImageArrayNonUniformIndexing = false });
            Assert.Equal(new BindlessSupport(BindlessTier.None, 0, $"missing {feature}, shaderSampledImageArrayNonUniformIndexing"), withoutNonUniform);
        }

        [Fact]
        public void VulkanCountIsTheLowestLimit()
        {
            Assert.Equal(1_000, GraphicsDeviceFeatures.GetVulkanBindlessSupport(AllVulkanFeatures with { MaxDescriptorSetUpdateAfterBindSampledImages = 1_000 }).MaxSampledTextureCount);
            Assert.Equal(2_000, GraphicsDeviceFeatures.GetVulkanBindlessSupport(AllVulkanFeatures with { MaxPerStageUpdateAfterBindResources = 2_000 }).MaxSampledTextureCount);
        }

        [Fact]
        public void VulkanUnlimitedCountIsClamped()
        {
            var support = GraphicsDeviceFeatures.GetVulkanBindlessSupport(AllVulkanFeatures with
            {
                MaxPerStageDescriptorUpdateAfterBindSampledImages = uint.MaxValue,
                MaxDescriptorSetUpdateAfterBindSampledImages = uint.MaxValue,
                MaxPerStageUpdateAfterBindResources = uint.MaxValue,
            });

            Assert.Equal(int.MaxValue, support.MaxSampledTextureCount);
        }

        [Theory]
        [InlineData(1, BindlessTier.None, 0)]
        [InlineData(2, BindlessTier.NonUniform, 1_000_000)]
        [InlineData(3, BindlessTier.NonUniform, 1_000_000)]
        public void D3D12FromResourceBindingTier(int resourceBindingTier, BindlessTier tier, int count)
        {
            var support = GraphicsDeviceFeatures.GetD3D12BindlessSupport(resourceBindingTier);

            Assert.Equal(new BindlessSupport(tier, count, $"resource binding tier {resourceBindingTier}"), support);
        }
    }
}
