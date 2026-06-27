// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Stride.Assets;
using Xunit;
using static Stride.Assets.CodeUpgrades;

namespace Stride.Assets.Tests;

/// <summary>
/// In-memory tests for the symbol-driven code-upgrade engine. These validate semantic precision and
/// the rewrite shape without a real Stride package: a stub type stands in for the migrated member, and
/// an unrelated same-named member proves the rewrite never fires on the wrong symbol.
/// </summary>
public class TestCodeUpgrade
{
    private static IEnumerable<MetadataReference> FrameworkReferences()
    {
        var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;
        foreach (var path in trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                yield return MetadataReference.CreateFromFile(path);
        }
    }

    private static async Task<string> ApplyAsync(string source, params CodeUpgrade[] upgrades)
    {
        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Create(), "TestProject", "TestProject", LanguageNames.CSharp)
            .WithMetadataReferences(FrameworkReferences())
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            // C# 14 extension members are the real #3037 shape; parse at the latest language version.
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.Preview));
        workspace.AddProject(projectInfo);

        var documentId = DocumentId.CreateNewId(projectId);
        // Give the document a real path: the engine only migrates on-disk user source (it writes back by path).
        var solution = workspace.CurrentSolution.AddDocument(documentId, "Test.cs", SourceText.From(source), filePath: Path.Combine(Path.GetTempPath(), "Test.cs"));

        IReadOnlyList<ProjectId> targets = [projectId];
        foreach (var upgrade in upgrades)
            solution = await upgrade(solution, targets, CancellationToken.None);

        var document = solution.GetDocument(documentId);
        var text = await document.GetTextAsync();
        return text.ToString();
    }

    [Fact]
    public async Task PropertyToMethodWrapsOnlyTheMatchingSymbol()
    {
        var source = """
            namespace TestNs
            {
                class Target
                {
                    public bool IsSRgb => true;
                }
                class Other
                {
                    public bool IsSRgb => false;
                }
                class Usage
                {
                    bool A(Target t) => t.IsSRgb;
                    bool B(Other o) => o.IsSRgb;
                }
            }
            """;

        var result = await ApplyAsync(source, Rewrite(PropertyToMethod("TestNs.Target", "IsSRgb")));

        // The matching access is wrapped in an invocation; the unrelated same-named access is untouched.
        Assert.Contains("t.IsSRgb()", result);
        Assert.Contains("o.IsSRgb;", result);
        Assert.DoesNotContain("o.IsSRgb()", result);
        Assert.DoesNotContain("t.IsSRgb;", result);
    }

    [Fact]
    public async Task PropertyToMethodMigratesCSharp14ExtensionProperty()
    {
        // The actual #3037 case: an extension *property* (C# 14 extension block) turned into a method.
        // This is what PixelFormatExtensions.IsSRgb is — validates the member resolver walks the
        // nested extension grouping type and FindReferences binds the instance-sugar usage.
        var source = """
            namespace TestNs
            {
                enum MyFormat { A, B }
                static class MyFormatExtensions
                {
                    extension(MyFormat format)
                    {
                        public bool IsSRgb => format == MyFormat.A;
                    }
                }
                class Usage
                {
                    bool A(MyFormat f) => f.IsSRgb;
                }
            }
            """;

        var result = await ApplyAsync(source, Rewrite(PropertyToMethod("TestNs.MyFormatExtensions", "IsSRgb")));

        Assert.Contains("f.IsSRgb()", result);
        Assert.DoesNotContain("=> f.IsSRgb;", result);
    }

    [Fact]
    public async Task PropertyToMethodHandlesConditionalAccess()
    {
        var source = """
            namespace TestNs
            {
                class Target
                {
                    public bool IsSRgb => true;
                }
                class Usage
                {
                    bool? A(Target t) => t?.IsSRgb;
                }
            }
            """;

        var result = await ApplyAsync(source, Rewrite(PropertyToMethod("TestNs.Target", "IsSRgb")));

        Assert.Contains("t?.IsSRgb()", result);
    }

    [Fact]
    public async Task PropertyToMethodIsIdempotent()
    {
        // A property already migrated to a method call must not be double-wrapped on a re-run.
        var source = """
            namespace TestNs
            {
                class Target
                {
                    public bool IsSRgb() => true;
                }
                class Usage
                {
                    bool A(Target t) => t.IsSRgb();
                }
            }
            """;

        var result = await ApplyAsync(source, Rewrite(PropertyToMethod("TestNs.Target", "IsSRgb")));

        Assert.Contains("t.IsSRgb()", result);
        Assert.DoesNotContain("t.IsSRgb()()", result);
    }

    [Fact]
    public async Task MethodToPropertyUnwrapsOnlyTheMatchingSymbol()
    {
        var source = """
            namespace TestNs
            {
                class Target
                {
                    public bool IsSRgb() => true;
                }
                class Other
                {
                    public bool IsSRgb() => false;
                }
                class Usage
                {
                    bool A(Target t) => t.IsSRgb();
                    bool B(Other o) => o.IsSRgb();
                }
            }
            """;

        var result = await ApplyAsync(source, Rewrite(MethodToProperty("TestNs.Target", "IsSRgb")));

        // The matching call loses its parentheses; the unrelated same-named call is untouched.
        Assert.Contains("=> t.IsSRgb;", result);
        Assert.Contains("o.IsSRgb();", result);
        Assert.DoesNotContain("t.IsSRgb()", result);
        Assert.DoesNotContain("o.IsSRgb;", result);
    }

    [Fact]
    public async Task MethodToPropertyMigratesExtensionMethod()
    {
        // The actual #3037 case: a classic extension *method* (the 4.1 form) became a property — so
        // f.IsSRgb() -> f.IsSRgb. Validates the member resolver finds the static extension method and
        // FindReferences binds the instance-sugar call.
        var source = """
            namespace TestNs
            {
                enum MyFormat { A, B }
                static class MyFormatExtensions
                {
                    public static bool IsSRgb(this MyFormat format) => format == MyFormat.A;
                }
                class Usage
                {
                    bool A(MyFormat f) => f.IsSRgb();
                }
            }
            """;

        var result = await ApplyAsync(source, Rewrite(MethodToProperty("TestNs.MyFormatExtensions", "IsSRgb")));

        Assert.Contains("=> f.IsSRgb;", result);
        Assert.DoesNotContain("f.IsSRgb()", result);
    }

    [Fact]
    public async Task MethodToPropertyHandlesConditionalAccess()
    {
        var source = """
            namespace TestNs
            {
                class Target
                {
                    public bool IsSRgb() => true;
                }
                class Usage
                {
                    bool? A(Target t) => t?.IsSRgb();
                }
            }
            """;

        var result = await ApplyAsync(source, Rewrite(MethodToProperty("TestNs.Target", "IsSRgb")));

        Assert.Contains("t?.IsSRgb;", result);
        Assert.DoesNotContain("t?.IsSRgb()", result);
    }

    [Fact]
    public async Task MethodToPropertyLeavesArgumentCallsAlone()
    {
        // A call that passes arguments can't become a property access; it must be left untouched.
        var source = """
            namespace TestNs
            {
                static class MyExtensions
                {
                    public static bool IsSRgb(this int value) => value > 0;
                    public static bool IsSRgb(this int value, int extra) => value > extra;
                }
                class Usage
                {
                    bool A(int v) => v.IsSRgb(2);
                }
            }
            """;

        var result = await ApplyAsync(source, Rewrite(MethodToProperty("TestNs.MyExtensions", "IsSRgb")));

        Assert.Contains("v.IsSRgb(2)", result);
    }
}
