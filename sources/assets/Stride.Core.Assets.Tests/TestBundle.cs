// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Selectors;

namespace Stride.Core.Assets.Tests;

public class TestBundle
{
    [Fact]
    public void TestConstructor()
    {
        var bundle = new Bundle();

        Assert.NotNull(bundle.Selectors);
        Assert.Empty(bundle.Selectors);
        Assert.NotNull(bundle.Dependencies);
        Assert.Empty(bundle.Dependencies);
    }

    [Fact]
    public void TestNameProperty()
    {
        var bundle = new Bundle { Name = "MainBundle" };

        Assert.Equal("MainBundle", bundle.Name);
    }

    [Fact]
    public void TestSelectorsCollection()
    {
        var bundle = new Bundle();
        var selector = new PathSelector();

        bundle.Selectors.Add(selector);

        Assert.Single(bundle.Selectors);
        Assert.Contains(selector, bundle.Selectors);
    }

    [Fact]
    public void TestDependenciesCollection()
    {
        var bundle = new Bundle();

        bundle.Dependencies.Add("Dependency1");
        bundle.Dependencies.Add("Dependency2");

        Assert.Equal(2, bundle.Dependencies.Count);
        Assert.Contains("Dependency1", bundle.Dependencies);
        Assert.Contains("Dependency2", bundle.Dependencies);
    }

    [Fact]
    public void TestBundleOutputGroup()
    {
        var bundle = new Bundle { OutputGroup = "RuntimeData" };

        Assert.Equal("RuntimeData", bundle.OutputGroup);
    }

    [Fact]
    public void TestBundleOutputGroupDefaultValue()
    {
        var bundle = new Bundle();

        // OutputGroup should have default value (null based on [DefaultValue(null)] attribute)
        Assert.Null(bundle.OutputGroup);
    }
}
