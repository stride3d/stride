// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

using Stride.Rendering;
using Stride.Shaders;

namespace Stride.Graphics.Tests;

/// <summary>
///   Measures what preparing resource groups costs the garbage collector.
/// </summary>
/// <remarks>
///   <para>
///     <see cref="ResourceGroupAllocator.PrepareResourceGroup"/> runs once per resource group per
///     frame, which is a few thousand times in a normal scene. Anything it allocates is allocated
///     again every frame, so this is the one place on the descriptor path where a small allocation
///     becomes a large allocation rate.
///   </para>
///   <para>
///     The pools this path uses grow on first touch, so the measurement discards a warm-up before
///     it counts anything.
///   </para>
/// </remarks>
public class TestResourceGroupAllocation : GraphicTestGameBase
{
    private const int ResourceGroupsPerFrame = 256;
    private const int ShaderResourceBindings = 4;

    /// <summary>
    ///   Preparing resource groups must not allocate once the pools have settled.
    /// </summary>
    [Fact]
    public void PreparingResourceGroupsDoesNotAllocate()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            using var resourceAllocator = new GraphicsResourceAllocator(device);
            using var groupAllocator = new ResourceGroupAllocator(resourceAllocator, commandList);

            var layout = CreateResourceGroupLayout(device);

            var measurement = GCMeasure.Run(
                () => PrepareOneFrameOfResourceGroups(groupAllocator, commandList, layout),
                warmupIterations: 16,
                measuredIterations: 64);

            Assert.True(
                measurement.AllocatedBytes == 0,
                $"Preparing {ResourceGroupsPerFrame} resource groups per frame allocated. Measured {measurement}.");
        });
    }

    /// <summary>
    ///   Models one frame: reset the pools, then prepare every resource group the frame needs.
    /// </summary>
    /// <remarks>
    ///   The reset is what makes the pools reusable. Without it the tracking pool grows for as long
    ///   as the loop runs, because it only rewinds when a frame starts.
    /// </remarks>
    private static void PrepareOneFrameOfResourceGroups(ResourceGroupAllocator groupAllocator, CommandList commandList, ResourceGroupLayout layout)
    {
        groupAllocator.Reset(commandList);

        for (int i = 0; i < ResourceGroupsPerFrame; i++)
        {
            var resourceGroup = groupAllocator.AllocateResourceGroup();
            groupAllocator.PrepareResourceGroup(layout, BufferPoolAllocationType.UsedMultipleTime, resourceGroup);
        }
    }

    /// <summary>
    ///   Builds a layout holding only shader resource views, so the measurement covers the
    ///   descriptor set and not the constant buffer pool.
    /// </summary>
    private static ResourceGroupLayout CreateResourceGroupLayout(GraphicsDevice device)
    {
        var builder = new DescriptorSetLayoutBuilder();

        for (int i = 0; i < ShaderResourceBindings; i++)
        {
            builder.AddBinding(
                ParameterKeys.NewObject<Texture>(name: $"TestResourceGroupAllocation.Texture{i}"),
                logicalGroup: null,
                EffectParameterClass.ShaderResourceView,
                EffectParameterType.Texture2D,
                EffectParameterType.Float);
        }

        return new ResourceGroupLayout
        {
            DescriptorSetLayoutBuilder = builder,
            DescriptorSetLayout = DescriptorSetLayout.New(device, builder),
        };
    }
}
