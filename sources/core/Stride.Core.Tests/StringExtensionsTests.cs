// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Xunit;

namespace Stride.Core.Tests;

public class StringExtensionsTests
{
    [Fact]
    public void SafeTrim_WithNormalString_TrimsWhitespace()
    {
        var input = "  test  ";
        var result = input.SafeTrim();

        Assert.Equal("test", result);
    }

    [Fact]
    public void SafeTrim_WithNullString_ReturnsNull()
    {
        string? input = null;
        var result = input.SafeTrim();

        Assert.Null(result);
    }

    [Fact]
    public void SafeTrim_WithEmptyString_ReturnsEmpty()
    {
        var input = "";
        var result = input.SafeTrim();

        Assert.Equal("", result);
    }

    [Fact]
    public void SafeTrim_WithWhitespaceOnly_ReturnsEmpty()
    {
        var input = "   ";
        var result = input.SafeTrim();

        Assert.Equal("", result);
    }

    [Fact]
    public void IndexOf_WithCharFound_ReturnsIndex()
    {
        var builder = new StringBuilder("hello world");
        var index = builder.IndexOf('w');

        Assert.Equal(6, index);
    }

    [Fact]
    public void IndexOf_WithCharNotFound_ReturnsNegativeOne()
    {
        var builder = new StringBuilder("hello world");
        var index = builder.IndexOf('x');

        Assert.Equal(-1, index);
    }

    [Fact]
    public void IndexOf_WithEmptyBuilder_ReturnsNegativeOne()
    {
        var builder = new StringBuilder();
        var index = builder.IndexOf('x');

        Assert.Equal(-1, index);
    }

    [Fact]
    public void IndexOf_WithNullBuilder_ThrowsArgumentNullException()
    {
        StringBuilder? builder = null;
        Assert.Throws<ArgumentNullException>(() => builder!.IndexOf('x'));
    }

    [Fact]
    public void IndexOf_WithCharAtStart_ReturnsZero()
    {
        var builder = new StringBuilder("hello");
        var index = builder.IndexOf('h');

        Assert.Equal(0, index);
    }

    [Fact]
    public void IndexOf_WithCharAtEnd_ReturnsLastIndex()
    {
        var builder = new StringBuilder("hello");
        var index = builder.IndexOf('o');

        Assert.Equal(4, index);
    }

    [Fact]
    public void LastIndexOf_WithCharFound_ReturnsLastIndex()
    {
        var builder = new StringBuilder("hello world");
        var index = builder.LastIndexOf('o');

        Assert.Equal(7, index);
    }

    [Fact]
    public void LastIndexOf_WithCharNotFound_ReturnsNegativeOne()
    {
        var builder = new StringBuilder("hello world");
        var index = builder.LastIndexOf('x');

        Assert.Equal(-1, index);
    }

    [Fact]
    public void LastIndexOf_WithEmptyBuilder_ReturnsNegativeOne()
    {
        var builder = new StringBuilder();
        var index = builder.LastIndexOf('x');

        Assert.Equal(-1, index);
    }

    [Fact]
    public void LastIndexOf_WithNullBuilder_ThrowsArgumentNullException()
    {
        StringBuilder? builder = null;
        Assert.Throws<ArgumentNullException>(() => builder!.LastIndexOf('x'));
    }

    [Fact]
    public void Substring_WithValidRange_ReturnsSubstring()
    {
        var builder = new StringBuilder("hello world");
        var result = builder.Substring(6, 5);

        Assert.Equal("world", result);
    }

    [Fact]
    public void Substring_WithZeroLength_ReturnsEmptyString()
    {
        var builder = new StringBuilder("hello");
        var result = builder.Substring(0, 0);

        Assert.Equal("", result);
    }

    [Fact]
    public void Substring_WithNullBuilder_ThrowsNullReferenceException()
    {
        // NOTE: Current implementation throws NullReferenceException instead of ArgumentNullException
        StringBuilder? builder = null;
        Assert.Throws<NullReferenceException>(() => builder!.Substring(0, 1));
    }

    [Fact]
    public void Replace_WithValidParameters_ReplacesSubstring()
    {
        var builder = new StringBuilder("hello world");
        builder.Replace("world", "there", 0, builder.Length);

        Assert.Equal("hello there", builder.ToString());
    }

    [Fact]
    public void Replace_WithNullBuilder_ThrowsNullReferenceException()
    {
        // NOTE: Current implementation throws NullReferenceException instead of ArgumentNullException
        StringBuilder? builder = null;
        Assert.Throws<NullReferenceException>(() => builder!.Replace("old", "new", 0, 1));
    }
}
