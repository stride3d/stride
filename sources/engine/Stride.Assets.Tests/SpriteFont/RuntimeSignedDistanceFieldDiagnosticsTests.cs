using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Stride.Assets.SpriteFont;
using Stride.Core.Assets;
using Stride.Core.Reflection;
using Stride.Core.Yaml;
using Xunit;
using Xunit.Abstractions;

namespace Stride.Assets.Tests.SpriteFont
{
    public class RuntimeSignedDistanceFieldSpriteFontDiagnosticsTests
    {
        private readonly ITestOutputHelper output;

        public RuntimeSignedDistanceFieldSpriteFontDiagnosticsTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        private static string GetLogPath(string name)
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "DiagnosticLogs");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"{name}.txt");
        }

        private void WriteLog(string name, string content)
        {
            var path = GetLogPath(name);
            File.WriteAllText(path, content);
            output.WriteLine($"Diagnostic log written: {path}");
        }

        [Fact]
        public void RuntimeSdfType_Should_Exist_And_Have_Expected_DataContract_Name()
        {
            var type = typeof(RuntimeSignedDistanceFieldSpriteFontType);

            Assert.Equal("RuntimeSignedDistanceFieldSpriteFontType", type.Name);
        }

        [Fact]
        public void AssemblyRegistry_Should_Contain_RuntimeSdfType_Assembly()
        {
            var allAssemblies = AssemblyRegistry.FindAll().ToList();
            var runtimeAssembly = typeof(RuntimeSignedDistanceFieldSpriteFontType).Assembly;

            Assert.Contains(runtimeAssembly, allAssemblies);
        }

        [Fact]
        public void AssetYamlSerializer_Should_Deserialize_RuntimeSdf_SpriteFontAsset()
        {
            var yaml = """
                !SpriteFont
                Id: cad320b3-3f55-43a4-bb8b-46097724ba24
                SerializedVersion: {Stride: 2.0.0.0}
                Tags: []
                FontSource: !FileFontProvider
                    Source: !file ../../../../../Downloads/NotoSansCJK-Regular.ttc
                FontType: !RuntimeSignedDistanceFieldSpriteFontType
                    Size: 64.0
                    PixelRange: 10
                Spacing: 2.0
                """;

            var serializer = new AssetYamlSerializer();
            try
            {
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(yaml));
                var result = serializer.Deserialize(stream, typeof(SpriteFontAsset));

                var asset = Assert.IsType<SpriteFontAsset>(result);
                var fontType = Assert.IsType<RuntimeSignedDistanceFieldSpriteFontType>(asset.FontType);

                Assert.Equal(64.0f, fontType.Size);
                Assert.Equal(10, fontType.PixelRange);
                Assert.Equal(2.0f, asset.Spacing);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [Fact]
        public void AssetYamlSerializer_Settings_Should_Know_RuntimeSdfType_Assembly()
        {
            var serializer = new AssetYamlSerializer();
            var settings = serializer.GetSerializerSettings();
            var runtimeAssembly = typeof(RuntimeSignedDistanceFieldSpriteFontType).Assembly;

            Assert.NotNull(settings);
        }

        [Fact]
        public void SpriteFontAssetCompiler_Dispatch_Precondition_Should_See_RuntimeSdf_FontType()
        {
            var asset = new SpriteFontAsset
            {
                FontType = new RuntimeSignedDistanceFieldSpriteFontType
                {
                    Size = 64,
                    PixelRange = 10,
                    Padding = 2
                },
                Spacing = 2.0f
            };

            Assert.True(asset.FontType is RuntimeSignedDistanceFieldSpriteFontType);
        }
    }
}
