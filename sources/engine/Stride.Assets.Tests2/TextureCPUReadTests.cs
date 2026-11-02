// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.Graphics.Regression;
using Xunit;

namespace Stride.Assets.Tests2;

public class TextureCPUReadTests : GameTestBase
{
    [Fact]
    public void TextureCPUReadTest()
    {
        PerformTest(game =>
            {
                var data = new (UrlReference<Texture> url, bool mips, bool streamed)[]
                {
                    (Assets.CPUNonStreamed, false, false),
                    (Assets.CPUStreamed, false, true),
                    (Assets.CPUNonStreamedMips, true, false),
                    (Assets.CPUStreamedMips, true, true),
                };

                var content = new ContentManager(game.Services.GetService<IDatabaseFileProviderService>());
                foreach (var (url, mips, streamed) in data)
                {
                    // Regardless of 'streamed', the full texture should still be output with this set of services
                    var loadedTexture = content.Load(url, streamed ? null : ContentManagerLoaderSettings.StreamingDisabled);
                    var baseImage = loadedTexture.GetSerializationData().Image;

                    Assert.Equal(1024, baseImage.Description.Height);
                    Assert.Equal(1024, baseImage.Description.Width);
                    Assert.Equal(mips, baseImage.Description.MipLevels > 1);
                    Assert.Equal(PixelFormat.R8G8B8A8_UNorm, baseImage.Description.Format);
                    var firstPixelValue = baseImage.GetPixelBuffer(0, 0, 0).GetPixel<Color>(0, 0);
                    Assert.Equal(new Color(255, 0, 255, 255), firstPixelValue);

                    content.Unload(loadedTexture);
                }
            });
    }
}
