// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Stride.Assets.Generators;
using Xunit;

namespace Stride.Assets.Tests
{
    public class TestAssetUrlConstantsGenerator
    {
        private const string AssetFolder = @"C:\proj\Assets";

        [Fact]
        public void TypedViaMapWithNestedFolders()
        {
            var result = Run(
            [
                Asset(@"Ground Textures\Skybox texture.sdtex"),
                Map(".sdtex|Stride.Graphics.Texture"),
            ]);

            Assert.Contains("internal static partial class Assets", result.Source);
            Assert.Contains("internal static partial class Ground_Textures", result.Source);
            Assert.Contains("global::Stride.Core.Serialization.UrlReference<global::Stride.Graphics.Texture> Skybox_texture", result.Source);
            Assert.Contains("\"/MyGame/Ground Textures/Skybox texture\"", result.Source);
            AssertCompiles(result);
        }

        [Fact]
        public void UntypedFallbackForUnregisteredExtension()
        {
            var result = Run([Asset("Thing.sdcustom")]);

            Assert.Contains("global::Stride.Core.Serialization.UrlReference Thing", result.Source);
            Assert.DoesNotContain("UrlReference<", result.Source);
            AssertCompiles(result);
        }

        [Fact]
        public void UnqualifiedUrlWhenNoNamespace()
        {
            var result = Run([Asset("Ground.sdtex")], urlNamespace: "");

            Assert.Contains("\"Ground\"", result.Source);
            Assert.DoesNotContain("\"/", result.Source);
        }

        [Fact]
        public void SanitizationAndCollisionSuffix()
        {
            var result = Run(
            [
                Asset("A b.sdcustom"),
                Asset("A_b.sdcustom"),
                Asset("2 Cool.sdcustom"),
            ]);

            Assert.Contains("UrlReference A_b ", result.Source);
            Assert.Contains("UrlReference A_b_1 ", result.Source);
            Assert.Contains("UrlReference _2_Cool ", result.Source);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "STRDIAG012");
            AssertCompiles(result);
        }

        [Fact]
        public void KeywordFolderIsEscaped()
        {
            var result = Run([Asset(@"new\Ground.sdtex")]);

            Assert.Contains("internal static partial class @new", result.Source);
            Assert.Contains("\"/MyGame/new/Ground\"", result.Source);
            AssertCompiles(result);
        }

        [Fact]
        public void NamespaceConflictSkipsGeneration()
        {
            var result = Run(
                [Asset("Ground.sdtex")],
                source: "namespace MyGame.Assets { public class Foo { } }");

            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "STRDIAG013");
            Assert.Null(result.Source);
        }

        [Fact]
        public void CompilationSymbolsWinOverMap()
        {
            var result = Run(
            [
                Asset("Thing.sdcustom"),
                Map(".sdcustom|Stride.Graphics.Texture"),
            ],
                source: """
                    using Stride.Core.Assets;
                    namespace MyPlugin
                    {
                        public class CustomContent { }

                        [AssetDescription(".sdcustom")]
                        [AssetContentType(typeof(CustomContent))]
                        public class CustomThingAsset { }
                    }
                    """);

            Assert.Contains("UrlReference<global::MyPlugin.CustomContent> Thing", result.Source);
            AssertCompiles(result);
        }

        [Fact]
        public void FilesWithoutAssetFolderMetadataAreSkipped()
        {
            // Only files the build task tagged with StrideAssetFolder are assets; the map file and
            // any other AdditionalFiles the generator is handed must never produce a constant.
            var result = Run(
            [
                (@"C:\other\note.txt", "hello"),
                Map(".sdtex|Stride.Graphics.Texture"),
            ]);

            Assert.Null(result.Source);
        }

        [Fact]
        public void EmitIsCachedWhenOnlyUnrelatedSourceChanges()
        {
            var (driver, _) = TrackingDriver([Asset("Ground.sdtex"), Map(".sdtex|Stride.Graphics.Texture")]);

            var result = driver.RunGenerators(CreateCompilation("public class TestDummy { }"));
            // An unrelated source edit produces a new compilation but the same assets, resolved
            // types and config, so the source emit must stay cached instead of regenerating.
            result = result.RunGenerators(CreateCompilation("public class TestDummy { public void Extra() { } }"));

            Assert.All(OutputReasons(result), reason => Assert.Equal(IncrementalStepRunReason.Cached, reason));
        }

        [Fact]
        public void EmitRefiresWhenAssetsChange()
        {
            var (driver, texts) = TrackingDriver([Asset("Ground.sdtex")]);
            var result = driver.RunGenerators(CreateCompilation("public class TestDummy { }"));

            // Rename the asset: its relative path changes, so the emit must re-run (guards against
            // the custom array comparer over-caching) and reflect the renamed constant.
            var renamed = new TestAdditionalText(Path.Combine(AssetFolder, "Water.sdtex"), "");
            result = result.ReplaceAdditionalText(texts[0], renamed)
                .RunGenerators(CreateCompilation("public class TestDummy { }"));

            Assert.Contains(OutputReasons(result), reason => reason != IncrementalStepRunReason.Cached);
        }

        private static IEnumerable<IncrementalStepRunReason> OutputReasons(GeneratorDriver driver)
            => driver.GetRunResult().Results.Single().TrackedOutputSteps
                .SelectMany(pair => pair.Value)
                .SelectMany(step => step.Outputs)
                .Select(output => output.Reason);

        private static (GeneratorDriver Driver, AdditionalText[] Texts) TrackingDriver((string Path, string Content)[] files)
        {
            var texts = files.Select(file => (AdditionalText)new TestAdditionalText(file.Path, file.Content)).ToArray();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                [new AssetUrlConstantsGenerator().AsSourceGenerator()],
                additionalTexts: texts,
                parseOptions: null,
                optionsProvider: new PrefixOptionsProvider(GlobalOptions("MyGame")),
                driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            return (driver, texts);
        }

        private static Compilation CreateCompilation(string source)
            => CSharpCompilation.Create("MyGame.Game",
                [CSharpSyntaxTree.ParseText(source)],
                References.Value,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // The generator types and includes assets purely by extension, so the file content is
        // irrelevant (never read); only the path and its StrideAssetFolder root matter.
        private static (string Path, string Content) Asset(string relativePath)
            => (Path.Combine(AssetFolder, relativePath), "");

        private static (string Path, string Content) Map(string content)
            => (@"C:\maps\Test.AssetContentTypeMap.txt", content);

        private static Dictionary<string, string> GlobalOptions(string urlNamespace) => new()
        {
            ["build_property.RootNamespace"] = "MyGame",
            ["build_property.StrideAssetUrlNamespace"] = urlNamespace,
        };

        private sealed record RunResult(string? Source, ImmutableArray<Diagnostic> Diagnostics, Compilation Output);

        private static RunResult Run(
            (string Path, string Content)[] files,
            string urlNamespace = "MyGame",
            string source = "public class TestDummy { }")
        {
            var compilation = CSharpCompilation.Create("MyGame.Game",
                [CSharpSyntaxTree.ParseText(source)],
                References.Value,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var texts = files.Select(file => (AdditionalText)new TestAdditionalText(file.Path, file.Content)).ToArray();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                [new AssetUrlConstantsGenerator().AsSourceGenerator()],
                additionalTexts: texts,
                optionsProvider: new PrefixOptionsProvider(GlobalOptions(urlNamespace)));
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

            var generated = driver.GetRunResult().Results.Single().GeneratedSources;
            return new RunResult(generated.Length > 0 ? generated[0].SourceText.ToString() : null, diagnostics, output);
        }

        private static void AssertCompiles(RunResult result)
        {
            var errors = result.Output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();
            Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
        }

        private static readonly Lazy<PortableExecutableReference[]> References = new(() =>
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .Select(path => MetadataReference.CreateFromFile(path))
                .ToArray());

        private sealed class TestAdditionalText(string path, string content) : AdditionalText
        {
            public override string Path => path;
            public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(content);
        }

        private sealed class TestOptions(Dictionary<string, string>? values) : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, out string value)
            {
                value = null!;
                return values?.TryGetValue(key, out value!) == true;
            }
        }

        // Mirrors the build task's tagging: files under an asset folder carry StrideAssetFolder,
        // the checked-in map carries StrideAssetContentTypeMap. Prefix-based so a renamed asset
        // still resolves its folder.
        private sealed class PrefixOptionsProvider(Dictionary<string, string> globalOptions) : AnalyzerConfigOptionsProvider
        {
            public override AnalyzerConfigOptions GlobalOptions { get; } = new TestOptions(globalOptions);
            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new TestOptions(null);
            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
            {
                var options = new Dictionary<string, string>();
                if (textFile.Path.StartsWith(AssetFolder + @"\", StringComparison.OrdinalIgnoreCase))
                    options["build_metadata.AdditionalFiles.StrideAssetFolder"] = AssetFolder;
                else if (textFile.Path.EndsWith("AssetContentTypeMap.txt", StringComparison.OrdinalIgnoreCase))
                    options["build_metadata.AdditionalFiles.StrideAssetContentTypeMap"] = "true";
                return new TestOptions(options);
            }
        }
    }
}
