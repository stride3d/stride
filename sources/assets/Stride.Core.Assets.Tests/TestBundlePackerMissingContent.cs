// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Stride.AssetCompiler;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Xunit;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// The error of the bundle packer when compiled content references content that nothing compiled.
/// </summary>
public sealed class TestBundlePackerMissingContent : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"stride-bundle-packer-{Guid.NewGuid():N}");
    private readonly IVirtualFileProvider mount;
    private readonly ObjectDatabase database;
    private readonly DatabaseFileProvider provider;

    public TestBundlePackerMissingContent()
    {
        Directory.CreateDirectory(directory);
        var url = $"/bundle_packer_{Guid.NewGuid():N}";
        mount = VirtualFileSystem.MountFileSystem(url, directory);
        database = new ObjectDatabase(url, "index", loadDefaultBundle: false);
        provider = new DatabaseFileProvider(database);

        // A plugin's content that references a texture that no compiler scheduled
        WriteChunk("MyPluginAsset", ["Textures/Missing"]);
    }

    public void Dispose()
    {
        provider.Dispose();
        database.Dispose();
        mount.Dispose();
        Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public void BundleErrorNamesTheReferencingContent()
    {
        var bundle = new ResolvedBundle(new Bundle { Name = "default" });

        var exception = Assert.Throws<InvalidOperationException>(() => new BundlePacker().CollectBundle(provider, bundle, "MyPluginAsset", referencedBy: null));

        Assert.Contains("[Textures/Missing]", exception.Message, StringComparison.Ordinal);
        Assert.Contains("referenced by [MyPluginAsset]", exception.Message, StringComparison.Ordinal);
        Assert.Contains("GetRuntimeTypes", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NamedBundleErrorNamesTheReferencingContent()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new BundlePacker().CollectReferences(provider, new Bundle { Name = "level1" }, [], "MyPluginAsset", referencedBy: null));

        Assert.Contains("[Textures/Missing]", exception.Message, StringComparison.Ordinal);
        Assert.Contains("referenced by [MyPluginAsset]", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingRootAssetKeepsItsMessage()
    {
        var bundle = new ResolvedBundle(new Bundle { Name = "default" });

        var exception = Assert.Throws<InvalidOperationException>(() => new BundlePacker().CollectBundle(provider, bundle, "NotCompiled", referencedBy: null));

        Assert.Equal("Could not find asset NotCompiled for bundle default", exception.Message);
    }

    // A chunk as the content manager writes it: its header, then the list of the content it references
    private void WriteChunk(string url, List<string> references)
    {
        using var stream = provider.OpenStream(url, VirtualFileMode.Create, VirtualFileAccess.Write);
        var writer = new BinarySerializationWriter(stream);
        var header = new ChunkHeader { Type = typeof(object).AssemblyQualifiedName };
        header.Write(writer);
        header.OffsetToObject = (int)writer.UnderlyingStream.Position;
        header.OffsetToReferences = (int)writer.UnderlyingStream.Position;
        var chunkReferences = references.ConvertAll(x => new ChunkReference(typeof(object), x));
        writer.Serialize(ref chunkReferences, ArchiveMode.Serialize);
        stream.Seek(0, SeekOrigin.Begin);
        header.Write(new BinarySerializationWriter(stream));
    }
}
