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
                Asset(@"Ground Textures\Skybox texture.sdtex", "!Texture"),
                Map("Texture|Stride.Graphics.Texture"),
            ]);

            Assert.Contains("internal static partial class Assets", result.Source);
            Assert.Contains("internal static partial class Ground_Textures", result.Source);
            Assert.Contains("global::Stride.Core.Serialization.UrlReference<global::Stride.Graphics.Texture> Skybox_texture", result.Source);
            Assert.Contains("\"/MyGame/Ground Textures/Skybox texture\"", result.Source);
            AssertCompiles(result);
        }

        [Fact]
        public void UntypedFallbackForUnknownTag()
        {
            var result = Run([Asset("Thing.sdcustom", "!SomeUnknownCustomTag")]);

            Assert.Contains("global::Stride.Core.Serialization.UrlReference Thing", result.Source);
            Assert.DoesNotContain("UrlReference<", result.Source);
            AssertCompiles(result);
        }

        [Fact]
        public void UnqualifiedUrlWhenNoNamespace()
        {
            var result = Run([Asset("Ground.sdtex", "!Texture")], urlNamespace: "");

            Assert.Contains("\"Ground\"", result.Source);
            Assert.DoesNotContain("\"/", result.Source);
        }

        [Fact]
        public void SanitizationAndCollisionSuffix()
        {
            var result = Run(
            [
                Asset("A b.sdtex", "!UnknownTagKeepsThemUntyped"),
                Asset("A_b.sdtex", "!UnknownTagKeepsThemUntyped"),
                Asset("2 Cool.sdtex", "!UnknownTagKeepsThemUntyped"),
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
            var result = Run([Asset(@"new\Ground.sdtex", "!Texture")]);

            Assert.Contains("internal static partial class @new", result.Source);
            Assert.Contains("\"/MyGame/new/Ground\"", result.Source);
            AssertCompiles(result);
        }

        [Fact]
        public void NamespaceConflictSkipsGeneration()
        {
            var result = Run(
                [Asset("Ground.sdtex", "!Texture")],
                source: "namespace MyGame.Assets { public class Foo { } }");

            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "STRDIAG013");
            Assert.Null(result.Source);
        }

        [Fact]
        public void CompilationSymbolsWinOverMap()
        {
            var result = Run(
            [
                Asset("Thing.sdcustom", "!CustomThing"),
                Map("CustomThing|Stride.Graphics.Texture"),
            ],
                source: """
                    using Stride.Core;
                    using Stride.Core.Assets;
                    namespace MyPlugin
                    {
                        public class CustomContent { }

                        [DataContract("CustomThing")]
                        [AssetContentType(typeof(CustomContent))]
                        public class CustomThingAsset { }
                    }
                    """);

            Assert.Contains("UrlReference<global::MyPlugin.CustomContent> Thing", result.Source);
            AssertCompiles(result);
        }

        [Fact]
        public void NonAssetFilesAreSkipped()
        {
            var result = Run(
            [
                (Path.Combine(AssetFolder, "shader.sdsl"), "shader Foo {}", AssetFolder, false),
                (Path.Combine(AssetFolder, "note.txt"), "hello", AssetFolder, false),
                (Path.Combine(AssetFolder, "MyGame.Game.sdpkg"), "!Package\nId: 00000000-0000-0000-0000-000000000002\n", AssetFolder, false),
            ]);

            Assert.Null(result.Source);
        }

        [Fact]
        public void EmitIsCachedWhenOnlyUnrelatedSourceChanges()
        {
            var (driver, _) = TrackingDriver([Asset("Ground.sdtex", "!Texture"), Map("Texture|Stride.Graphics.Texture")]);

            var result = driver.RunGenerators(CreateCompilation("public class TestDummy { }"));
            // An unrelated source edit produces a new compilation but the same assets, resolved
            // types and config, so the source emit must stay cached instead of regenerating.
            result = result.RunGenerators(CreateCompilation("public class TestDummy { public void Extra() { } }"));

            Assert.All(OutputReasons(result), reason => Assert.Equal(IncrementalStepRunReason.Cached, reason));
        }

        [Fact]
        public void EmitRefiresWhenAssetsChange()
        {
            var (driver, texts) = TrackingDriver([Asset("Ground.sdtex", "!Texture")]);
            var result = driver.RunGenerators(CreateCompilation("public class TestDummy { }"));

            // Retag the asset: its entry changes, so the emit must re-run (guards against the
            // custom array comparer over-caching) and reflect the new (untyped) constant.
            var retagged = new TestAdditionalText(texts[0].Path, "!Sound\nId: 00000000-0000-0000-0000-000000000001\n");
            result = result.ReplaceAdditionalText(texts[0], retagged)
                .RunGenerators(CreateCompilation("public class TestDummy { }"));

            Assert.Contains(OutputReasons(result), reason => reason != IncrementalStepRunReason.Cached);
        }

        private static IEnumerable<IncrementalStepRunReason> OutputReasons(GeneratorDriver driver)
            => driver.GetRunResult().Results.Single().TrackedOutputSteps
                .SelectMany(pair => pair.Value)
                .SelectMany(step => step.Outputs)
                .Select(output => output.Reason);

        private static (GeneratorDriver Driver, AdditionalText[] Texts) TrackingDriver((string Path, string Content, string? Folder, bool IsMap)[] files)
        {
            var texts = files.Select(file => (AdditionalText)new TestAdditionalText(file.Path, file.Content)).ToArray();
            var perFileOptions = files.ToDictionary(
                file => file.Path,
                file =>
                {
                    var options = new Dictionary<string, string>();
                    if (file.Folder != null)
                        options["build_metadata.AdditionalFiles.StrideAssetFolder"] = file.Folder;
                    if (file.IsMap)
                        options["build_metadata.AdditionalFiles.StrideAssetContentTypeMap"] = "true";
                    return options;
                });
            var globalOptions = new Dictionary<string, string>
            {
                ["build_property.RootNamespace"] = "MyGame",
                ["build_property.StrideAssetUrlNamespace"] = "MyGame",
            };
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                [new AssetUrlConstantsGenerator().AsSourceGenerator()],
                additionalTexts: texts,
                parseOptions: null,
                optionsProvider: new TestOptionsProvider(globalOptions, perFileOptions),
                driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            return (driver, texts);
        }

        private static Compilation CreateCompilation(string source)
            => CSharpCompilation.Create("MyGame.Game",
                [CSharpSyntaxTree.ParseText(source)],
                References.Value,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        private static (string Path, string Content, string? Folder, bool IsMap) Asset(string relativePath, string tag)
            => (Path.Combine(AssetFolder, relativePath), $"{tag}\nId: 00000000-0000-0000-0000-000000000001\n", AssetFolder, false);

        private static (string Path, string Content, string? Folder, bool IsMap) Map(string content)
            => (@"C:\maps\Test.AssetContentTypeMap.txt", content, null, true);

        private sealed record RunResult(string? Source, ImmutableArray<Diagnostic> Diagnostics, Compilation Output);

        private static RunResult Run(
            (string Path, string Content, string? Folder, bool IsMap)[] files,
            string urlNamespace = "MyGame",
            string source = "public class TestDummy { }")
        {
            var compilation = CSharpCompilation.Create("MyGame.Game",
                [CSharpSyntaxTree.ParseText(source)],
                References.Value,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var texts = files.Select(file => (AdditionalText)new TestAdditionalText(file.Path, file.Content)).ToArray();
            var perFileOptions = files.ToDictionary(
                file => file.Path,
                file =>
                {
                    var options = new Dictionary<string, string>();
                    if (file.Folder != null)
                        options["build_metadata.AdditionalFiles.StrideAssetFolder"] = file.Folder;
                    if (file.IsMap)
                        options["build_metadata.AdditionalFiles.StrideAssetContentTypeMap"] = "true";
                    return options;
                });
            var globalOptions = new Dictionary<string, string>
            {
                ["build_property.RootNamespace"] = "MyGame",
                ["build_property.StrideAssetUrlNamespace"] = urlNamespace,
            };

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                [new AssetUrlConstantsGenerator().AsSourceGenerator()],
                additionalTexts: texts,
                optionsProvider: new TestOptionsProvider(globalOptions, perFileOptions));
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

        private sealed class TestOptionsProvider(Dictionary<string, string> globalOptions, Dictionary<string, Dictionary<string, string>> perFileOptions)
            : AnalyzerConfigOptionsProvider
        {
            public override AnalyzerConfigOptions GlobalOptions { get; } = new TestOptions(globalOptions);
            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new TestOptions(null);
            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new TestOptions(perFileOptions.TryGetValue(textFile.Path, out var options) ? options : null);
        }
    }
}
