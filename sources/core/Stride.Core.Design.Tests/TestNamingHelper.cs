// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.IO;
using Xunit;

namespace Stride.Core.Design.Tests;

public class TestNamingHelper
{
    [Fact]
    public void TestIdentifier()
    {
        Assert.True(NamingHelper.IsIdentifier("_"));
        Assert.True(NamingHelper.IsIdentifier("a"));
        Assert.True(NamingHelper.IsIdentifier("aThisIsOk"));
        Assert.True(NamingHelper.IsIdentifier("aThis_IsOk"));
        Assert.True(NamingHelper.IsIdentifier("ThisIsOk"));
        Assert.True(NamingHelper.IsIdentifier("T"));
        Assert.True(NamingHelper.IsIdentifier("_a"));
        Assert.True(NamingHelper.IsIdentifier("_aThisIsOk987"));

        Assert.False(NamingHelper.IsIdentifier(""));
        Assert.False(NamingHelper.IsIdentifier("9"));
        Assert.False(NamingHelper.IsIdentifier("a "));
        Assert.False(NamingHelper.IsIdentifier("a x"));
        Assert.False(NamingHelper.IsIdentifier("9aaaaa"));
        Assert.False(NamingHelper.IsIdentifier("9aa.aaa"));
        Assert.False(NamingHelper.IsIdentifier("9aa.aaa"));
    }

    [Fact]
    public void TestNamespace()
    {
        Assert.True(NamingHelper.IsValidNamespace("a"));
        Assert.True(NamingHelper.IsValidNamespace("aThisIsOk"));
        Assert.True(NamingHelper.IsValidNamespace("aThis._IsOk"));
        Assert.True(NamingHelper.IsValidNamespace("a.b.c"));

        Assert.False(NamingHelper.IsValidNamespace(""));
        Assert.False(NamingHelper.IsValidNamespace("a   . w"));
        Assert.False(NamingHelper.IsValidNamespace("a e zaThis._IsOk"));
        Assert.False(NamingHelper.IsValidNamespace("9.b.c"));
        Assert.False(NamingHelper.IsValidNamespace("a.b."));
        Assert.False(NamingHelper.IsValidNamespace(".a."));
    }

    [Theory]
    [InlineData("MyClass")]
    [InlineData("MyClass123")]
    [InlineData("MyClass_123")]
    [InlineData("_MyClass")]
    [InlineData("a1")]
    public void IsIdentifier_ValidCases_ReturnsTrue(string input)
    {
        Assert.True(NamingHelper.IsIdentifier(input));
    }

    [Theory]
    [InlineData("123Class")]
    [InlineData("class-name")]
    [InlineData("class name")]
    [InlineData("class.name")]
    [InlineData("class-name!")]
    public void IsIdentifier_InvalidCases_ReturnsFalse(string input)
    {
        Assert.False(NamingHelper.IsIdentifier(input));
    }

    [Fact]
    public void IsIdentifier_WithNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => NamingHelper.IsIdentifier(null!));
    }

    [Theory]
    [InlineData("MyNamespace")]
    [InlineData("MyNamespace.SubNamespace")]
    [InlineData("System.Collections.Generic")]
    [InlineData("_Namespace")]
    public void IsValidNamespace_ValidCases_ReturnsTrue(string input)
    {
        Assert.True(NamingHelper.IsValidNamespace(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123Namespace")]
    [InlineData("Namespace.123Sub")]
    [InlineData("Name space")]
    public void IsValidNamespace_InvalidCases_ReturnsFalse(string input)
    {
        Assert.False(NamingHelper.IsValidNamespace(input));
    }

    [Fact]
    public void IsValidNamespace_WithErrorOut_ProvidesErrorMessage()
    {
        var result = NamingHelper.IsValidNamespace("123Invalid", out var error);

        Assert.False(result);
        Assert.NotNull(error);
        Assert.Contains("123Invalid", error);
    }

    [Fact]
    public void IsValidNamespace_WithNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => NamingHelper.IsValidNamespace(null!));
    }

    [Fact]
    public void ComputeNewName_WithAvailableBaseName_ReturnsBaseName()
    {
        var existingNames = new List<string> { "Item1", "Item3" };
        var result = NamingHelper.ComputeNewName("Item2", existingNames, x => x);

        Assert.Equal("Item2", result);
    }

    [Fact]
    public void ComputeNewName_WithTakenBaseName_ReturnsNumberedName()
    {
        var existingNames = new List<string> { "Item", "Item (2)" };
        var result = NamingHelper.ComputeNewName("Item", existingNames, x => x);

        Assert.Equal("Item (3)", result);
    }

    [Fact]
    public void ComputeNewName_WithCustomPattern_UsesPattern()
    {
        var existingNames = new List<string> { "Item", "Item_2" };
        var result = NamingHelper.ComputeNewName("Item", existingNames, x => x, "{0}_{1}");

        Assert.Equal("Item_3", result);
    }

    [Fact]
    public void ComputeNewName_WithDelegate_UsesDelegate()
    {
        var existingFiles = new HashSet<string> { "file1.txt", "file2.txt" };
        var result = NamingHelper.ComputeNewName(
            new UFile("file1.txt"),
            file => existingFiles.Contains(file.FullPath));

        Assert.Equal("file1.txt (2)", result);
    }

    [Fact]
    public void ComputeNewName_FindsFirstAvailableNumber()
    {
        var existingNames = new List<string> { "Item", "Item (2)", "Item (3)", "Item (5)" };
        var result = NamingHelper.ComputeNewName("Item", existingNames, x => x);

        // Should find next available, which is (4) since (5) exists
        Assert.Equal("Item (4)", result);
    }

    [Fact]
    public void ComputeNewName_WithInvalidPattern_ThrowsArgumentException()
    {
        var existingNames = new List<string> { "Item" };
        Assert.Throws<ArgumentException>(() =>
            NamingHelper.ComputeNewName("Item", existingNames, x => x, "Invalid Pattern"));
    }
}
