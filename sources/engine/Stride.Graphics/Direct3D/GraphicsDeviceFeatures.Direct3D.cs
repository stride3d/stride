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
using System.Linq;
using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

using Feature = Silk.NET.Direct3D11.Feature;

namespace Stride.Graphics
{
    /// <summary>
    ///   Contains information about the general features supported by a <see cref="GraphicsDevice"/>, as well as
    ///   supported features specific to a particular pixel format or data format.
    /// </summary>
    /// <remarks>
    ///   To obtain information about the supported features for a particular format, use the operator
    ///   <see cref="this[PixelFormat]"/>.
    /// </remarks>
    public unsafe partial struct GraphicsDeviceFeatures
    {
        private static readonly Format[] ObsoleteFormatToExcludes = new[]
        {
            Format.FormatR1Unorm,
            Format.FormatB5G6R5Unorm,
            Format.FormatB5G5R5A1Unorm
        };

        internal GraphicsDeviceFeatures(GraphicsDevice deviceRoot)
        {
            var nativeDevice = deviceRoot.NativeDevice;

            HasSRgb = true;

            mapFeaturesPerFormat = new FeaturesPerFormat[256];

            // Set back the real GraphicsProfile that is used
            RequestedProfile = deviceRoot.RequestedProfile;
            CurrentProfile = GraphicsProfileHelper.FromFeatureLevel(nativeDevice->GetFeatureLevel());

            HasResourceRenaming = true;

            HasComputeShaders = CheckComputeShadersSupport();
            HasDoublePrecision = CheckDoubleOpsInShadersSupport();
            CheckThreadingSupport(out HasMultiThreadingConcurrentResources, out HasDriverCommandLists);

            HasDepthAsSRV = CurrentProfile >= GraphicsProfile.Level_10_0;
            HasDepthAsReadOnlyRT = CurrentProfile >= GraphicsProfile.Level_11_0;
            HasMultisampleDepthAsSRV = CurrentProfile >= GraphicsProfile.Level_11_0;

            // Check features for each DXGI.Format
            foreach (var format in Enum.GetValues<Format>())
            {
                if (format == Format.FormatForceUint)
                    continue;

                if (ObsoleteFormatToExcludes.Contains(format))
                    continue;

                var maximumMultisampleCount = GetMaximumMultisampleCount(nativeDevice, format);

                var computeShaderFormatSupport = HasComputeShaders
                    ? (ComputeShaderFormatSupport) CheckComputeShaderFormatSupport(format)
                    : ComputeShaderFormatSupport.None;

                var formatSupport = CheckFormatSupport(format);

                var pixelFormat = (PixelFormat) format;
                mapFeaturesPerFormat[(int) format] = new FeaturesPerFormat(pixelFormat, maximumMultisampleCount, computeShaderFormatSupport, formatSupport);
            }

            /// <summary>
            ///   Checks if the Direct3D device does support Compute Shaders.
            /// </summary>
            bool CheckComputeShadersSupport()
            {
                FeatureDataD3D10XHardwareOptions hwOptions;

                HResult result = nativeDevice->CheckFeatureSupport(Feature.D3D10XHardwareOptions,
                                                                   &hwOptions, (uint) Unsafe.SizeOf<FeatureDataD3D10XHardwareOptions>());

                if (result.IsFailure)
                    return false;

                return hwOptions.ComputeShadersPlusRawAndStructuredBuffersViaShader4X != 0;
            }

            /// <summary>
            ///   Checks if the Direct3D device does support double precision operations in shaders.
            /// </summary>
            bool CheckDoubleOpsInShadersSupport()
            {
                FeatureDataDoubles doubles;

                HResult result = nativeDevice->CheckFeatureSupport(Feature.Doubles,
                                                                   &doubles, (uint) Unsafe.SizeOf<FeatureDataDoubles>());

                if (result.IsFailure)
                    return false;

                return doubles.DoublePrecisionFloatShaderOps != 0;
            }

            /// <summary>
            ///   Checks if the Direct3D device does support threading.
            /// </summary>
            void CheckThreadingSupport(out bool supportsConcurrentResources, out bool supportsCommandLists)
            {
                FeatureDataThreading featureDataThreading;

                HResult result = nativeDevice->CheckFeatureSupport(Feature.Threading,
                                                                   &featureDataThreading, (uint) Unsafe.SizeOf<FeatureDataThreading>());
                if (result.IsFailure)
                {
                    supportsConcurrentResources = false;
                    supportsCommandLists = false;
                }
                else
                {
                    supportsConcurrentResources = featureDataThreading.DriverConcurrentCreates != 0;
                    supportsCommandLists = featureDataThreading.DriverCommandLists != 0;
                }
            }

            /// <summary>
            ///   Gets the maximum sample count when enabling multisampling for a particular <see cref="Format"/>.
            /// </summary>
            MultisampleCount GetMaximumMultisampleCount(ID3D11Device* device, Format pixelFormat)
            {
                uint maxCount = 1;

                for (uint sampleCount = 1; sampleCount <= 8; sampleCount *= 2)
                {
                    uint qualityLevels;

                    HResult result = device->CheckMultisampleQualityLevels(pixelFormat, sampleCount, &qualityLevels);

                    if (result.IsSuccess && qualityLevels != 0)
                        maxCount = sampleCount;
                }
                return (MultisampleCount) maxCount;
            }

            /// <summary>
            ///   Check if the Direct3D device does support compute shaders for the specified format.
            /// </summary>
            /// <returns>Flags indicating usage contexts in which the specified format is supported.</returns>
            FormatSupport2 CheckComputeShaderFormatSupport(Format format)
            {
                var dataFormatSupport2 = new FeatureDataFormatSupport2(format);

                HResult result = nativeDevice->CheckFeatureSupport(Feature.FormatSupport2,
                                                                   &dataFormatSupport2, (uint) Unsafe.SizeOf<FeatureDataFormatSupport2>());

                if (result.IsFailure)
                    return 0;

                return (FormatSupport2) dataFormatSupport2.OutFormatSupport2;
            }

            /// <summary>
            ///   Check the support the Direct3D device has for the specified format.
            /// </summary>
            /// <returns>Flags indicating usage contexts in which the specified format is supported.</returns>
            FormatSupport CheckFormatSupport(Format format)
            {
                var dataFormatSupport = new FeatureDataFormatSupport(format);

                HResult result = nativeDevice->CheckFeatureSupport(Feature.FormatSupport,
                                                                   &dataFormatSupport, (uint) Unsafe.SizeOf<FeatureDataFormatSupport>());

                if (result.IsFailure)
                    return 0;

                return (FormatSupport) dataFormatSupport.OutFormatSupport;
            }
        }
    }
}

#endif
