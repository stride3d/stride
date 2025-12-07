// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Xunit;

namespace Stride.Core.Assets.Tests;

public class TestPackageStore
{
    [Fact]
    public void TestFindLocalPackageWithNullName()
    {
#pragma warning disable CS8625
        Assert.Throws<ArgumentNullException>(() => PackageStore.Instance.FindLocalPackage(null));
#pragma warning restore CS8625
    }

    [Fact]
    public void TestFindLocalPackageWithUnknownPackage()
    {
        var result = PackageStore.Instance.FindLocalPackage("NonExistentPackage99999", new PackageVersionRange(new PackageVersion("1.0.0")));

        Assert.Null(result);
    }

    [Fact]
    public void TestFindLocalPackageWithNullVersion()
    {
        var result = PackageStore.Instance.FindLocalPackage("TestPackage", null);

        // Should handle null version gracefully
        Assert.Null(result);
    }

    [Fact]
    public void TestDefaultPackageStore()
    {
        var defaultStore = PackageStore.Instance;

        Assert.NotNull(defaultStore);
        Assert.Same(PackageStore.Instance, PackageStore.Instance); // Should be singleton
    }
}
