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

using static Stride.Graphics.GraphicsProfile;

namespace Stride.Graphics
{
    public unsafe partial struct GraphicsDeviceFeatures
    {
        private static readonly Format[] ObsoleteFormatsToExclude =
        [
            Format.FormatR1Unorm,
            Format.FormatB5G6R5Unorm,
            Format.FormatB5G5R5A1Unorm
        ];

        internal GraphicsDeviceFeatures(GraphicsDevice device)
        {
            var nativeDevice = device.NativeDevice;

            HasSRgb = true;

            mapFeaturesPerFormat = new FeaturesPerFormat[256];

            // Set back the real GraphicsProfile that is used
            RequestedProfile = device.RequestedProfile;
            CurrentProfile = GraphicsProfileHelper.FromFeatureLevel(nativeDevice.GetFeatureLevel());

            HasResourceRenaming = true;

            HasComputeShaders = CheckComputeShadersSupport();
            HasDoublePrecision = CheckDoubleOpsInShadersSupport();
            CheckThreadingSupport(out HasMultiThreadingConcurrentResources, out HasDriverCommandLists);

            HasDepthAsSRV = CurrentProfile >= Level_10_0;
            HasDepthAsReadOnlyRT = CurrentProfile >= Level_11_0;
            HasMultiSampleDepthAsSRV = CurrentProfile >= Level_11_0;

            // Check features for each DXGI.Format
            foreach (var format in Enum.GetValues<Format>())
            {
                if (format == Format.FormatForceUint)
                    continue;

                if (ObsoleteFormatsToExclude.Contains(format))
                    continue;

                var maximumMultisampleCount = GetMaximumMultisampleCount(format);

                var computeShaderFormatSupport = HasComputeShaders
                    ? CheckComputeShaderFormatSupport(format)
                    : ComputeShaderFormatSupport.None;

                var formatSupport = CheckFormatSupport(format);

                var pixelFormat = (PixelFormat) format;
                mapFeaturesPerFormat[(int) format] = new FeaturesPerFormat(pixelFormat, maximumMultisampleCount, computeShaderFormatSupport, formatSupport);
            }

            // Sets the max resource sizes, mip counts, etc., for the supported profile
            switch (CurrentProfile)
            {
                case Level_11_2:
                case Level_11_1:
                case Level_11_0:
                    MaximumMipLevels = 15;
                    ResourceSizeInMegabytes = 128;
                    MaximumTexture1DArraySize = 2048;
                    MaximumTexture2DArraySize = 2048;
                    MaximumTexture1DSize = 16384;
                    MaximumTexture2DSize = 16384;
                    MaximumTexture3DSize = 2048;
                    MaximumTextureCubeSize = 16384;
                    break;

                case Level_10_1:
                case Level_10_0:
                    MaximumMipLevels = 14;
                    ResourceSizeInMegabytes = 128;
                    MaximumTexture1DArraySize = 512;
                    MaximumTexture2DArraySize = 512;
                    MaximumTexture1DSize = 8192;
                    MaximumTexture2DSize = 8192;
                    MaximumTexture3DSize = 2048;
                    MaximumTextureCubeSize = 8192;
                    break;

                case Level_9_1:
                case Level_9_2:
                case Level_9_3:
                    MaximumMipLevels = 14;
                    ResourceSizeInMegabytes = 128;
                    MaximumTexture1DArraySize = 512;
                    MaximumTexture2DArraySize = 512;
                    MaximumTexture1DSize = CurrentProfile < Level_9_3 ? 2048 : 4096;
                    MaximumTexture2DSize = CurrentProfile < Level_9_3 ? 2048 : 4096;
                    MaximumTexture3DSize = 256;
                    MaximumTextureCubeSize = CurrentProfile < Level_9_3 ? 512 : 4096;
                    break;
            }

            /// <summary>
            ///   Checks if the Direct3D device does support Compute Shaders.
            /// </summary>
            bool CheckComputeShadersSupport()
            {
                Unsafe.SkipInit(out FeatureDataD3D10XHardwareOptions hwOptions);

                HResult result = nativeDevice.CheckFeatureSupport(Feature.D3D10XHardwareOptions, ref hwOptions, (uint) sizeof(FeatureDataD3D10XHardwareOptions));

                if (result.IsFailure)
                    return false;

                return hwOptions.ComputeShadersPlusRawAndStructuredBuffersViaShader4X;
            }

            /// <summary>
            ///   Checks if the Direct3D device does support double precision operations in shaders.
            /// </summary>
            bool CheckDoubleOpsInShadersSupport()
            {
                Unsafe.SkipInit(out FeatureDataDoubles doubles);

                HResult result = nativeDevice.CheckFeatureSupport(Feature.Doubles, ref doubles, (uint) sizeof(FeatureDataDoubles));

                if (result.IsFailure)
                    return false;

                return doubles.DoublePrecisionFloatShaderOps;
            }

            /// <summary>
            ///   Checks if the Direct3D device does support threading.
            /// </summary>
            void CheckThreadingSupport(out bool supportsConcurrentResources, out bool supportsCommandLists)
            {
                Unsafe.SkipInit(out FeatureDataThreading featureDataThreading);

                HResult result = nativeDevice.CheckFeatureSupport(Feature.Threading, ref featureDataThreading, (uint) sizeof(FeatureDataThreading));

                if (result.IsFailure)
                {
                    supportsConcurrentResources = false;
                    supportsCommandLists = false;
                }
                else
                {
                    supportsConcurrentResources = featureDataThreading.DriverConcurrentCreates;
                    supportsCommandLists = featureDataThreading.DriverCommandLists;
                }
            }

            /// <summary>
            ///   Gets the maximum sample count when enabling multi-sampling for a particular <see cref="Format"/>.
            /// </summary>
            MultisampleCount GetMaximumMultisampleCount(Format pixelFormat)
            {
                uint maxCount = 1;

                for (uint sampleCount = 1; sampleCount <= 8; sampleCount *= 2)
                {
                    uint qualityLevels = 0;

                    HResult result = nativeDevice.CheckMultisampleQualityLevels(pixelFormat, sampleCount, ref qualityLevels);

                    if (result.IsSuccess && qualityLevels != 0)
                        maxCount = sampleCount;
                }
                return (MultisampleCount) maxCount;
            }

            /// <summary>
            ///   Check if the Direct3D device does support Compute Shaders for the specified format.
            /// </summary>
            /// <returns>Flags indicating usage contexts in which the specified format is supported.</returns>
            ComputeShaderFormatSupport CheckComputeShaderFormatSupport(Format format)
            {
                var dataFormatSupport2 = new FeatureDataFormatSupport2(format);

                HResult result = nativeDevice.CheckFeatureSupport(Feature.FormatSupport2, ref dataFormatSupport2, (uint) sizeof(FeatureDataFormatSupport2));

                if (result.IsFailure)
                    return 0;

                return (ComputeShaderFormatSupport) dataFormatSupport2.OutFormatSupport2;
            }

            /// <summary>
            ///   Check the support the Direct3D device has for the specified format.
            /// </summary>
            /// <returns>Flags indicating usage contexts in which the specified format is supported.</returns>
            FormatSupport CheckFormatSupport(Format format)
            {
                var dataFormatSupport = new FeatureDataFormatSupport(format);

                HResult result = nativeDevice.CheckFeatureSupport(Feature.FormatSupport, ref dataFormatSupport, (uint) sizeof(FeatureDataFormatSupport));

                if (result.IsFailure)
                    return 0;

                return (FormatSupport) dataFormatSupport.OutFormatSupport;
            }
        }
    }
}

#endif
