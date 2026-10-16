// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.IO;
using System.Text;
using Stride.Core.Assets;
using Stride.Core.Mathematics;
using Stride.Physics.Assets;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// A heightmap saved when its types lived in Stride.Assets names them with their implicit tags from that assembly.
    /// </summary>
    public class TestHeightmapAssetCompatibility
    {
        [Fact]
        public void HeightmapWithTheStrideAssetsTypeTagsLoads()
        {
            var yaml = """
                !HeightmapAsset
                Id: 3a7e0b91-5c2d-4e6f-8a1b-9c0d1e2f3a4b
                SerializedVersion: {Stride: 3.0.0.0}
                Tags: []
                Source: null
                HeightConversionParameters: !Stride.Assets.Physics.FloatHeightmapHeightConversionParamters,Stride.Assets
                    HeightRange: {X: -5.0, Y: 5.0}
                Resize: !Stride.Assets.Physics.HeightmapResizingParameters,Stride.Assets
                    Enabled: true
                    Size: {X: 256, Y: 256}
                sRGB sampling: false
                """;

            var result = AssetFileSerializer.Load<HeightmapAsset>(new MemoryStream(Encoding.UTF8.GetBytes(yaml)), "Terrain.sdhmap");

            var conversion = Assert.IsType<FloatHeightmapHeightConversionParamters>(result.Asset.HeightConversionParameters);
            Assert.Equal(new Vector2(-5, 5), conversion.HeightRange);
            Assert.True(result.Asset.Resizing.Enabled);
            Assert.Equal(new Int2(256, 256), result.Asset.Resizing.Size);
        }
    }
}
