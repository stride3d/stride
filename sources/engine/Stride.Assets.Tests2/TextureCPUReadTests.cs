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
using Parameters = (bool mips, bool streamed, bool compressed);

namespace Stride.Assets.Tests2;

public class TextureCPUReadTests : GameTestBase
{
    [Fact]
    public void TextureCPUReadTest()
    {
        PerformTest(game =>
            {
                var data = new (UrlReference<Texture> url, Parameters parameters)[]
                {
                    (Assets.CPUCompressed, (false, false, true)),
                    (Assets.CPUMips, (true, false, false)),
                    (Assets.CPUMipsCompressed, (true, false, true)),
                    (Assets.CPUNone, (false, false, false)),
                    (Assets.CPUStreamed, (false, true, false)),
                    (Assets.CPUStreamedCompressed, (false, true, true)),
                    (Assets.CPUStreamedMips, (true, true, false)),
                    (Assets.CPUStreamedMipsCompressed, (true, true, true)),
                };

                var content = new ContentManager(game.Services.GetService<IDatabaseFileProviderService>());
                foreach (var (url, parameters) in data)
                {
                    // Regardless of 'streamed', the full texture should still be output with this set of services
                    var loadedTexture = content.Load(url, parameters.streamed ? null : ContentManagerLoaderSettings.StreamingDisabled);
                    var baseImage = loadedTexture.GetSerializationData().Image;

                    Assert.Equal(1024, baseImage.Description.Height);
                    Assert.Equal(1024, baseImage.Description.Width);
                    Assert.Equal(parameters.mips, baseImage.Description.MipLevels > 1);
                    Assert.Equal(parameters.compressed, baseImage.Description.Format.IsCompressed);
                    var firstPixelValue = baseImage.GetPixelBuffer(0, 0, 0).GetPixel<Color>(0, 0);
                    if (parameters.compressed)
                    {
                        // Will likely differ based on the compression method it defaults to, 
                        // fine to correct when the default changes
                        Assert.Equal(new Color(31, 248, 31, 248), firstPixelValue);
                    }
                    else
                    {
                        Assert.Equal(new Color(255, 0, 255, 255), firstPixelValue);
                    }

                    content.Unload(loadedTexture);
                }
            });
    }
}
