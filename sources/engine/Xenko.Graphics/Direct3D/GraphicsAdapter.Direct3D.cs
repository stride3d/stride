// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_DIRECT3D
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
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Xenko.Core;
using ComponentBase = Xenko.Core.ComponentBase;
using Utilities = Xenko.Core.Utilities;

namespace Xenko.Graphics
{
    /// <summary>
    /// Provides methods to retrieve and manipulate graphics adapters. This is the equivalent to <see cref="Adapter1"/>.
    /// </summary>
    /// <msdn-id>ff471329</msdn-id>
    /// <unmanaged>IDXGIAdapter1</unmanaged>
    /// <unmanaged-short>IDXGIAdapter1</unmanaged-short>
    public partial class GraphicsAdapter
    {
        private readonly Adapter1 adapter;
        private readonly int adapterOrdinal;
        private readonly AdapterDescription1 description;

        private GraphicsProfile minimumUnsupportedProfile = (GraphicsProfile)int.MaxValue;
        private GraphicsProfile maximumSupportedProfile;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsAdapter" /> class.
        /// </summary>
        /// <param name="defaultFactory">The default factory.</param>
        /// <param name="adapterOrdinal">The adapter ordinal.</param>
        internal GraphicsAdapter(Factory1 defaultFactory, int adapterOrdinal)
        {
            this.adapterOrdinal = adapterOrdinal;
            adapter = defaultFactory.GetAdapter1(adapterOrdinal).DisposeBy(this);
            description = adapter.Description1;
            description.Description = description.Description.TrimEnd('\0'); // for some reason sharpDX returns an adaptater name of fixed size filled with trailing '\0'
            //var nativeOutputs = adapter.Outputs;

            var count = adapter.GetOutputCount();
            outputs = new GraphicsOutput[count];
            for (var i = 0; i < outputs.Length; i++)
                outputs[i] = new GraphicsOutput(this, i).DisposeBy(this);

            AdapterUid = adapter.Description1.Luid.ToString();
        }

        /// <summary>
        /// Gets the description of this adapter.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get
            {
                return description.Description;
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
            get { return description.VendorId; }
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

        internal Adapter1 NativeAdapter
        {
            get
            {
                return adapter;
            }
        }

        /// <summary>
        /// Tests to see if the adapter supports the requested profile.
        /// </summary>
        /// <param name="graphicsProfile">The graphics profile.</param>
        /// <returns>true if the profile is supported</returns>
        public bool IsProfileSupported(GraphicsProfile graphicsProfile)
        {
#if XENKO_GRAPHICS_API_DIRECT3D12
            return true;
#else
            // Did we check fo this or a higher profile, and it was supported?
            if (maximumSupportedProfile >= graphicsProfile)
                return true;

            // Did we check for this or a lower profile and it was unsupported?
            if (minimumUnsupportedProfile <= graphicsProfile)
                return false;

            // Check and min/max cached values
            if (SharpDX.Direct3D11.Device.IsSupportedFeatureLevel(this.NativeAdapter, (SharpDX.Direct3D.FeatureLevel)graphicsProfile))
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
