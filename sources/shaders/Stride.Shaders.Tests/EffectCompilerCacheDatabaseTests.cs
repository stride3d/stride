// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Graphics;
using Stride.Shaders.Compiler;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// An asset build shares one <see cref="EffectCompilerCache"/> between its commands, and each command has its own database,
/// whose index entries are saved when the command ends. An entry written anywhere else is lost.
/// </summary>
public class EffectCompilerCacheDatabaseTests : IDisposable
{
    private const string CompiledShadersPrefix = "__shaders_bytecode__/";

    private readonly string shaderPrefix = $"CacheDatabaseTest{Guid.NewGuid():N}";
    private readonly DirectoryInfo sourceDir;
    private readonly FileSystemProvider sourceProvider;
    private readonly ObjectDatabase objectDatabase;
    private readonly DatabaseFileProviderService databaseProvider;
    private readonly EffectCompiler compiler;
    private readonly EffectCompilerCache cache;

    public EffectCompilerCacheDatabaseTests()
    {
        sourceDir = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, $"cache-src-{Guid.NewGuid():N}"));
        sourceProvider = new FileSystemProvider($"/cachetest-{Guid.NewGuid():N}", AppContext.BaseDirectory);
        objectDatabase = ObjectDatabase.CreateDefaultDatabase();
        databaseProvider = new DatabaseFileProviderService(null);
        compiler = new EffectCompiler(sourceProvider);
        compiler.SourceDirectories.Add(sourceDir.Name);
        compiler.SourceDirectories.Add("assets/Stride/SDSL");
        cache = new EffectCompilerCache(compiler, databaseProvider) { CurrentCache = EffectBytecodeCacheLoadSource.StartupCache };
    }

    public void Dispose()
    {
        cache.Dispose();
        compiler.Dispose();
        objectDatabase.Dispose();
        sourceProvider.Dispose();
        sourceDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private CompilerResults Compile(string shaderName)
    {
        var shaderPath = Path.Combine(sourceDir.FullName, $"{shaderName}.sdsl");
        if (!File.Exists(shaderPath))
        {
            File.WriteAllText(shaderPath, $$"""
                shader {{shaderName}} : SpriteBase
                {
                    module override float4 Shading()
                    {
                        return float4(1, 0, 0, 1);
                    }
                };
                """);
            compiler.UrlToFilePath[$"{sourceDir.Name}/{shaderName}.sdsl"] = shaderPath;
        }

        // Vulkan: it consumes SPIR-V directly, so this compiles on every platform
        var parameters = new CompilerParameters
        {
            EffectParameters = EffectCompilerParameters.Default with { Platform = GraphicsPlatform.Vulkan },
        };
        return cache.Compile(new ShaderClassSource(shaderName), parameters);
    }

    private static void AssertCompiled(CompilerResults results)
    {
        var completed = results.Bytecode.WaitForResult();
        Assert.False(completed.CompilationLog.HasErrors,
            string.Join(Environment.NewLine, completed.CompilationLog.Messages.Select(m => m.Text)));
    }

    private DatabaseFileProvider CreateDatabase(IContentIndexMap indexMap) => new(indexMap, objectDatabase);

    private static string[] CompiledEntries(IContentIndexMap indexMap)
        => indexMap.SearchValues(x => x.Key.StartsWith(CompiledShadersPrefix)).Select(x => x.Key).ToArray();

    [Fact]
    public void AsynchronousCompileWritesInTheDatabaseOfItsCaller()
    {
        var firstIndex = ContentIndexMap.CreateInMemory();
        var secondIndex = ContentIndexMap.CreateInMemory();

        databaseProvider.FileProvider = CreateDatabase(firstIndex);
        var first = Compile($"{shaderPrefix}First");
        databaseProvider.FileProvider = CreateDatabase(secondIndex);
        var second = Compile($"{shaderPrefix}Second");

        AssertCompiled(first);
        AssertCompiled(second);

        var firstEntry = Assert.Single(CompiledEntries(firstIndex));
        var secondEntry = Assert.Single(CompiledEntries(secondIndex));
        Assert.NotEqual(firstEntry, secondEntry);
    }

    [Fact]
    public void EntryFoundInThePreviousIndexIsWrittenAgainOnlyByABuild()
    {
        var previousIndex = ContentIndexMap.CreateInMemory();
        databaseProvider.FileProvider = CreateDatabase(previousIndex);
        var shaderName = $"{shaderPrefix}Found";
        AssertCompiled(Compile(shaderName));
        var entry = Assert.Single(CompiledEntries(previousIndex));

        // A build rewrites its index from what its commands wrote
        var buildIndex = new RecordingIndexMap(previousIndex);
        databaseProvider.FileProvider = CreateDatabase(buildIndex);
        AssertCompiled(Compile(shaderName));
        Assert.Equal([entry], buildIndex.Written);

        // A running game does not write its index each time it looks up an effect
        cache.CurrentCache = EffectBytecodeCacheLoadSource.DynamicCache;
        var gameIndex = new RecordingIndexMap(previousIndex);
        databaseProvider.FileProvider = CreateDatabase(gameIndex);
        AssertCompiled(Compile(shaderName));
        Assert.Empty(gameIndex.Written);
    }

    private sealed class RecordingIndexMap(IContentIndexMap inner) : IContentIndexMap
    {
        public List<string> Written { get; } = [];

        public ObjectId this[string url]
        {
            get => inner[url];
            set
            {
                lock (Written)
                    Written.Add(url);
                inner[url] = value;
            }
        }

        public bool TryGetValue(string url, out ObjectId objectId) => inner.TryGetValue(url, out objectId);
        public bool Contains(string url) => inner.Contains(url);
        public IEnumerable<KeyValuePair<string, ObjectId>> SearchValues(Func<KeyValuePair<string, ObjectId>, bool> predicate) => inner.SearchValues(predicate);
        public IEnumerable<KeyValuePair<string, ObjectId>> GetMergedIdMap() => inner.GetMergedIdMap();
        public void Dispose() { }
    }
}
