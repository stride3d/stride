// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Tests;

public class TestPropertyCollection
{
    private static readonly PropertyKey<string> TestKey1 = new PropertyKey<string>("Test.Key1", typeof(TestPropertyCollection));
    private static readonly PropertyKey<int> TestKey2 = new PropertyKey<int>("Test.Key2", typeof(TestPropertyCollection));
    private static readonly PropertyKey<bool> TestKey3 = new PropertyKey<bool>("Test.Key3", typeof(TestPropertyCollection));

    [Fact]
    public void TestConstructorDefault()
    {
        var collection = new PropertyCollection();

        Assert.Empty(collection);
    }

    [Fact]
    public void TestConstructorWithDictionary()
    {
        var initial = new Dictionary<PropertyKey, object>
        {
            { TestKey1, "Value1" },
            { TestKey2, 42 }
        };

        var collection = new PropertyCollection(initial);

        Assert.Equal(2, collection.Count);
        Assert.Equal("Value1", collection[TestKey1]);
        Assert.Equal(42, collection[TestKey2]);
    }

    [Fact]
    public void TestGetMethod()
    {
        var collection = new PropertyCollection();
        collection[TestKey1] = "TestValue";

        var value = collection.Get(TestKey1);

        Assert.Equal("TestValue", value);
    }

    [Fact]
    public void TestGetMethodNotFound()
    {
        var collection = new PropertyCollection();

        var value = collection.Get(TestKey1);

        Assert.Null(value);
    }

    [Fact]
    public void TestGetGenericMethod()
    {
        var collection = new PropertyCollection();
        collection[TestKey2] = 123;

        var value = collection.Get(TestKey2);

        Assert.Equal(123, value);
    }

    [Fact]
    public void TestGetGenericMethodNotFound()
    {
        var collection = new PropertyCollection();

        var value = collection.Get(TestKey2);

        Assert.Equal(0, value); // Default value for int
    }

    [Fact]
    public void TestTryGetMethod()
    {
        var collection = new PropertyCollection();
        collection[TestKey1] = "FoundValue";

        var found = collection.TryGet(TestKey1, out var value);

        Assert.True(found);
        Assert.Equal("FoundValue", value);
    }

    [Fact]
    public void TestTryGetMethodNotFound()
    {
        var collection = new PropertyCollection();

        var found = collection.TryGet(TestKey1, out var value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void TestSetMethod()
    {
        var collection = new PropertyCollection();

        collection.Set(TestKey1, "NewValue");

        Assert.Equal("NewValue", collection[TestKey1]);
    }

    [Fact]
    public void TestSetGenericMethod()
    {
        var collection = new PropertyCollection();

        collection.Set(TestKey3, true);

        Assert.True((bool)collection[TestKey3]);
    }

    [Fact]
    public void TestSetMethodOverride()
    {
        var collection = new PropertyCollection();
        collection[TestKey2] = 10;

        collection.Set(TestKey2, 20);

        Assert.Equal(20, collection[TestKey2]);
    }

    [Fact]
    public void TestCopyToMethod()
    {
        var collection = new PropertyCollection();
        collection[TestKey1] = "Value1";
        collection[TestKey2] = 42;

        var target = new Dictionary<PropertyKey, object>();
        collection.CopyTo(target, false);

        Assert.Equal(2, target.Count);
        Assert.Equal("Value1", target[TestKey1]);
        Assert.Equal(42, target[TestKey2]);
    }

    [Fact]
    public void TestCopyToMethodWithOverride()
    {
        var collection = new PropertyCollection();
        collection[TestKey1] = "NewValue";
        collection[TestKey2] = 100;

        var target = new Dictionary<PropertyKey, object>
        {
            { TestKey1, "OldValue" },
            { TestKey2, 50 }
        };

        collection.CopyTo(target, true);

        Assert.Equal("NewValue", target[TestKey1]);
        Assert.Equal(100, target[TestKey2]);
    }

    [Fact]
    public void TestCopyToMethodWithoutOverride()
    {
        var collection = new PropertyCollection();
        collection[TestKey1] = "NewValue";

        var target = new Dictionary<PropertyKey, object>
        {
            { TestKey1, "OldValue" }
        };

        collection.CopyTo(target, false);

        // Without override, existing values should remain
        Assert.Equal("OldValue", target[TestKey1]);
    }

    [Fact]
    public void TestCopyToMethodNullThrows()
    {
        var collection = new PropertyCollection();

        Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null!, false));
    }

    [Fact]
    public void TestMultiplePropertiesCanBeStored()
    {
        var collection = new PropertyCollection();

        collection.Set(TestKey1, "String");
        collection.Set(TestKey2, 42);
        collection.Set(TestKey3, true);

        Assert.Equal(3, collection.Count);
        Assert.Equal("String", collection.Get(TestKey1));
        Assert.Equal(42, collection.Get(TestKey2));
        Assert.True(collection.Get(TestKey3));
    }
}
