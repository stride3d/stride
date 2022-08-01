using System;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Stride.Core.Memory;

#if false
For a similar implementation, see e.g.
https://raw.githubusercontent.com/dotnet/runtime/4f9ae42d861fcb4be2fcd5d3d55d5f227d30e723/src/libraries/Common/tests/System/Buffers/NativeMemoryManager.cs
#endif
/// <summary>Manages unmanaged <see cref="Memory{T}"/>.
/// <para>Do not expose directly, expose <see cref="MemoryManager{T}.Memory"/>.</para></summary>
internal unsafe class NativeMemoryManager<T> : MemoryManager<T> where T : unmanaged
{
    /// <summary>Allocates memory.
    /// <para>It is recommended not to allocate a total of 2GB or more.</para>
    /// <para>The allocated bytes will equal <paramref name="elements"/> times <see cref="Unsafe.SizeOf{T}()"/>.</para>
    /// <para>If 2GB or more is needed, extensively test all code paths that may need to work with the allocation.</para></summary>
    /// <param name="elements">The number of elements of type <typeparamref name="T"/> to allocate memory for.</param>
    /// <param name="alignment">The desired alignment.
    /// <para>Must be unspecified, <c>0</c>, or a power of 2.</para>
    /// <para>If not specified or <c>0</c>, the actual alignment will be at least the maximum of <c>sizeof(void*)</c>
    /// and the natural alignment of type <typeparamref name="T"/>.</para>
    /// <para>It is recommended to not specify an explicit alignment.
    /// Rather make sure the natural alignment of type <typeparamref name="T"/> is appropriate,
    /// such that the runtime will automatically align the type regardless of where instances
    /// of it are placed in memory - on the stack, or on the heap.</para></param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="elements"/> is negative or
    /// - in 32-bit processes - so large that <see cref="AllocatedBytes"/> would overflow <c>nuint</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="alignment"/> is not zero and not a power of 2.</exception>
    /// <exception cref="OutOfMemoryException">The requested amount of memory cannot be allocated.</exception>
    public NativeMemoryManager(int elements, uint alignment = 0)
    {
        if (elements < 0)
            throw new ArgumentOutOfRangeException(nameof(elements));
        if (alignment == 0)
            alignment = Math.Max((uint)sizeof(void*), GetNaturalAlignment());
        else if (!BitOperations.IsPow2(alignment))
            throw new ArgumentException("Alignment is not a power of 2.", nameof(alignment));

        nuint bytes;
        if (Environment.Is64BitProcess)
            bytes = (nuint)elements * (nuint)Unsafe.SizeOf<T>();
        else
            try {
                bytes = checked((nuint)elements * (nuint)Unsafe.SizeOf<T>());
            } catch (Exception overflow) {
                throw new ArgumentOutOfRangeException(nameof(elements), overflow);
            }

        if (alignment <= (uint)sizeof(void*)) {
            ptr = NativeMemory.Alloc(bytes);
            flags = Flags.None;
        } else {
            ptr = NativeMemory.AlignedAlloc(bytes, alignment);
            flags = Flags.Aligned;
        }
        Length = elements;
        AllocatedBytes = bytes;
        Alignment = alignment;
    }
    private void* ptr;
    /// <summary>The size in bytes of the allocated memory.</summary>
    public nuint AllocatedBytes { get; }
    /// <summary>The number of elements of type <typeparamref name="T"/> for which memory was allocated.</summary>
    public int Length { get; }
    /// <summary>The alignment of the first element of type <typeparamref name="T"/>.
    /// <para>The actual alignment is equal to or larger than the alignment requested or computed at allocation time.</para></summary>
    public uint Alignment { get; }
    /// <summary>The number of times the memory has been pinned.</summary>
    private int pincount;
    /// <summary>Tracks whether the memory was allocated aligned and whether this instance is disposed.</summary>
    private Flags flags;
    /// <summary>Indicates whether this instance is disposed.
    /// <para>The memory will not be freed until this instance is disposed
    /// and all pinned references to its memory are out of scope.</para></summary>
    public bool IsDisposed => 0 != (Flags.Disposed & flags);
    /// <summary>Indicates whether there are any pinned references
    /// to the memory managed by this instance.</summary>
    public bool IsRetained => pincount > 0;
    private enum Flags : int
    {
        None = 0,
        Aligned = 1,
        Disposed = unchecked((int)0x8000_0000)
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct NaturalAlignment
    {
        public byte b; // do not let t1 be 16-byte aligned on x64
        public T t1; // might happen to be aligned more than needed
        public T t2; // then this one isn't
    }
    [SkipLocalsInit]
    private static uint GetNaturalAlignment()
    {
        Unsafe.SkipInit(out NaturalAlignment na);
        return GetNaturalAlignment(ref na);
    }
    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe uint GetNaturalAlignment(ref NaturalAlignment a) => 1u << Math.Min(
        TrailingZeroCount((nuint)Unsafe.AsPointer(ref a.t1)),
        TrailingZeroCount((nuint)Unsafe.AsPointer(ref a.t2)));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int TrailingZeroCount(nuint value) => Environment.Is64BitProcess
        ? BitOperations.TrailingZeroCount((ulong)value)
        : BitOperations.TrailingZeroCount((uint)value);
    /// <inheritdoc/>

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Span<T> GetSpan() {
        var ptr = this.ptr;
        return ptr == null ? default : new(ptr, Length);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override MemoryHandle Pin(int elementIndex = 0) {
        // use > to allow pinning zero-length memory
        if ((uint)elementIndex > (uint)Length)
            throw new ArgumentOutOfRangeException(nameof(elementIndex));
        var offset = (nuint)elementIndex * (nuint)Unsafe.SizeOf<T>();
        var address = (nuint)ptr + offset;
        lock (this) {
            if (pincount <= 0 && 0 != (flags & Flags.Disposed))
                throw new ObjectDisposedException(nameof(NativeMemoryManager<T>));
            pincount++;
            return new MemoryHandle((void*)address, default, this);
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Unpin()
    {
        lock (this)
        {
            if (pincount <= 0)
                throw new SynchronizationLockException("An attempt was made to unpin memory more often than it has been pinned.");
            if (--pincount == 0 && IsDisposed)
                LockedFree();
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (0 == (flags & Flags.Disposed))
            lock (this) {
                flags |= Flags.Disposed;
                if (pincount <= 0)
                    LockedFree();
            }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void LockedFree()
    {
        Debug.Assert(Monitor.IsEntered(this));
        Debug.Assert(pincount == 0 && 0 != (flags & Flags.Disposed));
        Debug.Assert(ptr is not null);

        if (0 != (flags & Flags.Aligned))
            NativeMemory.AlignedFree(ptr);
        else
            NativeMemory.Free(ptr);
        ptr = null;
    }
}
