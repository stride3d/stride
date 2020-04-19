// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D12
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Direct3D12;

namespace Stride.Graphics
{
    /// <summary>
    /// Features supported by a <see cref="GraphicsDevice"/>.
    /// </summary>
    /// <remarks>
    /// This class gives also features for a particular format, using the operator this[dxgiFormat] on this structure.
    /// </remarks>
    public partial struct GraphicsDeviceFeatures
    {
        private static readonly List<SharpDX.DXGI.Format> ObsoleteFormatToExcludes = new List<SharpDX.DXGI.Format>() { Format.R1_UNorm, Format.B5G6R5_UNorm, Format.B5G5R5A1_UNorm };

        internal GraphicsDeviceFeatures(GraphicsDevice deviceRoot)
        {
            var nativeDevice = deviceRoot.NativeDevice;

            HasSRgb = true;

            mapFeaturesPerFormat = new FeaturesPerFormat[256];

            // Set back the real GraphicsProfile that is used
            // TODO D3D12
            RequestedProfile = deviceRoot.RequestedProfile;
            CurrentProfile = GraphicsProfileHelper.FromFeatureLevel(deviceRoot.CurrentFeatureLevel);

            // TODO D3D12
            HasComputeShaders = true;
            HasDoublePrecision = nativeDevice.D3D12Options.DoublePrecisionFloatShaderOps;

            // TODO D3D12 Confirm these are correct
            // Some docs: https://msdn.microsoft.com/en-us/library/windows/desktop/ff476876(v=vs.85).aspx
            HasDepthAsSRV = true;
            HasDepthAsReadOnlyRT = true;
            HasMultisampleDepthAsSRV = true;

            HasResourceRenaming = false;

            HasMultiThreadingConcurrentResources = true;
            HasDriverCommandLists = true;

            // Check features for each DXGI.Format
            foreach (var format in Enum.GetValues(typeof(SharpDX.DXGI.Format)))
            {
                var dxgiFormat = (SharpDX.DXGI.Format)format;
                var maximumMultisampleCount = MultisampleCount.None;
                var formatSupport = FormatSupport.None;

                if (!ObsoleteFormatToExcludes.Contains(dxgiFormat))
                {
                    SharpDX.Direct3D12.FeatureDataFormatSupport formatSupportData;
                    formatSupportData.Format = dxgiFormat;
                    formatSupportData.Support1 = FormatSupport1.None;
                    formatSupportData.Support2 = FormatSupport2.None;
                    if (nativeDevice.CheckFeatureSupport(SharpDX.Direct3D12.Feature.FormatSupport, ref formatSupportData))
                        formatSupport = (FormatSupport)formatSupportData.Support1;
                    maximumMultisampleCount = GetMaximumMultisampleCount(nativeDevice, dxgiFormat);
                }

                mapFeaturesPerFormat[(int)dxgiFormat] = new FeaturesPerFormat((PixelFormat)dxgiFormat, maximumMultisampleCount, formatSupport);
            }
        }

        /// <summary>
        /// Gets the maximum multisample count for a particular <see cref="PixelFormat" />.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="pixelFormat">The pixelFormat.</param>
        /// <returns>The maximum multisample count for this pixel pixelFormat</returns>
        private static MultisampleCount GetMaximumMultisampleCount(SharpDX.Direct3D12.Device device, SharpDX.DXGI.Format pixelFormat)
        {
            SharpDX.Direct3D12.FeatureDataMultisampleQualityLevels qualityLevels;
            qualityLevels.Format = pixelFormat;
            qualityLevels.Flags = MultisampleQualityLevelFlags.None;
            qualityLevels.QualityLevelCount = 0;

            int maxCount = 1;
            for (int i = 8; i >= 1; i /= 2)
            {
                qualityLevels.SampleCount = i;
                if (device.CheckFeatureSupport(SharpDX.Direct3D12.Feature.MultisampleQualityLevels, ref qualityLevels))
                {
                    maxCount = i;
                    break;
                }
            }
            return (MultisampleCount)maxCount;
        }
    }
}
#endif
