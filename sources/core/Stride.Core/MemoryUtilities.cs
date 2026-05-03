// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stride.Core;

/// <summary>
///   Provides a set of static utility methods for memory management.
/// </summary>
public static class MemoryUtilities
{
    /// <summary>
    ///   Determines whether the current process architecture supports <strong>safe unaligned memory access</strong>.
    /// </summary>
    /// <remarks>
    ///   Unaligned memory access is supported on x64, x86, and ARM64 architectures.
    ///   On other architectures, attempting unaligned access may result in exceptions or degraded performance.
    ///   Use this method to conditionally perform unaligned operations based on platform capabilities.
    /// </remarks>
    /// <returns>
    ///   <see langword="true"/> if unaligned memory access is safe on the current architecture;
    ///   otherwise, <see langword="false"/>.
    /// </returns>
    // NOTE: This MUST BE A METHOD and AGGRESSIVELY INLINED, otherwise the JIT will not eliminate the branch
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsUnalignedSafe() =>
        RuntimeInformation.ProcessArchitecture is Architecture.X64 or Architecture.X86 or Architecture.Arm64;


    /// <inheritdoc cref="Unsafe.CopyBlock(ref byte, ref readonly byte, uint)"/>
    /// <remarks>
    ///   <para>
    ///     Some platform architectures do not support arbitrary unaligned memory reads or writes.
    ///     This method checks whether unaligned memory access is safe on the current architecture
    ///     and uses the most efficient method available.
    ///   </para>
    ///   <para>
    ///     Use this method instead of other memory copying methods if you are not sure whether
    ///     the pointers or references you pass in are aligned.
    ///   </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void CopyWithAlignmentFallback(ref byte destination, ref readonly byte source, uint byteCount)
    {
        if (IsUnalignedSafe())
            fixed (void* src = &source, dst = &destination)
                Buffer.MemoryCopy(src, dst, byteCount, byteCount);
        else
            Unsafe.CopyBlockUnaligned(ref destination, in source, byteCount);
    }

    /// <inheritdoc cref="Unsafe.CopyBlock(ref byte, ref readonly byte, uint)"/>
    /// <inheritdoc cref="CopyWithAlignmentFallback(ref byte, ref readonly byte, uint)" path="/remarks"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void CopyWithAlignmentFallback(void* destination, void* source, uint byteCount)
    {
        if (IsUnalignedSafe())
            Buffer.MemoryCopy(source, destination, byteCount, byteCount);
        else
            Unsafe.CopyBlockUnaligned(destination, source, byteCount);
    }

    /// <inheritdoc cref="Unsafe.CopyBlock(ref byte, ref readonly byte, uint)"/>
    /// <inheritdoc cref="CopyWithAlignmentFallback(ref byte, ref readonly byte, uint)" path="/remarks"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void CopyWithAlignmentFallback(nint destination, nint source, uint byteCount)
    {
        if (IsUnalignedSafe())
            Buffer.MemoryCopy((void*) source, (void*) destination, byteCount, byteCount);
        else
            Unsafe.CopyBlockUnaligned((void*) destination, (void*) source, byteCount);
    }


    /// <summary>
    ///   Clears the contents of memory at a provided address, zeroing it out.
    /// </summary>
    /// <param name="startAddress">The starting address of the memory to clear.</param>
    /// <param name="byteCount">The number of bytes to clear.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Clear(void* startAddress, uint byteCount)
    {
        // Span swaps between InitBlockUnaligned and _ZeroMemory depending on the size
        new Span<byte>(startAddress, (int) byteCount).Clear();
    }

    /// <summary>
    ///   Clears the contents of memory at a provided address, zeroing it out.
    /// </summary>
    /// <param name="startAddress">The starting address of the memory to clear.</param>
    /// <param name="byteCount">The number of bytes to clear.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Clear(nint startAddress, uint byteCount)
    {
        // Span swaps between InitBlockUnaligned and _ZeroMemory depending on the size
        new Span<byte>((void*) startAddress, (int)byteCount).Clear();
    }

    /// <summary>
    ///   Clears the contents of memory at a provided address, zeroing it out.
    /// </summary>
    /// <param name="startAddress">A reference to the starting byte of the memory to clear.</param>
    /// <param name="byteCount">The number of bytes to clear.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Clear(ref byte startAddress, uint byteCount)
    {
        // Span swaps between InitBlockUnaligned and _ZeroMemory depending on the size
        MemoryMarshal.CreateSpan(ref startAddress, (int) byteCount).Clear();
    }

    /// <summary>
    ///   Allocates an aligned memory buffer of the requested size.
    /// </summary>
    /// <param name="sizeInBytes">The size of the buffer to allocate.</param>
    /// <param name="alignment">
    ///   The memory alignment. It must be a positive power of two value.
    ///   Defaults to 16 bytes.
    /// </param>
    /// <returns>A pointer to the allocated aligned buffer.</returns>
    /// <exception cref="ArgumentException">
    ///   The <paramref name="alignment"/> parameter is not a positive power of two value.
    /// </exception>
    /// <remarks>
    ///   To free the buffer allocated by this method, use the <see cref="Free"/> method
    /// </remarks>
    public static unsafe nint Allocate(int sizeInBytes, int alignment = 16)
    {
        var alignmentMask = alignment - 1;

        if (!BitOperations.IsPow2(alignment))
            ThrowAlignmentNotPowerOfTwo(alignment);

        // We allocate extra memory to be able to align the pointer returned
        // and to store the original pointer so it can be freed later
        IntPtr memoryPtr = Marshal.AllocHGlobal(sizeInBytes + alignmentMask + IntPtr.Size);

        IntPtr alignedPtr = (memoryPtr + IntPtr.Size + alignmentMask) & ~alignmentMask;
        ((nint*) alignedPtr)[-1] = memoryPtr;

        return alignedPtr;
    }

    /// <summary>
    ///   Allocates an aligned memory buffer of the requested size, and clears its contents.
    /// </summary>
    /// <param name="sizeInBytes">The size of the buffer to allocate.</param>
    /// <param name="clearValue">The value to use to clear the buffer. Default is 0.</param>
    /// <param name="alignment">
    ///   The memory alignment. It must be a positive power of two value.
    ///   Defaults to 16 bytes.
    /// </param>
    /// <returns>A pointer to the allocated aligned buffer.</returns>
    /// <remarks>
    ///   To free the buffer allocated by this method, use the <see cref="Free"/> method
    /// </remarks>
    public static unsafe nint AllocateCleared(int sizeInBytes, byte clearValue = 0, int alignment = 16)
    {
        var memoryPtr = Allocate(sizeInBytes, alignment);
        Unsafe.InitBlockUnaligned((void*) memoryPtr, clearValue, (uint) sizeInBytes);

        return memoryPtr;
    }

    /// <summary>
    ///   Frees an aligned memory buffer.
    /// </summary>
    /// <param name="alignedBuffer">The aligned buffer to free.</param>
    /// <remarks>
    ///   The buffer must have been allocated with <see cref="AllocateMemory"/>.
    /// </remarks>
    public static unsafe void Free(nint alignedBuffer)
    {
        nint originalPtr = ((nint*) alignedBuffer)[-1];
        Marshal.FreeHGlobal(originalPtr);
    }


    /// <summary>
    ///   Determines whether the specified memory pointer is aligned.
    /// </summary>
    /// <param name="memoryPtr">The memory pointer.</param>
    /// <param name="alignment">
    ///   The memory alignment. It must be a positive power of two value.
    ///   Defaults to 16 bytes.
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if the specified <paramref name="memoryPtr"/> is aligned
    ///   to the given <paramref name="alignment"/>;
    ///   otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///   The <paramref name="alignment"/> parameter is not a positive power of two value.
    /// </exception>
    public static unsafe bool IsAligned(void* memoryPtr, int alignment = 16)
        => BitOperations.IsPow2(alignment)
            ? ((nint) memoryPtr & (alignment - 1)) == 0
            : ThrowAlignmentNotPowerOfTwo(alignment);

    /// <summary>
    ///   Determines whether the specified memory address is aligned.
    /// </summary>
    /// <param name="memoryAddress">The memory address.</param>
    /// <param name="alignment">
    ///   The memory alignment. It must be a positive power of two value.
    ///   Defaults to 16 bytes.
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if the specified <paramref name="memoryAddress"/> is aligned
    ///   to the given <paramref name="alignment"/>;
    ///   otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///   The <paramref name="alignment"/> parameter is not a positive power of two value.
    /// </exception>
    public static bool IsAligned(nint memoryAddress, int alignment = 16)
        => BitOperations.IsPow2(alignment)
            ? (memoryAddress & (alignment - 1)) == 0
            : ThrowAlignmentNotPowerOfTwo(alignment);

    /// <summary>
    ///   Determines whether the specified memory reference is aligned.
    /// </summary>
    /// <param name="memoryRef">The memory reference.</param>
    /// <param name="alignment">
    ///   The memory alignment. It must be a positive power of two value.
    ///   Defaults to 16 bytes.
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if the specified <paramref name="memoryRef"/> is aligned
    ///   to the given <paramref name="alignment"/>;
    ///   otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///   The <paramref name="alignment"/> parameter is not a positive power of two value.
    /// </exception>
    public static unsafe bool IsAligned(ref byte memoryRef, int alignment = 16)
        => BitOperations.IsPow2(alignment)
            ? ((nint) Unsafe.AsPointer(ref memoryRef) & (alignment - 1)) == 0
            : ThrowAlignmentNotPowerOfTwo(alignment);

    /// <summary>
    ///   Determines whether the specified span is aligned.
    /// </summary>
    /// <param name="span">The memory span.</param>
    /// <param name="alignment">
    ///   The memory alignment. It must be a positive power of two value.
    ///   Defaults to 16 bytes.
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if the specified <paramref name="span"/> is aligned
    ///   to the given <paramref name="alignment"/>;
    ///   otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///   The <paramref name="alignment"/> parameter is not a positive power of two value.
    /// </exception>
    public static unsafe bool IsAligned(ReadOnlySpan<byte> span, int alignment = 16)
        => BitOperations.IsPow2(alignment)
            ? ((nint) Unsafe.AsPointer(in span) & (alignment - 1)) == 0
            : ThrowAlignmentNotPowerOfTwo(alignment);

    /// <summary>
    ///   Determines whether the specified span is aligned.
    /// </summary>
    /// <param name="span">The memory span.</param>
    /// <param name="alignment">
    ///   The memory alignment. It must be a positive power of two value.
    ///   Defaults to 16 bytes.
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if the specified <paramref name="span"/> is aligned
    ///   to the given <paramref name="alignment"/>;
    ///   otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///   The <paramref name="alignment"/> parameter is not a positive power of two value.
    /// </exception>
    public static unsafe bool IsAligned(Span<byte> span, int alignment = 16)
        => BitOperations.IsPow2(alignment)
            ? ((nint) Unsafe.AsPointer(in span) & (alignment - 1)) == 0
            : ThrowAlignmentNotPowerOfTwo(alignment);

    /// <summary>
    ///   Determines whether the specified memory is aligned.
    /// </summary>
    /// <param name="memory">The memory.</param>
    /// <param name="alignment">
    ///   The memory alignment. It must be a positive power of two value.
    ///   Defaults to 16 bytes.
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if the specified <paramref name="memory"/> is aligned
    ///   to the given <paramref name="alignment"/>;
    ///   otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///   The <paramref name="alignment"/> parameter is not a positive power of two value.
    /// </exception>
    public static unsafe bool IsAligned(ReadOnlyMemory<byte> memory, int alignment = 16)
        => BitOperations.IsPow2(alignment)
            ? ((nint) Unsafe.AsPointer(in memory) & (alignment - 1)) == 0
            : ThrowAlignmentNotPowerOfTwo(alignment);

    /// <summary>
    ///   Determines whether the specified memory is aligned.
    /// </summary>
    /// <param name="memory">The memory memory.</param>
    /// <param name="alignment">
    ///   The memory alignment. It must be a positive power of two value.
    ///   Defaults to 16 bytes.
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if the specified <paramref name="memory"/> is aligned
    ///   to the given <paramref name="alignment"/>;
    ///   otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///   The <paramref name="alignment"/> parameter is not a positive power of two value.
    /// </exception>
    public static unsafe bool IsAligned(Memory<byte> memory, int alignment = 16)
        => BitOperations.IsPow2(alignment)
            ? ((nint) Unsafe.AsPointer(in memory) & (alignment - 1)) == 0
            : ThrowAlignmentNotPowerOfTwo(alignment);


    /// <summary>
    ///   Swaps two values.
    /// </summary>
    /// <typeparam name="T">The type of the values to swap.</typeparam>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    public static void Swap<T>(ref T left, ref T right)
    {
        (right, left) = (left, right);
    }

    #region Throw helpers

    [DoesNotReturn]
    private static bool ThrowAlignmentNotPowerOfTwo(int align, [CallerArgumentExpression(nameof(align))] string? paramName = null)
    {
        throw new ArgumentException("The alignment must be a positive power of 2.", paramName);
    }

    #endregion
}
