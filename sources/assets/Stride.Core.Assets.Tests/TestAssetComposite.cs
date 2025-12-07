// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Tests;

public class TestAssetComposite
{
    [Fact]
    public void TestFindPart()
    {
        var asset = new TestAssetWithParts { Name = "Test" };
        var part = new AssetPartTestItem { Id = Guid.NewGuid() };
        asset.Parts.Add(part);

        var found = asset.FindPart(part.Id);

        Assert.NotNull(found);
        Assert.Same(part, found);
    }

    [Fact]
    public void TestFindPartNotFound()
    {
        var asset = new TestAssetWithParts { Name = "Test" };

        var found = asset.FindPart(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public void TestContainsPart()
    {
        var asset = new TestAssetWithParts { Name = "Test" };
        var part = new AssetPartTestItem { Id = Guid.NewGuid() };
        asset.Parts.Add(part);

        Assert.True(asset.ContainsPart(part.Id));
        Assert.False(asset.ContainsPart(Guid.NewGuid()));
    }

    [Fact]
    public void TestCollectParts()
    {
        var asset = new TestAssetWithParts { Name = "Test" };
        var part1 = new AssetPartTestItem { Id = Guid.NewGuid() };
        var part2 = new AssetPartTestItem { Id = Guid.NewGuid() };
        asset.Parts.Add(part1);
        asset.Parts.Add(part2);

#pragma warning disable CS0618 // Type or member is obsolete
        var parts = asset.CollectParts().ToList();
#pragma warning restore CS0618

        Assert.Equal(2, parts.Count);
    }

    [Fact]
    public void TestCompositeHierarchy()
    {
        var rootAsset = new TestAssetWithParts { Name = "Root" };
        var part = new AssetPartTestItem { Id = Guid.NewGuid() };
        rootAsset.Parts.Add(part);

        var derivedAsset = (TestAssetWithParts)rootAsset.CreateDerivedAsset("Base/Root.sdpart", out var idRemapping);

        Assert.NotNull(derivedAsset);
        Assert.Single(derivedAsset.Parts);
    }
}
