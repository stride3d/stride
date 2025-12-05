// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Tests;

public class ReferenceEqualityComparerTests
{
    private class TestClass
    {
        public int Value { get; set; }
    }

    [Fact]
    public void Default_ReturnsSameInstance()
    {
        var comparer1 = ReferenceEqualityComparer<TestClass>.Default;
        var comparer2 = ReferenceEqualityComparer<TestClass>.Default;

        Assert.Same(comparer1, comparer2);
    }

    [Fact]
    public void Equals_WithSameReference_ReturnsTrue()
    {
        var comparer = ReferenceEqualityComparer<TestClass>.Default;
        var obj = new TestClass { Value = 1 };

        Assert.True(comparer.Equals(obj, obj));
    }

    [Fact]
    public void Equals_WithDifferentReferences_ReturnsFalse()
    {
        var comparer = ReferenceEqualityComparer<TestClass>.Default;
        var obj1 = new TestClass { Value = 1 };
        var obj2 = new TestClass { Value = 1 };

        Assert.False(comparer.Equals(obj1, obj2));
    }

    [Fact]
    public void Equals_WithBothNull_ReturnsTrue()
    {
        var comparer = ReferenceEqualityComparer<TestClass>.Default;

        Assert.True(comparer.Equals(null, null));
    }

    [Fact]
    public void Equals_WithOneNull_ReturnsFalse()
    {
        var comparer = ReferenceEqualityComparer<TestClass>.Default;
        var obj = new TestClass();

        Assert.False(comparer.Equals(obj, null));
        Assert.False(comparer.Equals(null, obj));
    }

    [Fact]
    public void GetHashCode_ReturnsSameValueForSameObject()
    {
        var comparer = ReferenceEqualityComparer<TestClass>.Default;
        var obj = new TestClass { Value = 1 };

        var hash1 = comparer.GetHashCode(obj);
        var hash2 = comparer.GetHashCode(obj);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_ReturnsDifferentValuesForDifferentObjects()
    {
        var comparer = ReferenceEqualityComparer<TestClass>.Default;
        var obj1 = new TestClass { Value = 1 };
        var obj2 = new TestClass { Value = 1 };

        var hash1 = comparer.GetHashCode(obj1);
        var hash2 = comparer.GetHashCode(obj2);

        // While technically hash codes can collide, for different objects they should typically differ
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void UseInDictionary_WorksWithReferenceEquality()
    {
        var dict = new Dictionary<TestClass, string>(ReferenceEqualityComparer<TestClass>.Default);
        var key1 = new TestClass { Value = 1 };
        var key2 = new TestClass { Value = 1 };

        dict[key1] = "value1";
        dict[key2] = "value2";

        Assert.Equal(2, dict.Count);
        Assert.Equal("value1", dict[key1]);
        Assert.Equal("value2", dict[key2]);
    }
}
