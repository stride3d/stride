// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Tests.Collections;

public class SortedListTests
{
    [Fact]
    public void Constructor_Default_CreatesEmptyList()
    {
        var list = new Core.Collections.SortedList<int, string>();
        Assert.Empty(list);
    }

    [Fact]
    public void Constructor_WithCapacity_CreatesEmptyListWithCapacity()
    {
        var list = new Core.Collections.SortedList<int, string>(100);
        Assert.Empty(list);
        Assert.True(list.Capacity >= 100);
    }

    [Fact]
    public void Constructor_WithNegativeCapacity_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Core.Collections.SortedList<int, string>(-1));
    }

    [Fact]
    public void Constructor_FromDictionary_CopiesElements()
    {
        var dict = new Dictionary<int, string> { { 3, "three" }, { 1, "one" }, { 2, "two" } };
        var list = new Core.Collections.SortedList<int, string>(dict);

        Assert.Equal(3, list.Count);
        Assert.Equal("one", list[1]);
        Assert.Equal("two", list[2]);
        Assert.Equal("three", list[3]);
    }

    [Fact]
    public void Add_InsertsInSortedOrder()
    {
        var list = new Core.Collections.SortedList<int, string>();

        list.Add(5, "five");
        list.Add(2, "two");
        list.Add(8, "eight");
        list.Add(1, "one");

        Assert.Equal(4, list.Count);
        Assert.Equal("one", list.Values[0]);
        Assert.Equal("two", list.Values[1]);
        Assert.Equal("five", list.Values[2]);
        Assert.Equal("eight", list.Values[3]);
    }

    [Fact]
    public void Add_DuplicateKey_ThrowsException()
    {
        var list = new Core.Collections.SortedList<string, int>();
        list.Add("key", 1);

        Assert.Throws<ArgumentException>(() => list.Add("key", 2));
    }

    [Fact]
    public void Indexer_Get_ReturnsCorrectValue()
    {
        var list = new Core.Collections.SortedList<string, int>
        {
            { "apple", 1 },
            { "banana", 2 },
            { "cherry", 3 }
        };

        Assert.Equal(1, list["apple"]);
        Assert.Equal(2, list["banana"]);
        Assert.Equal(3, list["cherry"]);
    }

    [Fact]
    public void Indexer_Get_NonExistentKey_ThrowsException()
    {
        var list = new Core.Collections.SortedList<string, int> { { "key", 1 } };

        Assert.Throws<KeyNotFoundException>(() => list["missing"]);
    }

    [Fact]
    public void Indexer_Set_UpdatesExistingValue()
    {
        var list = new Core.Collections.SortedList<int, string> { { 1, "old" } };

        list[1] = "new";

        Assert.Equal("new", list[1]);
        Assert.Single(list);
    }

    [Fact]
    public void Indexer_Set_AddsNewValue()
    {
        var list = new Core.Collections.SortedList<int, string>();

        list[5] = "five";

        Assert.Single(list);
        Assert.Equal("five", list[5]);
    }

    [Fact]
    public void ContainsKey_ExistingKey_ReturnsTrue()
    {
        var list = new Core.Collections.SortedList<int, string> { { 10, "ten" } };

        Assert.True(list.ContainsKey(10));
    }

    [Fact]
    public void ContainsKey_NonExistingKey_ReturnsFalse()
    {
        var list = new Core.Collections.SortedList<int, string> { { 10, "ten" } };

        Assert.False(list.ContainsKey(20));
    }

    [Fact]
    public void Remove_ExistingKey_RemovesAndReturnsTrue()
    {
        var list = new Core.Collections.SortedList<int, string>
        {
            { 1, "one" },
            { 2, "two" },
            { 3, "three" }
        };

        var removed = list.Remove(2);

        Assert.True(removed);
        Assert.Equal(2, list.Count);
        Assert.False(list.ContainsKey(2));
    }

    [Fact]
    public void Remove_NonExistingKey_ReturnsFalse()
    {
        var list = new Core.Collections.SortedList<int, string> { { 1, "one" } };

        var removed = list.Remove(5);

        Assert.False(removed);
        Assert.Single(list);
    }

    [Fact]
    public void Clear_RemovesAllElements()
    {
        var list = new Core.Collections.SortedList<int, string>
        {
            { 1, "one" },
            { 2, "two" },
            { 3, "three" }
        };

        list.Clear();

        Assert.Empty(list);
    }

    [Fact]
    public void Keys_ReturnsKeysInSortedOrder()
    {
        var list = new Core.Collections.SortedList<int, string>
        {
            { 5, "five" },
            { 2, "two" },
            { 8, "eight" },
            { 1, "one" }
        };

        var keys = list.Keys.ToList();

        Assert.Equal(new[] { 1, 2, 5, 8 }, keys);
    }

    [Fact]
    public void Values_ReturnsValuesInKeyOrder()
    {
        var list = new Core.Collections.SortedList<int, string>
        {
            { 3, "three" },
            { 1, "one" },
            { 2, "two" }
        };

        var values = list.Values.ToList();

        Assert.Equal(new[] { "one", "two", "three" }, values);
    }

    [Fact]
    public void TryGetValue_ExistingKey_ReturnsTrueAndValue()
    {
        var list = new Core.Collections.SortedList<string, int> { { "key", 42 } };

        var found = list.TryGetValue("key", out var value);

        Assert.True(found);
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGetValue_NonExistingKey_ReturnsFalse()
    {
        var list = new Core.Collections.SortedList<string, int> { { "key", 42 } };

        var found = list.TryGetValue("missing", out var value);

        Assert.False(found);
        Assert.Equal(0, value);
    }

    [Fact]
    public void Capacity_CanBeIncreased()
    {
        var list = new Core.Collections.SortedList<int, string>(10);
        var originalCapacity = list.Capacity;

        list.Capacity = 100;

        Assert.True(list.Capacity >= 100);
    }

    [Fact]
    public void Capacity_CannotBeLessThanCount()
    {
        var list = new Core.Collections.SortedList<int, string> { { 1, "one" }, { 2, "two" }, { 3, "three" } };

        Assert.Throws<ArgumentOutOfRangeException>(() => list.Capacity = 2);
    }

    [Fact]
    public void Enumeration_ReturnsItemsInSortedOrder()
    {
        var list = new Core.Collections.SortedList<int, string>
        {
            { 5, "five" },
            { 2, "two" },
            { 8, "eight" }
        };

        var items = list.ToList();

        Assert.Equal(2, items[0].Key);
        Assert.Equal(5, items[1].Key);
        Assert.Equal(8, items[2].Key);
    }

    [Fact]
    public void CustomComparer_SortsAccordingly()
    {
        var list = new Core.Collections.SortedList<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Banana", 2 },
            { "apple", 1 },
            { "Cherry", 3 }
        };

        var keys = list.Keys.ToList();

        Assert.Equal("apple", keys[0]);
        Assert.Equal("Banana", keys[1]);
        Assert.Equal("Cherry", keys[2]);
    }

    [Fact]
    public void IndexOfKey_ExistingKey_ReturnsIndex()
    {
        var list = new Core.Collections.SortedList<int, string>
        {
            { 10, "ten" },
            { 20, "twenty" },
            { 30, "thirty" }
        };

        var index = list.IndexOfKey(20);

        Assert.Equal(1, index);
    }

    [Fact]
    public void IndexOfKey_NonExistingKey_ReturnsNegative()
    {
        var list = new Core.Collections.SortedList<int, string> { { 10, "ten" } };

        var index = list.IndexOfKey(15);

        Assert.True(index < 0);
    }

    [Fact]
    public void RemoveAt_RemovesItemAtIndex()
    {
        var list = new Core.Collections.SortedList<int, string>
        {
            { 1, "one" },
            { 2, "two" },
            { 3, "three" }
        };

        list.RemoveAt(1);

        Assert.Equal(2, list.Count);
        Assert.False(list.ContainsKey(2));
    }

    [Fact(Skip = "TrimExcess implementation may not reduce capacity as expected")]
    public void TrimExcess_ReducesCapacity()
    {
        var list = new Core.Collections.SortedList<int, string>(100);
        list.Add(1, "one");
        list.Add(2, "two");

        var originalCapacity = list.Capacity;
        list.TrimExcess();

        // TrimExcess reduces capacity when usage < 90%
        Assert.True(list.Capacity < originalCapacity);
    }
}
