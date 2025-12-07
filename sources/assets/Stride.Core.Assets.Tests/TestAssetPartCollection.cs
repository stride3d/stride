// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Xunit;

namespace Stride.Core.Assets.Tests;

public class TestAssetPartCollection
{
    [Fact]
    public void TestConstructor()
    {
        var collection = new AssetPartCollection<TestPartDesign, TestPart>();

        Assert.NotNull(collection);
        Assert.Empty(collection);
    }

    [Fact]
    public void TestAddWithPart()
    {
        var collection = new AssetPartCollection<TestPartDesign, TestPart>();
        var partId = Guid.NewGuid();
        var part = new TestPart { Id = partId };
        var design = new TestPartDesign { Part = part };

        collection.Add(design);

        Assert.Single(collection);
        Assert.True(collection.ContainsKey(partId));
        Assert.Equal(design, collection[partId]);
    }

    [Fact]
    public void TestAddWithNullPartThrows()
    {
        var collection = new AssetPartCollection<TestPartDesign, TestPart>();

        Assert.Throws<ArgumentNullException>(() => collection.Add(null!));
    }

    [Fact]
    public void TestAddWithKeyValuePair()
    {
        var collection = new AssetPartCollection<TestPartDesign, TestPart>();
        var partId = Guid.NewGuid();
        var part = new TestPart { Id = partId };
        var design = new TestPartDesign { Part = part };
        var kvp = new KeyValuePair<Guid, TestPartDesign>(partId, design);

        collection.Add(kvp);

        Assert.Single(collection);
        Assert.Equal(design, collection[partId]);
    }

    [Fact]
    public void TestAddWithMismatchedKeyThrows()
    {
        var collection = new AssetPartCollection<TestPartDesign, TestPart>();
        var partId = Guid.NewGuid();
        var wrongKey = Guid.NewGuid();
        var part = new TestPart { Id = partId };
        var design = new TestPartDesign { Part = part };
        var kvp = new KeyValuePair<Guid, TestPartDesign>(wrongKey, design);

        Assert.Throws<ArgumentException>(() => collection.Add(kvp));
    }

    [Fact]
    public void TestAddWithNullValueInKeyValuePairThrows()
    {
        var collection = new AssetPartCollection<TestPartDesign, TestPart>();
        var kvp = new KeyValuePair<Guid, TestPartDesign>(Guid.NewGuid(), null!);

        Assert.Throws<ArgumentNullException>(() => collection.Add(kvp));
    }

    [Fact]
    public void TestRefreshKeys()
    {
        var collection = new AssetPartCollection<TestPartDesign, TestPart>();
        var oldId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        var part = new TestPart { Id = oldId };
        var design = new TestPartDesign { Part = part };

        collection.Add(design);
        Assert.True(collection.ContainsKey(oldId));

        // Change the part ID
        part.Id = newId;

        // Keys should still be old until refresh
        Assert.True(collection.ContainsKey(oldId));
        Assert.False(collection.ContainsKey(newId));

        // Refresh keys
        collection.RefreshKeys();

        // Now new key should be present
        Assert.False(collection.ContainsKey(oldId));
        Assert.True(collection.ContainsKey(newId));
        Assert.Equal(design, collection[newId]);
    }

    [Fact]
    public void TestRefreshKeysWithMultipleItems()
    {
        var collection = new AssetPartCollection<TestPartDesign, TestPart>();

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var part1 = new TestPart { Id = id1 };
        var part2 = new TestPart { Id = id2 };
        var design1 = new TestPartDesign { Part = part1 };
        var design2 = new TestPartDesign { Part = part2 };

        collection.Add(design1);
        collection.Add(design2);

        Assert.Equal(2, collection.Count);

        // Change both IDs
        var newId1 = Guid.NewGuid();
        var newId2 = Guid.NewGuid();
        part1.Id = newId1;
        part2.Id = newId2;

        collection.RefreshKeys();

        Assert.Equal(2, collection.Count);
        Assert.True(collection.ContainsKey(newId1));
        Assert.True(collection.ContainsKey(newId2));
        Assert.False(collection.ContainsKey(id1));
        Assert.False(collection.ContainsKey(id2));
    }

    [Fact]
    public void TestRemove()
    {
        var collection = new AssetPartCollection<TestPartDesign, TestPart>();
        var partId = Guid.NewGuid();
        var part = new TestPart { Id = partId };
        var design = new TestPartDesign { Part = part };

        collection.Add(design);
        Assert.Single(collection);

        var removed = collection.Remove(partId);

        Assert.True(removed);
        Assert.Empty(collection);
    }

    [Fact]
    public void TestClear()
    {
        var collection = new AssetPartCollection<TestPartDesign, TestPart>();

        collection.Add(new TestPartDesign { Part = new TestPart { Id = Guid.NewGuid() } });
        collection.Add(new TestPartDesign { Part = new TestPart { Id = Guid.NewGuid() } });
        collection.Add(new TestPartDesign { Part = new TestPart { Id = Guid.NewGuid() } });

        Assert.Equal(3, collection.Count);

        collection.Clear();

        Assert.Empty(collection);
    }

    [Fact]
    public void TestIndexer()
    {
        var collection = new AssetPartCollection<TestPartDesign, TestPart>();
        var partId = Guid.NewGuid();
        var part = new TestPart { Id = partId };
        var design = new TestPartDesign { Part = part };

        collection.Add(design);

        var retrieved = collection[partId];

        Assert.Equal(design, retrieved);
        Assert.Same(part, retrieved.Part);
    }

    [Fact]
    public void TestEnumeration()
    {
        var collection = new AssetPartCollection<TestPartDesign, TestPart>();

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var design1 = new TestPartDesign { Part = new TestPart { Id = id1 } };
        var design2 = new TestPartDesign { Part = new TestPart { Id = id2 } };

        collection.Add(design1);
        collection.Add(design2);

        var enumerated = collection.ToList();

        Assert.Equal(2, enumerated.Count);
        Assert.Contains(enumerated, kvp => kvp.Key == id1 && kvp.Value == design1);
        Assert.Contains(enumerated, kvp => kvp.Key == id2 && kvp.Value == design2);
    }
}

// Test helper classes implementing the required interfaces
[DataContract]
public class TestPart : IIdentifiable
{
    public Guid Id { get; set; }
}

[DataContract]
public class TestPartDesign : IAssetPartDesign<TestPart>
{
    public TestPart Part { get; set; } = new TestPart();

    // Explicit interface implementation for non-generic interface
    IIdentifiable IAssetPartDesign.Part => Part;

    public BasePart? Base { get; set; }
}
