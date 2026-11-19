// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.AssetCompiler.Tasks;
using Stride.Core.Diagnostics;
using Xunit;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// The version a packed package states (<see cref="PackAssetsHelper"/>): the version of the NuGet package it goes in.
/// </summary>
public sealed class TestPackAssetsVersion : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"stride-pack-assets-version-{Guid.NewGuid():N}");

    public TestPackAssetsVersion()
    {
        // A companion project of a plugin, with no sdpkg of its own
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "Plugin.Editor.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
    }

    public void Dispose()
    {
        Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public void ImplicitPackageCarriesThePackageVersion()
    {
        var packed = Pack(packageVersion: "2.3.4-beta1");

        Assert.Equal(new PackageVersion("2.3.4-beta1"), packed.Meta.Version);
    }

    [Fact]
    public void ImplicitPackageKeepsItsFallbackVersionWithoutPackageVersion()
    {
        var packed = Pack(packageVersion: null);

        Assert.Equal(Package.GetFallbackVersion(directory), packed.Meta.Version);
    }

    [Fact]
    public void InvalidPackageVersionIsAnError()
    {
        var logger = new LoggerResult();

        var result = PackAssetsHelper.Run(logger, Path.Combine(directory, "Plugin.Editor.csproj"), Path.Combine(directory, "obj", "pack"), [], packageKind: "Editor", packageVersion: "not a version");

        Assert.False(result);
        Assert.Contains(logger.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains("[not a version]", StringComparison.Ordinal));
    }

    private Package Pack(string? packageVersion)
    {
        var logger = new LoggerResult();
        var generatedItems = new List<(string SourcePath, string PackagePath)>();

        var result = PackAssetsHelper.Run(logger, Path.Combine(directory, "Plugin.Editor.csproj"), Path.Combine(directory, "obj", "pack"), generatedItems, packageKind: "Editor", packageVersion: packageVersion);

        Assert.True(result, string.Join(Environment.NewLine, logger.Messages));
        var packedFile = Assert.Single(generatedItems, x => x.SourcePath.EndsWith(".sdpkg", StringComparison.OrdinalIgnoreCase)).SourcePath;
        return Package.LoadRaw(new LoggerResult(), packedFile);
    }
}
