// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;

using static Stride.Graphics.ComPtrHelpers;

namespace Stride.Graphics;

public abstract unsafe partial class GraphicsResource
{
    private ID3D11ShaderResourceView* shaderResourceView;
    private ID3D11UnorderedAccessView* unorderedAccessView;

    /// <summary>
    ///   Used to internally force a <c>WriteDiscard</c> (to force a resource rename) with the <see cref="GraphicsResourceAllocator"/>.
    /// </summary>
    internal bool DiscardNextMap;


    /// <inheritdoc/>
    protected override void OnNameChanged()
    {
        base.OnNameChanged();

        if (IsDebugMode)
        {
            if (shaderResourceView is not null)
            {
                NativeShaderResourceView.SetDebugName(Name is null ? null : $"{Name} SRV");
            }
            if (unorderedAccessView is not null)
            {
                NativeUnorderedAccessView.SetDebugName(Name is null ? null : $"{Name} UAV");
            }
        }
    }

    /// <summary>
    ///   Gets or sets the <see cref="ID3D11ShaderResourceView"/> attached to the Graphics Resource.
    /// </summary>
    /// <value>The Shader Resource View associated with the Graphics Resource.</value>
    /// <remarks>
    ///   Only <see cref="Texture"/>s are using this Shader Resource View.
    ///   <para>
    ///     If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
    ///     reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
    ///   </para>
    /// </remarks>
    protected internal ComPtr<ID3D11ShaderResourceView> NativeShaderResourceView
    {
        get => ToComPtr(shaderResourceView);
        set
        {
            if (shaderResourceView == value.Handle)
                return;

            var previousShaderResourceView = shaderResourceView;

            // We assume the COM pointer we receive has been called AddRef(), so we
            // just take ownership without calling it again
            shaderResourceView = value.Handle;

            if (shaderResourceView != previousShaderResourceView)
            {
                // Following the logic of the comment above, when we are no longer the owners
                // of the COM pointer, we Release() it.
                // It us up to users to call AddRef() on it if they plan to use it further
                if (previousShaderResourceView is not null)
                    previousShaderResourceView->Release();
            }

            if (IsDebugMode && shaderResourceView is not null)
            {
                NativeShaderResourceView.SetDebugName(Name is null ? null : $"{Name} SRV");
            }
        }
    }

    /// <summary>
    ///   Gets or sets the <see cref="ID3D11UnorderedAccessView"/> attached to the Graphics Resource.
    /// </summary>
    /// <value>The Unordered Access View associated with the Graphics Resource.</value>
    /// <remarks>
    ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
    ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
    /// </remarks>
    protected internal ComPtr<ID3D11UnorderedAccessView> NativeUnorderedAccessView
    {
        get => ToComPtr(unorderedAccessView);
        set
        {
            if (unorderedAccessView == value.Handle)
                return;

            var previousUnorderedAccessView = unorderedAccessView;

            // We assume the COM pointer we receive has been called AddRef(), so we
            // just take ownership without calling it again
            unorderedAccessView = value.Handle;

            if (unorderedAccessView != previousUnorderedAccessView)
            {
                // Following the logic of the comment above, when we are no longer the owners
                // of the COM pointer, we Release() it.
                // It us up to users to call AddRef() on it if they plan to use it further
                if (previousUnorderedAccessView is not null)
                    previousUnorderedAccessView->Release();
            }

            if (IsDebugMode && unorderedAccessView is not null)
            {
                NativeUnorderedAccessView.SetDebugName(Name is null ? null : $"{Name} UAV");
            }
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    ///   This method releases the underlying native resources (<see cref="ID3D11ShaderResourceView"/> and <see cref="ID3D11UnorderedAccessView"/>),
    ///   and then calls <see cref="GraphicsResourceBase.OnDestroyed"/>.
    /// </remarks>
    protected internal override void OnDestroyed(bool immediately = false)
    {
        SafeRelease(ref shaderResourceView);
        SafeRelease(ref unorderedAccessView);

        base.OnDestroyed(immediately);
    }

    /// <summary>
    ///   Swaps the Graphics Resource's internal data with another Graphics Resource.
    /// </summary>
    /// <param name="other">The other Graphics Resource.</param>
    internal override void SwapInternal(GraphicsResourceBase other)
    {
        var otherResource = (GraphicsResource)other;

        base.SwapInternal(other);

        var uav = unorderedAccessView;
        unorderedAccessView = otherResource.unorderedAccessView;
        otherResource.unorderedAccessView = uav;

        var srv = shaderResourceView;
        shaderResourceView = otherResource.shaderResourceView;
        otherResource.shaderResourceView = srv;
    }
}

#endif
