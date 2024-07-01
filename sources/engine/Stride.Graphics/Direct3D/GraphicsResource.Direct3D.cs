// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using Silk.NET.Direct3D11;

using static Stride.Graphics.DebugHelpers;

namespace Stride.Graphics
{
    /// <summary>
    /// GraphicsResource class
    /// </summary>
    public abstract unsafe partial class GraphicsResource
    {
        private ID3D11ShaderResourceView* shaderResourceView;
        private ID3D11UnorderedAccessView* unorderedAccessView;

        // Used to internally force a WriteDiscard (to force a rename) with the GraphicsResourceAllocator
        internal bool DiscardNextMap;

        protected bool IsDebugMode => GraphicsDevice?.IsDebugMode == true;

        protected override void OnNameChanged()
        {
            base.OnNameChanged();

            if (IsDebugMode)
            {
                if (shaderResourceView != null)
                {
                    SetDebugName((ID3D11DeviceChild*) shaderResourceView, Name is null ? null : $"{Name} SRV");
                }
                if (unorderedAccessView != null)
                {
                    SetDebugName((ID3D11DeviceChild*) unorderedAccessView, Name is null ? null : $"{Name} UAV");
                }
            }
        }

        /// <summary>
        /// Gets or sets the ShaderResourceView attached to this GraphicsResource.
        /// Note that only Texture, Texture3D, RenderTarget2D, RenderTarget3D, DepthStencil are using this ShaderResourceView
        /// </summary>
        /// <value>The device child.</value>
        protected internal ID3D11ShaderResourceView* NativeShaderResourceView
        {
            get => shaderResourceView;

            set
            {
                shaderResourceView = value;

                if (IsDebugMode && shaderResourceView != null)
                {
                    SetDebugName((ID3D11DeviceChild*)shaderResourceView, Name is null ? null : $"{Name} SRV");
                }
            }
        }

        /// <summary>
        /// Gets or sets the UnorderedAccessView attached to this GraphicsResource.
        /// </summary>
        /// <value>The device child.</value>
        protected internal ID3D11UnorderedAccessView* NativeUnorderedAccessView
        {
            get => unorderedAccessView;

            set
            {
                unorderedAccessView = value;

                if (IsDebugMode && unorderedAccessView != null)
                {
                    SetDebugName((ID3D11DeviceChild*)unorderedAccessView, Name is null ? null : $"{Name} UAV");
                }
            }
        }

        protected internal override void OnDestroyed()
        {
            if (shaderResourceView != null)
                shaderResourceView->Release();

            if (unorderedAccessView != null)
                unorderedAccessView->Release();

            base.OnDestroyed();
        }
    }
}

#endif
