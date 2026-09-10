// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Resolves which test suites a run needs, so a CI lane or a local build only builds those: either the
// explicit list given with --projects, or the suites a `dotnet test --filter` expression can hit.
// Suites are the test projects Stride.Tests.Combined references; for a filter, each is scanned for its
// test methods (syntax only, no compilation) and the filter is evaluated against their fully qualified
// names with the same engine vstest uses, so the tool and the run agree.
//
// "Every suite" is the answer whenever the tool is unsure: nothing given, a filter it cannot parse, or
// one that matches no test at all. Over-inclusion only costs build time.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestPlatform.Common.Filtering;

const string Usage = """
    Stride.TestSuites: which test suites a run needs.

      --projects <list>       explicit suites, as .csproj paths or names (separated by ; , space or newline)
      --filter <expression>   else the vstest filter to resolve (FullyQualifiedName~X|Name=Y, etc.)
      --combined <csproj>     the meta project listing the suites (default: sources/tests/Stride.Tests.Combined)
      --slnf <file>           write <file>.filtered.slnf keeping only the needed suites and print its path;
                              print <file> itself when every suite is needed, nothing when none of the
                              needed suites is in it

    Without --slnf, prints the needed suites' names one per line, and nothing when every suite is needed.
    """;

string? filter = null, combined = null, projects = null, slnf = null;
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--projects": projects = args[++i]; break;
        case "--filter": filter = args[++i]; break;
        case "--combined": combined = args[++i]; break;
        case "--slnf": slnf = args[++i]; break;
        default: Console.Error.WriteLine(Usage); return 2;
    }
}

combined ??= Path.Combine(FindRepoRoot(), "sources", "tests", "Stride.Tests.Combined", "Stride.Tests.Combined.csproj");
if (!File.Exists(combined))
{
    Console.Error.WriteLine($"Not found: {combined}");
    return 2;
}

// Empty means every suite
var needed = Resolve(projects, filter, combined);

if (slnf is null)
{
    foreach (var suite in needed)
        Console.WriteLine(suite);
    return 0;
}

Console.WriteLine(FilterSolution(slnf, needed, combined));
return 0;

static List<string> Resolve(string? projects, string? filter, string combined)
{
    if (!string.IsNullOrWhiteSpace(projects))
    {
        return projects.Split([';', ',', ' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(SuiteName)
            .ToList();
    }

    if (string.IsNullOrWhiteSpace(filter))
        return [];

    var expression = new FilterExpressionWrapper(filter);
    if (expression.ParseError is not null)
    {
        Console.Error.WriteLine($"Filter not understood ({expression.ParseError}), every suite is needed.");
        return [];
    }

    var matching = SuitesOf(combined)
        .Where(suite => TestsIn(suite).Any(test => Matches(expression, test)))
        .Select(SuiteName)
        .ToList();
    if (matching.Count == 0)
        Console.Error.WriteLine("No suite has a test matching the filter, every suite is needed.");
    return matching;
}

// The suites are the ProjectReference items Stride.Tests.Combined tags as suites, as MSBuild evaluates
// them, so attribute order or formatting in the csproj does not matter.
static List<string> SuitesOf(string combined)
{
    var msbuild = Process.Start(new ProcessStartInfo("dotnet", $"msbuild \"{combined}\" -nologo -getItem:ProjectReference")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    })!;
    var json = msbuild.StandardOutput.ReadToEnd();
    var errors = msbuild.StandardError.ReadToEnd();
    msbuild.WaitForExit();
    if (msbuild.ExitCode != 0)
        throw new InvalidOperationException($"dotnet msbuild -getItem failed:\n{errors}\n{json}");

    var directory = Path.GetDirectoryName(Path.GetFullPath(combined))!;
    return JsonDocument.Parse(json).RootElement
        .GetProperty("Items").GetProperty("ProjectReference").EnumerateArray()
        .Where(item => item.TryGetProperty("StrideTestSuiteRef", out var tag) && tag.GetString() == "true")
        .Select(item => Path.GetFullPath(Path.Combine(directory, item.GetProperty("Identity").GetString()!)))
        .ToList();
}

// Every method carrying a test attribute in the suite's sources, as namespace.Type.Method. Scans the
// directory rather than the csproj's Compile items, which can only over-include.
static IEnumerable<TestMethod> TestsIn(string csproj)
{
    var directory = Path.GetDirectoryName(csproj)!;
    foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
    {
        var relative = Path.GetRelativePath(directory, file);
        if (relative.StartsWith("bin", StringComparison.OrdinalIgnoreCase) || relative.StartsWith("obj", StringComparison.OrdinalIgnoreCase))
            continue;

        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot();
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (!method.AttributeLists.SelectMany(list => list.Attributes).Any(IsTestAttribute))
                continue;

            var types = method.Ancestors().OfType<TypeDeclarationSyntax>().Reverse().Select(type => type.Identifier.Text);
            var ns = method.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();
            var typeName = string.Join("+", types);
            yield return new TestMethod(ns is null ? typeName : $"{ns}.{typeName}", method.Identifier.Text);
        }
    }
}

// Fact, Theory, SkippableFact, and the repo's own attributes deriving from them.
static bool IsTestAttribute(AttributeSyntax attribute)
{
    var name = attribute.Name.ToString();
    name = name[(name.LastIndexOf('.') + 1)..];
    if (name.EndsWith("Attribute", StringComparison.Ordinal))
        name = name[..^"Attribute".Length];
    return name.EndsWith("Fact", StringComparison.Ordinal) || name.EndsWith("Theory", StringComparison.Ordinal);
}

static bool Matches(FilterExpressionWrapper expression, TestMethod test)
{
    var fullyQualifiedName = $"{test.Type}.{test.Method}";
    return expression.Evaluate(property => property switch
    {
        "FullyQualifiedName" => fullyQualifiedName,
        "Name" => test.Method,
        "DisplayName" => fullyQualifiedName,
        _ => null,
    });
}

// A solution filter next to the original with only the needed suites; other projects in it (the
// runner, helpers) are kept. The original is the answer when every suite is needed. Nothing is the
// answer when none of the needed suites is in this filter: the run has nothing to build from it.
static string FilterSolution(string slnf, List<string> needed, string combined)
{
    if (needed.Count == 0)
        return slnf;

    var suites = SuitesOf(combined).Select(SuiteName).ToHashSet(StringComparer.OrdinalIgnoreCase);
    var root = JsonNode.Parse(File.ReadAllText(slnf))!;
    var projects = root["solution"]!["projects"]!.AsArray().Select(project => project!.GetValue<string>()).ToList();

    var kept = projects
        .Where(project => !suites.Contains(SuiteName(project)) || needed.Contains(SuiteName(project), StringComparer.OrdinalIgnoreCase))
        .ToList();
    if (kept.Count == projects.Count)
        return slnf;
    if (!kept.Any(project => suites.Contains(SuiteName(project))))
    {
        Console.Error.WriteLine($"None of the needed suites is in {slnf}, nothing to build from it.");
        return "";
    }

    root["solution"]!["projects"] = new JsonArray(kept.Select(project => (JsonNode?)JsonValue.Create(project)).ToArray());
    var filtered = Path.ChangeExtension(slnf, ".filtered.slnf");
    File.WriteAllText(filtered, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    return filtered;
}

// A suite is named by its csproj path or by its bare name. Solution filters and user input write
// paths with backslashes, which Linux and macOS do not take as separators, and names contain dots
// (Stride.Graphics.Tests.10_0), so only a .csproj extension is stripped.
static string SuiteName(string project)
{
    var name = Path.GetFileName(project.Replace('\\', '/'));
    return name.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ? name[..^".csproj".Length] : name;
}

static string FindRepoRoot()
{
    for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        if (File.Exists(Path.Combine(directory.FullName, "Stride.sln")) || Directory.Exists(Path.Combine(directory.FullName, ".git")))
            return directory.FullName;
    return Directory.GetCurrentDirectory();
}

record TestMethod(string Type, string Method);
