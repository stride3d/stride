// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Collections;

namespace Stride.Core.Tests;

public class TestDeque
{
    [Fact]
    public void TestAddAndRemove()
    {
        var deque = new Deque<int>(8);
        for (int i = 0; i < 8; ++i)
            deque.AddToBack(i);

        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, deque.ToArray());

        for (int i = 0; i < 4; ++i)
            deque.RemoveFromFront();

        Assert.Equal(new[] { 4, 5, 6, 7 }, deque.ToArray());

        // Wrapping past end
        for (int i = 0; i < 4; ++i)
            deque.AddToBack(i);

        Assert.Equal(new[] { 4, 5, 6, 7, 0, 1, 2, 3 }, deque.ToArray());
        Assert.Equal(8, deque.Capacity); // We should be right

        for (int i = 0; i < 2; ++i)
            deque.RemoveFromBack();

        Assert.Equal(new[] { 4, 5, 6, 7, 0, 1 }, deque.ToArray());
    }

    [Fact]
    public void TestRangeInsertion()
    {
        var deque = new Deque<int>(8);
        deque.AddToFront(1010);
        deque.InsertRange(0, [0, 1, 2, 3, 4, 5, 6]);

        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5, 6, 1010 }, deque.ToArray());
        Assert.Equal(8, deque.Capacity);
        
        // Inserting past the end of the buffer
        deque.RemoveRange(0, 7);
        deque.InsertRange(1, [0, 1, 2, 3]);
        
        Assert.Equal(new[] { 1010, 0, 1, 2, 3 }, deque.ToArray());
        Assert.Equal(8, deque.Capacity);

        deque.InsertRange(0, [0, 1, 2, 3]);
        
        Assert.Equal(new[] { 0, 1, 2, 3, 1010, 0, 1, 2, 3 }, deque.ToArray());
        Assert.Equal(16, deque.Capacity);
    }

    [Fact]
    public void TestBinarySearch()
    {
        var deque = new Deque<int>(8);
        deque.InsertRange(0, [0, 1, 2, 3, 4, 5, 6, 7]);

        Assert.Equal(5, deque.BinarySearch(5));
        
        // Inserting past the end of the buffer to test binary search on a split buffer
        deque.RemoveRange(0, 4);
        deque.InsertRange(4, [8, 9, 10, 11]);

        Assert.Equal(8, deque.Capacity);
        Assert.Equal(new[] { 4, 5, 6, 7, 8, 9, 10, 11 }, deque.ToArray());

        Assert.Equal(5, deque.BinarySearch(9));

        // Test for sorted insertion
        deque.RemoveAt(5);
        deque.Insert(~deque.BinarySearch(9), 9);

        Assert.Equal(new[] { 4, 5, 6, 7, 8, 9, 10, 11 }, deque.ToArray());

        // ... at buffer end
        deque.RemoveAt(3);
        deque.Insert(~deque.BinarySearch(7), 7);

        Assert.Equal(new[] { 4, 5, 6, 7, 8, 9, 10, 11 }, deque.ToArray());

        // ... at buffer start
        deque.RemoveAt(4);
        deque.Insert(~deque.BinarySearch(8), 8);

        Assert.Equal(new[] { 4, 5, 6, 7, 8, 9, 10, 11 }, deque.ToArray());

        // ... on the last element
        deque.RemoveAt(7);
        deque.Insert(~deque.BinarySearch(11), 11);

        Assert.Equal(new[] { 4, 5, 6, 7, 8, 9, 10, 11 }, deque.ToArray());

        // ... on the first element
        deque.RemoveAt(0);
        deque.Insert(~deque.BinarySearch(3), 3);

        Assert.Equal(new[] { 3, 5, 6, 7, 8, 9, 10, 11 }, deque.ToArray());
    }
}
