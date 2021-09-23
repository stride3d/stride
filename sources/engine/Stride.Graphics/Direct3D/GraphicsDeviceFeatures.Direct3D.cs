// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D11
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
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Stride.Graphics.Direct3D;
using Feature = Silk.NET.Direct3D11.Feature;

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
        private static readonly List<Format> ObsoleteFormatToExcludes = new() { Format.FormatR1Unorm, Format.FormatB5G6R5Unorm, Format.FormatB5G5R5A1Unorm };

        internal GraphicsDeviceFeatures(GraphicsDevice deviceRoot)
        {
            var nativeDevice = deviceRoot.NativeDevice;

            HasSRgb = true;

            mapFeaturesPerFormat = new FeaturesPerFormat[256];

            // Set back the real GraphicsProfile that is used
            RequestedProfile = deviceRoot.RequestedProfile;
            CurrentProfile = GraphicsProfileHelper.FromFeatureLevel(nativeDevice.GetFeatureLevel());

            HasResourceRenaming = true;
            unsafe 
            {
                uint n = 0;
                FeatureDataThreading dt;
                HasComputeShaders = (uint)nativeDevice.CheckFeatureSupport(Silk.NET.Direct3D11.Feature.FeatureD3D10XHardwareOptions, null, n) == (uint)ReturnCodes.S_OK;
                HasDoublePrecision = (uint)nativeDevice.CheckFeatureSupport(Silk.NET.Direct3D11.Feature.FeatureDoubles,null, n) == (uint)ReturnCodes.S_OK;
                HasDriverCommandLists = (uint)nativeDevice.CheckFeatureSupport(Silk.NET.Direct3D11.Feature.FeatureThreading, &dt, n) == (uint)ReturnCodes.S_OK;
                HasMultiThreadingConcurrentResources = dt.DriverConcurrentCreates>0;
            }            

            HasDepthAsSRV = (CurrentProfile >= GraphicsProfile.Level_10_0);
            HasDepthAsReadOnlyRT = CurrentProfile >= GraphicsProfile.Level_11_0;
            HasMultisampleDepthAsSRV = CurrentProfile >= GraphicsProfile.Level_11_0;

            // Check features for each DXGI.Format
            foreach (var format in Enum.GetValues(typeof(Format)))
            {
                var dxgiFormat = (Format)format;
                var maximumMultisampleCount = MultisampleCount.None;
                
                var computeShaderFormatSupport = FormatSupport.None;
                var formatSupport = FormatSupport.None;

                if (!ObsoleteFormatToExcludes.Contains(dxgiFormat))
                {
                    maximumMultisampleCount = GetMaximumMultisampleCount(nativeDevice, dxgiFormat);
                    
                    unsafe
                    {
                        uint res = 0;
                        if (HasComputeShaders)
                        {
                            //TODO : To review, this seems very weird
                            
                            nativeDevice.CheckFormatSupport(dxgiFormat,&res);
                            computeShaderFormatSupport = (FormatSupport)res;
                        }
                        nativeDevice.CheckFormatSupport(dxgiFormat, &res);
                        formatSupport = (FormatSupport)res;

                    }
                        

                    
                }

                //mapFeaturesPerFormat[(int)dxgiFormat] = new FeaturesPerFormat((PixelFormat)dxgiFormat, maximumMultisampleCount, computeShaderFormatSupport, formatSupport);
                mapFeaturesPerFormat[(int)dxgiFormat] = new FeaturesPerFormat((PixelFormat)dxgiFormat, maximumMultisampleCount, formatSupport);
            }
        }

        /// <summary>
        /// Gets the maximum multisample count for a particular <see cref="PixelFormat" />.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="pixelFormat">The pixelFormat.</param>
        /// <returns>The maximum multisample count for this pixel pixelFormat</returns>
        private static MultisampleCount GetMaximumMultisampleCount(ID3D11Device device, Format pixelFormat)
        {
            int maxCount = 1;
            for (int i = 1; i <= 8; i *= 2)
            {
                unsafe
                {
                    uint res = 0;
                    device.CheckMultisampleQualityLevels(pixelFormat, (uint)i, &res);
                    if (res != 0)
                        maxCount = i;
                }
                
            }
            return (MultisampleCount)maxCount;
        }
    }
}
#endif
