// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Design.Tests;

/// <summary>
/// Tests for the <see cref="StringSpan"/> struct.
/// </summary>
public class TestStringSpan
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        var span = new StringSpan(5, 10);

        Assert.Equal(5, span.Start);
        Assert.Equal(10, span.Length);
    }

    [Fact]
    public void IsValid_WithPositiveValues_ShouldReturnTrue()
    {
        var span = new StringSpan(0, 1);
        Assert.True(span.IsValid);

        span = new StringSpan(5, 10);
        Assert.True(span.IsValid);
    }

    [Fact]
    public void IsValid_WithNegativeStart_ShouldReturnFalse()
    {
        var span = new StringSpan(-1, 10);
        Assert.False(span.IsValid);
    }

    [Fact]
    public void IsValid_WithZeroLength_ShouldReturnFalse()
    {
        var span = new StringSpan(5, 0);
        Assert.False(span.IsValid);
    }

    [Fact]
    public void IsValid_WithNegativeLength_ShouldReturnFalse()
    {
        var span = new StringSpan(5, -1);
        Assert.False(span.IsValid);
    }

    [Fact]
    public void Next_ShouldReturnStartPlusLength()
    {
        var span = new StringSpan(5, 10);
        Assert.Equal(15, span.Next);

        span = new StringSpan(0, 5);
        Assert.Equal(5, span.Next);
    }

    [Fact]
    public void End_ShouldReturnStartPlusLengthMinusOne()
    {
        var span = new StringSpan(5, 10);
        Assert.Equal(14, span.End);

        span = new StringSpan(0, 5);
        Assert.Equal(4, span.End);

        span = new StringSpan(10, 1);
        Assert.Equal(10, span.End);
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        var span1 = new StringSpan(5, 10);
        var span2 = new StringSpan(5, 10);

        Assert.True(span1.Equals(span2));
        Assert.True(span1 == span2);
        Assert.False(span1 != span2);
    }

    [Fact]
    public void Equals_WithDifferentStart_ShouldReturnFalse()
    {
        var span1 = new StringSpan(5, 10);
        var span2 = new StringSpan(6, 10);

        Assert.False(span1.Equals(span2));
        Assert.False(span1 == span2);
        Assert.True(span1 != span2);
    }

    [Fact]
    public void Equals_WithDifferentLength_ShouldReturnFalse()
    {
        var span1 = new StringSpan(5, 10);
        var span2 = new StringSpan(5, 11);

        Assert.False(span1.Equals(span2));
        Assert.False(span1 == span2);
        Assert.True(span1 != span2);
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        var span = new StringSpan(5, 10);
        Assert.False(span.Equals(null));
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        var span = new StringSpan(5, 10);
        Assert.False(span.Equals("not a StringSpan"));
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldBeEqual()
    {
        var span1 = new StringSpan(5, 10);
        var span2 = new StringSpan(5, 10);

        Assert.Equal(span1.GetHashCode(), span2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ShouldBeDifferent()
    {
        var span1 = new StringSpan(5, 10);
        var span2 = new StringSpan(6, 10);

        Assert.NotEqual(span1.GetHashCode(), span2.GetHashCode());
    }

    [Fact]
    public void ToString_WithValidSpan_ShouldReturnFormattedString()
    {
        var span = new StringSpan(5, 10);
        var result = span.ToString();

        Assert.Equal("[5-14]", result);
    }

    [Fact]
    public void ToString_WithInvalidSpan_ShouldReturnNotApplicable()
    {
        var span = new StringSpan(-1, 10);
        var result = span.ToString();

        Assert.Equal("[N/A]", result);

        span = new StringSpan(5, 0);
        result = span.ToString();

        Assert.Equal("[N/A]", result);
    }
}
