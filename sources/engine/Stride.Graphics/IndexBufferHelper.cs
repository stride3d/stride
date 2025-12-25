// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable
using System;
using System.Runtime.InteropServices;
using Stride.Core;

namespace Stride.Graphics;

/// <example>
/// Reading the indices of a mesh:
/// <code>
/// <![CDATA[
/// Model.Meshes[0].Draw.IndexBuffer.AsReadable(Services, out IndexBufferHelper helper, out int count)
/// var indices = helper.To32Bit();
/// ]]>
/// </code>
/// </example>
public readonly struct IndexBufferHelper
{
    /// <summary>
    /// Full index buffer, does not account for the binding offset or length
    /// </summary>
    public readonly byte[] DataOuter;
    public readonly IndexBufferBinding Binding;

    /// <summary>
    /// Effective index buffer, handles the binding offset
    /// </summary>
    public Span<byte> DataInner => DataOuter.AsSpan(Binding.Offset, Binding.Count * (Binding.Is32Bit ? 4 : 2));

    /// <inheritdoc cref="MeshExtension.AsReadable(IndexBufferBinding, IServiceRegistry, out IndexBufferHelper, out int)"/>
    public IndexBufferHelper(IndexBufferBinding binding, IServiceRegistry services, out int count) 
        : this(binding, MeshExtension.FetchBufferContentOrThrow(binding.Buffer, services), out count)
    {
    }

    /// <inheritdoc cref="MeshExtension.AsReadable(IndexBufferBinding, IServiceRegistry, out IndexBufferHelper, out int)"/>
    public IndexBufferHelper(IndexBufferBinding binding, byte[] dataOuter, out int count)
    {
        if (dataOuter.Length < binding.Offset + binding.Count * (binding.Is32Bit ? 4 : 2))
            throw new ArgumentException($"Binding describes an array larger than {nameof(dataOuter)} ({dataOuter.Length} < {binding.Offset} + {binding.Count} * {(binding.Is32Bit ? 4 : 2)})");

        DataOuter = dataOuter;
        Binding = binding;
        count = Binding.Count;
    }

    /// <summary>
    /// Branch to read the buffer as a 16 or 32 bit buffer, does not allocate
    /// </summary>
    /// <example>
    /// <code>
    /// if (Is32Bit(out var d32, out var d16))
    /// {
    ///     foreach (var value in d32)
    ///     {
    ///         // Your logic for 32 bit
    ///     }
    /// }
    /// else
    /// {
    ///     foreach (var value in d16)
    ///     {
    ///         // Your logic for 16bit
    ///     }
    /// }
    /// </code>
    /// </example>
    public bool Is32Bit(out Span<int> data32, out Span<ushort> data16)
    {
        if (Binding.Is32Bit)
        {
            data32 = MemoryMarshal.Cast<byte, int>(DataInner);
            data16 = Span<ushort>.Empty;
            return true;
        }
        else
        {
            data32 = Span<int>.Empty;
            data16 = MemoryMarshal.Cast<byte, ushort>(DataInner);
            return false;
        }
    }

    /// <summary>
    /// Does not allocate if the buffer is already 32 bit,
    /// otherwise allocates a new int[] and copies the data into it
    /// </summary>
    public Span<int> To32Bit()
    {
        if (Is32Bit(out var d32, out var d16))
        {
            return d32;
        }
        else
        {
            var output = new int[d16.Length];
            for (int i = 0; i < d16.Length; i++)
                output[i] = d16[i];

            return output;
        }
    }

    /// <summary>
    /// Does not allocate if the buffer is already 16 bit,
    /// otherwise allocates a new ushort[] and copies the data into it
    /// </summary>
    public Span<ushort> To16Bit()
    {
        if (Is32Bit(out var d32, out var d16))
        {
            var output = new ushort[d32.Length];
            for (int i = 0; i < d32.Length; i++)
                output[i] = (ushort)d32[i];

            return output;
        }
        else
        {
            return d16;
        }
    }

    public void CopyTo(Span<int> dest)
    {
        if (Is32Bit(out var d32, out var d16))
        {
            d32.CopyTo(dest);
        }
        else
        {
            for (int i = 0; i < d16.Length; i++)
                dest[i] = d16[i];
        }
    }

    public void CopyTo(Span<ushort> dest)
    {
        if (Is32Bit(out var d32, out var d16))
        {
            for (int i = 0; i < d32.Length; i++)
                dest[i] = (ushort)d32[i];
        }
        else
        {
            d16.CopyTo(dest);
        }
    }
}
