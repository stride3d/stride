// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Collections;
using Xunit;

namespace Stride.Core.Tests.Collections;

public class PoolListStructTests
{
    private class TestObject
    {
        public int Value { get; set; }
    }

    [Fact]
    public void Constructor_RequiresFactory()
    {
        Assert.Throws<ArgumentNullException>(() => new PoolListStruct<TestObject>(10, null!));
    }

    [Fact]
    public void Constructor_InitializesEmptyPool()
    {
        var pool = new PoolListStruct<TestObject>(5, () => new TestObject());

        Assert.Equal(0, pool.Count);
    }

    [Fact]
    public void Add_AllocatesNewObject_WhenPoolEmpty()
    {
        var allocCount = 0;
        var pool = new PoolListStruct<TestObject>(5, () =>
        {
            allocCount++;
            return new TestObject { Value = allocCount };
        });

        var obj1 = pool.Add();
        var obj2 = pool.Add();

        Assert.Equal(2, pool.Count);
        Assert.Equal(2, allocCount);
        Assert.Equal(1, obj1.Value);
        Assert.Equal(2, obj2.Value);
    }

    [Fact]
    public void Add_ReusesPooledObjects_AfterClear()
    {
        var allocCount = 0;
        var pool = new PoolListStruct<TestObject>(5, () =>
        {
            allocCount++;
            return new TestObject();
        });

        var obj1 = pool.Add();
        var obj2 = pool.Add();
        pool.Clear();

        var obj3 = pool.Add();
        var obj4 = pool.Add();

        Assert.Equal(2, pool.Count);
        Assert.Equal(2, allocCount); // No new allocations
        Assert.Same(obj1, obj3);
        Assert.Same(obj2, obj4);
    }

    [Fact]
    public void Clear_ResetsCount()
    {
        var pool = new PoolListStruct<TestObject>(5, () => new TestObject());

        pool.Add();
        pool.Add();
        pool.Add();

        Assert.Equal(3, pool.Count);

        pool.Clear();

        Assert.Equal(0, pool.Count);
    }

    [Fact]
    public void Reset_ClearsAllocatedObjects()
    {
        var pool = new PoolListStruct<TestObject>(5, () => new TestObject());

        pool.Add();
        pool.Add();

        pool.Reset();

        Assert.Equal(0, pool.Count);

        // Next add should allocate new object
        var obj = pool.Add();
        Assert.NotNull(obj);
    }

    [Fact]
    public void Indexer_AccessesObjects()
    {
        var pool = new PoolListStruct<TestObject>(5, () => new TestObject());

        var obj1 = pool.Add();
        var obj2 = pool.Add();

        obj1.Value = 10;
        obj2.Value = 20;

        Assert.Equal(10, pool[0].Value);
        Assert.Equal(20, pool[1].Value);
    }

    [Fact]
    public void Indexer_Set_ModifiesObject()
    {
        var pool = new PoolListStruct<TestObject>(5, () => new TestObject());

        pool.Add();
        var newObj = new TestObject { Value = 100 };

        pool[0] = newObj;

        Assert.Equal(100, pool[0].Value);
    }

    [Fact]
    public void IndexOf_ReturnsCorrectIndex()
    {
        var pool = new PoolListStruct<TestObject>(5, () => new TestObject());

        var obj1 = pool.Add();
        var obj2 = pool.Add();
        var obj3 = pool.Add();

        Assert.Equal(0, pool.IndexOf(obj1));
        Assert.Equal(1, pool.IndexOf(obj2));
        Assert.Equal(2, pool.IndexOf(obj3));
    }

    [Fact]
    public void IndexOf_NotFound_ReturnsMinusOne()
    {
        var pool = new PoolListStruct<TestObject>(5, () => new TestObject());

        pool.Add();
        var notInPool = new TestObject();

        Assert.Equal(-1, pool.IndexOf(notInPool));
    }

    [Fact]
    public void RemoveAt_RemovesItem()
    {
        var pool = new PoolListStruct<TestObject>(5, () => new TestObject());

        var obj1 = pool.Add();
        var obj2 = pool.Add();
        var obj3 = pool.Add();

        pool.RemoveAt(1);

        Assert.Equal(2, pool.Count);
        Assert.Equal(obj1, pool[0]);
        Assert.Equal(obj3, pool[1]);
    }

    [Fact]
    public void RemoveAt_InvalidIndex_ThrowsException()
    {
        var pool = new PoolListStruct<TestObject>(5, () => new TestObject());

        pool.Add();

        Assert.Throws<ArgumentOutOfRangeException>(() => pool.RemoveAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => pool.RemoveAt(5));
    }

    [Fact]
    public void Remove_RemovesSpecificItem()
    {
        var pool = new PoolListStruct<TestObject>(5, () => new TestObject());

        var obj1 = pool.Add();
        var obj2 = pool.Add();
        var obj3 = pool.Add();

        pool.Remove(obj2);

        Assert.Equal(2, pool.Count);
        Assert.Contains(obj1, pool);
        Assert.Contains(obj3, pool);
    }

    [Fact]
    public void Remove_NotInPool_ThrowsException()
    {
        var pool = new PoolListStruct<TestObject>(5, () => new TestObject());

        pool.Add();
        var notInPool = new TestObject();

        Assert.Throws<InvalidOperationException>(() => pool.Remove(notInPool));
    }

    [Fact]
    public void GetEnumerator_EnumeratesActiveItems()
    {
        var pool = new PoolListStruct<TestObject>(5, () => new TestObject());

        var obj1 = pool.Add();
        var obj2 = pool.Add();
        var obj3 = pool.Add();

        obj1.Value = 1;
        obj2.Value = 2;
        obj3.Value = 3;

        var values = pool.Select(o => o.Value).ToList();

        Assert.Equal(new[] { 1, 2, 3 }, values);
    }

    [Fact]
    public void PoolBehavior_ReuseAfterRemove()
    {
        var allocCount = 0;
        var pool = new PoolListStruct<TestObject>(5, () =>
        {
            allocCount++;
            return new TestObject();
        });

        var obj1 = pool.Add();
        var obj2 = pool.Add();
        pool.RemoveAt(1);

        var obj3 = pool.Add();

        Assert.Equal(2, allocCount); // obj2 should be reused
        Assert.Equal(2, pool.Count);
    }
}
