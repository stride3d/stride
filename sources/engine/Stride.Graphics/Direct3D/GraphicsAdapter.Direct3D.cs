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

using Stride.Core;
using Stride.Core.UnsafeExtensions;

namespace Stride.Graphics
{
    public sealed unsafe partial class GraphicsAdapter
    {
        private IDXGIAdapter1* dxgiAdapter;
        private readonly uint dxgiAdapterVersion;

        private readonly uint adapterOrdinal;

#if STRIDE_GRAPHICS_API_DIRECT3D11
        private GraphicsProfile minimumUnsupportedProfile = (GraphicsProfile) int.MaxValue;
        private GraphicsProfile maximumSupportedProfile;
#endif

        /// <summary>
        ///   Gets the native DXGI adapter.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        internal ComPtr<IDXGIAdapter1> NativeAdapter => ComPtrHelpers.ToComPtr(dxgiAdapter);

        /// <summary>
        ///   Gets the version number of the native DXGI adapter supported.
        /// </summary>
        /// <value>
        ///   This indicates the latest DXGI adapter interface version supported by this adapter.
        ///   For example, if the value is 4, then this adapter supports up to <see cref="IDXGIAdapter4"/>.
        /// </value>
        internal uint NativeAdapterVersion => dxgiAdapterVersion;

        /// <summary>
        ///   Gets the description of this adapter.
        /// </summary>
        public string Description { get; }

        /// <summary>
        ///   Gets the vendor identifier of this adapter.
        /// </summary>
        public int VendorId  { get; }


        /// <summary>
        ///   Gets the amount of memory, in bytes, on the graphics card (GPU) that is
        ///   exclusively reserved for graphics operations.
        ///   This is physical video memory dedicated to the adapter.
        /// </summary>
        /// <remarks>
        ///   Typically used for storing Textures, Frame Buffers, and other GPU-specific resources.
        ///   High-performance discrete GPUs usually have a large amount of dedicated video memory.
        /// </remarks>
        public ulong DedicatedVideoMemory { get; }

        /// <summary>
        ///   Gets the amount of system RAM, in bytes, that is reserved exclusively for use
        ///   by the adapter.
        ///   This memory is not available to other applications.
        /// </summary>
        /// <remarks>
        ///   Common in systems with integrated or hybrid graphics solutions.
        ///   It provides the GPU with guaranteed access to a portion of system memory for graphics tasks.
        /// </remarks>
        public ulong DedicatedSystemMemory { get; }

        /// <summary>
        ///   Gets the amount of system RAM, in bytes, that can be shared between the adapter
        ///   and the CPU.
        ///   This memory is dynamically allocated and can be used by both graphics and general system tasks.
        /// </summary>
        /// <remarks>
        ///   Used when the GPU needs additional memory beyond its dedicated resources.
        ///   Integrated GPUs rely heavily on shared system memory, while discrete GPUs use it as a fallback.
        /// </remarks>
        public ulong SharedSystemMemory { get; }


        /// <summary>
        ///   Determines if this <see cref="GraphicsAdapter"/> is the default adapter.
        /// </summary>
        public bool IsDefaultAdapter => adapterOrdinal == 0;


        /// <summary>
        ///   Initializes a new instance of the <see cref="GraphicsAdapter"/> class.
        /// </summary>
        /// <param name="adapter">
        ///   A COM pointer to the native <see cref="IDXGIAdapter"/> interface. The ownership is transferred to this instance, so the reference count is not incremented.
        /// </param>
        /// <param name="adapterOrdinal">The adapter ordinal.</param>
        internal GraphicsAdapter(ComPtr<IDXGIAdapter1> adapter, uint adapterOrdinal)
        {
            this.adapterOrdinal = adapterOrdinal;
            dxgiAdapter = adapter.Handle;
            dxgiAdapterVersion = GetLatestDxgiAdapterVersion(dxgiAdapter);

            Unsafe.SkipInit(out AdapterDesc1 dxgiAdapterDesc);
            HResult result = NativeAdapter.GetDesc1(ref dxgiAdapterDesc);

            if (result.IsFailure)
                result.Throw();

            Name = Description = SilkMarshal.PtrToString((nint) dxgiAdapterDesc.Description, NativeStringEncoding.LPWStr)!;
            AdapterUid = dxgiAdapterDesc.AdapterLuid.BitCast<Luid, long>();

            VendorId = (int) dxgiAdapterDesc.VendorId;
            DedicatedVideoMemory = dxgiAdapterDesc.DedicatedVideoMemory;
            SharedSystemMemory = dxgiAdapterDesc.SharedSystemMemory;
            DedicatedSystemMemory = dxgiAdapterDesc.DedicatedSystemMemory;

            uint outputIndex = 0;
            var outputsList = new List<GraphicsOutput>();
            ComPtr<IDXGIOutput> output = default;

            do
            {
                result = adapter.EnumOutputs(outputIndex, ref output);

                bool foundValidOutput = result.IsSuccess && result.Code != DxgiConstants.ErrorNotFound;
                if (!foundValidOutput)
                    break;

                var gfxOutput = new GraphicsOutput(adapter: this, output);
                gfxOutput.DisposeBy(this);
                outputsList.Add(gfxOutput);

                outputIndex++;
            }
            while (true);

            graphicsOutputs = outputsList.ToArray();

            //
            // Queries the latest DXGI adapter version supported.
            //
            static uint GetLatestDxgiAdapterVersion(IDXGIAdapter1* adapter)
            {
                uint adapterVersion;

                if (((HResult) adapter->QueryInterface<IDXGIAdapter4>(out _)).IsSuccess)
                {
                    adapterVersion = 4;
                    adapter->Release();
                }
                else if (((HResult) adapter->QueryInterface<IDXGIAdapter3>(out _)).IsSuccess)
                {
                    adapterVersion = 3;
                    adapter->Release();
                }
                else if (((HResult) adapter->QueryInterface<IDXGIAdapter2>(out _)).IsSuccess)
                {
                    adapterVersion = 2;
                    adapter->Release();
                }
                else
                {
                    adapterVersion = 1;
                }

                return adapterVersion;
            }
        }

        /// <inheritdoc/>
        protected override void Destroy()
        {
            base.Destroy();

            ComPtrHelpers.SafeRelease(ref dxgiAdapter);
        }

        /// <summary>
        ///   Checks if the graphics adapter supports the requested profile.
        /// </summary>
        /// <param name="graphicsProfile">The graphics profile to check.</param>
        /// <returns>
        ///   <see langword="true"/> if the graphics profile is supported; <see langword="false"/> otherwise.
        /// </returns>
        public bool IsProfileSupported(GraphicsProfile graphicsProfile)
        {
#if STRIDE_GRAPHICS_API_DIRECT3D12
            return true;
#else
            // Did we check for this or a higher profile, and it was supported?
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
                                                ref device, ref matchedFeatureLevel, ref deviceContext);

            ComPtrHelpers.SafeRelease(ref deviceContext);
            ComPtrHelpers.SafeRelease(ref device);

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
