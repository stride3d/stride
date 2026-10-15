// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Stride.Shaders;

namespace Stride.Graphics;

public partial class CommandList
{
    /// <summary>
    ///   The size in bytes of the push constants, on every graphics API.
    /// </summary>
    public const int PushConstantsSize = EffectReflection.PushConstantsSize;

    [InlineArray(PushConstantsSize / sizeof(uint))]
    private struct PushConstantsData
    {
        private uint element;
    }

    // The CPU copy of the push constants: kept until changed, and given to every pipeline with a push-constant block
    private PushConstantsData pushConstants;

    // Whether the native command list needs the CPU copy again: after SetPushConstants, a pipeline change (Vulkan,
    // Direct3D 12) or a new native command list. Cleared when applied before a draw or dispatch.
    private bool pushConstantsDirty = true;

    private ReadOnlySpan<byte> PushConstantsBytes => MemoryMarshal.AsBytes((ReadOnlySpan<uint>)pushConstants);

    /// <summary>
    ///   Sets push constants: small values that the shaders read from their <c>[PushConstant] cbuffer</c> declarations,
    ///   without a descriptor set or a constant buffer to update.
    /// </summary>
    /// <param name="data">The bytes to write.</param>
    /// <param name="offset">The byte offset where <paramref name="data"/> starts in the push-constant block.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="offset"/> is negative, or <paramref name="data"/> goes past <see cref="PushConstantsSize"/> bytes.
    /// </exception>
    /// <remarks>
    ///   <para>
    ///     The values stay set until changed, also when the pipeline changes and when the command list is flushed or reset;
    ///     <see cref="ClearState"/> sets them to zero. They are given to the GPU before the next draw or dispatch whose
    ///     pipeline has a push-constant block (Vulkan push constants, Direct3D 12 root constants, a constant buffer on
    ///     Direct3D 11).
    ///   </para>
    ///   <para>
    ///     The layout is the one of the effect's push-constant block (<see cref="EffectReflection.PushConstants"/>): the
    ///     members of all its <c>[PushConstant] cbuffer</c> declarations, one after the other, each vector at a multiple of
    ///     its size.
    ///   </para>
    /// </remarks>
    public void SetPushConstants(ReadOnlySpan<byte> data, int offset = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset + data.Length, PushConstantsSize, nameof(data));

        data.CopyTo(MemoryMarshal.AsBytes((Span<uint>)pushConstants)[offset..]);
        pushConstantsDirty = true;
    }

    /// <summary>
    ///   Sets push constants from a struct with the layout of the effect's push-constant block.
    /// </summary>
    /// <typeparam name="T">A struct with the same members as the push-constant block, in the same order.</typeparam>
    /// <param name="value">The values to write, from the start of the block.</param>
    /// <inheritdoc cref="SetPushConstants(ReadOnlySpan{byte}, int)" path="/remarks"/>
    public void SetPushConstants<T>(in T value) where T : unmanaged
        => SetPushConstants(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in value)));

    private void ClearPushConstants()
    {
        pushConstants = default;
        pushConstantsDirty = true;
    }
}
