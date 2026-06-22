// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.IO;

namespace Stride.Core.Assets.Tests;

public class TestFileExtensionCollection
{
    [Fact]
    public void TestConstructorWithExtensionsOnly()
    {
        var collection = new FileExtensionCollection("*.txt;*.cs");

        Assert.Null(collection.Description);
        var extensions = collection.SingleExtensions.ToList();
        Assert.Equal(2, extensions.Count);
        Assert.Contains(".txt", extensions);
        Assert.Contains(".cs", extensions);
    }

    [Fact]
    public void TestConstructorWithDescriptionAndExtensions()
    {
        var collection = new FileExtensionCollection("Text Files", "*.txt;*.log");

        Assert.Equal("Text Files", collection.Description);
        Assert.NotNull(collection.SingleExtensions);
        Assert.Contains(".txt", collection.SingleExtensions);
        Assert.Contains(".log", collection.SingleExtensions);
    }

    [Fact]
    public void TestConstructorWithAdditionalExtensions()
    {
        var collection = new FileExtensionCollection("Code Files", "*.cs", "*.vb", "*.fs");

        Assert.Equal("Code Files", collection.Description);
        var extensions = collection.SingleExtensions.ToList();
        Assert.Contains(".cs", extensions);
        Assert.Contains(".vb", extensions);
        Assert.Contains(".fs", extensions);
    }

    [Fact]
    public void TestConcatenatedExtensions()
    {
        var collection = new FileExtensionCollection("*.txt;*.cs;*.xml");

        // Normalized extensions joined with ';' in declaration order
        Assert.Equal(".txt;.cs;.xml", collection.ConcatenatedExtensions);
    }

    [Fact]
    public void TestSingleExtensionsEnumeration()
    {
        var collection = new FileExtensionCollection("*.a;*.b;*.c");

        var extensions = collection.SingleExtensions.ToList();
        Assert.Equal(3, extensions.Count);
    }

    [Fact]
    public void TestEmptyDescription()
    {
        var collection = new FileExtensionCollection(string.Empty, "*.txt");

        Assert.Empty(collection.Description);
        Assert.Single(collection.SingleExtensions);
    }

    [Fact]
    public void TestSingleExtension()
    {
        var collection = new FileExtensionCollection("*.sdpkg");

        Assert.Single(collection.SingleExtensions);
        Assert.Equal(".sdpkg", collection.ConcatenatedExtensions);
    }

    [Fact]
    public void TestMultipleAdditionalExtensions()
    {
        var collection = new FileExtensionCollection("All Files", "*.txt", "*.doc", "*.pdf", "*.xls");

        var extensions = collection.SingleExtensions.ToList();
        Assert.Equal(4, extensions.Count);
        Assert.Contains(".txt", extensions);
        Assert.Contains(".doc", extensions);
        Assert.Contains(".pdf", extensions);
        Assert.Contains(".xls", extensions);
    }

    [Fact]
    public void TestContainsMatchesExtension()
    {
        var collection = new FileExtensionCollection("*.txt;*.cs");

        Assert.True(collection.Contains(".txt"));
        Assert.True(collection.Contains("*.cs"));
        Assert.False(collection.Contains(".xml"));
    }
}
