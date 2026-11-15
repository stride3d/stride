using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

public class STRDIAG014_Test
{
    // The asset attributes (Stride.Core.Assets) and the runtime declaration (Stride.Core.Serialization) the analyzer matches
    // by name, and an asset type of a plugin compiling .sdspin files to SpinData
    private const string Source = """
        {0}
        namespace Stride.Core.Assets
        {{
            public class AssetDescriptionAttribute : System.Attribute {{ public AssetDescriptionAttribute(string fileExtensions) {{ }} }}
            public class AssetContentTypeAttribute : System.Attribute {{ public AssetContentTypeAttribute(System.Type contentType) {{ }} }}
        }}
        namespace Stride.Core.Serialization
        {{
            [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
            public class AssetFileExtensionAttribute : System.Attribute {{ public AssetFileExtensionAttribute(string extension, System.Type contentType) {{ }} }}
        }}
        namespace Plugin
        {{
            public class SpinData {{ }}

            [Stride.Core.Assets.AssetDescription(".sdspin")]
            [Stride.Core.Assets.AssetContentType(typeof(SpinData))]
            public class SpinAsset {{ }}

            public class FastSpinAsset : SpinAsset {{ }}
        }}
        """;

    [Fact]
    public async Task No_Warning_When_Declared()
    {
        var sourceCode = string.Format(Source, """[assembly: Stride.Core.Serialization.AssetFileExtension(".sdspin", typeof(Plugin.SpinData))]""");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task Warning_When_Not_Declared()
    {
        var sourceCode = string.Format(Source, "");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG014UndeclaredAssetFileExtension.DiagnosticId);
    }

    [Fact]
    public async Task Warning_When_Declared_With_Another_Type()
    {
        var sourceCode = string.Format(Source, """[assembly: Stride.Core.Serialization.AssetFileExtension(".sdspin", typeof(object))]""");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG014UndeclaredAssetFileExtension.DiagnosticId);
    }

    [Fact]
    public async Task Warning_When_Declared_Outside_The_Content_Type_Assembly()
    {
        // An Assets assembly declaring its runtime's content type: games reference the runtime only, so they get no constant
        var sourceCode = string.Format(Source.Replace("typeof(SpinData)", "typeof(System.Uri)"),
            """[assembly: Stride.Core.Serialization.AssetFileExtension(".sdspin", typeof(System.Uri))]""");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG014UndeclaredAssetFileExtension.DiagnosticId);
    }

    [Fact]
    public async Task Message_Points_To_The_Package_Runtime_Assembly()
    {
        // Plugin.Assets compiles .sdspin files to a content type of another assembly (here System.Uri), and references its runtime Plugin
        var runtime = CompilerUtils.CreateCompilation("namespace Plugin { public class Runtime { } }").WithAssemblyName("Plugin");
        var assets = CompilerUtils.CreateCompilation(string.Format(Source.Replace("typeof(SpinData)", "typeof(System.Uri)"), ""))
            .WithAssemblyName("Plugin.Assets")
            .AddReferences(runtime.ToMetadataReference());

        var diagnostic = Assert.Single(await assets.GetAnalyzerDiagnosticsAsync(new STRDIAG014UndeclaredAssetFileExtension()), d => d.GetMessage().Contains("'SpinAsset'"));
        Assert.Contains("to 'Plugin', the runtime assembly of this package", diagnostic.GetMessage());
    }

    [Fact]
    public async Task Message_Without_A_Package_Runtime_Assembly()
    {
        var compilation = CompilerUtils.CreateCompilation(string.Format(Source, ""));

        var diagnostic = Assert.Single(await compilation.GetAnalyzerDiagnosticsAsync(new STRDIAG014UndeclaredAssetFileExtension()), d => d.GetMessage().Contains("'SpinAsset'"));
        Assert.Contains("your own runtime project, or 'TestAssembly' if you can change it", diagnostic.GetMessage());
    }
}
