// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Contains code from TerraFX Framework, Copyright (c) Tanner Gooding and Contributors
// Licensed under the MIT License (MIT).

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using static Stride.Core.UnsafeExtensions.UnsafeUtilities;

namespace Stride.Core.UnsafeExtensions;

/// <summary>
///   Provides a set of methods to supplement or replace <see cref="Marshal"/> when operating on <see cref="string"/>s
///   and spans of characters.
/// </summary>
public static unsafe class StringMarshal
{
    /// <summary>
    ///   Gets a <see cref="string"/> for a pointer to a null-terminated string of bytes,
    ///   assuming an UTF-8 encoding, the current system codepage, or ANSI.
    /// </summary>
    /// <param name="ptr">
    ///   A pointer to a null-terminated array of 8-bit integers.
    ///   The integers are interpreted using the current system code page encoding on Windows (referred to as CP_ACP) and as UTF-8 encoding on non-Windows.
    /// </param>
    /// <returns>A string created from <paramref name="ptr"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetString(byte* ptr)
        => ptr is not null
            ? new string((sbyte*) ptr)
            : null;

    /// <summary>
    ///   Gets a <see cref="string"/> for a given span of bytes, assuming an UTF-8 encoding.
    /// </summary>
    /// <param name="span">The span for which to create the string.</param>
    /// <returns>A string created from <paramref name="span"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetString(this ReadOnlySpan<byte> span)
        => span.GetPointer() is not null
            ? Encoding.UTF8.GetString(span)
            : null;

    /// <summary>
    ///   Gets a <see cref="string"/> for a given span, assuming an UTF-16 encoding.
    /// </summary>
    /// <param name="span">The span for which to create the string.</param>
    /// <returns>A string created from <paramref name="span"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetString(this ReadOnlySpan<ushort> span)
        => span.GetPointer() is not null
            ? new string(span.As<ushort, char>())
            : null;

    /// <summary>
    ///   Gets a <see cref="string"/> for a pointer to a null-terminated string of characters,
    ///   assuming an UTF-16 encoding.
    /// </summary>
    /// <param name="ptr">A pointer to a null-terminated array of 16-bit characters.</param>
    /// <returns>A string created from <paramref name="ptr"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetString(char* ptr)
        => ptr is not null
            ? new string(ptr)
            : null;

    /// <summary>
    ///   Gets a <see cref="string"/> for a given span, assuming an UTF-16 encoding.
    /// </summary>
    /// <param name="span">The span for which to create the string.</param>
    /// <returns>A string created from <paramref name="span"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetString(this ReadOnlySpan<char> span)
        => span.GetPointer() is not null
            ? new string(span)
            : null;

    /// <summary>
    ///   Gets a null-terminated sequence of ASCII characters for a <see cref="string"/>.
    /// </summary>
    /// <param name="source">The string for which to marshal.</param>
    /// <returns>
    ///   A <see cref="ReadOnlySpan{Byte}"/> containing a null-terminated ASCII string
    ///   that is equivalent to <paramref name="source"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> GetAsciiSpan(this string source)
    {
        ReadOnlySpan<byte> result;

        if (source is not null)
        {
            var maxLength = Encoding.ASCII.GetMaxByteCount(source.Length);
            var bytes = new byte[maxLength + 1];

            var length = Encoding.ASCII.GetBytes(source, bytes);
            result = bytes.AsSpan(0, length);
        }
        else
        {
            result = null;
        }

        return result;
    }

    /// <summary>
    ///   Gets a span for a null-terminated ASCII character sequence.
    /// </summary>
    /// <param name="source">The pointer to a null-terminated ASCII character sequence.</param>
    /// <param name="maxLength">
    ///   The maxmimum length of <paramref name="source"/> or <c>-1</c> if the maximum length is unknown.
    /// </param>
    /// <returns>
    ///   A <see cref="ReadOnlySpan{Byte}"/> that starts at <paramref name="source"/> and extends to
    ///   <paramref name="maxLength"/> or the first null character, whichever comes first.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> GetAsciiSpan(byte* source, int maxLength = -1)
        => GetUtf8Span(source, maxLength);

    /// <summary>
    ///   Gets a span for a null-terminated ASCII character sequence.
    /// </summary>
    /// <param name="source">The reference to a null-terminated ASCII character sequence.</param>
    /// <param name="maxLength">
    ///   The maxmimum length of <paramref name="source"/> or <c>-1</c> if the maximum length is unknown.
    /// </param>
    /// <returns>
    ///   A <see cref="ReadOnlySpan{Byte}"/> that starts at <paramref name="source"/> and extends to
    ///   <paramref name="maxLength"/> or the first null character, whichever comes first.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> GetAsciiSpan(in byte source, int maxLength = -1)
        => GetUtf8Span(in source, maxLength);

    /// <summary>
    ///   Gets a null-terminated sequence of UTF-8 characters for a <see cref="string"/>.
    /// </summary>
    /// <param name="source">The string for which to get the null-terminated UTF8 character sequence.</param>
    /// <returns>
    ///   A <see cref="ReadOnlySpan{Byte}"/> containing a null-terminated UTF-8 string
    ///   that is equivalent to <paramref name="source"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> GetUtf8Span(this string source)
    {
        ReadOnlySpan<byte> result;

        if (source is not null)
        {
            var maxLength = Encoding.UTF8.GetMaxByteCount(source.Length);
            var bytes = new byte[maxLength + 1];

            var length = Encoding.UTF8.GetBytes(source, bytes);
            result = bytes.AsSpan(0, length);
        }
        else
        {
            result = null;
        }

        return result;
    }

    /// <summary>
    ///   Gets a span for a null-terminated UTF-8 character sequence.
    /// </summary>
    /// <param name="source">The pointer to a null-terminated UTF-8 character sequence.</param>
    /// <param name="maxLength">
    ///   The maxmimum length of <paramref name="source"/> or <c>-1</c> if the maximum length is unknown.
    /// </param>
    /// <returns>
    ///   A <see cref="ReadOnlySpan{Byte}"/> that starts at <paramref name="source"/> and extends to
    ///   <paramref name="maxLength"/> or the first null character, whichever comes first.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> GetUtf8Span(byte* source, int maxLength = -1)
        => source is not null
            ? GetUtf8Span(in source[0], maxLength)
            : null;

    /// <summary>
    ///   Gets a span for a null-terminated UTF-8 character sequence.
    /// </summary>
    /// <param name="source">The reference to a null-terminated UTF-8 character sequence.</param>
    /// <param name="maxLength">
    ///   The maxmimum length of <paramref name="source"/> or <c>-1</c> if the maximum length is unknown.
    /// </param>
    /// <returns>
    ///   A <see cref="ReadOnlySpan{Byte}"/> that starts at <paramref name="source"/> and extends to
    ///   <paramref name="maxLength"/> or the first null character, whichever comes first.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> GetUtf8Span(in byte source, int maxLength = -1)
    {
        ReadOnlySpan<byte> result;

        if (!IsNullRef(in source))
        {
            if (maxLength < 0)
                maxLength = int.MaxValue;

            result = CreateReadOnlySpan(in source, maxLength);
            var length = result.IndexOf((byte) '\0');

            if (length != -1)
            {
                result = result[..length];
            }
        }
        else
        {
            result = null;
        }

        return result;
    }

    /// <summary>
    ///   Gets a null-terminated sequence of UTF-16 characters for a <see cref="string"/>.
    /// </summary>
    /// <param name="source">The string for which to get the null-terminated UTF-16 character sequence.</param>
    /// <returns>
    ///   A <see cref="ReadOnlySpan{UInt16}"/> containing a null-terminated UTF-16 string
    ///   that is equivalent to <paramref name="source"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<ushort> GetUtf16Span(this string source)
        => source.AsSpan().As<char, ushort>();

    /// <summary>
    ///   Gets a span for a null-terminated UTF-16 character sequence.
    /// </summary>
    /// <param name="source">The pointer to a null-terminated UTF-16 string.</param>
    /// <param name="maxLength">
    ///   The maxmimum length of <paramref name="source"/> or <c>-1</c> if the maximum length is unknown.
    /// </param>
    /// <returns>
    ///   A <see cref="ReadOnlySpan{UInt16}"/> that starts at <paramref name="source"/> and extends to
    ///   <paramref name="maxLength"/> or the first null character, whichever comes first.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<ushort> GetUtf16Span(ushort* source, int maxLength = -1)
        => source is null
            ? GetUtf16Span(in source[0], maxLength)
            : null;

    /// <summary>
    ///   Gets a span for a null-terminated UTF-16 character sequence.
    /// </summary>
    /// <param name="source">The reference to a null-terminated UTF-16 string.</param>
    /// <param name="maxLength">
    ///   The maxmimum length of <paramref name="source"/> or <c>-1</c> if the maximum length is unknown.
    /// </param>
    /// <returns>
    ///   A <see cref="ReadOnlySpan{UInt16}"/> that starts at <paramref name="source"/> and extends to
    ///   <paramref name="maxLength"/> or the first null character, whichever comes first.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<ushort> GetUtf16Span(in ushort source, int maxLength = -1)
    {
        ReadOnlySpan<ushort> result;

        if (!IsNullRef(in source))
        {
            if (maxLength < 0)
                maxLength = int.MaxValue;

            result = CreateReadOnlySpan(in source, maxLength);
            var length = result.IndexOf('\0');

            if (length != -1)
            {
                result = result[..length];
            }
        }
        else
        {
            result = null;
        }

        return result;
    }
}
