// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Stride.Assets.Generators;
using Xunit;

namespace Stride.Assets.Tests
{
    public class TestAssetUrlConstantsGenerator
    {
        [Fact]
        public void TypedWithNestedFolders()
        {
            var result = Run(
            [
                Asset(@"Ground Textures\Skybox texture.sdtex"),
            ]);

            Assert.Contains("internal static partial class Assets", result.Source);
            Assert.Contains("internal static partial class Ground_Textures", result.Source);
            Assert.Contains("global::Stride.Core.Serialization.UrlReference<global::Stride.Graphics.Texture> Skybox_texture", result.Source);
            Assert.Contains("\"/MyGame/Ground Textures/Skybox texture\"", result.Source);
            AssertCompiles(result);
        }

        [Fact]
        public void AssetPathsAreUriEscapedBeforePipeDelimited()
        {
            var result = Run([Asset(@"folder|name\Ground texture.sdtex")]);

            Assert.Contains("\"/MyGame/folder|name/Ground texture\"", result.Source);
            AssertCompiles(result);
        }

        [Fact]
        public void AssetAttributesRegisterCustomExtension()
        {
            var result = Run(
                [Asset("Thing.custom")],
                source: """
                    using Stride.Core.Assets;
                    namespace MyPlugin
                    {
                        public class CustomContent { }

                        [AssetDescription(".custom")]
                        [AssetContentType(typeof(CustomContent))]
                        public class CustomThingAsset { }
                    }
                    """);

            Assert.Contains("UrlReference<global::MyPlugin.CustomContent> Thing", result.Source);
            AssertCompiles(result);
        }

        [Fact]
        public void UnknownExtensionGetsNoConstant()
        {
            // An extension with no declaration or visible asset type is not an asset.
            var result = Run([Asset("Effect.sdsl")]);

            Assert.Null(result.Source);
        }

        [Fact]
        public void UnknownNonSdExtensionGetsNoConstant()
        {
            var result = Run([Asset("Thing.custom")]);

            Assert.Null(result.Source);
        }

        [Fact]
        public void FormerEngineExtensionStaysUntypedWithAWarning()
        {
            // A video the game's references do not declare (no Stride.Video): an untyped constant, as before the move,
            // and a warning naming the package
            var result = Run([Asset("Intro.sdvid")], withoutReference: "Stride.Video");

            Assert.Contains("global::Stride.Core.Serialization.UrlReference Intro", result.Source);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "STRDIAG015" && diagnostic.GetMessage().Contains("Stride.Video"));
            AssertCompiles(result);
        }

        [Fact]
        public void TypedViaAssemblyDeclaration()
        {
            // A plugin's runtime declares the extension of its Assets package's asset type, which the game does not reference
            var result = Run(
                [Asset("Thing.sdcustom")],
                source: """
                    [assembly: Stride.Core.Serialization.AssetFileExtension(".sdcustom", typeof(MyPlugin.CustomContent))]
                    namespace MyPlugin
                    {
                        public class CustomContent { }
                    }
                    """);

            Assert.Contains("UrlReference<global::MyPlugin.CustomContent> Thing", result.Source);
            AssertCompiles(result);
        }

        [Fact]
        public void TypedViaReferencedRuntimeDeclaration()
        {
            // Stride.Graphics declares .sdtex
            var result = Run([Asset("Ground.sdtex")]);

            Assert.Contains("UrlReference<global::Stride.Graphics.Texture> Ground", result.Source);
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
            ],
                source: "[assembly: Stride.Core.Serialization.AssetFileExtension(\".sdcustom\", typeof(Stride.Graphics.Texture))]");

            Assert.Contains(" A_b =", result.Source);
            Assert.Contains(" A_b_1 =", result.Source);
            Assert.Contains(" _2_Cool =", result.Source);
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
        public void CompilationSymbolsWinOverReferences()
        {
            var result = Run(
            [
                Asset("Thing.sdtex"),
            ],
                source: """
                    using Stride.Core.Assets;
                    namespace MyPlugin
                    {
                        public class CustomContent { }

                        [AssetDescription(".sdtex")]
                        [AssetContentType(typeof(CustomContent))]
                        public class CustomThingAsset { }
                    }
                    """);

            Assert.Contains("UrlReference<global::MyPlugin.CustomContent> Thing", result.Source);
            AssertCompiles(result);
        }

        [Fact]
        public void NoOutputWhenNoAssetPaths()
        {
            var result = Run([]);

            Assert.Null(result.Source);
        }

        [Fact]
        public void EmitIsCachedWhenOnlyUnrelatedSourceChanges()
        {
            var driver = TrackingDriver([Asset("Ground.sdtex")]);

            var result = driver.RunGenerators(CreateCompilation("public class TestDummy { }"));
            // An unrelated source edit produces a new compilation but the same assets, resolved
            // types and config, so the source emit must stay cached instead of regenerating.
            result = result.RunGenerators(CreateCompilation("public class TestDummy { public void Extra() { } }"));

            Assert.All(OutputReasons(result), reason => Assert.Equal(IncrementalStepRunReason.Cached, reason));
        }

        [Fact]
        public void EmitRefiresWhenAssetsChange()
        {
            var driver = TrackingDriver([Asset("Ground.sdtex")]);
            var result = driver.RunGenerators(CreateCompilation("public class TestDummy { }"));

            // Changing the build property's packed paths must re-run the emit and reflect the new asset.
            var updatedOptions = new TestOptionsProvider(GlobalOptions("MyGame", [Asset("Water.sdtex")]));
            result = result.WithUpdatedAnalyzerConfigOptions(updatedOptions)
                .RunGenerators(CreateCompilation("public class TestDummy { }"));

            Assert.Contains(OutputReasons(result), reason => reason != IncrementalStepRunReason.Cached);
            Assert.Contains(" Water =", result.GetRunResult().Results.Single().GeneratedSources.Single().SourceText.ToString());
        }

        private static IEnumerable<IncrementalStepRunReason> OutputReasons(GeneratorDriver driver)
            => driver.GetRunResult().Results.Single().TrackedOutputSteps
                .SelectMany(pair => pair.Value)
                .SelectMany(step => step.Outputs)
                .Select(output => output.Reason);

        private static CSharpGeneratorDriver TrackingDriver(string[] assetPaths)
        {
            return CSharpGeneratorDriver.Create(
                [new AssetUrlConstantsGenerator().AsSourceGenerator()],
                parseOptions: null,
                optionsProvider: new TestOptionsProvider(GlobalOptions("MyGame", assetPaths)),
                driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        }

        private static Compilation CreateCompilation(string source)
            => CSharpCompilation.Create("MyGame.Game",
                [CSharpSyntaxTree.ParseText(source)],
                References.Value,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        private static string Asset(string relativePath)
            => Uri.EscapeDataString(relativePath);

        private static Dictionary<string, string> GlobalOptions(string urlNamespace, string[] assetPaths) => new()
        {
            ["build_property.RootNamespace"] = "MyGame",
            ["build_property.StrideAssetUrlNamespace"] = urlNamespace,
            ["build_property.StrideAssetPaths"] = string.Join("|", assetPaths),
        };

        private sealed record RunResult(string? Source, ImmutableArray<Diagnostic> Diagnostics, Compilation Output);

        private static RunResult Run(
            string[] assetPaths,
            string urlNamespace = "MyGame",
            string source = "public class TestDummy { }",
            string? withoutReference = null)
        {
            var compilation = CSharpCompilation.Create("MyGame.Game",
                [CSharpSyntaxTree.ParseText(source)],
                References.Value.Where(reference => withoutReference is null || !string.Equals(Path.GetFileNameWithoutExtension(reference.FilePath), withoutReference, StringComparison.OrdinalIgnoreCase)),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                [new AssetUrlConstantsGenerator().AsSourceGenerator()],
                optionsProvider: new TestOptionsProvider(GlobalOptions(urlNamespace, assetPaths)));
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

        private sealed class TestOptions(Dictionary<string, string>? values) : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, out string value)
            {
                value = null!;
                return values?.TryGetValue(key, out value!) == true;
            }
        }

        private sealed class TestOptionsProvider(Dictionary<string, string> globalOptions) : AnalyzerConfigOptionsProvider
        {
            public override AnalyzerConfigOptions GlobalOptions { get; } = new TestOptions(globalOptions);
            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new TestOptions(null);
            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new TestOptions(null);
        }
    }
}
