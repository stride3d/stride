// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;

namespace Stride.Graphics;

public abstract unsafe partial class GraphicsResource
{
    private ID3D11ShaderResourceView* shaderResourceView;
    private ID3D11UnorderedAccessView* unorderedAccessView;

    internal bool DiscardNextMap;

    protected bool IsDebugMode => GraphicsDevice?.IsDebugMode == true;

    protected override void OnNameChanged()
    {
        base.OnNameChanged();

        if (IsDebugMode)
        {
            if (shaderResourceView != null)
            {
                using var srv = shaderResourceView->QueryInterface<ID3D11DeviceChild>();
                srv.SetDebugName(Name is null ? null : $"{Name} SRV");
            }
            if (unorderedAccessView != null)
            {
                using var uav = unorderedAccessView->QueryInterface<ID3D11DeviceChild>();
                uav.SetDebugName(Name is null ? null : $"{Name} UAV");
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
        get => ComPtrHelpers.ToComPtr(shaderResourceView);
        set
        {
            var previousShaderResourceView = shaderResourceView;

            shaderResourceView = value.Handle;

            if (shaderResourceView != previousShaderResourceView)
            {
                previousShaderResourceView->Release();
            }

            if (IsDebugMode && shaderResourceView != null)
            {
                var srv = ComPtrHelpers.ToComPtr<ID3D11DeviceChild, ID3D11ShaderResourceView>(shaderResourceView);
                srv.SetDebugName(Name is null ? null : $"{Name} SRV");
            }
        }
    }

        /// <summary>
        ///   Gets or sets the <see cref="ID3D11UnorderedAccessView"/> attached to this <see cref="GraphicsResource"/>.
        /// </summary>
        /// <value>The Unordered Access View associated with this graphics resource.</value>
    protected internal ComPtr<ID3D11UnorderedAccessView> NativeUnorderedAccessView
    {
        get => ComPtrHelpers.ToComPtr(unorderedAccessView);
        set
        {
            var previousUnorderedAccessView = unorderedAccessView;

            unorderedAccessView = value.Handle;

            if (unorderedAccessView != previousUnorderedAccessView)
            {
                previousUnorderedAccessView->Release();
            }

            if (IsDebugMode && unorderedAccessView != null)
            {
                var uav = ComPtrHelpers.ToComPtr<ID3D11DeviceChild, ID3D11UnorderedAccessView>(unorderedAccessView);
                uav.SetDebugName(Name is null ? null : $"{Name} UAV");
            }
        }
    }

    protected internal override void OnDestroyed()
    {
        ComPtrHelpers.SafeRelease(ref shaderResourceView);
        ComPtrHelpers.SafeRelease(ref unorderedAccessView);

        base.OnDestroyed();
    }
}

#endif
