// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Stride.Core;

namespace Stride.Graphics
{
    public abstract unsafe partial class GraphicsResource
    {
        private ComPtr<ID3D11ShaderResourceView> shaderResourceView;
        private ComPtr<ID3D11UnorderedAccessView> unorderedAccessView;

        /// <summary>
        ///   Used to internally force a <c>WriteDiscard</c> (to force a rename) with the <see cref="GraphicsResourceAllocator"/>.
        /// </summary>
        internal bool DiscardNextMap;

        /// <summary>
        ///   Gets a value indicating whether this graphics resource is in "Debug mode".
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if this graphics resource is initialized in "Debug mode"; otherwise, <see langword="false"/>.
        /// </value>
        protected bool IsDebugMode => GraphicsDevice?.IsDebugMode == true;

        /// <inheritdoc/>
        protected override void OnNameChanged()
        {
            base.OnNameChanged();

            if (IsDebugMode)
            {
                if (shaderResourceView.Handle != null)
                {
                    using var srv = shaderResourceView.QueryInterface<ID3D11DeviceChild>();
                    srv.SetDebugName(Name is null ? null : $"{Name} SRV", owningObject: this);
                }
                if (unorderedAccessView.Handle != null)
                {
                    using var uav = unorderedAccessView.QueryInterface<ID3D11DeviceChild>();
                    uav.SetDebugName(Name is null ? null : $"{Name} UAV", owningObject: this);
                }
            }
        }

        /// <summary>
        ///   Gets or sets the <see cref="ID3D11ShaderResourceView"/> attached to this <see cref="GraphicsResource"/>.
        /// </summary>
        /// <value>The Shader Resource View associated with this graphics resource.</value>
        /// <remarks>
        ///   Only <see cref="Texture"/>s are using this Shader Resource View.
        /// </remarks>
        protected internal ComPtr<ID3D11ShaderResourceView> NativeShaderResourceView
        {
            get => shaderResourceView;
            set
            {
                var previousShaderResourceView = shaderResourceView;

                shaderResourceView = value;

                if (shaderResourceView.Handle != previousShaderResourceView.Handle)
                {
                    previousShaderResourceView.RemoveDisposeBy(this);
                    previousShaderResourceView.Release();

                    shaderResourceView.DisposeBy(this);
                }

                if (IsDebugMode && shaderResourceView.Handle != null)
                {
                    using var srv = shaderResourceView.QueryInterface<ID3D11DeviceChild>();
                    srv.SetDebugName(Name is null ? null : $"{Name} SRV", owningObject: this);
                }
            }
        }

        /// <summary>
        ///   Gets or sets the <see cref="ID3D11UnorderedAccessView"/> attached to this <see cref="GraphicsResource"/>.
        /// </summary>
        /// <value>The Unordered Access View associated with this graphics resource.</value>
        protected internal ID3D11UnorderedAccessView* NativeUnorderedAccessView
        {
            get => unorderedAccessView;
            set
            {
                var previousUnorderedAccessView = unorderedAccessView;

                unorderedAccessView = value;

                if (unorderedAccessView.Handle != previousUnorderedAccessView.Handle)
                {
                    previousUnorderedAccessView.RemoveDisposeBy(this);
                    previousUnorderedAccessView.Release();

                    unorderedAccessView.DisposeBy(this);
                }

                if (IsDebugMode && unorderedAccessView.Handle != null)
                {
                    using var uav = unorderedAccessView.QueryInterface<ID3D11DeviceChild>();
                    uav.SetDebugName(Name is null ? null : $"{Name} UAV", owningObject: this);
                }
            }
        }
    }
}

#endif
