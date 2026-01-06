// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestRandomSeed
{
    [Fact]
    public void TestRandomSeedConstruction()
    {
        // Just verify construction doesn't throw
        var seed = new RandomSeed(12345);
        var value = seed.GetDouble(0);
        Assert.InRange(value, 0.0, 1.0);
    }

    [Fact]
    public void TestGetDouble_ReturnsValueInRange()
    {
        var seed = new RandomSeed(12345);
        var value = seed.GetDouble(0);

        Assert.InRange(value, 0.0, 1.0);
    }

    [Fact]
    public void TestGetFloat_ReturnsValueInRange()
    {
        var seed = new RandomSeed(12345);
        var value = seed.GetFloat(0);

        Assert.InRange(value, 0.0f, 1.0f);
    }

    [Fact]
    public void TestGetDouble_Deterministic()
    {
        // Same seed and offset should produce same value
        var seed1 = new RandomSeed(12345);
        var seed2 = new RandomSeed(12345);

        var value1 = seed1.GetDouble(10);
        var value2 = seed2.GetDouble(10);

        Assert.Equal(value1, value2);
    }

    [Fact]
    public void TestGetFloat_Deterministic()
    {
        // Same seed and offset should produce same value
        var seed1 = new RandomSeed(12345);
        var seed2 = new RandomSeed(12345);

        var value1 = seed1.GetFloat(10);
        var value2 = seed2.GetFloat(10);

        Assert.Equal(value1, value2);
    }

    [Fact]
    public void TestGetDouble_DifferentOffsets()
    {
        var seed = new RandomSeed(12345);

        var value1 = seed.GetDouble(0);
        var value2 = seed.GetDouble(1);
        var value3 = seed.GetDouble(100);

        // Different offsets should (very likely) produce different values
        Assert.NotEqual(value1, value2);
        Assert.NotEqual(value2, value3);
        Assert.NotEqual(value1, value3);
    }

    [Fact]
    public void TestGetFloat_DifferentOffsets()
    {
        var seed = new RandomSeed(12345);

        var value1 = seed.GetFloat(0);
        var value2 = seed.GetFloat(1);
        var value3 = seed.GetFloat(100);

        // Different offsets should (very likely) produce different values
        Assert.NotEqual(value1, value2);
        Assert.NotEqual(value2, value3);
        Assert.NotEqual(value1, value3);
    }

    [Fact]
    public void TestGetDouble_DifferentSeeds()
    {
        var seed1 = new RandomSeed(12345);
        var seed2 = new RandomSeed(67890);

        var value1 = seed1.GetDouble(0);
        var value2 = seed2.GetDouble(0);

        // Different seeds should (very likely) produce different values
        Assert.NotEqual(value1, value2);
    }

    [Fact]
    public void TestGetFloat_DifferentSeeds()
    {
        var seed1 = new RandomSeed(12345);
        var seed2 = new RandomSeed(67890);

        var value1 = seed1.GetFloat(0);
        var value2 = seed2.GetFloat(0);

        // Different seeds should (very likely) produce different values
        Assert.NotEqual(value1, value2);
    }

    [Fact]
    public void TestGetDouble_LargeOffset()
    {
        var seed = new RandomSeed(12345);
        var value = seed.GetDouble(uint.MaxValue);

        Assert.InRange(value, 0.0, 1.0);
    }

    [Fact]
    public void TestGetFloat_LargeOffset()
    {
        var seed = new RandomSeed(12345);
        var value = seed.GetFloat(uint.MaxValue);

        Assert.InRange(value, 0.0f, 1.0f);
    }

    [Fact]
    public void TestGetDouble_ZeroSeed()
    {
        var seed = new RandomSeed(0);
        var value = seed.GetDouble(0);

        Assert.InRange(value, 0.0, 1.0);
    }

    [Fact]
    public void TestGetFloat_ZeroSeed()
    {
        var seed = new RandomSeed(0);
        var value = seed.GetFloat(0);

        Assert.InRange(value, 0.0f, 1.0f);
    }

    [Fact]
    public void TestGetDouble_MaxSeed()
    {
        var seed = new RandomSeed(uint.MaxValue);
        var value = seed.GetDouble(0);

        Assert.InRange(value, 0.0, 1.0);
    }

    [Fact]
    public void TestGetFloat_MaxSeed()
    {
        var seed = new RandomSeed(uint.MaxValue);
        var value = seed.GetFloat(0);

        Assert.InRange(value, 0.0f, 1.0f);
    }
}
