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
using System.Resources;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Stride.Core;
using Stride.Graphics.Direct3D;
using ComponentBase = Stride.Core.ComponentBase;
using Utilities = Stride.Core.Utilities;

namespace Stride.Graphics
{
    /// <summary>
    /// Provides methods to retrieve and manipulate graphics adapters. This is the equivalent to <see cref="Adapter1"/>.
    /// </summary>
    /// <msdn-id>ff471329</msdn-id>
    /// <unmanaged>IDXGIAdapter1</unmanaged>
    /// <unmanaged-short>IDXGIAdapter1</unmanaged-short>
    public partial class GraphicsAdapter
    {
        private readonly ComPtr<IDXGIAdapter> adapter;
        private readonly int adapterOrdinal;
        private readonly ComPtr<AdapterDesc> description;

        private GraphicsProfile minimumUnsupportedProfile = (GraphicsProfile)int.MaxValue;
        private GraphicsProfile maximumSupportedProfile;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsAdapter" /> class.
        /// </summary>
        /// <param name="defaultFactory">The default factory.</param>
        /// <param name="adapterOrdinal">The adapter ordinal.</param>
        internal GraphicsAdapter(ComPtr<IDXGIFactory> defaultFactory, int adapterOrdinal)
        {
            this.adapterOrdinal = adapterOrdinal;
            //GetAdapter1(adapterOrdinal).DisposeBy(this);

            unsafe
            {
                AdapterDesc d = new();
                description.Handle = &d;
                SilkMarshal.ThrowHResult(defaultFactory.Get().EnumAdapters((uint)adapterOrdinal, ref adapter.Handle));
                SilkMarshal.ThrowHResult(adapter.Get().GetDesc(description));
                // for some reason sharpDX returns an adaptater name of fixed size filled with trailing '\0'
                //description.Description = description.Description.TrimEnd('\0');


                //var nativeOutputs = adapter.Outputs;

                //TODO : This needs to be reviewed
                ComPtr<IDXGIOutput> e = new();
                int count = 0;

                
                while ((ulong)adapter.Get().EnumOutputs((uint)count, &e.Handle) == (ulong)ReturnCodes.S_OK)
                    count += 1;

                outputs = new GraphicsOutput[count];
                for (var i = 0; i < count; i++)
                    outputs[i] = new GraphicsOutput(this, i).DisposeBy(this);

                //AdapterUid = adapter.Description1.Luid.ToString();
                //TODO : This seems very weird, need review
                AdapterUid = description.Get().AdapterLuid.High.ToString("X") + description.Get().AdapterLuid.Low.ToString("X");
            }


            
        }

        /// <summary>
        /// Gets the description of this adapter.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get
            {
                unsafe
                {
                    fixed (char* str = description.Get().Description)
                        return SilkMarshal.PtrToString((nint)str);
                }
            }
        }

        /// <summary>
        /// Gets or sets the vendor identifier.
        /// </summary>
        /// <value>
        /// The vendor identifier.
        /// </value>
        public int VendorId
        {
            //TODO: Doesn't seem unsafe but a little bit somehow
            get { unsafe { return (int)description.Get().VendorId; } }
        }

        /// <summary>
        /// Determines if this instance of GraphicsAdapter is the default adapter.
        /// </summary>
        public bool IsDefaultAdapter
        {
            get
            {
                return adapterOrdinal == 0;
            }
        }

        internal ComPtr<IDXGIAdapter> NativeAdapter
        {
            get
            {
                return new ComPtr<IDXGIAdapter>(adapter);
            }
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
            //if (SharpDX.Direct3D11.Device.IsSupportedFeatureLevel(this.NativeAdapter, (SharpDX.Direct3D.FeatureLevel)graphicsProfile))
            //{
            //    maximumSupportedProfile = graphicsProfile;
            //    return true;
            //}
            unsafe
            {
                //TODO change to silk marshal with Guiid

                var featureLevels = new D3DFeatureLevel[]{
                    D3DFeatureLevel.D3DFeatureLevel111,
                    D3DFeatureLevel.D3DFeatureLevel110,
                    D3DFeatureLevel.D3DFeatureLevel101,
                    D3DFeatureLevel.D3DFeatureLevel100,
                    D3DFeatureLevel.D3DFeatureLevel93,
                    D3DFeatureLevel.D3DFeatureLevel92,
                    D3DFeatureLevel.D3DFeatureLevel91
                };

                D3DFeatureLevel level = D3DFeatureLevel.D3DFeatureLevel91;

                fixed (D3DFeatureLevel* levels = featureLevels)
                SilkMarshal.ThrowHResult(D3D11.GetApi().CreateDevice(
                    null,
                    D3DDriverType.D3DDriverTypeHardware,
                    0,
                    0,
                    levels,
                    (uint)featureLevels.Length,
                    D3D11.SdkVersion,
                    null,
                    &level,
                    null
                ));
                maximumSupportedProfile = (GraphicsProfile)level;
                minimumUnsupportedProfile = (GraphicsProfile)D3DFeatureLevel.D3DFeatureLevel91;
                return true;
            }
        }
    }
}
#endif
#endif
