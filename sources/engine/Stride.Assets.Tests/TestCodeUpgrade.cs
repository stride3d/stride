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

    private static Task<string> ApplyAsync(string source, params CodeUpgrade[] upgrades) => ApplyAsync(source, otherSource: null, DocumentationMode.Parse, upgrades);

    // documentationMode: the one the project is opened with; None for a project that generates no documentation file
    private static Task<string> ApplyAsync(string source, DocumentationMode documentationMode, params CodeUpgrade[] upgrades) => ApplyAsync(source, otherSource: null, documentationMode, upgrades);

    // otherSource: a second document of the project, that the result does not show
    private static Task<string> ApplyAsync(string source, string otherSource, params CodeUpgrade[] upgrades) => ApplyAsync(source, otherSource, DocumentationMode.Parse, upgrades);

    private static async Task<string> ApplyAsync(string source, string otherSource, DocumentationMode documentationMode, params CodeUpgrade[] upgrades)
    {
        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Create(), "TestProject", "TestProject", LanguageNames.CSharp)
            .WithMetadataReferences(FrameworkReferences())
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            // C# 14 extension members are the real #3037 shape; parse at the latest language version.
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.Preview, documentationMode));
        workspace.AddProject(projectInfo);

        var documentId = DocumentId.CreateNewId(projectId);
        // Give the document a real path: the engine only migrates on-disk user source (it writes back by path).
        var solution = workspace.CurrentSolution.AddDocument(documentId, "Test.cs", SourceText.From(source), filePath: Path.Combine(Path.GetTempPath(), "Test.cs"));
        if (otherSource != null)
            solution = solution.AddDocument(DocumentId.CreateNewId(projectId), "Other.cs", SourceText.From(otherSource), filePath: Path.Combine(Path.GetTempPath(), "Other.cs"));
        // As the runner opens it
        solution = RoslynCodeUpgradeRunner.ParseDocumentationComments(solution);

        IReadOnlyList<ProjectId> targets = [projectId];
        foreach (var upgrade in upgrades)
            solution = await upgrade(solution, targets, CancellationToken.None);

        var document = solution.GetDocument(documentId);
        var text = await document.GetTextAsync();
        return text.ToString();
    }

    // No error, and every cref names something
    private static void AssertCompiles(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview, DocumentationMode.Diagnose));
        var compilation = CSharpCompilation.Create("Check", [syntaxTree], FrameworkReferences(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error || x.Id is "CS1574" or "CS1580" or "CS1584" or "CS0419"));
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
    public async Task MemberToExtensionQualifiesBareUsesAndImportsTheNamespace()
    {
        // A bare use needs `this.` (simple-name lookup skips extension members); a same-named member elsewhere is untouched
        var source = """
            namespace TestNs
            {
                class AudioSystem { public object Engine; }
                class Game
                {
                    public AudioSystem Audio => null;
                }
                class Other
                {
                    public AudioSystem Audio => null;
                }
                class Script : Game
                {
                    object A() => Audio.Engine;
                    object B() => this.Audio.Engine;
                    object C(Game g) => g.Audio;
                    object D(Game g) => g?.Audio;
                    object E() => base.Audio;
                    object F(Other o) => o.Audio;
                }
            }
            """;

        var result = await ApplyAsync(source, Rewrite(MemberToExtension("TestNs.Game", "Audio", "Extensions.Audio")));

        Assert.StartsWith("using Extensions.Audio;", result);
        Assert.Equal(1, result.Split("using Extensions.Audio;").Length - 1);
        Assert.Contains("object A() => this.Audio.Engine;", result);
        Assert.Contains("object B() => this.Audio.Engine;", result);
        Assert.Contains("object C(Game g) => g.Audio;", result);
        Assert.Contains("object D(Game g) => g?.Audio;", result);
        Assert.Contains("object E() => this.Audio;", result);
        Assert.Contains("object F(Other o) => o.Audio;", result);
    }

    [Fact]
    public async Task MemberToExtensionLeavesNamesThatAreNotExpressions()
    {
        // A cref and a property pattern name the member without an expression to qualify: left alone, the rest migrates
        var source = """
            namespace TestNs
            {
                class AudioSystem { public object Engine; }
                class Game
                {
                    public AudioSystem Audio => null;
                }
                /// <summary>Plays through <see cref="Game.Audio"/>.</summary>
                class Script : Game
                {
                    bool A(Game g) => g is { Audio: not null };
                    object B() => Audio;
                }
            }
            """;

        var result = await ApplyAsync(source, Rewrite(MemberToExtension("TestNs.Game", "Audio", "Extensions.Audio")));

        Assert.Contains("<see cref=\"Game.Audio\"/>", result);
        Assert.Contains("bool A(Game g) => g is { Audio: not null };", result);
        Assert.Contains("object B() => this.Audio;", result);
    }

    [Fact]
    public async Task MemberToExtensionImportsNothingForADocumentationReference()
    {
        var source = """
            namespace TestNs
            {
                class Game
                {
                    public object Audio => null;
                }
                /// <summary>Plays through <see cref="Game.Audio"/>.</summary>
                class Script : Game { }
            }
            """;

        var result = await ApplyAsync(source, DocumentationMode.None, Rewrite(MemberToExtension("TestNs.Game", "Audio", "Extensions.Audio")));

        Assert.Equal(source, result);
    }

    [Fact]
    public async Task MemberToExtensionRoutesUsesThroughAnotherMember()
    {
        // Every use of the old member, bare or qualified, goes through the Game property
        var source = """
            namespace TestNs
            {
                class AudioSystem { public object Engine; }
                class Game { }
                class ScriptComponent
                {
                    public Game Game => null;
                    public AudioSystem Audio => null;
                }
                class Script : ScriptComponent
                {
                    object A() => Audio.Engine;
                    object B() => this.Audio.Engine;
                    object C(ScriptComponent s) => s.Audio;
                    object D(ScriptComponent s) => s?.Audio.Engine;
                    object E() => base.Audio;
                }
            }
            """;

        var result = await ApplyAsync(source, Rewrite(MemberToExtension("TestNs.ScriptComponent", "Audio", "Extensions.Audio", throughMember: "Game")));

        Assert.StartsWith("using Extensions.Audio;", result);
        Assert.Contains("object A() => Game.Audio.Engine;", result);
        Assert.Contains("object B() => this.Game.Audio.Engine;", result);
        Assert.Contains("object C(ScriptComponent s) => s.Game.Audio;", result);
        Assert.Contains("object D(ScriptComponent s) => s?.Game.Audio.Engine;", result);
        Assert.Contains("object E() => base.Game.Audio;", result);
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
    public async Task ParameterRenameRenamesNamedArgumentOnConstructor()
    {
        // The actual #3249 case: IndexBufferBinding's constructor parameter `count` became `indexCount`,
        // breaking call sites that used a named argument.
        var source = """
            namespace TestNs
            {
                class Target
                {
                    public Target(int count, int offset = 0) { }
                }
                class Other
                {
                    public Other(int count) { }
                }
                class Usage
                {
                    Target A() => new Target(count: 5);
                    Target B() => new(count: 5);
                    Target C(int count) => new Target(count);
                    Other D() => new Other(count: 5);
                }
            }
            """;

        var result = await ApplyAsync(source, Rewrite(ParameterRename("TestNs.Target", ".ctor", "count", "indexCount")));

        // Named arguments on the matching constructor are renamed (explicit and implicit creation)...
        Assert.Contains("new Target(indexCount: 5)", result);
        Assert.Contains("new(indexCount: 5)", result);
        // ...while positional arguments and the unrelated same-shaped constructor are untouched.
        Assert.Contains("new Target(count);", result);
        Assert.Contains("new Other(count: 5)", result);
    }

    [Fact]
    public async Task ParameterRenameRenamesNamedArgumentOnMethod()
    {
        var source = """
            namespace TestNs
            {
                class Target
                {
                    public void Fill(int count) { }
                    public void Fill(string other) { }
                }
                class Usage
                {
                    void A(Target t) => t.Fill(count: 3);
                    void B(Target t) => t.Fill(other: "x");
                    void C(Target t) => t?.Fill(count: 3);
                }
            }
            """;

        var result = await ApplyAsync(source, Rewrite(ParameterRename("TestNs.Target", "Fill", "count", "newCount")));

        Assert.Contains("t.Fill(newCount: 3)", result);
        Assert.Contains("t?.Fill(newCount: 3)", result);
        // The overload without the renamed parameter is untouched.
        Assert.Contains("t.Fill(other: \"x\")", result);
    }

    [Theory]
    [InlineData(DocumentationMode.Parse)]
    [InlineData(DocumentationMode.None)]
    public async Task RemoveUnusedUsingsRemovesOnlyListedAndUnusedDirectives(DocumentationMode documentationMode)
    {
        // Namespaces are declared so they resolve, standing in for the from-version closure (where e.g.
        // SharpDX still exists). Only the unused directive matching the dropped list may be removed.
        var source = """
            using DroppedNs;
            using DroppedNs.Sub;
            using DroppedNs.Documented;
            using System.Text;

            namespace DroppedNs { public class A { } }
            namespace DroppedNs.Sub { public class B { } }
            namespace DroppedNs.Documented { public class C { } }
            namespace TestNs
            {
                /// <summary>Documented with <see cref="C"/>.</summary>
                class Usage
                {
                    A a; // keeps `using DroppedNs;` in use
                }
            }
            """;

        var result = await ApplyAsync(source, documentationMode, RemoveUnusedUsings("DroppedNs"));

        // The unused directive of the dropped namespace family is removed (sub-namespaces included)...
        Assert.DoesNotContain("using DroppedNs.Sub;", result);
        // ...the used ones stay (that code needs manual porting; removing the using would only obscure it), a cref is a use,
        Assert.Contains("using DroppedNs;", result);
        Assert.Contains("using DroppedNs.Documented;", result);
        // ...and unused directives outside the dropped list are none of our business.
        Assert.Contains("using System.Text;", result);
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

    // The #1715 shape: memory helpers left Utilities for MemoryUtilities (same namespace), some renamed.
    private const string MovedMembersSource = """
        namespace TestNs
        {
            static class Old
            {
                public static nint AllocateMemory(int sizeInBytes, int align = 16) => 0;
                public static void FreeMemory(nint buffer) { }
                public static void Swap<T>(ref T left, ref T right) { }
                public static void Dispose(object o) { }
            }
            static class Other
            {
                public static void FreeMemory(nint buffer) { }
            }
        }
        """;

    private static readonly CodeUpgrade MoveUpgrade = Rewrite(
        StaticMemberMove("TestNs.Old", "AllocateMemory", "TestNs.New", "Allocate"),
        StaticMemberMove("TestNs.Old", "FreeMemory", "TestNs.New", "Free"),
        StaticMemberMove("TestNs.Old", "Swap", "TestNs.New"),
        ParameterRename("TestNs.Old", "AllocateMemory", "align", "alignment"));

    [Fact]
    public async Task StaticMemberMoveRetargetsOnlyTheMatchingSymbol()
    {
        var source = MovedMembersSource + """

            namespace TestNs
            {
                class Usage
                {
                    void A(nint p) => Old.FreeMemory(p);
                    void B(nint p) => Other.FreeMemory(p);
                    void C(object o) => Old.Dispose(o);
                    void D(nint p) => TestNs.Old.FreeMemory(p);
                    void E(nint p) => global::TestNs.Old.FreeMemory(p);
                    System.Action<nint> F() => Old.FreeMemory;
                    string G() => nameof(Old.FreeMemory);
                    void H(ref int a, ref int b) => Old.Swap<int>(ref a, ref b);
                }
            }
            """;

        var result = await ApplyAsync(source, MoveUpgrade);

        Assert.Contains("=> New.Free(p);", result);
        // The qualified receiver keeps its shape.
        Assert.Contains("=> TestNs.New.Free(p);", result);
        Assert.Contains("=> global::TestNs.New.Free(p);", result);
        // Method groups, nameof and generic names are references too.
        Assert.Contains("=> New.Free;", result);
        Assert.Contains("nameof(New.Free)", result);
        Assert.Contains("New.Swap<int>(ref a, ref b)", result);
        // The unrelated same-named member and the members that stayed are untouched.
        Assert.Contains("Other.FreeMemory(p)", result);
        Assert.Contains("Old.Dispose(o)", result);
    }

    [Fact]
    public async Task StaticMemberMoveComposesWithParameterRename()
    {
        // Both rewrites hit the same reference (the method name) but edit disjoint nodes.
        var source = MovedMembersSource + """

            namespace TestNs
            {
                class Usage
                {
                    nint A() => Old.AllocateMemory(64, align: 32);
                }
            }
            """;

        var result = await ApplyAsync(source, MoveUpgrade);

        Assert.Contains("New.Allocate(64, alignment: 32)", result);
    }

    [Fact]
    public async Task StaticMemberMoveQualifiesAliasAndUsingStaticReferences()
    {
        // Neither form names the old type at the call site, so the new type can't be assumed in scope.
        var source = """
            using static TestNs.Old;
            using Alias = TestNs.Old;

            """ + MovedMembersSource + """

            namespace UserNs
            {
                class Usage
                {
                    void A(nint p) => FreeMemory(p);
                    void B(nint p) => Alias.FreeMemory(p);
                }
            }
            """;

        var result = await ApplyAsync(source, MoveUpgrade);

        Assert.Contains("void A(nint p) => TestNs.New.Free(p);", result);
        Assert.Contains("void B(nint p) => TestNs.New.Free(p);", result);
    }

    [Fact]
    public async Task StaticMemberMoveRetargetsDocumentationReferences()
    {
        // A cref is qualified as code is, and keeps its parameters
        var source = """
            using static TestNs.Old;
            using Alias = TestNs.Old;

            """ + MovedMembersSource + """

            namespace TestNs
            {
                static class New { public static void Free(nint buffer) { } }

                /// <summary>Frees with <see cref="Old.FreeMemory"/> or <see cref="TestNs.Old.FreeMemory(nint)"/>.</summary>
                class Usage { }
            }
            namespace UserNs
            {
                /// <summary>Frees with <see cref="FreeMemory"/> or <see cref="Alias.FreeMemory"/>, not <see cref="TestNs.Other.FreeMemory"/>.</summary>
                class Usage { }
            }
            """;

        var result = await ApplyAsync(source, DocumentationMode.None, MoveUpgrade);

        Assert.Contains("""/// <summary>Frees with <see cref="New.Free"/> or <see cref="TestNs.New.Free(nint)"/>.</summary>""", result);
        Assert.Contains("""/// <summary>Frees with <see cref="TestNs.New.Free"/> or <see cref="TestNs.New.Free"/>, not <see cref="TestNs.Other.FreeMemory"/>.</summary>""", result);
        AssertCompiles(result);
    }

    [Fact]
    public async Task MemberRenameRenamesOnlyTheMatchingMember()
    {
        // The 4.5.1 shape: CommandList.UnsetRenderTargets became ResetTargets.
        var source = """
            namespace TestNs
            {
                class Target
                {
                    public void UnsetRenderTargets() { }
                    public void ResetTargets() { }
                }
                class Other
                {
                    public void UnsetRenderTargets() { }
                }
                class Usage
                {
                    void A(Target t)
                    {
                        t.UnsetRenderTargets();
                        t?.UnsetRenderTargets();
                        System.Action action = t.UnsetRenderTargets;
                    }
                    void B(Other o) => o.UnsetRenderTargets();
                }
            }
            """;

        var result = await ApplyAsync(source, Rewrite(MemberRename("TestNs.Target", "UnsetRenderTargets", "ResetTargets")));

        Assert.Contains("t.ResetTargets();", result);
        Assert.Contains("t?.ResetTargets();", result);
        Assert.Contains("System.Action action = t.ResetTargets;", result);
        // The unrelated same-named method is untouched.
        Assert.Contains("o.UnsetRenderTargets()", result);
        AssertCompiles(result);
    }

    [Theory]
    [InlineData(DocumentationMode.Parse)]
    [InlineData(DocumentationMode.None)]
    public async Task MemberRenameRenamesDocumentationReferences(DocumentationMode documentationMode)
    {
        // None: a project that generates no documentation file, whose documentation comments are read as text unless parsed for the upgrade
        var source = """
            namespace TestNs
            {
                class Target
                {
                    public void UnsetRenderTargets() { }
                    /// <summary>Same as <see cref="UnsetRenderTargets"/>.</summary>
                    public void ResetTargets() { }
                }
                class Other
                {
                    public void UnsetRenderTargets() { }
                }
                /// <summary>Calls <see cref="Target.UnsetRenderTargets"/> and <see cref="TestNs.Target.UnsetRenderTargets()"/>, not <see cref="Other.UnsetRenderTargets"/>.</summary>
                class Usage { }
            }
            """;

        var result = await ApplyAsync(source, documentationMode, Rewrite(MemberRename("TestNs.Target", "UnsetRenderTargets", "ResetTargets")));

        Assert.Contains("""/// <summary>Same as <see cref="ResetTargets"/>.</summary>""", result);
        Assert.Contains("""/// <summary>Calls <see cref="Target.ResetTargets"/> and <see cref="TestNs.Target.ResetTargets()"/>, not <see cref="Other.UnsetRenderTargets"/>.</summary>""", result);
        AssertCompiles(result);
    }

    [Fact]
    public async Task PropertyAndMethodSwapsKeepDocumentationReferences()
    {
        // A cref names a property and a method the same way, without the parameter list a method may have
        var source = """
            namespace TestNs
            {
                class Target
                {
                    public bool IsSRgb => true;
                    public bool IsHdr() => true;
                }
                /// <summary>Reads <see cref="Target.IsSRgb"/>, <see cref="Target.IsHdr()"/> and <see cref="Target.IsHdr"/>.</summary>
                class Usage
                {
                    bool A(Target t) => t.IsSRgb && t.IsHdr();
                }
            }
            """;

        var result = await ApplyAsync(source, DocumentationMode.None, Rewrite(PropertyToMethod("TestNs.Target", "IsSRgb"), MethodToProperty("TestNs.Target", "IsHdr")));

        Assert.Contains("""/// <summary>Reads <see cref="Target.IsSRgb"/>, <see cref="Target.IsHdr"/> and <see cref="Target.IsHdr"/>.</summary>""", result);
        Assert.Contains("bool A(Target t) => t.IsSRgb() && t.IsHdr;", result);
    }

    [Fact]
    public async Task FluentCallRemoveDropsTheCallsOfAMethodReturningNothing()
    {
        // The 4.5.1 shape: CommandList.ResetQueryPool went, the pools reset themselves.
        var source = """
            namespace TestNs
            {
                class Pool { }
                class Target
                {
                    public static Target Current() => new();
                    public void ResetQueryPool(Pool pool) { }
                }
                class Other
                {
                    public void ResetQueryPool(Pool pool) { }
                }
                class Usage
                {
                    void A(Target t, Pool p)
                    {
                        t.ResetQueryPool(p);
                        t?.ResetQueryPool(p);
                        Target.Current().ResetQueryPool(p);
                    }
                    void B(Target t, Pool p) => t.ResetQueryPool(p);
                    System.Action C(Target t, Pool p) => () => t.ResetQueryPool(p);
                    void D(Other o, Pool p) => o.ResetQueryPool(p);
                }
            }
            """;

        var result = await ApplyAsync(source, Rewrite(FluentCallRemove("TestNs.Target", "ResetQueryPool")));

        Assert.DoesNotContain("t.ResetQueryPool", result);
        Assert.DoesNotContain("t?.ResetQueryPool", result);
        // A receiver that is a call keeps its side effects.
        Assert.Contains("Target.Current();", result);
        // The unrelated same-named method is untouched.
        Assert.Contains("o.ResetQueryPool(p)", result);
        AssertCompiles(result);
    }

    [Fact]
    public async Task FluentCallRemoveDropsTheCallsOfEveryOverload()
    {
        // The 4.5.1 shape: CommandList.SetStreamTargets went, with a span overload on Direct3D 11 and an array one elsewhere.
        var source = """
            namespace TestNs
            {
                class Buffer { }
                class Target
                {
                    public void SetStreamTargets(params System.ReadOnlySpan<Buffer> buffers) { }
                    public void SetStreamTargets(params Buffer[] buffers) { }
                }
                class Usage
                {
                    void A(Target t, Buffer b, Buffer[] all)
                    {
                        t.SetStreamTargets(b);
                        t.SetStreamTargets(all);
                        t.SetStreamTargets();
                        t.SetStreamTargets(new System.ReadOnlySpan<Buffer>(all));
                    }
                }
            }
            """;

        var result = await ApplyAsync(source, Rewrite(FluentCallRemove("TestNs.Target", "SetStreamTargets")));

        Assert.DoesNotContain("t.SetStreamTargets", result);
        AssertCompiles(result);
    }

    [Fact]
    public async Task FluentCallRemoveKeepsTheReceiver()
    {
        // The 4.4 shape: Buffer.RecreateWith returned its receiver and went away with the device reset recovery.
        var source = """
            namespace TestNs
            {
                class Target
                {
                    public static Target New() => new();
                    public static Target Make() => new();
                    public Target RecreateWith(int[] data) => this;
                }
                class Other
                {
                    public Other RecreateWith(int[] data) => this;
                }
                class Usage
                {
                    Target A(int[] d) => Target.New().RecreateWith(d);
                    Target B(Target t, int[] d) => t?.RecreateWith(d);
                    Other C(Other o, int[] d) => o.RecreateWith(d);
                    void D(Target t, int[] d)
                    {
                        t.RecreateWith(d);
                        t?.RecreateWith(d);
                        Target.Make().RecreateWith(d);
                        if (t != null) t.RecreateWith(d);
                        for (t.RecreateWith(d); d.Length > 0; t.RecreateWith(d)) { }
                    }
                    void E(Target t, int[] d) => t.RecreateWith(d);
                    Target F(Target t, int[] d) => t.RecreateWith(d);
                    System.Action G(Target t, int[] d) => () => t.RecreateWith(d);
                    System.Func<Target> H(Target t, int[] d) => () => t.RecreateWith(d);
                    async System.Threading.Tasks.Task I(Target t, int[] d) => t.RecreateWith(d);
                }
            }
            """;

        var result = await ApplyAsync(source, Rewrite(FluentCallRemove("TestNs.Target", "RecreateWith")));

        Assert.Contains("=> Target.New();", result);
        Assert.Contains("=> t;", result);
        // As a statement, a bare receiver goes with the call, and a call receiver stays.
        Assert.DoesNotContain("t.RecreateWith", result);
        Assert.DoesNotContain("t?.RecreateWith", result);
        Assert.Contains("Target.Make();", result);
        Assert.Contains("if (t != null)", result);
        // Where the value is used, the receiver stays; where it is discarded, the call goes.
        Assert.Contains("Target F(Target t, int[] d) => t;", result);
        Assert.Contains("System.Func<Target> H(Target t, int[] d) => () => t;", result);
        // The unrelated same-named method is untouched.
        Assert.Contains("o.RecreateWith(d)", result);
        AssertCompiles(result);
    }

    [Fact]
    public async Task AssignmentRemoveDropsAssignmentsAndSubscriptions()
    {
        // The 4.4 shape: GraphicsResourceBase.Reload and the DeviceReset events are gone; user code only ever
        // assigned or subscribed to them.
        var source = """
            namespace TestNs
            {
                class Target
                {
                    public System.Action Reload;
                    public event System.EventHandler DeviceReset;
                }
                class Other
                {
                    public System.Action Reload;
                }
                class Usage
                {
                    void A(Target t)
                    {
                        t.Reload = () => { };
                        t.DeviceReset += (s, e) => { };
                        t.DeviceReset -= (s, e) => { };
                        if (t.Reload != null) t.Reload();
                        if (t != null) t.DeviceReset += (s, e) => { };
                        else t.Reload = null;
                    }
                    void B(Other o)
                    {
                        o.Reload = () => { };
                    }
                }
            }
            """;

        var result = await ApplyAsync(source, Rewrite(
            AssignmentRemove("TestNs.Target", "Reload"),
            AssignmentRemove("TestNs.Target", "DeviceReset")));

        Assert.DoesNotContain("t.Reload = ", result);
        Assert.DoesNotContain("t.DeviceReset +=", result);
        Assert.DoesNotContain("t.DeviceReset -=", result);
        // A read is left for the user to port, and the unrelated same-named member is untouched.
        Assert.Contains("if (t.Reload != null) t.Reload();", result);
        Assert.Contains("o.Reload = ", result);
        // The body of an if or else without braces cannot be removed: it becomes an empty block.
        Assert.Contains("if (t != null)", result);
        Assert.Contains("else", result);
        AssertCompiles(result);
    }

    [Fact]
    public async Task AssignmentRemoveDropsInitializerEntries()
    {
        // The 4.5 shape: BepuSimulation.MaxStepPerFrame is gone; user code may set it in an object initializer.
        var source = """
            namespace TestNs
            {
                class Target
                {
                    public int MaxStepPerFrame;
                    public int Other;
                }
                record Rec
                {
                    public int MaxStepPerFrame { get; init; }
                    public int Other { get; init; }
                }
                class Holder
                {
                    public Target Inner = new Target();
                }
                class Unrelated
                {
                    public int MaxStepPerFrame;
                }
                class Usage
                {
                    object Only() => new Target { MaxStepPerFrame = 3 };
                    object First() => new Target { MaxStepPerFrame = 3, Other = 1 };
                    object Last() => new Target { Other = 2, MaxStepPerFrame = 3 };
                    Target Implicit() => new() { Other = 3, MaxStepPerFrame = 3, };
                    object MultiLine() => new Target
                    {
                        Other = 4,
                        MaxStepPerFrame = 3,
                    };
                    object Nested() => new Holder { Inner = { MaxStepPerFrame = 3, Other = 5 } };
                    Rec With(Rec r) => r with { MaxStepPerFrame = 3, Other = 6 };
                    object Keep() => new Unrelated { MaxStepPerFrame = 7 };
                }
            }
            """;

        var result = await ApplyAsync(source, Rewrite(
            AssignmentRemove("TestNs.Target", "MaxStepPerFrame"),
            AssignmentRemove("TestNs.Rec", "MaxStepPerFrame")));

        Assert.DoesNotContain("MaxStepPerFrame = 3", result);
        // The other entries stay, wherever the removed one sat.
        for (var i = 1; i <= 6; i++)
            Assert.Contains($"Other = {i}", result);
        // The unrelated same-named member is untouched.
        Assert.Contains("new Unrelated { MaxStepPerFrame = 7 }", result);
        AssertCompiles(result);
    }

    // The 4.5 shape: types with hand-written ParameterKey fields get [EffectKeys], which does not exist at the old version.
    private const string ParameterKeySource = """
        namespace Stride.Rendering
        {
            public abstract class ParameterKey { }
            public sealed class ValueParameterKey<T> : ParameterKey { }
            public static class ParameterKeys { public static ValueParameterKey<T> NewValue<T>() => null; }
        }

        """;

    private const string EffectKeysAttributeSource = """

        namespace Stride.Rendering
        {
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
            public sealed class EffectKeysAttribute : System.Attribute { }
        }
        """;

    private static readonly CodeUpgrade EffectKeysUpgrade = AddAttributeToStaticFieldHolders("Stride.Rendering.ParameterKey", "Stride.Rendering.EffectKeysAttribute");

    [Fact]
    public async Task AddAttributeToStaticFieldHoldersMarksTypesWithStaticKeyFields()
    {
        var source = ParameterKeySource + """
            namespace Game
            {
                using Stride.Rendering;

                /// <summary>Keys.</summary>
                public static class MyKeys
                {
                    public static readonly ValueParameterKey<float> Intensity = ParameterKeys.NewValue<float>();
                }

                [System.Serializable]
                public struct MyStruct
                {
                    private static readonly ValueParameterKey<int> Count = ParameterKeys.NewValue<int>();
                }

                public class Outer
                {
                    public class Nested
                    {
                        public static ValueParameterKey<float> Key;
                    }
                }

                public partial class Split
                {
                    public static ValueParameterKey<float> First;
                }

                public partial class Split
                {
                    public static ValueParameterKey<float> Second;
                }

                public class NotKeys
                {
                    public ValueParameterKey<float> Instance;
                    public static string Text;
                }

                [EffectKeys]
                public class AlreadyMarked
                {
                    public static ValueParameterKey<float> Key;
                }
            }
            """;

        var result = await ApplyAsync(source, EffectKeysUpgrade);

        var expected = ParameterKeySource + """
            namespace Game
            {
                using Stride.Rendering;

                /// <summary>Keys.</summary>
                [EffectKeys]
                public static class MyKeys
                {
                    public static readonly ValueParameterKey<float> Intensity = ParameterKeys.NewValue<float>();
                }

                [EffectKeys]
                [System.Serializable]
                public struct MyStruct
                {
                    private static readonly ValueParameterKey<int> Count = ParameterKeys.NewValue<int>();
                }

                public class Outer
                {
                    [EffectKeys]
                    public class Nested
                    {
                        public static ValueParameterKey<float> Key;
                    }
                }

                [EffectKeys]
                public partial class Split
                {
                    public static ValueParameterKey<float> First;
                }

                public partial class Split
                {
                    public static ValueParameterKey<float> Second;
                }

                public class NotKeys
                {
                    public ValueParameterKey<float> Instance;
                    public static string Text;
                }

                [EffectKeys]
                public class AlreadyMarked
                {
                    public static ValueParameterKey<float> Key;
                }
            }
            """;
        Assert.Equal(expected.ReplaceLineEndings(), result.ReplaceLineEndings());
        AssertCompiles(result + EffectKeysAttributeSource);
    }

    [Fact]
    public async Task AddAttributeToStaticFieldHoldersQualifiesTheAttributeOutOfScopeAndIsIdempotent()
    {
        var source = ParameterKeySource + """
            namespace Game
            {
                public class MyKeys
                {
                    public static readonly Stride.Rendering.ValueParameterKey<float> Intensity = Stride.Rendering.ParameterKeys.NewValue<float>();
                }
            }
            """;

        // The second run sees the attribute written by the first one
        var result = await ApplyAsync(source, EffectKeysUpgrade, EffectKeysUpgrade);

        Assert.Contains("""
                [Stride.Rendering.EffectKeys]
                public class MyKeys
            """.ReplaceLineEndings(), result.ReplaceLineEndings());
        Assert.Equal(1, result.Split("[Stride.Rendering.EffectKeys]").Length - 1);
        AssertCompiles(result + EffectKeysAttributeSource);
    }

    // The Dispatcher before 4.5, with the overloads whose delegates the assembly processor pooled
    private const string OldDispatcherSource = """
        using System.Collections.Generic;
        using Stride.Core.Threading;

        namespace Stride.Core.Threading
        {
            public static class Dispatcher
            {
                public static void For(int fromInclusive, int toExclusive, System.Action<int> action) { }
                public static void For<TLocal>(int fromInclusive, int toExclusive, System.Func<TLocal> initializeLocal, System.Action<int, TLocal> action, System.Action<TLocal> finalizeLocal = null) { }
                public static void ForBatched(int items, System.Action<int, int> executeBatch) { }
                public static void ForEach<T>(T[] collection, System.Action<T> action) { }
                public static void ForEach<T>(System.Collections.Generic.List<T> collection, System.Action<T> action) { }
                public static void ForEach<T, TList>(TList collection, System.Action<T> action) where TList : System.Collections.Generic.IReadOnlyList<T> { }
                public static void ForEach<TKey, TValue>(System.Collections.Generic.Dictionary<TKey, TValue> collection, System.Action<System.Collections.Generic.KeyValuePair<TKey, TValue>> action) { }
                public static void ForEach<TItem, TLocal, TList>(TList collection, System.Func<TLocal> initializeLocal, System.Action<TItem, TLocal> action, System.Action<TLocal> finalizeLocal = null) where TList : System.Collections.Generic.IReadOnlyList<TItem> { }
                public static void ForEach<TKey, TValue, TLocal>(System.Collections.Generic.Dictionary<TKey, TValue> collection, System.Func<TLocal> initializeLocal, System.Action<System.Collections.Generic.KeyValuePair<TKey, TValue>, TLocal> action, System.Action<TLocal> finalizeLocal = null) { }
            }
        }

        """;

    // The Dispatcher after: the overloads taking a state
    private const string NewDispatcherSource = """
        using System.Collections.Generic;
        using Stride.Core.Threading;

        namespace Stride.Core.Threading
        {
            public static class Dispatcher
            {
                public static void For<TState>(int fromInclusive, int toExclusive, TState state, System.Action<TState, int> action) { }
                public static void ForBatched<TState>(int items, TState state, System.Action<TState, int, int> executeBatch) { }
                public static void ForEach<T, TState>(System.Collections.Generic.IReadOnlyList<T> collection, TState state, System.Action<TState, T> action) { }
            }
        }

        """;

    private static readonly CodeUpgrade DispatcherUpgrade = DispatcherCallbacksToStaticLambdas();

    [Fact]
    public async Task DispatcherCallbacksMoveCapturesToTheState()
    {
        var source = OldDispatcherSource + """
            namespace Game
            {
                public class Processor
                {
                    private readonly List<int> items = new();
                    private int total;

                    private void Process(int value) { }

                    public void Update(int[] hits)
                    {
                        Dispatcher.For(0, 10, i => Process(i));
                        Dispatcher.For(0, 10, i => this.Process(i));
                        Dispatcher.For(0, 10, i => total += i);
                        Dispatcher.For(0, 10, i => System.Console.WriteLine(i));
                        var scale = 2;
                        Dispatcher.ForEach(items, item => Process(item * scale));
                        Dispatcher.ForEach<int, List<int>>(items, delegate (int item) { Process(item); });
                        Dispatcher.ForBatched(hits.Length, (from, toExclusive) =>
                        {
                            for (int i = from; i < toExclusive; i++)
                                hits[i]++;
                        });
                        Dispatcher.ForEach(items, Process);
                    }
                }
            }
            """;

        var result = await ApplyAsync(source, DispatcherUpgrade);

        var expected = OldDispatcherSource + """
            namespace Game
            {
                public class Processor
                {
                    private readonly List<int> items = new();
                    private int total;

                    private void Process(int value) { }

                    public void Update(int[] hits)
                    {
                        Dispatcher.For(0, 10, this, static (state, i) => state.Process(i));
                        Dispatcher.For(0, 10, this, static (state, i) => state.Process(i));
                        Dispatcher.For(0, 10, this, static (state, i) => state.total += i);
                        Dispatcher.For(0, 10, default(object), static (_, i) => System.Console.WriteLine(i));
                        var scale = 2;
                        Dispatcher.ForEach(items, (self: this, scale), static (state, item) => state.self.Process(item * state.scale));
                        Dispatcher.ForEach(items, this, static (state, item) => { state.Process(item); });
                        Dispatcher.ForBatched(hits.Length, hits, static (state, from, toExclusive) =>
                        {
                            for (int i = from; i < toExclusive; i++)
                                state[i]++;
                        });
                        Dispatcher.ForEach(items, this, static (state, value) => state.Process(value));
                    }
                }
            }
            """;
        Assert.Equal(expected.ReplaceLineEndings(), result.ReplaceLineEndings());
        AssertCompiles(result.Replace(OldDispatcherSource, NewDispatcherSource));
    }

    [Fact]
    public async Task DispatcherCallbacksThatCannotMoveAreLeftAlone()
    {
        var user = """
            namespace Game
            {
                public class Processor
                {
                    private readonly List<int> items = new();

                    public void Update(Dictionary<int, int> map, System.Action<int> callback)
                    {
                        // Writes a captured local
                        var count = 0;
                        Dispatcher.For(0, 10, i => count++);
                        // A dictionary with a per-batch local: a loop would index it by key
                        Dispatcher.ForEach(map, () => new List<int>(), (pair, list) => list.Add(pair.Key));
                        // A per-batch local with a range that doesn't start at a constant
                        Dispatcher.For(count, 10, () => new List<int>(), (i, list) => list.Add(i));
                        // Writes its per-item parameter, which the loop would share
                        Dispatcher.For(0, 10, () => new List<int>(), (i, list) => list = null);
                        // Not a lambda nor a method
                        Dispatcher.For(0, 10, callback);
                        // Calls a local function that captures
                        void Log(int i) => System.Console.WriteLine(i + count);
                        Dispatcher.For(0, 10, i => Log(i));
                    }
                }
            }
            """;
        var source = OldDispatcherSource + user;

        var result = await ApplyAsync(source, DispatcherUpgrade);

        Assert.Equal(source.ReplaceLineEndings(), result.ReplaceLineEndings());
    }

    [Fact]
    public async Task DispatcherNestedCallbacksMoveTheInnerOne()
    {
        var source = OldDispatcherSource + """
            namespace Game
            {
                public class Processor
                {
                    private readonly List<List<int>> groups = new();

                    private void Process(int value) { }

                    public void Update()
                    {
                        Dispatcher.ForEach(groups, group => Dispatcher.ForEach(group, item => Process(item)));
                    }
                }
            }
            """;

        var result = await ApplyAsync(source, DispatcherUpgrade);

        Assert.Contains("Dispatcher.ForEach(groups, group => Dispatcher.ForEach(group, this, static (state, item) => state.Process(item)));", result);
    }

    [Fact]
    public async Task DispatcherLocalCallbacksBecomeForBatched()
    {
        var source = OldDispatcherSource + """
            namespace Game
            {
                public class Processor
                {
                    private readonly List<int> items = new();
                    private readonly Stack<List<int>> pool = new();

                    private void Process(int value, List<int> scratch) { }

                    public void Update(int count)
                    {
                        Dispatcher.For(0, count, () => pool.Pop(), (i, scratch) =>
                        {
                            if (i % 2 == 0)
                                return;
                            Process(i, scratch);
                        }, scratch => pool.Push(scratch));
                        Dispatcher.ForEach<int, List<int>, List<int>>(items, () => new List<int>(), (item, scratch) => Process(item, scratch));
                    }
                }
            }
            """;

        var result = await ApplyAsync(source, DispatcherUpgrade);

        var expected = OldDispatcherSource + """
            namespace Game
            {
                public class Processor
                {
                    private readonly List<int> items = new();
                    private readonly Stack<List<int>> pool = new();

                    private void Process(int value, List<int> scratch) { }

                    public void Update(int count)
                    {
                        Dispatcher.ForBatched(count, this, static (state, start, endExclusive) =>
                        {
                            var scratch = state.pool.Pop();
                            try
                            {
                                for (int i = start; i < endExclusive; i++)
                                {
                                    if (i % 2 == 0)
                                        continue;
                                    state.Process(i, scratch);
                                }
                            }
                            finally
                            {
                                state.pool.Push(scratch);
                            }
                        });
                        Dispatcher.ForBatched(items.Count, (self: this, collection: items), static (state, start, endExclusive) =>
                        {
                            var scratch = new List<int>();
                            for (int index = start; index < endExclusive; index++)
                            {
                                var item = state.collection[index];
                                state.self.Process(item, scratch);
                            }
                        });
                    }
                }
            }
            """;
        Assert.Equal(expected.ReplaceLineEndings(), result.ReplaceLineEndings());
        AssertCompiles(result.Replace(OldDispatcherSource, NewDispatcherSource));
    }

    [Fact]
    public async Task DispatcherDictionaryCallbackMovesButNoLongerCompiles()
    {
        var source = OldDispatcherSource + """
            namespace Game
            {
                public class Processor
                {
                    private void Process(int key) { }

                    public void Update(Dictionary<int, string> map)
                    {
                        Dispatcher.ForEach(map, pair => Process(pair.Key));
                    }
                }
            }
            """;

        var result = await ApplyAsync(source, DispatcherUpgrade);

        Assert.Contains("Dispatcher.ForEach(map, this, static (state, pair) => state.Process(pair.Key));", result);
        // The author picks a collection with an index
        var syntaxTree = CSharpSyntaxTree.ParseText(result.Replace(OldDispatcherSource, NewDispatcherSource), new CSharpParseOptions(LanguageVersion.Preview));
        var compilation = CSharpCompilation.Create("Check", [syntaxTree], FrameworkReferences(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Assert.Contains(compilation.GetDiagnostics(), x => x.Severity == DiagnosticSeverity.Error && x.Location.SourceTree.GetText().ToString(x.Location.SourceSpan).Contains("ForEach"));
    }

    [Fact]
    public async Task InterfaceMemberAddCompletesOnlyClassesThatImplementTheInterfaceThemselves()
    {
        // The 4.5.3 shape: IInputDevice gained CaptureState, which the engine base classes already provide
        var source = """
            namespace Input
            {
                public sealed class DeviceCaptureState { }
                public interface IInputDevice { string Name { get; } }
                public interface IMouseDevice : IInputDevice { }
                public abstract class DeviceBase : IInputDevice { public string Name => "Base"; }
            }
            namespace TestNs
            {
                class Mouse : Input.IMouseDevice
                {
                    public string Name => "Mouse";

                    void Update() { }
                }
                class DerivedMouse : Mouse { }
                class FromBase : Input.DeviceBase { }
                class Already : Input.IInputDevice
                {
                    public string Name => "Already";
                    public Input.DeviceCaptureState CaptureState => null;
                }
                partial class Split : Input.IInputDevice { }
                partial class Split { public string Name => "Split"; }
                class Other { public string Name => "Other"; }
            }
            """;

        var result = await ApplyAsync(source, InterfaceMemberAdd("Input.IInputDevice", "public DeviceCaptureState CaptureState { get; } = new DeviceCaptureState();", "Input"));

        Assert.StartsWith("using Input;", result);
        // DeviceBase, Mouse and one declaration of Split get the member; a derived class, a class deriving from the base and one that already has it don't
        Assert.Equal(3, result.Split("public DeviceCaptureState CaptureState { get; } = new DeviceCaptureState();").Length - 1);
        Assert.Contains("""
                    public string Name => "Mouse";
                    public DeviceCaptureState CaptureState { get; } = new DeviceCaptureState();

                    void Update() { }
            """, result);
        Assert.Contains("class DerivedMouse : Mouse { }", result);
        Assert.Contains("class FromBase : Input.DeviceBase { }", result);
        Assert.Contains("class Other { public string Name => \"Other\"; }", result);
        AssertCompiles(result);
    }

    [Fact]
    public async Task PropertyOverrideRenameMovesOverridesToTheNewProperty()
    {
        // The 4.5.3 shape: GamePadDeviceBase.State became non-virtual and subclasses override RawState instead
        const string OldBase = "public abstract class GamePadDeviceBase { public abstract int State { get; } }";
        const string NewBase = "public abstract class GamePadDeviceBase { public int State => RawState; protected abstract int RawState { get; } }";
        var source = $$"""
            namespace Input
            {
                {{OldBase}}
            }
            namespace TestNs
            {
                class Pad : Input.GamePadDeviceBase
                {
                    private int state;

                    public override int State => state;
                }
                class DerivedPad : Pad
                {
                    public override int State => base.State + 1;
                    public int Read() => State;
                }
                abstract class OtherBase { public abstract int State { get; } }
                class Other : OtherBase { public override int State => 0; }
            }
            """;

        var result = await ApplyAsync(source, PropertyOverrideRename("Input.GamePadDeviceBase", "State", "RawState", SyntaxKind.ProtectedKeyword));

        Assert.Contains("protected override int RawState => state;", result);
        // base.State would now call the public State, which calls RawState again
        Assert.Contains("protected override int RawState => base.RawState + 1;", result);
        // Reads of State and overrides of an unrelated State are left alone
        Assert.Contains("public int Read() => State;", result);
        Assert.Contains("class Other : OtherBase { public override int State => 0; }", result);
        AssertCompiles(result.Replace(OldBase, NewBase));
    }

    [Fact]
    public async Task PropertyOverrideRenameMovesAssignmentsToAGetOnlyOverride()
    {
        // GamePadSimulated before 4.5.3: a get-only auto-property override that the class assigns in its constructor
        const string OldBase = "public abstract class GamePadDeviceBase { public abstract int State { get; } }";
        const string NewBase = "public abstract class GamePadDeviceBase { public int State => RawState; protected abstract int RawState { get; } }";
        var source = $$"""
            namespace Input
            {
                {{OldBase}}
            }
            namespace TestNs
            {
                class Pad : Input.GamePadDeviceBase
                {
                    public Pad(bool reset)
                    {
                        State = 1;
                        if (reset)
                            this.State = 0;
                    }

                    public override int State { get; }
                }
            }
            """;

        var result = await ApplyAsync(source, PropertyOverrideRename("Input.GamePadDeviceBase", "State", "RawState", SyntaxKind.ProtectedKeyword));

        Assert.Contains("protected override int RawState { get; }", result);
        Assert.Contains("RawState = 1;", result);
        Assert.Contains("this.RawState = 0;", result);
        AssertCompiles(result.Replace(OldBase, NewBase));
    }

    [Fact]
    public async Task MethodOverrideRenameMovesOverridesToTheNewMethod()
    {
        // The 4.5.4 shape: TouchController.IsPressed became non-virtual and backends override IsRawPressed instead
        const string OldBase = "public abstract class TouchController { public abstract bool IsPressed(int button); }";
        const string NewBase = "public abstract class TouchController { public bool IsPressed(int button) => IsRawPressed(button); protected abstract bool IsRawPressed(int button); }";
        var source = $$"""
            namespace VR
            {
                {{OldBase}}
            }
            namespace TestNs
            {
                class Hand : VR.TouchController
                {
                    public override bool IsPressed(int button) => button == 0;
                }
                class DerivedHand : Hand
                {
                    public override bool IsPressed(int button) => base.IsPressed(button) || button == 1;
                    public bool Read() => IsPressed(2);
                }
                abstract class OtherBase { public abstract bool IsPressed(int button); }
                class Other : OtherBase { public override bool IsPressed(int button) => false; }
            }
            """;

        var result = await ApplyAsync(source, MethodOverrideRename("VR.TouchController", "IsPressed", "IsRawPressed", SyntaxKind.ProtectedKeyword));

        Assert.Contains("protected override bool IsRawPressed(int button) => button == 0;", result);
        // base.IsPressed would now call the public IsPressed, which calls IsRawPressed again
        Assert.Contains("protected override bool IsRawPressed(int button) => base.IsRawPressed(button) || button == 1;", result);
        // Calls of IsPressed and overrides of an unrelated IsPressed are left alone
        Assert.Contains("public bool Read() => IsPressed(2);", result);
        Assert.Contains("class Other : OtherBase { public override bool IsPressed(int button) => false; }", result);
        AssertCompiles(result.Replace(OldBase, NewBase));
    }

    // A math library, and the one that replaces it: other names, results returned instead of given through out parameters,
    // no method that changes its instance, and a product of quaternions with another meaning
    private const string OldMathLibrary = """
        namespace Old
        {
            public struct Vec
            {
                public float X, Y;
                public Vec(float x, float y) { X = x; Y = y; }
                public Vec(float[] values) { X = values[0]; Y = values[1]; }
                public static readonly Vec Zero = new Vec();
                public void Normalize() { }
                public static Vec Normalize(Vec value) => value;
                public static void Normalize(ref Vec value, out Vec result) { result = value; }
                public static Vec Add(Vec left, Vec right) => left;
                public static void Add(ref Vec left, ref Vec right, out Vec result) { result = left; }
                public static void Split(ref Vec value, out float x, out float y) { x = 0; y = 0; }
                public static void OnlyOut(ref Vec value, out Vec result) { result = value; }
                public static Vec Scale(Vec value, float factor) => value;
                public static void Scale(ref Vec value, float factor, out Vec result) { result = value; }
                public void Clamp() { }
                public static Vec Clamp(Vec value) => value;
                public static Small Shrink(Vec value) => new Small();
                public static void Shrink(ref Vec value, out Small result) { result = new Small(); }
                public static explicit operator Small(Vec value) => new Small();
                public static bool operator ==(Vec left, Vec right) => true;
                public static bool operator !=(Vec left, Vec right) => false;
                public bool Equals(Vec other) => true;
                public bool EqualsStrict(Vec other) => true;
                public override bool Equals(object other) => true;
                public override int GetHashCode() => 0;
                public override string ToString() => "";
                public string ToString(string format, System.IFormatProvider formatProvider) => "";
            }
            public struct Small { public float X; }
            public struct Quat
            {
                public float W;
                public static Quat operator *(Quat left, Quat right) => left;
                public static Quat operator *(Quat left, float right) => left;
                public static Quat Multiply(Quat left, Quat right) => left;
                public static Quat Multiply(Quat left, float right) => left;
                public static void Multiply(ref Quat left, ref Quat right, out Quat result) { result = left; }
            }
            public struct Mat
            {
                public float M11;
                public static Mat RotationX(float angle) => new Mat();
                public static void RotationX(float angle, out Mat result) { result = new Mat(); }
                public static Mat Invert(Mat value) => value;
                public static void Invert(ref Mat value, out Mat result) { result = value; }
                public void Invert() { }
                public Vec TranslationVector { get; set; }
                public Vec Up { get; set; }
                public float this[int index] { get => M11; set => M11 = value; }
            }
        }
        """;

    private const string NewMathLibrary = """
        namespace Old
        {
            public struct Unrelated { }
        }
        namespace New
        {
            public struct Vec
            {
                public float X, Y;
                public Vec(float x, float y) { X = x; Y = y; }
                public static Vec Zero => default;
                public static Vec Create(float[] values) => default;
                public static Vec NormalizeSafe(Vec value) => value;
                public static Vec Add(Vec left, Vec right) => left;
                public static void Split(ref Vec value, out float x, out float y) { x = 0; y = 0; }
                public static void OnlyOut(ref Vec value, out Vec result) { result = value; }
                public Small AsSmall() => default;
                public static bool operator ==(Vec left, Vec right) => true;
                public static bool operator !=(Vec left, Vec right) => false;
                public bool Equals(Vec other) => true;
                public override bool Equals(object other) => true;
                public override int GetHashCode() => 0;
            }
            public struct Small
            {
                public float X;
                public static Small FromVec(Vec value) => default;
            }
            public struct Quat
            {
                public float W;
                public static Quat operator *(Quat left, Quat right) => left;
                public static Quat operator *(Quat left, float right) => left;
                public static Quat Concatenate(Quat left, Quat right) => left;
                public static Quat Multiply(Quat left, float right) => left;
            }
            public struct Mat4
            {
                public float M11;
                public static Mat4 CreateRotationX(float angle) => default;
                public static bool Invert(Mat4 matrix, out Mat4 result) { result = matrix; return true; }
                public Vec Translation { get; set; }
                public float this[int row, int column] { get => M11; set => M11 = value; }
            }
            public static class Extensions
            {
                extension(Mat4)
                {
                    public static bool InvertSafe(in Mat4 matrix, out Mat4 result) { result = matrix; return true; }
                }
                extension(ref Mat4 matrix)
                {
                    public void SetUp(Vec value) { }
                }
                extension(Vec)
                {
                    public static Vec Scale(Vec value, float factor) => value;
                    public static void Scale(ref Vec value, float factor, out Vec result) { result = value; }
                    public static Vec Clamp(Vec value) => value;
                }
                extension(ref Vec value)
                {
                    public void Clamp() { }
                }
                extension(Vec value)
                {
                    public bool NearEquals(Vec other) => true;
                }
            }
        }
        """;

    private static TypeMigration MathMigration() => new TypeMigration()
        .Type("Old.Vec", "New.Vec")
        .Type("Old.Small", "New.Small")
        .Type("Old.Quat", "New.Quat")
        .Type("Old.Mat", "New.Mat4")
        .Rename("Old.Vec", "Normalize", "NormalizeSafe")
        .Rename("Old.Mat", "RotationX", "CreateRotationX")
        .ToOutCall("Old.Mat", "Invert", "InvertSafe", "Mat4.Invert({0}, out var {result}) ? {result} : default", "inverse")
        .Rename("Old.Mat", "TranslationVector", "Translation")
        .Rename("Old.Quat", "Multiply", "Concatenate", "Old.Quat", "Old.Quat")
        .OperatorToCall("Old.Quat", "op_Multiply", "Concatenate")
        .ConstructorToCall("Old.Vec", ["float[]"], "Create")
        .CastToCall("Old.Vec", "Old.Small", "AsSmall")
        .Move("Old.Vec", "Shrink", ["Old.Vec"], "Old.Small", "Old.Small", "FromVec")
        .LinearIndexer("Old.Mat", 4)
        .SetterToCall("Old.Mat", "Up", "SetUp")
        .OperatorToInstanceCall("Old.Vec", "op_Equality", "NearEquals")
        .OperatorToInstanceCall("Old.Vec", "op_Inequality", "NearEquals", negate: true)
        .Rename("Old.Vec", "Equals", "NearEquals", "Old.Vec")
        .Rename("Old.Vec", "EqualsStrict", "Equals");

    [Fact]
    public async Task MigrateTypesRewritesCallsToTheFormsOfTheNewTypes()
    {
        const string Usage = """
            using Old;

            namespace TestNs
            {
                class Usage
                {
                    Vec field;
                    Mat[] matrices = new Mat[2];
                    int index;

                    void Run(Vec a, Vec b, Quat p, Quat q)
                    {
                        var sum = Vec.Zero;
                        Vec.Add(ref a, ref b, out sum);
                        Vec.Add(ref a, ref b, out var other);
                        Vec.Normalize(ref a, out Vec unit);
                        field.Normalize();
                        matrices[1].Invert();
                        Vec.Split(ref a, out var x, out var y);
                        Vec.OnlyOut(ref a, out var kept);
                        // The new type has no Scale or Clamp of its own: these come over in all their forms
                        Vec.Scale(ref a, 2.0f, out var scaled);
                        field.Clamp();

                        var r = p * q * p;
                        r *= q;
                        r = r * 2.0f;
                        Quat.Multiply(ref p, ref q, out r);
                        r = Quat.Multiply(p, 0.5f);

                        Mat.RotationX(1.0f, out var rotation);
                        var m = Mat.Invert(Mat.RotationX(2.0f));
                        m.TranslationVector = new Vec(new[] { 1.0f, 2.0f });
                        var small = (Small)a;
                        var small2 = (Small)Vec.Normalize(Vec.Add(a, b));
                        Old.Vec qualified = new Old.Vec(1, 2);

                        var shrunk = Vec.Shrink(a);
                        Vec.Shrink(ref a, out shrunk);
                        var element = m[6];
                        m[index + 1] = element;
                        m.Up = Vec.Normalize(a);

                        var same = a == b || Vec.Add(a, b) != field;
                        var typed = a.Equals(b) && a.EqualsStrict(b) && a.Equals((object)b);
                    }
                }
            }
            """;
        const string Expected = """
            using Old;
            using New;

            namespace TestNs
            {
                class Usage
                {
                    Vec field;
                    Mat4[] matrices = new Mat4[2];
                    int index;

                    void Run(Vec a, Vec b, Quat p, Quat q)
                    {
                        var sum = Vec.Zero;
                        sum = Vec.Add(a, b);
                        var other = Vec.Add(a, b);
                        Vec unit = Vec.NormalizeSafe(a);
                        field = Vec.NormalizeSafe(field);
                        Mat4.InvertSafe(matrices[1], out matrices[1]);
                        Vec.Split(ref a, out var x, out var y);
                        Vec.OnlyOut(ref a, out var kept);
                        // The new type has no Scale or Clamp of its own: these come over in all their forms
                        Vec.Scale(ref a, 2.0f, out var scaled);
                        field.Clamp();

                        var r = Quat.Concatenate(Quat.Concatenate(p, q), p);
                        r = Quat.Concatenate(r, q);
                        r = r * 2.0f;
                        r = Quat.Concatenate(p, q);
                        r = Quat.Multiply(p, 0.5f);

                        var rotation = Mat4.CreateRotationX(1.0f);
                        Mat4.InvertSafe(Mat4.CreateRotationX(2.0f), out var m);
                        m.Translation = Vec.Create(new[] { 1.0f, 2.0f });
                        var small = a.AsSmall();
                        var small2 = Vec.NormalizeSafe(Vec.Add(a, b)).AsSmall();
                        New.Vec qualified = new New.Vec(1, 2);

                        var shrunk = Small.FromVec(a);
                        shrunk = Small.FromVec(a);
                        var element = m[1, 2];
                        m[(index + 1) / 4, (index + 1) % 4] = element;
                        m.SetUp(Vec.NormalizeSafe(a));

                        var same = a.NearEquals(b) || !Vec.Add(a, b).NearEquals(field);
                        var typed = a.NearEquals(b) && a.Equals(b) && a.Equals((object)b);
                    }
                }
            }
            """;

        var result = await ApplyAsync(Usage + Environment.NewLine + OldMathLibrary + Environment.NewLine + NewMathLibrary, MigrateTypes(MathMigration()));

        var usage = result[..result.IndexOf("namespace Old", StringComparison.Ordinal)].TrimEnd();
        Assert.Equal(Expected.ReplaceLineEndings(), usage.ReplaceLineEndings());
        AssertCompiles(Expected + Environment.NewLine + NewMathLibrary);
    }

    [Fact]
    public async Task MigrateTypesGivesAResultThroughOutWhereTheNewMethodDoes()
    {
        const string Usage = """
            using Old;

            namespace TestNs
            {
                class Holder { public Mat Value { get; set; } }

                class Usage
                {
                    Mat field;

                    void Run(Mat a, Mat b, Holder holder, bool condition)
                    {
                        Mat.Invert(ref a, out b);
                        Mat.Invert(ref a, out var declared);
                        a.Invert();
                        Mat typed = Mat.Invert(a); // Kept
                        b = Mat.Invert(a);
                        field = Mat.Invert(Mat.Invert(b));
                        Mat.Invert(a);
                        // A property is no out argument, and a call inside an expression has no statement of its own
                        holder.Value = Mat.Invert(a);
                        Use(Mat.Invert(a), Mat.Invert(b));
                        var chosen = condition ? Mat.Invert(a) : b;
                        var text = $"{Mat.Invert(a)}";
                        var inverse5 = a;
                    }

                    Mat Other(Mat inverse) => Mat.Invert(inverse);

                    void Use(Mat a, Mat b) { }
                }
            }
            """;
        const string Expected = """
            using Old;
            using New;

            namespace TestNs
            {
                class Holder { public Mat4 Value { get; set; } }

                class Usage
                {
                    Mat4 field;

                    void Run(Mat4 a, Mat4 b, Holder holder, bool condition)
                    {
                        Mat4.InvertSafe(a, out b);
                        Mat4.InvertSafe(a, out var declared);
                        Mat4.InvertSafe(a, out a);
                        Mat4.InvertSafe(a, out Mat4 typed); // Kept
                        Mat4.InvertSafe(a, out b);
                        Mat4.InvertSafe(Mat4.Invert(b, out var inverse) ? inverse : default, out field);
                        Mat4.InvertSafe(a, out _);
                        // A property is no out argument, and a call inside an expression has no statement of its own
                        holder.Value = Mat4.Invert(a, out var inverse2) ? inverse2 : default;
                        Use(Mat4.Invert(a, out var inverse3) ? inverse3 : default, Mat4.Invert(b, out var inverse4) ? inverse4 : default);
                        var chosen = condition ? (Mat4.Invert(a, out var inverse6) ? inverse6 : default) : b;
                        var text = $"{(Mat4.Invert(a, out var inverse7) ? inverse7 : default)}";
                        var inverse5 = a;
                    }

                    Mat4 Other(Mat4 inverse) => Mat4.Invert(inverse, out var inverse2) ? inverse2 : default;

                    void Use(Mat4 a, Mat4 b) { }
                }
            }
            """;

        var result = await ApplyAsync(Usage + Environment.NewLine + OldMathLibrary + Environment.NewLine + NewMathLibrary, MigrateTypes(MathMigration()));

        var usage = result[..result.IndexOf("namespace Old", StringComparison.Ordinal)].TrimEnd();
        Assert.Equal(Expected.ReplaceLineEndings(), usage.ReplaceLineEndings());
        AssertCompiles(Expected + Environment.NewLine + NewMathLibrary);
    }

    [Fact]
    public async Task MigrateTypesKeepsTheReturningTwinAndGivesValuesToReadOnlyReferences()
    {
        const string Usage = """
            using Old;

            namespace TestNs
            {
                class Usage
                {
                    void Run(Vec a, Vec b)
                    {
                        Vec.Blend(ref a, ref b, out var blended);
                        Vec.Blend(in a, in b, out b);
                        // No twin returns this result
                        Vec.Solo(ref a, out var solo);
                        var sum = Vec.Sum(ref a, in b);
                        // This one writes its argument
                        Vec.Fill(ref a);
                    }
                }
            }
            """;
        const string OldLibrary = """
            namespace Old
            {
                public struct Vec
                {
                    public float X;
                    public static void Blend(ref readonly Vec a, ref readonly Vec b, out Vec result) { result = a; }
                    public static Vec Blend(Vec a, Vec b) => a;
                    public static void Solo(ref readonly Vec a, out Vec result) { result = a; }
                    public static float Sum(ref readonly Vec a, in Vec b) => 0;
                    public static void Fill(ref Vec a) { }
                }
            }
            """;
        const string NewLibrary = """
            namespace Old
            {
                public static class Extensions
                {
                    extension(New.Vec)
                    {
                        public static New.Vec Blend(New.Vec a, New.Vec b) => a;
                        public static void Solo(in New.Vec a, out New.Vec result) { result = a; }
                        public static float Sum(New.Vec a, in New.Vec b) => 0;
                        public static void Fill(ref New.Vec a) { }
                    }
                }
            }
            namespace New
            {
                public struct Vec { public float X; }
            }
            """;
        const string Expected = """
            using Old;
            using New;

            namespace TestNs
            {
                class Usage
                {
                    void Run(Vec a, Vec b)
                    {
                        var blended = Vec.Blend(a, b);
                        b = Vec.Blend(a, b);
                        // No twin returns this result
                        Vec.Solo(a, out var solo);
                        var sum = Vec.Sum(a, b);
                        // This one writes its argument
                        Vec.Fill(ref a);
                    }
                }
            }
            """;

        var result = await ApplyAsync(Usage + Environment.NewLine + OldLibrary, MigrateTypes(new TypeMigration().Type("Old.Vec", "New.Vec").ReturningTwinsOnly()));

        var usage = result[..result.IndexOf("namespace Old", StringComparison.Ordinal)].TrimEnd();
        Assert.Equal(Expected.ReplaceLineEndings(), usage.ReplaceLineEndings());
        AssertCompiles(Expected + Environment.NewLine + NewLibrary);
    }

    [Fact]
    public async Task MigrateTypesWritesTheExpressionsTheMigrationGives()
    {
        const string Usage = """
            using Old;

            namespace TestNs
            {
                class Usage
                {
                    string Run(Vec a, Quat q, int i)
                    {
                        var size = Vec.Size;
                        var unit = a.IsUnit;
                        var angles = Vec.FromRotation(q);
                        Vec.FromRotation(ref q, out var other);
                        var along = Vec.Along(a, 2.0f);
                        // A member the rule names needs less
                        var alongOne = Vec.Along(Vec.One, 2.0f) + Vec.Along(Vec.MakeUnit(a), 2.0f);
                        Vec.Along(ref a, 2.0f, out var alongToo);
                        var down = Vec.Down(a);
                        // A method that only gives its result through out
                        Vec.FromNumber(3.0f, out var three);
                        Vec.FromNumber(4.0f, out three);
                        q.Turn(ref a);
                        a.Flip();
                        var plus = +a;
                        var ratio = a / a;
                        // A value the expression names twice is computed once
                        var ratioOnce = a / Vec.MakeUnit(a);
                        a.Half = 2.0f;
                        Vec.Split(a, out var x, out var y);
                        Vec.Split(a, out x, out y);
                        var same = new Vec(2.0f);
                        var first = a[0];
                        a[1] = first;
                        var any = a[i];
                        // No member has an index that is not a constant
                        a[i] = first;
                        return $"{a:F2} {a}";
                    }
                }
            }
            """;
        const string OldLibrary = """
            namespace Old
            {
                public struct Vec
                {
                    public float X, Y;
                    public Vec(float value) { X = value; Y = value; }
                    public Vec(float x, float y) { X = x; Y = y; }
                    public static readonly int Size = 8;
                    public static readonly Vec One = new Vec(1, 1);
                    public static Vec Along(Vec direction, float length) => default;
                    public static Vec MakeUnit(Vec value) => value;
                    public static Vec operator +(Vec left, Vec right) => left;
                    public static Vec Down(Vec value) => value;
                    public void Flip() { }
                    public static Vec operator +(Vec value) => value;
                    public static Vec operator /(Vec left, Vec right) => left;
                    public float Half { get => X; set => X = value; }
                    public static void Split(Vec value, out float x, out float y) { x = 0; y = 0; }
                    public static void FromNumber(float value, out Vec result) { result = default; }
                    public static void Along(ref readonly Vec direction, float length, out Vec result) { result = default; }
                    public bool IsUnit => true;
                    public float this[int index] { get => X; set => X = value; }
                    public static Vec FromRotation(Quat rotation) => default;
                    public static void FromRotation(ref readonly Quat rotation, out Vec result) { result = default; }
                    public string ToString(string format, System.IFormatProvider provider) => "";
                }
                public struct Quat
                {
                    public float W;
                    public readonly void Turn(ref Vec value) { }
                }
            }
            """;
        const string NewLibrary = """
            namespace Old
            {
                public static class Extensions
                {
                    extension(New.Vec value)
                    {
                        public bool NearEquals(New.Vec other) => true;
                        public New.Wide AsWide() => default;
                        public New.Vec Unit() => default;
                        public (float, float) Parts => default;
                        public string ToString(string format, System.IFormatProvider provider) => "";
                    }
                    extension(New.Quat rotation)
                    {
                        public New.Vec Angles => default;
                    }
                }
            }
            namespace New
            {
                public struct Vec
                {
                    public float X, Y;
                    public Vec(float x, float y) { X = x; Y = y; }
                    public static Vec One => new Vec(1, 1);
                    public static Vec Scale(Vec value, float by) => value;
                    public static Vec Round(Vec value, System.MidpointRounding mode) => value;
                    public static Vec Turned(Vec value, Quat by) => value;
                    public static Vec Pair(float x, float y) => default;
                    public static Vec operator -(Vec value) => value;
                    public static Vec MakeUnit(Vec value) => value;
                    public static Vec operator +(Vec left, Vec right) => left;
                }
                public struct Quat { public float W; }
                public struct Wide { public float this[int index] => 0; }
            }
            """;
        const string Expected = """
            using Old;
            using New;
            using System;
            using System.Runtime.CompilerServices;

            namespace TestNs
            {
                class Usage
                {
                    string Run(Vec a, Quat q, int i)
                    {
                        var size = Unsafe.SizeOf<Vec>();
                        var unit = a.NearEquals(Vec.One);
                        var angles = q.Angles;
                        var other = q.Angles;
                        var along = Vec.Scale(a.Unit(), 2.0f);
                        // A member the rule names needs less
                        var alongOne = Vec.Scale(Vec.One, 2.0f) + Vec.Scale(Vec.MakeUnit(a), 2.0f);
                        var alongToo = Vec.Scale(a.Unit(), 2.0f);
                        var down = Vec.Round(a, MidpointRounding.ToNegativeInfinity);
                        // A method that only gives its result through out
                        var three = new Vec(3.0f, 3.0f);
                        three = new Vec(4.0f, 4.0f);
                        a = Vec.Turned(a, q);
                        a = -a;
                        var plus = a;
                        var ratio = Vec.Pair(a.X / a.X, a.Y / a.Y);
                        // A value the expression names twice is computed once
                        var ratioOnce = ((a, Vec.MakeUnit(a)) is var (left, right) ? Vec.Pair(left.X / right.X, left.Y / right.Y) : default);
                        a = a with { X = 2.0f };
                        var (x, y) = a.Parts;
                        (x, y) = a.Parts;
                        var same = new Vec(2.0f, 2.0f);
                        var first = a.X;
                        a.Y = first;
                        var any = a.AsWide()[i];
                        // No member has an index that is not a constant
                        a[i] = first;
                        return $"{a.ToString("F2", null)} {a}";
                    }
                }
            }
            """;

        var migration = new TypeMigration()
            .Type("Old.Vec", "New.Vec")
            .Type("Old.Quat", "New.Quat")
            .ReturningTwinsOnly()
            .MemberToExpression("Old.Vec", "Size", "Unsafe.SizeOf<Vec>()", "System.Runtime.CompilerServices")
            .MemberToExpression("Old.Vec", "IsUnit", "{0}.NearEquals(Vec.One)")
            .CallToExpression("Old.Vec", "FromRotation", ["Old.Quat"], "{0}.Angles")
            .CallToExpression("Old.Vec", "Along", ["Old.Vec", "float"], "Vec.Scale({0}, {1})", ["One", "MakeUnit"])
            .CallToExpression("Old.Vec", "Along", ["Old.Vec", "float"], "Vec.Scale({0}.Unit(), {1})")
            .CallToExpression("Old.Vec", "Down", ["Old.Vec"], "Vec.Round({0}, MidpointRounding.ToNegativeInfinity)", import: "System")
            .CallToExpression("Old.Vec", "FromNumber", ["float"], "new Vec({0}, {0})")
            .RefCallToAssignment("Old.Quat", "Turn", "Vec.Turned({1}, {0})")
            .InPlaceToExpression("Old.Vec", "Flip", "-{0}")
            .OperatorToExpression("Old.Vec", "op_UnaryPlus", "{0}")
            .OperatorToExpression("Old.Vec", "op_Division", "Vec.Pair({0}.X / {1}.X, {0}.Y / {1}.Y)")
            .SetterToAssignment("Old.Vec", "Half", "{0} with { X = {1} }")
            .OutsToDeconstruction("Old.Vec", "Split", ["Old.Vec"], "{0}.Parts")
            .ConstructorToExpression("Old.Vec", ["float"], "new Vec({0}, {0})")
            .IndexerToMembers("Old.Vec", ["X", "Y"], "{0}.AsWide()[{1}]")
            .FormatToCall("Old.Vec", "ToString");
        var result = await ApplyAsync(Usage + Environment.NewLine + OldLibrary, MigrateTypes(migration));

        var usage = result[..result.IndexOf("namespace Old", StringComparison.Ordinal)].TrimEnd();
        Assert.Equal(Expected.ReplaceLineEndings(), usage.ReplaceLineEndings());
        // But the write through an index that is no constant: the new type has no indexer
        AssertCompiles(Expected.Replace("a[i] = first;", "") + Environment.NewLine + NewLibrary);
    }

    [Fact]
    public async Task MigrateTypesLeavesWhatItCannotWriteTwiceOrDeclare()
    {
        const string Usage = """
            namespace TestNs
            {
                using Old;

                class Usage
                {
                    Vec[] values = new Vec[4];
                    int index;

                    Vec Next() => values[index++];

                    void Run(Vec a, Quat p, Quat q, bool condition)
                    {
                        // The element is evaluated once
                        values[index++].Normalize();
                        // An embedded statement cannot declare a local
                        if (condition)
                            Vec.Normalize(ref a, out var unit);
                        var var = a;
                    }
                }
            }
            """;

        var result = await ApplyAsync(Usage + Environment.NewLine + OldMathLibrary + Environment.NewLine + NewMathLibrary, MigrateTypes(MathMigration()));

        Assert.Contains("values[index++].NormalizeSafe();", result);
        Assert.Contains("Vec.NormalizeSafe(ref a, out var unit);", result);
        Assert.Contains("var var = a;", result);
    }

    [Fact]
    public async Task MigrateTypesKeepsNullChecksAndWritesThroughPointers()
    {
        const string Usage = """
            using Old;
            using static Old.Vec;

            namespace TestNs
            {
                unsafe class Usage
                {
                    bool Run(Vec a, Vec? maybe, Vec* pointer, Mat* matrix)
                    {
                        pointer->Normalize();
                        matrix->Invert();
                        var sum = Add(a, Zero);
                        return maybe == null || maybe != a;
                    }
                }
            }
            """;

        var result = await ApplyAsync(Usage + Environment.NewLine + OldMathLibrary + Environment.NewLine + NewMathLibrary, MigrateTypes(MathMigration()));

        Assert.Contains("*pointer = Vec.NormalizeSafe(*pointer);", result);
        Assert.Contains("Mat4.InvertSafe(*matrix, out *matrix);", result);
        // A static member named alone, through a using static
        Assert.Contains("var sum = Vec.Add(a, Vec.Zero);", result);
        // With a nullable value or null, == stays
        Assert.Contains("return maybe == null || maybe != a;", result);
    }

    [Fact]
    public async Task MigrateTypesImportsTheNamespaceOfExtensionMembers()
    {
        // Members the new type does not have are extension members, declared in the namespace of the old type
        const string NeedsExtensions = """
            namespace TestNs
            {
                class Usage
                {
                    bool Run(Old.Vec a, Old.Vec b)
                    {
                        a.Clamp();
                        return a == b;
                    }
                }
            }
            """;
        const string NeedsNone = """
            namespace TestNs
            {
                class Usage
                {
                    Old.Vec Run(Old.Vec a, Old.Vec b) => Old.Vec.Add(a, b);
                }
            }
            """;

        var withExtensions = await ApplyAsync(NeedsExtensions + Environment.NewLine + OldMathLibrary + Environment.NewLine + NewMathLibrary, MigrateTypes(MathMigration()));
        var without = await ApplyAsync(NeedsNone + Environment.NewLine + OldMathLibrary + Environment.NewLine + NewMathLibrary, MigrateTypes(MathMigration()));

        Assert.Contains("using Old;", withExtensions);
        Assert.Contains("return a.NearEquals(b);", withExtensions);
        Assert.Contains("New.Vec Run(New.Vec a, New.Vec b) => New.Vec.Add(a, b);", without);
        Assert.DoesNotContain("using Old;", without);
    }

    [Fact]
    public async Task AFileThatNamesNoTypeStillGetsTheUsingOfAnExtensionMember()
    {
        const string Usage = """
            namespace TestNs
            {
                class Usage { void Run(Holder holder) { holder.Value.Clamp(); } }
            }
            """;
        const string Holder = """
            namespace TestNs
            {
                class Holder { public Old.Vec Value; }
            }
            """;

        var result = await ApplyAsync(Usage, Holder + Environment.NewLine + OldMathLibrary + Environment.NewLine + NewMathLibrary, MigrateTypes(MathMigration()));

        Assert.StartsWith("using Old;", result);
        Assert.Contains("holder.Value.Clamp();", result);
    }

    [Fact]
    public async Task AnAddedUsingGoesWhereEveryConfigurationOfTheFileGetsIt()
    {
        const string Usage = """
            namespace TestNs
            {
                class Usage { Old.Vec Run(Old.Vec a) { a.Clamp(); return a; } }
            }
            """;
        async Task<string> Start(string start, string end = "")
        {
            var result = await ApplyAsync(start + Usage + Environment.NewLine + OldMathLibrary + Environment.NewLine + NewMathLibrary + end, MigrateTypes(MathMigration()));
            return result[..result.IndexOf("namespace TestNs", StringComparison.Ordinal)].ReplaceLineEndings("\n");
        }

        // No using yet: after the header of the file, not above it
        Assert.Equal("// Header\n\nusing New;\nusing Old;\n\n", await Start("// Header\n\n"));
        // The documentation of the first declaration stays with it
        Assert.Equal("using New;\nusing Old;\n\n/// <summary>Documented.</summary>\n", await Start("/// <summary>Documented.</summary>\n"));
        // The last using is under #if: before the block
        Assert.Equal("using System;\nusing New;\nusing Old;\n#if true\nusing System.Text;\n#endif\n\n", await Start("using System;\n#if true\nusing System.Text;\n#endif\n\n"));
        // Every using is under #if: after the block
        Assert.Equal("#if true\nusing System;\n#endif\nusing New;\nusing Old;\n\n", await Start("#if true\nusing System;\n#endif\n\n"));
        // The whole file is under #if: in the block, after the other usings
        Assert.Equal("#if true\nusing System;\nusing New;\nusing Old;\n\n", await Start("#if true\nusing System;\n\n", Environment.NewLine + "#endif" + Environment.NewLine));
    }

    [Fact]
    public async Task MigrateTypesRenamesTheTypesNamedInDocumentation()
    {
        const string Usage = """
            using Old;

            namespace TestNs
            {
                class Usage
                {
                    /// <summary>Takes a <see cref="Vec"/> and an <see cref="Old.Mat"/>.</summary>
                    void Run(Vec a, Mat m) { }
                }
            }
            """;
        const string Expected = """
            using Old;
            using New;

            namespace TestNs
            {
                class Usage
                {
                    /// <summary>Takes a <see cref="Vec"/> and an <see cref="New.Mat4"/>.</summary>
                    void Run(Vec a, Mat4 m) { }
                }
            }
            """;

        var result = await ApplyAsync(Usage + Environment.NewLine + OldMathLibrary + Environment.NewLine + NewMathLibrary, MigrateTypes(MathMigration()));

        var usage = result[..result.IndexOf("namespace Old", StringComparison.Ordinal)].TrimEnd();
        Assert.Equal(Expected.ReplaceLineEndings(), usage.ReplaceLineEndings());
        AssertCompiles(Expected + Environment.NewLine + NewMathLibrary);
    }

    [Fact]
    public async Task MathTypesToSystemNumericsKeepsWhatStrideMathDid()
    {
        const string Usage = """
            using Stride.Core.Mathematics;

            class Usage
            {
                string Run(Vector3 a, Vector3 b, Quaternion p, Quaternion q, Matrix m)
                {
                    a.Normalize();
                    Vector3.Add(ref a, ref b, out var sum);
                    var rotation = p * q;
                    Matrix.RotationX(1.0f, out var x);
                    Matrix.Invert(ref x, out var inverted);
                    Vector3.TransformCoordinate(ref a, ref m, out var moved);
                    var identity = m.IsIdentity || Matrix.LayoutIsRowMajor;
                    var bytes = Vector3.SizeInBytes;
                    m = Matrix.Invert(x) * Matrix.Translation(a);
                    m.Up = m.TranslationVector;
                    var plane = new Plane(a, b);
                    // A normal that a call gives is computed once
                    plane = new Plane(a, Next());
                    q = Quaternion.RotationAxis(a, 1.0f);
                    Quaternion.RotationAxis(ref b, 2.0f, out p);
                    q = Quaternion.RotationAxis(Vector3.UnitY, 1.0f);
                    q = Quaternion.RotationAxis(Vector3.Normalize(b), 1.0f);
                    m = Matrix.RotationAxis(a, 1.0f) * Matrix.Reflection(plane);
                    var floor = Vector3.Floor(a);
                    q.Rotate(ref a);
                    q = Quaternion.RotationX(1.0f);
                    Plane.Normalize(1.0f, 2.0f, 3.0f, 4.0f, out plane);
                    m = Matrix.Zero;
                    m = Matrix.PerspectiveFovRH(1.0f, 1.5f, 0.1f, 100.0f);
                    Matrix.PerspectiveOffCenterLH(-1.0f, 1.0f, -1.0f, 1.0f, 0.1f, 100.0f, out m);
                    Quaternion.RotationYawPitchRoll(ref q, out var yaw, out var pitch, out var roll);
                    var angles = q.YawPitchRoll;
                    a = q.Axis;
                    var turn = q.Angle;
                    q = Quaternion.One;
                    m.Orthogonalize();
                    m = Matrix.AffineTransformation(1.0f, q, a);
                    m = m / m;
                    a = m.ScaleVector;
                    m.ScaleVector = a;
                    plane.Negate();
                    plane = Plane.Multiply(plane, 2.0f);
                    a.Pow(2.0f);
                    var near = Vector3.NearEqual(a, b, a);
                    var same = a == b || m[6] > 0;
                    return $"{a} {(Vector2)b}";
                }

                Vector3 Next() => default;
            }
            """;
        // The members of Stride.Core.Mathematics this code uses, as they were
        const string StrideMath = """
            namespace Stride.Core.Mathematics
            {
                public struct Vector2 { public float X, Y; }
                public struct Vector3
                {
                    public float X, Y, Z;
                    public static readonly int SizeInBytes = 12;
                    public static readonly Vector3 UnitY;
                    public static Vector3 Normalize(Vector3 value) => value;
                    public static Vector3 Floor(Vector3 value) => value;
                    public void Pow(float exponent) { }
                    public static bool NearEqual(Vector3 left, Vector3 right, Vector3 epsilon) => true;
                    public void Normalize() { }
                    public static void Add(ref Vector3 left, ref Vector3 right, out Vector3 result) { result = left; }
                    public static void TransformCoordinate(ref readonly Vector3 coordinate, ref readonly Matrix transform, out Vector3 result) { result = coordinate; }
                    public static Vector3 TransformCoordinate(Vector3 coordinate, Matrix transform) => coordinate;
                    public static explicit operator Vector2(Vector3 value) => default;
                    public static bool operator ==(Vector3 left, Vector3 right) => true;
                    public static bool operator !=(Vector3 left, Vector3 right) => false;
                    public override bool Equals(object other) => true;
                    public override int GetHashCode() => 0;
                }
                public struct Quaternion
                {
                    public static Quaternion operator *(Quaternion left, Quaternion right) => left;
                    public static Quaternion RotationAxis(Vector3 axis, float angle) => default;
                    public static void RotationAxis(ref readonly Vector3 axis, float angle, out Quaternion result) { result = default; }
                    public static Quaternion RotationX(float angle) => default;
                    public static readonly Quaternion One;
                    public Vector3 YawPitchRoll => default;
                    public float Angle => 0;
                    public Vector3 Axis => default;
                    public static void RotationYawPitchRoll(ref readonly Quaternion rotation, out float yaw, out float pitch, out float roll) { yaw = pitch = roll = 0; }
                    public readonly void Rotate(ref Vector3 vector) { }
                }
                public struct Matrix
                {
                    public static Matrix RotationAxis(Vector3 axis, float angle) => default;
                    public static Matrix Reflection(Plane plane) => default;
                    public const bool LayoutIsRowMajor = false;
                    public static readonly Matrix Zero;
                    public void Orthogonalize() { }
                    public static Matrix AffineTransformation(float scaling, Quaternion rotation, Vector3 translation) => default;
                    public static Matrix operator /(Matrix left, Matrix right) => left;
                    public Vector3 ScaleVector { get; set; }
                    public static Matrix PerspectiveFovRH(float fov, float aspect, float znear, float zfar) => default;
                    public static Matrix PerspectiveOffCenterLH(float left, float right, float bottom, float top, float znear, float zfar) => default;
                    public static void PerspectiveOffCenterLH(float left, float right, float bottom, float top, float znear, float zfar, out Matrix result) { result = default; }
                    public bool IsIdentity => true;
                    public static void RotationX(float angle, out Matrix result) { result = default; }
                    public static Matrix Invert(Matrix value) => value;
                    public static void Invert(ref Matrix value, out Matrix result) { result = value; }
                    public static Matrix Translation(Vector3 value) => default;
                    public static Matrix operator *(Matrix left, Matrix right) => left;
                    public Vector3 TranslationVector { get; set; }
                    public Vector3 Up { get; set; }
                    public float this[int index] { get => 0; set { } }
                }
                public struct Plane
                {
                    public Plane(Vector3 point, Vector3 normal) { }
                    public static void Normalize(float normalX, float normalY, float normalZ, float planeD, out Plane result) { result = default; }
                    public void Negate() { }
                    public static Plane Multiply(Plane value, float scale) => value;
                }
            }
            """;
        const string Expected = """
            using Stride.Core.Mathematics;
            using System;
            using System.Numerics;
            using System.Runtime.CompilerServices;

            class Usage
            {
                string Run(Vector3 a, Vector3 b, Quaternion p, Quaternion q, Matrix4x4 m)
                {
                    a = Vector3.NormalizeSafe(a);
                    var sum = Vector3.Add(a, b);
                    var rotation = Quaternion.Concatenate(p, q);
                    var x = Matrix4x4.CreateRotationX(1.0f);
                    Matrix4x4.InvertSafe(x, out var inverted);
                    var moved = Vector3.TransformCoordinate(a, m);
                    var identity = m.NearEquals(Matrix4x4.Identity) || true;
                    var bytes = Unsafe.SizeOf<Vector3>();
                    m = (Matrix4x4.Invert(x, out var inverse) ? inverse : default) * Matrix4x4.CreateTranslation(a);
                    m.SetUp(m.Translation);
                    var plane = new Plane(b, Vector3.Dot(b, a));
                    // A normal that a call gives is computed once
                    plane = ((a, Next()) is var (point, normal) ? new Plane(normal, Vector3.Dot(normal, point)) : default);
                    q = Quaternion.CreateFromAxisAngle(Vector3.NormalizeSafe(a), 1.0f);
                    p = Quaternion.CreateFromAxisAngle(Vector3.NormalizeSafe(b), 2.0f);
                    q = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 1.0f);
                    q = Quaternion.CreateFromAxisAngle(Vector3.NormalizeSafe(b), 1.0f);
                    m = Matrix4x4.CreateFromAxisAngle(a, 1.0f) * Matrix4x4.CreateReflection(plane);
                    var floor = Vector3.Round(a, MidpointRounding.ToNegativeInfinity);
                    a = Vector3.Transform(a, q);
                    q = Quaternion.CreateRotationX(1.0f);
                    plane = Plane.Normalize(new Plane(1.0f, 2.0f, 3.0f, 4.0f));
                    m = default(Matrix4x4);
                    m = Matrix4x4.CreatePerspectiveFieldOfView(1.0f, 1.5f, 0.1f, 100.0f);
                    m = Matrix4x4.CreatePerspectiveOffCenterLeftHanded(-1.0f, 1.0f, -1.0f, 1.0f, 0.1f, 100.0f);
                    var (yaw, pitch, roll) = q.GetYawPitchRoll();
                    var angles = q.GetYawPitchRoll();
                    a = q.GetAxis();
                    var turn = q.GetAngle();
                    q = new Quaternion(1, 1, 1, 1);
                    m = Matrix4x4.Orthogonalize(m);
                    m = Matrix4x4.CreateAffineTransformation(1.0f, q, a);
                    m = Matrix4x4.Create(m.X / m.X, m.Y / m.Y, m.Z / m.Z, m.W / m.W);
                    a = new Vector3(m.M11, m.M22, m.M33);
                    m = m with { M11 = a.X, M22 = a.Y, M33 = a.Z };
                    plane = -plane;
                    plane = plane * 2.0f;
                    a = Vector3.Pow(a, 2.0f);
                    var near = a.NearEquals(b, a);
                    var same = a.NearEquals(b) || m[1, 2] > 0;
                    return $"{a} {b.AsVector2()}";
                }

                Vector3 Next() => default;
            }
            """;

        var result = await ApplyAsync(Usage + Environment.NewLine + StrideMath, MathTypesToSystemNumerics());

        var usage = result[..result.IndexOf("namespace Stride.Core.Mathematics", StringComparison.Ordinal)].TrimEnd();
        Assert.Equal(Expected.ReplaceLineEndings(), usage.ReplaceLineEndings());
    }

    [Fact]
    public async Task MathTypesToSystemNumericsGivesTheDoubleVectorsTheFormsOfTheVectors()
    {
        const string Usage = """
            using Stride.Core.Mathematics;

            class Usage
            {
                bool Run(Double3 a, Double3 b)
                {
                    Double3.Add(ref a, ref b, out var sum);
                    a.Normalize();
                    var unit = Double3.Normalize(b);
                    a = Double3.Modulate(a, unit);
                    a.Pow(2.0);
                    return Double3.NearEqual(ref a, ref b, ref sum);
                }
            }
            """;
        const string StrideMath = """
            namespace Stride.Core.Mathematics
            {
                public struct Double3
                {
                    public double X, Y, Z;
                    public static Double3 Add(Double3 left, Double3 right) => left;
                    public static void Add(ref readonly Double3 left, ref readonly Double3 right, out Double3 result) { result = left; }
                    public void Normalize() { }
                    public static Double3 Normalize(Double3 value) => value;
                    public static Double3 Modulate(Double3 left, Double3 right) => left;
                    public void Pow(double exponent) { }
                    public static bool NearEqual(ref readonly Double3 left, ref readonly Double3 right, ref readonly Double3 epsilon) => true;
                }
            }
            """;
        const string Expected = """
            using Stride.Core.Mathematics;

            class Usage
            {
                bool Run(Double3 a, Double3 b)
                {
                    var sum = Double3.Add(a, b);
                    a = Double3.NormalizeSafe(a);
                    var unit = Double3.NormalizeSafe(b);
                    a = Double3.Multiply(a, unit);
                    a = Double3.Pow(a, 2.0);
                    return a.NearEquals(b, sum);
                }
            }
            """;

        var result = await ApplyAsync(Usage + Environment.NewLine + StrideMath, MathTypesToSystemNumerics());

        var usage = result[..result.IndexOf("namespace Stride.Core.Mathematics", StringComparison.Ordinal)].TrimEnd();
        Assert.Equal(Expected.ReplaceLineEndings(), usage.ReplaceLineEndings());
    }

    [Fact]
    public async Task MathTypesToSystemNumericsGivesTheIntegerAndHalfVectorsTheFormsOfTheVectors()
    {
        const string Usage = """
            using Stride.Core.Mathematics;

            class Usage
            {
                bool Run(Int3 a, Int3 b, Half3 h, Half3 k)
                {
                    Int3.Add(ref a, ref b, out var sum);
                    a = Int3.Modulate(a, sum);
                    Int3.Modulate(ref a, ref b, out sum);
                    a.Pow(2);
                    return Half3.Equals(ref h, ref k) || !Half3.Equals(in k, in h);
                }
            }
            """;
        const string StrideMath = """
            namespace Stride.Core.Mathematics
            {
                public struct Int3
                {
                    public int X, Y, Z;
                    public static Int3 Add(Int3 left, Int3 right) => left;
                    public static void Add(ref readonly Int3 left, ref readonly Int3 right, out Int3 result) { result = left; }
                    public static Int3 Modulate(Int3 left, Int3 right) => left;
                    public static void Modulate(ref readonly Int3 left, ref readonly Int3 right, out Int3 result) { result = left; }
                    public void Pow(int exponent) { }
                }
                public struct Half3
                {
                    public static bool Equals(ref readonly Half3 value1, ref readonly Half3 value2) => true;
                    public bool Equals(Half3 other) => true;
                }
            }
            """;
        const string Expected = """
            using Stride.Core.Mathematics;

            class Usage
            {
                bool Run(Int3 a, Int3 b, Half3 h, Half3 k)
                {
                    var sum = Int3.Add(a, b);
                    a = Int3.Multiply(a, sum);
                    sum = Int3.Multiply(a, b);
                    a = Int3.Pow(a, 2);
                    return h.Equals(k) || !k.Equals(h);
                }
            }
            """;

        var result = await ApplyAsync(Usage + Environment.NewLine + StrideMath, MathTypesToSystemNumerics());

        var usage = result[..result.IndexOf("namespace Stride.Core.Mathematics", StringComparison.Ordinal)].TrimEnd();
        Assert.Equal(Expected.ReplaceLineEndings(), usage.ReplaceLineEndings());
    }

    private const string StrideHalf = """
        namespace Stride.Core.Mathematics
        {
            public struct Half
            {
                public const int MantissaBits = 11;
                public static readonly float Epsilon = 0.0004887581f;
                public static readonly float MaxValue = 65504f;
                public static readonly Half Zero;
                public Half(float value) { }
                public ushort RawValue { get; set; }
                public static float[] ConvertToFloat(Half[] values) => null;
                public static explicit operator Half(float value) => default;
                public static implicit operator float(Half value) => 0;
                public static bool Equals(ref readonly Half value1, ref readonly Half value2) => true;
                public static bool operator ==(Half left, Half right) => true;
                public static bool operator !=(Half left, Half right) => false;
                public override bool Equals(object other) => true;
                public override int GetHashCode() => 0;
            }
            public struct Half2
            {
                public Half2(Half x, Half y) { }
                public Half2(float x, float y) { }
            }
        }
        """;

    [Fact]
    public async Task MathTypesToSystemNumericsReplacesHalfByTheHalfOfDotNet()
    {
        const string Usage = """
            using Stride.Core.Mathematics;

            class Usage
            {
                float Run(Half h, float f, Half[] halves)
                {
                    var half = new Half(f + 1.0f);
                    half = new Half { RawValue = 15360 };
                    float single = h;
                    single += half * 2.0f + Twice(h);
                    ushort bits = h.RawValue;
                    h.RawValue = bits;
                    var floats = Half.ConvertToFloat(halves);
                    if (Half.Equals(ref h, ref half) || f > Half.MaxValue || h == Half.Zero || f < Half.Zero)
                        return Half.Epsilon + Half.MantissaBits;
                    return (float)h + (f > 0 ? h : half);
                }

                float Twice(float value) => value * 2.0f;
            }
            """;
        const string Expected = """
            using Stride.Core.Mathematics;
            using System;

            class Usage
            {
                float Run(Half h, float f, Half[] halves)
                {
                    var half = (Half)(f + 1.0f);
                    half = BitConverter.UInt16BitsToHalf(15360);
                    float single = (float)h;
                    single += (float)half * 2.0f + Twice((float)h);
                    ushort bits = BitConverter.HalfToUInt16Bits(h);
                    h = BitConverter.UInt16BitsToHalf(bits);
                    var floats = Array.ConvertAll(halves, x => (float)x);
                    if (h.Equals(half) || f > 65504f || h == Half.Zero || f < (float)Half.Zero)
                        return 0.0004887581f + 11;
                    return (float)h + (float)(f > 0 ? h : half);
                }

                float Twice(float value) => value * 2.0f;
            }
            """;

        var result = await ApplyAsync(Usage + Environment.NewLine + StrideHalf, MathTypesToSystemNumerics());

        var usage = result[..result.IndexOf("namespace Stride.Core.Mathematics", StringComparison.Ordinal)].TrimEnd();
        Assert.Equal(Expected.ReplaceLineEndings(), usage.ReplaceLineEndings());
    }

    [Fact]
    public async Task MathTypesToSystemNumericsKeepsTheFloatsOfACallThatAlsoTakesHalves()
    {
        // System.Half converts from a byte by itself, and so from a small constant: with it, these calls would not choose
        const string Usage = """
            using Stride.Core.Mathematics;

            class Usage
            {
                Half2 Run(byte b, int i, Half h)
                {
                    var whole = new Half2(1, -2);
                    var bytes = new Half2(b, b);
                    var mixed = new Half2(1.5f, i);
                    return new Half2(h, h);
                }
            }
            """;
        const string Expected = """
            using Stride.Core.Mathematics;
            using System;

            class Usage
            {
                Half2 Run(byte b, int i, Half h)
                {
                    var whole = new Half2(1f, -2f);
                    var bytes = new Half2((float)b, (float)b);
                    var mixed = new Half2(1.5f, i);
                    return new Half2(h, h);
                }
            }
            """;

        var result = await ApplyAsync(Usage + Environment.NewLine + StrideHalf, MathTypesToSystemNumerics());

        var usage = result[..result.IndexOf("namespace Stride.Core.Mathematics", StringComparison.Ordinal)].TrimEnd();
        Assert.Equal(Expected.ReplaceLineEndings(), usage.ReplaceLineEndings());
    }

    [Fact]
    public async Task MathTypesToSystemNumericsKeepsAnAliasOfHalfOnTheHalfOfDotNet()
    {
        // The alias a file needed while System and Stride.Core.Mathematics both had a type of this name
        const string Usage = """
            using System;
            using Stride.Core.Mathematics;
            using Half = Stride.Core.Mathematics.Half;

            class Usage
            {
                float Run(Half h) => MathF.Abs(h);
            }
            """;
        const string Expected = """
            using System;
            using Stride.Core.Mathematics;
            using Half = System.Half;

            class Usage
            {
                float Run(Half h) => MathF.Abs((float)h);
            }
            """;

        var result = await ApplyAsync(Usage + Environment.NewLine + StrideHalf, MathTypesToSystemNumerics());

        var usage = result[..result.IndexOf("namespace Stride.Core.Mathematics", StringComparison.Ordinal)].TrimEnd();
        Assert.Equal(Expected.ReplaceLineEndings(), usage.ReplaceLineEndings());
    }

    [Fact]
    public async Task MathTypesToSystemNumericsKeepsTheToleranceOfDoubleVectorComparisons()
    {
        const string Usage = """
            using Stride.Core.Mathematics;

            class Usage
            {
                bool Run(Double3 a, Double3 b)
                {
                    var sum = a;
                    return a == b || a != sum || a.Equals(b);
                }
            }
            """;
        const string StrideMath = """
            namespace Stride.Core.Mathematics
            {
                public struct Double3
                {
                    public double X, Y, Z;
                    public static bool operator ==(Double3 left, Double3 right) => true;
                    public static bool operator !=(Double3 left, Double3 right) => false;
                    public bool Equals(Double3 other) => true;
                    public override bool Equals(object other) => true;
                    public override int GetHashCode() => 0;
                }
            }
            """;
        const string Expected = """
            using Stride.Core.Mathematics;

            class Usage
            {
                bool Run(Double3 a, Double3 b)
                {
                    var sum = a;
                    return a.NearEquals(b) || !a.NearEquals(sum) || a.NearEquals(b);
                }
            }
            """;

        var result = await ApplyAsync(Usage + Environment.NewLine + StrideMath, MathTypesToSystemNumerics());

        var usage = result[..result.IndexOf("namespace Stride.Core.Mathematics", StringComparison.Ordinal)].TrimEnd();
        Assert.Equal(Expected.ReplaceLineEndings(), usage.ReplaceLineEndings());
    }

    [Fact]
    public async Task MathTypesToSystemNumericsKeepsTheToleranceOfBoundingComparisonsAndTheFormsOfTheirCalls()
    {
        const string Usage = """
            using Stride.Core.Mathematics;

            class Usage
            {
                bool Run(BoundingBox a, BoundingBox b)
                {
                    // The type stays: its name and the form of its calls do too
                    BoundingBox.Merge(ref a, ref b, out var merged);
                    return a == b || a != merged;
                }
            }
            """;
        const string StrideMath = """
            namespace Stride.Core.Mathematics
            {
                public struct BoundingBox
                {
                    public static BoundingBox Merge(BoundingBox value1, BoundingBox value2) => value1;
                    public static void Merge(ref readonly BoundingBox value1, ref readonly BoundingBox value2, out BoundingBox result) { result = value1; }
                    public static bool operator ==(BoundingBox left, BoundingBox right) => true;
                    public static bool operator !=(BoundingBox left, BoundingBox right) => false;
                    public override bool Equals(object other) => true;
                    public override int GetHashCode() => 0;
                }
            }
            """;
        const string Expected = """
            using Stride.Core.Mathematics;

            class Usage
            {
                bool Run(BoundingBox a, BoundingBox b)
                {
                    // The type stays: its name and the form of its calls do too
                    BoundingBox.Merge(ref a, ref b, out var merged);
                    return a.NearEquals(b) || !a.NearEquals(merged);
                }
            }
            """;

        var result = await ApplyAsync(Usage + Environment.NewLine + StrideMath, MathTypesToSystemNumerics());

        var usage = result[..result.IndexOf("namespace Stride.Core.Mathematics", StringComparison.Ordinal)].TrimEnd();
        Assert.Equal(Expected.ReplaceLineEndings(), usage.ReplaceLineEndings());
    }
}
