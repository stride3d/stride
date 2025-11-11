// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Xunit;

namespace Stride.Core.Design.Tests.Extensions;

/// <summary>
/// Tests for <see cref="ObjectExtensions"/> class.
/// </summary>
public class TestObjectExtensions
{
    #region ToStringSafe Tests

    [Fact]
    public void ToStringSafe_WithValidObject_ReturnsToStringValue()
    {
        var obj = "test string";
        Assert.Equal("test string", obj.ToStringSafe());
    }

    [Fact]
    public void ToStringSafe_WithNull_ReturnsNullIndicator()
    {
        object? obj = null;
        Assert.Equal("(null)", obj.ToStringSafe());
    }

    [Fact]
    public void ToStringSafe_WithExceptionThrowingToString_ReturnsExceptionIndicator()
    {
        var obj = new ThrowingToString();
        Assert.Equal("(ExceptionInToString)", obj.ToStringSafe());
    }

    private class ThrowingToString
    {
        public override string ToString()
        {
            throw new InvalidOperationException("ToString failed");
        }
    }

    #endregion

    #region Yield Tests

    [Fact]
    public void Yield_WithValue_ReturnsSingleElementEnumerable()
    {
        var obj = 42;
        var result = obj.Yield();
        Assert.Single(result);
        Assert.Equal(42, result.First());
    }

    [Fact]
    public void Yield_WithNull_ReturnsSingleNullElement()
    {
        int? obj = null;
        var result = obj.Yield();
        Assert.Single(result);
        Assert.Null(result.First());
    }

    #endregion

    #region ToEnumerable Tests

    [Fact]
    public void ToEnumerable_WithEnumerableOfT_ReturnsSameEnumerable()
    {
        var list = new List<int> { 1, 2, 3 };
        var result = list.ToEnumerable<int>();
        Assert.Same(list, result);
    }

    [Fact]
    public void ToEnumerable_WithNonGenericEnumerable_ReturnsOfTypeResult()
    {
        var arrayList = new System.Collections.ArrayList { 1, 2, "skip", 3 };
        var result = arrayList.ToEnumerable<int>();
        Assert.Equal([1, 2, 3], result);
    }

    [Fact]
    public void ToEnumerable_WithNull_ReturnsYieldedNull()
    {
        object? obj = null;
        var result = obj.ToEnumerable<string>();
        Assert.Single(result);
        Assert.Null(result.First());
    }

    [Fact]
    public void ToEnumerable_WithMatchingType_ReturnsYieldedObject()
    {
        var obj = 42;
        var result = obj.ToEnumerable<int>();
        Assert.Single(result);
        Assert.Equal(42, result.First());
    }

    [Fact]
    public void ToEnumerable_WithNonMatchingType_ReturnsEmpty()
    {
        var obj = "string value";
        var result = obj.ToEnumerable<int>();
        Assert.Empty(result);
    }

    #endregion

    #region SafeArgument Tests

    [Fact]
    public void SafeArgument_WithNonNull_ReturnsSameObject()
    {
        var obj = "test";
        var result = obj.SafeArgument();
        Assert.Same(obj, result);
    }

    [Fact]
    public void SafeArgument_WithNull_ThrowsArgumentNullException()
    {
        string? obj = null;
        var ex = Assert.Throws<ArgumentNullException>(() => obj.SafeArgument());
        Assert.Equal("obj", ex.ParamName);
    }

    [Fact]
    public void SafeArgument_WithCustomArgumentName_UsesProvidedName()
    {
        string? obj = null;
        var ex = Assert.Throws<ArgumentNullException>(() => obj.SafeArgument("customName"));
        Assert.Equal("customName", ex.ParamName);
    }

    #endregion
}
