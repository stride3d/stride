// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Storage;
using Stride.Shaders.Compilers;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using Spv = Stride.Shaders.Spirv.Tools.Spv;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// The on-disk shader cache: a shader is read back from the cache while its sources are unchanged, and
/// recompiled once its own file, one of its bases, its macros or its generic arguments change.
/// Each test writes its own shaders to a temporary directory and shares one cache directory between
/// loaders, so a new loader behaves like a new process reading the cache the previous one wrote.
/// </summary>
public class ShaderDiskCacheTests : IDisposable
{
    private readonly DirectoryInfo sourceDir = Directory.CreateTempSubdirectory("stride-shader-cache-src");
    private readonly DirectoryInfo cacheDir = Directory.CreateTempSubdirectory("stride-shader-cache");
    private readonly FileSystemProvider provider;

    public ShaderDiskCacheTests()
    {
        provider = new FileSystemProvider($"/shadercache-{Guid.NewGuid():N}", cacheDir.FullName);
    }

    public void Dispose()
    {
        provider.Dispose();
        sourceDir.Delete(recursive: true);
        cacheDir.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private const string BaseV1 = """
        namespace Stride.Shaders.Tests;

        shader CacheBase
        {
            int BaseValue() { return 1; }
        }
        """;

    private const string BaseV2 = """
        namespace Stride.Shaders.Tests;

        shader CacheBase
        {
            int BaseValue() { return 1; }
            int BaseAdded() { return 2; }
        }
        """;

    private const string LeafV1 = """
        namespace Stride.Shaders.Tests;

        shader CacheLeaf : CacheBase
        {
            int LeafValue() { return BaseValue(); }
        }
        """;

    private const string LeafV2 = """
        namespace Stride.Shaders.Tests;

        shader CacheLeaf : CacheBase
        {
            int LeafValue() { return BaseValue(); }
            int LeafAdded() { return 3; }
        }
        """;

    private const string LeafV3 = """
        namespace Stride.Shaders.Tests;

        shader CacheLeaf : CacheBase
        {
            int LeafValue() { return BaseValue(); }
            int LeafAdded() { return 3; }
            int LeafAddedAgain() { return 4; }
        }
        """;

    // Issue #3392 shape: one long-lived loader, several edits. The in-memory half of the cache stays
    // warm across all of them, so a second edit must be seen just like the first.
    [Fact]
    public void RepeatedEditsAreSeenByOneLongLivedLoader()
    {
        Write("CacheBase", BaseV1);
        Write("CacheLeaf", LeafV1);

        var loader = NewLoader();
        Assert.DoesNotContain("LeafAdded", Load(loader, "CacheLeaf"));

        Write("CacheLeaf", LeafV2);
        Assert.Contains("LeafAdded", Load(loader, "CacheLeaf"));

        Write("CacheLeaf", LeafV3);
        Assert.Contains("LeafAddedAgain", Load(loader, "CacheLeaf"));
    }

    private const string MacroShader = """
        namespace Stride.Shaders.Tests;

        shader CacheMacro
        {
        #if VARIANT_B
            int ValueB() { return 2; }
        #else
            int ValueA() { return 1; }
        #endif
        }
        """;

    // A cache that never hits would satisfy every staleness test below, so pin the hit down first.
    [Fact]
    public void UnchangedShaderIsReadBackFromTheDiskCache()
    {
        Write("CacheBase", BaseV1);
        Write("CacheLeaf", LeafV1);

        var first = NewLoader();
        var compiled = Load(first, "CacheLeaf");
        Assert.Contains("CacheLeaf", first.Compiled);

        var second = NewLoader();
        var cached = Load(second, "CacheLeaf");
        Assert.Empty(second.Compiled);
        Assert.Equal(compiled, cached);
    }

    [Fact]
    public void EditedShaderIsRecompiled()
    {
        Write("CacheBase", BaseV1);
        Write("CacheLeaf", LeafV1);
        Assert.DoesNotContain("LeafAdded", Load(NewLoader(), "CacheLeaf"));

        Write("CacheLeaf", LeafV2);

        var loader = NewLoader();
        Assert.Contains("LeafAdded", Load(loader, "CacheLeaf"));
        Assert.Contains("CacheLeaf", loader.Compiled);
    }

    // The leaf's own file is untouched here; only the hash of the base it recorded tells it to rebuild.
    [Fact]
    public void EditedBaseRecompilesTheShadersThatInheritIt()
    {
        Write("CacheBase", BaseV1);
        Write("CacheLeaf", LeafV1);
        Load(NewLoader(), "CacheLeaf");

        Write("CacheBase", BaseV2);

        var loader = NewLoader();
        Load(loader, "CacheLeaf");
        Assert.Contains("CacheLeaf", loader.Compiled);
    }

    [Fact]
    public void MacroVariantsGetTheirOwnCacheEntry()
    {
        Write("CacheMacro", MacroShader);

        var plain = Load(NewLoader(), "CacheMacro");
        Assert.Contains("ValueA", plain);
        Assert.DoesNotContain("ValueB", plain);

        var variant = Load(NewLoader(), "CacheMacro", new ShaderMacro("VARIANT_B", "1"));
        Assert.Contains("ValueB", variant);
        Assert.DoesNotContain("ValueA", variant);
    }

    private const string GenericBase = """
        namespace Stride.Shaders.Tests;

        shader CacheGenericBase
        {
            int M() { return 0; }
        }
        """;

    private const string GenericImpl = """
        namespace Stride.Shaders.Tests;

        shader CacheGenericImpl<int TValue> : CacheGenericBase
        {
            override int M() { return TValue; }
        }
        """;

    private const string GenericImplV2 = """
        namespace Stride.Shaders.Tests;

        shader CacheGenericImpl<int TValue> : CacheGenericBase
        {
            override int M() { return TValue + 100; }
        }
        """;

    private const string ComposeRoot = """
        namespace Stride.Shaders.Tests;

        shader CacheComposeRoot : ComputeShaderBase
        {
            compose CacheGenericBase helper;
            RWBuffer<int> Out;
            override void Compute() { Out[0] = helper.M(); }
        }
        """;

    private const string GenericBaseWithN = """
        namespace Stride.Shaders.Tests;

        shader CacheGenericBase
        {
            int M() { return 0; }
            int N() { return 0; }
        }
        """;

    private const string GenericImplWithN = """
        namespace Stride.Shaders.Tests;

        shader CacheGenericImpl<int TValue> : CacheGenericBase
        {
            override int M() { return TValue; }
            override int N() { return TValue + 10; }
        }
        """;

    private const string ComposeRootCallingN = """
        namespace Stride.Shaders.Tests;

        shader CacheComposeRoot : ComputeShaderBase
        {
            compose CacheGenericBase helper;
            RWBuffer<int> Out;
            override void Compute() { Out[0] = helper.M() + helper.N(); }
        }
        """;

    // A generic is only instantiated while mixing, so these go through the mixer rather than the loader.
    [Fact]
    public void UnchangedGenericInstantiationIsReadBackFromTheDiskCache()
    {
        WriteGenericShaders();

        var first = NewLoader();
        var compiled = Mix(first, 1);
        Assert.NotEmpty(first.Compiled);

        var second = NewLoader();
        var cached = Mix(second, 1);
        Assert.Empty(second.Compiled);
        Assert.Equal(compiled, cached);
    }

    [Fact]
    public void EditedGenericIsRecompiled()
    {
        WriteGenericShaders();
        var before = Mix(NewLoader(), 1);

        // The mixer drops what nothing calls, so change the body of a called method rather than adding one.
        Write("CacheGenericImpl", GenericImplV2);

        var loader = NewLoader();
        Assert.NotEqual(before, Mix(loader, 1));
        Assert.Contains("CacheGenericImpl", loader.Compiled);
    }

    // A method added to both the base and the generic must resolve to the generic's override. Getting the
    // base's instead means the stale instantiation was reused, where the override did not exist yet.
    [Fact]
    public void RebuiltInstantiationKeepsItsOverrideOverTheBase()
    {
        WriteGenericShaders();
        Mix(NewLoader(), 1);

        Write("CacheGenericBase", GenericBaseWithN);
        Write("CacheGenericImpl", GenericImplWithN);
        Write("CacheComposeRoot", ComposeRootCallingN);

        var mixed = Mix(NewLoader(), 1);
        Assert.Contains("CacheGenericImpl<1>.N", mixed);
        Assert.DoesNotContain("CacheGenericBase.N", mixed);
    }

    // Both instantiations come from one template file, so they must not end up sharing its cache entry.
    [Fact]
    public void DifferentGenericArgumentsDoNotShareACacheEntry()
    {
        WriteGenericShaders();

        var one = Mix(NewLoader(), 1);
        var two = Mix(NewLoader(), 2);

        Assert.Contains("CacheGenericImpl<1>", one);
        Assert.Contains("CacheGenericImpl<2>", two);
        Assert.DoesNotContain("CacheGenericImpl<2>", one);
        Assert.DoesNotContain("CacheGenericImpl<1>", two);
    }

    private const string MemberGen = """
        namespace Stride.Shaders.Tests;

        shader CacheMemberGen<MemberName TGroup>
        {
            cbuffer TGroup
            {
                float GroupValue;
            }

            float MemberValue() { return GroupValue; }
        }
        """;

    private const string MemberGenV2 = """
        namespace Stride.Shaders.Tests;

        shader CacheMemberGen<MemberName TGroup>
        {
            cbuffer TGroup
            {
                float GroupValue;
            }

            float MemberValue() { return GroupValue + 100; }
        }
        """;

    private const string MemberRoot = """
        namespace Stride.Shaders.Tests;

        shader CacheMemberRoot : ComputeShaderBase, CacheMemberGen<PerFrameGroup>
        {
            RWBuffer<float> Out;
            override void Compute() { Out[0] = MemberValue(); }
        }
        """;

    private const string MemberRootOtherGroup = """
        namespace Stride.Shaders.Tests;

        shader CacheMemberRootB : ComputeShaderBase, CacheMemberGen<PerDrawGroup>
        {
            RWBuffer<float> Out;
            override void Compute() { Out[0] = MemberValue(); }
        }
        """;

    // A MemberName generic doesn't just substitute a value, it is recompiled from macro-expanded source
    // under its own cache key, so it reaches the cache by a different path than CacheGenericImpl<1> above.
    [Fact]
    public void UnchangedMemberNameInstantiationIsReadBackFromTheDiskCache()
    {
        WriteMemberNameShaders();

        var first = NewLoader();
        var compiled = Mix(first, "CacheMemberRoot");
        Assert.Contains("CacheMemberGen", first.Compiled);

        var second = NewLoader();
        var cached = Mix(second, "CacheMemberRoot");
        Assert.Empty(second.Compiled);
        Assert.Equal(compiled, cached);
    }

    [Fact]
    public void EditedMemberNameGenericIsRecompiled()
    {
        WriteMemberNameShaders();
        var before = Mix(NewLoader(), "CacheMemberRoot");

        Write("CacheMemberGen", MemberGenV2);

        var loader = NewLoader();
        Assert.NotEqual(before, Mix(loader, "CacheMemberRoot"));
        Assert.Contains("CacheMemberGen", loader.Compiled);
    }

    [Fact]
    public void DifferentMemberNameArgumentsDoNotShareACacheEntry()
    {
        WriteMemberNameShaders();

        var perFrame = Mix(NewLoader(), "CacheMemberRoot");
        var perDraw = Mix(NewLoader(), "CacheMemberRootB");

        Assert.Contains("PerFrameGroup", perFrame);
        Assert.Contains("PerDrawGroup", perDraw);
        Assert.DoesNotContain("PerDrawGroup", perFrame);
        Assert.DoesNotContain("PerFrameGroup", perDraw);
    }

    private const string GenericImplV3 = """
        namespace Stride.Shaders.Tests;

        shader CacheGenericImpl<int TValue> : CacheGenericBase
        {
            override int M() { return TValue + 200; }
        }
        """;

    private const string MemberGenV3 = """
        namespace Stride.Shaders.Tests;

        shader CacheMemberGen<MemberName TGroup>
        {
            cbuffer TGroup
            {
                float GroupValue;
            }

            float MemberValue() { return GroupValue + 200; }
        }
        """;

    // The reload case for both generic kinds: one loader, several edits, every one of them seen.
    [Fact]
    public void RepeatedEditsToAGenericAreSeenByOneLongLivedLoader()
    {
        WriteGenericShaders();
        var loader = NewLoader();

        var first = Mix(loader, 1);

        Write("CacheGenericImpl", GenericImplV2);
        var second = Mix(loader, 1);
        Assert.NotEqual(first, second);

        Write("CacheGenericImpl", GenericImplV3);
        Assert.NotEqual(second, Mix(loader, 1));
    }

    [Fact]
    public void RepeatedEditsToAMemberNameGenericAreSeenByOneLongLivedLoader()
    {
        WriteMemberNameShaders();
        var loader = NewLoader();

        var first = Mix(loader, "CacheMemberRoot");

        Write("CacheMemberGen", MemberGenV2);
        var second = Mix(loader, "CacheMemberRoot");
        Assert.NotEqual(first, second);

        Write("CacheMemberGen", MemberGenV3);
        Assert.NotEqual(second, Mix(loader, "CacheMemberRoot"));
    }

    private void WriteMemberNameShaders()
    {
        Write("CacheMemberGen", MemberGen);
        Write("CacheMemberRoot", MemberRoot);
        Write("CacheMemberRootB", MemberRootOtherGroup);
    }

    private void WriteGenericShaders()
    {
        Write("CacheGenericBase", GenericBase);
        Write("CacheGenericImpl", GenericImpl);
        Write("CacheComposeRoot", ComposeRoot);
    }

    private void Write(string name, string code)
        => File.WriteAllText(Path.Combine(sourceDir.FullName, $"{name}.sdsl"), code);

    /// <summary>A loader with an empty in-memory cache over the cache directory the previous ones filled.</summary>
    private CountingLoader NewLoader() => new(provider, sourceDir.FullName, "./assets/Stride/SDSL");

    private static string Load(CountingLoader loader, string name, params ShaderMacro[] macros)
    {
        Assert.True(loader.LoadExternalBuffer(name, macros, out var buffer, out _, out _), $"could not load shader {name}");
        return Spv.Dis(buffer, DisassemblerFlags.Name);
    }

    /// <summary>Mixes CacheComposeRoot with CacheGenericImpl&lt;<paramref name="genericValue"/>&gt; composed into it.</summary>
    private static string Mix(CountingLoader loader, int genericValue) => Mix(loader, new ShaderMixinSource
    {
        Mixins = { new ShaderClassSource("CacheComposeRoot") },
        Compositions = { ["helper"] = new ShaderMixinSource { Mixins = { new ShaderClassSource("CacheGenericImpl", genericValue) } } },
    });

    /// <summary>Mixes a root shader that instantiates its generics itself, through its inheritance list.</summary>
    private static string Mix(CountingLoader loader, string rootName)
        => Mix(loader, new ShaderMixinSource { Mixins = { new ShaderClassSource(rootName) } });

    private static string Mix(CountingLoader loader, ShaderMixinSource source)
    {
        var log = new LoggerResult();
        Assert.True(new ShaderMixer(loader).MergeSDSL(source, new ShaderMixer.Options(true), log, out var bytecode, out _, out _, out _),
            string.Join(Environment.NewLine, log.Messages.Select(m => m.Text)));
        return Spv.Dis(SpirvBytecode.CreateFromSpan(bytecode), DisassemblerFlags.Name);
    }

    /// <summary>Records which shaders were actually compiled, as opposed to read back from the cache.</summary>
    private sealed class CountingLoader(IVirtualFileProvider cacheProvider, params string[] searchPaths) : ShaderLoaderBase(new FileShaderCache(cacheProvider))
    {
        private readonly List<string> compiled = [];

        public IReadOnlyList<string> Compiled => compiled;

        protected override bool ExternalFileExists(string name) => SourceOf(name) != null;

        public override bool LoadExternalFileContent(string name, out string filename, out string code, out ObjectId hash)
        {
            var found = SourceOf(name);
            filename = found ?? "";
            if (found == null)
            {
                code = "";
                hash = ObjectId.Empty;
                return false;
            }

            var fileData = File.ReadAllBytes(found);
            hash = ObjectId.FromBytes(fileData);
            // A StreamReader so a UTF8 BOM doesn't end up in the code, like the engine's loader does.
            using var reader = new StreamReader(new MemoryStream(fileData), Encoding.UTF8);
            code = reader.ReadToEnd();
            return true;
        }

        protected override bool LoadFromCode(string? filename, string code, ObjectId hash, ReadOnlySpan<ShaderMacro> macros, out ShaderBuffers buffer, bool registerInCache = true)
        {
            compiled.Add(Path.GetFileNameWithoutExtension(filename) ?? "");
            return base.LoadFromCode(filename, code, hash, macros, out buffer, registerInCache);
        }

        private string? SourceOf(string name) => searchPaths.Select(p => Path.Combine(p, $"{name}.sdsl")).FirstOrDefault(File.Exists);
    }
}
