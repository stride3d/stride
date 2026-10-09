// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Builds the shader corpus used by the Stride.Shaders.Tests snapshot test: evaluates every *.sdeffectlog of the repository
// (samples, templates, editor package, engine) into the mixin tree the effect compiler would mix, and merges the folders
// written by ShaderCorpusCapture (STRIDE_SHADER_CORPUS_CAPTURE) from running games.
//
// Usage: Stride.Shaders.CorpusCapture [--out <corpus.json>] [--merge <capture folder>]...

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Stride.Core.Yaml;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Shaders;
using Stride.Shaders.Compiler;

var root = FindRepositoryRoot();
var output = Path.Combine(root, "sources", "shaders", "Stride.Shaders.Tests", "Corpus", "corpus.json");
var mergeFolders = new List<string>();
for (int i = 0; i < args.Length; ++i)
{
    switch (args[i])
    {
        case "--out": output = Path.GetFullPath(args[++i]); break;
        case "--merge": mergeFolders.Add(Path.GetFullPath(args[++i])); break;
        default: Console.Error.WriteLine($"Unknown argument {args[i]}"); return 1;
    }
}

// Module initializers register the effects (ShaderMixinManager), the parameter keys and the YAML serializers
foreach (var file in Directory.EnumerateFiles(AppContext.BaseDirectory, "Stride*.dll"))
{
    try
    {
        var assembly = Assembly.LoadFrom(file);
        RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
    }
    catch (Exception e) when (e is BadImageFormatException or FileLoadException or FileNotFoundException)
    {
        // Native or unrelated assembly
    }
}
RuntimeHelpers.RunModuleConstructor(typeof(Program).Module.ModuleHandle);

var entries = new Dictionary<string, ShaderCorpusEntry>();
void Add(ShaderCorpusEntry entry)
{
    if (entries.TryGetValue(entry.Hash, out var existing))
        existing.Sources.UnionWith(entry.Sources);
    else
        entries.Add(entry.Hash, entry);
}

// Keep what earlier runs merged from captures; the effect log entries are rebuilt below
var kept = 0;
if (File.Exists(output))
{
    foreach (var entry in ShaderCorpusEntry.ReadFile(output))
    {
        entry.Sources.RemoveWhere(x => x.EndsWith(".sdeffectlog", StringComparison.Ordinal));
        if (entry.Sources.Count > 0)
        {
            Add(entry);
            kept++;
        }
    }
}

var logs = new[] { "samples", "sources/editor", "sources/engine" }
    .SelectMany(x => Directory.EnumerateFiles(Path.Combine(root, x), "*.sdeffectlog", SearchOption.AllDirectories))
    .Select(x => Path.GetRelativePath(root, x).Replace('\\', '/'))
    .Where(x => !x.Contains("/bin/") && !x.Contains("/obj/"))
    .Order(StringComparer.Ordinal)
    .ToList();

int requestCount = 0, failedCount = 0;
foreach (var log in logs)
{
    var profile = FindProfile(Path.Combine(root, log));
    int ok = 0, failed = 0;
    foreach (var (document, index) in SplitDocuments(File.ReadAllText(Path.Combine(root, log))).Select((x, i) => (x, i)))
    {
        requestCount++;
        try
        {
            var request = (EffectCompileRequest)AssetYamlSerializer.Default.Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(document)), typeof(EffectCompileRequest));
            var parameters = new CompilerParameters(request.UsedParameters);
            parameters.EffectParameters.Platform = GraphicsPlatform.Direct3D11;
            parameters.EffectParameters.Profile = profile;

            var mixin = ShaderMixinManager.Contains(request.EffectName)
                ? ShaderMixinManager.Generate(request.EffectName, parameters)
                : new ShaderMixinSource { Name = request.EffectName, Mixins = { new ShaderClassSource(request.EffectName) } };
            var entry = new ShaderCorpusEntry(request.EffectName, profile, mixin);
            entry.Sources.Add(log);
            Add(entry);
            ok++;
        }
        catch (Exception e)
        {
            Console.WriteLine($"  stale: {log} #{index}: {Flatten(e)}");
            failed++;
        }
    }
    failedCount += failed;
    Console.WriteLine($"{log}: {ok} ok, {failed} stale ({profile})");
}

foreach (var folder in mergeFolders)
{
    var files = Directory.EnumerateFiles(folder, "*.json").ToList();
    foreach (var file in files)
        Add(ShaderCorpusEntry.FromJson(File.ReadAllText(file)));
    Console.WriteLine($"{folder}: {files.Count} captured permutations");
}

ShaderCorpusEntry.WriteFile(output, entries.Values);
Console.WriteLine($"{requestCount} effect log requests ({failedCount} stale) + {kept} kept captures + new captures -> {entries.Count} unique permutations, written to {output}");
return 0;

static string FindRepositoryRoot()
{
    for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
    {
        if (Directory.Exists(Path.Combine(directory.FullName, "sources", "shaders")) && Directory.Exists(Path.Combine(directory.FullName, "samples")))
            return directory.FullName;
    }
    throw new InvalidOperationException($"Can't find the repository root above {AppContext.BaseDirectory}");
}

// The profile the asset build compiles a log with: the game settings of its package (Level_10_0 is Stride's default)
static GraphicsProfile FindProfile(string logPath)
{
    for (var directory = new DirectoryInfo(Path.GetDirectoryName(logPath)!); directory != null && directory.Name != "samples"; directory = directory.Parent)
    {
        foreach (var settings in directory.EnumerateFiles("*.sdgamesettings"))
        {
            var match = Regex.Match(File.ReadAllText(settings.FullName), @"DefaultGraphicsProfile:\s*(\w+)");
            if (match.Success)
                return Enum.Parse<GraphicsProfile>(match.Groups[1].Value);
        }
    }
    return GraphicsProfile.Level_10_0;
}

// One YAML document per request, so that one stale entry (e.g. a removed parameter key) doesn't hide the others
static IEnumerable<string> SplitDocuments(string text)
    => Regex.Split(text.TrimStart((char)0xFEFF), @"^---[ \t]*\r?$", RegexOptions.Multiline).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => "---" + x);

static string Flatten(Exception e) => e.InnerException != null ? $"{e.Message} -> {Flatten(e.InnerException)}" : e.Message;

partial class Program;
