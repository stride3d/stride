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
    public async Task No_Error_When_Declared()
    {
        var sourceCode = string.Format(Source, """[assembly: Stride.Core.Serialization.AssetFileExtension(".sdspin", typeof(Plugin.SpinData))]""");
        await TestHelper.ExpectNoDiagnosticsAsync(sourceCode);
    }

    [Fact]
    public async Task Error_When_Not_Declared()
    {
        var sourceCode = string.Format(Source, "");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG014UndeclaredAssetFileExtension.DiagnosticId);
    }

    [Fact]
    public async Task Error_When_Declared_With_Another_Type()
    {
        var sourceCode = string.Format(Source, """[assembly: Stride.Core.Serialization.AssetFileExtension(".sdspin", typeof(object))]""");
        await TestHelper.ExpectDiagnosticAsync(sourceCode, STRDIAG014UndeclaredAssetFileExtension.DiagnosticId);
    }
}
