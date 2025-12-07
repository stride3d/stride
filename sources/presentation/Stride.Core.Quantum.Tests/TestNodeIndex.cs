// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Quantum.Tests;

public class TestNodeIndex
{
    [Fact]
    public void TestEmptyIndex()
    {
        var emptyIndex = NodeIndex.Empty;
        Assert.True(emptyIndex.IsEmpty);
        Assert.False(emptyIndex.IsInt);
        Assert.Null(emptyIndex.Value);
        Assert.False(emptyIndex.TryGetValue(out var value));
        Assert.Null(value);
        Assert.Equal("(null)", emptyIndex.ToString());
    }

    [Fact]
    public void TestDefaultIndex()
    {
        var defaultIndex = new NodeIndex();
        Assert.True(defaultIndex.IsEmpty);
        Assert.False(defaultIndex.IsInt);
        Assert.Null(defaultIndex.Value);
        Assert.Equal(NodeIndex.Empty, defaultIndex);
    }

    [Fact]
    public void TestIntIndex()
    {
        var index = new NodeIndex(42);
        Assert.False(index.IsEmpty);
        Assert.True(index.IsInt);
        Assert.Equal(42, index.Value);
        Assert.Equal(42, index.Int);
        Assert.True(index.TryGetValue(out var value));
        Assert.Equal(42, value);
        Assert.Equal("42", index.ToString());
    }

    [Fact]
    public void TestStringIndex()
    {
        var index = new NodeIndex("test");
        Assert.False(index.IsEmpty);
        Assert.False(index.IsInt);
        Assert.Equal("test", index.Value);
        Assert.True(index.TryGetValue(out var value));
        Assert.Equal("test", value);
        Assert.Equal("test", index.ToString());
    }

    [Fact]
    public void TestInvalidIntCast()
    {
        var index = new NodeIndex("not an int");
        Assert.Throws<InvalidCastException>(() => index.Int);
    }

    [Fact]
    public void TestNestedIndexThrows()
    {
        var index = new NodeIndex(42);
        Assert.Throws<ArgumentException>(() => new NodeIndex(index));
    }

    [Fact]
    public void TestEquality()
    {
        var index1 = new NodeIndex(42);
        var index2 = new NodeIndex(42);
        var index3 = new NodeIndex(43);
        var index4 = new NodeIndex("42");

        Assert.True(index1.Equals(index2));
        Assert.False(index1.Equals(index3));
        Assert.False(index1.Equals(index4));
        Assert.True(index1.Equals((object)index2));
        Assert.False(index1.Equals(null));
        Assert.False(index1.Equals(42));
    }

    [Fact]
    public void TestHashCode()
    {
        var index1 = new NodeIndex(42);
        var index2 = new NodeIndex(42);
        var index3 = new NodeIndex("test");
        var emptyIndex = NodeIndex.Empty;

        Assert.Equal(index1.GetHashCode(), index2.GetHashCode());
        Assert.NotEqual(index1.GetHashCode(), index3.GetHashCode());
        Assert.Equal(0, emptyIndex.GetHashCode());
    }

    [Fact]
    public void TestComparison()
    {
        var index1 = new NodeIndex(10);
        var index2 = new NodeIndex(20);
        var index3 = new NodeIndex(10);
        var emptyIndex = NodeIndex.Empty;

        Assert.True(index1.CompareTo(index2) < 0);
        Assert.True(index2.CompareTo(index1) > 0);
        Assert.Equal(0, index1.CompareTo(index3));
        Assert.True(index1.CompareTo(emptyIndex) > 0);
        Assert.Equal(0, emptyIndex.CompareTo(NodeIndex.Empty));
    }

    [Fact]
    public void TestIComparableComparison()
    {
        IComparable index1 = new NodeIndex(10);
        var index2 = new NodeIndex(20);

        Assert.True(index1.CompareTo(index2) < 0);
        Assert.Throws<ArgumentException>(() => index1.CompareTo("not a NodeIndex"));
        Assert.Throws<ArgumentException>(() => index1.CompareTo(null));
    }

    [Fact]
    public void TestComparisonWithNonComparable()
    {
        var obj1 = new object();
        var obj2 = new object();
        var index1 = new NodeIndex(obj1);
        var index2 = new NodeIndex(obj2);
        var index3 = new NodeIndex(10);
        var index4 = new NodeIndex(20);

        // Non-comparable objects should return 0 when compared to each other
        Assert.Equal(0, index1.CompareTo(index2));
        // Non-comparable should return -1 when compared to comparable
        Assert.True(index1.CompareTo(index3) < 0);
        // Comparable should throw when compared to non-comparable (Int32.CompareTo expects Int32)
        Assert.Throws<ArgumentException>(() => index3.CompareTo(index1));
        // Two comparable values of the same type should work fine
        Assert.True(index3.CompareTo(index4) < 0);
    }
}
