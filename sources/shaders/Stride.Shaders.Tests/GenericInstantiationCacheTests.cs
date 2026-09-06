// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Stride.Core.IO;
using Stride.Core.Storage;
using Stride.Shaders.Compilers;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using Spv = Stride.Shaders.Spirv.Tools.Spv;
using Xunit;

namespace Stride.Shaders.Parsers.Tests;

public class GenericInstantiationCacheTests
{
    /// <summary>A loader over a few directories, with the on-disk cache the engine uses.</summary>
    private sealed class FileCachedLoader(IVirtualFileProvider cacheProvider, params string[] searchPaths) : ShaderLoaderBase(new FileShaderCache(cacheProvider))
    {
        private string? Find(string name) => searchPaths.Select(p => $"{p}/{name}.sdsl").FirstOrDefault(File.Exists);

        protected override bool ExternalFileExists(string name) => Find(name) != null;

        public override bool LoadExternalFileContent(string name, out string filename, out string code, out ObjectId hash)
        {
            var found = Find(name);
            filename = found ?? "";
            if (found == null)
            {
                code = "";
                hash = ObjectId.Empty;
                return false;
            }
            var fileData = File.ReadAllBytes(found);
            hash = ObjectId.FromBytes(fileData);
            using var reader = new StreamReader(new MemoryStream(fileData), Encoding.UTF8);
            code = reader.ReadToEnd();
            return true;
        }
    }

    private const string BaseV1 = """
        namespace Stride.Shaders.Tests
        {
        shader GenBase
        {
            int M() { return 0; }
        }
        }
        """;

    private const string ImplV1 = """
        namespace Stride.Shaders.Tests
        {
        shader GenImpl<int TValue> : GenBase
        {
            override int M() { return TValue; }
        }
        }
        """;

    private const string RootV1 = """
        namespace Stride.Shaders.Tests
        {
        shader GenRoot : ComputeShaderBase
        {
            compose GenBase helper;
            RWBuffer<int> Out;
            override void Compute() { Out[0] = helper.M(); }
        }
        }
        """;

    private const string BaseV2 = """
        namespace Stride.Shaders.Tests
        {
        shader GenBase
        {
            int M() { return 0; }
            int N() { return 0; }
        }
        }
        """;

    private const string ImplV2 = """
        namespace Stride.Shaders.Tests
        {
        shader GenImpl<int TValue> : GenBase
        {
            override int M() { return TValue; }
            override int N() { return TValue + 10; }
        }
        }
        """;

    private const string RootV2 = """
        namespace Stride.Shaders.Tests
        {
        shader GenRoot : ComputeShaderBase
        {
            compose GenBase helper;
            RWBuffer<int> Out;
            override void Compute() { Out[0] = helper.M() + helper.N(); }
        }
        }
        """;

    // Regression: an instantiated generic (Foo<1>) was taken from the on-disk cache without the
    // hash validation every other class goes through, so an edit to the generic or to its base
    // was never seen again until the cache was deleted by hand. Stride.Voxels' walk is a generic
    // (VoxelGridTraversalDDA<TSurface>), and a method added to its interface kept resolving to
    // the interface's empty body in the game while the same sources built correctly elsewhere.
    [Fact]
    public void EditedGenericInstantiationIsNotServedStaleFromTheDiskCache()
    {
        var sources = Directory.CreateTempSubdirectory("stride-generic-cache-src");
        var cacheDir = Directory.CreateTempSubdirectory("stride-generic-cache");
        try
        {
            void Write(string name, string code) => File.WriteAllText(Path.Combine(sources.FullName, name + ".sdsl"), code);

            Write("GenBase", BaseV1);
            Write("GenImpl", ImplV1);
            Write("GenRoot", RootV1);

            var provider = new FileSystemProvider("/gencache-" + Guid.NewGuid().ToString("N"), cacheDir.FullName);
            string Compile()
            {
                var loader = new FileCachedLoader(provider, sources.FullName, "./assets/Stride/SDSL");
                var mixer = new ShaderMixer(loader);
                var source = new ShaderMixinSource
                {
                    Mixins = { new ShaderClassSource("GenRoot") },
                    Compositions = { ["helper"] = new ShaderMixinSource { Mixins = { new ShaderClassSource("GenImpl", 1) } } },
                };
                var log = new Stride.Core.Diagnostics.LoggerResult();
                Assert.True(mixer.MergeSDSL(source, new ShaderMixer.Options(true), log, out var bytecode, out _, out _, out _),
                    string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));
                return Spv.Dis(SpirvBytecode.CreateFromSpan(bytecode), DisassemblerFlags.Name | DisassemblerFlags.Id | DisassemblerFlags.InstructionIndex, true);
            }

            // First build fills the cache with GenImpl<1>.
            var first = Compile();
            Assert.Contains("helper.GenImpl<1>.M", first);

            // A method added to the base and overridden by the generic, then a fresh process over
            // the same cache: the instantiation must be rebuilt, not served with its old method table.
            Write("GenBase", BaseV2);
            Write("GenImpl", ImplV2);
            Write("GenRoot", RootV2);

            var second = Compile();
            Assert.Contains("helper.GenImpl<1>.N", second);
            Assert.DoesNotContain("helper.GenBase.N", second);
        }
        finally
        {
            sources.Delete(recursive: true);
            cacheDir.Delete(recursive: true);
        }
    }
}
