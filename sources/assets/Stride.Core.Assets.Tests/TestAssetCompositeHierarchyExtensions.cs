// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Xunit;

namespace Stride.Core.Assets.Tests;

public class TestAssetCompositeHierarchyExtensions
{
    [Fact]
    public void TestEnumerateRootPartDesigns()
    {
        var hierarchy = new TestHierarchy();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var part1 = new TestPart { Id = id1 };
        var part2 = new TestPart { Id = id2 };

        hierarchy.Hierarchy.RootParts.Add(part1);
        hierarchy.Hierarchy.RootParts.Add(part2);
        hierarchy.Hierarchy.Parts.Add(new TestPartDesign { Part = part1 });
        hierarchy.Hierarchy.Parts.Add(new TestPartDesign { Part = part2 });

        var designs = hierarchy.Hierarchy.EnumerateRootPartDesigns().ToList();

        Assert.Equal(2, designs.Count);
        Assert.All(designs, design => Assert.NotNull(design));
    }

    [Fact]
    public void TestEnumerateRootPartDesignsEmpty()
    {
        var hierarchy = new TestHierarchy();

        var designs = hierarchy.Hierarchy.EnumerateRootPartDesigns().ToList();

        Assert.Empty(designs);
    }

    [Fact]
    public void TestMergeInto()
    {
        var hierarchy1 = new TestHierarchy();
        var hierarchy2 = new TestHierarchy();

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var part1 = new TestPart { Id = id1 };
        var part2 = new TestPart { Id = id2 };

        hierarchy2.Hierarchy.RootParts.Add(part1);
        hierarchy2.Hierarchy.RootParts.Add(part2);
        hierarchy2.Hierarchy.Parts.Add(new TestPartDesign { Part = part1 });
        hierarchy2.Hierarchy.Parts.Add(new TestPartDesign { Part = part2 });

        hierarchy1.Hierarchy.MergeInto(hierarchy2.Hierarchy);

        Assert.Equal(2, hierarchy1.Hierarchy.RootParts.Count);
        Assert.Equal(2, hierarchy1.Hierarchy.Parts.Count);
    }

    [Fact]
    public void TestMergeIntoEmpty()
    {
        var hierarchy1 = new TestHierarchy();
        var hierarchy2 = new TestHierarchy();

        hierarchy1.Hierarchy.MergeInto(hierarchy2.Hierarchy);

        Assert.Empty(hierarchy1.Hierarchy.RootParts);
        Assert.Empty(hierarchy1.Hierarchy.Parts);
    }

    [Fact]
    public void TestMergeIntoWithExistingParts()
    {
        var hierarchy1 = new TestHierarchy();
        var hierarchy2 = new TestHierarchy();

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        var part1 = new TestPart { Id = id1 };
        var part2 = new TestPart { Id = id2 };
        var part3 = new TestPart { Id = id3 };

        hierarchy1.Hierarchy.RootParts.Add(part1);
        hierarchy1.Hierarchy.Parts.Add(new TestPartDesign { Part = part1 });

        hierarchy2.Hierarchy.RootParts.Add(part2);
        hierarchy2.Hierarchy.RootParts.Add(part3);
        hierarchy2.Hierarchy.Parts.Add(new TestPartDesign { Part = part2 });
        hierarchy2.Hierarchy.Parts.Add(new TestPartDesign { Part = part3 });

        hierarchy1.Hierarchy.MergeInto(hierarchy2.Hierarchy);

        Assert.Equal(3, hierarchy1.Hierarchy.RootParts.Count);
        Assert.Equal(3, hierarchy1.Hierarchy.Parts.Count);
    }
}

// Test implementation of AssetCompositeHierarchy
public class TestHierarchy : AssetCompositeHierarchy<TestPartDesign, TestPart>
{
    public TestHierarchy()
    {
        Hierarchy = new AssetCompositeHierarchyData<TestPartDesign, TestPart>();
    }

    public override TestPart GetParent(TestPart part)
    {
        return null!;
    }

    public override int IndexOf(TestPart part)
    {
        return Hierarchy.RootParts.IndexOf(part);
    }

    public override TestPart GetChild(TestPart part, int index)
    {
        return null!;
    }

    public override int GetChildCount(TestPart part)
    {
        return 0;
    }

    public override IEnumerable<TestPart> EnumerateChildParts(TestPart part, bool isRecursive)
    {
        yield break;
    }
}
