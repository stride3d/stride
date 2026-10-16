// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Stride.Graphics;

/// <summary>
///   The bindless access a GPU supports.
/// </summary>
/// <param name="Tier">The bindless level.</param>
/// <param name="MaxSampledTextureCount">The maximum number of sampled textures in one bindless array; 0 for <see cref="BindlessTier.None"/>.</param>
/// <param name="Detail">What limits the level (missing Vulkan features, D3D12 resource binding tier), or <see langword="null"/>.</param>
internal readonly record struct BindlessSupport(BindlessTier Tier, int MaxSampledTextureCount, string Detail);

/// <summary>
///   The Vulkan features and limits that decide the bindless level, for sampled images.
/// </summary>
internal readonly record struct VulkanDescriptorIndexingSupport(
    bool ShaderSampledImageArrayDynamicIndexing,
    bool RuntimeDescriptorArray,
    bool DescriptorBindingPartiallyBound,
    bool DescriptorBindingVariableDescriptorCount,
    bool DescriptorBindingSampledImageUpdateAfterBind,
    bool DescriptorBindingUpdateUnusedWhilePending,
    bool ShaderSampledImageArrayNonUniformIndexing,
    uint MaxPerStageDescriptorUpdateAfterBindSampledImages,
    uint MaxDescriptorSetUpdateAfterBindSampledImages,
    uint MaxPerStageUpdateAfterBindResources);

public partial struct GraphicsDeviceFeatures
{
    /// <summary>
    ///   The number of descriptors in a shader-visible D3D12 CBV/SRV/UAV heap that every resource binding tier allows.
    /// </summary>
    internal const int D3D12MaxShaderVisibleDescriptorHeapSize = 1_000_000;

    internal static BindlessSupport GetVulkanBindlessSupport(in VulkanDescriptorIndexingSupport support)
    {
        var missing = new List<string>();
        if (!support.ShaderSampledImageArrayDynamicIndexing)
            missing.Add("shaderSampledImageArrayDynamicIndexing");
        if (!support.RuntimeDescriptorArray)
            missing.Add("runtimeDescriptorArray");
        if (!support.DescriptorBindingPartiallyBound)
            missing.Add("descriptorBindingPartiallyBound");
        if (!support.DescriptorBindingVariableDescriptorCount)
            missing.Add("descriptorBindingVariableDescriptorCount");
        if (!support.DescriptorBindingSampledImageUpdateAfterBind)
            missing.Add("descriptorBindingSampledImageUpdateAfterBind");
        if (!support.DescriptorBindingUpdateUnusedWhilePending)
            missing.Add("descriptorBindingUpdateUnusedWhilePending");

        var tier = missing.Count == 0 ? BindlessTier.Uniform : BindlessTier.None;

        if (!support.ShaderSampledImageArrayNonUniformIndexing)
            missing.Add("shaderSampledImageArrayNonUniformIndexing");
        else if (tier == BindlessTier.Uniform)
            tier = BindlessTier.NonUniform;

        var detail = missing.Count == 0 ? null : $"missing {string.Join(", ", missing)}";
        if (tier == BindlessTier.None)
            return new(BindlessTier.None, 0, detail);

        var count = Math.Min(Math.Min(support.MaxPerStageDescriptorUpdateAfterBindSampledImages,
                                      support.MaxDescriptorSetUpdateAfterBindSampledImages),
                             support.MaxPerStageUpdateAfterBindResources);
        // Some drivers report 0xFFFFFFFF
        return new(tier, (int)Math.Min(count, int.MaxValue), detail);
    }

    internal static BindlessSupport GetD3D12BindlessSupport(int resourceBindingTier)
    {
        var detail = $"resource binding tier {resourceBindingTier}";

        // Tier 1 allows 128 SRVs per stage, and every descriptor of a bound table must be initialized
        return resourceBindingTier >= 2
            ? new(BindlessTier.NonUniform, D3D12MaxShaderVisibleDescriptorHeapSize, detail)
            : new(BindlessTier.None, 0, detail);
    }
}
