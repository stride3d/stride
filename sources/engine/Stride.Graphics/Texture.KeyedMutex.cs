// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

#if STRIDE_GRAPHICS_API_DIRECT3D11
using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Silk.NET.Direct3D11;
#endif

namespace Stride.Graphics;

public partial class Texture
{
    /// <summary>
    ///   Gets a value indicating whether this Texture can be synchronized with another device through a keyed mutex
    ///   (<see cref="AcquireSync"/> and <see cref="ReleaseSync"/>).
    /// </summary>
    /// <remarks>
    ///   Only a Direct3D 11 Texture created with the <c>SharedKeyedMutex</c> option has a keyed mutex.
    /// </remarks>
    public bool HasKeyedMutex
    {
        get
        {
#if STRIDE_GRAPHICS_API_DIRECT3D11
            return textureDescription.Options.HasFlag(TextureOptions.SharedKeyedMutex);
#else
            return false;
#endif
        }
    }

    /// <summary>
    ///   Acquires the keyed mutex of a shared Texture, so that this device can use the Texture until
    ///   <see cref="ReleaseSync"/>.
    /// </summary>
    /// <param name="key">The key the other device released the mutex with (<c>0</c> for a Texture nobody used yet).</param>
    /// <param name="timeoutMilliseconds">
    ///   How long to wait for the other device, in milliseconds (<c>0</c> to try without waiting).
    /// </param>
    /// <returns><see langword="true"/> if the mutex was acquired; <see langword="false"/> on a timeout.</returns>
    /// <exception cref="NotSupportedException">The Texture has no keyed mutex (see <see cref="HasKeyedMutex"/>).</exception>
    /// <remarks>
    ///   A device that shares the Texture with another one (a UI compositor, another Direct3D device) acquires the mutex
    ///   before its commands use the Texture and releases it after, with the key the other device waits for.
    /// </remarks>
    public bool AcquireSync(ulong key, uint timeoutMilliseconds)
    {
#if STRIDE_GRAPHICS_API_DIRECT3D11
        using var keyedMutex = GetKeyedMutex();
        HResult result = keyedMutex.AcquireSync(key, timeoutMilliseconds);
        // WAIT_TIMEOUT (0x102) and WAIT_ABANDONED (0x80) are success codes
        if (result.Value == 0x102)
            return false;
        if (result.IsFailure)
            result.Throw();
        return true;
#else
        throw new NotSupportedException("Only a Direct3D 11 Texture has a keyed mutex.");
#endif
    }

    /// <summary>
    ///   Releases the keyed mutex of a shared Texture, so that the device that acquires it with <paramref name="key"/>
    ///   can use the Texture.
    /// </summary>
    /// <param name="key">The key the other device acquires the mutex with.</param>
    /// <exception cref="NotSupportedException">The Texture has no keyed mutex (see <see cref="HasKeyedMutex"/>).</exception>
    public void ReleaseSync(ulong key)
    {
#if STRIDE_GRAPHICS_API_DIRECT3D11
        using var keyedMutex = GetKeyedMutex();
        HResult result = keyedMutex.ReleaseSync(key);
        if (result.IsFailure)
            result.Throw();
#else
        throw new NotSupportedException("Only a Direct3D 11 Texture has a keyed mutex.");
#endif
    }

#if STRIDE_GRAPHICS_API_DIRECT3D11
    private ComPtr<IDXGIKeyedMutex> GetKeyedMutex()
    {
        if (!HasKeyedMutex)
            throw new NotSupportedException("The Texture was not created with the SharedKeyedMutex option.");

        HResult result = NativeDeviceChild.QueryInterface(out ComPtr<IDXGIKeyedMutex> keyedMutex);
        if (result.IsFailure)
            result.Throw();
        return keyedMutex;
    }
#endif
}
