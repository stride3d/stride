// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Builds the shader corpus used by the Stride.Shaders.Tests snapshot test, from empty, out of the given sources:
//   --corpus <corpus.json>       the entries of an existing corpus, to add captures to it
//   --embedded <file or folder>  lists of the effects embedded in Stride.Graphics (EmbeddedEffects.yaml): a shader class each
//   --capture <folder>           folders written by ShaderCorpusCapture (STRIDE_SHADER_CORPUS_CAPTURE), searched recursively
//   --out <corpus.json>          default: sources/shaders/Stride.Shaders.Tests/Corpus/corpus.json
// Paths are relative to the repository root. A permutation found in several sources is stored once, with all of them.
// See sources/shaders/Stride.Shaders.Tests/Corpus/README.md for the refresh recipe.

using Stride.Core.Yaml.Serialization;
using Stride.Graphics;
using Stride.Shaders;
using Stride.Shaders.Compiler;

var root = FindRepositoryRoot();
var output = Path.Combine(root, "sources", "shaders", "Stride.Shaders.Tests", "Corpus", "corpus.json");
var corpusFiles = new List<string>();
var embeddedSources = new List<string>();
var captureFolders = new List<string>();
for (int i = 0; i < args.Length; ++i)
{
    switch (args[i])
    {
        case "--out": output = Path.GetFullPath(args[++i], root); break;
        case "--corpus": corpusFiles.Add(Path.GetFullPath(args[++i], root)); break;
        case "--embedded": embeddedSources.Add(Path.GetFullPath(args[++i], root)); break;
        case "--capture": captureFolders.Add(Path.GetFullPath(args[++i], root)); break;
        default: Console.Error.WriteLine($"Unknown argument {args[i]}"); return 1;
    }
}
if (corpusFiles.Count == 0 && embeddedSources.Count == 0 && captureFolders.Count == 0)
{
    Console.Error.WriteLine("Nothing to build the corpus from: pass --corpus, --embedded and/or --capture (see the header of Program.cs)");
    return 1;
}

var entries = new Dictionary<string, ShaderCorpusEntry>();
void Add(ShaderCorpusEntry entry)
{
    if (entries.TryGetValue(entry.Hash, out var existing))
        existing.Sources.UnionWith(entry.Sources);
    else
        entries.Add(entry.Hash, entry);
}

foreach (var file in corpusFiles)
{
    var corpus = ShaderCorpusEntry.ReadFile(file);
    foreach (var entry in corpus)
        Add(entry);
    Console.WriteLine($"{Path.GetRelativePath(root, file)}: {corpus.Count} permutations");
}

// Each effect of the list is one shader class, with its generic values (Name or Name<value, ...>), at the profile of the list
var embeddedLists = embeddedSources
    .SelectMany(x => File.Exists(x) ? new[] { x } : Directory.GetFiles(x, "EmbeddedEffects.yaml", SearchOption.AllDirectories))
    .Select(x => Path.GetRelativePath(root, x).Replace('\\', '/'))
    .Where(x => !x.Contains("/bin/") && !x.Contains("/obj/"))
    .Order(StringComparer.Ordinal)
    .ToList();
foreach (var list in embeddedLists)
{
    var yaml = new YamlStream();
    using (var reader = new StreamReader(Path.Combine(root, list)))
        yaml.Load(reader);
    var document = (YamlMappingNode)yaml.Documents[0].RootNode;
    string Get(YamlMappingNode node, string key) => ((YamlScalarNode)node.Children[new YamlScalarNode(key)]).Value!;
    var profile = Enum.Parse<GraphicsProfile>(Get(document, "Profile"));
    var effects = ((YamlSequenceNode)document.Children[new YamlScalarNode("Effects")]).Children.Cast<YamlMappingNode>().ToList();
    foreach (var effect in effects)
    {
        var name = Get(effect, "Name");
        var shader = Get(effect, "Shader");
        var generics = shader.IndexOf('<');
        var mixin = new ShaderMixinSource { Name = name };
        mixin.Mixins.Add(generics < 0
            ? new ShaderClassSource(shader)
            : new ShaderClassSource(shader[..generics], shader[(generics + 1)..^1].Split(',', StringSplitOptions.TrimEntries)));
        var entry = new ShaderCorpusEntry(name, profile, mixin);
        entry.Sources.Add(list);
        Add(entry);
    }
    Console.WriteLine($"{list}: {effects.Count} embedded effects ({profile})");
}

foreach (var folder in captureFolders)
{
    var files = Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToList();
    foreach (var file in files)
        Add(ShaderCorpusEntry.FromJson(File.ReadAllText(file)));
    Console.WriteLine($"{Path.GetRelativePath(root, folder)}: {files.Count} captured files");
}

ShaderCorpusEntry.WriteFile(output, entries.Values);
Console.WriteLine($"{entries.Count} unique permutations, written to {output}");
foreach (var source in entries.Values.SelectMany(x => x.Sources).GroupBy(x => x).OrderBy(x => x.Key, StringComparer.Ordinal))
    Console.WriteLine($"  {source.Count(),5} {source.Key}");
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
