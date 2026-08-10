// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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
}
