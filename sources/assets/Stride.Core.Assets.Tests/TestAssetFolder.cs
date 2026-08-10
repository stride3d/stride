// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Core.Assets.Tests;

public class TestAssetFolder
{
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

}
