// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;

namespace Stride.Core.Assets.Editor.Common.Tests;

public class TestAbstractNodeValue
{
    [Fact]
    public void NullEntrySetsNullAndSortsFirst()
    {
        var entry = AbstractNodeValue.Null;
        Assert.Null(entry.Value);
        Assert.Null(entry.GenerateValue(new object()));
        Assert.Equal("None", entry.DisplayValue);
        Assert.True(entry.Order < 0);
        Assert.True(entry.IsMatchingValue(null));
    }

    [Fact]
    public void EntriesWithEqualValuesAreEqual()
    {
        var first = new AbstractNodeValue("value", "First", 1);
        var second = new AbstractNodeValue("value", "Second", 2);
        var other = new AbstractNodeValue("other", "First", 1);

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, other);
        Assert.True(first != other);
        Assert.False(first.Equals((object?)null));
    }

    [Fact]
    public void MatchingUsesReferenceEquality()
    {
        var value = new object();
        var entry = new AbstractNodeValue(value, "Value", 0);

        Assert.True(entry.IsMatchingValue(value));
        Assert.False(entry.IsMatchingValue(new object()));
        Assert.Same(value, entry.GenerateValue(null!));
    }

    [Fact]
    public void DisplayValueIsRequired()
    {
        Assert.Throws<ArgumentNullException>(() => new AbstractNodeValue(1, null!, 0));
    }
}
