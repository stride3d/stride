// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Graphics.Tests;

/// <summary>
/// The memory of an adapter and the description of its outputs, which every graphics API reports.
/// </summary>
public class TestGraphicsAdapterInfo
{
    [Fact]
    public void EveryAdapterReportsItsMemory()
    {
        foreach (var adapter in GraphicsAdapterFactory.Adapters)
        {
            var memory = adapter.DedicatedVideoMemory + adapter.DedicatedSystemMemory + adapter.SharedSystemMemory;
            Assert.True(memory > 0, $"Adapter {adapter.Description} reports no memory");
        }
    }

    [Fact]
    public void EveryOutputDescribesItsDisplay()
    {
        foreach (var adapter in GraphicsAdapterFactory.Adapters)
        {
            foreach (var output in adapter.Outputs)
            {
                Assert.False(string.IsNullOrEmpty(output.Description), $"An output of {adapter.Description} has no description");
                Assert.True(output.MaxLuminance >= output.MinLuminance, $"Output {output.Description} has a luminance range of {output.MinLuminance} to {output.MaxLuminance}");
                Assert.Equal(output.ColorSpace == ColorSpaceType.Rgb_Full_G2084_None_P2020, output.SupportsHDR);
            }
        }
    }
}
