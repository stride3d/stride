// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Xunit;

namespace Stride.Core.Assets.Tests;

public class TestAssetCompositeHierarchyData
{
    [Fact]
    public void TestDefaultConstructor()
    {
        var data = new AssetCompositeHierarchyData<TestPartDesign, TestPart>();

        Assert.NotNull(data);
        Assert.NotNull(data.RootParts);
        Assert.NotNull(data.Parts);
        Assert.Empty(data.RootParts);
        Assert.Empty(data.Parts);
    }

    [Fact]
    public void TestRootPartsProperty()
    {
        var data = new AssetCompositeHierarchyData<TestPartDesign, TestPart>();

        Assert.NotNull(data.RootParts);
        Assert.Empty(data.RootParts);
    }

    [Fact]
    public void TestPartsProperty()
    {
        var data = new AssetCompositeHierarchyData<TestPartDesign, TestPart>();

        Assert.NotNull(data.Parts);
        Assert.Empty(data.Parts);
    }

    [Fact]
    public void TestAddRootParts()
    {
        var data = new AssetCompositeHierarchyData<TestPartDesign, TestPart>();

        var part1 = new TestPart { Id = Guid.NewGuid() };
        var part2 = new TestPart { Id = Guid.NewGuid() };

        data.RootParts.Add(part1);
        data.RootParts.Add(part2);

        Assert.Equal(2, data.RootParts.Count);
        Assert.Contains(part1, data.RootParts);
        Assert.Contains(part2, data.RootParts);
    }

    [Fact]
    public void TestAddToParts()
    {
        var data = new AssetCompositeHierarchyData<TestPartDesign, TestPart>();

        var id = Guid.NewGuid();
        var part = new TestPart { Id = id };
        var design = new TestPartDesign { Part = part };

        data.Parts.Add(design);

        Assert.Single(data.Parts);
        Assert.True(data.Parts.ContainsKey(id));
    }

    [Fact]
    public void TestClearRootParts()
    {
        var data = new AssetCompositeHierarchyData<TestPartDesign, TestPart>();

        data.RootParts.Add(new TestPart { Id = Guid.NewGuid() });
        data.RootParts.Add(new TestPart { Id = Guid.NewGuid() });

        data.RootParts.Clear();

        Assert.Empty(data.RootParts);
    }

    [Fact]
    public void TestClearParts()
    {
        var data = new AssetCompositeHierarchyData<TestPartDesign, TestPart>();

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        data.Parts.Add(new TestPartDesign { Part = new TestPart { Id = id1 } });
        data.Parts.Add(new TestPartDesign { Part = new TestPart { Id = id2 } });

        data.Parts.Clear();

        Assert.Empty(data.Parts);
    }

    [Fact]
    public void TestRemoveFromRootParts()
    {
        var part = new TestPart { Id = Guid.NewGuid() };
        var data = new AssetCompositeHierarchyData<TestPartDesign, TestPart>();
        data.RootParts.Add(part);

        data.RootParts.Remove(part);

        Assert.Empty(data.RootParts);
    }

    [Fact]
    public void TestRemoveFromParts()
    {
        var id = Guid.NewGuid();
        var part = new TestPart { Id = id };
        var design = new TestPartDesign { Part = part };
        var data = new AssetCompositeHierarchyData<TestPartDesign, TestPart>();

        data.Parts.Add(design);
        data.Parts.Remove(id);

        Assert.Empty(data.Parts);
    }
}
