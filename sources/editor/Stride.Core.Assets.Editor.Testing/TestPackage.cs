// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.Testing;

/// <summary>
/// Writes the package that a test opens: a package with its assets in an <c>Assets</c> folder, and a project next to it.
/// </summary>
public static class TestPackage
{
    /// <summary>
    /// The project written next to the package: a library that holds no asset types, so that the session doesn't build it.
    /// </summary>
    public const string ProjectFile = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <StrideContainsAssetTypes>false</StrideContainsAssetTypes>
          </PropertyGroup>
        </Project>

        """;

    /// <summary>
    /// Writes a package with the given assets in a folder named after it.
    /// </summary>
    /// <param name="directory">The folder that receives the folder of the package.</param>
    /// <param name="name">The name of the package, of its folder and of its project.</param>
    /// <param name="assets">The assets, each with its location in the package, without extension (<c>Dialogues/Guard</c>).</param>
    /// <param name="editable">Whether to write the project next to the package, which makes it a project of the solution, hence
    /// editable. Without it, the package is read-only, as the packages of the dependencies are.</param>
    /// <returns>The path of the package file.</returns>
    /// <exception cref="InvalidOperationException">The type of an asset has no <see cref="AssetDescriptionAttribute"/>.</exception>
    public static UFile Write(string directory, string name, IEnumerable<(string Location, Asset Asset)> assets, bool editable = true)
    {
        ArgumentNullException.ThrowIfNull(directory);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(assets);

        var packageDirectory = Path.Combine(directory, name);
        Directory.CreateDirectory(Path.Combine(packageDirectory, "Assets"));
        if (editable)
            File.WriteAllText(Path.Combine(packageDirectory, name + ".csproj"), ProjectFile);

        var packagePath = new UFile(Path.Combine(packageDirectory, name + Package.PackageFileExtension));
        var package = new Package
        {
            FullPath = packagePath,
            Meta = { Name = name, Version = new PackageVersion("1.0.0") },
            AssetFolders = { new AssetFolder("Assets") },
            ResourceFolders = { "Resources" },
        };
        AssetFileSerializer.Save(packagePath.ToOSPath(), package, null);

        foreach (var (location, asset) in assets)
        {
            var extension = AssetRegistry.GetDefaultExtension(asset.GetType()) ?? throw new InvalidOperationException($"{asset.GetType()} has no AssetDescription.");
            var path = Path.Combine(packageDirectory, "Assets", location + extension);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            AssetFileSerializer.Save(path, asset, null);
        }
        return packagePath;
    }
}
