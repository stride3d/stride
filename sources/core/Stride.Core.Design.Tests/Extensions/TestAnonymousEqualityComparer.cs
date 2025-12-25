// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Xunit;

namespace Stride.Core.Design.Tests.Extensions;

/// <summary>
/// Tests for <see cref="AnonymousEqualityComparer{T}"/> class.
/// </summary>
public class TestAnonymousEqualityComparer
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithBothFunctions_CreatesComparer()
    {
        var comparer = new AnonymousEqualityComparer<int>(
            (x, y) => x == y,
            obj => obj.GetHashCode());

        Assert.NotNull(comparer);
    }

    [Fact]
    public void Constructor_WithNullEqualsFunction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AnonymousEqualityComparer<int>(null!, obj => obj.GetHashCode()));
    }

    [Fact]
    public void Constructor_WithNullGetHashCodeFunction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AnonymousEqualityComparer<int>((x, y) => x == y, null!));
    }

    [Fact]
    public void Constructor_WithOnlyEqualsFunction_UsesDefaultGetHashCode()
    {
        var comparer = new AnonymousEqualityComparer<int>((x, y) => x == y);

        // Should use default GetHashCode
        Assert.Equal(42.GetHashCode(), comparer.GetHashCode(42));
    }

    #endregion

    #region Equals Tests

    [Fact]
    public void Equals_WithMatchingValues_ReturnsTrue()
    {
        var comparer = new AnonymousEqualityComparer<string>(
            (x, y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase));

        Assert.True(comparer.Equals("Test", "test"));
    }

    [Fact]
    public void Equals_WithNonMatchingValues_ReturnsFalse()
    {
        var comparer = new AnonymousEqualityComparer<int>((x, y) => x == y);

        Assert.False(comparer.Equals(1, 2));
    }

    [Fact]
    public void Equals_WithNullValues_CallsCustomFunction()
    {
        bool equalsWasCalled = false;
        var comparer = new AnonymousEqualityComparer<string?>((x, y) =>
        {
            equalsWasCalled = true;
            return x == y;
        });

        Assert.True(comparer.Equals(null, null));
        Assert.True(equalsWasCalled);
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_WithCustomFunction_ReturnsCustomHashCode()
    {
        var comparer = new AnonymousEqualityComparer<string>(
            (x, y) => x == y,
            obj => obj?.Length ?? 0);

        Assert.Equal(5, comparer.GetHashCode("hello"));
        Assert.Equal(0, comparer.GetHashCode(""));
    }

    [Fact]
    public void GetHashCode_WithDefaultConstructor_UsesObjectGetHashCode()
    {
        var obj = "test";
        var comparer = new AnonymousEqualityComparer<string>((x, y) => x == y);

        Assert.Equal(obj.GetHashCode(), comparer.GetHashCode(obj));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AnonymousEqualityComparer_WithDictionary_WorksCorrectly()
    {
        var comparer = new AnonymousEqualityComparer<string>(
            (x, y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase),
            obj => obj.ToUpperInvariant().GetHashCode());

        var dict = new Dictionary<string, int>(comparer)
        {
            ["Test"] = 1
        };

        Assert.True(dict.ContainsKey("test"));
        Assert.True(dict.ContainsKey("TEST"));
        Assert.Equal(1, dict["TeSt"]);
    }

    [Fact]
    public void AnonymousEqualityComparer_WithCustomType_WorksCorrectly()
    {
        var comparer = new AnonymousEqualityComparer<Point>(
            (p1, p2) => p1.X == p2.X && p1.Y == p2.Y,
            p => HashCode.Combine(p.X, p.Y));

        var point1 = new Point(1, 2);
        var point2 = new Point(1, 2);
        var point3 = new Point(3, 4);

        Assert.True(comparer.Equals(point1, point2));
        Assert.False(comparer.Equals(point1, point3));
        Assert.Equal(comparer.GetHashCode(point1), comparer.GetHashCode(point2));
    }

    private record struct Point(int X, int Y);

    #endregion
}
