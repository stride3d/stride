// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using Xunit;
using Stride.Core.Assets.Templates;
using Stride.Core.Yaml;

namespace Stride.Core.Assets.Tests
{
    public class TestTemplateAssetDescription
    {
        [Fact]
        public void LoadsFactoryTemplateWithPrompts()
        {
            const string yaml = """
                !TemplateAssetFactory
                Id: FDBCB65A-9866-4EF2-9A03-AF21F943BDFD
                AssetTypeName: HullAsset
                Name: Convex hull
                Scope: Asset
                FactoryTypeName: HullAssetFactory
                Prompts:
                    - !AssetReferencePrompt
                      Member: Model
                      Message: Select a model
                      AssetTypes: [IModelAsset]
                """;

            var file = Path.GetTempFileName();
            File.WriteAllText(file, yaml);
            var description = (TemplateAssetFactoryDescription)YamlSerializer.Load<TemplateDescription>(file);
            File.Delete(file);

            var prompt = Assert.IsType<AssetReferencePrompt>(Assert.Single(description.Prompts));
            Assert.Equal("Model", prompt.Member);
            Assert.Equal("Select a model", prompt.Message);
            Assert.Equal("IModelAsset", Assert.Single(prompt.AssetTypes));
        }
    }
}
