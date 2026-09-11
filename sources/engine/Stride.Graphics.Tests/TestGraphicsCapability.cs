// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Xunit;

namespace Stride.Graphics.Tests;

/// <summary>
/// Tests the invariants that hold the capability set together as it grows.
/// </summary>
/// <remarks>
/// These need no device. <c>DeviceFeatureCapability.IsProvidedByDevice</c> ends in a default arm that
/// throws, so the compiler cannot report a kind that nothing answers for. A renderer finds out instead
/// when it asks.
/// </remarks>
public class TestGraphicsCapability
{
    /// <summary>
    /// Every capability the type exposes: one static property each, and a factory for multisampling.
    /// </summary>
    private static IEnumerable<GraphicsCapability> AllCapabilities()
    {
        foreach (var property in typeof(GraphicsCapability).GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            if (property.PropertyType == typeof(GraphicsCapability))
                yield return (GraphicsCapability) property.GetValue(null);
        }

        yield return GraphicsCapability.Multisampling(PixelFormat.R8G8B8A8_UNorm, MultisampleCount.X4);
    }

    [Fact]
    public void EveryKind_HasACapabilityThatAnswersForIt()
    {
        var kinds = AllCapabilities().Select(capability => capability.Kind).ToHashSet();

        foreach (GraphicsCapabilityKind kind in Enum.GetValues<GraphicsCapabilityKind>())
        {
            Assert.True(kinds.Contains(kind), $"No GraphicsCapability answers for {kind}.");
        }
    }

    [Fact]
    public void NoTwoCapabilities_ShareAKind()
    {
        // The static capabilities are near-identical lines. A copy-paste that leaves two on one kind
        // compiles, and then one of them answers with the other one's flag.
        var duplicates = AllCapabilities()
            .GroupBy(capability => capability.Kind)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        Assert.Empty(duplicates);
    }

    [Fact]
    public void EveryFlagCapability_ReadsAFlagFromTheDevice()
    {
        // A default struct has every flag false, which is enough to reach the switch. Multisampling is
        // left out because it indexes a per-format table that a default struct has not built.
        var features = default(GraphicsDeviceFeatures);

        foreach (var capability in AllCapabilities().Where(c => c.Kind != GraphicsCapabilityKind.Multisampling))
        {
            Assert.False(capability.IsProvidedByDevice(features),
                         $"{capability.Kind} claimed a device with no features provides it.");
        }
    }
}
