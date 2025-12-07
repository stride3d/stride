// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Xunit;

namespace Stride.Core.Assets.Tests;

public class TestPackageCollection
{
    [Fact]
    public void TestConstructor()
    {
        var collection = new PackageCollection();

        Assert.NotNull(collection);
        Assert.Empty(collection);
    }

    [Fact]
    public void TestAddPackage()
    {
        var collection = new PackageCollection();
        var package = new Package { Meta = { Name = "TestPackage", Version = new PackageVersion("1.0.0") } };

        collection.Add(package);

        Assert.Single(collection);
        Assert.Contains(package, collection);
    }

    [Fact]
    public void TestRemovePackage()
    {
        var collection = new PackageCollection();
        var package = new Package { Meta = { Name = "TestPackage", Version = new PackageVersion("1.0.0") } };

        collection.Add(package);
        var removed = collection.Remove(package);

        Assert.True(removed);
        Assert.Empty(collection);
    }

    [Fact]
    public void TestClearCollection()
    {
        var collection = new PackageCollection();
        collection.Add(new Package { Meta = { Name = "Package1", Version = new PackageVersion("1.0.0") } });
        collection.Add(new Package { Meta = { Name = "Package2", Version = new PackageVersion("1.0.0") } });

        collection.Clear();

        Assert.Empty(collection);
    }

    [Fact]
    public void TestCountProperty()
    {
        var collection = new PackageCollection();

        Assert.Empty(collection);

        collection.Add(new Package { Meta = { Name = "Package1", Version = new PackageVersion("1.0.0") } });
        Assert.Single(collection);

        collection.Add(new Package { Meta = { Name = "Package2", Version = new PackageVersion("1.0.0") } });
        Assert.Equal(2, collection.Count);
    }

    [Fact]
    public void TestContainsPackage()
    {
        var collection = new PackageCollection();
        var package = new Package { Meta = { Name = "TestPackage", Version = new PackageVersion("1.0.0") } };

        Assert.DoesNotContain(package, collection);

        collection.Add(package);
        Assert.Contains(package, collection);
    }

    [Fact]
    public void TestEnumeration()
    {
        var collection = new PackageCollection();
        var package1 = new Package { Meta = { Name = "Package1", Version = new PackageVersion("1.0.0") } };
        var package2 = new Package { Meta = { Name = "Package2", Version = new PackageVersion("1.0.0") } };

        collection.Add(package1);
        collection.Add(package2);

        var enumerated = collection.ToList();
        Assert.Equal(2, enumerated.Count);
        Assert.Contains(package1, enumerated);
        Assert.Contains(package2, enumerated);
    }

    [Fact]
    public void TestIsReadOnly()
    {
        var collection = new PackageCollection();

        Assert.False(collection.IsReadOnly);
    }

    [Fact]
    public void TestCollectionChangedEvent()
    {
        var collection = new PackageCollection();
        var eventRaised = false;

        collection.CollectionChanged += (sender, args) => eventRaised = true;

        collection.Add(new Package { Meta = { Name = "TestPackage", Version = new PackageVersion("1.0.0") } });

        Assert.True(eventRaised);
    }

    [Fact]
    public void TestFindByDependency()
    {
        var collection = new PackageCollection();
        var package = new Package { Meta = { Name = "TestPackage", Version = new PackageVersion("1.0.0") } };
        collection.Add(package);

        var dependency = new Dependency("TestPackage", new PackageVersion("1.0.0"), DependencyType.Package);
        var found = collection.Find(dependency);

        Assert.NotNull(found);
        Assert.Equal(package.Meta.Name, found.Meta.Name);
    }

    [Fact]
    public void TestFindByPackageDependency()
    {
        var collection = new PackageCollection();
        var package = new Package { Meta = { Name = "TestPackage", Version = new PackageVersion("1.0.0") } };
        collection.Add(package);

        var packageDep = new PackageDependency("TestPackage", new PackageVersionRange(new PackageVersion("1.0.0")));
        var found = collection.Find(packageDep);

        Assert.NotNull(found);
        Assert.Equal(package.Meta.Name, found.Meta.Name);
    }

    [Fact]
    public void TestFindByNameAndVersionRange()
    {
        var collection = new PackageCollection();
        var package = new Package { Meta = { Name = "TestPackage", Version = new PackageVersion("1.5.0") } };
        collection.Add(package);

        // Use exact version range that includes the package version
        var versionRange = new PackageVersionRange(new PackageVersion("1.5.0"));
        var found = collection.Find("TestPackage", versionRange);

        Assert.NotNull(found);
        Assert.Equal(package, found);
    }

    [Fact]
    public void TestFindReturnsNullWhenNotFound()
    {
        var collection = new PackageCollection();

        var dependency = new Dependency("NonExistent", new PackageVersion("1.0.0"), DependencyType.Package);
        var found = collection.Find(dependency);

        Assert.Null(found);
    }
}
