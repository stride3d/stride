// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using Xunit;

using Stride.Graphics.Regression;

namespace Stride.Graphics.Tests
{
    /// <summary>
    ///   The maximum sample count per format. The device only asks the driver for 2D-texture formats (some drivers remove
    ///   the device for other formats); the result must not change for the formats Stride multisamples.
    /// </summary>
    public class TestFormatMultisampleSupport : GameTestBase
    {
        [SkippableFact]
        public void TypelessFormatsKeepTheirMultisampleCount()
        {
            PerformTest(
                game =>
                {
                    var features = game.GraphicsDevice.Features;

                    // Not a texture format: 1 sample (None), as drivers answer for it
                    Assert.Equal(MultisampleCount.None, features[PixelFormat.None].MultisampleCountMax);

                    // Typeless formats report no multisample flags, but multisampled depth textures are created typeless
                    Assert.Equal(features[PixelFormat.D24_UNorm_S8_UInt].MultisampleCountMax, features[PixelFormat.R24G8_Typeless].MultisampleCountMax);
                    Assert.Equal(features[PixelFormat.D32_Float].MultisampleCountMax, features[PixelFormat.R32_Typeless].MultisampleCountMax);
                    Assert.Equal(features[PixelFormat.R8G8B8A8_UNorm].MultisampleCountMax, features[PixelFormat.R8G8B8A8_Typeless].MultisampleCountMax);
                    Assert.True(features[PixelFormat.R8G8B8A8_Typeless].MultisampleCountMax > MultisampleCount.None);
                });
        }
    }
}

#endif
