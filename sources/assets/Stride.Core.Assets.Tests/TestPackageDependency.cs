// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Tests;

public class TestPackageDependency
{
    [Fact]
    public void TestDefaultConstructor()
    {
        var dependency = new PackageDependency();

        Assert.Null(dependency.Name);
        Assert.Null(dependency.Version);
    }

    [Fact]
    public void TestConstructorWithNameAndVersion()
    {
        var version = new PackageVersionRange(new PackageVersion("1.0.0"));
        var dependency = new PackageDependency("TestPackage", version);

        Assert.Equal("TestPackage", dependency.Name);
        Assert.Equal(version, dependency.Version);
    }

    [Fact]
    public void TestNameProperty()
    {
        var dependency = new PackageDependency { Name = "MyPackage" };

        Assert.Equal("MyPackage", dependency.Name);
    }

    [Fact]
    public void TestVersionProperty()
    {
        var version = new PackageVersionRange(new PackageVersion("2.5.0"));
        var dependency = new PackageDependency { Version = version };

        Assert.Equal(version, dependency.Version);
    }

    [Fact]
    public void TestClone()
    {
        var version = new PackageVersionRange(new PackageVersion("1.2.3"));
        var dependency = new PackageDependency("OriginalPackage", version);

        var cloned = dependency.Clone();

        Assert.NotSame(dependency, cloned);
        Assert.Equal(dependency.Name, cloned.Name);
        Assert.Equal(dependency.Version, cloned.Version);
    }

    [Fact]
    public void TestEqualityWithSameValues()
    {
        var version = new PackageVersionRange(new PackageVersion("1.0.0"));
        var dep1 = new PackageDependency("TestPackage", version);
        var dep2 = new PackageDependency("TestPackage", version);

        Assert.True(dep1.Equals(dep2));
        Assert.True(dep1 == dep2);
    }

    [Fact]
    public void TestInequalityWithDifferentNames()
    {
        var version = new PackageVersionRange(new PackageVersion("1.0.0"));
        var dep1 = new PackageDependency("Package1", version);
        var dep2 = new PackageDependency("Package2", version);

        Assert.False(dep1.Equals(dep2));
        Assert.True(dep1 != dep2);
    }

    [Fact]
    public void TestInequalityWithDifferentVersions()
    {
        var version1 = new PackageVersionRange(new PackageVersion("1.0.0"));
        var version2 = new PackageVersionRange(new PackageVersion("2.0.0"));
        var dep1 = new PackageDependency("TestPackage", version1);
        var dep2 = new PackageDependency("TestPackage", version2);

        Assert.False(dep1.Equals(dep2));
    }

    [Fact]
    public void TestEqualityWithNull()
    {
        var dependency = new PackageDependency("TestPackage", new PackageVersionRange(new PackageVersion("1.0.0")));

        Assert.False(dependency.Equals(null));
#pragma warning disable CS8625
        Assert.True(dependency != null);
#pragma warning restore CS8625
    }

    [Fact]
    public void TestEqualityWithSameReference()
    {
        var dependency = new PackageDependency("TestPackage", new PackageVersionRange(new PackageVersion("1.0.0")));

        Assert.True(dependency.Equals(dependency));
#pragma warning disable CS1718
        Assert.True(dependency == dependency);
#pragma warning restore CS1718
    }

    [Fact]
    public void TestGetHashCode()
    {
        var version = new PackageVersionRange(new PackageVersion("1.0.0"));
        var dep1 = new PackageDependency("TestPackage", version);
        var dep2 = new PackageDependency("TestPackage", version);

        // Equal dependencies must produce equal hash codes
        Assert.Equal(dep1, dep2);
        Assert.Equal(dep1.GetHashCode(), dep2.GetHashCode());
    }

    [Fact]
    public void TestGetHashCodeWithNullName()
    {
        var dependency = new PackageDependency();

        var hash = dependency.GetHashCode();
        Assert.Equal(0, hash);
    }
}

public class TestPackageDependencyCollection
{
    [Fact]
    public void TestConstructor()
    {
        var collection = new PackageDependencyCollection();

        Assert.Empty(collection);
    }

    [Fact]
    public void TestAdd()
    {
        var collection = new PackageDependencyCollection();
        var dep1 = new PackageDependency("Package1", new PackageVersionRange(new PackageVersion("1.0.0")));
        var dep2 = new PackageDependency("Package2", new PackageVersionRange(new PackageVersion("2.0.0")));

        collection.Add(dep1);
        collection.Add(dep2);

        Assert.Equal(2, collection.Count);
        Assert.Contains(dep1, collection);
        Assert.Contains(dep2, collection);

        // Enumeration yields all added dependencies
        var list = collection.ToList();
        Assert.Equal(2, list.Count);
        Assert.Contains(dep1, list);
        Assert.Contains(dep2, list);
    }

    [Fact]
    public void TestRemove()
    {
        var collection = new PackageDependencyCollection();
        var dependency = new PackageDependency("TestPackage", new PackageVersionRange(new PackageVersion("1.0.0")));

        collection.Add(dependency);
        var removed = collection.Remove(dependency);

        Assert.True(removed);
        Assert.Empty(collection);
    }

    [Fact]
    public void TestIndexByKey()
    {
        var collection = new PackageDependencyCollection();
        var dependency = new PackageDependency("TestPackage", new PackageVersionRange(new PackageVersion("1.0.0")));

        collection.Add(dependency);

        Assert.Equal(dependency, collection["TestPackage"]);
    }

    [Fact]
    public void TestContainsKey()
    {
        var collection = new PackageDependencyCollection();
        var dependency = new PackageDependency("TestPackage", new PackageVersionRange(new PackageVersion("1.0.0")));

        collection.Add(dependency);

        Assert.True(collection.Contains("TestPackage"));
        Assert.False(collection.Contains("NonExistent"));
    }

    [Fact]
    public void TestClear()
    {
        var collection = new PackageDependencyCollection();
        collection.Add(new PackageDependency("Package1", new PackageVersionRange(new PackageVersion("1.0.0"))));
        collection.Add(new PackageDependency("Package2", new PackageVersionRange(new PackageVersion("2.0.0"))));

        collection.Clear();

        Assert.Empty(collection);
    }
}
