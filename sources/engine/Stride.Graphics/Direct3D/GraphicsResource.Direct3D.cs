// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_DIRECT3D11
using System;

using SharpDX.Direct3D11;

namespace Xenko.Graphics
{
    /// <summary>
    /// GraphicsResource class
    /// </summary>
    public abstract partial class GraphicsResource
    {
        private ShaderResourceView shaderResourceView;
        private UnorderedAccessView unorderedAccessView;
        internal bool DiscardNextMap; // Used to internally force a WriteDiscard (to force a rename) with the GraphicsResourceAllocator

        protected bool IsDebugMode
        {
            get
            {
                return GraphicsDevice != null && GraphicsDevice.IsDebugMode;
            }
        }

        protected override void OnNameChanged()
        {
            base.OnNameChanged();
            if (IsDebugMode)
            {
                if (this.shaderResourceView != null)
                {
                    shaderResourceView.DebugName = Name == null ? null : $"{Name} SRV";
                }

                if (this.unorderedAccessView != null)
                {
                    unorderedAccessView.DebugName = Name == null ? null : $"{Name} UAV";
                }
            }
        }

        /// <summary>
        /// Gets or sets the ShaderResourceView attached to this GraphicsResource.
        /// Note that only Texture, Texture3D, RenderTarget2D, RenderTarget3D, DepthStencil are using this ShaderResourceView
        /// </summary>
        /// <value>The device child.</value>
        protected internal SharpDX.Direct3D11.ShaderResourceView NativeShaderResourceView
        {
            get
            {
                return shaderResourceView;
            }
            set
            {
                shaderResourceView = value;

                if (IsDebugMode && shaderResourceView != null)
                {
                    shaderResourceView.DebugName = Name == null ? null : $"{Name} SRV";
                }
            }
        }

        /// <summary>
        /// Gets or sets the UnorderedAccessView attached to this GraphicsResource.
        /// </summary>
        /// <value>The device child.</value>
        protected internal UnorderedAccessView NativeUnorderedAccessView
        {
            get
            {
                return unorderedAccessView;
            }
            set
            {
                unorderedAccessView = value;

                if (IsDebugMode && unorderedAccessView != null)
                {
                    unorderedAccessView.DebugName = Name == null ? null : $"{Name} UAV";
                }
            }
        }

        protected internal override void OnDestroyed()
        {
            ReleaseComObject(ref shaderResourceView);
            ReleaseComObject(ref unorderedAccessView);

            base.OnDestroyed();
        }
    }
}
 
#endif
