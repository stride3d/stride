// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Spirv.Generators;

// Pinned upstream refs (tag or SHA). Bump these (and re-run the tool) to refresh Generated/*.cs.
// SPIRV-Headers ships vulkan-sdk-* tags; SPIRV-Registry has no tags so we pin a SHA.
const string SpirvHeadersRef = "vulkan-sdk-1.4.304.0";
const string SpirvRegistryRef = "a74197a3f0d5400764ce3bec2880f06e27b7b5d3";

// Files pulled from KhronosGroup/SPIRV-Headers @ SpirvHeadersRev, under include/spirv/unified1/
string[] spirvHeadersFiles =
[
    "spirv.json",
    "spirv.core.grammar.json",
    "extinst.glsl.std.450.grammar.json",
    "extinst.opencl.std.100.grammar.json",
    "extinst.spv-amd-gcn-shader.grammar.json",
    "extinst.spv-amd-shader-ballot.grammar.json",
    "extinst.spv-amd-shader-explicit-vertex-parameter.grammar.json",
    "extinst.spv-amd-shader-trinary-minmax.grammar.json",
];

// Files pulled from KhronosGroup/Registry-Root-SPIR-V @ SpirvRegistryRev, under specs/unified1/
string[] spirvRegistryFiles =
[
    "SPIRV.html",
    "GLSL.std.450.html",
];

string? outputDir = null;
string? extraGrammarPath = null;
for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--output" or "-o" when i + 1 < args.Length:
            outputDir = args[++i];
            break;
        case "--extra" when i + 1 < args.Length:
            extraGrammarPath = args[++i];
            break;
        case "--help" or "-h":
            PrintUsage();
            return 0;
        default:
            Console.Error.WriteLine($"Unknown argument: {args[i]}");
            PrintUsage();
            return 1;
    }
}

if (outputDir is null)
{
    Console.Error.WriteLine("Missing --output");
    PrintUsage();
    return 1;
}

// Local cache: %TEMP%/stride-spv-grammar/<rev>/<file>
var cacheRoot = Path.Combine(Path.GetTempPath(), "stride-spv-grammar");
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

var files = new List<SpvInputFile>();
foreach (var name in spirvHeadersFiles)
    files.Add(await FetchAsync(
        $"https://raw.githubusercontent.com/KhronosGroup/SPIRV-Headers/{SpirvHeadersRef}/include/spirv/unified1/{name}",
        SpirvHeadersRef, name));
foreach (var name in spirvRegistryFiles)
    files.Add(await FetchAsync(
        $"https://raw.githubusercontent.com/KhronosGroup/Registry-Root-SPIR-V/{SpirvRegistryRef}/specs/unified1/{name}",
        SpirvRegistryRef, name));

if (extraGrammarPath is not null)
{
    if (!File.Exists(extraGrammarPath))
    {
        Console.Error.WriteLine($"--extra file not found: {extraGrammarPath}");
        return 1;
    }
    files.Add(new SpvInputFile(extraGrammarPath, File.ReadAllText(extraGrammarPath)));
}

var sink = new DiskSpvOutput(outputDir);
SPVGenerator.Run(files, sink);
Console.WriteLine($"Wrote generated files to {outputDir}");
return 0;

async Task<SpvInputFile> FetchAsync(string url, string @ref, string fileName)
{
    // Replace path-unfriendly chars in ref (slashes from branch names etc.)
    var safeRef = @ref.Replace('/', '_').Replace('\\', '_');
    var cachePath = Path.Combine(cacheRoot, safeRef, fileName);
    if (!File.Exists(cachePath))
    {
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
        Console.WriteLine($"  Fetching {url}");
        var bytes = await http.GetByteArrayAsync(url);
        File.WriteAllBytes(cachePath, bytes);
    }
    return new SpvInputFile(cachePath, File.ReadAllText(cachePath));
}

void PrintUsage()
{
    Console.WriteLine("Usage: Stride.Shaders.Spirv.Generators --output <dir> [--extra <sdsl-grammar-ext.json>]");
    Console.WriteLine();
    Console.WriteLine("  --output <dir>   Directory where generated .cs files are written");
    Console.WriteLine("  --extra <path>   Optional Stride-specific grammar extension JSON");
    Console.WriteLine();
    Console.WriteLine($"  Pinned: SPIRV-Headers {SpirvHeadersRef}, Registry-Root-SPIR-V {SpirvRegistryRef[..7]}");
}
