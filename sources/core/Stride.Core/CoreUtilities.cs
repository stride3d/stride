using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Stride.Core;

internal static class CoreUtilities
{
    /// <summary>Copies bytes from the source address to the destination address.
    /// <para>A thin wrapper around <see cref="Unsafe.CopyBlock(void*, void*, uint)"/>.</para></summary>
    /// <param name="destination">The destination address.</param>
    /// <param name="source">The source address.</param>
    /// <param name="byteCount">The number of bytes to copy.</param>
    /// <remarks>This API corresponds to the <c>cpblk</c> opcode.
    /// Both the <paramref name="destination"/> and <paramref name="source"/>
    /// pointers are assumed to be pointer-aligned.</remarks>
    public static unsafe void CopyBlock(nint destination, nint source, int byteCount)
    {
        Debug.Assert(byteCount >= 0);
        Unsafe.CopyBlock((void*)destination, (void*)source, (uint)byteCount);
    }
    /// <summary>Copies bytes from the source address to the destination address without assuming architecture dependent alignment of the addresses.
    /// <para>A thin wrapper around <see cref="Unsafe.CopyBlockUnaligned(void*, void*, uint)"/></para></summary>
    /// <param name="destination">The destination address.</param>
    /// <param name="source">The source address.</param>
    /// <param name="byteCount">The number of bytes to copy.</param>
    /// <remarks>This API corresponds to the <c>unaligned.1 cpblk</c> opcode sequence.
    /// No alignment assumptions are made about the <paramref name="destination"/> or <paramref name="source"/> pointers.</remarks>
    public static unsafe void CopyBlockUnaligned(nint destination, nint source, int byteCount)
    {
        Debug.Assert(byteCount >= 0);
        Unsafe.CopyBlockUnaligned((void*)destination, (void*)source, (uint)byteCount);
    }
    /// <summary>Initializes a block of memory.
    /// <para>A thin wrapper around <see cref="Unsafe.InitBlock(void*, byte, uint)"/>.</para></summary>
    /// <param name="destination">The destination address.</param>
    /// <param name="value">The value to initialize all bytes of the memory block to.</param>
    /// <param name="byteCount">The number of bytes to initialize.</param>
    /// <remarks>This API corresponds to the <c>initblk</c> opcode.
    /// The <paramref name="destination"/> pointer is assumed to be pointer-aligned.</remarks>
    public static unsafe void InitBlock(nint destination, byte value, int byteCount)
    {
        Debug.Assert(byteCount >= 0);
        Unsafe.InitBlock((void*)destination, value, (uint)byteCount);
    }
    /// <summary>Initializes a block of memory.
    /// <para>A thin wrapper around <see cref="Unsafe.InitBlockUnaligned(void*, byte, uint)"/>.</para></summary>
    /// <param name="destination">The destination address.</param>
    /// <param name="value">The value to initialize all bytes of the memory block to.</param>
    /// <param name="byteCount">The number of bytes to initialize.</param>
    /// <remarks>This API corresponds to the <c>initblk</c> opcode.
    /// No alignment assumption is made about the <paramref name="destination"/> pointer.</remarks>
    public static unsafe void InitBlockUnaligned(nint destination, byte value, int byteCount)
    {
        Debug.Assert(byteCount >= 0);
        Unsafe.InitBlock((void*)destination, value, (uint)byteCount);
    }
    /// <summary>Determines whether the memory ranges starting at
    /// <paramref name="address"/> and <paramref name="otherAddress"/> have the same contents.
    /// <para>A thin wrapper around <see cref="MemoryExtensions.SequenceEqual{T}(ReadOnlySpan{T}, ReadOnlySpan{T})"/>.</para></summary>
    /// <param name="address">An address in memory.</param>
    /// <param name="otherAddress">Another address in memory.</param>
    /// <param name="byteCount">The number of bytes to compare.</param>
    /// <returns><c>true</c> if both memory ranges contain the same data. <c>false</c> otherwise.</returns>
    public static unsafe bool SequenceEqual(nint address, nint otherAddress, int byteCount)
        => SequenceEqual((void*)address, (void*)otherAddress, byteCount);
    /// <summary>Determines whether the memory ranges starting at
    /// <paramref name="address"/> and <paramref name="otherAddress"/> have the same contents.
    /// <para>A thin wrapper around <see cref="MemoryExtensions.SequenceEqual{T}(ReadOnlySpan{T}, ReadOnlySpan{T})"/>.</para></summary>
    /// <param name="address">An address in memory.</param>
    /// <param name="otherAddress">Another address in memory.</param>
    /// <param name="byteCount">The number of bytes to compare.</param>
    /// <returns><c>true</c> if both memory ranges contain the same data. <c>false</c> otherwise.</returns>
    public static unsafe bool SequenceEqual(void* address, void* otherAddress, int byteCount)
    {
        var lhs = new ReadOnlySpan<byte>(address, byteCount);
        var rhs = new ReadOnlySpan<byte>(otherAddress, byteCount);
        return lhs.SequenceEqual(rhs);
    }
}
