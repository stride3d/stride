// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Stride.Core.Assets.TextAccessors;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// A source code asset is saved in the encoding of its file: with a byte order mark only when the file had one, and without for a new file.
/// </summary>
public sealed class TestSourceCodeEncoding : IDisposable
{
    private static readonly byte[] Utf8ByteOrderMark = [0xEF, 0xBB, 0xBF];

    private readonly string directory = Path.Combine(Path.GetTempPath(), "Stride", nameof(TestSourceCodeEncoding), Guid.NewGuid().ToString("N"));

    [DataContract("TestEncodingSourceCodeAsset")]
    [AssetDescription(".sdencodingtest")]
    public sealed class TestEncodingSourceCodeAsset : SourceCodeAsset;

    public TestSourceCodeEncoding()
    {
        Directory.CreateDirectory(directory);
    }

    public void Dispose()
    {
        Directory.Delete(directory, recursive: true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnEditedFileKeepsItsByteOrderMarkOrItsLackOfOne(bool withByteOrderMark)
    {
        var path = WriteFile("Edited", "shader Foo {}", withByteOrderMark);
        var asset = AssetFileSerializer.Load<TestEncodingSourceCodeAsset>(path).Asset;

        Assert.Equal("shader Foo {}", asset.Text);
        asset.Text = "shader Bar {}";
        AssetFileSerializer.Save(path, asset, null);

        Assert.Equal(Expected("shader Bar {}", withByteOrderMark), File.ReadAllBytes(path));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AFileLoadedFromMemoryKeepsItsByteOrderMarkOrItsLackOfOne(bool withByteOrderMark)
    {
        var path = WriteFile("InMemory", "shader Foo {}", withByteOrderMark);
        var asset = AssetFileSerializer.Load<TestEncodingSourceCodeAsset>(new MemoryStream(File.ReadAllBytes(path)), path).Asset;

        Assert.Equal("shader Foo {}", asset.Text);
        asset.Text = "shader Bar {}";
        AssetFileSerializer.Save(path, asset, null);

        Assert.Equal(Expected("shader Bar {}", withByteOrderMark), File.ReadAllBytes(path));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TextSetBeforeTheFileIsReadKeepsTheByteOrderMarkOfTheFile(bool withByteOrderMark)
    {
        var path = WriteFile("SetFirst", "shader Foo {}", withByteOrderMark);
        var accessor = new FileTextAccessor { FilePath = path }.Create();

        accessor.Set("shader Bar {}");
        using var output = new MemoryStream();
        accessor.Save(output).Wait();

        Assert.Equal(Expected("shader Bar {}", withByteOrderMark), output.ToArray());
    }

    [Fact]
    public void ANewFileHasNoByteOrderMark()
    {
        var path = Path.Combine(directory, "New.sdencodingtest");
        var asset = new TestEncodingSourceCodeAsset { Text = "shader Foo {}" };

        AssetFileSerializer.Save(path, asset, null);

        Assert.Equal(Expected("shader Foo {}", false), File.ReadAllBytes(path));
    }

    [Fact]
    public void AUtf16FileStaysUtf16()
    {
        var path = Path.Combine(directory, "Utf16.sdencodingtest");
        File.WriteAllText(path, "shader Foo {}", Encoding.Unicode);
        var asset = AssetFileSerializer.Load<TestEncodingSourceCodeAsset>(path).Asset;

        Assert.Equal("shader Foo {}", asset.Text);
        asset.Text = "shader Bar {}";
        AssetFileSerializer.Save(path, asset, null);

        Assert.Equal([.. Encoding.Unicode.Preamble, .. Encoding.Unicode.GetBytes("shader Bar {}")], File.ReadAllBytes(path));
    }

    private string WriteFile(string name, string text, bool withByteOrderMark)
    {
        var path = Path.Combine(directory, name + ".sdencodingtest");
        File.WriteAllBytes(path, Expected(text, withByteOrderMark));
        return path;
    }

    private static byte[] Expected(string text, bool withByteOrderMark)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return withByteOrderMark ? [.. Utf8ByteOrderMark, .. bytes] : bytes;
    }
}
