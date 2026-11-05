// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Xunit;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// Tests copying the files a raw asset needs along with a raw asset copied into a project.
/// </summary>
public sealed class TestRawAssetDependencyCopier : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"stride-dependencies-{Guid.NewGuid():N}");
    private readonly string source;
    private readonly string resources;

    public TestRawAssetDependencyCopier()
    {
        source = Path.Combine(root, "art", "scenes");
        resources = Path.Combine(root, "project", "Resources");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(resources);
        File.WriteAllText(Path.Combine(source, "model.gltf"), "model");
        File.Copy(Path.Combine(source, "model.gltf"), Path.Combine(resources, "model.gltf"));
    }

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void FileIsCopiedWhereTheImporterReadsIt()
    {
        var texture = Write(Path.Combine(root, "art", "sourceimages", "color.png"));
        var buffer = Write(Path.Combine(source, "buffers", "model.bin"));

        var copied = Copy(new RawAssetDependency(texture, "color.png"), new RawAssetDependency(buffer, Path.Combine("buffers", "model.bin")));

        Assert.Equal([Path.Combine(resources, "color.png"), Path.Combine(resources, "buffers", "model.bin")], copied.Select(file => file.ToOSPath()));
        Assert.Equal(File.ReadAllText(texture.ToOSPath()), File.ReadAllText(Path.Combine(resources, "color.png")));
    }

    [Fact]
    public void ExistingFileIsNotOverwritten()
    {
        var texture = Write(Path.Combine(source, "color.png"));
        File.WriteAllText(Path.Combine(resources, "color.png"), "existing");
        var logger = new LoggerResult();

        Assert.Empty(Copy(logger, new RawAssetDependency(texture, "color.png")));
        Assert.Equal("existing", File.ReadAllText(Path.Combine(resources, "color.png")));
        Assert.Contains(logger.Messages, message => message.Type == LogMessageType.Warning && message.Text.Contains("color.png"));
    }

    [Fact]
    public void ExistingFileWithTheSameContentsGivesNoWarning()
    {
        var texture = Write(Path.Combine(source, "color.png"));
        File.Copy(texture.ToOSPath(), Path.Combine(resources, "color.png"));
        var logger = new LoggerResult();

        Assert.Empty(Copy(logger, new RawAssetDependency(texture, "color.png")));
        Assert.DoesNotContain(logger.Messages, message => message.Type == LogMessageType.Warning);
    }

    [Fact]
    public void FileThatCannotBeCopiedDoesNotStopTheOthers()
    {
        var missing = new UFile(Path.Combine(source, "missing.png"));
        var texture = Write(Path.Combine(source, "color.png"));
        var logger = new LoggerResult();

        var copied = Copy(logger, new RawAssetDependency(missing, "missing.png"), new RawAssetDependency(texture, "color.png"));

        Assert.Equal([Path.Combine(resources, "color.png")], copied.Select(file => file.ToOSPath()));
        Assert.Contains(logger.Messages, message => message.Type == LogMessageType.Warning && message.Text.Contains("missing.png"));
    }

    [Fact]
    public void FileOutsideTheCopysFolderIsNotCopied()
    {
        var buffer = Write(Path.Combine(root, "art", "shared.bin"));
        var logger = new LoggerResult();

        Assert.Empty(Copy(logger, new RawAssetDependency(buffer, Path.Combine("..", "shared.bin"))));
        Assert.False(File.Exists(Path.Combine(root, "project", "shared.bin")));
        Assert.Contains(logger.Messages, message => message.Type == LogMessageType.Warning && message.Text.Contains("shared.bin"));
    }

    private UFile Write(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, Path.GetFileName(path) + " from " + Path.GetFileName(Path.GetDirectoryName(path)));
        return new UFile(path);
    }

    private IReadOnlyList<UFile> Copy(params RawAssetDependency[] dependencies) => Copy(new LoggerResult(), dependencies);

    private IReadOnlyList<UFile> Copy(LoggerResult logger, params RawAssetDependency[] dependencies)
    {
        var original = new UFile(Path.Combine(source, "model.gltf"));
        var copy = new UFile(Path.Combine(resources, "model.gltf"));
        return RawAssetDependencyCopier.CopyDependencies(original, copy, [new FakeImporter(original, dependencies)], logger);
    }

    private sealed class FakeImporter(UFile expectedFile, IReadOnlyList<RawAssetDependency> dependencies) : AssetImporterBase
    {
        public override Guid Id { get; } = Guid.NewGuid();

        public override string Description => "Fake importer";

        public override string SupportedFileExtensions => ".gltf";

        public override IEnumerable<Type> RootAssetTypes => [];

        public override IEnumerable<AssetItem> Import(UFile rawAssetPath, AssetImporterParameters importParameters) => [];

        // The dependencies are found from the original, which is read where it is.
        public override IEnumerable<RawAssetDependency> GetSourceDependencies(UFile rawAssetPath, Logger logger)
        {
            Assert.Equal(expectedFile, rawAssetPath);
            return dependencies;
        }
    }
}
