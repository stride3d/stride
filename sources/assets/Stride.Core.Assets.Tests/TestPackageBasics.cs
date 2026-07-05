// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Core.Assets.Tests;

public class TestPackageBasics
{
    [Fact]
    public void TestConstructor()
    {
        var package = new Package();

        Assert.NotNull(package.Meta);
        Assert.NotNull(package.Assets);
        Assert.NotNull(package.Bundles);
        Assert.NotNull(package.TemporaryAssets);
        Assert.NotNull(package.TemplateFolders);
    }

    [Fact]
    public void TestMetaProperty()
    {
        var package = new Package();

        Assert.NotNull(package.Meta);
        package.Meta.Name = "TestPackage";
        Assert.Equal("TestPackage", package.Meta.Name);
    }

    [Fact]
    public void TestBundlesCollection()
    {
        var package = new Package();
        var bundle = new Bundle { Name = "TestBundle" };

        package.Bundles.Add(bundle);

        Assert.Single(package.Bundles);
        Assert.Contains(bundle, package.Bundles);
    }

    [Fact]
    public void TestTemplateFolders()
    {
        var package = new Package();
        var templateFolder = new TemplateFolder("Templates");

        package.TemplateFolders.Add(templateFolder);

        Assert.Single(package.TemplateFolders);
        Assert.Contains(templateFolder, package.TemplateFolders);
    }

    [Fact]
    public void TestFullPathProperty()
    {
        var package = new Package();
        var path = new UFile("/Projects/MyPackage/MyPackage.sdpkg");

        package.FullPath = path;

        Assert.Equal(path, package.FullPath);
    }

    [Fact]
    public void TestDefaultFullPathIsNull()
    {
        var package = new Package();

        Assert.Null(package.FullPath);
    }

    [Fact]
    public void TestIsDirtyProperty()
    {
        var package = new Package();

        // Initially dirty after construction
        Assert.True(package.IsDirty);

        // Clear dirty flag
        package.IsDirty = false;
        Assert.False(package.IsDirty);
    }

    [Fact]
    public void TestPackageDirtyChangedEvent()
    {
        var package = new Package();
        var eventRaised = false;
        bool? oldValue = null;
        bool? newValue = null;

        package.PackageDirtyChanged += (sender, oldV, newV) =>
        {
            eventRaised = true;
            oldValue = oldV;
            newValue = newV;
        };

        package.IsDirty = false; // Set to false first
        package.IsDirty = true;  // Then change to true to trigger event

        Assert.True(eventRaised);
        Assert.False(oldValue!.Value);
        Assert.True(newValue!.Value);
    }

    [Fact]
    public void TestMetaVersionProperty()
    {
        var package = new Package();
        var version = new PackageVersion("1.2.3");

        package.Meta.Version = version;

        Assert.Equal(version, package.Meta.Version);
    }

    [Fact]
    public void TestMetaAuthorsProperty()
    {
        var package = new Package();
        var authors = new List<string> { "Author1", "Author2" };

        package.Meta.Authors.AddRange(authors);

        Assert.Equal(2, package.Meta.Authors.Count);
        Assert.Contains("Author1", package.Meta.Authors);
        Assert.Contains("Author2", package.Meta.Authors);
    }

    [Fact]
    public void TestMetaDependencies()
    {
        var package = new Package();

        // PackageMeta does not initialize Dependencies in its constructor, so it is null
        // on a freshly created Package (only assigned during serialization).
        Assert.Null(package.Meta.Dependencies);
    }

    [Fact]
    public void TestRootAssetsCollection()
    {
        var package = new Package();
        var assetReference = new AssetReference(AssetId.New(), "Assets/Test.sdobj");

        package.RootAssets.Add(assetReference);

        Assert.Single(package.RootAssets);
        Assert.Contains(assetReference, package.RootAssets);
    }
}
