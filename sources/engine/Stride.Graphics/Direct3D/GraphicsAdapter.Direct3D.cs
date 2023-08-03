// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D

// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
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
using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

namespace Stride.Graphics
{
    /// <summary>
    /// Provides methods to retrieve and manipulate graphics adapters. This is the equivalent to <see cref="Adapter1"/>.
    /// </summary>
    /// <msdn-id>ff471329</msdn-id>
    /// <unmanaged>IDXGIAdapter1</unmanaged>
    /// <unmanaged-short>IDXGIAdapter1</unmanaged-short>
    public unsafe partial class GraphicsAdapter
    {
        /// <summary>
        ///   Gets the native DXGI adapter.
        /// </summary>
        internal IDXGIAdapter1* NativeAdapter { get; }

        private readonly int adapterOrdinal;
        private readonly AdapterDesc1 adapterDesc;

        private readonly string adapterDescriptionString;

        private GraphicsProfile minimumUnsupportedProfile = (GraphicsProfile) int.MaxValue;
        private GraphicsProfile maximumSupportedProfile;

        /// <summary>
        ///   Gets the description of this adapter.
        /// </summary>
        public string Description => adapterDescriptionString;

        /// <summary>
        ///   Gets the vendor identifier of this adapter.
        /// </summary>
        public int VendorId => (int)adapterDesc.VendorId;

        /// <summary>
        ///   Determines if this instance of GraphicsAdapter is the default adapter.
        /// </summary>
        public bool IsDefaultAdapter => adapterOrdinal == 0;


        /// <summary>
        ///   Initializes a new instance of the <see cref="GraphicsAdapter"/> class.
        /// </summary>
        /// <param name="adapter">The DXGI adapter.</param>
        /// <param name="adapterOrdinal">The adapter ordinal.</param>
        internal GraphicsAdapter(IDXGIAdapter1* adapter, int adapterOrdinal)
        {
            this.adapterOrdinal = adapterOrdinal;

            NativeAdapter = adapter;

            HResult result = NativeAdapter->GetDesc1(ref adapterDesc);

            if (result.IsFailure)
                result.Throw();

            fixed (char* descString = adapterDesc.Description)
                adapterDescriptionString = SilkMarshal.PtrToString((nint) descString, NativeStringEncoding.LPWStr);

            var nativeOutputs = new List<GraphicsOutput>();

            const int DXGI_ERROR_NOT_FOUND = unchecked((int) 0x887A0002);

            uint outputIndex = 0;
            var outputsList = new List<GraphicsOutput>();

            do
            {
                IDXGIOutput* output;
                result = adapter->EnumOutputs(outputIndex, &output);

                if (result == DXGI_ERROR_NOT_FOUND)
                    break;

                var gfxOutput = new GraphicsOutput(adapter: this, output, (int) outputIndex);
                outputsList.Add(gfxOutput);

                outputIndex++;
            }
            while (result.Code != DXGI_ERROR_NOT_FOUND);

            Outputs = outputsList.ToArray();

            AdapterUid = Unsafe.As<Luid, long>(ref adapterDesc.AdapterLuid);
        }

        /// <summary>
        /// Tests to see if the adapter supports the requested profile.
        /// </summary>
        /// <param name="graphicsProfile">The graphics profile.</param>
        /// <returns>true if the profile is supported</returns>
        public bool IsProfileSupported(GraphicsProfile graphicsProfile)
        {
#if STRIDE_GRAPHICS_API_DIRECT3D12
            return true;
#else
            // Did we check fo this or a higher profile, and it was supported?
            if (maximumSupportedProfile >= graphicsProfile)
                return true;

            // Did we check for this or a lower profile and it was unsupported?
            if (minimumUnsupportedProfile <= graphicsProfile)
                return false;

            // Check and min/max cached values

            var d3d11 = D3D11.GetApi(window: null);

            ID3D11Device* device = null;
            ID3D11DeviceContext* deviceContext = null;
            D3DFeatureLevel matchedFeatureLevel = 0;
            var featureLevel = (D3DFeatureLevel) graphicsProfile;
            var featureLevels = stackalloc D3DFeatureLevel[] { featureLevel };

            HResult result = d3d11.CreateDevice(pAdapter: null, D3DDriverType.Hardware, Software: IntPtr.Zero,
                                                Flags: 0, featureLevels, 1, D3D11.SdkVersion,
                                                &device, &matchedFeatureLevel, &deviceContext);

            if (deviceContext != null)
                deviceContext->Release();

            if (device != null)
                device->Release();

            if (result.IsSuccess && matchedFeatureLevel == featureLevel)
            {
                maximumSupportedProfile = graphicsProfile;
                return true;
            }
            else
            {
                minimumUnsupportedProfile = graphicsProfile;
                return false;
            }
#endif
        }
    }
}

#endif
