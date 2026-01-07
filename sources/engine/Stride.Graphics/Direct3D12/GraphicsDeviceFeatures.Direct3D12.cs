// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
using System.Linq;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

using DXGIFormat = Silk.NET.DXGI.Format;

using static System.Runtime.CompilerServices.Unsafe;

namespace Stride.Graphics;

public unsafe partial struct GraphicsDeviceFeatures
{
    private static readonly DXGIFormat[] ObsoleteFormatToExclude =
    [
        DXGIFormat.FormatR1Unorm,
        DXGIFormat.FormatB5G6R5Unorm,
        DXGIFormat.FormatB5G5R5A1Unorm
    ];

    internal GraphicsDeviceFeatures(GraphicsDevice device)
    {
        var nativeDevice = device.NativeDevice;

        HasSRgb = true;

        mapFeaturesPerFormat = new FeaturesPerFormat[256];

        // Set back the real GraphicsProfile that is used
        // TODO D3D12
        RequestedProfile = device.RequestedProfile;
        CurrentProfile = GraphicsProfileHelper.FromFeatureLevel(device.CurrentFeatureLevel);

        // TODO D3D12
        HasComputeShaders = true;

        SkipInit(out FeatureDataD3D12Options d3D12Options);
        HResult result = nativeDevice.CheckFeatureSupport(Feature.D3D12Options, ref d3D12Options, (uint) SizeOf<FeatureDataD3D12Options>());

        if (result.IsFailure)
            result.Throw();

        HasDoublePrecision = d3D12Options.DoublePrecisionFloatShaderOps;

        // TODO: D3D12: Confirm these are correct
        // Some docs: https://msdn.microsoft.com/en-us/library/windows/desktop/ff476876(v=vs.85).aspx
        HasDepthAsSRV = true;
        HasDepthAsReadOnlyRT = true;
        HasMultiSampleDepthAsSRV = true;

        HasResourceRenaming = false;

        HasMultiThreadingConcurrentResources = true;
        HasDriverCommandLists = true;

        // Check features for each DXGI.Format
        foreach (var format in Enum.GetValues<DXGIFormat>())
        {
            if (format == DXGIFormat.FormatForceUint)
                continue;

            var maximumMultisampleCount = MultisampleCount.None;
            var formatSupport = FormatSupport.None;
            var csFormatSupport = ComputeShaderFormatSupport.None;

            if (!ObsoleteFormatToExclude.Contains(format))
            {
                CheckFormatSupport(format, out formatSupport, out csFormatSupport, out maximumMultisampleCount);
            }

            mapFeaturesPerFormat[(int) format] = new((PixelFormat) format, maximumMultisampleCount, csFormatSupport, formatSupport);
        }

        /// <summary>
        ///   Gets the maximum sample count when enabling multi-sampling for a particular <see cref="Format"/>.
        /// </summary>
        MultisampleCount GetMaximumMultisampleCount(DXGIFormat pixelFormat)
        {
            FeatureDataMultisampleQualityLevels qualityLevels = new()
            {
                Format = pixelFormat,
                Flags = MultisampleQualityLevelFlags.None,
                NumQualityLevels = 0
            };
            var sizeInBytes = (uint) SizeOf<FeatureDataMultisampleQualityLevels>();

            uint maxCount = 1;
            for (uint i = 8; i >= 1; i /= 2)
            {
                qualityLevels.SampleCount = i;

                HResult result = nativeDevice.CheckFeatureSupport(Feature.MultisampleQualityLevels, ref qualityLevels, sizeInBytes);

                if (result.IsSuccess)
                {
                    maxCount = i;
                    break;
                }
            }
            return (MultisampleCount) maxCount;
        }

        /// <summary>
        ///   Check the support the Direct3D device has for the specified format.
        /// </summary>
        void CheckFormatSupport(DXGIFormat format,
                                out FormatSupport formatSupport,
                                out ComputeShaderFormatSupport csFormatSupport,
                                out MultisampleCount maximumMultisampleCount)
        {
            FeatureDataFormatSupport formatSupportData = new()
            {
                Format = format,
                Support1 = FormatSupport1.None,
                Support2 = FormatSupport2.None
            };

            HResult result = nativeDevice.CheckFeatureSupport(Feature.FormatSupport, ref formatSupportData, (uint) SizeOf<FeatureDataFormatSupport>());

            if (result.IsFailure)
            {
                // Format not supported
                formatSupport = FormatSupport.None;
                csFormatSupport = ComputeShaderFormatSupport.None;

                maximumMultisampleCount = MultisampleCount.None;
            }
            else
            {
                formatSupport = (FormatSupport) formatSupportData.Support1;
                csFormatSupport = (ComputeShaderFormatSupport) formatSupportData.Support2;

                maximumMultisampleCount = GetMaximumMultisampleCount(format);
            }
        }
    }
}

#endif
