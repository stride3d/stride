// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using Stride.Core.Diagnostics;
using Stride.Core.Storage;
using Stride.Graphics;
using Stride.Shaders.Compiler;
using Stride.Shaders.Compilers;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Parsing.Upgrade;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Tools;

namespace Stride.Shaders.Parsers.Tests.Corpus;

/// <summary>
/// Compiles the shaders of external libraries from local clones (never committed; first written for vvvv's VL.Fuse,
/// VL.StandardLibs and VL.Addons) against the engine shaders: each class alone, then each class mixed alone at the root, and
/// mixin trees shaped like the ones VL.Stride builds from C#, through the Direct3D 11 chain (validate, legalize, SPIRV-Cross,
/// fxc). Explicit tests, see the corpus README ("External shader libraries").
/// </summary>
public class ExternalShaderTests
{
    // STRIDE_EXTERNAL_SHADERS_SET="Name=relative/path;..." replaces the list (paths relative to STRIDE_EXTERNAL_SHADERS)
    static readonly (string Name, string Path)[] DefaultSet =
    [
        ("VL.StandardLibs", "VL.StandardLibs"),
        ("VL.Fuse", "VL.Fuse/vl/shaders"),
        ("VL.Addons", "VL.Addons/VL.Addons.Stride/shaders"),
    ];

    static (string Name, string Path)[] Set => Environment.GetEnvironmentVariable("STRIDE_EXTERNAL_SHADERS_SET") is { Length: > 0 } set
        ? set.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split('=', 2)).Select(x => (x[0], x[1])).ToArray()
        : DefaultSet;

    static List<string>? Names(string variable) => Environment.GetEnvironmentVariable(variable) is { Length: > 0 } names ? [.. names.Split(',')] : null;

    sealed record ExternalFile(string Library, string RelativePath, string Text);

    sealed record Run(Loader Loader, string Output, List<ExternalFile> Files);

    static Run? Prepare()
    {
        var root = Environment.GetEnvironmentVariable("STRIDE_EXTERNAL_SHADERS");
        if (string.IsNullOrEmpty(root))
            return null;
        var output = Environment.GetEnvironmentVariable("STRIDE_EXTERNAL_SHADERS_OUT") is { Length: > 0 } o ? o : Path.Combine(Path.GetTempPath(), "stride-external-shaders");
        Directory.CreateDirectory(output);

        var files = new List<ExternalFile>();
        foreach (var (name, path) in Set)
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(root, path), "*.sdsl", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
            {
                var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                if (relative.Contains("/bin/") || relative.Contains("/obj/"))
                    continue;
                using var reader = new StreamReader(file, Encoding.UTF8);
                files.Add(new ExternalFile(name, relative, reader.ReadToEnd()));
            }
        }

        // STRIDE_EXTERNAL_SHADERS_UPGRADE=1: each library is upgraded in memory, in the order of the set, with the libraries before it
        // (upgraded) as its dependencies, as their authors would do it; the changed files and the notes go to the output folder
        if (Environment.GetEnvironmentVariable("STRIDE_EXTERNAL_SHADERS_UPGRADE") == "1")
        {
            var upgraded = new Dictionary<string, string>(StringComparer.Ordinal);
            var notes = new List<string>();
            foreach (var library in files.Select(x => x.Library).Distinct())
            {
                var input = files.Where(x => x.Library == library).ToDictionary(x => x.RelativePath, x => x.Text, StringComparer.Ordinal);
                var result = ShaderUpgrader.Upgrade(input, EngineShaderClasses.Current, dependencies: new Dictionary<string, string>(upgraded));
                foreach (var (path, text) in result.Files)
                {
                    upgraded[path] = text;
                    if (input[path] == text)
                        continue;
                    var target = Path.Combine(output, "upgraded", path);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.WriteAllText(target, text);
                }
                notes.AddRange(result.Notes);
            }
            File.WriteAllLines(Path.Combine(output, "upgrade-notes.txt"), notes);
            files = files.Select(x => x with { Text = upgraded[x.RelativePath] }).ToList();
        }

        var engine = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in new[] { "engine", "plugins" }
            .SelectMany(x => Directory.EnumerateFiles(Path.Combine(ShaderCorpus.RepositoryRoot, "sources", x), "*.sdsl", SearchOption.AllDirectories).Order(StringComparer.Ordinal)))
        {
            var relative = file.Replace('\\', '/');
            if (!relative.Contains("/bin/") && !relative.Contains("/obj/"))
                engine.TryAdd(Path.GetFileNameWithoutExtension(file), file);
        }

        // The first file wins on a name clash (reported by the class test)
        var external = new Dictionary<string, ExternalFile>(StringComparer.Ordinal);
        foreach (var file in files)
            external.TryAdd(Path.GetFileNameWithoutExtension(file.RelativePath), file);

        // Warnings stay warnings: the test project turns them into errors
        return new Run(new Loader(engine, external) { WarningsAsErrors = false }, output, files);
    }

    sealed class Loader(Dictionary<string, string> engine, Dictionary<string, ExternalFile> external) : ShaderLoaderBase(new ShaderCache())
    {
        protected override bool ExternalFileExists(string name) => external.ContainsKey(name) || engine.ContainsKey(name);

        public override bool LoadExternalFileContent(string name, out string filename, out string code, out ObjectId hash)
        {
            if (external.TryGetValue(name, out var file))
            {
                filename = file.RelativePath;
                code = file.Text;
            }
            else if (engine.TryGetValue(name, out var path))
            {
                filename = path;
                using var reader = new StreamReader(path, Encoding.UTF8);
                code = reader.ReadToEnd();
            }
            else
            {
                filename = code = "";
                hash = default;
                return false;
            }
            hash = ObjectId.FromBytes(Encoding.UTF8.GetBytes(code));
            return true;
        }
    }

    static ShaderMixinSource WithPlatformMacros(ShaderMixinSource mixin)
    {
        EffectCompiler.AddPlatformMacros(mixin, new EffectCompilerParameters { Platform = GraphicsPlatform.Direct3D11, Profile = GraphicsProfile.Level_11_0 });
        return mixin;
    }

    static readonly Regex Codes = new(@"SDSL\d{4}", RegexOptions.Compiled);

    static string OneLine(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    static string Line(params string[] parts) => string.Join('\t', parts.Select(x => x.Replace('\t', ' ')));

    static string CodesOf(IEnumerable<string> messages) => string.Join(',', messages.SelectMany(x => Codes.Matches(x).Select(m => m.Value)).Distinct().Order());

    // result = ok, warning, error; the codes of the errors, then of the warnings; the messages
    static string[] Outcome(bool success, LoggerResult log)
    {
        var errors = log.Messages.Where(m => m.Type >= LogMessageType.Error).Select(m => OneLine(m.Text)).ToList();
        var warnings = log.Messages.Where(m => m.Type == LogMessageType.Warning).Select(m => OneLine(m.Text)).ToList();
        var message = string.Join(" | ", errors.Concat(warnings));
        return [!success || errors.Count > 0 ? "error" : warnings.Count > 0 ? "warning" : "ok", CodesOf(errors), CodesOf(warnings), message.Length > 4000 ? message[..4000] : message];
    }

    /// <summary>
    /// Every class: compiled alone, then mixed alone at the root. Writes <c>results.tsv</c> (library, file, class, step, result,
    /// error codes, warning codes, messages). <c>progress.txt</c> gets each step before it runs, so a process crash names its shader.
    /// STRIDE_EXTERNAL_SHADERS_ONLY / _SKIP = class names (comma-separated); STRIDE_EXTERNAL_SHADERS_DUMP=1 writes the mixed SPIR-V.
    /// </summary>
    [Fact(Explicit = true)]
    public void CompileExternalShaders()
    {
        var run = Prepare();
        if (run == null)
        {
            Assert.Skip("STRIDE_EXTERNAL_SHADERS is not set");
            return;
        }

        var lines = new List<string> { Line("library", "file", "class", "step", "result", "errors", "warnings", "message") };
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        var progress = Path.Combine(run.Output, "progress.txt");
        File.WriteAllText(progress, "");
        var only = Names("STRIDE_EXTERNAL_SHADERS_ONLY");
        var skip = Names("STRIDE_EXTERNAL_SHADERS_SKIP") ?? [];
        var dump = Environment.GetEnvironmentVariable("STRIDE_EXTERNAL_SHADERS_DUMP") == "1";

        foreach (var file in run.Files)
        {
            var name = Path.GetFileNameWithoutExtension(file.RelativePath);
            if (!seen.TryAdd(name, file.RelativePath))
            {
                lines.Add(Line(file.Library, file.RelativePath, name, "index", "duplicate", "", "", $"hidden by {seen[name]}"));
                continue;
            }
            if (only != null && !only.Contains(name))
                continue;
            if (skip.Contains(name))
            {
                lines.Add(Line(file.Library, file.RelativePath, name, "skip", "skipped", "", "", ""));
                continue;
            }

            File.AppendAllText(progress, $"load {name}{Environment.NewLine}");
            var log = new LoggerResult();
            run.Loader.Log = log;
            bool loaded;
            try
            {
                loaded = run.Loader.LoadExternalBuffer(name, WithPlatformMacros(new ShaderMixinSource()).Macros.ToArray(), out _, out _, out _);
            }
            catch (Exception e)
            {
                log.Error($"{e.GetType().Name}: {e.Message}");
                loaded = false;
            }
            run.Loader.Log = null;
            lines.Add(Line([file.Library, file.RelativePath, name, "load", .. Outcome(loaded, log)]));
            if (!loaded || log.HasErrors)
                continue;

            var (step, outcome) = Compile(run.Loader, name, new ShaderMixinSource { Mixins = { new ShaderClassSource(name) } }, progress, dump ? run.Output : null);
            lines.Add(Line([file.Library, file.RelativePath, name, step, .. outcome]));
        }

        File.WriteAllLines(Path.Combine(run.Output, "results.tsv"), lines);
    }

    // Mixin trees shaped like the ones VL.Stride builds from C#: class names of the libraries, generic values as strings
    static ShaderMixinSource Node(string root, object[]? generics = null, params (string Name, ShaderSource Value)[] compositions)
    {
        var node = new ShaderMixinSource { Mixins = { new ShaderClassSource(root, generics ?? []) } };
        foreach (var (name, value) in compositions)
            node.AddComposition(name, value);
        return node;
    }

    static ShaderArraySource Array(params ShaderSource[] values)
    {
        var array = new ShaderArraySource();
        foreach (var value in values)
            array.Add(value);
        return array;
    }

    static readonly (string Name, Func<ShaderMixinSource> Tree)[] Trees =
    [
        // ShaderFX draw graph: an input and a texture declared through LinkType / MemberName generics, in compositions
        ("ShaderFXDraw", () => Node("ShaderFXGraph", null,
            ("VertexRoot", Node("InputFloat4", ["ShaderFX.InputValueFloat4", "PerUpdate"])),
            ("PixelRoot", Node("SampleLevelTextureFloat4", ["ObjectTexture_fx0", "PerMaterial", "ObjectSampler_fx1", "PerMaterial"],
                ("TexCd", Node("ConstantFloat2", ["float2(0.5, 0.5)"])),
                ("LOD", Node("InputFloat", ["ShaderFX.InputValueFloat", "PerMaterial"])))))),
        // Two textures (other keys), the same sampler key twice
        ("ShaderFXDrawTwoTextures", () => Node("ShaderFXGraph", null,
            ("VertexRoot", Node("InputFloat4", ["ShaderFX.InputValueFloat4", "PerUpdate"])),
            ("PixelRoot", Node("PlusFloat4", null,
                ("Left", Node("SampleLevelTextureFloat4", ["ObjectTexture_fx0", "PerMaterial", "ObjectSampler_fx2", "PerMaterial"],
                    ("TexCd", Node("ConstantFloat2", ["float2(0.25, 0.5)"])),
                    ("LOD", Node("InputFloat", ["ShaderFX.InputValueFloat", "PerMaterial"])))),
                ("Right", Node("SampleLevelTextureFloat4", ["ObjectTexture_fx1", "PerMaterial", "ObjectSampler_fx2", "PerMaterial"],
                    ("TexCd", Node("ConstantFloat2", ["float2(0.75, 0.5)"])),
                    ("LOD", Node("InputFloat", ["ShaderFX.InputValueFloat.i1", "PerMaterial"])))))))),
        ("TextureFXGraph", () => Node("TextureFXGraph", null,
            ("Root", Node("PlusFloat4", null,
                ("Left", Node("InputFloat4", ["ShaderFX.InputValueFloat4", "PerMaterial"])),
                ("Right", Node("InputFloat4", ["ShaderFX.InputValueFloat4.i1", "PerMaterial"])))))),
        // A node in a composition that calls a parent's helper through `base.` (VL.Addons ValueMap)
        ("TextureFXGraphBaseHelper", () => Node("TextureFXGraph", null,
            ("Root", Node("JoinFloat4Float3", null,
                ("v", Node("ConstantFloat3", ["float3(1, 0, 0)"])),
                ("w", Node("ValueMap_Advanced_Internal_ShaderFX", null,
                    ("ColorIn", Node("InputFloat4", ["ShaderFX.InputValueFloat4", "PerMaterial"])))))))),
        // ComputeFX graph: an array of compositions, stream variables named by MemberName, a thread id node that reads the compute
        // streams (inherited ComputeShaderBase before 4.5)
        ("ComputeFXGraph", () => Node("ComputeFXGraph", null,
            ("Root", Node("ComputeOrder", null,
                ("Computes", Array(
                    Node("AssignVarFloat", ["Var_1"], ("Value", Node("InputFloat", ["ShaderFX.InputValueFloat", "PerUpdate"]))),
                    Node("SetItemRWBufferFloat", ["ObjectBuffer_fx2", "PerUpdate"],
                        ("Value", Node("GetVarFloat", ["Var_1"])),
                        ("Index", Node("GetLinearDispatchThreadId"))))))))),
    ];

    /// <summary>
    /// The trees above (skipped when their classes are not in the clones). Writes <c>trees.tsv</c> and each tree's reflection
    /// (<c>&lt;tree&gt;.reflection.txt</c>), to compare runs (e.g. master and a branch).
    /// </summary>
    [Fact(Explicit = true)]
    public void CompileExternalTrees()
    {
        var run = Prepare();
        if (run == null)
        {
            Assert.Skip("STRIDE_EXTERNAL_SHADERS is not set");
            return;
        }

        var lines = new List<string> { Line("tree", "step", "result", "errors", "warnings", "message") };
        var progress = Path.Combine(run.Output, "progress.txt");
        File.WriteAllText(progress, "");
        foreach (var (name, tree) in Trees)
        {
            var (step, outcome) = Compile(run.Loader, name, tree(), progress, null, reflection => File.WriteAllText(Path.Combine(run.Output, $"{name}.reflection.txt"), ReflectionText(reflection)));
            lines.Add(Line([name, step, .. outcome]));
        }
        File.WriteAllLines(Path.Combine(run.Output, "trees.tsv"), lines);
    }

    // Mix, then the Direct3D 11 effect compiler's chain; returns the last step reached and its outcome
    static (string Step, string[] Outcome) Compile(Loader loader, string name, ShaderMixinSource mixin, string progress, string? dumpFolder, Action<EffectReflection>? onReflection = null)
    {
        var log = new LoggerResult();
        var step = "mix";
        loader.Log = log;
        try
        {
            File.AppendAllText(progress, $"mix {name}{Environment.NewLine}");
            mixin.Name = name;
            WithPlatformMacros(mixin);
            if (!new ShaderMixer(loader).MergeSDSL(mixin, EffectCompiler.GetMixerOptions(GraphicsPlatform.Direct3D11), log, out var bytecode, out var reflection, out _, out _))
                return (step, Outcome(false, log));
            onReflection?.Invoke(reflection);
            if (dumpFolder != null)
                File.WriteAllBytes(Path.Combine(dumpFolder, $"{name}.spv"), bytecode.ToArray());

            // spirv-opt assumes valid input: an invalid module can crash the process
            step = "validate";
            File.AppendAllText(progress, $"  {step}{Environment.NewLine}");
            var validation = Stride.Shaders.Spirv.Tools.Spv.ValidateBinary(bytecode, targetVulkan: false);
            if (!validation.IsValid)
            {
                log.Error($"invalid SPIR-V: {validation.Output}");
                return (step, Outcome(false, log));
            }

            step = "legalize";
            File.AppendAllText(progress, $"  {step}{Environment.NewLine}");
            var translator = new SpirvTranslator(SpirvTools.LegalizeForHlsl(MemoryMarshal.Cast<byte, uint>(bytecode)).AsMemory());
            foreach (var entryPoint in translator.GetEntryPoints())
            {
                step = "spirv-cross";
                File.AppendAllText(progress, $"  {step} {entryPoint.ExecutionModel}{Environment.NewLine}");
                var code = translator.Translate(Backend.Hlsl, entryPoint);
                step = "fxc";
                var model = entryPoint.ExecutionModel switch
                {
                    ExecutionModel.Vertex => "vs_5_0",
                    ExecutionModel.TessellationControl => "hs_5_0",
                    ExecutionModel.TessellationEvaluation => "ds_5_0",
                    ExecutionModel.Geometry => "gs_5_0",
                    ExecutionModel.Fragment => "ps_5_0",
                    _ => "cs_5_0",
                };
                if (FxcSupport.Compile(code, model, entryPoint.TranslatedName) is { } fxcError)
                {
                    log.Error($"fxc {model}: {fxcError}");
                    return (step, Outcome(false, log));
                }
            }
            return (step, Outcome(true, log));
        }
        // A class used in compositions or as a parent has no entry point alone
        catch (Exception e) when (step == "mix" && e.Message.Contains("At least a pixel or compute shader is expected"))
        {
            return ("mix", ["not-root", "", "", ""]);
        }
        catch (Exception e)
        {
            log.Error($"{e.GetType().Name}: {e.Message}");
            return (step, Outcome(false, log));
        }
        finally
        {
            loader.Log = null;
        }
    }

    static string ReflectionText(EffectReflection reflection)
    {
        var builder = new StringBuilder();
        foreach (var cbuffer in reflection.ConstantBuffers)
        {
            builder.AppendLine($"cbuffer {cbuffer.Name} size {cbuffer.Size}");
            foreach (var member in cbuffer.Members)
                builder.AppendLine($"  {member.Offset,5} {member.Size,5} {member.RawName} => {member.KeyInfo.KeyName}");
        }
        foreach (var binding in reflection.ResourceBindings)
            builder.AppendLine($"binding {binding.Stage} {binding.Class} {binding.Type} {binding.RawName} => {binding.KeyInfo.KeyName} group {binding.ResourceGroup}");
        return builder.ToString();
    }
}
