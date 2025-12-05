// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Collections;
using Xunit;

namespace Stride.Core.Tests.Collections;

public class ReadOnlySetTests
{
    [Fact]
    public void Constructor_WrapsSet()
    {
        var innerSet = new HashSet<int> { 1, 2, 3 };
        var readOnlySet = new ReadOnlySet<int>(innerSet);

        Assert.Equal(3, readOnlySet.Count);
    }

    [Fact]
    public void Contains_ExistingItem_ReturnsTrue()
    {
        var innerSet = new HashSet<string> { "a", "b", "c" };
        var readOnlySet = new ReadOnlySet<string>(innerSet);

        Assert.True(readOnlySet.Contains("b"));
    }

    [Fact]
    public void Contains_NonExistingItem_ReturnsFalse()
    {
        var innerSet = new HashSet<string> { "a", "b", "c" };
        var readOnlySet = new ReadOnlySet<string>(innerSet);

        Assert.False(readOnlySet.Contains("d"));
    }

    [Fact]
    public void Count_ReflectsInnerSetCount()
    {
        var innerSet = new HashSet<int> { 10, 20, 30, 40 };
        var readOnlySet = new ReadOnlySet<int>(innerSet);

        Assert.Equal(4, readOnlySet.Count);
    }

    [Fact]
    public void GetEnumerator_EnumeratesAllItems()
    {
        var innerSet = new HashSet<int> { 1, 2, 3 };
        var readOnlySet = new ReadOnlySet<int>(innerSet);

        var items = readOnlySet.OrderBy(x => x).ToList();

        Assert.Equal(new[] { 1, 2, 3 }, items);
    }

    [Fact]
    public void GetEnumerator_NonGeneric_Works()
    {
        var innerSet = new HashSet<string> { "x", "y", "z" };
        var readOnlySet = new ReadOnlySet<string>(innerSet);

        var enumerator = ((System.Collections.IEnumerable)readOnlySet).GetEnumerator();
        var items = new List<string>();

        while (enumerator.MoveNext())
        {
            items.Add((string)enumerator.Current);
        }

        Assert.Equal(3, items.Count);
        Assert.Contains("x", items);
        Assert.Contains("y", items);
        Assert.Contains("z", items);
    }

    [Fact]
    public void Changes_InInnerSet_ReflectedInReadOnlySet()
    {
        var innerSet = new HashSet<int> { 1, 2 };
        var readOnlySet = new ReadOnlySet<int>(innerSet);

        Assert.Equal(2, readOnlySet.Count);

        innerSet.Add(3);

        Assert.Equal(3, readOnlySet.Count);
        Assert.True(readOnlySet.Contains(3));
    }

    [Fact]
    public void EmptySet_Works()
    {
        var innerSet = new HashSet<double>();
        var readOnlySet = new ReadOnlySet<double>(innerSet);

        Assert.Empty(readOnlySet);
        Assert.Equal(0, readOnlySet.Count);
    }
}
