// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics
{
    /// <summary>
    ///   Flags specifying which resources and features are supported when using compute shaders
    ///   for a given pixel format for a graphics device.
    /// </summary>
    /// <remarks>
    ///   For more information, see <see cref="GraphicsDevice.Features"/>.
    /// </remarks>
    [Flags]
    public enum ComputeShaderFormatSupport
    {
        None = 0,

        // D3D11_FORMAT_SUPPORT2_UAV_ATOMIC_ADD
        AtomicAdd = 1,

        // D3D11_FORMAT_SUPPORT2_UAV_ATOMIC_BITWISE_OPS
        AtomicBitwiseOperations = 2,

        // D3D11_FORMAT_SUPPORT2_UAV_ATOMIC_COMPARE_STORE_OR_COMPARE_EXCHANGE
        AtomicCompareStoreOrCompareExchange = 4,

        // D3D11_FORMAT_SUPPORT2_UAV_ATOMIC_EXCHANGE
        AtomicExchange = 8,

        // D3D11_FORMAT_SUPPORT2_UAV_ATOMIC_SIGNED_MIN_OR_MAX
        AtomicSignedMinimumOrMaximum = 0x00000010,

        // D3D11_FORMAT_SUPPORT2_UAV_ATOMIC_UNSIGNED_MIN_OR_MAX
        AtomicUnsignedMinimumOrMaximum = 0x00000020,

        // D3D11_FORMAT_SUPPORT2_UAV_TYPED_LOAD
        TypedLoad = 0x00000040,

        // D3D11_FORMAT_SUPPORT2_UAV_TYPED_STORE
        TypedStore = 0x00000080,

        // D3D11_FORMAT_SUPPORT2_OUTPUT_MERGER_LOGIC_OP
        OutputMergerLogicOperation = 0x00000100,

        // D3D11_FORMAT_SUPPORT2_TILED
        Tiled = 0x00000200,

        // D3D11_FORMAT_SUPPORT2_SHAREABLE
        Shareable = 0x00000400,

        // D3D11_FORMAT_SUPPORT2_MULTIPLANE_OVERLAY
        MultiplaneOverlay = 0x00004000
    }
}
