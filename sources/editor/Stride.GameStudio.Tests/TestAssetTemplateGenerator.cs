// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Assets.Presentation.Templates;
using Stride.Assets.Rendering;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core.Assets.Yaml;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Reflection;
using Stride.Core.Yaml;
using Xunit;

namespace Stride.GameStudio.Tests
{
    public class TestAssetTemplateGenerator
    {
        [Fact]
        public void AddedAssetKeepsTheOverridesItsGeneratorMarked()
        {
            var package = new Package();
            var generator = new OverridingGenerator();
            var parameters = new AssetTemplateGeneratorParameters(UDirectory.Empty)
            {
                Package = package,
                Name = "Compositor",
                Description = new TemplateAssetDescription(),
                Logger = new LoggerResult(),
            };

            Assert.True(generator.Run(parameters));

            var item = Assert.Single(package.Assets);
            var overrides = item.YamlMetadata.RetrieveMetadata(AssetObjectSerializerBackend.OverrideDictionaryKey);
            Assert.NotNull(overrides);
            Assert.Equal(OverrideType.New, overrides.TryGet(generator.OverriddenItem));
        }

        private sealed class OverridingGenerator : AssetTemplateGenerator
        {
            public YamlAssetPath OverriddenItem { get; } = new();

            public override bool IsSupportingTemplate(TemplateDescription templateDescription) => true;

            protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
            {
                var item = new AssetItem(GenerateLocation(parameters), new GraphicsCompositorAsset());
                OverriddenItem.PushMember(nameof(GraphicsCompositorAsset.RenderFeatures));
                OverriddenItem.PushItemId(ItemId.New());
                var overrides = new YamlAssetMetadata<OverrideType>();
                overrides.Set(OverriddenItem, OverrideType.New);
                item.YamlMetadata.AttachMetadata(AssetObjectSerializerBackend.OverrideDictionaryKey, overrides);
                return [item];
            }
        }
    }
}
