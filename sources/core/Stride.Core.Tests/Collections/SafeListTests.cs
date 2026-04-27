// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Collections;

namespace Stride.Core.Tests.Collections;

public class SafeListTests
{
    [Fact]
    public void Constructor_CreatesEmptyList()
    {
        var list = new SafeList<string>();

        Assert.Empty(list);
        Assert.True(list.ThrowException);
    }

    [Fact]
    public void Add_WithNonNullItem_AddsSuccessfully()
    {
        var list = new SafeList<string>();

        list.Add("test");
        list.Add("another");

        Assert.Equal(2, list.Count);
        Assert.Contains("test", list);
    }

    [Fact]
    public void Add_WithNullItem_ThrowsException()
    {
        var list = new SafeList<string>();

        var ex = Assert.Throws<ArgumentException>(() => list.Add(null!));
        Assert.Contains("cannot be null", ex.Message);
    }

    [Fact]
    public void Insert_WithNonNullItem_InsertsSuccessfully()
    {
        var list = new SafeList<string> { "first", "third" };

        list.Insert(1, "second");

        Assert.Equal(3, list.Count);
        Assert.Equal("second", list[1]);
    }

    [Fact]
    public void Insert_WithNullItem_ThrowsException()
    {
        var list = new SafeList<string> { "first" };

        Assert.Throws<ArgumentException>(() => list.Insert(0, null!));
    }

    [Fact]
    public void Indexer_Set_WithNonNullItem_UpdatesValue()
    {
        var list = new SafeList<object> { new object() };
        var newObj = new object();

        list[0] = newObj;

        Assert.Same(newObj, list[0]);
    }

    [Fact]
    public void Indexer_Set_WithNullItem_ThrowsException()
    {
        var list = new SafeList<string> { "test" };

        Assert.Throws<ArgumentException>(() => list[0] = null!);
    }

    [Fact]
    public void SafeList_WorksWithReferenceTypes()
    {
        var list = new SafeList<object>();
        var obj1 = new object();
        var obj2 = new object();

        list.Add(obj1);
        list.Add(obj2);

        Assert.Equal(2, list.Count);
        Assert.Contains(obj1, list);
        Assert.Contains(obj2, list);
    }
}
