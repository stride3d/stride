// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_NULL

namespace Stride.Graphics
{
    /// <summary>
    /// Features supported by a <see cref="GraphicsDevice"/>.
    /// </summary>
    /// <remarks>This class gives also features for a particular format, using the operator this[Format] on this structure. </remarks>
    public partial struct GraphicsDeviceFeatures
    {
        internal GraphicsDeviceFeatures(GraphicsDevice deviceRoot)
        {
            NullHelper.ToImplement();
            mapFeaturesPerFormat = new FeaturesPerFormat[256];
            for (int i = 0; i < mapFeaturesPerFormat.Length; i++)
                mapFeaturesPerFormat[i] = new FeaturesPerFormat((PixelFormat)i, MultisampleCount.None, FormatSupport.None);
            HasComputeShaders = true;
            HasDepthAsReadOnlyRT = false;
            HasDepthAsSRV = true;
            HasMultisampleDepthAsSRV = false;
            HasDoublePrecision = true;
            HasDriverCommandLists = true;
            HasMultiThreadingConcurrentResources = true;
            HasResourceRenaming = true;
            HasSRgb = true;
            RequestedProfile = GraphicsProfile.Level_11_2;
            CurrentProfile = GraphicsProfile.Level_11_2;
        }
    }
}
#endif
