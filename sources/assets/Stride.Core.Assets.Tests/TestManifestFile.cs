// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Packages;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// Tests for the <see cref="ManifestFile"/> class.
/// </summary>
public class TestManifestFile
{
    [Fact]
    public void TestProperties()
    {
        // Arrange & Act
        var manifestFile = new ManifestFile
        {
            Source = "bin/Release/**/*",
            Target = "tools/",
            Exclude = "**/*.pdb"
        };

        // Assert
        Assert.Equal("bin/Release/**/*", manifestFile.Source);
        Assert.Equal("tools/", manifestFile.Target);
        Assert.Equal("**/*.pdb", manifestFile.Exclude);
    }

    [Fact]
    public void TestPropertiesDefaultToNull()
    {
        // Act
        var manifestFile = new ManifestFile();

        // Assert
        Assert.Null(manifestFile.Source);
        Assert.Null(manifestFile.Target);
        Assert.Null(manifestFile.Exclude);
    }
}
