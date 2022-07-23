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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Stride.Core.Annotations;

namespace Stride.Core
{
    /// <summary>
    /// Utility class.
    /// </summary>
    public static class Utilities
    {
        /// <summary>Copies bytes from the source address to the destination address.
        /// <para>A thin wrapper around <see cref="Unsafe.CopyBlock(void*, void*, uint)"/>.</para></summary>
        /// <param name="destination">The destination address.</param>
        /// <param name="source">The source address.</param>
        /// <param name="byteCount">The number of bytes to copy.</param>
        /// <remarks>This API corresponds to the <c>unaligned.1 cpblk</c> opcode sequence.
        /// No alignment assumptions are made about the <paramref name="destination"/> or <paramref name="source"/> pointers.</remarks>
        [Obsolete("Use System.Runtime.CompilerServices.Unsafe.CopyBlockUnaligned or CoreUtilities.CopyBlockUnaligned")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void CopyMemory(nint destination, nint source, int byteCount)
            => CoreUtilities.CopyBlockUnaligned(destination, source, byteCount);

        /// <summary>
        /// Compares two block of memory.
        /// </summary>
        /// <param name="from">The pointer to compare from.</param>
        /// <param name="against">The pointer to compare against.</param>
        /// <param name="sizeToCompare">The size in bytes to compare.</param>
        /// <returns>True if the buffers are equivalent, false otherwise.</returns>
        [Obsolete("Use CoreUtilities.SequenceEqual or Span<T>.SequenceEqual.")]
        public static unsafe bool CompareMemory(nint from, nint against, int sizeToCompare)
            => CoreUtilities.SequenceEqual(from, against, sizeToCompare);

        /// <summary>Clears the memory.</summary>
        /// <param name="destination">The destination address.</param>
        /// <param name="value">The byte value to fill the memory with.</param>
        /// <param name="sizeInBytesToClear">The size in bytes to clear.</param>
        [Obsolete("Use Span<T>.Fill or System.Runtime.CompilerServices.Unsafe.InitBlockUnaligned")]
        public static unsafe void ClearMemory(nint destination, byte value, int sizeInBytesToClear)
            => Unsafe.InitBlockUnaligned((void*)destination, value, (uint)sizeInBytesToClear);

        /// <summary>
        /// Return the sizeof a struct from a CLR. Equivalent to sizeof operator but works on generics too.
        /// </summary>
        /// <typeparam name="T">a struct to evaluate</typeparam>
        /// <returns>sizeof this struct</returns>
        [Obsolete("Use System.Runtime.CompilerServices.Unsafe.SizeOf<T>()")]
        public static int SizeOf<T>() where T : struct => Unsafe.SizeOf<T>();

        /// <summary>
        /// Return the sizeof an array of struct. Equivalent to sizeof operator but works on generics too.
        /// </summary>
        /// <typeparam name="T">a struct</typeparam>
        /// <param name="array">The array of struct to evaluate.</param>
        /// <returns>sizeof in bytes of this array of struct</returns>
        [Obsolete("Use System.Runtime.CompilerServices.Unsafe.SizeOf<T>() * array.Length")]
        public static int SizeOf<T>(T[] array) where T : struct
            => array is null ? 0 : array.Length * Unsafe.SizeOf<T>();

        /// <summary>
        /// Pins the specified source and call an action with the pinned pointer.
        /// </summary>
        /// <typeparam name="T">The type of the structure to pin</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="pinAction">The pin action to perform on the pinned pointer.</param>
        [Obsolete("Use fixed statement with `unmanaged` type constraint. See https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/fixed-statement")]
        public static unsafe void Pin<T>(ref T source, Action<nint> pinAction) where T : struct
            => pinAction((nint)Interop.Fixed(ref source));

        /// <summary>
        /// Pins the specified source and call an action with the pinned pointer.
        /// </summary>
        /// <typeparam name="T">The type of the structure to pin</typeparam>
        /// <param name="source">The source array.</param>
        /// <param name="pinAction">The pin action to perform on the pinned pointer.</param>
        [Obsolete("Use fixed statement with `unmanaged` type constraint. See https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/fixed-statement")]
        public static unsafe void Pin<T>(T[] source, [NotNull] Action<nint> pinAction) where T : struct
            => pinAction(source is null ? 0 : (nint)Interop.Fixed(source));

        /// <summary>
        /// Covnerts a structured array to an equivalent byte array.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>The byte array.</returns>
        [Obsolete("Allocates. Consider using System.Runtime.InteropServices.MemoryMarshal.Cast or System.Buffers.ArrayPool<T>.Shared")]
        public static unsafe byte[] ToByteArray<T>(T[] source) where T : struct
        {
            if (source is null) return null;
            var bytes = MemoryMarshal.Cast<T, byte>(source.AsSpan());
            var result = new byte[bytes.Length];
            bytes.CopyTo(result);
            return result;
        }

        /// <summary>
        /// Reads the specified T data from a memory location.
        /// </summary>
        /// <typeparam name="T">Type of a data to read</typeparam>
        /// <param name="source">Memory location to read from.</param>
        /// <returns>The data read from the memory location</returns>
        [Obsolete("Use System.Runtime.CompilerServices.Unsafe.ReadUnaligned")]
        public static unsafe T Read<T>(nint source) where T : struct
            => Unsafe.ReadUnaligned<T>((void*)source);

        /// <summary>
        /// Reads the specified T data from a memory location.
        /// </summary>
        /// <typeparam name="T">Type of a data to read</typeparam>
        /// <param name="source">Memory location to read from.</param>
        /// <param name="data">The data write to.</param>
        [Obsolete("Use System.Runtime.CompilerServices.Unsafe.ReadUnaligned")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Read<T>(nint source, ref T data) where T : struct
            => data = Unsafe.ReadUnaligned<T>((void*)source);

        /// <summary>
        /// Reads the specified T data from a memory location.
        /// </summary>
        /// <typeparam name="T">Type of a data to read</typeparam>
        /// <param name="source">Memory location to read from.</param>
        /// <param name="data">The data write to.</param>
        [Obsolete("Use System.Runtime.CompilerServices.Unsafe.ReadUnaligned")]
        public static unsafe void ReadOut<T>(nint source, out T data) where T : struct
            => data = Unsafe.ReadUnaligned<T>((void*)source);

        /// <summary>
        /// Reads the specified T data from a memory location.
        /// </summary>
        /// <typeparam name="T">Type of a data to read</typeparam>
        /// <param name="source">Memory location to read from.</param>
        /// <param name="data">The data write to.</param>
        /// <returns>source pointer + sizeof(T)</returns>
        [Obsolete("Use System.Runtime.CompilerServices.Unsafe.ReadUnaligned. Consider pointer arithmetic or System.Runtime.CompilerServices.Unsafe.Add.")]
        public static unsafe nint ReadAndPosition<T>(nint source, ref T data) where T : struct
        {
            data = Unsafe.ReadUnaligned<T>((void*)source);
            return (nint)source + Unsafe.SizeOf<T>();
        }

        /// <summary>
        /// Reads the specified array T[] data from a memory location.
        /// </summary>
        /// <typeparam name="T">Type of a data to read</typeparam>
        /// <param name="source">Memory location to read from.</param>
        /// <param name="data">The data write to.</param>
        /// <param name="offset">The offset in the array to write to.</param>
        /// <param name="count">The number of T element to read from the memory location</param>
        /// <returns>source pointer + sizeof(T) * count</returns>
        [Obsolete("Use Span<T>.Copy or System.Runtime.CompilerServices.Unsafe.CopyBlockUnaligned")]
        public static unsafe nint Read<T>(nint source, T[] data, int offset, int count) where T : struct
        {
            var src = new Span<T>((void*)source, count);
            src.CopyTo(data.AsSpan(offset));
            return (nint)source + count * Unsafe.SizeOf<T>();
        }

        /// <summary>
        /// Reads the specified T data from a memory location.
        /// </summary>
        /// <typeparam name="T">Type of a data to read</typeparam>
        /// <param name="source">Memory location to read from.</param>
        /// <param name="data">The data write to.</param>
        [Obsolete("Use System.Runtime.CompilerServices.Unsafe.ReadUnaligned")]
        internal static unsafe void UnsafeReadOut<T>(nint source, out T data)
            => data = Unsafe.ReadUnaligned<T>((void*)source);

        /// <summary>
        /// Writes the specified T data to a memory location.
        /// </summary>
        /// <typeparam name="T">Type of a data to write</typeparam>
        /// <param name="destination">Memory location to write to.</param>
        /// <param name="data">The data to write.</param>
        [Obsolete("Use System.Runtime.CompilerServices.Unsafe.WriteUnaligned")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Write<T>(nint destination, ref T data) where T : struct
            => Unsafe.WriteUnaligned((void*)destination, data);

        /// <summary>
        /// Writes the specified T data to a memory location.
        /// </summary>
        /// <typeparam name="T">Type of a data to write</typeparam>
        /// <param name="destination">Memory location to write to.</param>
        /// <param name="data">The data to write.</param>
        /// <returns>destination pointer + sizeof(T)</returns>
        [Obsolete("Use System.Runtime.CompilerServices.Unsafe.WriteUnaligned. Consider pointer arithmetic or System.Runtime.CompilerServices.Unsafe.Add.")]
        public static unsafe nint WriteAndPosition<T>(nint destination, ref T data) where T : struct
        {
            Unsafe.WriteUnaligned((void*)destination, data);
            return (nint)destination + Unsafe.SizeOf<T>();
        }

        /// <summary>
        /// Writes the specified T data to a memory location.
        /// </summary>
        /// <typeparam name="T">Type of a data to write</typeparam>
        /// <param name="destination">Memory location to write to.</param>
        /// <param name="data">The data to write.</param>
        [Obsolete("Use System.Runtime.CompilerServices.Unsafe.WriteUnaligned")]
        internal static unsafe void UnsafeWrite<T>(nint destination, ref T data)
            => Unsafe.WriteUnaligned((void*)destination, data);

        /// <summary>
        /// Writes the specified array T[] data to a memory location.
        /// </summary>
        /// <typeparam name="T">Type of a data to write</typeparam>
        /// <param name="destination">Memory location to write to.</param>
        /// <param name="data">The array of T data to write.</param>
        /// <param name="offset">The offset in the array to read from.</param>
        /// <param name="count">The number of T element to write to the memory location</param>
        [Obsolete("Use Span<T>.CopyTo or System.Runtime.CompilerServices.Unsafe.CopyBlockUnaligned")]
        public static void Write<T>(byte[] destination, T[] data, int offset, int count) where T : struct
        {
            var src = MemoryMarshal.Cast<T, byte>(data.AsSpan(offset, count));
            if (destination.Length < src.Length) 
                throw new ArgumentException("The destination array is too short.", nameof(destination));
            src.CopyTo(destination);
        }

        /// <summary>
        /// Writes the specified array T[] data to a memory location.
        /// </summary>
        /// <typeparam name="T">Type of a data to write</typeparam>
        /// <param name="destination">Memory location to write to.</param>
        /// <param name="data">The array of T data to write.</param>
        /// <param name="offset">The offset in the array to read from.</param>
        /// <param name="count">The number of T element to write to the memory location</param>
        /// <returns>destination pointer + sizeof(T) * count</returns>
        [Obsolete("Use Span<T>.CopyTo or System.Runtime.CompilerServices.Unsafe.CopyBlockUnaligned")]
        public static unsafe nint Write<T>(nint destination, T[] data, int offset, int count) where T : struct
        {
            var src = MemoryMarshal.Cast<T, byte>(data.AsSpan(offset, count));
            var dst = new Span<byte>((void*)destination, src.Length);
            src.CopyTo(dst);
            return (nint)destination + src.Length;
        }

        /// <summary>
        /// Allocate an aligned memory buffer.
        /// </summary>
        /// <param name="sizeInBytes">Size of the buffer to allocate.</param>
        /// <param name="align">Alignment, a positive value which is a power of 2. 16 bytes by default.</param>
        /// <returns>A pointer to a buffer aligned.</returns>
        /// <remarks>
        /// To free this buffer, call <see cref="FreeMemory"/>
        /// </remarks>
        public static unsafe nint AllocateMemory(int sizeInBytes, int align = 16)
        {
            var mask = align - 1;
            if ((align & mask) != 0)
            {
                throw new ArgumentException("Alignment is not a power of 2.", nameof(align));
            }
            var memPtr = (nint)Marshal.AllocHGlobal(sizeInBytes + mask + sizeof(void*));
            var ptr = (memPtr + sizeof(void*) + mask) & ~mask;
            ((nint*)ptr)[-1] = memPtr;
            return ptr;
        }

        /// <summary>
        /// Allocate an aligned memory buffer and clear it with a specified value (0 by defaault).
        /// </summary>
        /// <param name="sizeInBytes">Size of the buffer to allocate.</param>
        /// <param name="clearValue">Default value used to clear the buffer.</param>
        /// <param name="align">Alignment, 16 bytes by default.</param>
        /// <returns>A pointer to a buffer aligned.</returns>
        /// <remarks>
        /// To free this buffer, call <see cref="FreeMemory"/>
        /// </remarks>
        public static unsafe nint AllocateClearedMemory(int sizeInBytes, byte clearValue = 0, int align = 16)
        {
            var ptr = AllocateMemory(sizeInBytes, align);
            Unsafe.InitBlockUnaligned((void*)ptr, clearValue, (uint)sizeInBytes);
            return ptr;
        }

        /// <summary>
        /// Determines whether the specified memory pointer is aligned in memory.
        /// </summary>
        /// <param name="memoryPtr">The memory pointer.</param>
        /// <param name="align">The align.</param>
        /// <returns><c>true</c> if the specified memory pointer is aligned in memory; otherwise, <c>false</c>.</returns>
        public static bool IsMemoryAligned(nint memoryPtr, int align = 16)
            => BitOperations.IsPow2(align)
            ? ((nint)memoryPtr & --align) == 0
            : throw new ArgumentException("Alignment is not a power of 2.", nameof(align));

        /// <summary>
        /// Allocate an aligned memory buffer.
        /// </summary>
        /// <remarks>
        /// The buffer must have been allocated with <see cref="AllocateMemory"/>
        /// </remarks>
        public static unsafe void FreeMemory(nint alignedBuffer)
            => Marshal.FreeHGlobal(((nint*)alignedBuffer)[-1]);

        /// <summary>
        /// If non-null, disposes the specified object and set it to null, otherwise do nothing.
        /// </summary>
        /// <param name="disposable">The disposable.</param>
        public static void Dispose<T>(ref T disposable) where T : class, IDisposable
        {
            if (disposable is not null)
            {
                disposable.Dispose();
                disposable = null;
            }
        }

        /// <summary>
        /// String helper join method to display an array of object as a single string.
        /// </summary>
        /// <param name="separator">The separator.</param>
        /// <param name="array">The array.</param>
        /// <returns>a string with array elements serparated by the seperator</returns>
        [Obsolete("Use string.Join")]
        [NotNull]
        public static string Join<T>(string separator, T[] array)
            => string.Join(separator, array);

        /// <summary>
        /// String helper join method to display an enumrable of object as a single string.
        /// </summary>
        /// <param name="separator">The separator.</param>
        /// <param name="elements">The enumerable.</param>
        /// <returns>a string with array elements serparated by the seperator</returns>
        [Obsolete("Use string.Join")]
        [NotNull]
        public static string Join(string separator, [NotNull] IEnumerable elements)
            => string.Join(separator, elements);

        /// <summary>
        /// String helper join method to display an enumrable of object as a single string.
        /// </summary>
        /// <param name="separator">The separator.</param>
        /// <param name="elements">The enumerable.</param>
        /// <returns>a string with array elements serparated by the seperator</returns>
        [Obsolete("Use string.Join")]
        [NotNull]
        public static string Join(string separator, [NotNull] IEnumerator elements)
        {
            static IEnumerable<object> Enumerate(IEnumerator elements)
            {
                while (elements.MoveNext()) yield return elements.Current;
            }
            return string.Join(separator, Enumerate(elements));
        }

        /// <summary>
        ///   Read stream to a byte[] buffer
        /// </summary>
        /// <param name = "stream">input stream</param>
        /// <returns>a byte[] buffer</returns>
        [Obsolete("Allocates. Read into the destination.")]
        [NotNull]
        public static byte[] ReadStream([NotNull] Stream stream)
        {
            var readLength = 0;
            return ReadStream(stream, ref readLength);
        }

        /// <summary>
        ///   Read stream to a byte[] buffer
        /// </summary>
        /// <param name = "stream">input stream</param>
        /// <param name = "readLength">length to read</param>
        /// <returns>a byte[] buffer</returns>
        [Obsolete("Allocates. Read into the destination.")]
        [NotNull]
        public static byte[] ReadStream([NotNull] Stream stream, ref int readLength)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.CanRead);
            var count = readLength;
            Debug.Assert(count <= (stream.Length - stream.Position));
            if (count == 0) {
                readLength = (int)(stream.Length - stream.Position);
                count = readLength;
            }

            Debug.Assert(count >= 0);
            if (count == 0)
                return Array.Empty<byte>();

            var buffer = new byte[count];
            var bytesRead = 0;
            if (count > 0)
            {
                do
                {
                    bytesRead += stream.Read(buffer, bytesRead, readLength - bytesRead);
                } while (bytesRead < readLength);
            }
            return buffer;
        }

        /// <summary>
        /// Computes a hashcode for a dictionary.
        /// </summary>
        /// <returns>Hashcode for the list.</returns>
        public static int GetHashCode(IDictionary dict)
        {
            if (dict is null)
                return 0;

            var hashCode = 0;
            foreach (DictionaryEntry keyValue in dict)
            {
                hashCode = (hashCode * 397) ^ keyValue.Key.GetHashCode();
                hashCode = (hashCode * 397) ^ (keyValue.Value?.GetHashCode() ?? 0);
            }
            return hashCode;
        }

        /// <summary>
        /// Computes a hashcode for an enumeration
        /// </summary>
        /// <param name="it">An enumerator.</param>
        /// <returns>Hashcode for the list.</returns>
        public static int GetHashCode(IEnumerable it)
        {
            if (it is null)
                return 0;

            var hashCode = 0;
            foreach (var current in it)
            {
                hashCode = (hashCode * 397) ^ (current?.GetHashCode() ?? 0);
            }
            return hashCode;
        }

        /// <summary>
        /// Computes a hashcode for an enumeration
        /// </summary>
        /// <param name="it">An enumerator.</param>
        /// <returns>Hashcode for the list.</returns>
        public static int GetHashCode(IEnumerator it)
        {
            if (it is null)
                return 0;

            var hashCode = 0;
            while (it.MoveNext())
            {
                var current = it.Current;
                hashCode = (hashCode * 397) ^ (current?.GetHashCode() ?? 0);
            }
            return hashCode;
        }

        /// <summary>
        /// Compares two collection, element by elements.
        /// </summary>
        /// <param name="left">A "from" enumerator.</param>
        /// <param name="right">A "to" enumerator.</param>
        /// <returns>True if lists are identical. False otherwise.</returns>
        [Obsolete("Use SequenceEqualAllowNull")]
        public static bool Compare(IEnumerable left, IEnumerable right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;
            return left.Cast<object>().SequenceEqual(right.Cast<object>());
        }

        /// <summary>
        /// Compares two collection, element by elements.
        /// </summary>
        /// <param name="leftIt">A "from" enumerator.</param>
        /// <param name="rightIt">A "to" enumerator.</param>
        /// <returns>True if lists are identical. False otherwise.</returns>
        [Obsolete("Use SequenceEqualAllowNull")]
        public static bool Compare(IEnumerator leftIt, IEnumerator rightIt)
        {
            if (ReferenceEquals(leftIt, rightIt))
                return true;
            if (leftIt is null || rightIt is null)
                return false;

            bool hasLeftNext;
            bool hasRightNext;
            while (true)
            {
                hasLeftNext = leftIt.MoveNext();
                hasRightNext = rightIt.MoveNext();
                if (!hasLeftNext || !hasRightNext)
                    break;

                if (!Equals(leftIt.Current, rightIt.Current))
                    return false;
            }

            // If there is any left element
            if (hasLeftNext != hasRightNext)
                return false;

            return true;
        }

        /// <summary>
        /// Compares two collection, element by elements.
        /// </summary>
        /// <param name="first">The collection to compare from.</param>
        /// <param name="second">The colllection to compare to.</param>
        /// <returns>True if lists are identical (but no necessarely of the same time). False otherwise.</returns>
        public static bool Compare<TKey, TValue>(IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second)
        {
            if (ReferenceEquals(first, second)) return true;
            if (ReferenceEquals(first, null) || ReferenceEquals(second, null)) return false;
            if (first.Count != second.Count) return false;

            var comparer = EqualityComparer<TValue>.Default;

            foreach (var keyValue in first)
            {
                if (!second.TryGetValue(keyValue.Key, out var secondValue)) return false;
                if (!comparer.Equals(keyValue.Value, secondValue)) return false;
            }

            // Check that all keys in second are in first
            return second.Keys.All(first.ContainsKey);
        }

        /// <summary>
        /// Compares two collection, element by elements.
        /// </summary>
        /// <param name="first">The collection to compare from.</param>
        /// <param name="second">The colllection to compare to.</param>
        /// <returns>True if lists are identical (but not necessarily in the same order). False otherwise.</returns>
        /// <remarks>Concrete SortedList is favored over interface to avoid enumerator object allocation.</remarks>
        public static bool Compare<TKey, TValue>(Collections.SortedList<TKey, TValue> first, Collections.SortedList<TKey, TValue> second)
        {
            if (ReferenceEquals(first, second)) return true;
            if (ReferenceEquals(first, null) || ReferenceEquals(second, null)) return false;
            if (first.Count != second.Count) return false;

            var comparer = EqualityComparer<TValue>.Default;

            foreach (var keyValue in first)
            {
                if (!second.TryGetValue(keyValue.Key, out var secondValue)) return false;
                if (!comparer.Equals(keyValue.Value, secondValue)) return false;
            }

            return true;
        }

        [Obsolete("Use StrideCoreExtensions.SequenceEqualAllowNull")]
        public static bool Compare<T>(T[] left, T[] right)
            => left.SequenceEqualAllowNull(right, null);

        /// <summary>
        /// Compares two collection, element by elements.
        /// </summary>
        /// <param name="left">The collection to compare from.</param>
        /// <param name="right">The colllection to compare to.</param>
        /// <returns>True if lists are identical (but no necessarely of the same time). False otherwise.</returns>
        [Obsolete("Use StrideCoreExtensions.SequenceEqualAllowNull")]
        public static bool Compare<T>(ICollection<T> left, ICollection<T> right)
            => left.SequenceEqualAllowNull(right, null);

        /// <summary>
        /// Compares two list, element by elements.
        /// </summary>
        /// <param name="left">The list to compare from.</param>
        /// <param name="right">The colllection to compare to.</param>
        /// <returns>True if lists are sequentially equal. False otherwise.</returns>
        /// <remarks>Concrete List is favored over interface to avoid enumerator object allocation.</remarks>
        [Obsolete("Use StrideCoreExtensions.SequenceEqualAllowNull")]
        public static bool Compare<T>(List<T> left, List<T> right)
            => left.SequenceEqualAllowNull(right, null);

        /// <summary>
        /// Swaps the value between two references.
        /// </summary>
        /// <typeparam name="T">Type of a data to swap.</typeparam>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        public static void Swap<T>(ref T left, ref T right) => (right, left) = (left, right);

        /// <summary>Suspends the current thread for a duration.</summary>
        /// <param name="duration">The duration of sleep. Must be non-negative.</param>
        public static void Sleep(TimeSpan duration)
        { 
            if (duration.Ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(duration));
            Thread.Sleep(duration);
        }

        /// <summary>Suspends the current thread for a number of milliseconds.</summary>
        /// <param name="ms">The duration in milliseconds. Must be non-negative.</param>
        public static void Sleep(int ms)
        { 
            if (ms < 0)
                throw new ArgumentOutOfRangeException(nameof(ms));
            Thread.Sleep(ms);
        }

        /// <summary>
        /// Linq assisted full tree iteration and collection in a single line.
        /// Warning, could be slow.
        /// </summary>
        /// <typeparam name="T">The type to iterate.</typeparam>
        /// <param name="root">The root item</param>
        /// <param name="childrenF">The function to retrieve a child</param>
        public static IEnumerable<T> IterateTree<T>(T root, Func<T, IEnumerable<T>> childrenF)
        {
            var q = new List<T> { root };
            while (q.Any())
            {
                var c = q[0];
                q.RemoveAt(0);
                q.AddRange(childrenF(c) ?? Enumerable.Empty<T>());
                yield return c;
            }
        }

        /// <summary>
        /// Converts a <see cref="Stopwatch" /> raw time to a <see cref="TimeSpan" />.
        /// </summary>
        /// <param name="delta">The delta.</param>
        /// <returns>The <see cref="TimeSpan" />.</returns>
        public static TimeSpan ConvertRawToTimestamp(long delta)
            => delta == 0 ? default : TimeSpan.FromTicks(delta * TimeSpan.TicksPerSecond / Stopwatch.Frequency);
    }
}
