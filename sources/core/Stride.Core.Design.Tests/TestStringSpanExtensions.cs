// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Design.Tests;

/// <summary>
/// Tests for the <see cref="StringSpanExtensions"/> class.
/// </summary>
public class TestStringSpanExtensions
{
    [Fact]
    public void Substring_WithValidSpan_ShouldReturnCorrectSubstring()
    {
        const string text = "Hello, World!";
        var span = new StringSpan(0, 5);

        var result = text.Substring(span);

        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Substring_WithSpanInMiddle_ShouldReturnCorrectSubstring()
    {
        const string text = "Hello, World!";
        var span = new StringSpan(7, 5);

        var result = text.Substring(span);

        Assert.Equal("World", result);
    }

    [Fact]
    public void Substring_WithSpanAtEnd_ShouldReturnCorrectSubstring()
    {
        const string text = "Hello, World!";
        var span = new StringSpan(12, 1);

        var result = text.Substring(span);

        Assert.Equal("!", result);
    }

    [Fact]
    public void Substring_WithFullStringSpan_ShouldReturnFullString()
    {
        const string text = "Test";
        var span = new StringSpan(0, 4);

        var result = text.Substring(span);

        Assert.Equal("Test", result);
    }

    [Fact]
    public void Substring_WithInvalidSpan_ShouldReturnNull()
    {
        const string text = "Hello, World!";
        var invalidSpan = new StringSpan(-1, 5);

        var result = text.Substring(invalidSpan);

        Assert.Null(result);
    }

    [Fact]
    public void Substring_WithZeroLengthSpan_ShouldReturnNull()
    {
        const string text = "Hello, World!";
        var emptySpan = new StringSpan(5, 0);

        var result = text.Substring(emptySpan);

        Assert.Null(result);
    }

    [Fact]
    public void Substring_WithNegativeLengthSpan_ShouldReturnNull()
    {
        const string text = "Hello, World!";
        var invalidSpan = new StringSpan(0, -1);

        var result = text.Substring(invalidSpan);

        Assert.Null(result);
    }

    [Fact]
    public void Substring_WithEmptyString_AndValidSpan_ShouldThrowArgumentOutOfRangeException()
    {
        const string text = "";
        var span = new StringSpan(0, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => text.Substring(span));
    }
}
