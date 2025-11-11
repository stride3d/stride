// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestGuillotinePacker
{
    [Fact]
    public void TestGuillotinePackerInitialization()
    {
        var packer = new GuillotinePacker();
        Assert.Equal(0, packer.Width);
        Assert.Equal(0, packer.Height);
    }

    [Fact]
    public void TestGuillotinePackerClear()
    {
        var packer = new GuillotinePacker();
        packer.Clear(100, 100);

        Assert.Equal(100, packer.Width);
        Assert.Equal(100, packer.Height);
    }

    [Fact]
    public void TestGuillotinePackerClearResetsWithSameSize()
    {
        var packer = new GuillotinePacker();
        packer.Clear(100, 100);

        // Insert a rectangle
        var rect = new Rectangle();
        packer.Insert(50, 50, ref rect);

        // Clear should reset to empty state
        packer.Clear();

        Assert.Equal(100, packer.Width);
        Assert.Equal(100, packer.Height);

        // Should be able to insert same rectangle again
        var rect2 = new Rectangle();
        Assert.True(packer.Insert(50, 50, ref rect2));
        Assert.Equal(0, rect2.X);
        Assert.Equal(0, rect2.Y);
    }

    [Fact]
    public void TestGuillotinePackerInsertPerfectFit()
    {
        var packer = new GuillotinePacker();
        packer.Clear(100, 100);

        var rect = new Rectangle();
        Assert.True(packer.Insert(100, 100, ref rect));
        Assert.Equal(0, rect.X);
        Assert.Equal(0, rect.Y);
        Assert.Equal(100, rect.Width);
        Assert.Equal(100, rect.Height);
    }

    [Fact]
    public void TestGuillotinePackerInsertSingleRectangle()
    {
        var packer = new GuillotinePacker();
        packer.Clear(100, 100);

        var rect = new Rectangle();
        Assert.True(packer.Insert(50, 50, ref rect));
        Assert.Equal(0, rect.X);
        Assert.Equal(0, rect.Y);
        Assert.Equal(50, rect.Width);
        Assert.Equal(50, rect.Height);
    }

    [Fact]
    public void TestGuillotinePackerInsertMultipleRectangles()
    {
        var packer = new GuillotinePacker();
        packer.Clear(100, 100);

        var rect1 = new Rectangle();
        Assert.True(packer.Insert(50, 50, ref rect1));

        var rect2 = new Rectangle();
        Assert.True(packer.Insert(50, 50, ref rect2));

        // Second rectangle should be placed adjacent
        Assert.True((rect2.X == 50 && rect2.Y == 0) || (rect2.X == 0 && rect2.Y == 50));
    }

    [Fact]
    public void TestGuillotinePackerInsertTooLarge()
    {
        var packer = new GuillotinePacker();
        packer.Clear(100, 100);

        var rect = new Rectangle();
        Assert.False(packer.Insert(150, 150, ref rect));
    }

    [Fact]
    public void TestGuillotinePackerInsertWhenFull()
    {
        var packer = new GuillotinePacker();
        packer.Clear(100, 100);

        // Fill the packer
        var rect1 = new Rectangle();
        packer.Insert(100, 100, ref rect1);

        // Try to insert another rectangle
        var rect2 = new Rectangle();
        Assert.False(packer.Insert(10, 10, ref rect2));
    }

    [Fact]
    public void TestGuillotinePackerFree()
    {
        var packer = new GuillotinePacker();
        packer.Clear(100, 100);

        var rect = new Rectangle();
        packer.Insert(50, 50, ref rect);

        // Free the rectangle
        packer.Free(ref rect);

        // Should be able to insert a rectangle of the same size again
        // Note: Free just adds the rectangle back to the free list,
        // but doesn't guarantee the exact same placement
        var rect2 = new Rectangle();
        Assert.True(packer.Insert(50, 50, ref rect2));
    }

    [Fact]
    public void TestGuillotinePackerTryInsertSuccess()
    {
        var packer = new GuillotinePacker();
        packer.Clear(200, 200);

        var rectangles = new List<Rectangle>();
        bool success = packer.TryInsert(50, 50, 4, (index, ref rect) =>
        {
            rectangles.Add(rect);
        });

        Assert.True(success);
        Assert.Equal(4, rectangles.Count);

        // All rectangles should have the correct size
        foreach (var rect in rectangles)
        {
            Assert.Equal(50, rect.Width);
            Assert.Equal(50, rect.Height);
        }
    }

    [Fact]
    public void TestGuillotinePackerTryInsertFailure()
    {
        var packer = new GuillotinePacker();
        packer.Clear(100, 100);

        var rectangles = new List<Rectangle>();
        bool success = packer.TryInsert(60, 60, 4, (index, ref rect) =>
        {
            rectangles.Add(rect);
        });

        Assert.False(success);
        // The callback is invoked for each successful insertion until failure occurs
        Assert.NotEmpty(rectangles);
    }

    [Fact]
    public void TestGuillotinePackerTryInsertRollback()
    {
        var packer = new GuillotinePacker();
        packer.Clear(100, 100);

        // First, insert a rectangle successfully
        var rect1 = new Rectangle();
        packer.Insert(50, 50, ref rect1);

        // Try to insert 4 rectangles (should fail)
        var rectangles = new List<Rectangle>();
        bool success = packer.TryInsert(50, 50, 4, (index, ref rect) =>
        {
            rectangles.Add(rect);
        });

        Assert.False(success);

        // The packer should rollback the state, so we should still be able
        // to insert the original remaining space
        var rect2 = new Rectangle();
        Assert.True(packer.Insert(50, 50, ref rect2));
    }

    [Fact]
    public void TestGuillotinePackerBestAreaFit()
    {
        var packer = new GuillotinePacker();
        packer.Clear(100, 100);

        // Insert first rectangle
        var rect1 = new Rectangle();
        packer.Insert(50, 100, ref rect1);

        // Insert small rectangle - should use best area fit
        var rect2 = new Rectangle();
        packer.Insert(10, 10, ref rect2);

        // Should be placed in the 50x100 free space
        Assert.Equal(50, rect2.X);
        Assert.Equal(0, rect2.Y);
    }

    [Fact]
    public void TestGuillotinePackerSplitHorizontal()
    {
        var packer = new GuillotinePacker();
        packer.Clear(100, 100);

        // Insert a wide rectangle to trigger horizontal split
        var rect = new Rectangle();
        packer.Insert(80, 30, ref rect);

        // Verify the rectangle was inserted
        Assert.Equal(80, rect.Width);
        Assert.Equal(30, rect.Height);

        // Insert another rectangle to verify space is available
        var rect2 = new Rectangle();
        Assert.True(packer.Insert(20, 20, ref rect2));
    }

    [Fact]
    public void TestGuillotinePackerSplitVertical()
    {
        var packer = new GuillotinePacker();
        packer.Clear(100, 100);

        // Insert a tall rectangle to trigger vertical split
        var rect = new Rectangle();
        packer.Insert(30, 80, ref rect);

        // Verify the rectangle was inserted
        Assert.Equal(30, rect.Width);
        Assert.Equal(80, rect.Height);

        // Insert another rectangle to verify space is available
        var rect2 = new Rectangle();
        Assert.True(packer.Insert(20, 20, ref rect2));
    }

    [Fact]
    public void TestGuillotinePackerCallbackIndexes()
    {
        var packer = new GuillotinePacker();
        packer.Clear(200, 200);

        var indexes = new List<int>();
        packer.TryInsert(40, 40, 5, (index, ref rect) =>
        {
            indexes.Add(index);
        });

        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, indexes);
    }

    [Fact]
    public void TestGuillotinePackerZeroSizeInsert()
    {
        var packer = new GuillotinePacker();
        packer.Clear(100, 100);

        var rect = new Rectangle();
        // The algorithm allows zero-sized rectangles (width=0 or height=0)
        Assert.True(packer.Insert(0, 0, ref rect));
        Assert.Equal(0, rect.Width);
        Assert.Equal(0, rect.Height);
    }

    [Fact]
    public void TestGuillotinePackerSmallInLargeSpace()
    {
        var packer = new GuillotinePacker();
        packer.Clear(1000, 1000);

        var rect = new Rectangle();
        Assert.True(packer.Insert(10, 10, ref rect));
        Assert.Equal(0, rect.X);
        Assert.Equal(0, rect.Y);
    }
}
