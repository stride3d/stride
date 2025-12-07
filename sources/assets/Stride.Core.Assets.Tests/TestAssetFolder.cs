// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.IO;
using Xunit;

namespace Stride.Core.Assets.Tests;

public class TestAssetFolder
{
    [Fact]
    public void TestConstructorDefault()
    {
        var folder = new AssetFolder();
        // Path field is not initialized in default constructor (private field with no initialization)
        // The type has [DataContract] so it's meant for serialization
        Assert.NotNull(folder); // Just verify the object is created
    }

    [Fact]
    public void TestConstructorWithPath()
    {
        var path = new UDirectory("Assets/Textures");
        var folder = new AssetFolder(path);

        Assert.Equal(path, folder.Path);
    }

    [Fact]
    public void TestConstructorWithNullPathThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new AssetFolder(null!));
    }

    [Fact]
    public void TestPathProperty()
    {
        var folder = new AssetFolder();
        var path = new UDirectory("Assets/Models");

        folder.Path = path;
        Assert.Equal(path, folder.Path);
    }

    [Fact]
    public void TestPathPropertySetNull()
    {
        var folder = new AssetFolder(new UDirectory("Assets"));

        Assert.Throws<ArgumentNullException>(() => folder.Path = null!);
    }

    [Fact]
    public void TestPathPropertyGet()
    {
        var path = new UDirectory("Assets/Audio");
        var folder = new AssetFolder(path);

        var retrievedPath = folder.Path;
        Assert.Equal(path, retrievedPath);
    }

    [Fact]
    public void TestMultiplePathChanges()
    {
        var folder = new AssetFolder(new UDirectory("Assets/Initial"));

        var path2 = new UDirectory("Assets/Second");
        folder.Path = path2;
        Assert.Equal(path2, folder.Path);

        var path3 = new UDirectory("Assets/Third");
        folder.Path = path3;
        Assert.Equal(path3, folder.Path);
    }
}
