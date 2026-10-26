// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Stride.Core.Storage;
using Stride.Graphics;
using Stride.Shaders.Compiler;
using Stride.Shaders.Compilers;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Tools;

namespace Stride.Shaders.Parsers.Tests.Corpus;

/// <summary>
/// The permutations the snapshot test compiles: the captured corpus (<c>corpus.json</c>, see <c>Corpus/README.md</c>) plus one
/// entry per <c>RenderTests</c> shader.
/// </summary>
static class ShaderCorpus
{
    /// <summary>
    /// Every corpus entry is compiled for each of these: shaders have <c>#if</c> on the graphics API, and the mixer binds
    /// resources per register bank for Direct3D11 but with one unified scheme for Vulkan (and Direct3D12, Android, iOS).
    /// </summary>
    public static readonly GraphicsPlatform[] Platforms = [GraphicsPlatform.Direct3D11, GraphicsPlatform.Vulkan];

    /// <summary>
    /// The platforms of this run: all of them, or those listed in STRIDE_SHADER_CORPUS_PLATFORMS (comma-separated; the CI
    /// lanes check one each).
    /// </summary>
    public static GraphicsPlatform[] SelectedPlatforms { get; } = Environment.GetEnvironmentVariable("STRIDE_SHADER_CORPUS_PLATFORMS") is { Length: > 0 } platforms
        ? platforms.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Enum.Parse<GraphicsPlatform>).ToArray()
        : Platforms;

    // Folders searched for the .sdsl files of the captured corpus (recursively, bin/obj excluded); the first one wins on a name clash.
    static readonly string[] ShaderRoots =
    [
        "sources/engine",
        "sources/plugins",
        "sources/editor",
        "samples",
    ];

    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    public static string CorpusDirectory => Path.Combine(RepositoryRoot, "sources", "shaders", "Stride.Shaders.Tests", "Corpus");

    /// <summary>
    /// Written by Stride.Shaders.CorpusCapture.
    /// </summary>
    public static string CorpusFile => Path.Combine(CorpusDirectory, "corpus.json");

    public static List<ShaderCorpusEntry> ReadEntries() => ShaderCorpusEntry.ReadFile(CorpusFile);

    /// <summary>
    /// The engine shader classes the corpus should reach (name → file, relative to the repository): the .sdsl files of the
    /// shader roots apart from the samples, without the shaders of test projects.
    /// </summary>
    public static SortedDictionary<string, string> EngineShaderClasses()
    {
        var names = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var root in ShaderRoots.Where(x => x != "samples"))
        {
            var directory = Path.Combine(RepositoryRoot, root);
            if (!Directory.Exists(directory))
                continue;
            foreach (var file in Directory.EnumerateFiles(directory, "*.sdsl", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(RepositoryRoot, file).Replace('\\', '/');
                var folders = relative.Split('/')[..^1];
                // bin / obj, and test projects such as Stride.Engine.Tests or Stride.Graphics.Tests.11_0
                if (folders.Any(x => x is "bin" or "obj" || x.EndsWith(".Tests", StringComparison.Ordinal) || x.Contains(".Tests.", StringComparison.Ordinal)))
                    continue;
                names.TryAdd(Path.GetFileNameWithoutExtension(file), relative);
            }
        }
        return names;
    }

    static string RenderTestsDirectory => Path.Combine(RepositoryRoot, "sources", "shaders", "assets", "SDSL", "RenderTests");

    static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "sources", "shaders", "Stride.Shaders.Tests")))
                return directory.FullName;
        }
        throw new InvalidOperationException($"Can't find the repository root above {AppContext.BaseDirectory}");
    }

    public static IReadOnlyList<CorpusItem> LoadItems()
    {
        var items = new List<CorpusItem>();
        foreach (var entry in ReadEntries())
            items.Add(new CorpusItem(entry.Id, entry.EffectName, entry.Profile, null, () => entry.Mixin));
        foreach (var file in Directory.EnumerateFiles(RenderTestsDirectory, "*.sdsl").Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            items.Add(new CorpusItem($"RenderTests.{name}", name, GraphicsProfile.Level_11_0, file, () => ShaderMixinManager.Contains(name)
                ? ShaderMixinManager.Generate(name, new CompilerParameters())
                : new ShaderMixinSource { Name = name, Mixins = { new ShaderClassSource(name) } }));
        }
        return items;
    }

    static readonly Lazy<Dictionary<string, string>> ShaderIndex = new(() =>
    {
        var index = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var root in ShaderRoots)
        {
            var rootPath = Path.Combine(RepositoryRoot, root);
            if (!Directory.Exists(rootPath))
                continue;
            // Sorted on the '/' form of the path, so a name clash resolves the same way on every OS
            var files = Directory.EnumerateFiles(rootPath, "*.sdsl", SearchOption.AllDirectories)
                .Select(file => (File: file, Relative: Path.GetRelativePath(RepositoryRoot, file).Replace('\\', '/')))
                .Where(x => !x.Relative.Contains("/bin/") && !x.Relative.Contains("/obj/"))
                .OrderBy(x => x.Relative, StringComparer.Ordinal);
            foreach (var (file, _) in files)
                index.TryAdd(Path.GetFileNameWithoutExtension(file), file);
        }
        return index;
    });

    /// <summary>
    /// Loader for the captured corpus: shared by all its entries, so each shader class is parsed once.
    /// </summary>
    public static ShaderLoaderBase CreateLoader() => new IndexShaderLoader(ShaderIndex.Value);

    /// <param name="optimize">Also fills <see cref="CorpusResult.Optimized"/> (slower; for baseline runs).</param>
    public static CorpusResult Compile(CorpusItem item, GraphicsPlatform platform, ShaderLoaderBase sharedLoader, bool optimize = false)
    {
        // RenderTests files hold several shaders and reuse names across files: each one gets its own loader
        var loader = item.SourceFile != null ? new IndexShaderLoader(ShaderIndex.Value, item.SourceFile) : sharedLoader;

        var log = new Stride.Core.Diagnostics.LoggerResult();
        var mixer = new ShaderMixer(loader) { CollectExplanation = true };
        bool success;
        Span<byte> bytecode = default;
        EffectReflection? reflection = null;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var mixin = new ShaderMixinSource();
            mixin.DeepCloneFrom(item.CreateMixin());
            mixin.Name = item.EffectName;
            // RenderTests are compiled without macros, like RenderingTests does
            if (item.SourceFile == null)
                EffectCompiler.AddPlatformMacros(mixin, new EffectCompilerParameters { Platform = platform, Profile = item.Profile });

            // Registers every shader of the file, under the macros the mixer looks them up with
            if (item.SourceFile != null)
                loader.LoadExternalBuffer(item.EffectName, mixin.Macros.ToArray(), out _, out _, out _);

            success = mixer.MergeSDSL(mixin, EffectCompiler.GetMixerOptions(platform), log, out bytecode, out reflection, out _, out _);
        }
        catch (Exception e)
        {
            log.Error($"{e.GetType().Name}: {e.Message}", e);
            success = false;
        }

        if (!success || log.HasErrors)
        {
            var errors = log.Messages.Where(m => m.Type >= Stride.Core.Diagnostics.LogMessageType.Error).ToList();
            var error = errors.Count > 0 ? string.Join(Environment.NewLine, errors.Select(m => m.Text)) : "MergeSDSL failed without an error message";
            // The stack trace goes in the file only: line numbers would change the hash on unrelated edits
            var detail = string.Join(Environment.NewLine, errors.Select(m => m is Stride.Core.Diagnostics.LogMessage { Exception: { } exception } ? $"{m.Text}{Environment.NewLine}{exception}" : m.Text));
            return new CorpusResult(item, error, detail.Length > 0 ? detail : error, null, null, null, null, null, null) { Platform = platform };
        }

        var mixTime = stopwatch.Elapsed;
        var words = MemoryMarshal.Cast<byte, int>(bytecode).ToArray();
        int[]? legalizedWords = null;
        string? legalizationError = null;
        try
        {
            legalizedWords = MemoryMarshal.Cast<uint, int>(SpirvTools.LegalizeForHlsl(MemoryMarshal.Cast<int, uint>(words.AsSpan()))).ToArray();
        }
        catch (Exception e)
        {
            legalizationError = $"; legalization failed: {e.Message}{Environment.NewLine}";
        }
        var legalizeTime = stopwatch.Elapsed - mixTime;

        string? optimized = null;
        if (optimize && legalizedWords != null)
        {
            try
            {
                var words2 = SpirvTools.OptimizeForPerformance(MemoryMarshal.Cast<int, uint>(legalizedWords.AsSpan()));
                words2 = SpirvTools.Optimize(words2, ["--unify-const", "--canonicalize-ids"]);
                optimized = SpirvNormalizer.Normalize(MemoryMarshal.Cast<uint, int>(words2.AsSpan()), keepNames: false);
            }
            catch (Exception e)
            {
                optimized = $"; optimization failed: {e.Message}{Environment.NewLine}";
            }
        }

        var result = new CorpusResult(item, null, null,
            Strict: SpirvNormalizer.Normalize(words, keepNames: false),
            StrictNamed: SpirvNormalizer.Normalize(words, keepNames: true),
            Legalized: legalizedWords != null ? SpirvNormalizer.Normalize(legalizedWords, keepNames: false) : legalizationError,
            LegalizedNamed: legalizedWords != null ? SpirvNormalizer.Normalize(legalizedWords, keepNames: true) : legalizationError,
            Reflection: ReflectionText(reflection!),
            Explain: mixer.Explanation ?? "")
        {
            Platform = platform,
            Optimized = optimized,
            MixTime = mixTime,
            LegalizeTime = legalizeTime,
            NormalizeTime = stopwatch.Elapsed - mixTime - legalizeTime,
        };
        return result;
    }

    static string ReflectionText(EffectReflection reflection)
    {
        var builder = new StringBuilder();
        foreach (var cbuffer in reflection.ConstantBuffers)
        {
            builder.AppendLine($"cbuffer {cbuffer.Name} size {cbuffer.Size}");
            foreach (var member in cbuffer.Members)
                builder.AppendLine($"  {member.Offset,5} {member.Size,5} {member.RawName} => {member.KeyInfo.KeyName} : {TypeText(member.Type)} [{member.LogicalGroup}]");
        }
        foreach (var binding in reflection.ResourceBindings)
            builder.AppendLine($"binding {binding.Stage} {binding.Class} {binding.Type} {binding.RawName} => {binding.KeyInfo.KeyName} slot {binding.SlotStart}+{binding.SlotCount} group {binding.ResourceGroup} [{binding.LogicalGroup}]");
        foreach (var group in reflection.ResourceGroups)
        {
            builder.AppendLine($"group {group.Name}");
            foreach (var entry in group.Entries)
                builder.AppendLine($"  {entry.Class} {entry.Type} {entry.RawName} => {entry.KeyInfo.KeyName} stages {entry.Stages} slot {entry.SlotStart}+{entry.SlotCount} [{entry.LogicalGroup}]");
        }
        foreach (var attribute in reflection.InputAttributes ?? [])
            builder.AppendLine($"input {attribute.Location} {attribute.SemanticName}{attribute.SemanticIndex}");
        return builder.ToString();

        static string TypeText(EffectTypeDescription type) => $"{type.Class} {type.Type} {type.RowCount}x{type.ColumnCount}{(type.Elements > 0 ? $"[{type.Elements}]" : "")}";
    }

    /// <summary>
    /// Hash of a snapshot part, the same on every OS (line endings) and checkout (repository path).
    /// </summary>
    public static string ShortHash(string text)
    {
        text = text.Replace("\r\n", "\n").Replace(RepositoryRoot, "<repo>").Replace(RepositoryRoot.Replace('\\', '/'), "<repo>");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..12].ToLowerInvariant();
    }

    /// <param name="extraFile">Found by its file name before the index (a RenderTests file).</param>
    sealed class IndexShaderLoader(Dictionary<string, string> index, string? extraFile = null) : ShaderLoaderBase(new ShaderCache())
    {
        bool TryFind(string name, out string path)
        {
            if (extraFile != null && Path.GetFileNameWithoutExtension(extraFile) == name)
            {
                path = extraFile;
                return true;
            }
            return index.TryGetValue(name, out path!);
        }

        protected override bool ExternalFileExists(string name) => TryFind(name, out _);

        public override bool LoadExternalFileContent(string name, out string filename, out string code, out ObjectId hash)
        {
            if (!TryFind(name, out var path))
            {
                filename = code = "";
                hash = default;
                return false;
            }

            var fileData = File.ReadAllBytes(path);
            hash = ObjectId.FromBytes(fileData);
            // StreamReader skips the UTF-8 BOM
            using var reader = new StreamReader(new MemoryStream(fileData), Encoding.UTF8);
            code = reader.ReadToEnd();
            filename = path;
            return true;
        }
    }
}

sealed record CorpusItem(string Id, string EffectName, GraphicsProfile Profile, string? SourceFile, Func<ShaderMixinSource> CreateMixin);

/// <summary>
/// Normalized output of one corpus entry for one platform; <see cref="Error"/> is set instead when it doesn't compile.
/// </summary>
sealed record CorpusResult(CorpusItem Item, string? Error, string? ErrorDetail, string? Strict, string? StrictNamed, string? Legalized, string? LegalizedNamed, string? Reflection, string? Explain)
{
    public GraphicsPlatform Platform { get; init; }

    /// <summary>
    /// Snapshot key and output path: <c>Vulkan/StrideForwardShadingEffect.98f4d62b70</c>.
    /// </summary>
    public string Key => $"{Platform}/{Item.Id}";

    /// <summary>
    /// The legalized code after <c>spirv-opt -O --unify-const --canonicalize-ids</c>, normalized: what runs, whatever the
    /// order of functions and declarations. Not hashed; baseline runs compare it when the legalized code changed.
    /// </summary>
    public string? Optimized { get; init; }

    public TimeSpan MixTime { get; init; }
    public TimeSpan LegalizeTime { get; init; }
    public TimeSpan NormalizeTime { get; init; }
}
