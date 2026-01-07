// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Contains code from TerraFX Framework, Copyright (c) Tanner Gooding and Contributors
// Licensed under the MIT License (MIT).

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using DotNetUnsafe = System.Runtime.CompilerServices.Unsafe;

namespace Stride.Core.UnsafeExtensions
{
    /// <summary>
    ///   Provides a set of methods to supplement or replace <see cref="System.Runtime.CompilerServices.Unsafe"/> and
    ///   <see cref="MemoryMarshal"/>, mainly for working with <see langword="struct"/>s and <see langword="unmanaged"/> types.
    /// </summary>
    public static unsafe class UnsafeUtilities
    {
        /// <inheritdoc cref="DotNetUnsafe.As{T}(object)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNullIfNotNull(nameof(o))]
        public static T? As<T>(this object? o)
            where T : class?
        {
            Debug.Assert(o is null or T);

            return DotNetUnsafe.As<T>(o);
        }

        /// <inheritdoc cref="DotNetUnsafe.As{TFrom, TTo}(ref TFrom)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TTo As<TFrom, TTo>(this ref TFrom source)
            where TFrom : unmanaged
            where TTo : unmanaged
            => ref DotNetUnsafe.As<TFrom, TTo>(ref source);

        /// <inheritdoc cref="DotNetUnsafe.As{TFrom, TTo}(ref TFrom)"/>
        /// <param name="span">The span to reinterpret.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<TTo> As<TFrom, TTo>(this Span<TFrom> span)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            Debug.Assert(SizeOf<TFrom>() == SizeOf<TTo>());

            return MemoryMarshal.CreateSpan(ref DotNetUnsafe.As<TFrom, TTo>(ref span.GetReference()), span.Length);
        }

        /// <inheritdoc cref="DotNetUnsafe.As{TFrom, TTo}(ref TFrom)"/>
        /// <param name="span">The span to reinterpret.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<TTo> As<TFrom, TTo>(this ReadOnlySpan<TFrom> span)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            Debug.Assert(SizeOf<TFrom>() == SizeOf<TTo>());

            return MemoryMarshal.CreateReadOnlySpan(in AsReadonly<TFrom, TTo>(in span.GetReference()), span.Length);
        }

        /// <summary>
        ///   Reinterprets a regular managed object as a read-only span of elements of another type. This can be useful
        ///   if a managed object represents a "fixed array".
        /// </summary>
        /// <param name="reference">A reference to data.</param>
        /// <returns>A read-only span representing the specified reference as elements of type <typeparamref name="TTo"/>.</returns>
        /// <remarks>
        ///   This method should be used with caution. Even though the <see langword="ref"/> is annotated as <see langword="scoped"/>,
        ///   it will be stored into the returned span, and the lifetime of the returned span will not be validated for safety, even by
        ///   span-aware languages.
        /// </remarks>
        public static ReadOnlySpan<TTo> AsReadOnlySpan<TFrom, TTo>(this scoped ref TFrom reference)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            // NOTE: `reference` should be passed as `ref readonly`, but that results in error CS8338.
            //       It must not be modified by this method however

            Debug.Assert(SizeOf<TFrom>() % SizeOf<TTo>() == 0);

            ref readonly var referenceAsTTo = ref AsReadonly<TFrom, TTo>(in reference);
            uint elementCount = SizeOf<TFrom>() / SizeOf<TTo>();
            return CreateReadOnlySpan(in referenceAsTTo, (int) elementCount);
        }

        /// <summary>
        ///   Reinterprets a regular managed object as an array of elements of another type and returns a read-only span over
        ///   a portion of that array. This can be useful if a managed object represents a "fixed array".
        ///   This is dangerous because the <paramref name="elementCount"/> is not checked.
        /// </summary>
        /// <param name="reference">A reference to data.</param>
        /// <param name="elementCount">The number of <typeparamref name="TTo"/> elements the memory contains.</param>
        /// <returns>
        ///   A read-only span representing the specified reference as <paramref name="elementCount"/> elements of type <typeparamref name="TTo"/>.
        /// </returns>
        /// <remarks>
        ///   This method should be used with caution. It is dangerous because the <paramref name="elementCount"/> argument is not checked.
        ///   Even though the <see langword="ref"/> is annotated as <see langword="scoped"/>, it will be stored into the returned span,
        ///   and the lifetime of the returned span will not be validated for safety, even by span-aware languages.
        /// </remarks>
        public static ReadOnlySpan<TTo> AsReadOnlySpan<TFrom, TTo>(this scoped ref TFrom reference, int elementCount)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            // NOTE: `reference` should be passed as `ref readonly`, but that results in error CS8338.
            //       It must not be modified by this method however

            Debug.Assert((SizeOf<TTo>() * elementCount) <= SizeOf<TFrom>());

            ref readonly var referenceAsTTo = ref AsReadonly<TFrom, TTo>(in reference);
            return CreateReadOnlySpan(in referenceAsTTo, elementCount);
        }

        /// <summary>
        ///   Reinterprets a regular managed object as a span of elements of another type. This can be useful
        ///   if a managed object represents a "fixed array".
        /// </summary>
        /// <param name="reference">A reference to data.</param>
        /// <returns>A span representing the specified reference as elements of type <typeparamref name="TTo"/>.</returns>
        /// <remarks>
        ///   This method should be used with caution. Even though the <see langword="ref"/> is annotated as <see langword="scoped"/>,
        ///   it will be stored into the returned span, and the lifetime of the returned span will not be validated for safety, even by
        ///   span-aware languages.
        /// </remarks>
        public static Span<TTo> AsSpan<TFrom, TTo>(this scoped ref TFrom reference)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            Debug.Assert(SizeOf<TFrom>() % SizeOf<TTo>() == 0);

            ref var referenceAsTTo = ref AsRef<TFrom, TTo>(in reference);
            uint elementCount = SizeOf<TFrom>() / SizeOf<TTo>();
            return CreateSpan(ref referenceAsTTo, (int) elementCount);
        }

        /// <summary>
        ///   Reinterprets a regular managed object as an array of elements of another type and returns a span over
        ///   a portion of that array. This can be useful if a managed object represents a "fixed array".
        ///   This is dangerous because the <paramref name="elementCount"/> is not checked.
        /// </summary>
        /// <param name="reference">A reference to data.</param>
        /// <param name="elementCount">The number of <typeparamref name="TTo"/> elements the memory contains.</param>
        /// <returns>
        ///   A span representing the specified reference as <paramref name="elementCount"/> elements of type <typeparamref name="TTo"/>.
        /// </returns>
        /// <remarks>
        ///   This method should be used with caution. It is dangerous because the <paramref name="elementCount"/> argument is not checked.
        ///   Even though the <see langword="ref"/> is annotated as <see langword="scoped"/>, it will be stored into the returned span,
        ///   and the lifetime of the returned span will not be validated for safety, even by span-aware languages.
        /// </remarks>
        public static Span<TTo> AsSpan<TFrom, TTo>(this scoped ref TFrom reference, int elementCount)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            Debug.Assert((SizeOf<TTo>() * elementCount) <= SizeOf<TFrom>());

            ref var referenceAsTTo = ref AsRef<TFrom, TTo>(in reference);
            return CreateSpan(ref referenceAsTTo, elementCount);
        }

        /// <summary>
        ///   Creates a new read-only span over the target array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <returns>A read-only span over the provided <paramref name="array"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[]? array) => array;

        /// <summary>
        ///   Reinterprets a regular array as a read-only span of elements of another type. This can be useful
        ///   if the types are the same size and interchangeable.
        /// </summary>
        /// <param name="array">An array of data.</param>
        /// <returns>
        ///   A read-only span representing the data of <paramref name="array"/> reinterpreted as elements of type <typeparamref name="TTo"/>.
        ///   If the array is <see langword="null"/>, returns an empty span.
        /// </returns>
        public static ReadOnlySpan<TTo> AsReadOnlySpan<TFrom, TTo>(this TFrom[]? array)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            Debug.Assert(SizeOf<TFrom>() == SizeOf<TTo>());

            if (array is null)
                return default;

            ref var refArray0 = ref MemoryMarshal.GetArrayDataReference(array);
            return MemoryMarshal.CreateReadOnlySpan(in AsReadonly<TFrom, TTo>(in refArray0), array.Length);
        }

        /// <summary>
        ///   Reinterprets a regular array as a span of elements of another type. This can be useful
        ///   if the types are the same size and interchangeable.
        /// </summary>
        /// <param name="array">An array of data.</param>
        /// <returns>
        ///   A span representing the data of <paramref name="array"/> reinterpreted as elements of type <typeparamref name="TTo"/>.
        ///   If the array is <see langword="null"/>, returns an empty span.
        /// </returns>
        public static Span<TTo> AsSpan<TFrom, TTo>(this TFrom[]? array)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            Debug.Assert(SizeOf<TFrom>() == SizeOf<TTo>());

            if (array is null)
                return default;

            ref var refArray0 = ref MemoryMarshal.GetArrayDataReference(array);
            return MemoryMarshal.CreateSpan(ref As<TFrom, TTo>(ref refArray0), array.Length);
        }

        /// <inheritdoc cref="DotNetUnsafe.AsPointer{T}(ref T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AsPointer<T>(this ref T value) where T : unmanaged
            => (T*) DotNetUnsafe.AsPointer(ref value);

        /// <inheritdoc cref="DotNetUnsafe.As{TFrom, TTo}(ref TFrom)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly TTo AsReadonly<TFrom, TTo>(ref readonly TFrom source)
            => ref DotNetUnsafe.As<TFrom, TTo>(ref AsRef(in source));

        /// <inheritdoc cref="DotNetUnsafe.AsPointer{T}(ref T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AsReadonlyPointer<T>(ref readonly T value) where T : unmanaged
            => AsPointer(ref AsRef(in value));

        /// <summary>
        ///   Reinterprets the given native integer as a reference.
        /// </summary>
        /// <typeparam name="T">The type of the reference.</typeparam>
        /// <param name="source">The native integer to reinterpret.</param>
        /// <returns>A reference to a value of type <typeparamref name="T"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsRef<T>(nint source) => ref DotNetUnsafe.AsRef<T>((void*) source);

        /// <summary>
        ///   Reinterprets the given native unsigned integer as a reference.
        /// </summary>
        /// <typeparam name="T">The type of the reference.</typeparam>
        /// <param name="source">The native unsigned integer to reinterpret.</param>
        /// <returns>A reference to a value of type <typeparamref name="T"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsRef<T>(nuint source) => ref DotNetUnsafe.AsRef<T>((void*) source);

        /// <inheritdoc cref="DotNetUnsafe.AsRef{T}(ref readonly T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsRef<T>(scoped ref readonly T source) => ref DotNetUnsafe.AsRef(in source);

        /// <inheritdoc cref="DotNetUnsafe.AsRef{T}(ref readonly T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TTo AsRef<TFrom, TTo>(scoped ref readonly TFrom source)
        {
            ref var mutable = ref DotNetUnsafe.AsRef(in source);
            return ref DotNetUnsafe.As<TFrom, TTo>(ref mutable);
        }

        /// <inheritdoc cref="DotNetUnsafe.AsRef{T}(void*)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsRef<T>(void* source) => ref DotNetUnsafe.AsRef<T>(source);

        /// <summary>
        ///   Reinterprets the read-only span as a writeable span.
        /// </summary>
        /// <typeparam name="T">The type of items in <paramref name="span"/>.</typeparam>
        /// <param name="span">The read-only span to reinterpret.</param>
        /// <returns>A writeable span that points to the same items as <paramref name="span"/>.</returns>
        public static Span<T> AsSpan<T>(this ReadOnlySpan<T> span)
            => MemoryMarshal.CreateSpan(ref DotNetUnsafe.AsRef(in span.GetReference()), span.Length);

        /// <inheritdoc cref="MemoryMarshal.AsBytes{T}(ReadOnlySpan{T})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<byte> AsBytes<T>(this ReadOnlySpan<T> span) where T : struct
            => MemoryMarshal.AsBytes(span);

        /// <inheritdoc cref="MemoryMarshal.AsBytes{T}(Span{T})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<byte> AsBytes<T>(this Span<T> span) where T : struct
            => MemoryMarshal.AsBytes(span);

        /// <inheritdoc cref="DotNetUnsafe.BitCast{TFrom, TTo}(TFrom)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TTo BitCast<TFrom, TTo>(this TFrom source)
            where TFrom : struct
            where TTo : struct
        {
            return DotNetUnsafe.BitCast<TFrom, TTo>(source);
        }

        /// <inheritdoc cref="MemoryMarshal.Cast{TFrom, TTo}(Span{TFrom})"/>
        public static Span<TTo> Cast<TFrom, TTo>(this Span<TFrom> span)
            where TFrom : struct
            where TTo : struct
        {
            return MemoryMarshal.Cast<TFrom, TTo>(span);
        }

        /// <inheritdoc cref="MemoryMarshal.Cast{TFrom, TTo}(ReadOnlySpan{TFrom})"/>
        public static ReadOnlySpan<TTo> Cast<TFrom, TTo>(this ReadOnlySpan<TFrom> span)
            where TFrom : struct
            where TTo : struct
        {
            return MemoryMarshal.Cast<TFrom, TTo>(span);
        }


        /// <inheritdoc cref="DotNetUnsafe.CopyBlock(ref byte, ref readonly byte, uint)"/>
        public static void CopyBlock<TDestination, TSource>(ref TDestination destination, ref readonly TSource source, uint byteCount)
        {
            DotNetUnsafe.CopyBlock(destination: ref DotNetUnsafe.As<TDestination, byte>(ref destination),
                                   source: in AsReadonly<TSource, byte>(in source),
                                   byteCount);
        }

        /// <inheritdoc cref="DotNetUnsafe.CopyBlockUnaligned(ref byte, ref readonly byte, uint)"/>
        public static void CopyBlockUnaligned<TDestination, TSource>(ref TDestination destination, ref readonly TSource source, uint byteCount)
        {
            DotNetUnsafe.CopyBlockUnaligned(destination: ref DotNetUnsafe.As<TDestination, byte>(ref destination),
                                            source: in AsReadonly<TSource, byte>(in source),
                                            byteCount);
        }


        /// <inheritdoc cref="MemoryMarshal.CreateSpan{T}(ref T, int)"/>
        public static Span<T> CreateSpan<T>(scoped ref T reference, int length)
            => MemoryMarshal.CreateSpan(ref reference, length);

        /// <inheritdoc cref="MemoryMarshal.CreateReadOnlySpan{T}(ref readonly T, int)"/>
        public static ReadOnlySpan<T> CreateReadOnlySpan<T>(scoped ref readonly T reference, int length)
            => MemoryMarshal.CreateReadOnlySpan(in reference, length);


        /// <summary>
        ///   Returns a pointer to the element of the span at index zero.
        /// </summary>
        /// <typeparam name="T">The type of items in <paramref name="span"/>.</typeparam>
        /// <param name="span">The span from which the pointer is retrieved.</param>
        /// <returns>A pointer to the item at index zero of <paramref name="span"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* GetPointer<T>(this Span<T> span) where T : unmanaged
            => (T*) DotNetUnsafe.AsPointer(ref span.GetReference());

        /// <summary>
        ///   Returns a pointer to the element of the span at index zero.
        /// </summary>
        /// <typeparam name="T">The type of items in <paramref name="span"/>.</typeparam>
        /// <param name="span">The span from which the pointer is retrieved.</param>
        /// <returns>A pointer to the item at index zero of <paramref name="span"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* GetPointer<T>(this ReadOnlySpan<T> span) where T : unmanaged
            => (T*) DotNetUnsafe.AsPointer(ref AsRef(in span.GetReference()));


        /// <inheritdoc cref="MemoryMarshal.GetArrayDataReference{T}(T[])"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetReference<T>(this T[] array)
            => ref MemoryMarshal.GetArrayDataReference(array);

        /// <inheritdoc cref="MemoryMarshal.GetArrayDataReference{T}(T[])"/>
        /// <summary>
        ///   Returns a reference to the element at the specified <paramref name="index"/> of <paramref name="array"/>.
        ///   If the array is empty, returns a reference to where that element would have been stored.
        ///   Such a reference may be used for pinning but must never be dereferenced.
        /// </summary>
        /// <param name="index">The index of the element of <paramref name="array"/> for which to take a reference.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetReference<T>(this T[] array, int index)
            => ref DotNetUnsafe.Add(ref array.GetReference(), index);

        /// <inheritdoc cref="MemoryMarshal.GetArrayDataReference{T}(T[])"/>
        /// <summary>
        ///   Returns a reference to the element at the specified <paramref name="index"/> of <paramref name="array"/>.
        ///   If the array is empty, returns a reference to where that element would have been stored.
        ///   Such a reference may be used for pinning but must never be dereferenced.
        /// </summary>
        /// <param name="index">The index of the element of <paramref name="array"/> for which to take a reference.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetReference<T>(this T[] array, nuint index) => ref DotNetUnsafe.Add(ref array.GetReference(), index);

        /// <inheritdoc cref="MemoryMarshal.GetReference{T}(Span{T})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetReference<T>(this Span<T> span)
            => ref MemoryMarshal.GetReference(span);

        /// <inheritdoc cref="MemoryMarshal.GetReference{T}(Span{T})"/>
        /// <summary>
        ///   Returns a reference to the element at the specified <paramref name="index"/> of <paramref name="span"/>.
        ///   If the span is empty, returns a reference to the location where that element would have been stored.
        ///   Such a reference may or may not be null. It can be used for pinning but must never be dereferenced.
        /// </summary>
        /// <param name="index">The index of the element of <paramref name="span"/> for which to take a reference.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetReference<T>(this Span<T> span, int index)
            => ref DotNetUnsafe.Add(ref MemoryMarshal.GetReference(span), index);

        /// <inheritdoc cref="MemoryMarshal.GetReference{T}(Span{T})"/>
        /// <summary>
        ///   Returns a reference to the element at the specified <paramref name="index"/> of <paramref name="span"/>.
        ///   If the span is empty, returns a reference to the location where that element would have been stored.
        ///   Such a reference may or may not be null. It can be used for pinning but must never be dereferenced.
        /// </summary>
        /// <param name="index">The index of the element of <paramref name="span"/> for which to take a reference.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetReference<T>(this Span<T> span, nuint index)
            => ref DotNetUnsafe.Add(ref MemoryMarshal.GetReference(span), index);

        /// <inheritdoc cref="MemoryMarshal.GetReference{T}(ReadOnlySpan{T})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T GetReference<T>(this ReadOnlySpan<T> span)
            => ref MemoryMarshal.GetReference(span);

        /// <inheritdoc cref="MemoryMarshal.GetReference{T}(ReadOnlySpan{T})"/>
        /// <summary>
        ///   Returns a reference to the element at the specified <paramref name="index"/> of <paramref name="span"/>.
        ///   If the span is empty, returns a reference to the location where that element would have been stored.
        ///   Such a reference may or may not be null. It can be used for pinning but must never be dereferenced.
        /// </summary>
        /// <param name="index">The index of the element of <paramref name="span"/> for which to take a reference.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T GetReference<T>(this ReadOnlySpan<T> span, int index)
            => ref DotNetUnsafe.Add(ref MemoryMarshal.GetReference(span), index);

        /// <inheritdoc cref="MemoryMarshal.GetReference{T}(ReadOnlySpan{T})"/>
        /// <summary>
        ///   Returns a reference to the element at the specified <paramref name="index"/> of <paramref name="span"/>.
        ///   If the span is empty, returns a reference to the location where that element would have been stored.
        ///   Such a reference may or may not be null. It can be used for pinning but must never be dereferenced.
        /// </summary>
        /// <param name="index">The index of the element of <paramref name="span"/> for which to take a reference.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T GetReference<T>(this ReadOnlySpan<T> span, nuint index)
            => ref DotNetUnsafe.Add(ref MemoryMarshal.GetReference(span), index);


        /// <summary>
        ///   Determines if a given reference to a value of type <typeparamref name="T"/> is not a null reference.
        /// </summary>
        /// <typeparam name="T">The type of the reference.</typeparam>
        /// <param name="source">The reference to check.</param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="source"/> is not a null reference;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNotNullRef<T>(ref readonly T source) => !DotNetUnsafe.IsNullRef(in source);

        /// <inheritdoc cref="DotNetUnsafe.IsNullRef{T}(ref readonly T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullRef<T>(ref readonly T source) => DotNetUnsafe.IsNullRef(in source);

        /// <inheritdoc cref="DotNetUnsafe.NullRef{T}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T NullRef<T>() => ref DotNetUnsafe.NullRef<T>();


        /// <inheritdoc cref="DotNetUnsafe.ReadUnaligned{T}(void*)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadUnaligned<T>(void* source) where T : unmanaged
            => DotNetUnsafe.ReadUnaligned<T>(source);

        /// <inheritdoc cref="DotNetUnsafe.ReadUnaligned{T}(void*)"/>
        /// <param name="offset">The offset in bytes from the location pointed to by <paramref name="source"/>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadUnaligned<T>(void* source, nuint offset) where T : unmanaged
            => DotNetUnsafe.ReadUnaligned<T>((void*) ((nuint) source + offset));


        /// <inheritdoc cref="DotNetUnsafe.WriteUnaligned{T}(void*, T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUnaligned<T>(void* destination, T value) where T : unmanaged
            => DotNetUnsafe.WriteUnaligned(destination, value);

        /// <inheritdoc cref="DotNetUnsafe.WriteUnaligned{T}(void*, T)"/>
        /// <param name="offset">The offset in bytes from the location pointed to by <paramref name="destination"/>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUnaligned<T>(void* destination, nuint offset, T value) where T : unmanaged
            => DotNetUnsafe.WriteUnaligned((void*) ((nuint) destination + offset), value);


#pragma warning disable CS8500
        /// <inheritdoc cref="DotNetUnsafe.SizeOf{T}"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SizeOf<T>() => unchecked((uint) sizeof(T));
#pragma warning restore CS8500
    }
}
